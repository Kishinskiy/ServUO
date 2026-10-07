using System;

namespace Server.Items
{
    public class GargoyleCandelabra : BaseLight, IShipwreckedItem
    {
        public override int LitItemID => 0x40BE;

        public override int UnlitItemID => 0x4039;

        [Constructable]
        public GargoyleCandelabra()
            : base(0x4039)
        {
            Duration = TimeSpan.Zero; // Never burnt out
            Burning = false;
            Light = LightType.Circle225;
            Weight = 3.0;
        }

        public GargoyleCandelabra(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);

            writer.Write(IsShipwreckedItem);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                {
                    IsShipwreckedItem = reader.ReadBool();
                    break;
                }
            }
        }

        public override void AddNameProperties(ObjectPropertyList list)
        {
            base.AddNameProperties(list);

            if (IsShipwreckedItem)
                list.Add(1041645); // recovered from a shipwreck
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            LabelTo(from, 1041645); //recovered from a shipwreck
        }

        #region IShipwreckedItem Members

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsShipwreckedItem { get; set; }

        #endregion
    }
}
