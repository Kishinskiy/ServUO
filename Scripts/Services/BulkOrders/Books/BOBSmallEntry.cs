using System;
using Server.Items;

namespace Server.Engines.BulkOrders
{
    public class BOBSmallEntry
    {
        public Type ItemType { get; }

        public bool RequireExceptional { get; }

        public BODType DeedType { get; }

        public BulkMaterialType Material { get; }

        public int AmountCur { get; }

        public int AmountMax { get; }

        public int Number { get; }

        public int Graphic { get; }

        public int Hue { get; }

        public GemType GemType { get; }

        public int Price { get; set; }

        public BOBSmallEntry(SmallBOD bod)
        {
            ItemType = bod.Type;
            RequireExceptional = bod.RequireExceptional;

            DeedType = bod.BODType;

            Material = bod.Material;
            AmountCur = bod.AmountCur;
            AmountMax = bod.AmountMax;
            Number = bod.Number;
            Graphic = bod.Graphic;
            Hue = bod.GraphicHue;

            if (bod is SmallTinkerBOD) GemType = ((SmallTinkerBOD)bod).GemType;
        }

        public BOBSmallEntry(GenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            switch (version)
            {
                case 2:
                {
                    GemType = (GemType)reader.ReadInt();
                    goto case 1;
                }
                case 1:
                {
                    Hue = reader.ReadInt();
                    goto case 0;
                }
                case 0:
                {
                    var type = reader.ReadString();

                    if (type != null)
                        ItemType = ScriptCompiler.FindTypeByFullName(type);

                    RequireExceptional = reader.ReadBool();

                    DeedType = (BODType)reader.ReadEncodedInt();

                    Material = (BulkMaterialType)reader.ReadEncodedInt();
                    AmountCur = reader.ReadEncodedInt();
                    AmountMax = reader.ReadEncodedInt();
                    Number = reader.ReadEncodedInt();
                    Graphic = reader.ReadEncodedInt();
                    Price = reader.ReadEncodedInt();

                    break;
                }
            }
        }

        public Item Reconstruct()
        {
            SmallBOD bod = null;

            switch (DeedType)
            {
                case BODType.Smith:
                    bod = new SmallSmithBOD(AmountCur, AmountMax, ItemType, Number, Graphic, RequireExceptional,
                        Material, Hue); break;
                case BODType.Tailor:
                    bod = new SmallTailorBOD(AmountCur, AmountMax, ItemType, Number, Graphic, RequireExceptional,
                        Material, Hue); break;
                case BODType.Inscription:
                    bod = new SmallInscriptionBOD(AmountCur, AmountMax, ItemType, Number, Graphic, RequireExceptional,
                        Material, Hue); break;
                case BODType.Alchemy:
                    bod = new SmallAlchemyBOD(AmountCur, AmountMax, ItemType, Number, Graphic, RequireExceptional,
                        Material, Hue); break;
                case BODType.Carpentry:
                    bod = new SmallCarpentryBOD(AmountCur, AmountMax, ItemType, Number, Graphic, RequireExceptional,
                        Material, Hue); break;
                case BODType.Fletching:
                    bod = new SmallFletchingBOD(AmountCur, AmountMax, ItemType, Number, Graphic, RequireExceptional,
                        Material, Hue); break;
                case BODType.Tinkering:
                    bod = new SmallTinkerBOD(AmountCur, AmountMax, ItemType, Number, Graphic, RequireExceptional,
                        Material, Hue, GemType); break;
                case BODType.Cooking:
                    bod = new SmallCookingBOD(AmountCur, AmountMax, ItemType, Number, Graphic, RequireExceptional,
                        Material, Hue); break;
            }

            return bod;
        }

        public void Serialize(GenericWriter writer)
        {
            writer.WriteEncodedInt(2); // version

            writer.Write((int)GemType);

            writer.Write(Hue);

            writer.Write(ItemType == null ? null : ItemType.FullName);

            writer.Write(RequireExceptional);

            writer.WriteEncodedInt((int)DeedType);
            writer.WriteEncodedInt((int)Material);
            writer.WriteEncodedInt(AmountCur);
            writer.WriteEncodedInt(AmountMax);
            writer.WriteEncodedInt(Number);
            writer.WriteEncodedInt(Graphic);
            writer.WriteEncodedInt(Price);
        }
    }
}
