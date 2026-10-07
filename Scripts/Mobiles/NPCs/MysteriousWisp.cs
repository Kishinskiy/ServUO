using System;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Engines.Points;
using Server.Engines.Quests;
using Server.Gumps;
using Server.Items;
using Server.Network;
using Server.SkillHandlers;

namespace Server.Mobiles
{
    [CorpseName("a wisp corpse")]
    public class MysteriousWisp : BaseCreature
    {
        private readonly Dictionary<Mobile, int> m_Conversation = new Dictionary<Mobile, int>();

        private readonly Dictionary<Item, int> m_ItemTable = new Dictionary<Item, int>();

        private readonly string[][] m_Keywords =
        {
            new string[] { },
            new[] { "corporeal" },
            new[] { "sentient" },
            new[] { "deal" },
            new[] { "learn", "teach" },
            new[] { "good and evil", "good", "evil" },
            new[] { "guide" },
            new[] { "follow" },
            new[] { "fight" },
            new[] { "resonate" },
            new[] { "join" },
            new[] { "command" },
            new[] { "trade" }
        };

        private readonly int[] m_Responses =
        {
            1153441,
            1153443,
            1153445,
            1153447,
            1153449,
            1153451,
            1153453,
            1153455,
            1153457,
            1153459,
            1153461,
            1153463,
            1153467
        };

        private DateTime m_NextRestock;

        [CommandProperty(AccessLevel.GameMaster)]
        public int ItemCountMin { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ItemCountMax { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int IntensityMin { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int IntensityMax { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int RestockMin { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int RestockMax { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public double MutateChance { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ForceRestock
        {
            get => false;
            set
            {
                if (value) DoRestock();
            }
        }

        [Constructable]
        public MysteriousWisp()
            : base(AIType.AI_Mage, FightMode.None, 10, 1, 0.2, 0.4)
        {
            CantWalk = true;
            Blessed = true;
            SpeechHue = 52;

            Name = "a mysterious wisp";
            Body = 58;
            BaseSoundID = 466;

            SetStr(196, 225);
            SetDex(196, 225);
            SetInt(196, 225);

            SetHits(118, 135);

            SetDamage(17, 18);

            SetDamageType(ResistanceType.Physical, 50);
            SetDamageType(ResistanceType.Energy, 50);

            SetResistance(ResistanceType.Physical, 35, 45);
            SetResistance(ResistanceType.Fire, 20, 40);
            SetResistance(ResistanceType.Cold, 10, 30);
            SetResistance(ResistanceType.Poison, 5, 10);
            SetResistance(ResistanceType.Energy, 50, 70);

            SetSkill(SkillName.EvalInt, 80.0);
            SetSkill(SkillName.Magery, 80.0);
            SetSkill(SkillName.MagicResist, 80.0);
            SetSkill(SkillName.Tactics, 80.0);
            SetSkill(SkillName.Wrestling, 80.0);

            if (Backpack != null)
                Backpack.Delete();

            ItemCountMin = 14;
            ItemCountMax = 20;
            IntensityMin = 40;
            IntensityMax = 100;
            RestockMin = 45;
            RestockMax = 65;
            MutateChance = 0.10;

            AddItem(new BuyBackpack());
            DoRestock();
        }

        public MysteriousWisp(Serial serial)
            : base(serial)
        {
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            return m_Conversation.ContainsKey(from);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.InRange(Location, 5))
                if (!m_Conversation.ContainsKey(from))
                {
                    SayTo(from, m_Responses[0]);
                    m_Conversation[from] = 1;
                }

            if (from is PlayerMobile)
            {
                var pm = from as PlayerMobile;

                if (QuestHelper.CheckDoneOnce(pm, typeof(WishesOfTheWispQuest), null, false))
                {
                    var q = QuestHelper.GetQuest<WhisperingWithWispsQuest>(pm);

                    if (q == null)
                    {
                        var quest = QuestHelper.RandomQuest(pm, new[] { typeof(WhisperingWithWispsQuest) }, this);

                        if (quest != null)
                        {
                            pm.CloseGump(typeof(MondainQuestGump));
                            pm.SendGump(new MondainQuestGump(quest));
                        }
                    }
                    else if (q.Completed)
                    {
                        q.CompleteQuest();
                    }
                }
            }
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (m is PlayerMobile && InRange(m.Location, 5) && !InRange(oldLocation, 5))
            {
                var quest = QuestHelper.GetQuest<WishesOfTheWispQuest>((PlayerMobile)m);

                if (quest != null) quest.CompleteQuest();
            }
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (!m_Conversation.ContainsKey(e.Mobile))
                return;

            var speech = e.Speech.ToLower();
            var idx = m_Conversation[e.Mobile];

            if (idx < 0 || idx >= m_Keywords.Length)
            {
                m_Conversation.Remove(e.Mobile);
                return;
            }

            foreach (var str in m_Keywords[idx])
                if (speech.Contains(str))
                {
                    SayTo(e.Mobile, m_Responses[idx]);

                    if (idx + 1 >= m_Keywords.Length)
                        m_Conversation.Remove(e.Mobile);
                    else
                        m_Conversation[e.Mobile]++;

                    break;
                }
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            list.Add(new InternalEntry(this, from));
        }

        public void CheckRestock()
        {
            if (m_NextRestock <= DateTime.UtcNow)
                DoRestock();
        }

        private void DoRestock()
        {
            var list = new List<Item>(Backpack.Items);
            m_ItemTable.Clear();

            InternalGump.Clear();

            foreach (var item in list)
                item.Delete();

            var count = Utility.RandomMinMax(ItemCountMin, ItemCountMax);

            for (var i = 0; i < count; i++)
            {
                Item item;

                switch (Utility.Random(3))
                {
                    default:
                    case 0: item = Loot.RandomArmorOrShieldOrWeaponOrJewelry(false, false, true); break;
                    case 1: item = Loot.RandomArmorOrShieldOrWeaponOrJewelry(false, true); break;
                    case 2: item = Loot.RandomArmorOrShieldOrWeaponOrJewelry(true); break;
                }

                var weight = 0;

                if (0.10 > Utility.RandomDouble())
                {
                    var budget = Utility.RandomMinMax(500, 860);
                    RunicReforging.GenerateRandomItem(item, null, Utility.RandomMinMax(500, 860), 0,
                        ReforgedPrefix.None, ReforgedSuffix.None);
                    weight += budget / 3;
                }
                else
                {
                    var props = Utility.RandomMinMax(3, 5);

                    if (props == 5 && 0.05 > Utility.RandomDouble())
                    {
                        props++;
                        weight += 100;
                    }

                    if (item is BaseWeapon)
                        BaseRunicTool.ApplyAttributesTo((BaseWeapon)item, false, 0, props, IntensityMin, IntensityMax);
                    else if (item is BaseArmor)
                        BaseRunicTool.ApplyAttributesTo((BaseArmor)item, false, 0, props, IntensityMin, IntensityMax);
                    else if (item is BaseJewel)
                        BaseRunicTool.ApplyAttributesTo((BaseJewel)item, false, 0, props, IntensityMin, IntensityMax);
                    else if (item is BaseHat)
                        BaseRunicTool.ApplyAttributesTo((BaseHat)item, false, 0, props, IntensityMin, IntensityMax);
                }

                m_ItemTable[item] = (int)((weight + Imbuing.GetTotalWeight(item, -1, false, true)) * 31.5);
                item.Movable = false;
                Backpack.DropItem(item);
            }

            m_NextRestock = DateTime.UtcNow + TimeSpan.FromMinutes(Utility.RandomMinMax(RestockMin, RestockMax));
        }

        public int GetCostFor(Item item)
        {
            if (!m_ItemTable.ContainsKey(item))
                return -1;

            return m_ItemTable[item];
        }

        public void TryBuyItem(Mobile from, Item item)
        {
            if (item.Deleted || !Backpack.Items.Contains(item) || !m_ItemTable.ContainsKey(item))
            {
                from.SendMessage("This item is no longer available.");
                return;
            }

            var points = (int)PointsSystem.DespiseCrystals.GetPoints(from);
            var cost = m_ItemTable[item];

            if (points >= cost)
            {
                //DespiseController.Instance.DeductDespisePoints(from, cost);
                PointsSystem.DespiseCrystals.DeductPoints(from, cost);
                item.Movable = true;

                if (from.Backpack == null || !from.Backpack.TryDropItem(from, item, false))
                    item.MoveToWorld(from.Location, from.Map);

                from.SendLocalizedMessage(1153427,
                    cost.ToString()); // You have spent ~1_AMT~ Dungeon Crystal Points of Despise.

                InternalGump.Resend(this);
            }
            else
            {
                from.SendMessage("You need {0} more Despise dungeon crystal points for this.", cost - points);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);

            writer.Write(ItemCountMin);
            writer.Write(ItemCountMax);
            writer.Write(IntensityMin);
            writer.Write(IntensityMax);
            writer.Write(MutateChance);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            ItemCountMin = reader.ReadInt();
            ItemCountMax = reader.ReadInt();
            IntensityMin = reader.ReadInt();
            IntensityMax = reader.ReadInt();
            MutateChance = reader.ReadDouble();

            m_NextRestock = DateTime.UtcNow;
        }

        private class InternalEntry : ContextMenuEntry
        {
            private readonly Mobile m_Clicker;
            private readonly MysteriousWisp m_Wisp;

            public InternalEntry(MysteriousWisp wisp, Mobile clicker)
                : base(1150143, 3)
            {
                m_Clicker = clicker;
                m_Wisp = wisp;
            }

            public override void OnClick()
            {
                if (m_Clicker.Alive && !m_Wisp.Deleted && m_Clicker.InRange(m_Wisp.Location, 3))
                {
                    if (m_Wisp.Backpack == null)
                    {
                        Console.WriteLine("Error, wisp backpack is null");
                        return;
                    }

                    m_Wisp.CheckRestock();
                    m_Clicker.SendGump(new InternalGump(m_Wisp, m_Clicker));
                }
            }
        }

        public class BuyBackpack : Backpack
        {
            public override int DefaultMaxWeight => 0;

            public BuyBackpack()
            {
                Movable = false;
                Layer = Layer.Backpack;
                Weight = 1.0;
            }

            public BuyBackpack(Serial serial)
                : base(serial)
            {
            }

            public override bool IsAccessibleTo(Mobile m)
            {
                return true;
            }

            public override bool CheckItemUse(Mobile from, Item item)
            {
                from.SendLocalizedMessage(500447); // That is not accessible.
                return false;
            }

            public override void GetChildProperties(ObjectPropertyList list, Item item)
            {
                base.GetChildProperties(list, item);

                var wisp = RootParent as MysteriousWisp;

                if (wisp == null)
                    return;

                var cost = wisp.GetCostFor(item);

                list.Add(1153425, cost.ToString()); // Price: ~1_AMT~ Despise Crystals
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                writer.Write(0); // version
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                var version = reader.ReadInt();
            }
        }

        private class InternalGump : Gump
        {
            private static readonly List<Mobile> m_Viewers = new List<Mobile>();
            private readonly List<Item> m_Items;

            private readonly MysteriousWisp m_Wisp;

            public InternalGump(MysteriousWisp wisp, Mobile viewer) : base(40, 40)
            {
                m_Wisp = wisp;
                if (!m_Viewers.Contains(viewer))
                    m_Viewers.Add(viewer);

                AddBackground(0, 0, 320, 404, 0x13BE);
                AddImageTiled(10, 10, 300, 20, 0xA40);
                AddImageTiled(10, 40, 300, 324, 0xA40);
                AddImageTiled(10, 374, 300, 20, 0xA40);
                AddAlphaRegion(10, 10, 300, 384);

                AddHtml(14, 12, 500, 20, "<BASEFONT COLOR=#FFFFFF>Mysterious Wisp Inventory</BASEFONT>", false, false);

                AddButton(10, 374, 0xFB1, 0xFB2, 0, GumpButtonType.Reply, 0);
                AddHtmlLocalized(45, 376, 450, 20, 1011012, 0x7FFF, false, false); // CANCEL

                m_Items = new List<Item>(m_Wisp.Backpack.Items);
                var current = 0;

                for (var i = 0; i < m_Items.Count; i++)
                {
                    var item = m_Items[i];

                    var page = current / 5 + 1;
                    var pos = current % 5;

                    if (pos == 0)
                    {
                        if (page > 1)
                        {
                            AddButton(200, 374, 0xFA5, 0xFA7, 0, GumpButtonType.Page, page);
                            AddHtmlLocalized(240, 376, 60, 20, 1043353, 0x7FFF, false, false); // Next
                        }

                        AddPage(page);

                        if (page > 1)
                        {
                            AddButton(100, 374, 0xFAE, 0xFB0, 0, GumpButtonType.Page, page - 1);
                            AddHtmlLocalized(140, 376, 60, 20, 1011393, 0x7FFF, false, false); // Back
                        }
                    }

                    if (item != null && !item.Deleted)
                    {
                        var x = 14;
                        var y = pos * 64 + 44;

                        var b = ItemBounds.Table[item.ItemID];

                        AddImageTiledButton(x, y, 0x918, 0x919, i + 1, GumpButtonType.Reply, 0, item.ItemID, item.Hue,
                            40 - b.Width / 2 - b.X, 30 - b.Height / 2 - b.Y);
                        AddItemProperty(item.Serial);

                        var cost = m_Wisp.GetCostFor(item) > -1
                            ? m_Wisp.GetCostFor(item).ToString("###,###,###")
                            : "No Longer Available";

                        AddHtmlLocalized(x + 84, y, 250, 60, item.LabelNumber, 0x7FFF, false, false);
                        AddHtml(x + 84, y + 20, 250, 60, "<BASEFONT COLOR=#FFFFFF>" + cost + "</BASEFONT>", false,
                            false);

                        current++;
                    }
                }
            }

            public override void OnResponse(NetState state, RelayInfo info)
            {
                var from = state.Mobile;

                if (m_Viewers.Contains(from))
                    m_Viewers.Remove(from);

                var idx = info.ButtonID - 1;

                if (idx >= 0 && idx < m_Items.Count)
                {
                    var item = m_Items[idx];

                    if (item != null && !item.Deleted && m_Wisp.Backpack != null &&
                        m_Wisp.Backpack.Items.Contains(item)) from.SendGump(new InternalGump2(m_Wisp, item));
                }
            }

            public static void Resend(MysteriousWisp wisp)
            {
                if (m_Viewers == null || m_Viewers.Count == 0)
                    return;

                var viewers = new List<Mobile>(m_Viewers);
                var t = typeof(InternalGump);

                foreach (var m in viewers)
                    if (m.HasGump(t))
                    {
                        m.CloseGump(t);

                        if (m_Viewers.Contains(m))
                            m_Viewers.Remove(m);

                        if (wisp.InRange(m.Location, 5) && wisp.Backpack != null)
                            m.SendGump(new InternalGump(wisp, m));
                    }
            }

            public static void Clear()
            {
                if (m_Viewers == null || m_Viewers.Count == 0)
                    return;

                var viewers = new List<Mobile>(m_Viewers);
                var t = typeof(InternalGump);

                foreach (var m in viewers)
                    if (m.HasGump(t))
                        m.CloseGump(t);

                m_Viewers.Clear();
            }
        }

        private class InternalGump2 : Gump
        {
            private readonly bool m_Available;
            private readonly Item m_Item;
            private readonly MysteriousWisp m_Wisp;

            public InternalGump2(MysteriousWisp wisp, Item item) : base(40, 40)
            {
                m_Wisp = wisp;
                m_Item = item;

                AddImageTiled(0, 0, 300, 180, 0xA8E);
                AddAlphaRegion(0, 0, 300, 180);
                AddBackground(150, 10, 130, 130, 0xBB8);

                AddHtml(10, 10, 150, 20, "<BASEFONT COLOR=#FFFFFF>CONFIRM PURCHASE:</BASEFONT>", false, false);
                AddHtmlLocalized(10, 30, 140, 20, item.LabelNumber, 0x7FFF, false, false);

                m_Available = m_Wisp.GetCostFor(item) > -1;
                var cost = m_Available ? m_Wisp.GetCostFor(item).ToString() : "No Longer Available";

                AddHtml(10, 100, 140, 20, "<BASEFONT COLOR=#FFFFFF>Despise Crystals:</BASEFONT>", false, false);
                AddHtml(10, 120, 140, 20, "<BASEFONT COLOR=#FFFFFF>" + cost + "</BASEFONT>", false, false);

                var b = ItemBounds.Table[item.ItemID];
                AddItem(210 - b.Width / 2 - b.X, 70 - b.Height / 2 - b.Y, item.ItemID, item.Hue);
                AddItemProperty(item.Serial);

                if (m_Available)
                    AddButton(90, 150, 2128, 2129, 1, GumpButtonType.Reply, 0);

                AddButton(155, 150, 2119, 2120, 0, GumpButtonType.Reply, 0);
            }

            public override void OnResponse(NetState state, RelayInfo info)
            {
                var from = state.Mobile;

                if (info.ButtonID == 1)
                {
                    if (m_Available)
                        m_Wisp.TryBuyItem(from, m_Item);
                    else
                        from.SendMessage("That item is no longer available");
                }
            }
        }
    }
}
