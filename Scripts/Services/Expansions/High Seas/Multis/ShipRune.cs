using Server.Multis;

namespace Server.Items
{
    // This item is no longer used.
    [FlipableAttribute(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    public class ShipRune : Item
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public BaseGalleon Galleon { get; set; }

        public BaseDockedBoat DockedBoat
        {
            get
            {
                if (Galleon != null)
                    return Galleon.BoatItem;

                return null;
            }
        }

        public override int LabelNumber => 1149570; //A Ship Rune

        [Constructable]
        public ShipRune() : this(null)
        {
        }

        [Constructable]
        public ShipRune(BaseGalleon galleon) : base(0x1F14)
        {
            Galleon = galleon;
            Hue = 0x47F;
        }

        public ShipRune(Serial serial) : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
            }
            else
            {
                if (Galleon != null)
                {
                    var rune = new RecallRune();
                    rune.SetGalleon(Galleon);
                    from.AddToBackpack(rune);
                }

                Delete();
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (Galleon != null)
            {
                list.Add(1149571, Galleon.Owner != null ? Galleon.Owner.Name : "Nobody"); //Owner: ~1_NAME~

                if (Galleon.ShipName != null)
                    list.Add(1149572, Galleon.ShipName); //Name: the ~1_NAME~
                else list.Add(1149573); //Name: the Unnamed Ship

                if (Galleon.Map != Map.Internal)
                {
                    list.Add(1149574, Galleon.Map != null ? Galleon.Map.ToString() : "Unknown"); //Location: ~1_FACET~
                }
                else
                {
                    var name = Galleon.ShipName != null && Galleon.ShipName != "" ? Galleon.ShipName : "Unnamed Ship";
                    list.Add(1041644, name); //The ~1_VAL~ (Dry Docked)
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);
            writer.Write(Galleon);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
            Galleon = reader.ReadItem() as BaseGalleon;
            if (version == 0)
                reader.ReadItem();
        }
    }
}
