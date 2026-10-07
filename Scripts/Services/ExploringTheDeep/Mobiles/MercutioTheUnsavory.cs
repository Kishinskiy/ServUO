using System;
using System.Collections.Generic;
using System.Linq;
using Server.Engines.Quests;
using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a mercutio corpse")]
    public class MercutioTheUnsavory : BaseCreature
    {
        public static List<MercutioTheUnsavory> Instances { get; set; }

        public override bool ClickTitle => false;
        public override bool AlwaysMurderer => true;

        [Constructable]
        public MercutioTheUnsavory()
            : base(AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Body = 0x190;
            Hue = Utility.RandomSkinHue();
            Name = "Mercutio";
            Title = "The Unsavory";
            Female = false;

            SetStr(1000, 1300);
            SetDex(101, 125);
            SetInt(61, 75);

            SetDamage(11, 24);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 10, 15);
            SetResistance(ResistanceType.Fire, 10, 15);
            SetResistance(ResistanceType.Poison, 10, 15);
            SetResistance(ResistanceType.Energy, 10, 15);

            SetSkill(SkillName.Fencing, 106.0, 117.5);
            SetSkill(SkillName.Macing, 105.0, 117.5);
            SetSkill(SkillName.MagicResist, 50.0, 90.5);
            SetSkill(SkillName.Swords, 105.0, 117.5);
            SetSkill(SkillName.Parry, 105.0, 117.5);
            SetSkill(SkillName.Tactics, 105.0, 117.5);
            SetSkill(SkillName.Wrestling, 55.0, 87.5);

            Fame = 3000;
            Karma = -3000;

            if (Instances == null)
                Instances = new List<MercutioTheUnsavory>();

            Instances.Add(this);

            AddImmovableItem(new Cutlass());
            AddImmovableItem(new ChainChest());
            AddImmovableItem(Loot.RandomShield());
            AddImmovableItem(new ShortPants(Utility.RandomNeutralHue()));
            AddImmovableItem(new Boots(Utility.RandomNeutralHue()));

            Utility.AssignRandomHair(this);

            Timer SelfDeleteTimer = new InternalSelfDeleteTimer(this);
            SelfDeleteTimer.Start();
        }

        public MercutioTheUnsavory(Serial serial)
            : base(serial)
        {
        }

        private void AddImmovableItem(Item item)
        {
            item.LootType = LootType.Blessed;
            AddItem(item);
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
                        Item item = new MercutiosCutlass();

                        if (m.Backpack == null || !m.Backpack.TryDropItem(m, item, false)) m.BankBox.DropItem(item);

                        m.SendLocalizedMessage(1154489); // You received a Quest Item!
                    }
                }

            if (Instances != null && Instances.Contains(this))
                Instances.Remove(this);

            base.OnDeath(c);
        }

        public static MercutioTheUnsavory Spawn(Point3D platLoc, Map platMap)
        {
            if (Instances != null && Instances.Count > 0)
                return null;

            var creature = new MercutioTheUnsavory();
            creature.Home = platLoc;
            creature.RangeHome = 4;
            creature.MoveToWorld(platLoc, platMap);

            return creature;
        }

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average);
        }

        public override void OnAfterDelete()
        {
            Instances.Remove(this);

            base.OnAfterDelete();
        }

        public override void OnGotMeleeAttack(Mobile attacker)
        {
            base.OnGotMeleeAttack(attacker);

            if (0.1 >= Utility.RandomDouble())
                SpawnBrigand(attacker);
        }

        public override void OnGaveMeleeAttack(Mobile defender)
        {
            base.OnGaveMeleeAttack(defender);

            if (0.1 >= Utility.RandomDouble())
                SpawnBrigand(defender);
        }

        #region Helpers

        public void SpawnBrigand(Mobile target)
        {
            var map = Map;

            if (map == null)
                return;

            var brigands = 0;

            IPooledEnumerable eable = GetMobilesInRange(10);

            foreach (Mobile m in eable)
                if (m is Brigand)
                    ++brigands;

            eable.Free();

            if (brigands < 16)
            {
                PlaySound(0x3D);

                var newBrigands = Utility.RandomMinMax(3, 6);

                for (var i = 0; i < newBrigands; ++i)
                {
                    BaseCreature brigand = new Brigand();

                    brigand.Team = Team;

                    var validLocation = false;
                    var loc = Location;

                    for (var j = 0; !validLocation && j < 10; ++j)
                    {
                        var x = X + Utility.Random(3) - 1;
                        var y = Y + Utility.Random(3) - 1;
                        var z = map.GetAverageZ(x, y);

                        if (validLocation = map.CanFit(x, y, Z, 16, false, false))
                            loc = new Point3D(x, y, Z);
                        else if (validLocation = map.CanFit(x, y, z, 16, false, false))
                            loc = new Point3D(x, y, z);
                    }

                    brigand.MoveToWorld(loc, map);
                    brigand.Combatant = target;
                }
            }
        }

        #endregion

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            Instances = new List<MercutioTheUnsavory>();
            Instances.Add(this);

            Timer SelfDeleteTimer = new InternalSelfDeleteTimer(this);
            SelfDeleteTimer.Start();
        }

        public class InternalSelfDeleteTimer : Timer
        {
            private readonly MercutioTheUnsavory Mare;

            public InternalSelfDeleteTimer(Mobile p) : base(TimeSpan.FromMinutes(60))
            {
                Priority = TimerPriority.FiveSeconds;
                Mare = (MercutioTheUnsavory)p;
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
