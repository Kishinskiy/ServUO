using System.Linq;
using Server.Mobiles;

namespace Server.Items
{
    public class HelmOfVillainousEpiphany : DragonHelm, IEpiphanyArmor
    {
        public override int LabelNumber => 1150253; // Helm of Villainous Epiphany

        [Constructable]
        public HelmOfVillainousEpiphany()
        {
            Resource = CraftResource.None;

            Hue = 1778;
            ArmorAttributes.MageArmor = 1;
        }

        public HelmOfVillainousEpiphany(Serial serial) : base(serial)
        {
        }

        public Alignment Alignment => Alignment.Evil;
        public SurgeType Type => SurgeType.Mana;
        public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);
        public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

        public override void AddWeightProperty(ObjectPropertyList list)
        {
            base.AddWeightProperty(list);

            EpiphanyHelper.AddProperties(this, list);
        }

        public override bool OnEquip(Mobile from)
        {
            var canEquip = base.OnEquip(from);

            if (canEquip)
                foreach (var armor in from.Items.Where(i => i is IEpiphanyArmor))
                    armor.InvalidateProperties();

            return canEquip;
        }

        public override void OnRemoved(object parent)
        {
            base.OnRemoved(parent);

            if (parent is Mobile)
            {
                var m = (Mobile)parent;

                foreach (var armor in m.Items.Where(i => i is IEpiphanyArmor)) armor.InvalidateProperties();
            }
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

    public class GorgetOfVillainousEpiphany : PlateGorget, IEpiphanyArmor
    {
        public override int LabelNumber => 1150254; // Gorget of Villainous Epiphany

        [Constructable]
        public GorgetOfVillainousEpiphany()
        {
            Hue = 1778;
            ArmorAttributes.MageArmor = 1;
        }

        public GorgetOfVillainousEpiphany(Serial serial) : base(serial)
        {
        }

        public Alignment Alignment => Alignment.Evil;
        public SurgeType Type => SurgeType.Mana;
        public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);
        public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

        public override void AddWeightProperty(ObjectPropertyList list)
        {
            base.AddWeightProperty(list);

            EpiphanyHelper.AddProperties(this, list);
        }

        public override bool OnEquip(Mobile from)
        {
            var canEquip = base.OnEquip(from);

            if (canEquip)
                foreach (var armor in from.Items.Where(i => i is IEpiphanyArmor))
                    armor.InvalidateProperties();

            return canEquip;
        }

        public override void OnRemoved(object parent)
        {
            base.OnRemoved(parent);

            if (parent is Mobile)
            {
                var m = (Mobile)parent;

                foreach (var armor in m.Items.Where(i => i is IEpiphanyArmor)) armor.InvalidateProperties();
            }
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

    public class BreastplateOfVillainousEpiphany : DragonChest, IEpiphanyArmor
    {
        public override int LabelNumber => 1150255; // Breastplate of Villainous Epiphany

        [Constructable]
        public BreastplateOfVillainousEpiphany()
        {
            Resource = CraftResource.None;

            Hue = 1778;
            ArmorAttributes.MageArmor = 1;
        }

        public BreastplateOfVillainousEpiphany(Serial serial) : base(serial)
        {
        }

        public Alignment Alignment => Alignment.Evil;
        public SurgeType Type => SurgeType.Mana;
        public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);
        public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

        public override void AddWeightProperty(ObjectPropertyList list)
        {
            base.AddWeightProperty(list);

            EpiphanyHelper.AddProperties(this, list);
        }

        public override bool OnEquip(Mobile from)
        {
            var canEquip = base.OnEquip(from);

            if (canEquip)
                foreach (var armor in from.Items.Where(i => i is IEpiphanyArmor))
                    armor.InvalidateProperties();

            return canEquip;
        }

        public override void OnRemoved(object parent)
        {
            base.OnRemoved(parent);

            if (parent is Mobile)
            {
                var m = (Mobile)parent;

                foreach (var armor in m.Items.Where(i => i is IEpiphanyArmor)) armor.InvalidateProperties();
            }
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

    public class ArmsOfVillainousEpiphany : DragonArms, IEpiphanyArmor
    {
        public override int LabelNumber => 1150256; // Arms of Villainous Epiphany

        [Constructable]
        public ArmsOfVillainousEpiphany()
        {
            Resource = CraftResource.None;

            Hue = 1778;
            ArmorAttributes.MageArmor = 1;
        }

        public ArmsOfVillainousEpiphany(Serial serial) : base(serial)
        {
        }

        public Alignment Alignment => Alignment.Evil;
        public SurgeType Type => SurgeType.Mana;
        public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);
        public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

        public override void AddWeightProperty(ObjectPropertyList list)
        {
            base.AddWeightProperty(list);

            EpiphanyHelper.AddProperties(this, list);
        }

        public override bool OnEquip(Mobile from)
        {
            var canEquip = base.OnEquip(from);

            if (canEquip)
                foreach (var armor in from.Items.Where(i => i is IEpiphanyArmor))
                    armor.InvalidateProperties();

            return canEquip;
        }

        public override void OnRemoved(object parent)
        {
            base.OnRemoved(parent);

            if (parent is Mobile)
            {
                var m = (Mobile)parent;

                foreach (var armor in m.Items.Where(i => i is IEpiphanyArmor)) armor.InvalidateProperties();
            }
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

    public class GauntletsOfVillainousEpiphany : DragonGloves, IEpiphanyArmor
    {
        public override int LabelNumber => 1150257; // Gauntlets of Villainous Epiphany

        [Constructable]
        public GauntletsOfVillainousEpiphany()
        {
            Resource = CraftResource.None;

            Hue = 1778;
            ArmorAttributes.MageArmor = 1;
        }

        public GauntletsOfVillainousEpiphany(Serial serial) : base(serial)
        {
        }

        public Alignment Alignment => Alignment.Evil;
        public SurgeType Type => SurgeType.Mana;
        public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);
        public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

        public override void AddWeightProperty(ObjectPropertyList list)
        {
            base.AddWeightProperty(list);

            EpiphanyHelper.AddProperties(this, list);
        }

        public override bool OnEquip(Mobile from)
        {
            var canEquip = base.OnEquip(from);

            if (canEquip)
                foreach (var armor in from.Items.Where(i => i is IEpiphanyArmor))
                    armor.InvalidateProperties();

            return canEquip;
        }

        public override void OnRemoved(object parent)
        {
            base.OnRemoved(parent);

            if (parent is Mobile)
            {
                var m = (Mobile)parent;

                foreach (var armor in m.Items.Where(i => i is IEpiphanyArmor)) armor.InvalidateProperties();
            }
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

    public class LegsOfVillainousEpiphany : DragonLegs, IEpiphanyArmor
    {
        public override int LabelNumber => 1150258; // Leggings of Villainous Epiphany

        [Constructable]
        public LegsOfVillainousEpiphany()
        {
            Resource = CraftResource.None;

            Hue = 1778;
            ArmorAttributes.MageArmor = 1;
        }

        public LegsOfVillainousEpiphany(Serial serial) : base(serial)
        {
        }

        public Alignment Alignment => Alignment.Evil;
        public SurgeType Type => SurgeType.Mana;
        public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);
        public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

        public override void AddWeightProperty(ObjectPropertyList list)
        {
            base.AddWeightProperty(list);

            EpiphanyHelper.AddProperties(this, list);
        }

        public override bool OnEquip(Mobile from)
        {
            var canEquip = base.OnEquip(from);

            if (canEquip)
                foreach (var armor in from.Items.Where(i => i is IEpiphanyArmor))
                    armor.InvalidateProperties();

            return canEquip;
        }

        public override void OnRemoved(object parent)
        {
            base.OnRemoved(parent);

            if (parent is Mobile)
            {
                var m = (Mobile)parent;

                foreach (var armor in m.Items.Where(i => i is IEpiphanyArmor)) armor.InvalidateProperties();
            }
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

    public class KiltOfVillainousEpiphany : GargishPlateKilt, IEpiphanyArmor
    {
        public override int LabelNumber => 1150263; // Kilt of Villainous Epiphany

        [Constructable]
        public KiltOfVillainousEpiphany()
        {
            Hue = 1778;
            ArmorAttributes.MageArmor = 1;
        }

        public KiltOfVillainousEpiphany(Serial serial) : base(serial)
        {
        }

        public Alignment Alignment => Alignment.Evil;
        public SurgeType Type => SurgeType.Mana;
        public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);
        public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

        public override void AddWeightProperty(ObjectPropertyList list)
        {
            base.AddWeightProperty(list);

            EpiphanyHelper.AddProperties(this, list);
        }

        public override bool OnEquip(Mobile from)
        {
            var canEquip = base.OnEquip(from);

            if (canEquip)
                foreach (var armor in from.Items.Where(i => i is IEpiphanyArmor))
                    armor.InvalidateProperties();

            return canEquip;
        }

        public override void OnRemoved(object parent)
        {
            base.OnRemoved(parent);

            if (parent is Mobile)
            {
                var m = (Mobile)parent;

                foreach (var armor in m.Items.Where(i => i is IEpiphanyArmor)) armor.InvalidateProperties();
            }
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

    public class EarringsOfVillainousEpiphany : GargishEarrings, IEpiphanyArmor
    {
        public override int LabelNumber => 1150260; // Earrings of Villainous Epiphany

        [Constructable]
        public EarringsOfVillainousEpiphany()
        {
            Hue = 1778;
            ArmorAttributes.MageArmor = 1;
        }

        public EarringsOfVillainousEpiphany(Serial serial) : base(serial)
        {
        }

        public Alignment Alignment => Alignment.Evil;
        public SurgeType Type => SurgeType.Mana;
        public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);
        public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

        public override void AddWeightProperty(ObjectPropertyList list)
        {
            base.AddWeightProperty(list);

            EpiphanyHelper.AddProperties(this, list);
        }

        public override bool OnEquip(Mobile from)
        {
            var canEquip = base.OnEquip(from);

            if (canEquip)
                foreach (var armor in from.Items.Where(i => i is IEpiphanyArmor))
                    armor.InvalidateProperties();

            return canEquip;
        }

        public override void OnRemoved(object parent)
        {
            base.OnRemoved(parent);

            if (parent is Mobile)
            {
                var m = (Mobile)parent;

                foreach (var armor in m.Items.Where(i => i is IEpiphanyArmor)) armor.InvalidateProperties();
            }
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

    public class GargishBreastplateOfVillainousEpiphany : GargishPlateChest, IEpiphanyArmor
    {
        public override int LabelNumber => 1150255; // Breastplate of Villainous Epiphany

        [Constructable]
        public GargishBreastplateOfVillainousEpiphany()
        {
            Hue = 1778;
            ArmorAttributes.MageArmor = 1;
        }

        public GargishBreastplateOfVillainousEpiphany(Serial serial) : base(serial)
        {
        }

        public Alignment Alignment => Alignment.Evil;
        public SurgeType Type => SurgeType.Mana;
        public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);
        public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

        public override void AddWeightProperty(ObjectPropertyList list)
        {
            base.AddWeightProperty(list);

            EpiphanyHelper.AddProperties(this, list);
        }

        public override bool OnEquip(Mobile from)
        {
            var canEquip = base.OnEquip(from);

            if (canEquip)
                foreach (var armor in from.Items.Where(i => i is IEpiphanyArmor))
                    armor.InvalidateProperties();

            return canEquip;
        }

        public override void OnRemoved(object parent)
        {
            base.OnRemoved(parent);

            if (parent is Mobile)
            {
                var m = (Mobile)parent;

                foreach (var armor in m.Items.Where(i => i is IEpiphanyArmor)) armor.InvalidateProperties();
            }
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

    public class GargishArmsOfVillainousEpiphany : GargishPlateArms, IEpiphanyArmor
    {
        public override int LabelNumber => 1150256; // Arms of Villainous Epiphany

        [Constructable]
        public GargishArmsOfVillainousEpiphany()
        {
            Hue = 1778;
            ArmorAttributes.MageArmor = 1;
        }

        public GargishArmsOfVillainousEpiphany(Serial serial) : base(serial)
        {
        }

        public Alignment Alignment => Alignment.Evil;
        public SurgeType Type => SurgeType.Mana;
        public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);
        public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

        public override void AddWeightProperty(ObjectPropertyList list)
        {
            base.AddWeightProperty(list);

            EpiphanyHelper.AddProperties(this, list);
        }

        public override bool OnEquip(Mobile from)
        {
            var canEquip = base.OnEquip(from);

            if (canEquip)
                foreach (var armor in from.Items.Where(i => i is IEpiphanyArmor))
                    armor.InvalidateProperties();

            return canEquip;
        }

        public override void OnRemoved(object parent)
        {
            base.OnRemoved(parent);

            if (parent is Mobile)
            {
                var m = (Mobile)parent;

                foreach (var armor in m.Items.Where(i => i is IEpiphanyArmor)) armor.InvalidateProperties();
            }
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

    public class NecklaceOfVillainousEpiphany : GargishNecklace, IEpiphanyArmor
    {
        public override int LabelNumber => 1150264; // Necklace of Villainous Epiphany

        [Constructable]
        public NecklaceOfVillainousEpiphany()
        {
            Hue = 1778;
            ArmorAttributes.MageArmor = 1;
        }

        public NecklaceOfVillainousEpiphany(Serial serial) : base(serial)
        {
        }

        public Alignment Alignment => Alignment.Evil;
        public SurgeType Type => SurgeType.Mana;
        public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);
        public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

        public override void AddWeightProperty(ObjectPropertyList list)
        {
            base.AddWeightProperty(list);

            EpiphanyHelper.AddProperties(this, list);
        }

        public override bool OnEquip(Mobile from)
        {
            var canEquip = base.OnEquip(from);

            if (canEquip)
                foreach (var armor in from.Items.Where(i => i is IEpiphanyArmor))
                    armor.InvalidateProperties();

            return canEquip;
        }

        public override void OnRemoved(object parent)
        {
            base.OnRemoved(parent);

            if (parent is Mobile)
            {
                var m = (Mobile)parent;

                foreach (var armor in m.Items.Where(i => i is IEpiphanyArmor)) armor.InvalidateProperties();
            }
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

    public class GargishLegsOfVillainousEpiphany : GargishPlateLegs, IEpiphanyArmor
    {
        public override int LabelNumber => 1150258; // Legs of Villainous Epiphany

        [Constructable]
        public GargishLegsOfVillainousEpiphany()
        {
            Hue = 1778;
            ArmorAttributes.MageArmor = 1;
        }

        public GargishLegsOfVillainousEpiphany(Serial serial)
            : base(serial)
        {
        }

        public Alignment Alignment => Alignment.Evil;
        public SurgeType Type => SurgeType.Mana;
        public int Frequency => EpiphanyHelper.GetFrequency(Parent as Mobile, this);
        public int Bonus => EpiphanyHelper.GetBonus(Parent as Mobile, this);

        public override void AddWeightProperty(ObjectPropertyList list)
        {
            base.AddWeightProperty(list);

            EpiphanyHelper.AddProperties(this, list);
        }

        public override bool OnEquip(Mobile from)
        {
            var canEquip = base.OnEquip(from);

            if (canEquip)
                foreach (var armor in from.Items.Where(i => i is IEpiphanyArmor))
                    armor.InvalidateProperties();

            return canEquip;
        }

        public override void OnRemoved(object parent)
        {
            base.OnRemoved(parent);

            if (parent is Mobile)
            {
                var m = (Mobile)parent;

                foreach (var armor in m.Items.Where(i => i is IEpiphanyArmor)) armor.InvalidateProperties();
            }
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
