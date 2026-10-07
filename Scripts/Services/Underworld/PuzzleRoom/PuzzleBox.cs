using System;
using Server.Mobiles;

namespace Server.Items
{
    public enum PuzzleType
    {
        WestBox, // Maze #1
        EastBox, // Maze #2
        NorthBox // Mastermind
    }

    public class PuzzleBox : Item
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public PuzzleType PuzzleType { get; set; }

        public override int LabelNumber => 1113486; // a puzzle box

        [Constructable]
        public PuzzleBox(PuzzleType type) : base(2472)
        {
            PuzzleType = type;
            Movable = false;
        }

        public PuzzleBox(Serial serial) : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(Location, 3))
            {
                from.SendLocalizedMessage(3000268); // that is too far away.
            }
            else if (from.Backpack != null)
            {
                Type needed;
                Item puzzle = null;

                var key = from.Backpack.FindItemByType(typeof(MagicKey));
                var puzzleItem1 = from.Backpack.FindItemByType(typeof(MazePuzzleItem));
                var puzzleItem2 = from.Backpack.FindItemByType(typeof(MastermindPuzzleItem));

                if (key == null)
                {
                    var x = Utility.RandomMinMax(1095, 1099);
                    var y = Utility.RandomMinMax(1178, 1179);
                    var z = -1;

                    var loc = from.Location;
                    var p = new Point3D(x, y, z);
                    BaseCreature.TeleportPets(from, p, Map.TerMur);
                    from.MoveToWorld(p, Map.TerMur);

                    from.PlaySound(0x1FE);
                    Effects.SendLocationParticles(EffectItem.Create(loc, from.Map, EffectItem.DefaultDuration), 0x3728,
                        10, 10, 2023);
                    Effects.SendLocationParticles(EffectItem.Create(p, from.Map, EffectItem.DefaultDuration), 0x3728,
                        10, 10, 5023);
                }

                if (puzzleItem1 != null || puzzleItem2 != null)
                {
                    from.SendMessage("You already have a puzzle board.");
                    return;
                }

                switch (PuzzleType)
                {
                    default:
                    case PuzzleType.WestBox:
                    case PuzzleType.EastBox: needed = typeof(MagicKey); break;
                    case PuzzleType.NorthBox: needed = typeof(GoldPuzzleKey); break;
                }

                var item = from.Backpack.FindItemByType(needed);

                if (item != null && key is MagicKey)
                {
                    if (PuzzleType == PuzzleType.NorthBox)
                        puzzle = new MastermindPuzzleItem((MagicKey)key);
                    else
                        puzzle = new MazePuzzleItem((MagicKey)key);
                }

                if (puzzle != null)
                {
                    if (!from.Backpack.TryDropItem(from, puzzle, true))
                        puzzle.Delete();
                    else
                        from.SendMessage("You recieve a puzzle board.");
                }
                else
                {
                    from.SendMessage("You do not have the required key to get that puzzle board.");
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // ver
            writer.Write((int)PuzzleType);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
            PuzzleType = (PuzzleType)reader.ReadInt();
        }
    }
}
