using System;
using Server.Mobiles;
using Server.Network;

namespace Server.Items
{
    public class PlagueBeastMutationCore : Item, IScissorable
    {
        public override int LabelNumber => 1153760; // a plague beast mutation core

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Cut { get; set; }

        [Constructable]
        public PlagueBeastMutationCore()
            : base(0x1CF0)
        {
            Weight = 1.0;
            Hue = 0x480;
            Cut = true;
        }

        public PlagueBeastMutationCore(Serial serial)
            : base(serial)
        {
        }

        public virtual bool Scissor(Mobile from, Scissors scissors)
        {
            if (!Cut)
            {
                var owner = RootParent as PlagueBeastLord;

                Cut = true;
                Movable = true;

                from.AddToBackpack(this);
                from.LocalOverheadMessage(MessageType.Regular, 0x34,
                    1071906); // * You remove the plague mutation core from the plague beast, causing it to dissolve into a pile of goo *				

                if (owner != null)
                    Timer.DelayCall(TimeSpan.FromSeconds(1), KillParent, owner);

                return true;
            }

            return false;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write(Cut);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            Cut = reader.ReadBool();
        }

        private void KillParent(PlagueBeastLord parent)
        {
            parent.Unfreeze();
            parent.Kill();
        }
    }
}
