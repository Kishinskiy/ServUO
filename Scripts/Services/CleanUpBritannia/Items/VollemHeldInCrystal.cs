using Server.Gumps;
using Server.Mobiles;

namespace Server.Items
{
    public class VollemHeldInCrystal : BaseImprisonedMobile
    {
        public override int LabelNumber => 1113629; // A Vollem Held in Crystal

        public override BaseCreature Summon => new VollemHeld();

        [Constructable]
        public VollemHeldInCrystal()
            : base(0x1f19)
        {
            Hue = 1154;
            LootType = LootType.Blessed;
            Weight = 1;
        }

        public VollemHeldInCrystal(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
                from.SendGump(new ConfirmBreakCrystalGump(this));
            else
                from.SendLocalizedMessage(1010095); // This must be on your person to use.
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }

    public class VollemHeld : Vollem
    {
        [Constructable]
        public VollemHeld()
        {
        }

        public VollemHeld(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }
}
