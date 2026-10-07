using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using CustomsFramework;
using Server.Guilds;

namespace Server
{
    public sealed class ParallelSaveStrategy : SaveStrategy
    {
        private readonly Queue<Item> _decayQueue;
        private readonly int processorCount;
        private Consumer[] consumers;
        private SequentialFileWriter customData, customIndex;
        private int cycle;
        private bool finished;
        private SequentialFileWriter guildData, guildIndex;
        private SequentialFileWriter itemData, itemIndex;
        private SaveMetrics metrics;
        private SequentialFileWriter mobileData, mobileIndex;

        public override string Name => "Parallel";

        public ParallelSaveStrategy(int processorCount)
        {
            this.processorCount = processorCount;

            _decayQueue = new Queue<Item>();
        }

        public override void Save(SaveMetrics metrics, bool permitBackgroundWrite)
        {
            this.metrics = metrics;

            OpenFiles();

            consumers = new Consumer[GetThreadCount()];

            for (var i = 0; i < consumers.Length; ++i) consumers[i] = new Consumer(this, 256);

            IEnumerable<ISerializable> collection = new Producer();

            foreach (var value in collection)
                while (!Enqueue(value))
                    if (!Commit())
                        Thread.Sleep(0);

            finished = true;

            SaveTypeDatabases();

            WaitHandle.WaitAll(
                Array.ConvertAll<Consumer, WaitHandle>(
                    consumers,
                    delegate(Consumer input) { return input.completionEvent; }));

            Commit();

            CloseFiles();
        }

        public override void ProcessDecay()
        {
            while (_decayQueue.Count > 0)
            {
                var item = _decayQueue.Dequeue();

                if (item.OnDecay()) item.Delete();
            }
        }

        private int GetThreadCount()
        {
            return processorCount - 1;
        }

        private void SaveTypeDatabases()
        {
            SaveTypeDatabase(World.ItemTypesPath, World.m_ItemTypes);
            SaveTypeDatabase(World.MobileTypesPath, World.m_MobileTypes);
            SaveTypeDatabase(World.DataTypesPath, World._DataTypes);
        }

        private void SaveTypeDatabase(string path, List<Type> types)
        {
            var bfw = new BinaryFileWriter(path, false);

            bfw.Write(types.Count);

            foreach (var type in types) bfw.Write(type.FullName);

            bfw.Flush();

            bfw.Close();
        }

        private void OpenFiles()
        {
            itemData = new SequentialFileWriter(World.ItemDataPath, metrics);
            itemIndex = new SequentialFileWriter(World.ItemIndexPath, metrics);

            mobileData = new SequentialFileWriter(World.MobileDataPath, metrics);
            mobileIndex = new SequentialFileWriter(World.MobileIndexPath, metrics);

            guildData = new SequentialFileWriter(World.GuildDataPath, metrics);
            guildIndex = new SequentialFileWriter(World.GuildIndexPath, metrics);

            customData = new SequentialFileWriter(World.DataBinaryPath, metrics);
            customIndex = new SequentialFileWriter(World.DataIndexPath, metrics);

            WriteCount(itemIndex, World.Items.Count);
            WriteCount(mobileIndex, World.Mobiles.Count);
            WriteCount(guildIndex, BaseGuild.List.Count);
            WriteCount(customIndex, World.Data.Count);
        }

        private void WriteCount(SequentialFileWriter indexFile, int count)
        {
            var buffer = new byte[4];

            buffer[0] = (byte)count;
            buffer[1] = (byte)(count >> 8);
            buffer[2] = (byte)(count >> 16);
            buffer[3] = (byte)(count >> 24);

            indexFile.Write(buffer, 0, buffer.Length);
        }

        private void CloseFiles()
        {
            itemData.Close();
            itemIndex.Close();

            mobileData.Close();
            mobileIndex.Close();

            guildData.Close();
            guildIndex.Close();

            customData.Close();
            customIndex.Close();

            World.NotifyDiskWriteComplete();
        }

        private void OnSerialized(ConsumableEntry entry)
        {
            var value = entry.value;
            var writer = entry.writer;

            var item = value as Item;

            if (item != null)
            {
                Save(item, writer);
            }
            else
            {
                var mob = value as Mobile;

                if (mob != null)
                {
                    Save(mob, writer);
                }
                else
                {
                    var guild = value as BaseGuild;

                    if (guild != null)
                    {
                        Save(guild, writer);
                    }
                    else
                    {
                        var data = value as SaveData;

                        if (data != null)
                            Save(data, writer);
                    }
                }
            }
        }

        private void Save(Item item, BinaryMemoryWriter writer)
        {
            var length = writer.CommitTo(itemData, itemIndex, item.m_TypeRef, item.Serial);

            if (metrics != null) metrics.OnItemSaved(length);

            if (item.Decays && item.Parent == null && item.Map != Map.Internal &&
                DateTime.UtcNow > item.LastMoved + item.DecayTime) _decayQueue.Enqueue(item);
        }

        private void Save(Mobile mob, BinaryMemoryWriter writer)
        {
            var length = writer.CommitTo(mobileData, mobileIndex, mob.m_TypeRef, mob.Serial);

            if (metrics != null) metrics.OnMobileSaved(length);
        }

        private void Save(BaseGuild guild, BinaryMemoryWriter writer)
        {
            var length = writer.CommitTo(guildData, guildIndex, 0, guild.Id);

            if (metrics != null) metrics.OnGuildSaved(length);
        }

        private void Save(SaveData data, BinaryMemoryWriter writer)
        {
            var length = writer.CommitTo(customData, customIndex, data._TypeID, data.Serial);

            if (metrics != null)
                metrics.OnDataSaved(length);
        }

        private bool Enqueue(ISerializable value)
        {
            for (var i = 0; i < consumers.Length; ++i)
            {
                var consumer = consumers[cycle++ % consumers.Length];

                if (consumer.tail - consumer.head < consumer.buffer.Length)
                {
                    consumer.buffer[consumer.tail % consumer.buffer.Length].value = value;
                    consumer.tail++;

                    return true;
                }
            }

            return false;
        }

        private bool Commit()
        {
            var committed = false;

            for (var i = 0; i < consumers.Length; ++i)
            {
                var consumer = consumers[i];

                while (consumer.head < consumer.done)
                {
                    OnSerialized(consumer.buffer[consumer.head % consumer.buffer.Length]);
                    consumer.head++;

                    committed = true;
                }
            }

            return committed;
        }

        private struct ConsumableEntry
        {
            public ISerializable value;
            public BinaryMemoryWriter writer;
        }

        private sealed class Producer : IEnumerable<ISerializable>
        {
            private readonly IEnumerable<SaveData> data;
            private readonly IEnumerable<BaseGuild> guilds;
            private readonly IEnumerable<Item> items;
            private readonly IEnumerable<Mobile> mobiles;

            public Producer()
            {
                items = World.Items.Values;
                mobiles = World.Mobiles.Values;
                guilds = BaseGuild.List.Values;
                data = World.Data.Values;
            }

            public IEnumerator<ISerializable> GetEnumerator()
            {
                foreach (var item in items)
                    yield return item;

                foreach (var mob in mobiles)
                    yield return mob;

                foreach (var guild in guilds)
                    yield return guild;

                foreach (var data in this.data)
                    yield return data;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        private sealed class Consumer
        {
            public readonly ConsumableEntry[] buffer;
            public readonly ManualResetEvent completionEvent;
            private readonly ParallelSaveStrategy owner;
            private readonly Thread thread;
            public int head, done, tail;

            public Consumer(ParallelSaveStrategy owner, int bufferSize)
            {
                this.owner = owner;

                buffer = new ConsumableEntry[bufferSize];

                for (var i = 0; i < buffer.Length; ++i) buffer[i].writer = new BinaryMemoryWriter();

                completionEvent = new ManualResetEvent(false);

                thread = new Thread(Processor);

                thread.Name = "Parallel Serialization Thread";

                thread.Start();
            }

            private void Processor()
            {
                try
                {
                    while (!owner.finished)
                    {
                        Process();
                        Thread.Sleep(0);
                    }

                    Process();

                    completionEvent.Set();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            private void Process()
            {
                ConsumableEntry entry;

                while (done < tail)
                {
                    entry = buffer[done % buffer.Length];

                    entry.value.Serialize(entry.writer);

                    ++done;
                }
            }
        }
    }
}
