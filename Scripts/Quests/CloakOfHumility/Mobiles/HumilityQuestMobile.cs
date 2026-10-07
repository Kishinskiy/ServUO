using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Engines.Quests
{
    public class HumilityQuestMobile : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();

        private DateTime m_NextGreet;
        public virtual int Greeting => 0;

        public override bool IsActiveVendor => false;
        public override bool IsInvulnerable => true;
        public override bool CanTeach => false;
        public override bool PlayerRangeSensitive => false;
        protected override List<SBInfo> SBInfos => m_SBInfos;

        public static List<HumilityQuestMobile> Instance { get; } = new List<HumilityQuestMobile>();

        public HumilityQuestMobile(string name) : base(null)
        {
            Name = name;
            m_NextGreet = DateTime.UtcNow;
        }

        public HumilityQuestMobile(string name, string title) : base(title)
        {
            Name = name;
            m_NextGreet = DateTime.UtcNow;
        }

        public HumilityQuestMobile(Serial serial) : base(serial)
        {
        }

        public override void InitSBInfo()
        {
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            var pm = m as PlayerMobile;

            if (pm == null || !pm.InRange(Location, 3))
                return;

            var quest = QuestHelper.GetQuest(pm, typeof(WhosMostHumbleQuest)) as WhosMostHumbleQuest;

            if (quest != null)
                if (m_NextGreet < DateTime.UtcNow && pm is PlayerMobile)
                {
                    var item = pm.FindItemOnLayer(Layer.Cloak);

                    if (item is GreyCloak && ((GreyCloak)item).Owner == null && Greeting > 0)
                    {
                        SayTo(pm, Greeting);

                        m_NextGreet = DateTime.UtcNow + TimeSpan.FromSeconds(5);
                    }
                }
        }

        public override void OnDoubleClick(Mobile from)
        {
            var pm = from as PlayerMobile;

            if (pm == null || !InRange(from.Location, 3))
                return;

            var quest = QuestHelper.GetQuest(pm, typeof(WhosMostHumbleQuest)) as WhosMostHumbleQuest;

            if (quest != null && pm.Backpack != null && !quest.HasGivenTo(this))
            {
                var item = from.FindItemOnLayer(Layer.Cloak);

                if (item is GreyCloak && ((GreyCloak)item).Owner == null)
                {
                    var idx = HumilityQuestMobileInfo.GetNPCIndex(GetType());

                    if (idx > -1 && quest.Infos.ContainsKey(idx) && idx < quest.Infos.Count)
                    {
                        var needs = quest.Infos[idx].Needs;

                        var need = from.Backpack.FindItemByType(needs);

                        // Found needed item
                        if (need != null)
                        {
                            need.Delete();
                            quest.RemoveQuestItem(need);

                            var nextItem = Loot.Construct(quest.Infos[idx].Gives);

                            if (nextItem != null)
                            {
                                from.Backpack.DropItem(nextItem);
                                quest.AddQuestItem(nextItem, this);

                                if (this is Sean)
                                    SayTo(from, Greeting + 3, string.Format("#{0}", quest.Infos[idx].NeedsLoc));
                                else
                                    SayTo(from, Greeting + 4,
                                        string.Format("#{0}\t#{1}", quest.Infos[idx].NeedsLoc,
                                            quest.Infos[idx].GivesLoc));
                            }
                        }
                        else //Didn't find needed item
                        {
                            from.SendGump(new HumilityItemQuestGump(this, quest, idx));
                        }
                    }
                    else
                    {
                        Console.WriteLine("Error finding index for {0}", this);
                    }
                }
                else
                {
                    base.OnDoubleClick(from);
                }
            }
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

            m_NextGreet = DateTime.UtcNow;
        }
    }

    public class HumilityQuestMobileInfo
    {
        // Greeting(say), Greeting2(gump), Desire(say), Gift(say), OnExchange(say)

        public Type Needs { get; }

        public Type Gives { get; }

        public int NeedsLoc { get; }

        public int GivesLoc { get; }

        public static Type[] ItemTypes { get; } =
        {
            typeof(BrassRing),
            typeof(SeasonedSkillet),
            typeof(VillageCauldron),
            typeof(ShortStool),
            typeof(FriendshipMug),
            typeof(WornHammer),
            typeof(PairOfWorkGloves),
            typeof(IronChain)
        };

        public static int[] ItemLocs { get; } =
        {
            1075778,
            1075774,
            1075775,
            1075776,
            1075777,
            1075779,
            1075780,
            1075788
        };

        public static Type[] MobileTypes { get; } =
        {
            typeof(Maribel),
            typeof(Dierdre),
            typeof(Kevin),
            typeof(Jason),
            typeof(Walton),
            typeof(Nelson),
            typeof(Sean)
        };

        public HumilityQuestMobileInfo(Type needs, Type gives, int needsLoc, int givesLoc)
        {
            Needs = needs;
            Gives = gives;
            NeedsLoc = needsLoc;
            GivesLoc = givesLoc;
        }

        public HumilityQuestMobileInfo(GenericReader reader)
        {
            var version = reader.ReadInt();

            var needs = reader.ReadInt();
            var gives = reader.ReadInt();

            Needs = ItemTypes[needs];

            if (gives == -1)
                Gives = typeof(IronChain);
            else
                Gives = ItemTypes[gives];

            NeedsLoc = reader.ReadInt();
            GivesLoc = reader.ReadInt();
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write(0);

            var needs = Array.IndexOf(ItemTypes, Needs);
            writer.Write(needs);

            var gives = Array.IndexOf(ItemTypes, Gives);
            writer.Write(gives);

            writer.Write(NeedsLoc);
            writer.Write(GivesLoc);
        }

        public static int GetNPCIndex(Type type)
        {
            for (var i = 0; i < MobileTypes.Length; i++)
                if (MobileTypes[i] == type)
                    return i;

            return -1;
        }

        public static int GetLoc(Type type)
        {
            for (var i = 0; i < ItemTypes.Length; i++)
                if (type == ItemTypes[i])
                    return ItemLocs[i];

            return -1;
        }
    }
}
