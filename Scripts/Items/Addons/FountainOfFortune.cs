using System;
using System.Collections.Generic;
using Server.Gumps;

namespace Server.Items
{
    public class FountainOfFortune : BaseAddon
    {
        private static readonly int LuckBonus = 400;

        private static readonly List<FountainOfFortune> m_Fountains = new List<FountainOfFortune>();
        private static readonly Dictionary<Mobile, DateTime> m_LuckTable = new Dictionary<Mobile, DateTime>();

        private static Timer m_Timer;

        public Dictionary<Mobile, DateTime> ResCooldown { get; private set; }

        public Dictionary<Mobile, DateTime> RewardCooldown { get; private set; }

        public static Dictionary<Mobile, DateTime> SpecialProtection { get; } = new Dictionary<Mobile, DateTime>();

        public static Dictionary<Mobile, DateTime> BalmBoost { get; } = new Dictionary<Mobile, DateTime>();

        public override bool HandlesOnMovement => true;

        [Constructable]
        public FountainOfFortune()
        {
            var itemID = 0x1731;

            AddComponent(new AddonComponent(itemID++), -2, +1, 0);
            AddComponent(new AddonComponent(itemID++), -1, +1, 0);
            AddComponent(new AddonComponent(itemID++), +0, +1, 0);
            AddComponent(new AddonComponent(itemID++), +1, +1, 0);

            AddComponent(new AddonComponent(itemID++), +1, +0, 0);
            AddComponent(new AddonComponent(itemID++), +1, -1, 0);
            AddComponent(new AddonComponent(itemID++), +1, -2, 0);

            AddComponent(new AddonComponent(itemID++), +0, -2, 0);
            AddComponent(new AddonComponent(itemID++), +0, -1, 0);
            AddComponent(new AddonComponent(itemID++), +0, +0, 0);

            AddComponent(new AddonComponent(itemID++), -1, +0, 0);
            AddComponent(new AddonComponent(itemID++), -2, +0, 0);

            AddComponent(new AddonComponent(itemID++), -2, -1, 0);
            AddComponent(new AddonComponent(itemID++), -1, -1, 0);

            AddComponent(new AddonComponent(itemID++), -1, -2, 0);
            AddComponent(new AddonComponent(++itemID), -2, -2, 0);

            ResCooldown = new Dictionary<Mobile, DateTime>();
            RewardCooldown = new Dictionary<Mobile, DateTime>();

            Movable = false;

            AddFountain(this);
        }

        public FountainOfFortune(Serial serial) : base(serial)
        {
        }

        public bool OnTarget(Mobile from, Item coin)
        {
            DefragTables();

            if (IsCoolingDown(from))
            {
                from.SendLocalizedMessage(1113368); // You already made a wish today. Try again tomorrow!
                return false;
            }

            if (.20 >= Utility.RandomDouble())
            {
                Item item = null;
                switch (Utility.Random(4))
                {
                    case 0: item = new SolesOfProvidence(); break;
                    case 1: item = new GemologistsSatchel(); break;
                    case 2: item = new RelicFragment(5); break;
                    case 3: item = new EnchantedEssence(5); break;
                }

                if (from.Backpack == null || !from.Backpack.TryDropItem(from, item, false))
                    item.MoveToWorld(from.Location, from.Map);
            }
            else
            {
                switch (Utility.Random(4))
                {
                    case 0:
                        from.AddStatMod(new StatMod(StatType.Str, "FoF_Str", 10, TimeSpan.FromMinutes(60)));
                        from.SendLocalizedMessage(1113373); // You suddenly feel stronger!
                        break;
                    case 1:
                        from.AddStatMod(new StatMod(StatType.Dex, "FoF_Dex", 10, TimeSpan.FromMinutes(60)));
                        from.SendLocalizedMessage(1113374); // You suddenly feel more agile!
                        break;
                    case 2:
                        from.AddStatMod(new StatMod(StatType.Int, "FoF_Int", 10, TimeSpan.FromMinutes(60)));
                        from.SendLocalizedMessage(1113371); // You suddenly feel wiser!
                        break;
                    case 3:
                        m_LuckTable[from] = DateTime.UtcNow + TimeSpan.FromMinutes(60);
                        from.SendLocalizedMessage(1079551); // Your luck just improved!
                        break;
                    case 4:
                        SpecialProtection[from] = DateTime.UtcNow + TimeSpan.FromMinutes(60);
                        from.SendLocalizedMessage(1113375); // You suddenly feel less vulnerable!
                        break;
                    case 5:
                        BalmBoost[from] = DateTime.UtcNow + TimeSpan.FromMinutes(60);
                        from.SendLocalizedMessage(1113372); // The duration of your balm has been increased by an hour!
                        break;
                }

                from.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);
            }

            from.PlaySound(0x22);

            RewardCooldown[from] = DateTime.UtcNow + TimeSpan.FromHours(24);

            if (coin.Amount <= 1)
                coin.Delete();
            else
                coin.Amount--;

            return false;
        }

        public bool IsCoolingDown(Mobile from)
        {
            foreach (var fountain in m_Fountains)
                if (fountain.RewardCooldown != null && fountain.RewardCooldown.ContainsKey(from))
                    return true;

            return false;
        }

        public static int GetLuckBonus(Mobile from)
        {
            if (m_LuckTable.ContainsKey(from))
                return LuckBonus;

            return 0;
        }

        public static bool UnderProtection(Mobile m)
        {
            if (SpecialProtection.ContainsKey(m))
                return true;

            return false;
        }

        public bool CanRes(Mobile m)
        {
            if (!ResCooldown.ContainsKey(m))
                return true;

            if (ResCooldown[m] < DateTime.UtcNow)
            {
                ResCooldown.Remove(m);
                return true;
            }

            return false;
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (m.Player && CanRes(m) && !m.Alive && m.InRange(Location, 5))
                m.SendGump(new ResurrectGump(m, m, ResurrectMessage.Generic, false, 0.0, Resurrect_Callback));
        }

        public void Resurrect_Callback(Mobile m)
        {
            ResCooldown[m] = DateTime.UtcNow + TimeSpan.FromMinutes(10);
        }

        public static void DefragTables()
        {
            foreach (var fountain in m_Fountains)
            {
                var list = new List<Mobile>(fountain.ResCooldown.Keys);
                var list2 = new List<Mobile>(fountain.RewardCooldown.Keys);

                foreach (var m in list)
                    if (fountain.ResCooldown.ContainsKey(m) && fountain.ResCooldown[m] < DateTime.UtcNow)
                        fountain.ResCooldown.Remove(m);

                foreach (var m in list2)
                    if (fountain.RewardCooldown.ContainsKey(m) && fountain.RewardCooldown[m] < DateTime.UtcNow)
                        fountain.RewardCooldown.Remove(m);

                list.Clear();
                list2.Clear();
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

            foreach (var kvp in SpecialProtection)
                if (kvp.Value < DateTime.UtcNow)
                    remove.Add(kvp.Key);

            remove.ForEach(m => { SpecialProtection.Remove(m); });

            remove.Clear();

            foreach (var kvp in BalmBoost)
                if (kvp.Value < DateTime.UtcNow)
                    remove.Add(kvp.Key);

            remove.ForEach(m => { BalmBoost.Remove(m); });

            remove.Clear();
        }

        public override void Delete()
        {
            base.Delete();

            RemoveFountain(this);

            if (ResCooldown != null)
                ResCooldown.Clear();

            if (RewardCooldown != null)
                RewardCooldown.Clear();
        }

        public static void AddFountain(FountainOfFortune fountain)
        {
            if (!m_Fountains.Contains(fountain))
            {
                m_Fountains.Add(fountain);
                StartTimer();
            }
        }

        public static void RemoveFountain(FountainOfFortune fountain)
        {
            if (m_Fountains.Contains(fountain))
                m_Fountains.Remove(fountain);

            if (m_Fountains.Count == 0 && m_Timer != null)
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

            ResCooldown = new Dictionary<Mobile, DateTime>();
            RewardCooldown = new Dictionary<Mobile, DateTime>();

            AddFountain(this);
        }
    }
}
