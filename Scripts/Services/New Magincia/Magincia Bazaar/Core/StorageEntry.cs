using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Engines.NewMagincia
{
    public class StorageEntry
    {
        public int Funds { get; set; }

        public DateTime Expires { get; private set; }

        public Dictionary<Type, int> CommodityTypes { get; } = new Dictionary<Type, int>();

        public List<BaseCreature> Creatures { get; } = new List<BaseCreature>();

        public StorageEntry(Mobile m, BaseBazaarBroker broker)
        {
            AddInventory(m, broker);
        }

        public StorageEntry(GenericReader reader)
        {
            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    Funds = reader.ReadInt();
                    Expires = reader.ReadDateTime();

                    var count = reader.ReadInt();
                    for (var i = 0; i < count; i++)
                    {
                        var cType = ScriptCompiler.FindTypeByName(reader.ReadString());
                        var amount = reader.ReadInt();

                        if (cType != null)
                            CommodityTypes[cType] = amount;
                    }

                    count = reader.ReadInt();
                    for (var i = 0; i < count; i++)
                    {
                        var bc = reader.ReadMobile() as BaseCreature;

                        if (bc != null)
                            Creatures.Add(bc);
                    }

                    break;
                case 0:
                    var type = reader.ReadInt();
                    Funds = reader.ReadInt();
                    Expires = reader.ReadDateTime();

                    switch (type)
                    {
                        case 0: break;
                        case 1:
                        {
                            var c1 = reader.ReadInt();
                            for (var i = 0; i < c1; i++)
                            {
                                var cType = ScriptCompiler.FindTypeByName(reader.ReadString());
                                var amount = reader.ReadInt();

                                if (cType != null)
                                    CommodityTypes[cType] = amount;
                            }

                            break;
                        }
                        case 2:
                        {
                            var c2 = reader.ReadInt();
                            for (var i = 0; i < c2; i++)
                            {
                                var bc = reader.ReadMobile() as BaseCreature;

                                if (bc != null) Creatures.Add(bc);
                            }

                            break;
                        }
                    }

                    break;
            }
        }

        public void AddInventory(Mobile m, BaseBazaarBroker broker)
        {
            Funds += broker.BankBalance;
            Expires = DateTime.UtcNow + TimeSpan.FromDays(7);

            if (broker is CommodityBroker)
            {
                foreach (var entry in ((CommodityBroker)broker).CommodityEntries)
                    if (entry.Stock > 0)
                        CommodityTypes[entry.CommodityType] = entry.Stock;
            }
            else if (broker is PetBroker)
            {
                foreach (var entry in ((PetBroker)broker).BrokerEntries)
                {
                    if (entry.Pet.Map != Map.Internal || !entry.Pet.IsStabled) entry.Internalize();

                    Creatures.Add(entry.Pet);
                }
            }
        }

        public void RemoveCommodity(Type type, int amount)
        {
            if (CommodityTypes.ContainsKey(type))
            {
                CommodityTypes[type] -= amount;

                if (CommodityTypes[type] <= 0)
                    CommodityTypes.Remove(type);
            }
        }

        public void RemovePet(BaseCreature pet)
        {
            if (Creatures.Contains(pet))
                Creatures.Remove(pet);
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write(1);

            writer.Write(Funds);
            writer.Write(Expires);

            writer.Write(CommodityTypes.Count);
            foreach (var kvp in CommodityTypes)
            {
                writer.Write(kvp.Key.Name);
                writer.Write(kvp.Value);
            }

            writer.Write(Creatures.Count);
            foreach (var bc in Creatures) writer.Write(bc);
        }
    }
}
