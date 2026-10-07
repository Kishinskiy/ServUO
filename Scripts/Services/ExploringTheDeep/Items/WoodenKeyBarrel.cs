using System;
using Server.Mobiles;
using Server.Network;

namespace Server.Items
{
    public class WoodenKeyBarrel : DamageableItem
    {
        private Parts m_key;
        public override int LabelNumber => 1023703; // barrel

        [CommandProperty(AccessLevel.GameMaster)]
        public StorageLocker StorageLocker { get; set; }

        [Constructable]
        public WoodenKeyBarrel(Parts key)
            : base(0x0FAE, 0x0FAE)
        {
            Name = "barrel";
            m_key = key;
            Level = ItemLevel.VeryEasy;
            Movable = false;
            HitsMax = 60;
        }

        public WoodenKeyBarrel(Serial serial)
            : base(serial)
        {
        }

        public override bool OnBeforeDestroyed()
        {
            if (m_key != Parts.None)
            {
                new LockerKey(m_key).MoveToWorld(new Point3D(base.Location), Map);
                StorageLocker.BeginRestart(TimeSpan.FromMinutes(10.0));
            }
            else
            {
                Item debris = new WoodKeyDebris();
                debris.Movable = false;
                debris.MoveToWorld(new Point3D(base.Location), Map);

                switch (Utility.Random(3))
                {
                    case 0:
                    {
                        BarrelWrath.DoRandomWrath(debris);
                        break;
                    }
                    case 1:
                    {
                        BarrelPoisonWrath.DoRandomWrath(debris);
                        break;
                    }
                    case 2:
                    {
                        switch (Utility.Random(8))
                        {
                            case 0:
                            {
                                new SeaSnake().MoveToWorld(new Point3D(Location), Map);
                                break;
                            }
                            case 1:
                            {
                                new ShipRat().MoveToWorld(new Point3D(Location), Map);
                                break;
                            }
                            case 2:
                            {
                                new ShipBat().MoveToWorld(new Point3D(Location), Map);
                                break;
                            }
                            case 3:
                            {
                                new ShipBat().MoveToWorld(new Point3D(Location), Map);
                                break;
                            }
                            case 4:
                            {
                                new ShipRat().MoveToWorld(new Point3D(Location), Map);
                                break;
                            }
                            case 5:
                            {
                                new SeaSnake().MoveToWorld(new Point3D(Location), Map);
                                break;
                            }
                        }
                    }
                        break;
                }

                if (Utility.RandomDouble() < 0.05)
                    new BarrelHoops().MoveToWorld(new Point3D(Location), Map);
                if (Utility.RandomDouble() < 0.05)
                    new BarrelStaves().MoveToWorld(new Point3D(Location), Map);
                if (Utility.RandomDouble() < 0.05)
                    new BarrelLid().MoveToWorld(new Point3D(Location), Map);
                if (Utility.RandomDouble() < 0.05)
                    new CopperWire().MoveToWorld(new Point3D(Location), Map);
            }

            return true;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1); //version

            writer.Write((int)m_key);
            writer.Write(StorageLocker);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                {
                    m_key = (Parts)reader.ReadInt();
                    StorageLocker = (StorageLocker)reader.ReadItem();

                    break;
                }
            }
        }
    }

    public class WoodenToMetalBarrel : DamageableItem
    {
        public override int LabelNumber => 1023703; // barrel

        [CommandProperty(AccessLevel.GameMaster)]
        public StorageLocker StorageLocker { get; set; }

        [Constructable]
        public WoodenToMetalBarrel(StorageLocker item)
            : base(0x0FAE, 0x0FAE)
        {
            Name = "barrel";

            Level = ItemLevel.VeryEasy;
            Movable = false;
            HitsMax = 60;
            StorageLocker = item;
        }

        public WoodenToMetalBarrel(Serial serial)
            : base(serial)
        {
        }

        public override bool OnBeforeDestroyed()
        {
            Item barrel = new MetalBarrel();

            StorageLocker.Barrels.Add(barrel);
            barrel.MoveToWorld(new Point3D(Location), Map);

            return true;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1); //version

            writer.Write(StorageLocker);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                {
                    StorageLocker = (StorageLocker)reader.ReadItem();

                    break;
                }
            }
        }
    }

    public class MetalBarrel : Item
    {
        public override int LabelNumber => 1023703; // barrel

        [Constructable]
        public MetalBarrel()
            : base(0x0FAE)
        {
            Movable = false;
            Hue = 2301;
        }

        public MetalBarrel(Serial serial)
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
    }

    public class WoodKeyDebris : Item
    {
        [Constructable]
        public WoodKeyDebris()
            : base(0x0C2F)
        {
            Movable = false;
            new InternalTimer(this).Start();
        }

        public WoodKeyDebris(Serial serial)
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

            new InternalTimer(this).Start();
        }

        private class InternalTimer : Timer
        {
            private readonly Item m_Item;

            public InternalTimer(Item item) : base(TimeSpan.FromMinutes(30))
            {
                m_Item = item;
                Priority = TimerPriority.FiveSeconds;
            }

            protected override void OnTick()
            {
                m_Item.Delete();
                Stop();
            }
        }
    }

    public class MetalBarrelWrath
    {
        public static void DoRandomWrath(Item source)
        {
            if (source.Deleted)
                return;

            var randomrange = Utility.RandomMinMax(4, 7);
            var mindmg = 60 + Utility.Random(60) - randomrange * 10;
            if (mindmg < 0) mindmg = 1;
            var maxdmg = mindmg += Utility.Random(200);

            DoWrath(source, mindmg, maxdmg, randomrange);
        }

        public static void DoWrath(Item source, int mindmg, int maxdmg, int range)
        {
            if (source.Deleted)
                return;

            var map = source.Map;

            if (map != null)
                for (var x = -range; x <= range; ++x)
                for (var y = -range; y <= range; ++y)
                {
                    var dist = Math.Sqrt(x * x + y * y);

                    if (dist <= range)
                        new BarrelExplodeWrathTimer(map, source.X + x, source.Y + y, mindmg, maxdmg, source).Start();
                }
        }
    }

    public class BarrelWrath
    {
        public static void DoRandomWrath(Item source)
        {
            if (source.Deleted)
                return;

            var randomrange = Utility.RandomMinMax(2, 4);
            var mindmg = 30 + Utility.Random(30) - randomrange * 10;
            if (mindmg < 0) mindmg = 1;
            var maxdmg = mindmg += Utility.Random(100);

            DoWrath(source, mindmg, maxdmg, randomrange);
        }

        public static void DoWrath(Item source, int mindmg, int maxdmg, int range)
        {
            if (source.Deleted)
                return;

            var map = source.Map;

            if (map != null)
                for (var x = -range; x <= range; ++x)
                for (var y = -range; y <= range; ++y)
                {
                    var dist = Math.Sqrt(x * x + y * y);

                    if (dist <= range)
                        new BarrelExplodeWrathTimer(map, source.X + x, source.Y + y, mindmg, maxdmg, source).Start();
                }
        }
    }

    public class BarrelExplodeWrathTimer : Timer
    {
        private readonly Map n_Map;
        private readonly int n_MaxDamage;
        private readonly int n_MinDamage;
        private readonly Item n_SourceItem;
        private readonly int n_X;
        private readonly int n_Y;

        public BarrelExplodeWrathTimer(Map map, int x, int y, int mindamage, int maxdamage, Item sourceitem) : base(
            TimeSpan.FromSeconds(0))
        {
            n_Map = map;
            n_X = x;
            n_Y = y;
            n_MinDamage = mindamage;
            n_MaxDamage = maxdamage;
            n_SourceItem = sourceitem;
        }

        protected override void OnTick()
        {
            var z = n_Map.GetAverageZ(n_X, n_Y);
            var canFit = n_Map.CanFit(n_X, n_Y, z, 6, false, false);

            for (var i = -3; !canFit && i <= 3; ++i)
            {
                canFit = n_Map.CanFit(n_X, n_Y, z + i, 6, false, false);

                if (canFit)
                    z += i;
            }

            if (!canFit)
                return;

            var g = n_SourceItem;

            // Explosion
            if (g == null || g.Deleted)
                return;

            Effects.SendLocationParticles(EffectItem.Create(g.Location, g.Map, EffectItem.DefaultDuration), 0x36BD, 9,
                10, 5044);
            Effects.PlaySound(g, g.Map, 0x307);
            DoDamage(g, n_MinDamage, n_MaxDamage);
        }

        public virtual void DoDamage(Item g, int mindmg, int maxdmg)
        {
            IPooledEnumerable eable = g.GetMobilesInRange(4);
            foreach (Mobile m in eable)
                if (m != null)
                    if (m.Alive && m is PlayerMobile && m.AccessLevel == AccessLevel.Player)
                    {
                        m.DoHarmful(m);
                        m.FixedParticles(0x376A, 1, 3, 5052, EffectLayer.Waist);
                        m.PublicOverheadMessage(MessageType.Regular, 0x3B2,
                            1154447); // *The barrel explodes sending deadly debris hurdling in your direction!*
                        m.Damage(Utility.RandomMinMax(mindmg, maxdmg), m);
                        Effects.PlaySound(g, g.Map, 0x307);
                    }

            eable.Free();

            n_SourceItem.Delete();
        }
    }

    public class BarrelPoisonWrath
    {
        public static void DoRandomWrath(Item source)
        {
            if (source.Deleted)
                return;

            var randomrange = Utility.RandomMinMax(2, 4);
            var mindmg = 0;
            var maxdmg = 0;

            DoWrath(source, mindmg, maxdmg, randomrange);
        }

        public static void DoWrath(Item source, int mindmg, int maxdmg, int range)
        {
            if (source.Deleted)
                return;

            var map = source.Map;

            if (map != null)
                for (var x = -range; x <= range; ++x)
                for (var y = -range; y <= range; ++y)
                {
                    var dist = Math.Sqrt(x * x + y * y);

                    if (dist <= range)
                        new BarrelPoisonWrathTimer(map, source.X + x, source.Y + y, mindmg, maxdmg, source).Start();
                }
        }
    }

    public class BarrelPoisonWrathTimer : Timer
    {
        private readonly Map n_Map;
        private readonly int n_MaxDamage;
        private readonly int n_MinDamage;
        private readonly Item n_SourceItem;
        private readonly int n_X;
        private readonly int n_Y;

        public BarrelPoisonWrathTimer(Map map, int x, int y, int mindamage, int maxdamage, Item sourceitem) : base(
            TimeSpan.FromSeconds(0))
        {
            n_Map = map;
            n_X = x;
            n_Y = y;
            n_MinDamage = mindamage;
            n_MaxDamage = maxdamage;
            n_SourceItem = sourceitem;
        }

        protected override void OnTick()
        {
            var z = n_Map.GetAverageZ(n_X, n_Y);
            var canFit = n_Map.CanFit(n_X, n_Y, z, 6, false, false);

            for (var i = -3; !canFit && i <= 3; ++i)
            {
                canFit = n_Map.CanFit(n_X, n_Y, z + i, 6, false, false);

                if (canFit)
                    z += i;
            }

            if (!canFit)
                return;

            var g = n_SourceItem;

            //Poisonous Vapor
            if (g == null || g.Deleted)
                return;

            Effects.SendLocationParticles(EffectItem.Create(g.Location, g.Map, EffectItem.DefaultDuration), 0x376A, 9,
                10, 5040);
            Effects.PlaySound(g, g.Map, 0x474);
            DoDamage(g, n_MinDamage, n_MaxDamage);
        }

        public virtual void DoDamage(Item g, int mindmg, int maxdmg)
        {
            if (g == null || g.Deleted)
                return;

            IPooledEnumerable eable = g.GetMobilesInRange(4);
            foreach (Mobile m in eable)
                if (m != null)
                    if (m.Alive && m is PlayerMobile && m.AccessLevel == AccessLevel.Player)
                    {
                        m.DoHarmful(m);
                        m.Damage(Utility.RandomMinMax(mindmg, maxdmg), m);
                        m.PublicOverheadMessage(MessageType.Regular, 0x3B2,
                            1154446); // *Poisonous gas escapes from the ruptured barrel enveloping you in a noxious cloud!*
                        m.ApplyPoison(m, Poison.Deadly);
                        m.PlaySound(0x474);
                    }

            eable.Free();
            g.Delete();
        }
    }
}
