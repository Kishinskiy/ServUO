using System;

namespace Server.Factions
{
    [TypeAlias("Server.Factions.FactionPersistance")]
    public class FactionPersistence : Item
    {
        public static FactionPersistence Instance { get; private set; }

        public override string DefaultName => "Faction Persistance - Internal";

        public FactionPersistence()
            : base(1)
        {
            Movable = false;

            if (Instance == null || Instance.Deleted)
                Instance = this;
            else
                base.Delete();
        }

        public FactionPersistence(Serial serial)
            : base(serial)
        {
            Instance = this;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // Version 0 removes faction items, ie monoliths, stones, etc
            writer.Write(1); // version

            var factions = Faction.Factions;

            for (var i = 0; i < factions.Count; ++i)
            {
                writer.WriteEncodedInt((int)PersistedType.Faction);
                factions[i].State.Serialize(writer);
            }

            var towns = Town.Towns;

            for (var i = 0; i < towns.Count; ++i)
            {
                writer.WriteEncodedInt((int)PersistedType.Town);
                towns[i].State.Serialize(writer);
            }

            writer.WriteEncodedInt((int)PersistedType.Terminator);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                case 0:
                {
                    PersistedType type;

                    while ((type = (PersistedType)reader.ReadEncodedInt()) != PersistedType.Terminator)
                        switch (type)
                        {
                            case PersistedType.Faction:
                                new FactionState(reader);
                                break;
                            case PersistedType.Town:
                                new TownState(reader);
                                break;
                        }

                    break;
                }
            }

            if (!Settings.Enabled && version == 0)
                Timer.DelayCall(TimeSpan.FromSeconds(10), () => Generator.RemoveFactions());
        }

        public override void Delete()
        {
        }

        private enum PersistedType
        {
            Terminator,
            Faction,
            Town
        }
    }
}
