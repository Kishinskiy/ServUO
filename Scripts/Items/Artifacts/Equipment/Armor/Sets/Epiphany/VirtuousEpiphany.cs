using System.Linq;
using Server.Mobiles;

namespace Server.Items
{
    public class HelmOfVirtuousEpiphany : PlateHelm, IEpiphanyArmor
    {
        public override int LabelNumber => 1150233; // Helm of Virtuous Epiphany

        [Constructable]
        public HelmOfVirtuousEpiphany()
        {
            Hue = 2076;
            ArmorAttributes.MageArmor = 1;
        }

        public HelmOfVirtuousEpiphany(Serial serial) : base(serial)
        {
        }

        public Alignment Alignment => Alignment.Good;
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

    public class GorgetOfVirtuousEpiphany : PlateGorget, IEpiphanyArmor
    {
        public override int LabelNumber => 1150234; // Gorget of Virtuous Epiphany

        [Constructable]
        public GorgetOfVirtuousEpiphany()
        {
            Hue = 2076;
            ArmorAttributes.MageArmor = 1;
        }

        public GorgetOfVirtuousEpiphany(Serial serial) : base(serial)
        {
        }

        public Alignment Alignment => Alignment.Good;
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

    public class BreastplateOfVirtuousEpiphany : PlateChest, IEpiphanyArmor
    {
        public override int LabelNumber => 1150235; // Breastplate of Virtuous Epiphany

        [Constructable]
        public BreastplateOfVirtuousEpiphany()
        {
            Hue = 2076;
            ArmorAttributes.MageArmor = 1;
        }

        public BreastplateOfVirtuousEpiphany(Serial serial) : base(serial)
        {
        }

        public Alignment Alignment => Alignment.Good;
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

    public class ArmsOfVirtuousEpiphany : PlateArms, IEpiphanyArmor
    {
        public override int LabelNumber => 1150236; // Arms of Virtuous Epiphany

        [Constructable]
        public ArmsOfVirtuousEpiphany()
        {
            Hue = 2076;
            ArmorAttributes.MageArmor = 1;
        }

        public ArmsOfVirtuousEpiphany(Serial serial) : base(serial)
        {
        }

        public Alignment Alignment => Alignment.Good;
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

    public class GauntletsOfVirtuousEpiphany : PlateGloves, IEpiphanyArmor
    {
        public override int LabelNumber => 1150237; // Gauntlets of Virtuous Epiphany

        [Constructable]
        public GauntletsOfVirtuousEpiphany()
        {
            Hue = 2076;
            ArmorAttributes.MageArmor = 1;
        }

        public GauntletsOfVirtuousEpiphany(Serial serial) : base(serial)
        {
        }

        public Alignment Alignment => Alignment.Good;
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

    public class LegsOfVirtuousEpiphany : PlateLegs, IEpiphanyArmor
    {
        public override int LabelNumber => 1150238; // Leggings of Virtuous Epiphany

        [Constructable]
        public LegsOfVirtuousEpiphany()
        {
            Hue = 2076;
            ArmorAttributes.MageArmor = 1;
        }

        public LegsOfVirtuousEpiphany(Serial serial) : base(serial)
        {
        }

        public Alignment Alignment => Alignment.Good;
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

    public class KiltOfVirtuousEpiphany : GargishPlateKilt, IEpiphanyArmor
    {
        public override int LabelNumber => 1150262; // Kilt of Virtuous Epiphany

        [Constructable]
        public KiltOfVirtuousEpiphany()
        {
            Hue = 2076;
            ArmorAttributes.MageArmor = 1;
        }

        public KiltOfVirtuousEpiphany(Serial serial) : base(serial)
        {
        }

        public Alignment Alignment => Alignment.Good;
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

    public class EarringsOfVirtuousEpiphany : GargishEarrings, IEpiphanyArmor
    {
        public override int LabelNumber => 1150262; // Earrings of Virtuous Epiphany

        [Constructable]
        public EarringsOfVirtuousEpiphany()
        {
            Hue = 2076;
            ArmorAttributes.MageArmor = 1;
        }

        public EarringsOfVirtuousEpiphany(Serial serial) : base(serial)
        {
        }

        public Alignment Alignment => Alignment.Good;
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

    public class GargishBreastplateOfVirtuousEpiphany : GargishPlateChest, IEpiphanyArmor
    {
        public override int LabelNumber => 1150235; // Breastplate of Virtuous Epiphany

        [Constructable]
        public GargishBreastplateOfVirtuousEpiphany()
        {
            Hue = 2076;
        }

        public GargishBreastplateOfVirtuousEpiphany(Serial serial) : base(serial)
        {
        }

        public Alignment Alignment => Alignment.Good;
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

    public class GargishArmsOfVirtuousEpiphany : GargishPlateArms, IEpiphanyArmor
    {
        public override int LabelNumber => 1150236; // Arms of Virtuous Epiphany

        [Constructable]
        public GargishArmsOfVirtuousEpiphany()
        {
            Hue = 2076;
        }

        public GargishArmsOfVirtuousEpiphany(Serial serial) : base(serial)
        {
        }

        public Alignment Alignment => Alignment.Good;
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

    public class NecklaceOfVirtuousEpiphany : GargishNecklace, IEpiphanyArmor
    {
        public override int LabelNumber => 1150261; // Necklace of Virtuous Epiphany

        [Constructable]
        public NecklaceOfVirtuousEpiphany()
        {
            Hue = 2076;
        }

        public NecklaceOfVirtuousEpiphany(Serial serial) : base(serial)
        {
        }

        public Alignment Alignment => Alignment.Good;
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

    public class GargishLegsOfVirtuousEpiphany : GargishPlateLegs, IEpiphanyArmor
    {
        public override int LabelNumber => 1150238; // Legs of Virtuous Epiphany

        [Constructable]
        public GargishLegsOfVirtuousEpiphany()
        {
            Hue = 2076;
        }

        public GargishLegsOfVirtuousEpiphany(Serial serial)
            : base(serial)
        {
        }

        public Alignment Alignment => Alignment.Good;
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
