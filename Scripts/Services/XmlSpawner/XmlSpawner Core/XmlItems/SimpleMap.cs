namespace Server.Items
{
    public class SimpleMap : MapItem
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public int CurrentPin
        {
            // get/set the index (one-based) of the pin that will be referred to by PinLocation
            get;
            set;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int NPins
        {
            get
            {
                if (Pins != null) return Pins.Count;

                return 0;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point2D PinLocation
        {
            get
            {
                // get the coordinates of the current pin
                if (Pins != null && CurrentPin > 0 && CurrentPin <= Pins.Count)
                {
                    int mapx, mapy;
                    ConvertToWorld(Pins[CurrentPin - 1].X, Pins[CurrentPin - 1].Y, out mapx, out mapy);
                    return new Point2D(mapx, mapy);
                }

                return Point2D.Zero;
            }
            set
            {
                // change the coordinates of the current pin
                if (Pins != null && CurrentPin > 0 && CurrentPin <= Pins.Count)
                {
                    int mapx, mapy;
                    ConvertToMap(value.X, value.Y, out mapx, out mapy);
                    Pins[CurrentPin - 1] = new Point2D(mapx, mapy);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point2D NewPin
        {
            get
            {
                // return the last pin added to the Pins arraylist
                if (Pins != null && NPins > 0)
                {
                    int mapx, mapy;
                    ConvertToWorld(Pins[NPins - 1].X, Pins[NPins - 1].Y, out mapx, out mapy);
                    return new Point2D(mapx, mapy);
                }

                return Point2D.Zero;
            }
            set
            {
                // add a new pin at the specified world coordinate
                AddWorldPin(value.X, value.Y);
                CurrentPin = NPins;
            }
        }


        [CommandProperty(AccessLevel.GameMaster)]
        public bool ClearAllPins
        {
            get => false;
            set
            {
                if (value) ClearPins();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PinRemove
        {
            get => 0;
            set => RemovePin(value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile ShowTo
        {
            get => null;
            set
            {
                if (value != null)
                    //DisplayTo(value);
                    OnDoubleClick(value);
            }
        }

        public override int LabelNumber => 1025355; // map


        [Constructable]
        public SimpleMap()
        {
            SetDisplay(0, 0, 5119, 4095, 400, 400);
        }

        public SimpleMap(Serial serial) : base(serial)
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
