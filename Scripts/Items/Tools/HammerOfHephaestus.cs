using System;
using System.Collections.Generic;
using System.Linq;
using Server.Engines.Craft;

namespace Server.Items
{
    public class HammerOfHephaestus : AncientSmithyHammer
    {
        private static readonly List<HammerOfHephaestus> _Instances = new List<HammerOfHephaestus>();

        public override int LabelNumber => 1077740; // Hammer of Hephaestus

        [Constructable]
        public HammerOfHephaestus()
            : base(10, 20)
        {
            LootType = LootType.Blessed;
            Hue = 0x0;

            _Instances.Add(this);
        }

        public HammerOfHephaestus(Serial serial)
            : base(serial)
        {
        }

        public static void Initialize()
        {
            Timer.DelayCall(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5), Tick_Callback);
        }

        private static void Tick_Callback()
        {
            foreach (var hammer in _Instances.Where(h => h != null && !h.Deleted && h.UsesRemaining < 20))
            {
                hammer.UsesRemaining++;
                hammer.InvalidateProperties();
            }
        }

        public override void Delete()
        {
            base.Delete();

            _Instances.Remove(this);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack) || Parent == from)
            {
                if (UsesRemaining > 0)
                {
                    var system = CraftSystem;

                    var num = system.CanCraft(from, this, null);

                    if (num > 0)
                    {
                        from.SendLocalizedMessage(num);
                    }
                    else
                    {
                        var context = system.GetContext(from);

                        from.SendGump(new CraftGump(from, system, this, null));
                    }
                }
                else
                {
                    from.SendLocalizedMessage(1072306); // You must wait a moment for it to recharge.
                }
            }
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
        }

        public override bool CanEquip(Mobile from)
        {
            if (UsesRemaining > 0)
                return base.CanEquip(from);

            from.SendLocalizedMessage(1072306); // You must wait a moment for it to recharge.
            return false;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            if (version == 0 && Hue == 0x482)
                Hue = 0x0;

            _Instances.Add(this);
        }
    }
}
