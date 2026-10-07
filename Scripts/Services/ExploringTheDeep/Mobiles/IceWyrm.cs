using System;
using System.Collections.Generic;
using System.Linq;
using Server.Engines.Quests;
using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("an ice wyrm corpse")]
    public class IceWyrm : WhiteWyrm
    {
        public static List<IceWyrm> Instances { get; set; }

        public override bool ReacquireOnMovement => true;
        public override int TreasureMapLevel => 4;
        public override int Meat => 20;
        public override int Hides => 25;
        public override HideType HideType => HideType.Barbed;
        public override FoodType FavoriteFood => FoodType.Meat;
        public override bool CanAngerOnTame => true;
        public override bool CanRummageCorpses => true;

        [Constructable]
        public IceWyrm()
        {
            Name = "Ice Wyrm";
            Hue = 2729;
            Body = 180;

            SetResistance(ResistanceType.Cold, 100);

            Timer SelfDeleteTimer = new InternalSelfDeleteTimer(this);
            SelfDeleteTimer.Start();

            Tamable = false;

            if (Instances == null)
                Instances = new List<IceWyrm>();

            Instances.Add(this);
        }

        public IceWyrm(Serial serial)
            : base(serial)
        {
        }

        public static IceWyrm Spawn(Point3D platLoc, Map platMap)
        {
            if (Instances != null && Instances.Count > 0)
                return null;

            var creature = new IceWyrm();
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

                    if (pm.ExploringTheDeepQuest == ExploringTheDeepQuestChain.CusteauPerron)
                    {
                        Item item = new IceWyrmScale();

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
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            Instances = new List<IceWyrm>();
            Instances.Add(this);

            Timer SelfDeleteTimer = new InternalSelfDeleteTimer(this);
            SelfDeleteTimer.Start();
        }

        public class InternalSelfDeleteTimer : Timer
        {
            private readonly IceWyrm Mare;

            public InternalSelfDeleteTimer(Mobile p) : base(TimeSpan.FromMinutes(60))
            {
                Priority = TimerPriority.FiveSeconds;
                Mare = (IceWyrm)p;
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
