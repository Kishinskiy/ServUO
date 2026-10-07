using Server.Mobiles;

namespace Server.Items
{
    public class DeathCertificate : Item
    {
        public override int LabelNumber => 1116716;

        [CommandProperty(AccessLevel.GameMaster)]
        public string Owner { get; private set; }

        public DeathCertificate(Mobile owner)
            : base(0x14F0)
        {
            if (owner is PirateCaptain)
            {
                var capt = (PirateCaptain)owner;

                if (capt.PirateName > 0)
                    Owner = string.Format("#{0}\t#{1}\t#{2}", capt.Adjective, capt.Noun, capt.PirateName);
                else
                    Owner = string.Format("#{0}\t#{1}\t{2}", capt.Adjective, capt.Noun, Name);
            }
        }

        public DeathCertificate(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (Owner != null) list.Add(1116690, Owner);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
            writer.Write(Owner);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
            Owner = reader.ReadString();
        }
    }
}
