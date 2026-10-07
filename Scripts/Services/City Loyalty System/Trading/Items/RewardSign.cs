namespace Server.Items
{
    public class RewardSign : Sign, IEngravable
    {
        [Constructable]
        public RewardSign() : base((SignType)Utility.RandomMinMax(31, 58),
            Utility.RandomBool() ? SignFacing.North : SignFacing.West)
        {
            Movable = true;
        }

        public RewardSign(Serial serial)
            : base(serial)
        {
        }

        public string EngravedText { get; set; }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (!string.IsNullOrEmpty(EngravedText)) list.Add(1072305, EngravedText); // Engraved: ~1_INSCRIPTION~
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);

            writer.Write(EngravedText);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            EngravedText = reader.ReadString();
        }
    }
}
