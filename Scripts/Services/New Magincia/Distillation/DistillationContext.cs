using System.IO;
using Server.Items;

namespace Server.Engines.Distillation
{
    public class DistillationContext
    {
        public Group LastGroup { get; set; }

        public Liquor LastLiquor { get; set; }

        public Yeast[] SelectedYeast { get; } = new Yeast[4];

        public bool MakeStrong { get; set; }

        public bool Mark { get; set; }

        public string Label { get; set; }

        public DistillationContext()
        {
            LastGroup = Group.WheatBased;
            LastLiquor = Liquor.None;
            MakeStrong = false;
            Mark = true;
            Label = null;
        }

        public DistillationContext(GenericReader reader)
        {
            var version = reader.ReadInt();

            LastGroup = (Group)reader.ReadInt();
            LastLiquor = (Liquor)reader.ReadInt();
            MakeStrong = reader.ReadBool();
            Mark = reader.ReadBool();
            Label = reader.ReadString();
        }

        public bool YeastInUse(Yeast yeast)
        {
            foreach (var y in SelectedYeast)
                if (y != null && y == yeast)
                    return true;

            return false;
        }

        public void ClearYeasts()
        {
            for (var i = 0; i < SelectedYeast.Length; i++) SelectedYeast[i] = null;
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write(0);

            writer.Write((int)LastGroup);
            writer.Write((int)LastLiquor);
            writer.Write(MakeStrong);
            writer.Write(Mark);
            writer.Write(Label);
        }

        #region Serialize/Deserialize Persistence

        private static readonly string FilePath = Path.Combine("Saves", "CraftContext", "DistillationContexts.bin");

        public static void Configure()
        {
            EventSink.WorldSave += OnSave;
            EventSink.WorldLoad += OnLoad;
        }

        public static void OnSave(WorldSaveEventArgs e)
        {
            Persistence.Serialize(
                FilePath,
                writer =>
                {
                    writer.Write(0); // version

                    writer.Write(DistillationSystem.Contexts.Count);

                    foreach (var kvp in DistillationSystem.Contexts)
                    {
                        writer.Write(kvp.Key);
                        kvp.Value.Serialize(writer);
                    }
                });
        }

        public static void OnLoad()
        {
            Persistence.Deserialize(
                FilePath,
                reader =>
                {
                    var version = reader.ReadInt();

                    var count = reader.ReadInt();
                    for (var i = 0; i < count; i++)
                    {
                        var m = reader.ReadMobile();
                        var context = new DistillationContext(reader);

                        if (m != null)
                            DistillationSystem.Contexts[m] = context;
                    }
                });
        }

        #endregion
    }
}
