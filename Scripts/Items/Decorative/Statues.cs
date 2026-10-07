namespace Server.Items
{
    public class StatueSouth : Item
    {
        public override bool ForceShowProperties => ObjectPropertyList.Enabled;

        [Constructable]
        public StatueSouth()
            : base(0x139A)
        {
            Weight = 10;
        }

        public StatueSouth(Serial serial)
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
            var version = reader.ReadInt();
        }
    }

    public class StatueSouth2 : Item
    {
        public override bool ForceShowProperties => ObjectPropertyList.Enabled;

        [Constructable]
        public StatueSouth2()
            : base(0x1227)
        {
            Weight = 10;
        }

        public StatueSouth2(Serial serial)
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
            var version = reader.ReadInt();
        }
    }

    public class StatueNorth : Item
    {
        public override bool ForceShowProperties => ObjectPropertyList.Enabled;

        [Constructable]
        public StatueNorth()
            : base(0x139B)
        {
            Weight = 10;
        }

        public StatueNorth(Serial serial)
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
            var version = reader.ReadInt();
        }
    }

    public class StatueWest : Item
    {
        public override bool ForceShowProperties => ObjectPropertyList.Enabled;

        [Constructable]
        public StatueWest()
            : base(0x1226)
        {
            Weight = 10;
        }

        public StatueWest(Serial serial)
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
            var version = reader.ReadInt();
        }
    }

    public class StatueEast : Item
    {
        public override bool ForceShowProperties => ObjectPropertyList.Enabled;

        [Constructable]
        public StatueEast()
            : base(0x139C)
        {
            Weight = 10;
        }

        public StatueEast(Serial serial)
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
            var version = reader.ReadInt();
        }
    }

    public class StatueEast2 : Item
    {
        public override bool ForceShowProperties => ObjectPropertyList.Enabled;

        [Constructable]
        public StatueEast2()
            : base(0x1224)
        {
            Weight = 10;
        }

        public StatueEast2(Serial serial)
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
            var version = reader.ReadInt();
        }
    }

    public class StatueSouthEast : Item
    {
        public override bool ForceShowProperties => ObjectPropertyList.Enabled;

        [Constructable]
        public StatueSouthEast()
            : base(0x1225)
        {
            Weight = 10;
        }

        public StatueSouthEast(Serial serial)
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
            var version = reader.ReadInt();
        }
    }

    public class BustSouth : Item
    {
        public override bool ForceShowProperties => ObjectPropertyList.Enabled;

        [Constructable]
        public BustSouth()
            : base(0x12CB)
        {
            Weight = 10;
        }

        public BustSouth(Serial serial)
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
            var version = reader.ReadInt();
        }
    }

    public class BustEast : Item
    {
        public override bool ForceShowProperties => ObjectPropertyList.Enabled;

        [Constructable]
        public BustEast()
            : base(0x12CA)
        {
            Weight = 10;
        }

        public BustEast(Serial serial)
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
            var version = reader.ReadInt();
        }
    }

    [TypeAlias("Server.Items.StatuePegasus")]
    public class StatuePegasusSouth : Item
    {
        public override int LabelNumber => 1044510; // pegasus statuette

        public override bool ForceShowProperties => ObjectPropertyList.Enabled;

        [Constructable]
        public StatuePegasusSouth()
            : base(0x139D)
        {
            Weight = 1.0;
        }

        public StatuePegasusSouth(Serial serial)
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
            var version = reader.ReadInt();
        }
    }

    [TypeAlias("Server.Items.StatuePegasus2")]
    public class StatuePegasusEast : Item
    {
        public override int LabelNumber => 1044510; // pegasus statuette

        public override bool ForceShowProperties => ObjectPropertyList.Enabled;

        [Constructable]
        public StatuePegasusEast()
            : base(0x1228)
        {
            Weight = 1.0;
        }

        public StatuePegasusEast(Serial serial)
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
            var version = reader.ReadInt();
        }
    }

    public class SmallTowerSculpture : Item
    {
        [Constructable]
        public SmallTowerSculpture()
            : base(0x241A)
        {
            Weight = 1.0;
        }

        public SmallTowerSculpture(Serial serial)
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
            var version = reader.ReadInt();
        }
    }

    [FlipableAttribute(0x494E, 0x494D)]
    public class StatueGargoyleEast : Item
    {
        public override bool ForceShowProperties => ObjectPropertyList.Enabled;

        [Constructable]
        public StatueGargoyleEast()
            : base(0x494E)
        {
            Weight = 1.0;
        }

        public StatueGargoyleEast(Serial serial)
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
            var version = reader.ReadInt();
        }
    }

    [FlipableAttribute(0x494D, 0x494E)]
    public class StatueGargoyleSouth : Item
    {
        public override bool ForceShowProperties => ObjectPropertyList.Enabled;

        [Constructable]
        public StatueGargoyleSouth()
            : base(0x494D)
        {
            Weight = 1.0;
        }

        public StatueGargoyleSouth(Serial serial)
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
            var version = reader.ReadInt();
        }
    }

    [FlipableAttribute(0x493C, 0x493B)]
    public class StatueGryphonEast : Item
    {
        public override bool ForceShowProperties => ObjectPropertyList.Enabled;

        [Constructable]
        public StatueGryphonEast()
            : base(0x493C)
        {
            Weight = 1.0;
        }

        public StatueGryphonEast(Serial serial)
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
            var version = reader.ReadInt();
        }
    }

    [FlipableAttribute(0x493B, 0x493C)]
    public class StatueGryphonSouth : Item
    {
        public override bool ForceShowProperties => ObjectPropertyList.Enabled;

        [Constructable]
        public StatueGryphonSouth()
            : base(0x493B)
        {
            Weight = 1.0;
        }

        public StatueGryphonSouth(Serial serial)
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
            var version = reader.ReadInt();
        }
    }
}
