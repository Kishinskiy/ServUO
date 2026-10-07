using System;
using System.Collections.Generic;

namespace Server.Engines.CityLoyalty
{
    [PropertyObject]
    public class TradeEntry
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public City Origin { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public City Destination { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Kills { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Distance { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastAmbush { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string TurnIn
        {
            get
            {
                var str = "";
                Details.ForEach(d => { str += d.ItemType.Name + ", "; });
                return str;
            }
        }

        public List<TradeDetails> Details { get; set; }

        public TradeEntry(City destination, City origin, int distance)
        {
            Origin = origin;
            Destination = destination;
            Distance = distance;

            Details = new List<TradeDetails>();
        }

        public TradeEntry(GenericReader reader)
        {
            var version = reader.ReadInt();

            Origin = (City)reader.ReadInt();
            Destination = (City)reader.ReadInt();

            LastAmbush = reader.ReadDateTime();

            Kills = reader.ReadInt();
            Distance = reader.ReadInt();

            Details = new List<TradeDetails>();

            var count = reader.ReadInt();
            for (var i = 0; i < count; i++) Details.Add(new TradeDetails(reader));
        }

        public override string ToString()
        {
            return "...";
        }

        public int CalculateGold()
        {
            if (Distance == 0)
                return 0;

            var gold = 0;

            Details.ForEach(d => { gold += d.Worth * 3; });

            return Math.Max(CityTradeSystem.TurnInGold, gold + Distance * 10 + Math.Min(25000, Kills * 500));
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write(0);

            writer.Write((int)Origin);
            writer.Write((int)Destination);

            writer.Write(LastAmbush);

            writer.Write(Kills);
            writer.Write(Distance);

            writer.Write(Details.Count);
            Details.ForEach(d => d.Serialize(writer));
        }

        public class TradeDetails
        {
            public Type ItemType { get; set; }
            public int Amount { get; set; }
            public int Worth { get; set; }
            public string Name { get; set; }

            public TradeDetails(Type type, int worth, int amount, string fallbackname)
            {
                ItemType = type;
                Worth = worth;
                Amount = amount;

                Name = CityLoyaltySystem.CityTrading.GetNameFor(type, fallbackname);
            }

            public TradeDetails(GenericReader reader)
            {
                var version = reader.ReadInt();

                ItemType = ScriptCompiler.FindTypeByName(reader.ReadString());
                Worth = reader.ReadInt();
                Amount = reader.ReadInt();
                Name = reader.ReadString();
            }

            public void Serialize(GenericWriter writer)
            {
                writer.Write(0);

                writer.Write(ItemType.Name);
                writer.Write(Worth);
                writer.Write(Amount);
                writer.Write(Name);
            }
        }
    }
}
