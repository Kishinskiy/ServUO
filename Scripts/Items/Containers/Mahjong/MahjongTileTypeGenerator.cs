using System.Collections;

namespace Server.Engines.Mahjong
{
    public class MahjongTileTypeGenerator
    {
        public ArrayList LeftTileTypes { get; }

        public MahjongTileTypeGenerator(int count)
        {
            LeftTileTypes = new ArrayList(34 * count);

            for (var i = 1; i <= 34; i++)
            for (var j = 0; j < count; j++)
                LeftTileTypes.Add((MahjongTileType)i);
        }

        public MahjongTileType Next()
        {
            var random = Utility.Random(LeftTileTypes.Count);
            var next = (MahjongTileType)LeftTileTypes[random];
            LeftTileTypes.RemoveAt(random);

            return next;
        }
    }
}
