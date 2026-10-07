using Server.Network;

namespace Server.Items
{
    [FlipableAttribute(0x1765, 0x1767)]
    public class AbyssalCloth : Item, ICommodity, IScissorable
    {
        public override int LabelNumber => 1113350; // abyssal cloth

        [Constructable]
        public AbyssalCloth()
            : this(1)
        {
        }

        [Constructable]
        public AbyssalCloth(int amount)
            : base(0x1767)
        {
            Stackable = true;
            Amount = amount;
            Hue = 2075;
        }

        public AbyssalCloth(Serial serial)
            : base(serial)
        {
        }

        TextDefinition ICommodity.Description => LabelNumber;

        bool ICommodity.IsDeedable => true;

        public bool Scissor(Mobile from, Scissors scissors)
        {
            if (Deleted || !from.CanSee(this))
                return false;

            base.ScissorHelper(from, new Bandage(), 1);

            return true;
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

        public override void OnSingleClick(Mobile from)
        {
            var number = Amount == 1 ? 1049124 : 1049123;

            from.Send(
                new MessageLocalized(Serial, ItemID, MessageType.Regular, 0x3B2, 3, number, "", Amount.ToString()));
        }
    }
}
