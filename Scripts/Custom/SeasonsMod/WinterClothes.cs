using System;
using Server;
using Server.Items;

namespace Server.Custom.Seasons
{
    public interface ISeasonalClothing
    {
        int ColdProtection { get; }
        int RainProtection { get; }
    }

    // Компонент для крафта
    public class WoolenLining : Item
    {
        [Constructable]
        public WoolenLining() : base(0x103A) // Графика мотка ниток/ткани
        {
            Name = "Шерстяная подкладка";
            Weight = 1.0;
            Hue = 0x481; // Светло-серый/шерстяной цвет
        }

        public WoolenLining(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    // Утепленный зимний плащ
    public class WinterCloak : Cloak, ISeasonalClothing
    {
        public int ColdProtection => 2;
        public int RainProtection => 2;

        [Constructable]
        public WinterCloak() : base()
        {
            Name = "Утепленный зимний плащ";
            Hue = 0x4B5; // Сине-ледяной цвет по умолчанию
            Weight = 5.0;
        }

        public WinterCloak(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class WoolenHood : Bonnet, ISeasonalClothing
    {
        public int ColdProtection => 1;
        public int RainProtection => 0;

        [Constructable]
        public WoolenHood() : base()
        {
            Name = "Шерстяной капюшон";
            Hue = 0x481;
        }

        public WoolenHood(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class FurLinedBoots : FurBoots, ISeasonalClothing
    {
        public int ColdProtection => 1;
        public int RainProtection => 0;

        [Constructable]
        public FurLinedBoots() : base()
        {
            Name = "Утепленные меховые сапоги";
            Hue = 0x481;
        }

        public FurLinedBoots(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class OilskinCloak : Cloak, ISeasonalClothing
    {
        public int ColdProtection => 0;
        public int RainProtection => 2;

        [Constructable]
        public OilskinCloak() : base()
        {
            Name = "Непромокаемый плащ";
            Hue = 0x59B;
            Weight = 5.0;
        }

        public OilskinCloak(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class WoolenShirt : Shirt, ISeasonalClothing
    {
        public int ColdProtection => 1;
        public int RainProtection => 0;

        [Constructable]
        public WoolenShirt() : base()
        {
            Name = "Шерстяная рубаха";
            Hue = 0x481;
        }

        public WoolenShirt(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class WinterTunic : Tunic, ISeasonalClothing
    {
        public int ColdProtection => 2;
        public int RainProtection => 0;

        [Constructable]
        public WinterTunic() : base()
        {
            Name = "Утепленная зимняя туника";
            Hue = 0x481;
        }

        public WinterTunic(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class ThermalPants : LongPants, ISeasonalClothing
    {
        public int ColdProtection => 1;
        public int RainProtection => 0;

        [Constructable]
        public ThermalPants() : base()
        {
            Name = "Утепленные штаны";
            Hue = 0x481;
        }

        public ThermalPants(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class FurKilt : Kilt, ISeasonalClothing
    {
        public int ColdProtection => 1;
        public int RainProtection => 0;

        [Constructable]
        public FurKilt() : base()
        {
            Name = "Меховой килт";
            Hue = 0x481;
        }

        public FurKilt(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class RainHood : Bonnet, ISeasonalClothing
    {
        public int ColdProtection => 0;
        public int RainProtection => 1;

        [Constructable]
        public RainHood() : base()
        {
            Name = "Водонепроницаемый капюшон";
            Hue = 0x59B;
        }

        public RainHood(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class RainBoots : Boots, ISeasonalClothing
    {
        public int ColdProtection => 0;
        public int RainProtection => 1;

        [Constructable]
        public RainBoots() : base()
        {
            Name = "Непромокаемые сапоги";
            Hue = 0x59B;
        }

        public RainBoots(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}
