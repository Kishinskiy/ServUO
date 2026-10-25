using System;
using Server;
using Server.Items;
using Server.Accounting;

namespace Server.Items
{
    public enum ContractType
    {
        Bronze,
        Silver,
        Gold
    }

    public class SovereignContract : Item
    {
        private ContractType _contractType;

        [CommandProperty(AccessLevel.GameMaster)]
        public ContractType ContractType
        {
            get { return _contractType; }
            set 
            { 
                _contractType = value; 
                UpdateProperties(); 
                InvalidateProperties();
            }
        }

        public int SovereignAmount
        {
            get
            {
                switch (_contractType)
                {
                    case ContractType.Bronze: return 3000;
                    case ContractType.Silver: return 5000;
                    case ContractType.Gold: return 8000;
                    default: return 0;
                }
            }
        }

        [Constructable]
        public SovereignContract() : this(ContractType.Bronze)
        {
        }

        [Constructable]
        public SovereignContract(ContractType type) : base(0x14F0) // Графика Deed
        {
            Weight = 0.1;
            LootType = LootType.Regular; // Как у баклера, выпадает при смерти
            ContractType = type;
        }

        public SovereignContract(Serial serial) : base(serial)
        {
        }

        private void UpdateProperties()
        {
            switch (_contractType)
            {
                case ContractType.Bronze:
                    Hue = 2207;
                    Name = "Бронзовый контракт на северены";
                    break;
                case Server.Items.ContractType.Silver:
                    Hue = 2101;
                    Name = "Серебряный контракт на северены";
                    break;
                case Server.Items.ContractType.Gold:
                    Hue = 2213;
                    Name = "Золотой контракт на северены";
                    break;
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add($"Номинал: {SovereignAmount} Sovereigns");
            list.Add("Скажите банкиру 'bank deposit sovereigns' для обмена");
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // Версия

            writer.Write((int)_contractType);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            _contractType = (ContractType)reader.ReadInt();
            UpdateProperties();
        }
    }
}
