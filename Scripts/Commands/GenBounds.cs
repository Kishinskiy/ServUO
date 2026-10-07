using System;
using System.IO;
using Server.Commands;
using Ultima;

namespace Server.Bounds
{
    public class Bounds
    {
        public static void Initialize()
        {
            CommandSystem.Register("GenBounds", AccessLevel.Administrator, GenBounds_OnCommand);
        }

        [Usage("GenBounds")]
        [Description("GenBounds")]
        public static void GenBounds_OnCommand(CommandEventArgs e)
        {
            if (Files.MulPath["artlegacymul.uop"] != null ||
                (Files.MulPath["art.mul"] != null && Files.MulPath["artidx.mul"] != null))
            {
                Utility.PushColor(ConsoleColor.Yellow);
                Console.Write("Generating Bounds.bin...");
                Utility.PopColor();

                var fs = new FileStream("Data/Binary/Bounds.bin", FileMode.Create, FileAccess.Write);

                var bin = new BinaryWriter(fs);

                int xMin, yMin, xMax, yMax;

                for (var i = 0; i < Art.GetMaxItemID(); ++i)
                {
                    Art.Measure(Item.GetBitmap(i), out xMin, out yMin, out xMax, out yMax);

                    bin.Write((ushort)xMin);
                    bin.Write((ushort)yMin);
                    bin.Write((ushort)xMax);
                    bin.Write((ushort)yMax);
                }

                Utility.PushColor(ConsoleColor.Green);
                Console.WriteLine("done");
                Utility.PopColor();
                bin.Close();
            }
            else
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine("Art files missing.");
                Utility.PopColor();
            }
        }
    }
}
