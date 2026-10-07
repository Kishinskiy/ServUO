using Server.Items;

namespace Server.Engines.BulkOrders
{
    public class BOBLargeEntry
    {
        public bool RequireExceptional { get; }

        public BODType DeedType { get; }

        public BulkMaterialType Material { get; }

        public int AmountMax { get; }

        public int Price { get; set; }

        public GemType GemType { get; set; }

        public BOBLargeSubEntry[] Entries { get; }

        public BOBLargeEntry(LargeBOD bod)
        {
            RequireExceptional = bod.RequireExceptional;

            DeedType = bod.BODType;

            Material = bod.Material;
            AmountMax = bod.AmountMax;

            Entries = new BOBLargeSubEntry[bod.Entries.Length];

            for (var i = 0; i < Entries.Length; ++i)
                Entries[i] = new BOBLargeSubEntry(bod.Entries[i]);

            if (bod is LargeTinkerBOD) GemType = ((LargeTinkerBOD)bod).GemType;
        }

        public BOBLargeEntry(GenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            switch (version)
            {
                case 1:
                {
                    GemType = (GemType)reader.ReadInt();
                    goto case 0;
                }
                case 0:
                {
                    RequireExceptional = reader.ReadBool();

                    DeedType = (BODType)reader.ReadEncodedInt();

                    Material = (BulkMaterialType)reader.ReadEncodedInt();
                    AmountMax = reader.ReadEncodedInt();
                    Price = reader.ReadEncodedInt();

                    Entries = new BOBLargeSubEntry[reader.ReadEncodedInt()];

                    for (var i = 0; i < Entries.Length; ++i)
                        Entries[i] = new BOBLargeSubEntry(reader);

                    break;
                }
            }
        }

        public Item Reconstruct()
        {
            LargeBOD bod = null;

            switch (DeedType)
            {
                case BODType.Smith:
                    bod = new LargeSmithBOD(AmountMax, RequireExceptional, Material, ReconstructEntries()); break;
                case BODType.Tailor:
                    bod = new LargeTailorBOD(AmountMax, RequireExceptional, Material, ReconstructEntries()); break;
                case BODType.Inscription:
                    bod = new LargeInscriptionBOD(AmountMax, RequireExceptional, Material, ReconstructEntries()); break;
                case BODType.Alchemy:
                    bod = new LargeAlchemyBOD(AmountMax, RequireExceptional, Material, ReconstructEntries()); break;
                case BODType.Carpentry:
                    bod = new LargeCarpentryBOD(AmountMax, RequireExceptional, Material, ReconstructEntries()); break;
                case BODType.Fletching:
                    bod = new LargeFletchingBOD(AmountMax, RequireExceptional, Material, ReconstructEntries()); break;
                case BODType.Tinkering:
                    bod = new LargeTinkerBOD(AmountMax, RequireExceptional, Material, ReconstructEntries(),
                        GemType); break;
                case BODType.Cooking:
                    bod = new LargeCookingBOD(AmountMax, RequireExceptional, Material, ReconstructEntries()); break;
            }

            for (var i = 0; bod != null && i < bod.Entries.Length; ++i)
                bod.Entries[i].Owner = bod;

            return bod;
        }

        public void Serialize(GenericWriter writer)
        {
            writer.WriteEncodedInt(1); // version

            writer.Write((int)GemType);

            writer.Write(RequireExceptional);

            writer.WriteEncodedInt((int)DeedType);
            writer.WriteEncodedInt((int)Material);
            writer.WriteEncodedInt(AmountMax);
            writer.WriteEncodedInt(Price);

            writer.WriteEncodedInt(Entries.Length);

            for (var i = 0; i < Entries.Length; ++i)
                Entries[i].Serialize(writer);
        }

        private LargeBulkEntry[] ReconstructEntries()
        {
            var entries = new LargeBulkEntry[Entries.Length];

            for (var i = 0; i < Entries.Length; ++i)
            {
                entries[i] = new LargeBulkEntry(null,
                    new SmallBulkEntry(Entries[i].ItemType, Entries[i].Number, Entries[i].Graphic, Entries[i].Hue));
                entries[i].Amount = Entries[i].AmountCur;
            }

            return entries;
        }
    }
}
