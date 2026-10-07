using System.Collections.Generic;
using Server.ContextMenus;
using Server.Gumps;
using Server.Multis;
using Server.Network;

namespace Server.Items
{
    [FlipableAttribute(0x3BB6, 0x3BB7)]
    public class MapOfTheKnownWorld : Item, ISecurable
    {
        public override bool IsArtifact => true;

        [Constructable]
        public MapOfTheKnownWorld() : base(0x3BB6)
        {
            Name = "a map of the known world";
            Weight = 1.0;
            LootType = LootType.Blessed;
        }

        public MapOfTheKnownWorld(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public SecureLevel Level { get; set; }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            SetSecureLevelEntry.AddTo(from, this, list);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.InRange(GetWorldLocation(), 2))
            {
                from.CloseGump(typeof(InternalGump));
                from.SendGump(new InternalGump());
            }
            else
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.WriteEncodedInt((int)Level);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            Level = (SecureLevel)reader.ReadEncodedInt();
        }

        private class InternalGump : Gump
        {
            public InternalGump() : base(50, 50)
            {
                AddImage(0, 0, 0x12B); //299
            }
        }
    }
}
