namespace Server.Mobiles
{
    public class CapturedHordeMinion : HordeMinion
    {
        public override bool InitialInnocent => true;

        [Constructable]
        public CapturedHordeMinion()
        {
            FightMode = FightMode.None;
        }

        public CapturedHordeMinion(Serial serial)
            : base(serial)
        {
        }

        public override bool CanBeDamaged()
        {
            return false;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
