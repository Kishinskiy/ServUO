using System.Collections.Generic;
using Server.Engines.VvV;

namespace Server.Items
{
    [TypeAlias("Server.Engines.VvV.MorphEarrings")]
    public class MorphEarrings : GoldEarrings
    {
        public override int LabelNumber => 1094746; // Morph Earrings

        [Constructable]
        public MorphEarrings()
        {
        }

        public MorphEarrings(Serial serial)
            : base(serial)
        {
        }

        public override void OnRemoved(object parent)
        {
            base.OnRemoved(parent);

            if (parent is Mobile) ValidateEquipment((Mobile)parent);
        }

        private void ValidateEquipment(Mobile m)
        {
            if (m == null)
                return;

            var race = m.Race;
            var didDrop = false;

            var list = new List<Item>(m.Items);

            foreach (var item in list)
            {
                var drop = false;

                if (item is BaseArmor && ((BaseArmor)item).RequiredRace != null &&
                    ((BaseArmor)item).RequiredRace != race)
                    drop = true;
                else if (item is BaseWeapon && ((BaseWeapon)item).RequiredRace != null &&
                         ((BaseWeapon)item).RequiredRace != race)
                    drop = true;
                else if (item is BaseJewel && ((BaseJewel)item).RequiredRace != null &&
                         ((BaseJewel)item).RequiredRace != race)
                    drop = true;
                else if (item is BaseClothing && ((BaseClothing)item).RequiredRace != null &&
                         ((BaseClothing)item).RequiredRace != race)
                    drop = true;

                if (drop)
                {
                    if (!didDrop)
                        didDrop = true;

                    if (m.Backpack == null || !m.Backpack.TryDropItem(m, item, false)) m.BankBox.DropItem(item);
                }
            }

            ColUtility.Free(list);

            if (didDrop)
                m.SendLocalizedMessage(500647); // Some equipment has been moved to your backpack.
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            if (version == 0 && ViceVsVirtueSystem.Enabled)
                Timer.DelayCall(() => ViceVsVirtueSystem.Instance.AddVvVItem(this));
        }
    }
}
