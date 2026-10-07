using Server.Multis;

namespace Server.Items
{
    public class SerpentPillar : Item
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Word { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Rectangle2D Destination { get; set; }

        public override bool HandlesOnSpeech => true;

        [Constructable]
        public SerpentPillar()
            : this(null, new Rectangle2D(), false)
        {
        }

        public SerpentPillar(string word, Rectangle2D destination)
            : this(word, destination, true)
        {
        }

        public SerpentPillar(string word, Rectangle2D destination, bool active)
            : base(0x233F)
        {
            Movable = false;

            Active = active;
            Word = word;
            Destination = destination;
        }

        public SerpentPillar(Serial serial)
            : base(serial)
        {
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            var from = e.Mobile;

            if (!e.Handled && from.InRange(this, 10) && e.Speech.ToLower() == Word)
            {
                var boat = BaseBoat.FindBoatAt(from, from.Map);

                if (boat == null)
                    return;

                if (!Active)
                {
                    if (boat.TillerMan != null)
                        boat.TillerManSay(
                            502507); // Ar, Legend has it that these pillars are inactive! No man knows how it might be undone!

                    return;
                }

                var map = from.Map;

                for (var i = 0; i < 5; i++) // Try 5 times
                {
                    var x = Utility.Random(Destination.X, Destination.Width);
                    var y = Utility.Random(Destination.Y, Destination.Height);
                    var z = map.GetAverageZ(x, y);

                    var dest = new Point3D(x, y, z);

                    if (boat.CanFit(dest, map, boat.ItemID))
                    {
                        var xOffset = x - boat.X;
                        var yOffset = y - boat.Y;
                        var zOffset = z - boat.Z;

                        boat.Teleport(xOffset, yOffset, zOffset);

                        return;
                    }
                }

                if (boat.TillerMan != null)
                    boat.TillerManSay(502508); // Ar, I refuse to take that matey through here!
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write(Active);
            writer.Write(Word);
            writer.Write(Destination);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            Active = reader.ReadBool();
            Word = reader.ReadString();
            Destination = reader.ReadRect2D();
        }
    }
}
