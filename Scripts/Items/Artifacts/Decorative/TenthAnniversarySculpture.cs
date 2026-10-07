using System;
using System.Collections.Generic;
using Server.Accounting;
using Server.Engines.VeteranRewards;

namespace Server.Items
{
    [Flipable(0x3BB3, 0x3BB4)]
    public class TenthAnniversarySculpture : Item
    {
        private static readonly Dictionary<Mobile, DateTime> m_LuckTable = new Dictionary<Mobile, DateTime>();
        private static readonly List<TenthAnniversarySculpture> m_sculptures = new List<TenthAnniversarySculpture>();

        private static Timer m_Timer;

        private static readonly int MaxLuckBonus = 1000;
        public override bool IsArtifact => true;
        public override int LabelNumber => 1079532; // 10th Anniversary Sculpture
        public Dictionary<Mobile, DateTime> RewardCooldown { get; private set; }

        [Constructable]
        public TenthAnniversarySculpture() : base(15283)
        {
            Weight = 1.0;
            RewardCooldown = new Dictionary<Mobile, DateTime>();
            AddSculpture(this);
        }

        public TenthAnniversarySculpture(Serial serial) : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 2))
            {
                from.SendLocalizedMessage(500446); // That is too far away.
                return;
            }

            DefragTables();

            if (!IsCoolingDown(from))
            {
                m_LuckTable[from] = DateTime.UtcNow + TimeSpan.FromMinutes(60);
                from.SendLocalizedMessage(1079551); // Your luck just improved!
                RewardCooldown[from] = DateTime.UtcNow + TimeSpan.FromHours(24);

                from.Delta(MobileDelta.Armor);
            }
        }

        public bool IsCoolingDown(Mobile from)
        {
            var donemessage = false;

            if (m_LuckTable.ContainsKey(from))
            {
                from.SendLocalizedMessage(
                    1079534); // You're still feeling lucky from the last time you touched the sculpture.
                donemessage = true;
            }

            foreach (var sculpture in m_sculptures)
                if (sculpture.RewardCooldown != null && sculpture.RewardCooldown.ContainsKey(from))
                {
                    if (!donemessage)
                    {
                        var left = sculpture.RewardCooldown[from] - DateTime.UtcNow;

                        if (left.TotalHours > 1)
                            from.SendLocalizedMessage(1079550,
                                ((int)left.TotalHours)
                                .ToString()); // You can improve your fortunes again in about ~1_TIME~ hours.
                        else if (left.TotalMinutes > 1)
                            from.SendLocalizedMessage(1079548,
                                ((int)left.TotalMinutes)
                                .ToString()); // You can improve your fortunes in about ~1_TIME~ minutes.
                        else
                            from.SendLocalizedMessage(1079547); // Your fortunes are about to improve.
                    }

                    return true;
                }

            return false;
        }

        public static void DefragTables()
        {
            foreach (var sculpture in m_sculptures)
            {
                var list = new List<Mobile>(sculpture.RewardCooldown.Keys);

                foreach (var m in list)
                    if (sculpture.RewardCooldown.ContainsKey(m) && sculpture.RewardCooldown[m] < DateTime.UtcNow)
                        sculpture.RewardCooldown.Remove(m);

                list.Clear();
            }

            var remove = new List<Mobile>();
            foreach (var kvp in m_LuckTable)
                if (kvp.Value < DateTime.UtcNow)
                    remove.Add(kvp.Key);

            remove.ForEach(m =>
            {
                m_LuckTable.Remove(m);

                if (m.NetState != null)
                    m.SendLocalizedMessage(1079552); //Your luck just ran out.
            });

            remove.Clear();
        }

        public static int GetLuckBonus(Mobile from)
        {
            if (m_LuckTable.ContainsKey(from))
            {
                var account = from.Account as Account;

                if (account != null) return Math.Min(MaxLuckBonus, 200 + RewardSystem.GetRewardLevel(account) * 50);
            }

            return 0;
        }

        public override void Delete()
        {
            base.Delete();

            RemoveSculpture(this);

            if (RewardCooldown != null)
                RewardCooldown.Clear();
        }

        public static void AddSculpture(TenthAnniversarySculpture sculpture)
        {
            if (!m_sculptures.Contains(sculpture))
            {
                m_sculptures.Add(sculpture);
                StartTimer();
            }
        }

        public static void RemoveSculpture(TenthAnniversarySculpture sculpture)
        {
            if (m_sculptures.Contains(sculpture))
                m_sculptures.Remove(sculpture);

            if (m_sculptures.Count == 0 && m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer = null;
            }
        }

        public static void StartTimer()
        {
            if (m_Timer != null && m_Timer.Running)
                return;

            m_Timer = Timer.DelayCall(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), DefragTables);
            m_Timer.Start();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
            RewardCooldown = new Dictionary<Mobile, DateTime>();

            AddSculpture(this);
        }
    }
}
