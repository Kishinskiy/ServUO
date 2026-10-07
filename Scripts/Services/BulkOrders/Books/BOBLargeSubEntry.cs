using System;

namespace Server.Engines.BulkOrders
{
    public class BOBLargeSubEntry
    {
        public Type ItemType { get; }

        public int AmountCur { get; }

        public int Number { get; }

        public int Graphic { get; }

        public int Hue { get; }

        public BOBLargeSubEntry(LargeBulkEntry lbe)
        {
            ItemType = lbe.Details.Type;
            AmountCur = lbe.Amount;
            Number = lbe.Details.Number;
            Graphic = lbe.Details.Graphic;
            Hue = lbe.Details.Hue;
        }

        public BOBLargeSubEntry(GenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            switch (version)
            {
                case 1:
                {
                    Hue = reader.ReadEncodedInt();
                    goto case 0;
                }
                case 0:
                {
                    var type = reader.ReadString();

                    if (type != null)
                        ItemType = ScriptCompiler.FindTypeByFullName(type);

                    AmountCur = reader.ReadEncodedInt();
                    Number = reader.ReadEncodedInt();
                    Graphic = reader.ReadEncodedInt();

                    break;
                }
            }
        }

        public void Serialize(GenericWriter writer)
        {
            writer.WriteEncodedInt(1); // version

            writer.WriteEncodedInt(Hue);

            writer.Write(ItemType == null ? null : ItemType.FullName);

            writer.WriteEncodedInt(AmountCur);
            writer.WriteEncodedInt(Number);
            writer.WriteEncodedInt(Graphic);
        }
    }
}
