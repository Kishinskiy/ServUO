using System.Collections.Generic;
using Server.Multis;
using Server.Targeting;

namespace Server.Items
{
    public class RuinedShipPlans : Item
    {
        public enum PlanType
        {
            One = 1,
            Two,
            Three,
            Four,
            Five,
            Six,
            Seven,
            Eight
        }

        public PlanType Type { get; set; }

        public List<PlanType> Joined { get; } = new List<PlanType>();

        public override int LabelNumber => 1116784;

        [Constructable]
        public RuinedShipPlans() : this((PlanType)Utility.RandomMinMax(1, 8))
        {
        }

        [Constructable]
        public RuinedShipPlans(PlanType type) : base(5360)
        {
            Type = type;
            Joined.Add(type);
        }

        public RuinedShipPlans(Serial serial) : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1116747); //Orc Ship

            if (Joined.Count == 1)
                list.Add(1116776, string.Format("{0}\t{1}", ((int)Type).ToString(), "8")); //Part ~1_val~ of ~2_val~
            else
                list.Add(1116777, string.Format("{0}\t{1}", Joined.Count.ToString(), "8")); //Parts ~1_val~ of ~2_val~

            if (Joined.Count > 1)
            {
                Joined.Sort();

                var str = "Contains Parts ";
                foreach (int i in Joined)
                    str += i + ", ";

                list.Add(1114057, str);
            }
        }

        public bool TryCombine(Mobile from, RuinedShipPlans plans)
        {
            if (Joined.Contains(plans.Type))
            {
                from.SendLocalizedMessage(1116787); //This part is already attached.
                return false;
            }

            plans.Delete();
            from.PlaySound(0x249);

            foreach (var type in plans.Joined)
                if (!Joined.Contains(type))
                    Joined.Add(type);

            InvalidateProperties();

            if (Joined.Count == 8)
            {
                from.AddToBackpack(new OrcishGalleonDeed());
                from.SendLocalizedMessage(1116788); //You have completed a deed for an Orc Ship!

                Delete();
            }
            else
            {
                from.Target = new InternalTarget(this);
            }

            return true;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
            {
                from.Target = new InternalTarget(this);
                from.SendLocalizedMessage(1116785); //Target another piece of ship plans to combine.
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
            writer.Write((int)Type);

            writer.Write(Joined.Count);
            for (var i = 0; i < Joined.Count; i++)
                writer.Write((int)Joined[i]);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
            Type = (PlanType)reader.ReadInt();

            var count = reader.ReadInt();
            for (var i = 0; i < count; i++)
                Joined.Add((PlanType)reader.ReadInt());
        }

        private class InternalTarget : Target
        {
            private readonly RuinedShipPlans m_Plans;

            public InternalTarget(RuinedShipPlans plans) : base(-1, false, TargetFlags.None)
            {
                m_Plans = plans;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Item && !((Item)targeted).IsChildOf(from.Backpack))
                {
                    from.SendMessage("That must be in your pack to combine.");
                }
                else if (targeted is RuinedShipPlans)
                {
                    var toAttach = (RuinedShipPlans)targeted;
                    m_Plans.TryCombine(from, toAttach);
                }
                else
                {
                    from.SendLocalizedMessage(1116786); //These do not fit together.
                }
            }
        }
    }
}
