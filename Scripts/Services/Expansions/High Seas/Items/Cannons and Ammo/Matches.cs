using System;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Items
{
    public class Matches : Item, ICommodity
    {
        private static readonly TimeSpan LightDuration = TimeSpan.FromMinutes(60);

        public override int LabelNumber => 1116112;

        public bool IsLight { get; set; }

        [Constructable]
        public Matches() : this(1)
        {
        }

        [Constructable]
        public Matches(int amount) : base(3947)
        {
            Stackable = true;
            Layer = Layer.TwoHanded;
            Amount = amount;
        }

        public Matches(Serial serial) : base(serial)
        {
        }

        TextDefinition ICommodity.Description => LabelNumber;
        bool ICommodity.IsDeedable => true;

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
            {
                if (Amount > 1)
                {
                    var pack = from.Backpack;

                    if (pack != null)
                    {
                        var match = new Matches();

                        if (pack.CheckHold(from, match, true))
                        {
                            pack.DropItem(match);
                            Amount--;

                            match.ItemID = 2578;
                            from.SendSound(0x047);
                            match.IsLight = true;
                        }
                        else
                        {
                            match.Delete();
                        }
                    }
                }
                else if (!IsLight)
                {
                    new InternalTimer(this);
                    from.SendLocalizedMessage(1116114); //You ignite the match.

                    ItemID = 2578;
                    from.SendSound(0x047);
                    IsLight = true;
                }
                else
                {
                    from.Target = new InternalTarget(this);
                    from.SendLocalizedMessage(1116113); //Target the cannon whose fuse you wish to light.
                }
            }
        }


        public void BurnOut()
        {
            if (RootParent is PlayerMobile)
                ((PlayerMobile)RootParent).SendLocalizedMessage(1116115); //Your match splutters and dies.

            Delete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
            writer.Write(IsLight);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
            if (reader.ReadBool())
                Delete();
        }

        private class InternalTimer : Timer
        {
            private readonly Matches m_Match;

            public InternalTimer(Matches match) : base(LightDuration)
            {
                m_Match = match;
                Start();
            }

            protected override void OnTick()
            {
                if (m_Match != null)
                    m_Match.BurnOut();
            }
        }

        private class InternalTarget : Target
        {
            private Matches m_Match;

            public InternalTarget(Matches match) : base(3, false, TargetFlags.None)
            {
                m_Match = match;
            }

            protected override void OnTarget(Mobile from, object obj)
            {
                if (obj is IShipCannon)
                {
                    var cannon = (IShipCannon)obj;

                    if (cannon.CanLight)
                        cannon.LightFuse(from);
                    else
                        from.SendLocalizedMessage(1116078); //There is no fuse to light! Prime the cannon first.
                }
            }
        }
    }
}
