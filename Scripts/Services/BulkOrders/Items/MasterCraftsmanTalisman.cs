namespace Server.Items
{
    public class MasterCraftsmanTalisman : BaseTalisman
    {
        private int _Type;
        public override bool IsArtifact => true;
        public virtual int Type => _Type;

        public override int LabelNumber => 1157217; // MasterCraftsmanTalisman
        public override bool ForceShowName => true;

        [Constructable]
        public MasterCraftsmanTalisman(int charges, int itemID, TalismanSkill skill)
            : base(itemID)
        {
            Skill = skill;

            SuccessBonus = GetRandomSuccessful();
            ExceptionalBonus = GetRandomExceptional();
            Blessed = GetRandomBlessed();

            _Type = charges;
            Charges = charges;
        }

        public MasterCraftsmanTalisman(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1157213); // Crafting Failure Protection

            if (Charges > 0)
                list.Add(1049116, Charges.ToString()); // [ Charges: ~1_CHARGES~ ]
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.Write(_Type);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    _Type = reader.ReadInt();
                    break;
                case 0:
                    _Type = 10;
                    break;
            }
        }
    }
}
