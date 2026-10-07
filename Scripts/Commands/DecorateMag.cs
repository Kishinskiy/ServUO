using System;
using System.Collections;
using System.IO;
using Server.Engines.Quests.Haven;
using Server.Engines.Quests.Necro;
using Server.Items;
using Server.Mobiles;

namespace Server.Commands
{
    public class DecorateMag
    {
        private static Mobile m_Mobile;
        private static int m_Count;

        public static void Initialize()
        {
            CommandSystem.Register("DecorateMag", AccessLevel.Administrator, DecorateMag_OnCommand);
        }

        public static void Generate(string folder, params Map[] maps)
        {
            if (!Directory.Exists(folder))
                return;

            var files = Directory.GetFiles(folder, "*.cfg");

            for (var i = 0; i < files.Length; ++i)
            {
                var list = DecorationListMag.ReadAll(files[i]);

                for (var j = 0; j < list.Count; ++j)
                    m_Count += ((DecorationListMag)list[j]).Generate(maps);
            }
        }

        [Usage("DecorateMag")]
        [Description("Generates world decoration.")]
        private static void DecorateMag_OnCommand(CommandEventArgs e)
        {
            m_Mobile = e.Mobile;
            m_Count = 0;

            m_Mobile.SendMessage("Generating world decoration, please wait.");

            Generate("Data/Decoration/RuinedMaginciaTram", Map.Trammel);
            Generate("Data/Decoration/RuinedMaginciaFel", Map.Felucca);

            m_Mobile.SendMessage("World generating complete. {0} items were generated.", m_Count);
        }
    }

    public class DecorationListMag
    {
        private static readonly Type typeofStatic = typeof(Static);
        private static readonly Type typeofLocalizedStatic = typeof(LocalizedStatic);
        private static readonly Type typeofBaseDoor = typeof(BaseDoor);
        private static readonly Type typeofAnkhWest = typeof(AnkhWest);
        private static readonly Type typeofAnkhNorth = typeof(AnkhNorth);
        private static readonly Type typeofBeverage = typeof(BaseBeverage);
        private static readonly Type typeofLocalizedSign = typeof(LocalizedSign);
        private static readonly Type typeofMarkContainer = typeof(MarkContainer);
        private static readonly Type typeofWarningItem = typeof(WarningItem);
        private static readonly Type typeofHintItem = typeof(HintItem);
        private static readonly Type typeofCannon = typeof(Cannon);
        private static readonly Type typeofSerpentPillar = typeof(SerpentPillar);
        private static readonly Queue m_DeleteQueue = new Queue();
        private static readonly string[] m_EmptyParams = new string[0];
        private ArrayList m_Entries;
        private int m_ItemID;
        private string[] m_Params;
        private Type m_Type;

        public static ArrayList ReadAll(string path)
        {
            using (var ip = new StreamReader(path))
            {
                var list = new ArrayList();

                for (var v = Read(ip); v != null; v = Read(ip))
                    list.Add(v);

                return list;
            }
        }

        public static DecorationListMag Read(StreamReader ip)
        {
            string line;

            while ((line = ip.ReadLine()) != null)
            {
                line = line.Trim();

                if (line.Length > 0 && !line.StartsWith("#"))
                    break;
            }

            if (string.IsNullOrEmpty(line))
                return null;

            var list = new DecorationListMag();

            var indexOf = line.IndexOf(' ');

            list.m_Type = ScriptCompiler.FindTypeByName(line.Substring(0, indexOf++), true);

            if (list.m_Type == null)
                throw new ArgumentException(string.Format("Type not found for header: '{0}'", line));

            line = line.Substring(indexOf);
            indexOf = line.IndexOf('(');
            if (indexOf >= 0)
            {
                list.m_ItemID = Utility.ToInt32(line.Substring(0, indexOf - 1));

                var parms = line.Substring(++indexOf);

                if (line.EndsWith(")"))
                    parms = parms.Substring(0, parms.Length - 1);

                list.m_Params = parms.Split(';');

                for (var i = 0; i < list.m_Params.Length; ++i)
                    list.m_Params[i] = list.m_Params[i].Trim();
            }
            else
            {
                list.m_ItemID = Utility.ToInt32(line);
                list.m_Params = m_EmptyParams;
            }

            list.m_Entries = new ArrayList();

            while ((line = ip.ReadLine()) != null)
            {
                line = line.Trim();

                if (line.Length == 0)
                    break;

                if (line.StartsWith("#"))
                    continue;

                list.m_Entries.Add(new DecorationEntryMag(line));
            }

            return list;
        }

        public Item Construct()
        {
            if (m_Type == null)
                return null;

            Item item;

            try
            {
                if (m_Type == typeofStatic)
                {
                    item = new Static(m_ItemID);
                }
                else if (m_Type == typeofLocalizedStatic)
                {
                    var labelNumber = 0;

                    for (var i = 0; i < m_Params.Length; ++i)
                        if (m_Params[i].StartsWith("LabelNumber"))
                        {
                            var indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                            {
                                labelNumber = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                                break;
                            }
                        }

                    item = new LocalizedStatic(m_ItemID, labelNumber);
                }
                else if (m_Type == typeofLocalizedSign)
                {
                    var labelNumber = 0;

                    for (var i = 0; i < m_Params.Length; ++i)
                        if (m_Params[i].StartsWith("LabelNumber"))
                        {
                            var indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                            {
                                labelNumber = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                                break;
                            }
                        }

                    item = new LocalizedSign(m_ItemID, labelNumber);
                }
                else if (m_Type == typeofAnkhWest || m_Type == typeofAnkhNorth)
                {
                    var bloodied = false;

                    for (var i = 0; !bloodied && i < m_Params.Length; ++i)
                        bloodied = m_Params[i] == "Bloodied";

                    if (m_Type == typeofAnkhWest)
                        item = new AnkhWest(bloodied);
                    else
                        item = new AnkhNorth(bloodied);
                }
                else if (m_Type == typeofMarkContainer)
                {
                    var bone = false;
                    var locked = false;
                    var map = Map.Malas;

                    for (var i = 0; i < m_Params.Length; ++i)
                        if (m_Params[i] == "Bone")
                        {
                            bone = true;
                        }
                        else if (m_Params[i] == "Locked")
                        {
                            locked = true;
                        }
                        else if (m_Params[i].StartsWith("TargetMap"))
                        {
                            var indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                map = Map.Parse(m_Params[i].Substring(++indexOf));
                        }

                    var mc = new MarkContainer(bone, locked);

                    mc.TargetMap = map;
                    mc.Description = "strange location";

                    item = mc;
                }
                else if (m_Type == typeofHintItem)
                {
                    var range = 0;
                    var messageNumber = 0;
                    string messageString = null;
                    var hintNumber = 0;
                    string hintString = null;
                    var resetDelay = TimeSpan.Zero;

                    for (var i = 0; i < m_Params.Length; ++i)
                        if (m_Params[i].StartsWith("Range"))
                        {
                            var indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                range = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                        }
                        else if (m_Params[i].StartsWith("WarningString"))
                        {
                            var indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                messageString = m_Params[i].Substring(++indexOf);
                        }
                        else if (m_Params[i].StartsWith("WarningNumber"))
                        {
                            var indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                messageNumber = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                        }
                        else if (m_Params[i].StartsWith("HintString"))
                        {
                            var indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                hintString = m_Params[i].Substring(++indexOf);
                        }
                        else if (m_Params[i].StartsWith("HintNumber"))
                        {
                            var indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                hintNumber = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                        }
                        else if (m_Params[i].StartsWith("ResetDelay"))
                        {
                            var indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                resetDelay = TimeSpan.Parse(m_Params[i].Substring(++indexOf));
                        }

                    var hi = new HintItem(m_ItemID, range, messageNumber, hintNumber);

                    hi.WarningString = messageString;
                    hi.HintString = hintString;
                    hi.ResetDelay = resetDelay;

                    item = hi;
                }
                else if (m_Type == typeofWarningItem)
                {
                    var range = 0;
                    var messageNumber = 0;
                    string messageString = null;
                    var resetDelay = TimeSpan.Zero;

                    for (var i = 0; i < m_Params.Length; ++i)
                        if (m_Params[i].StartsWith("Range"))
                        {
                            var indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                range = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                        }
                        else if (m_Params[i].StartsWith("WarningString"))
                        {
                            var indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                messageString = m_Params[i].Substring(++indexOf);
                        }
                        else if (m_Params[i].StartsWith("WarningNumber"))
                        {
                            var indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                messageNumber = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                        }
                        else if (m_Params[i].StartsWith("ResetDelay"))
                        {
                            var indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                resetDelay = TimeSpan.Parse(m_Params[i].Substring(++indexOf));
                        }

                    var wi = new WarningItem(m_ItemID, range, messageNumber);

                    wi.WarningString = messageString;
                    wi.ResetDelay = resetDelay;

                    item = wi;
                }
                else if (m_Type == typeofCannon)
                {
                    var direction = CannonDirection.North;

                    for (var i = 0; i < m_Params.Length; ++i)
                        if (m_Params[i].StartsWith("CannonDirection"))
                        {
                            var indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                direction = (CannonDirection)Enum.Parse(typeof(CannonDirection),
                                    m_Params[i].Substring(++indexOf), true);
                        }

                    item = new Cannon(direction);
                }
                else if (m_Type == typeofSerpentPillar)
                {
                    string word = null;
                    var destination = new Rectangle2D();

                    for (var i = 0; i < m_Params.Length; ++i)
                        if (m_Params[i].StartsWith("Word"))
                        {
                            var indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                word = m_Params[i].Substring(++indexOf);
                        }
                        else if (m_Params[i].StartsWith("DestStart"))
                        {
                            var indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                destination.Start = Point2D.Parse(m_Params[i].Substring(++indexOf));
                        }
                        else if (m_Params[i].StartsWith("DestEnd"))
                        {
                            var indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                destination.End = Point2D.Parse(m_Params[i].Substring(++indexOf));
                        }

                    item = new SerpentPillar(word, destination);
                }
                else if (m_Type.IsSubclassOf(typeofBeverage))
                {
                    var content = BeverageType.Liquor;
                    var fill = false;

                    for (var i = 0; !fill && i < m_Params.Length; ++i)
                        if (m_Params[i].StartsWith("Content"))
                        {
                            var indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                            {
                                content = (BeverageType)Enum.Parse(typeof(BeverageType),
                                    m_Params[i].Substring(++indexOf), true);
                                fill = true;
                            }
                        }

                    if (fill)
                        item = (Item)Activator.CreateInstance(m_Type, content);
                    else
                        item = (Item)Activator.CreateInstance(m_Type);
                }
                else if (m_Type.IsSubclassOf(typeofBaseDoor))
                {
                    var facing = DoorFacing.WestCW;

                    for (var i = 0; i < m_Params.Length; ++i)
                        if (m_Params[i].StartsWith("Facing"))
                        {
                            var indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                            {
                                facing = (DoorFacing)Enum.Parse(typeof(DoorFacing), m_Params[i].Substring(++indexOf),
                                    true);
                                break;
                            }
                        }

                    item = (Item)Activator.CreateInstance(m_Type, facing);
                }
                else
                {
                    item = (Item)Activator.CreateInstance(m_Type);
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("Bad type: {0}", m_Type), e);
            }

            if (item is BaseAddon)
            {
                if (item is MaabusCoffin)
                {
                    var coffin = (MaabusCoffin)item;

                    for (var i = 0; i < m_Params.Length; ++i)
                        if (m_Params[i].StartsWith("SpawnLocation"))
                        {
                            var indexOf = m_Params[i].IndexOf('=');

                            if (indexOf >= 0)
                                coffin.SpawnLocation = Point3D.Parse(m_Params[i].Substring(++indexOf));
                        }
                }
                else if (m_ItemID > 0)
                {
                    var comps = ((BaseAddon)item).Components;

                    for (var i = 0; i < comps.Count; ++i)
                    {
                        var comp = comps[i];

                        if (comp.Offset == Point3D.Zero)
                            comp.ItemID = m_ItemID;
                    }
                }
            }
            else if (item is BaseLight)
            {
                bool unlit = false, unprotected = false;

                for (var i = 0; i < m_Params.Length; ++i)
                {
                    if (!unlit && m_Params[i] == "Unlit")
                        unlit = true;
                    else if (!unprotected && m_Params[i] == "Unprotected")
                        unprotected = true;

                    if (unlit && unprotected)
                        break;
                }

                if (!unlit)
                    ((BaseLight)item).Ignite();
                if (!unprotected)
                    ((BaseLight)item).Protected = true;

                if (m_ItemID > 0)
                    item.ItemID = m_ItemID;
            }
            else if (item is Spawner)
            {
                var sp = (Spawner)item;

                sp.NextSpawn = TimeSpan.Zero;

                for (var i = 0; i < m_Params.Length; ++i)
                    if (m_Params[i].StartsWith("Spawn"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            sp.SpawnObjects.Add(new SpawnObject(m_Params[i].Substring(++indexOf)));
                    }
                    else if (m_Params[i].StartsWith("MinDelay"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            sp.MinDelay = TimeSpan.Parse(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("MaxDelay"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            sp.MaxDelay = TimeSpan.Parse(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("NextSpawn"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            sp.NextSpawn = TimeSpan.Parse(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("Count"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            sp.MaxCount = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("Team"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            sp.Team = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("HomeRange"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            sp.HomeRange = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("Running"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            sp.Running = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("Group"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            sp.Group = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
                    }
            }
            else if (item is RecallRune)
            {
                var rune = (RecallRune)item;

                for (var i = 0; i < m_Params.Length; ++i)
                    if (m_Params[i].StartsWith("Description"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            rune.Description = m_Params[i].Substring(++indexOf);
                    }
                    else if (m_Params[i].StartsWith("Marked"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            rune.Marked = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("TargetMap"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            rune.TargetMap = Map.Parse(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("Target"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            rune.Target = Point3D.Parse(m_Params[i].Substring(++indexOf));
                    }
            }
            else if (item is SkillTeleporter)
            {
                var tp = (SkillTeleporter)item;

                for (var i = 0; i < m_Params.Length; ++i)
                    if (m_Params[i].StartsWith("Skill"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.Skill = (SkillName)Enum.Parse(typeof(SkillName), m_Params[i].Substring(++indexOf), true);
                    }
                    else if (m_Params[i].StartsWith("RequiredFixedPoint"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.Required = Utility.ToInt32(m_Params[i].Substring(++indexOf)) * 0.1;
                    }
                    else if (m_Params[i].StartsWith("Required"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.Required = Utility.ToDouble(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("MessageString"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.MessageString = m_Params[i].Substring(++indexOf);
                    }
                    else if (m_Params[i].StartsWith("MessageNumber"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.MessageNumber = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("PointDest"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.PointDest = Point3D.Parse(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("MapDest"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.MapDest = Map.Parse(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("Creatures"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.Creatures = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("SourceEffect"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.SourceEffect = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("DestEffect"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.DestEffect = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("SoundID"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.SoundID = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("Delay"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.Delay = TimeSpan.Parse(m_Params[i].Substring(++indexOf));
                    }

                if (m_ItemID > 0)
                    item.ItemID = m_ItemID;
            }
            else if (item is KeywordTeleporter)
            {
                var tp = (KeywordTeleporter)item;

                for (var i = 0; i < m_Params.Length; ++i)
                    if (m_Params[i].StartsWith("Substring"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.Substring = m_Params[i].Substring(++indexOf);
                    }
                    else if (m_Params[i].StartsWith("Keyword"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.Keyword = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("Range"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.Range = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("PointDest"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.PointDest = Point3D.Parse(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("MapDest"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.MapDest = Map.Parse(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("Creatures"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.Creatures = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("SourceEffect"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.SourceEffect = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("DestEffect"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.DestEffect = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("SoundID"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.SoundID = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("Delay"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.Delay = TimeSpan.Parse(m_Params[i].Substring(++indexOf));
                    }

                if (m_ItemID > 0)
                    item.ItemID = m_ItemID;
            }
            else if (item is Teleporter)
            {
                var tp = (Teleporter)item;

                for (var i = 0; i < m_Params.Length; ++i)
                    if (m_Params[i].StartsWith("PointDest"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.PointDest = Point3D.Parse(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("MapDest"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.MapDest = Map.Parse(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("Creatures"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.Creatures = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("SourceEffect"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.SourceEffect = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("DestEffect"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.DestEffect = Utility.ToBoolean(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("SoundID"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.SoundID = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                    }
                    else if (m_Params[i].StartsWith("Delay"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            tp.Delay = TimeSpan.Parse(m_Params[i].Substring(++indexOf));
                    }

                if (m_ItemID > 0)
                    item.ItemID = m_ItemID;
            }
            else if (item is FillableContainer)
            {
                var cont = (FillableContainer)item;

                for (var i = 0; i < m_Params.Length; ++i)
                    if (m_Params[i].StartsWith("ContentType"))
                    {
                        var indexOf = m_Params[i].IndexOf('=');

                        if (indexOf >= 0)
                            cont.ContentType = (FillableContentType)Enum.Parse(typeof(FillableContentType),
                                m_Params[i].Substring(++indexOf), true);
                    }

                if (m_ItemID > 0)
                    item.ItemID = m_ItemID;
            }
            else if (m_ItemID > 0)
            {
                item.ItemID = m_ItemID;
            }

            item.Movable = false;

            for (var i = 0; i < m_Params.Length; ++i)
                if (m_Params[i].StartsWith("Light"))
                {
                    var indexOf = m_Params[i].IndexOf('=');

                    if (indexOf >= 0)
                        item.Light = (LightType)Enum.Parse(typeof(LightType), m_Params[i].Substring(++indexOf), true);
                }
                else if (m_Params[i].StartsWith("Hue"))
                {
                    var indexOf = m_Params[i].IndexOf('=');

                    if (indexOf >= 0)
                    {
                        var hue = Utility.ToInt32(m_Params[i].Substring(++indexOf));

                        if (item is DyeTub)
                            ((DyeTub)item).DyedHue = hue;
                        else
                            item.Hue = hue;
                    }
                }
                else if (m_Params[i].StartsWith("Name"))
                {
                    var indexOf = m_Params[i].IndexOf('=');

                    if (indexOf >= 0)
                        item.Name = m_Params[i].Substring(++indexOf);
                }
                else if (m_Params[i].StartsWith("Amount"))
                {
                    var indexOf = m_Params[i].IndexOf('=');

                    if (indexOf >= 0)
                    {
                        // Must supress stackable warnings
                        var wasStackable = item.Stackable;

                        item.Stackable = true;
                        item.Amount = Utility.ToInt32(m_Params[i].Substring(++indexOf));
                        item.Stackable = wasStackable;
                    }
                }

            return item;
        }

        public int Generate(Map[] maps)
        {
            var count = 0;

            Item item = null;

            for (var i = 0; i < m_Entries.Count; ++i)
            {
                var entry = (DecorationEntryMag)m_Entries[i];
                var loc = entry.Location;
                var extra = entry.Extra;

                for (var j = 0; j < maps.Length; ++j)
                {
                    if (item == null)
                        item = Construct();

                    if (item == null)
                        continue;

                    if (FindItem(loc.X, loc.Y, loc.Z, maps[j], item))
                    {
                    }
                    else
                    {
                        item.MoveToWorld(loc, maps[j]);
                        ++count;

                        if (item is BaseDoor)
                        {
                            IPooledEnumerable eable = maps[j].GetItemsInRange(loc, 1);

                            var itemType = item.GetType();

                            foreach (Item link in eable)
                                if (link != item && link.Z == item.Z && link.GetType() == itemType)
                                {
                                    ((BaseDoor)item).Link = (BaseDoor)link;
                                    ((BaseDoor)link).Link = (BaseDoor)item;
                                    break;
                                }

                            eable.Free();
                        }
                        else if (item is MarkContainer)
                        {
                            try
                            {
                                ((MarkContainer)item).Target = Point3D.Parse(extra);
                            }
                            catch
                            {
                            }
                        }

                        item = null;
                    }
                }
            }

            if (item != null)
                item.Delete();

            return count;
        }

        private static bool FindItem(int x, int y, int z, Map map, Item srcItem)
        {
            var itemID = srcItem.ItemID;

            var res = false;

            IPooledEnumerable eable;

            if (srcItem is BaseDoor)
            {
                eable = map.GetItemsInRange(new Point3D(x, y, z), 1);

                foreach (Item item in eable)
                {
                    if (!(item is BaseDoor))
                        continue;

                    var bd = (BaseDoor)item;
                    Point3D p;
                    int bdItemID;

                    if (bd.Open)
                    {
                        p = new Point3D(bd.X - bd.Offset.X, bd.Y - bd.Offset.Y, bd.Z - bd.Offset.Z);
                        bdItemID = bd.ClosedID;
                    }
                    else
                    {
                        p = bd.Location;
                        bdItemID = bd.ItemID;
                    }

                    if (p.X != x || p.Y != y)
                        continue;

                    if (item.Z == z && bdItemID == itemID)
                        res = true;
                    else if (Math.Abs(item.Z - z) < 8)
                        m_DeleteQueue.Enqueue(item);
                }
            }
            else if ((TileData.ItemTable[itemID & TileData.MaxItemValue].Flags & TileFlag.LightSource) != 0)
            {
                eable = map.GetItemsInRange(new Point3D(x, y, z), 0);

                var lt = srcItem.Light;
                var srcName = srcItem.ItemData.Name;

                foreach (Item item in eable)
                    if (item.Z == z)
                    {
                        if (item.ItemID == itemID)
                        {
                            if (item.Light != lt)
                                m_DeleteQueue.Enqueue(item);
                            else
                                res = true;
                        }
                        else if ((item.ItemData.Flags & TileFlag.LightSource) != 0 && item.ItemData.Name == srcName)
                        {
                            m_DeleteQueue.Enqueue(item);
                        }
                    }
            }
            else if (srcItem is Teleporter || srcItem is FillableContainer || srcItem is BaseBook)
            {
                eable = map.GetItemsInRange(new Point3D(x, y, z), 0);

                var type = srcItem.GetType();

                foreach (Item item in eable)
                    if (item.Z == z && item.ItemID == itemID)
                    {
                        if (item.GetType() != type)
                            m_DeleteQueue.Enqueue(item);
                        else
                            res = true;
                    }
            }
            else
            {
                eable = map.GetItemsInRange(new Point3D(x, y, z), 0);

                foreach (Item item in eable)
                    if (item.Z == z && item.ItemID == itemID)
                    {
                        eable.Free();
                        return true;
                    }
            }

            eable.Free();

            while (m_DeleteQueue.Count > 0)
                ((Item)m_DeleteQueue.Dequeue()).Delete();

            return res;
        }
    }

    public class DecorationEntryMag
    {
        public Point3D Location { get; }

        public string Extra { get; }

        public DecorationEntryMag(string line)
        {
            string x, y, z;

            Pop(out x, ref line);
            Pop(out y, ref line);
            Pop(out z, ref line);

            Location = new Point3D(Utility.ToInt32(x), Utility.ToInt32(y), Utility.ToInt32(z));
            Extra = line;
        }

        public void Pop(out string v, ref string line)
        {
            var space = line.IndexOf(' ');

            if (space >= 0)
            {
                v = line.Substring(0, space++);
                line = line.Substring(space);
            }
            else
            {
                v = line;
                line = "";
            }
        }
    }
}
