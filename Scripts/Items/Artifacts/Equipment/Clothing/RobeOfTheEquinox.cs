namespace Server.Items
{
    [Flipable(0x1F03, 0x1F04)]
    public class RobeOfTheEquinox : BaseOuterTorso, ICanBeElfOrHuman
    {
        private bool _ElfOnly;
        public override bool IsArtifact => true;
        public override Race RequiredRace => _ElfOnly ? Race.Elf : null;

        public override int LabelNumber => 1075042; // Robe of the Equinox


        [Constructable]
        public RobeOfTheEquinox()
            : base(0x1F04, 0xD6)
        {
            Weight = 3.0;
            _ElfOnly = true;
            Attributes.Luck = 95;
        }

        public RobeOfTheEquinox(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ElfOnly
        {
            get => _ElfOnly;
            set
            {
                _ElfOnly = value;
                InvalidateProperties();
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(1); // version

            writer.Write(_ElfOnly);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            switch (version)
            {
                case 1:
                    _ElfOnly = reader.ReadBool();
                    break;
                case 0:
                    _ElfOnly = true;
                    break;
            }
        }
    }
}
