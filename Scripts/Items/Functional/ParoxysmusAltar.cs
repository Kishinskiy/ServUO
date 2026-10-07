using System;
using System.Collections.Generic;
using Server.Mobiles;
using Server.Network;

namespace Server.Items
{
    public class ParoxysmusAltar : PeerlessAltar
    {
        public static Dictionary<Mobile, Timer> ProtectionTable = new Dictionary<Mobile, Timer>();

        private readonly Rectangle2D[] m_Bounds =
        {
            new Rectangle2D(6501, 351, 35, 48)
        };

        public override int KeyCount => 16;
        public override MasterKey MasterKey => new ParoxysmusKey();

        public override Type[] Keys
        {
            get
            {
                return new[]
                {
                    typeof(CoagulatedLegs), typeof(PartiallyDigestedTorso),
                    typeof(GelatanousSkull), typeof(SpleenOfThePutrefier)
                };
            }
        }

        public override BasePeerless Boss => new ChiefParoxysmus();

        public override Rectangle2D[] BossBounds => m_Bounds;

        [Constructable]
        public ParoxysmusAltar() : base(0x207A)
        {
            Hue = 0x465;

            BossLocation = new Point3D(6517, 357, 0);
            TeleportDest = new Point3D(6519, 381, 0);
            ExitDest = new Point3D(5623, 3038, 15);
        }

        public ParoxysmusAltar(Serial serial) : base(serial)
        {
        }

        public static void AddProtection(Mobile m)
        {
            if (ProtectionTable != null && !ProtectionTable.ContainsKey(m))
                ProtectionTable[m] = Timer.DelayCall(TimeSpan.FromMinutes(5), () => Damage(m));
        }

        public static bool IsUnderEffects(Mobile m)
        {
            return ProtectionTable != null && ProtectionTable.ContainsKey(m);
        }

        public static void Damage(Mobile m)
        {
            Timer t;

            if (ProtectionTable.TryGetValue(m, out t))
            {
                if (t != null) t.Stop();

                ProtectionTable.Remove(m);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1); // version

            writer.Write(ProtectionTable.Count);

            foreach (var kvp in ProtectionTable)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value.Next);
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                {
                    var count = reader.ReadInt();

                    for (var i = 0; i < count; ++i)
                    {
                        var m = reader.ReadMobile();
                        var end = reader.ReadDateTime();

                        ProtectionTable[m] = Timer.DelayCall(end - DateTime.UtcNow, () => Damage(m));
                    }

                    break;
                }
                case 0:
                {
                    break;
                }
            }

            if (version < 1)
            {
                IPooledEnumerable eable = Map.GetItemsInBounds(new Rectangle2D(6516, 492, 5, 1));

                foreach (Item item in eable)
                    if (item.Movable)
                        item.Delete();

                eable.Free();

                Item gate = new ParoxysmusIronGate(this);
                gate.MoveToWorld(new Point3D(6518, 492, -50), Map);
            }
        }
    }

    public class ParoxysmusIronGate : Item
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public PeerlessAltar Altar { get; set; }

        [Constructable]
        public ParoxysmusIronGate()
            : this(null)
        {
        }

        [Constructable]
        public ParoxysmusIronGate(PeerlessAltar altar)
            : base(0x857)
        {
            Altar = altar;
            Movable = false;
        }

        public ParoxysmusIronGate(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClickDead(Mobile from)
        {
            base.OnDoubleClickDead(from);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.Alive)
                return;

            if (!from.InRange(GetWorldLocation(), 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
            else if (Altar != null && Altar.Fighters.Contains(from))
            {
                from.SendLocalizedMessage(1075070); // The rusty gate cracks open as you step through...
                from.MoveToWorld(Altar.TeleportDest, Altar.Map);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version

            writer.Write(Altar);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            Altar = reader.ReadItem() as PeerlessAltar;
        }
    }
}
