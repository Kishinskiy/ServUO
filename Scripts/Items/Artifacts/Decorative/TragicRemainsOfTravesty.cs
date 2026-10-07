namespace Server.Items
{
    public class TragicRemainsOfTravesty : BaseStatuette
    {
        private static readonly int[] m_Sounds =
        {
            0x314, 0x315, 0x316, 0x317 // TODO check
        };

        public override bool IsArtifact => true;

        public override int LabelNumber => 1074500; // Tragic Remains of the Travesty

        [Constructable]
        public TragicRemainsOfTravesty()
            : base(Utility.Random(0x122A, 6))
        {
            Weight = 1.0;
            Hue = Utility.RandomList(0x11E, 0x846);
        }

        public TragicRemainsOfTravesty(Serial serial)
            : base(serial)
        {
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (TurnedOn && IsLockedDown && (!m.Hidden || m.IsPlayer()) && Utility.InRange(m.Location, Location, 2) &&
                !Utility.InRange(oldLocation, Location, 2))
                Effects.PlaySound(Location, Map, m_Sounds[Utility.Random(m_Sounds.Length)]);

            base.OnMovement(m, oldLocation);
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
