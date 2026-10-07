using System;
using System.Collections.Generic;
using Server.Engines.HuntsmasterChallenge;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Items
{
    public class HuntingPermit : Item
    {
        private bool m_HasSubmitted;

        public static List<HuntingPermit> Permits { get; } = new List<HuntingPermit>();

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ProducedTrophy { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public HuntingKillEntry KillEntry { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool HasDocumentedKill => KillEntry != null;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool CanUseTaxidermyOn =>
            HasDocumentedKill && KillEntry.KillIndex < HuntingTrophyInfo.Infos.Count && !ProducedTrophy;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool HasSubmitted
        {
            get => m_HasSubmitted;
            set
            {
                m_HasSubmitted = value;

                if (m_HasSubmitted && Permits.Contains(this))
                    Permits.Remove(this);
            }
        }

        public override int LabelNumber => 1155704; // Hunting Permit

        [Constructable]
        public HuntingPermit(Mobile from)
            : base(5360)
        {
            Owner = from;
            Permits.Add(this);
        }

        public HuntingPermit(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.Backpack != null && IsChildOf(from.Backpack))
            {
                if (!HasDocumentedKill)
                {
                    from.Target = new InternalTarget(this);
                    MessageHelper.SendLocalizedMessageTo(this, from, 1155705,
                        0x45); // Target the kill you wish to document
                }
                else
                {
                    from.SendLocalizedMessage(1155712); // This hunting permit has already documented a kill.
                }
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (KillEntry == null || KillEntry.KillIndex < 0 || KillEntry.KillIndex >= HuntingTrophyInfo.Infos.Count)
                return;

            var info = HuntingTrophyInfo.Infos[KillEntry.KillIndex];

            if (Owner != null)
                list.Add(1155708, Owner.Name); // Hunter: ~1_NAME~

            if (KillEntry.DateKilled != DateTime.MinValue)
                list.Add(1155709, KillEntry.DateKilled.ToShortDateString()); // Date of Kill: ~1_DATE~

            if (KillEntry.Location != null)
                list.Add(1061114, KillEntry.Location); // Location: ~1_val~

            list.Add(1155718, info.Species.ToString());

            if (info.MeasuredBy == MeasuredBy.Length)
                list.Add(1155711, KillEntry.Measurement.ToString()); // Length: ~1_VAL~
            else if (info.MeasuredBy == MeasuredBy.Wingspan)
                list.Add(1155710, KillEntry.Measurement.ToString()); // Wingspan: ~1_VAL~
            else
                list.Add(1072789, KillEntry.Measurement.ToString()); // Weight: ~1_WEIGHT~
        }

        public override void Delete()
        {
            base.Delete();

            if (Permits.Contains(this))
                Permits.Remove(this);
        }

        public static bool HasPermit(Mobile from)
        {
            foreach (var permit in Permits)
                if (permit.Owner == from)
                    return true;

            return false;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);

            writer.Write(Owner);
            writer.Write(ProducedTrophy);
            writer.Write(m_HasSubmitted);

            if (KillEntry != null)
            {
                writer.Write(1);
                KillEntry.Serialize(writer);
            }
            else
            {
                writer.Write(0);
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var v = reader.ReadInt();

            Owner = reader.ReadMobile();
            ProducedTrophy = reader.ReadBool();
            m_HasSubmitted = reader.ReadBool();

            if (reader.ReadInt() == 1)
                KillEntry = new HuntingKillEntry(reader);

            if (Owner != null && !m_HasSubmitted)
                Permits.Add(this);
        }

        private class InternalTarget : Target
        {
            private readonly HuntingPermit m_Permit;

            public InternalTarget(HuntingPermit Permit)
                : base(-1, false, TargetFlags.None)
            {
                m_Permit = Permit;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Corpse)
                {
                    var c = targeted as Corpse;

                    if (!from.InRange(c.Location, 3))
                        from.SendLocalizedMessage(500446); // That is too far away.
                    if (c.VisitedByTaxidermist)
                    {
                        from.SendLocalizedMessage(
                            1042596); // That corpse seems to have been visited by a taxidermist already.
                    }
                    else if (!m_Permit.IsChildOf(from.Backpack))
                    {
                        from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                    }
                    else if (c.Owner == null)
                    {
                        from.SendLocalizedMessage(1155706); // That is not a valid kill.
                    }
                    else if (!IsOnlyAttacker(from, c.Owner))
                    {
                        from.SendLocalizedMessage(1155707); // You cannot document someone else's kill.
                    }
                    else
                    {
                        var t = c.Owner.GetType();

                        if (t == typeof(RagingGrizzlyBear)) // Bandaid Fix, we'll keep this until others arise
                            t = typeof(GrizzlyBear);

                        for (var i = 0; i < HuntingTrophyInfo.Infos.Count; i++)
                        {
                            var info = HuntingTrophyInfo.Infos[i];

                            if (t == info.CreatureType)
                            {
                                var v = 0;

                                if (HuntingSystem.Instance != null &&
                                    HuntingSystem.Instance.IsPrimeHunt(from, c.Location))
                                {
                                    v = Utility.RandomMinMax(0, 100);
                                }
                                else
                                {
                                    v = Utility.RandomMinMax(0, 10000);
                                    v = (int)Math.Sqrt(v);
                                    v = 100 - v;
                                }

                                var measurement = info.MinMeasurement +
                                                  (int)((info.MaxMeasurement - info.MinMeasurement) * (v / 100.0));
                                m_Permit.KillEntry = new HuntingKillEntry(m_Permit.Owner, measurement, DateTime.Now, i,
                                    WorldLocationInfo.GetLocationString(c.Location, c.Map));
                                c.VisitedByTaxidermist = true;

                                from.PlaySound(0x249);
                                from.PrivateOverheadMessage(MessageType.Regular, 0x45, 1155713,
                                    from.NetState); // *You document your kill on the permit*
                                m_Permit.InvalidateProperties();
                                return;
                            }
                        }

                        from.SendLocalizedMessage(1155706); // That is not a valid kill.
                    }
                }
            }

            private bool IsOnlyAttacker(Mobile from, Mobile creature)
            {
                var bc = creature as BaseCreature;

                if (bc != null)
                {
                    var rights = bc.GetLootingRights();

                    return rights.Count > 0 && rights[0].m_Mobile == from && rights[0].m_Damage >= bc.HitsMax;
                }

                return false;
            }
        }
    }
}
