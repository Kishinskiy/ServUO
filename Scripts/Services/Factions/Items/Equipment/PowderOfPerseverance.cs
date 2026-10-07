using Server.Items;
using Server.Targeting;

namespace Server.Factions
{
    public class PowderOfPerseverance : Item, IFactionItem
    {
        public override int LabelNumber => 1094756; // Powder of Perseverance

        public PowderOfPerseverance()
            : base(4102)
        {
            Hue = 2419;
        }

        public PowderOfPerseverance(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
            {
                if (FactionEquipment.CanUse(this, from))
                    from.BeginTarget(-1, false, TargetFlags.None, (m, targeted) =>
                    {
                        if (targeted is IFactionItem && targeted is IWearableDurability)
                        {
                            var durability = targeted as IWearableDurability;

                            if (durability.HitPoints >= durability.MaxHitPoints)
                            {
                                m.SendLocalizedMessage(1094761); // This item is already in perfect condition.
                            }
                            else if (durability.MaxHitPoints <= 125)
                            {
                                m.SendLocalizedMessage(1049083); // You cannot use the powder on that item.
                            }
                            else
                            {
                                if (durability.MaxHitPoints == 255)
                                    durability.MaxHitPoints = 225;
                                else
                                    durability.MaxHitPoints -= 25;

                                durability.HitPoints = durability.MaxHitPoints;

                                m.SendLocalizedMessage(1049084); // You successfully use the powder on the item.
                                m.SendLocalizedMessage(1094760); // You have used up your Powder of Perseverance.
                                m.PlaySound(0x247);

                                Delete();
                            }
                        }
                        else
                        {
                            m.SendLocalizedMessage(1049083); // You cannot use the powder on that item.
                        }
                    });
            }
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1060584, "1"); // uses remaining: ~1_val~

            FactionEquipment.AddFactionProperties(this, list);
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

        #region Factions

        public FactionItem FactionItemState { get; set; }

        #endregion
    }
}
