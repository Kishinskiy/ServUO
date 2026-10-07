using System;
using System.Collections.Generic;
using System.Linq;
using Server.Engines.Quests;
using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("an orcish corpse")]
    public class OrcEngineer : Orc
    {
        public static List<OrcEngineer> Instances { get; set; }

        [Constructable]
        public OrcEngineer()
        {
            Title = "the Orcish Engineer";

            SetStr(500, 510);
            SetDex(200, 210);
            SetInt(200, 210);

            SetHits(3500, 3700);

            SetDamage(8, 14);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 65, 70);
            SetResistance(ResistanceType.Fire, 65, 70);
            SetResistance(ResistanceType.Cold, 65, 70);
            SetResistance(ResistanceType.Poison, 60);
            SetResistance(ResistanceType.Energy, 65, 70);

            SetSkill(SkillName.Parry, 100.0, 110.0);
            SetSkill(SkillName.MagicResist, 70.0, 80.0);
            SetSkill(SkillName.Tactics, 110.0, 120.0);
            SetSkill(SkillName.Wrestling, 120.0, 130.0);
            SetSkill(SkillName.DetectHidden, 100.0, 110.0);

            Fame = 1500;
            Karma = -1500;

            if (Instances == null)
                Instances = new List<OrcEngineer>();

            Instances.Add(this);

            Timer SelfDeleteTimer = new InternalSelfDeleteTimer(this);
            SelfDeleteTimer.Start();
        }

        public OrcEngineer(Serial serial)
            : base(serial)
        {
        }

        public static OrcEngineer Spawn(Point3D platLoc, Map platMap)
        {
            if (Instances != null && Instances.Count > 0)
                return null;

            var creature = new OrcEngineer();
            creature.Home = platLoc;
            creature.RangeHome = 4;
            creature.MoveToWorld(platLoc, platMap);

            return creature;
        }

        public override void OnDeath(Container c)
        {
            var rights = GetLootingRights();

            foreach (var m in rights.Select(x => x.m_Mobile).Distinct())
                if (m is PlayerMobile)
                {
                    var pm = m as PlayerMobile;

                    if (pm.ExploringTheDeepQuest == ExploringTheDeepQuestChain.CollectTheComponent)
                    {
                        Item item = new OrcishSchematics();

                        if (m.Backpack == null || !m.Backpack.TryDropItem(m, item, false)) m.BankBox.DropItem(item);

                        m.SendLocalizedMessage(1154489); // You received a Quest Item!
                    }
                }

            if (Instances != null && Instances.Contains(this))
                Instances.Remove(this);

            base.OnDeath(c);
        }

        public override void OnAfterDelete()
        {
            Instances.Remove(this);

            base.OnAfterDelete();
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

            Instances = new List<OrcEngineer>();
            Instances.Add(this);

            Timer SelfDeleteTimer = new InternalSelfDeleteTimer(this);
            SelfDeleteTimer.Start();
        }

        public class InternalSelfDeleteTimer : Timer
        {
            private readonly OrcEngineer Mare;

            public InternalSelfDeleteTimer(Mobile p) : base(TimeSpan.FromMinutes(10))
            {
                Priority = TimerPriority.FiveSeconds;
                Mare = (OrcEngineer)p;
            }

            protected override void OnTick()
            {
                if (Mare.Map != Map.Internal)
                {
                    Mare.Delete();
                    Stop();
                }
            }
        }
    }
}
