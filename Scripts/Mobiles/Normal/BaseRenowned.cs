using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public abstract class BaseRenowned : BaseCreature
    {
        private Dictionary<Mobile, int> m_DamageEntries;

        public abstract Type[] UniqueSAList { get; }
        public abstract Type[] SharedSAList { get; }

        public virtual bool NoGoodies => false;

        public BaseRenowned(AIType aiType)
            : this(aiType, FightMode.Closest)
        {
        }

        public BaseRenowned(AIType aiType, FightMode mode)
            : base(aiType, mode, 18, 1, 0.1, 0.2)
        {
        }

        public BaseRenowned(Serial serial)
            : base(serial)
        {
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

        public virtual void RegisterDamageTo(Mobile m)
        {
            if (m == null)
                return;

            foreach (var de in m.DamageEntries)
            {
                var damager = de.Damager;

                var master = damager.GetDamageMaster(m);

                if (master != null)
                    damager = master;

                RegisterDamage(damager, de.DamageGiven);
            }
        }

        public void RegisterDamage(Mobile from, int amount)
        {
            if (from == null || !from.Player)
                return;

            if (m_DamageEntries.ContainsKey(from))
                m_DamageEntries[from] += amount;
            else
                m_DamageEntries.Add(from, amount);
        }

        public void AwardArtifact(Item artifact)
        {
            if (artifact == null)
                return;

            var totalDamage = 0;

            var validEntries = new Dictionary<Mobile, int>();

            foreach (var kvp in m_DamageEntries)
                if (IsEligible(kvp.Key, artifact))
                {
                    validEntries.Add(kvp.Key, kvp.Value);
                    totalDamage += kvp.Value;
                }

            var randomDamage = Utility.RandomMinMax(1, totalDamage);

            totalDamage = 0;

            foreach (var kvp in m_DamageEntries)
            {
                totalDamage += kvp.Value;

                if (totalDamage > randomDamage)
                {
                    GiveArtifact(kvp.Key, artifact);
                    break;
                }
            }
        }

        public void GiveArtifact(Mobile to, Item artifact)
        {
            if (to == null || artifact == null)
                return;

            to.PlaySound(0x5B4);

            var pack = to.Backpack;

            if (pack == null || !pack.TryDropItem(to, artifact, false))
                artifact.Delete();
            else
                to.SendLocalizedMessage(
                    1062317); // For your valor in combating the fallen beast, a special artifact has been bestowed on you.
        }

        public bool IsEligible(Mobile m, Item Artifact)
        {
            return m.Player && m.Alive && m.InRange(Location, 32) && m.Backpack != null &&
                   m.Backpack.CheckHold(m, Artifact, false);
        }

        public Item GetArtifact()
        {
            var random = Utility.RandomDouble();

            if (0.05 >= random)
                return CreateArtifact(UniqueSAList);
            if (0.15 >= random)
                return CreateArtifact(SharedSAList);

            return null;
        }

        public Item CreateArtifact(Type[] list)
        {
            if (list.Length == 0)
                return null;

            var random = Utility.Random(list.Length);

            var type = list[random];

            var artifact = Loot.Construct(type);

            return artifact;
        }

        public override bool OnBeforeDeath()
        {
            if (!NoKillAwards)
            {
                if (NoGoodies)
                    return base.OnBeforeDeath();

                m_DamageEntries = new Dictionary<Mobile, int>();

                RegisterDamageTo(this);
                AwardArtifact(GetArtifact());
            }

            return base.OnBeforeDeath();
        }
    }
}
