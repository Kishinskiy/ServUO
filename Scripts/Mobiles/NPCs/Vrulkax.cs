using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests
{
    public class Vrulkax : BaseVendor
    {
        private readonly Type[][] _Table =
        {
            new[] { typeof(BritchesOfWarding), typeof(GargishBritchesOfWarding) },
            new[] { typeof(GlovesOfFeudalGrip), typeof(GargishKiltOfFeudalVise) },
            new[] { typeof(CuffsOfTheArchmage), typeof(GargishCuffsOfTheArchmage) },
            new[] { typeof(BritchesOfWarding), typeof(GargishBritchesOfWarding) },
            new[] { typeof(BowOfTheInfiniteSwarm), typeof(GlaiveOfTheInfiniteSwarm) }
        };

        protected readonly List<SBInfo> m_SBInfos = new List<SBInfo>();

        private DateTime _NextSpeak;
        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override bool IsActiveVendor => false;
        public override bool IsInvulnerable => true;

        [Constructable]
        public Vrulkax()
            : base("the Exalted Artificer")
        {
            Name = "Vrulkax";
        }

        public Vrulkax(Serial serial)
            : base(serial)
        {
        }

        public override void InitSBInfo()
        {
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            foreach (var t in _Table)
                if (dropped is IDurability && t[0] == dropped.GetType())
                {
                    var dur = dropped as IDurability;

                    if (dur != null && dur.MaxHitPoints == 255 && dur.HitPoints == 255)
                    {
                        var item = Loot.Construct(t[1]);

                        if (item != null)
                        {
                            from.AddToBackpack(item);
                            dropped.Delete();
                            return true;
                        }
                    }
                    else
                    {
                        SayTo(from, 1157368); // I can only work with artifacts that are in pristine condition.
                        return false;
                    }
                }

            SayTo(from, 1157365); // I'm sorry, I cannot accept this item.
            return false;
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (_NextSpeak < DateTime.UtcNow && m is PlayerMobile && InRange(m.Location, 5) && InLOS(m))
            {
                Say(1157369); // I am the Exalted Artificer. Hand me select human artifacts to convert them for gargoyle use.
                _NextSpeak = DateTime.UtcNow + TimeSpan.FromSeconds(25);
            }

            base.OnMovement(m, oldLocation);
        }

        public override void InitBody()
        {
            InitStats(100, 100, 25);

            CantWalk = true;
            Race = Race.Gargoyle;

            Hue = 34545;
            HairItemID = 0x4259;
            HairHue = 0x0;
        }

        public override void InitOutfit()
        {
            AddItem(new SerpentStoneStaff());
            AddItem(new GargishClothChest(902));
            AddItem(new GargishClothArms(902));
            AddItem(new GargishClothKilt(902));
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
}
