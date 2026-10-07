namespace Server.Custom.PowerScrollsAdditional
{
    public class MageryMythicalScroll : BaseAdditionalPowerScroll
    {
        [Constructable]
        public MageryMythicalScroll(int value) : base(SkillName.Magery, value)
        {
            Name = $"Мифический Свиток: Магия (+{value})";
        }

        public MageryMythicalScroll(Serial serial) : base(serial)
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
