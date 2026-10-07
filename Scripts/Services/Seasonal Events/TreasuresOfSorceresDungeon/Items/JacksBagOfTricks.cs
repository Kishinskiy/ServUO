using System;
using Server.Events.Halloween;
using Server.Network;

namespace Server.Items
{
    public class JacksBagOfTricks : Item
    {
        public override int LabelNumber => 1157656; // Jack's Bag of Tricks

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextUse { get; set; }

        [Constructable]
        public JacksBagOfTricks()
            : base(0x9F85)
        {
        }

        public JacksBagOfTricks(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (DateTime.Now.Month == 10 || from.AccessLevel > AccessLevel.Player)
            {
                if (NextUse < DateTime.UtcNow)
                {
                    Item item = null;

                    if (0.1 > Utility.RandomDouble())
                        item = HolidaySettings.RandomTreat;
                    else
                        switch (Utility.Random(6))
                        {
                            case 0: item = new RancidReindeerMeat(); break;
                            case 1: item = new GlassyCandyCane(); break;
                            case 2: item = new NamedSeveredElfEars(); break;
                            case 3: item = new SuspiciousGiftBox(); break;
                            case 4: item = new InsultingDoll(); break;
                            case 5: item = new SpikedEggnog(); break;
                        }

                    if (item != null)
                    {
                        if (from.Backpack == null || !from.Backpack.TryDropItem(from, item, false))
                            item.MoveToWorld(from.Location, from.Map);

                        NextUse = DateTime.UtcNow + TimeSpan.FromDays(1);
                    }
                }
                else
                {
                    from.SendLocalizedMessage(1157633); // You have already used this today.
                }
            }
            else
            {
                from.SendLocalizedMessage(1157632); // This only works during the Month of October.
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);

            writer.Write(NextUse);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var v = reader.ReadInt();

            NextUse = reader.ReadDateTime();
        }
    }

    [Flipable(0x1E90, 0x1E91)]
    public class RancidReindeerMeat : Item
    {
        public override int LabelNumber => 1157634; // Rancid Reindeer Meat

        [Constructable]
        public RancidReindeerMeat()
            : base(0x1E90)
        {
            Hue = 2707;
        }

        public RancidReindeerMeat(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile m)
        {
            if (IsChildOf(m.Backpack))
                PrivateOverheadMessage(MessageType.Regular, 0, 1157635, m.NetState); // *It smells terrible!*
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var v = reader.ReadInt();
        }
    }

    [Flipable(0x9DC3, 0x9DC4)]
    public class GlassyCandyCane : Item
    {
        public override int LabelNumber => 1157636; // Glass Candy Cane

        [Constructable]
        public GlassyCandyCane()
            : base(0x9DC3)
        {
            Hue = 1927;
        }

        public GlassyCandyCane(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var v = reader.ReadInt();
        }
    }

    [Flipable(0x312D, 0x312E)]
    public class NamedSeveredElfEars : Item
    {
        private readonly string[] _Names =
        {
            "Alabaster Snowball", "Pepper Minstix", "Wunorse Openslae", "Sugarplum Mary"
        };

        private string _Name;

        [Constructable]
        public NamedSeveredElfEars()
            : base(0x312D)
        {
            _Name = _Names[Utility.Random(_Names.Length)];
        }

        public NamedSeveredElfEars(Serial serial)
            : base(serial)
        {
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            list.Add(1157637, _Name); // The Severed Ears of an Elf Named ~1_NAME~
        }


        public override void OnDoubleClick(Mobile m)
        {
            if (IsChildOf(m.Backpack))
                PrivateOverheadMessage(MessageType.Regular, 0, 1157638,
                    m.NetState); // *You hear the faint jingle of cheery bells...*
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);

            writer.Write(_Name);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var v = reader.ReadInt();

            _Name = reader.ReadString();
        }
    }

    public class SuspiciousGiftBox : Container
    {
        public override int LabelNumber => 1157639; // Suspicious Gift Box

        [Constructable]
        public SuspiciousGiftBox()
            : base(0x232A)
        {
            Hue = 1976;
        }

        public SuspiciousGiftBox(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile m)
        {
            if (IsChildOf(m.Backpack)) PrivateOverheadMessage(MessageType.Regular, 0, 1157640, m.NetState); // Uh oh...
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var v = reader.ReadInt();
        }
    }

    [Flipable(0x48E2, 0x48E3)]
    public class InsultingDoll : Item
    {
        public override int LabelNumber => 1157641; // Insulting Doll

        [Constructable]
        public InsultingDoll()
            : base(0x48E2)
        {
            Hue = 1933;
        }

        public InsultingDoll(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile m)
        {
            if (IsChildOf(m.Backpack))
                PrivateOverheadMessage(MessageType.Regular, 0, Utility.RandomMinMax(1157642, 1157646), m.NetState);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var v = reader.ReadInt();
        }
    }

    public class SpikedEggnog : Item
    {
        public override int LabelNumber => 1157647; // Spiked Egg Nog

        [Constructable]
        public SpikedEggnog()
            : base(0x142A)
        {
            Hue = 2711;
        }

        public SpikedEggnog(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var v = reader.ReadInt();
        }
    }
}
