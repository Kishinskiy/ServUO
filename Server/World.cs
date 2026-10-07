#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using CustomsFramework;
using Server.Guilds;
using Server.Network;

#endregion

namespace Server
{
    public static class World
    {
        private static readonly bool m_Metrics = Config.Get("General.Metrics", false);

        private static readonly ManualResetEvent m_DiskWriteHandle = new ManualResetEvent(true);

        private static Queue<IEntity> _addQueue, _deleteQueue;
        private static Queue<ICustomsEntity> _CustomsAddQueue, _CustomsDeleteQueue;

        public static readonly string MobileIndexPath = Path.Combine("Saves/Mobiles/", "Mobiles.idx");
        public static readonly string MobileTypesPath = Path.Combine("Saves/Mobiles/", "Mobiles.tdb");
        public static readonly string MobileDataPath = Path.Combine("Saves/Mobiles/", "Mobiles.bin");

        public static readonly string ItemIndexPath = Path.Combine("Saves/Items/", "Items.idx");
        public static readonly string ItemTypesPath = Path.Combine("Saves/Items/", "Items.tdb");
        public static readonly string ItemDataPath = Path.Combine("Saves/Items/", "Items.bin");

        public static readonly string GuildIndexPath = Path.Combine("Saves/Guilds/", "Guilds.idx");
        public static readonly string GuildDataPath = Path.Combine("Saves/Guilds/", "Guilds.bin");

        public static readonly string DataIndexPath = Path.Combine("Saves/Customs/", "SaveData.idx");
        public static readonly string DataTypesPath = Path.Combine("Saves/Customs/", "SaveData.tdb");
        public static readonly string DataBinaryPath = Path.Combine("Saves/Customs/", "SaveData.bin");

        private static readonly Type[] m_SerialTypeArray = new Type[1] { typeof(Serial) };
        private static readonly Type[] _CustomSerialTypeArray = new Type[1] { typeof(CustomSerial) };

        internal static int m_Saves;

        internal static List<Type> m_ItemTypes = new List<Type>();
        internal static List<Type> m_MobileTypes = new List<Type>();
        internal static List<Type> _DataTypes = new List<Type>();

        public static bool Saving { get; private set; }

        public static bool Loaded { get; private set; }

        public static bool Loading { get; private set; }

        public static Dictionary<Serial, Mobile> Mobiles { get; private set; }

        public static Dictionary<Serial, Item> Items { get; private set; }

        public static Dictionary<CustomSerial, SaveData> Data { get; private set; }

        public static string LoadingType { get; private set; }

        public static void NotifyDiskWriteComplete()
        {
            if (m_DiskWriteHandle.Set()) Console.WriteLine("Closing Save Files. ");
        }

        public static void WaitForWriteCompletion()
        {
            m_DiskWriteHandle.WaitOne();
        }

        public static bool OnDelete(IEntity entity)
        {
            if (Saving || Loading)
            {
                if (Saving) AppendSafetyLog("delete", entity);

                _deleteQueue.Enqueue(entity);

                return false;
            }

            return true;
        }

        public static bool OnDelete(ICustomsEntity entity)
        {
            if (Saving || Loading)
            {
                if (Saving) AppendSafetyLog("delete", entity);

                _CustomsDeleteQueue.Enqueue(entity);

                return false;
            }

            return true;
        }

        public static void Broadcast(int hue, bool ascii, string text)
        {
            Broadcast(hue, ascii, AccessLevel.Player, text);
        }

        public static void Broadcast(int hue, bool ascii, AccessLevel access, string text)
        {
            var e = new WorldBroadcastEventArgs(hue, ascii, access, text);

            EventSink.InvokeWorldBroadcast(e);

            hue = e.Hue;
            ascii = e.Ascii;
            text = e.Text;
            access = e.Access;

            if (string.IsNullOrWhiteSpace(text)) return;

            Packet p;

            // Автоматически определяем, есть ли в тексте русские буквы
            var hasCyrillic = false;
            if (!string.IsNullOrEmpty(text))
                for (var i = 0; i < text.Length; i++)
                    if (text[i] >= 0x0400 && text[i] <= 0x04FF)
                    {
                        hasCyrillic = true;
                        break;
                    }

            // Если в строке есть кириллица — принудительно переключаем пакет в Unicode режим(РУС локализация)
            if (ascii && !hasCyrillic)
                p = new AsciiMessage(Serial.MinusOne, -1, MessageType.Regular, hue, 3, "System", text);
            else
                // Отправляем Unicode пакет с языковым маркером "RUS", который Mono на Mac и клиент UO переварят идеально
                p = new UnicodeMessage(Serial.MinusOne, -1, MessageType.Regular, hue, 3, "RUS", "System", text);

            var list = NetState.Instances;

            p.Acquire();

            foreach (var s in list)
                if (s.Mobile != null && s.Mobile.AccessLevel >= access)
                    s.Send(p);

            p.Release();

            NetState.FlushAll();
        }

        public static void Broadcast(int hue, bool ascii, string format, params object[] args)
        {
            Broadcast(hue, ascii, AccessLevel.Player, format, args);
        }

        public static void Broadcast(int hue, bool ascii, AccessLevel access, string format, params object[] args)
        {
            Broadcast(hue, ascii, access, string.Format(format, args));
        }

        private static List<Tuple<ConstructorInfo, string>> ReadTypes(BinaryReader tdbReader)
        {
            var count = tdbReader.ReadInt32();

            var types = new List<Tuple<ConstructorInfo, string>>(count);

            for (var i = 0; i < count; ++i)
            {
                var typeName = tdbReader.ReadString();

                var t = ScriptCompiler.FindTypeByFullName(typeName);

                if (t == null)
                {
                    Console.WriteLine("failed");

                    if (!Core.Service)
                    {
                        Console.WriteLine("Error: Type '{0}' was not found. Delete all of those types? (y/n)",
                            typeName);

                        if (Console.ReadKey(true).Key == ConsoleKey.Y)
                        {
                            types.Add(null);
                            Utility.PushColor(ConsoleColor.Yellow);
                            Console.Write("World: Loading...");
                            Utility.PopColor();
                            continue;
                        }

                        Console.WriteLine("Types will not be deleted. An exception will be thrown.");
                    }
                    else
                    {
                        Console.WriteLine("Error: Type '{0}' was not found.", typeName);
                    }

                    throw new Exception(string.Format("Bad type '{0}'", typeName));
                }

                var ctor = t.GetConstructor(m_SerialTypeArray);
                var cctor = t.GetConstructor(_CustomSerialTypeArray);

                if (ctor != null)
                    types.Add(new Tuple<ConstructorInfo, string>(ctor, typeName));
                else if (cctor != null)
                    types.Add(new Tuple<ConstructorInfo, string>(cctor, typeName));
                else
                    throw new Exception(string.Format("Type '{0}' does not have a serialization constructor", t));
            }

            return types;
        }

        public static void Load()
        {
            if (Loaded) return;

            Loaded = true;
            LoadingType = null;

            Utility.PushColor(ConsoleColor.Yellow);
            Console.WriteLine("World: Loading...");
            Utility.PopColor();

            var watch = Stopwatch.StartNew();

            Loading = true;

            _addQueue = new Queue<IEntity>();
            _deleteQueue = new Queue<IEntity>();
            _CustomsAddQueue = new Queue<ICustomsEntity>();
            _CustomsDeleteQueue = new Queue<ICustomsEntity>();

            int mobileCount = 0, itemCount = 0, guildCount = 0, dataCount = 0;

            var ctorArgs = new object[1];

            var items = new List<ItemEntry>();
            var mobiles = new List<MobileEntry>();
            var guilds = new List<GuildEntry>();
            var data = new List<DataEntry>();

            if (File.Exists(MobileIndexPath) && File.Exists(MobileTypesPath))
                using (var idx = new FileStream(MobileIndexPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var idxReader = new BinaryReader(idx);

                    using (var tdb = new FileStream(MobileTypesPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var tdbReader = new BinaryReader(tdb);

                        var types = ReadTypes(tdbReader);

                        mobileCount = idxReader.ReadInt32();

                        Mobiles = new Dictionary<Serial, Mobile>(mobileCount);

                        for (var i = 0; i < mobileCount; ++i)
                        {
                            var typeID = idxReader.ReadInt32();
                            var serial = idxReader.ReadInt32();
                            var pos = idxReader.ReadInt64();
                            var length = idxReader.ReadInt32();

                            var objs = types[typeID];

                            if (objs == null) continue;

                            Mobile m = null;
                            var ctor = objs.Item1;
                            var typeName = objs.Item2;

                            try
                            {
                                ctorArgs[0] = (Serial)serial;
                                m = (Mobile)ctor.Invoke(ctorArgs);
                            }
                            catch
                            {
                            }

                            if (m != null)
                            {
                                mobiles.Add(new MobileEntry(m, typeID, typeName, pos, length));
                                AddMobile(m);
                            }
                        }

                        tdbReader.Close();
                    }

                    idxReader.Close();
                }
            else
                Mobiles = new Dictionary<Serial, Mobile>();

            if (File.Exists(ItemIndexPath) && File.Exists(ItemTypesPath))
                using (var idx = new FileStream(ItemIndexPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var idxReader = new BinaryReader(idx);

                    using (var tdb = new FileStream(ItemTypesPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var tdbReader = new BinaryReader(tdb);

                        var types = ReadTypes(tdbReader);

                        itemCount = idxReader.ReadInt32();

                        Items = new Dictionary<Serial, Item>(itemCount);

                        for (var i = 0; i < itemCount; ++i)
                        {
                            var typeID = idxReader.ReadInt32();
                            var serial = idxReader.ReadInt32();
                            var pos = idxReader.ReadInt64();
                            var length = idxReader.ReadInt32();

                            var objs = types[typeID];

                            if (objs == null) continue;

                            Item item = null;
                            var ctor = objs.Item1;
                            var typeName = objs.Item2;

                            try
                            {
                                ctorArgs[0] = (Serial)serial;
                                item = (Item)ctor.Invoke(ctorArgs);
                            }
                            catch
                            {
                            }

                            if (item != null)
                            {
                                items.Add(new ItemEntry(item, typeID, typeName, pos, length));
                                AddItem(item);
                            }
                        }

                        tdbReader.Close();
                    }

                    idxReader.Close();
                }
            else
                Items = new Dictionary<Serial, Item>();

            if (File.Exists(GuildIndexPath))
                using (var idx = new FileStream(GuildIndexPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var idxReader = new BinaryReader(idx);

                    guildCount = idxReader.ReadInt32();

                    var createEventArgs = new CreateGuildEventArgs(-1);
                    for (var i = 0; i < guildCount; ++i)
                    {
                        idxReader.ReadInt32(); //no typeid for guilds
                        var id = idxReader.ReadInt32();
                        var pos = idxReader.ReadInt64();
                        var length = idxReader.ReadInt32();

                        createEventArgs.Id = id;
                        EventSink.InvokeCreateGuild(createEventArgs);
                        var guild = createEventArgs.Guild;
                        if (guild != null) guilds.Add(new GuildEntry(guild, pos, length));
                    }

                    idxReader.Close();
                }

            if (File.Exists(DataIndexPath) && File.Exists(DataTypesPath))
                using (var indexStream = new FileStream(DataIndexPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var indexReader = new BinaryReader(indexStream);

                    using (var typeStream =
                           new FileStream(DataTypesPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var typeReader = new BinaryReader(typeStream);

                        var types = ReadTypes(typeReader);

                        dataCount = indexReader.ReadInt32();
                        Data = new Dictionary<CustomSerial, SaveData>(dataCount);

                        for (var i = 0; i < dataCount; ++i)
                        {
                            var typeID = indexReader.ReadInt32();
                            var serial = indexReader.ReadInt32();
                            var pos = indexReader.ReadInt64();
                            var length = indexReader.ReadInt32();

                            var objects = types[typeID];

                            if (objects == null) continue;

                            SaveData saveData = null;
                            var ctor = objects.Item1;
                            var typeName = objects.Item2;

                            try
                            {
                                ctorArgs[0] = (CustomSerial)serial;
                                saveData = (SaveData)ctor.Invoke(ctorArgs);
                            }
                            catch
                            {
                                Utility.PushColor(ConsoleColor.Red);
                                Console.WriteLine("Error loading {0}, Serial: {1}", typeName, serial);
                                Utility.PopColor();
                            }

                            if (saveData != null)
                            {
                                data.Add(new DataEntry(saveData, typeID, typeName, pos, length));
                                AddData(saveData);
                            }
                        }

                        typeReader.Close();
                    }

                    indexReader.Close();
                }
            else
                Data = new Dictionary<CustomSerial, SaveData>();

            bool failedMobiles = false, failedItems = false, failedGuilds = false, failedData = false;
            Type failedType = null;
            var failedSerial = Serial.Zero;
            var failedCustomSerial = CustomSerial.Zero;
            Exception failed = null;
            var failedTypeID = 0;

            if (File.Exists(MobileDataPath))
                using (var bin = new FileStream(MobileDataPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var reader = new BinaryFileReader(new BinaryReader(bin));

                    for (var i = 0; i < mobiles.Count; ++i)
                    {
                        var entry = mobiles[i];
                        var m = entry.Mobile;

                        if (m != null)
                        {
                            reader.Seek(entry.Position, SeekOrigin.Begin);

                            try
                            {
                                LoadingType = entry.TypeName;
                                m.Deserialize(reader);

                                if (reader.Position != entry.Position + entry.Length)
                                    throw new Exception(string.Format("***** Bad serialize on {0} *****", m.GetType()));
                            }
                            catch (Exception e)
                            {
                                mobiles.RemoveAt(i);

                                failed = e;
                                failedMobiles = true;
                                failedType = m.GetType();
                                failedTypeID = entry.TypeID;
                                failedSerial = m.Serial;

                                break;
                            }
                        }
                    }

                    reader.Close();
                }

            if (!failedMobiles && File.Exists(ItemDataPath))
                using (var bin = new FileStream(ItemDataPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var reader = new BinaryFileReader(new BinaryReader(bin));

                    for (var i = 0; i < items.Count; ++i)
                    {
                        var entry = items[i];
                        var item = entry.Item;

                        if (item != null)
                        {
                            reader.Seek(entry.Position, SeekOrigin.Begin);

                            try
                            {
                                LoadingType = entry.TypeName;
                                item.Deserialize(reader);

                                if (reader.Position != entry.Position + entry.Length)
                                    throw new Exception(string.Format("***** Bad serialize on {0} *****",
                                        item.GetType()));
                            }
                            catch (Exception e)
                            {
                                items.RemoveAt(i);

                                failed = e;
                                failedItems = true;
                                failedType = item.GetType();
                                failedTypeID = entry.TypeID;
                                failedSerial = item.Serial;

                                break;
                            }
                        }
                    }

                    reader.Close();
                }

            LoadingType = null;

            if (!failedMobiles && !failedItems && File.Exists(GuildDataPath))
                using (var bin = new FileStream(GuildDataPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var reader = new BinaryFileReader(new BinaryReader(bin));

                    for (var i = 0; i < guilds.Count; ++i)
                    {
                        var entry = guilds[i];
                        var g = entry.Guild;

                        if (g != null)
                        {
                            reader.Seek(entry.Position, SeekOrigin.Begin);

                            try
                            {
                                g.Deserialize(reader);

                                if (reader.Position != entry.Position + entry.Length)
                                    throw new Exception(string.Format("***** Bad serialize on Guild {0} *****", g.Id));
                            }
                            catch (Exception e)
                            {
                                guilds.RemoveAt(i);

                                failed = e;
                                failedGuilds = true;
                                failedType = typeof(BaseGuild);
                                failedTypeID = g.Id;
                                failedSerial = g.Id;

                                break;
                            }
                        }
                    }

                    reader.Close();
                }

            if (!failedMobiles && !failedItems && !failedGuilds && File.Exists(DataBinaryPath))
                using (var stream = new FileStream(DataBinaryPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var reader = new BinaryFileReader(new BinaryReader(stream));

                    for (var i = 0; i < data.Count; ++i)
                    {
                        var entry = data[i];
                        var saveData = entry.Data;

                        if (saveData != null)
                        {
                            reader.Seek(entry.Position, SeekOrigin.Begin);

                            try
                            {
                                LoadingType = entry.TypeName;
                                saveData.Deserialize(reader);

                                if (reader.Position != entry.Position + entry.Length)
                                    throw new Exception(string.Format("***** Bad serialize on {0} *****",
                                        saveData.GetType()));
                            }
                            catch (Exception error)
                            {
                                data.RemoveAt(i);

                                failed = error;
                                failedData = true;
                                failedType = saveData.GetType();
                                failedTypeID = entry.TypeID;
                                failedCustomSerial = saveData.Serial;

                                break;
                            }
                        }
                    }

                    reader.Close();
                }

            if (failedItems || failedMobiles || failedGuilds || failedData)
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("An error was encountered while loading a saved object");
                Utility.PopColor();

                Console.WriteLine(" - Type: {0}", failedType);

                if (failedSerial != Serial.Zero)
                    Console.WriteLine(" - Serial: {0}", failedSerial);
                else
                    Console.WriteLine(" - Serial: {0}", failedCustomSerial);

                if (!Core.Service)
                {
                    Console.WriteLine("Delete the object? (y/n)");

                    if (Console.ReadKey(true).Key == ConsoleKey.Y)
                    {
                        if (failedType != typeof(BaseGuild))
                        {
                            Console.WriteLine("Delete all objects of that type? (y/n)");

                            if (Console.ReadKey(true).Key == ConsoleKey.Y)
                            {
                                if (failedMobiles)
                                    for (var i = 0; i < mobiles.Count;)
                                        if (mobiles[i].TypeID == failedTypeID)
                                            mobiles.RemoveAt(i);
                                        else
                                            ++i;
                                else if (failedItems)
                                    for (var i = 0; i < items.Count;)
                                        if (items[i].TypeID == failedTypeID)
                                            items.RemoveAt(i);
                                        else
                                            ++i;
                                else if (failedData)
                                    for (var i = 0; i < data.Count;)
                                        if (data[i].TypeID == failedTypeID)
                                            data.RemoveAt(i);
                                        else
                                            ++i;
                            }
                        }

                        SaveIndex(mobiles, MobileIndexPath);
                        SaveIndex(items, ItemIndexPath);
                        SaveIndex(guilds, GuildIndexPath);
                        SaveIndex(DataIndexPath, data);
                    }

                    Console.WriteLine(
                        "After pressing return an exception will be thrown and the server will terminate.");
                    Console.ReadLine();
                }
                else
                {
                    Utility.PushColor(ConsoleColor.Red);
                    Console.WriteLine("An exception will be thrown and the server will terminate.");
                    Utility.PopColor();
                }

                throw new Exception(
                    string.Format(
                        "Load failed (items={0}, mobiles={1}, guilds={2}, data={3}, type={4}, serial={5})",
                        failedItems,
                        failedMobiles,
                        failedGuilds,
                        failedData,
                        failedType,
                        failedSerial != Serial.Zero ? failedSerial.ToString() : failedCustomSerial.ToString()),
                    failed);
            }

            EventSink.InvokeWorldLoad();

            Loading = false;

            ProcessSafetyQueues();

            foreach (var item in Items.Values)
            {
                if (item.Parent == null) item.UpdateTotals();

                item.ClearProperties();
            }

            foreach (var m in Mobiles.Values)
            {
                m.UpdateRegion(); // Is this really needed?
                m.UpdateTotals();

                m.ClearProperties();
            }

            foreach (var saveData in Data.Values) saveData.Prep();

            watch.Stop();

            Utility.PushColor(ConsoleColor.Green);
            Console.WriteLine(
                "...done ({1} items, {2} mobiles, {3} customs) ({0:F2} seconds)",
                watch.Elapsed.TotalSeconds,
                Items.Count,
                Mobiles.Count,
                Data.Count);
            Utility.PopColor();
        }

        private static void ProcessSafetyQueues()
        {
            while (_addQueue.Count > 0)
            {
                var entity = _addQueue.Dequeue();

                var item = entity as Item;

                if (item != null)
                {
                    AddItem(item);
                }
                else
                {
                    var mob = entity as Mobile;

                    if (mob != null) AddMobile(mob);
                }
            }

            while (_deleteQueue.Count > 0)
            {
                var entity = _deleteQueue.Dequeue();

                var item = entity as Item;

                if (item != null)
                {
                    item.Delete();
                }
                else
                {
                    var mob = entity as Mobile;

                    if (mob != null) mob.Delete();
                }
            }

            while (_CustomsAddQueue.Count > 0)
            {
                var entity = _CustomsAddQueue.Dequeue();

                var data = entity as SaveData;

                if (data != null) AddData(data);
            }

            while (_CustomsDeleteQueue.Count > 0)
            {
                var entity = _CustomsDeleteQueue.Dequeue();

                var data = entity as SaveData;

                if (data != null) data.Delete();
            }
        }

        private static void AppendSafetyLog(string action, ICustomsEntity entity)
        {
            var message =
                string.Format(
                    "Warning: Attempted to {1} {2} during world save." +
                    "{0}This action could cause inconsistent state." +
                    "{0}It is strongly advised that the offending scripts be corrected.",
                    Environment.NewLine,
                    action,
                    entity);

            AppendSafetyLog(message);
        }

        private static void AppendSafetyLog(string action, IEntity entity)
        {
            var message =
                string.Format(
                    "Warning: Attempted to {1} {2} during world save." +
                    "{0}This action could cause inconsistent state." +
                    "{0}It is strongly advised that the offending scripts be corrected.",
                    Environment.NewLine,
                    action,
                    entity);

            AppendSafetyLog(message);
        }

        private static void AppendSafetyLog(string message)
        {
            Console.WriteLine(message);

            try
            {
                using (var op = new StreamWriter("world-save-errors.log", true))
                {
                    op.WriteLine("{0}\t{1}", DateTime.UtcNow, message);
                    op.WriteLine(new StackTrace(2).ToString());
                    op.WriteLine();
                }
            }
            catch
            {
            }
        }

        private static void SaveIndex<T>(List<T> list, string path) where T : IEntityEntry
        {
            if (!Directory.Exists("Saves/Mobiles/")) Directory.CreateDirectory("Saves/Mobiles/");

            if (!Directory.Exists("Saves/Items/")) Directory.CreateDirectory("Saves/Items/");

            if (!Directory.Exists("Saves/Guilds/")) Directory.CreateDirectory("Saves/Guilds/");

            using (var idx = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var idxWriter = new BinaryWriter(idx);

                idxWriter.Write(list.Count);

                for (var i = 0; i < list.Count; ++i)
                {
                    var e = list[i];

                    idxWriter.Write(e.TypeID);
                    idxWriter.Write(e.Serial);
                    idxWriter.Write(e.Position);
                    idxWriter.Write(e.Length);
                }

                idxWriter.Close();
            }
        }

        private static void SaveIndex<T>(string path, List<T> list) where T : ICustomsEntry
        {
            if (!Directory.Exists("Saves/Customs/")) Directory.CreateDirectory("Saves/Customs/");

            using (var indexStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var indexWriter = new BinaryWriter(indexStream);

                indexWriter.Write(list.Count);

                for (var i = 0; i < list.Count; ++i)
                {
                    var e = list[i];

                    indexWriter.Write(e.TypeID);
                    indexWriter.Write(e.Serial);
                    indexWriter.Write(e.Position);
                    indexWriter.Write(e.Length);
                }

                indexWriter.Close();
            }
        }

        public static void Save()
        {
            Save(true, false);
        }

        public static void Save(bool message, bool permitBackgroundWrite)
        {
            if (Saving) return;

            ++m_Saves;

            NetState.FlushAll();
            NetState.Pause();

            WaitForWriteCompletion(); //Blocks Save until current disk flush is done.

            Saving = true;

            m_DiskWriteHandle.Reset();

            if (message) Broadcast(0x35, true, AccessLevel.Counselor, "The world is saving, please wait.");

            var strategy = SaveStrategy.Acquire();
            Console.WriteLine("Core: Using {0} save strategy", strategy.Name.ToLowerInvariant());

            Console.WriteLine("World: Saving...");

            var watch = Stopwatch.StartNew();

            if (!Directory.Exists("Saves/Mobiles/")) Directory.CreateDirectory("Saves/Mobiles/");
            if (!Directory.Exists("Saves/Items/")) Directory.CreateDirectory("Saves/Items/");
            if (!Directory.Exists("Saves/Guilds/")) Directory.CreateDirectory("Saves/Guilds/");
            if (!Directory.Exists("Saves/Customs/")) Directory.CreateDirectory("Saves/Customs/");

            try
            {
                EventSink.InvokeBeforeWorldSave(new BeforeWorldSaveEventArgs());
            }
            catch (Exception e)
            {
                throw new Exception("FATAL: Exception in EventSink.BeforeWorldSave", e);
            }

            if (m_Metrics)
                using (var metrics = new SaveMetrics())
                {
                    strategy.Save(metrics, permitBackgroundWrite);
                }
            else
                strategy.Save(null, permitBackgroundWrite);

            try
            {
                EventSink.InvokeWorldSave(new WorldSaveEventArgs(message));
            }
            catch (Exception e)
            {
                throw new Exception("FATAL: Exception in EventSink.WorldSave", e);
            }

            watch.Stop();

            Saving = false;

            if (!permitBackgroundWrite) NotifyDiskWriteComplete();
            //Sets the DiskWriteHandle.  If we allow background writes, we leave this upto the individual save strategies.
            ProcessSafetyQueues();

            strategy.ProcessDecay();

            Console.WriteLine("Save finished in {0:F2} seconds.", watch.Elapsed.TotalSeconds);

            if (message)
                Broadcast(0x35, true, AccessLevel.Counselor, "World save done in {0:F1} seconds.",
                    watch.Elapsed.TotalSeconds);

            NetState.Resume();

            try
            {
                EventSink.InvokeAfterWorldSave(new AfterWorldSaveEventArgs());
            }
            catch (Exception e)
            {
                throw new Exception("FATAL: Exception in EventSink.AfterWorldSave", e);
            }
        }

        public static IEntity FindEntity(Serial serial)
        {
            if (serial.IsItem) return FindItem(serial);

            if (serial.IsMobile) return FindMobile(serial);

            return null;
        }

        public static ICustomsEntity FindCustomEntity(CustomSerial serial)
        {
            if (serial.IsValid) return GetData(serial);

            return null;
        }

        public static Mobile FindMobile(Serial serial)
        {
            Mobile mob;

            Mobiles.TryGetValue(serial, out mob);

            return mob;
        }

        public static void AddMobile(Mobile m)
        {
            if (Saving)
            {
                AppendSafetyLog("add", m);
                _addQueue.Enqueue(m);
            }
            else
            {
                Mobiles[m.Serial] = m;
            }
        }

        public static Item FindItem(Serial serial)
        {
            Item item;

            Items.TryGetValue(serial, out item);

            return item;
        }

        public static void AddItem(Item item)
        {
            if (Saving)
            {
                AppendSafetyLog("add", item);
                _addQueue.Enqueue(item);
            }
            else
            {
                Items[item.Serial] = item;
            }
        }

        public static void RemoveMobile(Mobile m)
        {
            Mobiles.Remove(m.Serial);
        }

        public static void RemoveItem(Item item)
        {
            Items.Remove(item.Serial);
        }

        public static void AddData(SaveData data)
        {
            if (Saving)
            {
                AppendSafetyLog("add", data);
                _CustomsAddQueue.Enqueue(data);
            }
            else
            {
                Data[data.Serial] = data;
            }
        }

        public static void RemoveData(SaveData data)
        {
            Data.Remove(data.Serial);
        }

        public static SaveData GetData(CustomSerial serial)
        {
            SaveData data;

            Data.TryGetValue(serial, out data);

            return data;
        }

        public static SaveData GetData(string name)
        {
            foreach (var data in Data.Values)
                if (data.Name == name)
                    return data;

            return null;
        }

        public static SaveData GetData(Type type)
        {
            foreach (var data in Data.Values)
                if (data.GetType() == type)
                    return data;

            return null;
        }

        public static List<SaveData> GetDataList(Type type)
        {
            var results = new List<SaveData>();

            foreach (var data in Data.Values)
                if (data.GetType() == type)
                    results.Add(data);

            return results;
        }

        public static List<SaveData> SearchData(string find)
        {
            var keywords = find.ToLower().Split(' ');
            var results = new List<SaveData>();

            foreach (var data in Data.Values)
            {
                var match = true;
                var name = data.Name.ToLower();

                for (var i = 0; i < keywords.Length; i++)
                    if (name.IndexOf(keywords[i]) == -1)
                        match = false;

                if (match) results.Add(data);
            }

            return results;
        }

        public static SaveData GetCore(Type type)
        {
            foreach (var data in Data.Values)
                if (data.GetType() == type)
                    return data;

            return null;
        }

        public static List<SaveData> GetCores(Type type)
        {
            var results = new List<SaveData>();

            foreach (var data in Data.Values)
                if (data.GetType() == type)
                    results.Add(data);

            return results;
        }

        public static BaseModule GetModule(Mobile mobile)
        {
            foreach (var data in Data.Values)
                if (data is BaseModule)
                {
                    var module = data as BaseModule;

                    if (module.LinkedMobile == mobile) return module;
                }

            return null;
        }

        public static List<BaseModule> GetModules(Mobile mobile)
        {
            var results = new List<BaseModule>();

            foreach (var data in Data.Values)
                if (data is BaseModule)
                {
                    var module = data as BaseModule;

                    if (module.LinkedMobile == mobile) results.Add(module);
                }

            return results;
        }

        public static BaseModule GetModule(Item item)
        {
            foreach (var data in Data.Values)
                if (data is BaseModule)
                {
                    var module = data as BaseModule;

                    if (module.LinkedItem == item) return module;
                }

            return null;
        }

        public static List<BaseModule> GetModules(Item item)
        {
            var results = new List<BaseModule>();

            foreach (var data in Data.Values)
                if (data is BaseModule)
                {
                    var module = data as BaseModule;

                    if (module.LinkedItem == item) results.Add(module);
                }

            return results;
        }

        public static BaseModule GetModule(Mobile mobile, string name)
        {
            foreach (var data in Data.Values)
                if (data is BaseModule)
                {
                    var module = data as BaseModule;

                    if (module.Name == name && module.LinkedMobile == mobile) return module;
                }

            return null;
        }

        public static List<BaseModule> GetModules(Mobile mobile, string name)
        {
            var results = new List<BaseModule>();

            foreach (var data in Data.Values)
                if (data is BaseModule)
                {
                    var module = data as BaseModule;

                    if (module.Name == name && module.LinkedMobile == mobile) results.Add(module);
                }

            return results;
        }

        public static BaseModule GetModule(Mobile mobile, Type type)
        {
            foreach (var data in Data.Values)
                if (data is BaseModule)
                {
                    var module = data as BaseModule;

                    if (module.GetType() == type && module.LinkedMobile == mobile) return module;
                }

            return null;
        }

        public static BaseModule GetModule(Item item, string name)
        {
            foreach (var data in Data.Values)
                if (data is BaseModule)
                {
                    var module = data as BaseModule;

                    if (module.Name == name && module.LinkedItem == item) return module;
                }

            return null;
        }

        public static List<BaseModule> GetModules(Item item, string name)
        {
            var results = new List<BaseModule>();

            foreach (var data in Data.Values)
                if (data is BaseModule)
                {
                    var module = data as BaseModule;

                    if (module.Name == name && module.LinkedItem == item) results.Add(module);
                }

            return results;
        }

        public static BaseModule GetModule(Item item, Type type)
        {
            foreach (var data in Data.Values)
                if (data is BaseModule)
                {
                    var module = data as BaseModule;

                    if (module.GetType() == type && module.LinkedItem == item) return module;
                }

            return null;
        }

        public static List<BaseModule> SearchModules(Mobile mobile, string text)
        {
            var keywords = text.ToLower().Split(' ');
            var results = new List<BaseModule>();

            foreach (var data in Data.Values)
                if (data is BaseModule)
                {
                    var module = data as BaseModule;

                    var match = true;
                    var name = module.Name.ToLower();

                    for (var i = 0; i < keywords.Length; i++)
                        if (name.IndexOf(keywords[i]) == -1)
                            match = false;

                    if (match && module.LinkedMobile == mobile) results.Add(module);
                }

            return results;
        }

        public static List<BaseModule> SearchModules(Item item, string text)
        {
            var keywords = text.ToLower().Split(' ');
            var results = new List<BaseModule>();

            foreach (var data in Data.Values)
                if (data is BaseModule)
                {
                    var module = data as BaseModule;

                    var match = true;
                    var name = module.Name.ToLower();

                    for (var i = 0; i < keywords.Length; i++)
                        if (name.IndexOf(keywords[i]) == -1)
                            match = false;

                    if (match && module.LinkedItem == item) results.Add(module);
                }

            return results;
        }

        private interface IEntityEntry
        {
            Serial Serial { get; }
            int TypeID { get; }
            long Position { get; }
            int Length { get; }
        }

        private sealed class GuildEntry : IEntityEntry
        {
            public BaseGuild Guild { get; }

            public GuildEntry(BaseGuild g, long pos, int length)
            {
                Guild = g;
                Position = pos;
                Length = length;
            }

            public Serial Serial => Guild == null ? 0 : Guild.Id;

            public int TypeID => 0;

            public long Position { get; }

            public int Length { get; }
        }

        private sealed class ItemEntry : IEntityEntry
        {
            public Item Item { get; }

            public string TypeName { get; }

            public ItemEntry(Item item, int typeID, string typeName, long pos, int length)
            {
                Item = item;
                TypeID = typeID;
                TypeName = typeName;
                Position = pos;
                Length = length;
            }

            public Serial Serial => Item == null ? Serial.MinusOne : Item.Serial;

            public int TypeID { get; }

            public long Position { get; }

            public int Length { get; }
        }

        private sealed class MobileEntry : IEntityEntry
        {
            public Mobile Mobile { get; }

            public string TypeName { get; }

            public MobileEntry(Mobile mobile, int typeID, string typeName, long pos, int length)
            {
                Mobile = mobile;
                TypeID = typeID;
                TypeName = typeName;
                Position = pos;
                Length = length;
            }

            public Serial Serial => Mobile == null ? Serial.MinusOne : Mobile.Serial;

            public int TypeID { get; }

            public long Position { get; }

            public int Length { get; }
        }

        public sealed class DataEntry : ICustomsEntry
        {
            public SaveData Data { get; }

            public string TypeName { get; }

            public DataEntry(SaveData data, int typeID, string typeName, long pos, int length)
            {
                Data = data;
                TypeID = typeID;
                TypeName = typeName;
                Position = pos;
                Length = length;
            }

            public CustomSerial Serial => Data == null ? CustomSerial.MinusOne : Data.Serial;
            public int TypeID { get; }

            public long Position { get; }

            public int Length { get; }
        }
    }
}
