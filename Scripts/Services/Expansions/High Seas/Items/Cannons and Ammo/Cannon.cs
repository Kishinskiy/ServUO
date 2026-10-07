using System;
using System.Collections.Generic;
using System.Linq;
using Server.ContextMenus;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;
using Server.Regions;
using Server.Targeting;

namespace Server.Items
{
    public enum ShipPosition
    {
        Bow,
        BowPort,
        BowStarboard,
        AmidShipPort,
        AmidShipStarboard,
        AftPort,
        AftStarboard,
        Aft
    }

    public interface IShipCannon : IEntity
    {
        int Hits { get; set; }
        int Range { get; }
        AmmunitionType AmmoType { get; set; }
        BaseGalleon Galleon { get; set; }
        DamageLevel DamageState { get; set; }
        Direction Facing { get; }
        ShipPosition Position { get; set; }
        ShipCannonDeed GetDeed { get; }
        bool CanLight { get; }

        Direction GetFacing();
        void OnDamage(int damage, Mobile shooter);
        void LightFuse(Mobile from);
        void Shoot(object cannoneer);
        void DoAreaMessage(int cliloc, int range, Mobile from);
    }

    public abstract class BaseCannon : Item, IShipCannon
    {
        private int m_Cleansliness;
        private int m_Hits;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Cleaned { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Charged { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Primed { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Type LoadedAmmo { get; set; }

        public virtual bool HitMultipleMobs => false;
        public virtual int MaxHits => 100;
        public virtual TimeSpan ActionTime => TimeSpan.FromSeconds(1.5);

        public virtual Type[] LoadTypes => null;

        [CommandProperty(AccessLevel.GameMaster)]
        public double Durability => m_Hits / (double)MaxHits * 100.0;

        public override bool ForceShowProperties => true;

        public override bool HandlesOnMovement => true;

        public Dictionary<Mobile, List<int>> Actions { get; } = new Dictionary<Mobile, List<int>>();

        public BaseCannon(BaseGalleon galleon)
        {
            Movable = false;
            Cleaned = true;
            Charged = false;
            Primed = false;
            Galleon = galleon;
            AmmoType = AmmunitionType.Empty;
            m_Hits = MaxHits;
            DamageState = DamageLevel.Pristine;
        }

        public BaseCannon(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Hits
        {
            get => m_Hits;
            set
            {
                m_Hits = value;
                InvalidateDamageState();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseGalleon Galleon { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public AmmunitionType AmmoType { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public ShipPosition Position { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DamageLevel DamageState { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Direction Facing => GetFacing();

        public virtual ShipCannonDeed GetDeed => null;

        public virtual int Range => 0;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool CanLight => Cleaned && Charged && Primed && AmmoType != AmmunitionType.Empty && LoadedAmmo != null;

        public Direction GetFacing()
        {
            if (ItemID == 16918 || ItemID == 16922)
                return Direction.South;
            if (ItemID == 16919 || ItemID == 16923)
                return Direction.West;
            if (ItemID == 16920 || ItemID == 16924)
                return Direction.North;
            if (ItemID == 16921 || ItemID == 16925)
                return Direction.East;
            return Direction.North;
        }

        public void DoAreaMessage(int cliloc, int range, Mobile from)
        {
            if (from == null)
                return;

            IPooledEnumerable eable = GetMobilesInRange(6);
            foreach (Mobile mob in eable)
                if (mob is PlayerMobile && mob.InLOS(this))
                {
                    if (from != null)
                        mob.SendLocalizedMessage(cliloc, from.Name);
                    else
                        mob.SendLocalizedMessage(cliloc);
                }

            eable.Free();
        }

        public void LightFuse(Mobile from)
        {
            if (!CheckRegion(from))
                return;

            switch (DamageState)
            {
                case DamageLevel.Pristine:
                    break;
                case DamageLevel.Severely:
                    from.SendLocalizedMessage(
                        1116608); //The cannon is severely damaged. It needs major repairs before it is safe to fire.
                    return;
                case DamageLevel.Heavily:
                case DamageLevel.Moderately:
                    from.SendLocalizedMessage(
                        1116607); //The cannon is moderately damaged and needs some repairs. This cannon is safe to fire.
                    break;
                default:
                    from.SendLocalizedMessage(1116606); //The cannon is lightly damaged and needs some minor repair.
                    break;
            }

            DoAreaMessage(1116080, 10, from);
            AddAction(from, 1149683); //The fuse is lit!
            Effects.PlaySound(Location, Map, 0x666);
            Timer.DelayCall(TimeSpan.FromSeconds(2), new TimerStateCallback(Shoot), from);
        }

        public virtual void Shoot(object cannoneer)
        {
            var ammo = AmmoInfo.GetAmmoInfo(LoadedAmmo);

            if (ammo == null)
                return;

            Mobile shooter = null;

            if (cannoneer is Mobile)
                shooter = (Mobile)cannoneer;

            if (shooter != null && shooter.Player)
                m_Hits -= Utility.RandomMinMax(0, 4);

            DoShootEffects();
            AddAction(shooter, 1149691); //Fired successfully.

            var xOffset = 0;
            var yOffset = 0;
            var currentRange = 0;
            var pnt = Location;
            var map = Map;
            var d = GetFacing();

            switch (d)
            {
                case Direction.North:
                    xOffset = 0;
                    yOffset = -1;
                    break;
                case Direction.South:
                    xOffset = 0;
                    yOffset = 1;
                    break;
                case Direction.West:
                    xOffset = -1;
                    yOffset = 0;
                    break;
                case Direction.East:
                    xOffset = 1;
                    yOffset = 0;
                    break;
            }

            var xo = xOffset;
            var yo = yOffset;
            var lateralOffset = 1;

            var latDist = ammo.LateralOffset;

            var range = Range;
            var hit = false;

            while (currentRange++ <= range)
            {
                xOffset = xo;
                yOffset = yo;

                if (currentRange % latDist == 0)
                    lateralOffset++;

                var delay = TimeSpan.FromSeconds(currentRange / 10.0);
                var ammoType = LoadedAmmo;

                switch (AmmoType)
                {
                    case AmmunitionType.Empty: break;
                    case AmmunitionType.Cannonball:
                    {
                        var newPoint = pnt;
                        var list = new List<IEntity>();
                        var mobs = new List<Mobile>();

                        for (var i = -lateralOffset; i <= lateralOffset; i++)
                        {
                            if (xOffset == 0)
                                newPoint = new Point3D(pnt.X + xOffset + i, pnt.Y + yOffset * currentRange, pnt.Z);
                            else
                                newPoint = new Point3D(pnt.X + xOffset * currentRange, pnt.Y + yOffset + i, pnt.Z);

                            //For Testing
                            /*if (i == -lateralOffset || i == lateralOffset)
                            {
                                Effects.SendLocationParticles(EffectItem.Create(newPoint, this.Map, EffectItem.DefaultDuration), 0x3709, 10, 30, 5052);
                            }*/

                            var g = FindValidBoatTarget(newPoint, map, ammo);

                            if (g != null && g != Galleon && g.IsEnemy(Galleon))
                                list.Add(g);

                            mobs.AddRange(FindMobiles(shooter, newPoint, map, false, false, false, true));
                        }

                        foreach (var m in mobs)
                            list.Add(m);

                        if (list.Count > 0)
                        {
                            var toHit = list[Utility.Random(list.Count)];

                            if (toHit is Mobile)
                            {
                                Timer.DelayCall(delay, new TimerStateCallback(OnMobileHit),
                                    new object[] { mobs, newPoint, ammo, shooter });
                                hit = true;
                            }
                            else if (toHit is BaseGalleon)
                            {
                                Timer.DelayCall(delay, new TimerStateCallback(OnShipHit),
                                    new object[] { (BaseGalleon)toHit, newPoint, ammo, shooter });
                                hit = true;
                            }
                        }
                    }
                        break;
                    case AmmunitionType.Grapeshot:
                    {
                        var newPoint = pnt;
                        var mobiles = new List<Mobile>();

                        for (var i = -lateralOffset; i <= lateralOffset; i++)
                        {
                            if (xOffset == 0)
                                newPoint = new Point3D(pnt.X + xOffset + i, pnt.Y + yOffset * currentRange, pnt.Z);
                            else
                                newPoint = new Point3D(pnt.X + xOffset * currentRange, pnt.Y + yOffset + i, pnt.Z);

                            mobiles.AddRange(FindMobiles(shooter, newPoint, map, true, true, true, true));

                            //For Testing
                            /*if (i == -lateralOffset || i == lateralOffset)
                            {
                                Effects.SendLocationParticles(EffectItem.Create(newPoint, this.Map, EffectItem.DefaultDuration), 0x3709, 10, 30, 5052);
                            }*/
                        }

                        if (mobiles.Count > 0)
                        {
                            Timer.DelayCall(delay, new TimerStateCallback(OnMobileHit),
                                new object[] { mobiles, newPoint, ammo, shooter });
                            hit = true;
                        }
                    }
                        break;
                }

                if (hit && ammo.SingleTarget)
                    break;
            }

            ClearCannon();
            InvalidateDamageState();

            if (shooter != null && shooter.HasGump(typeof(CannonGump)))
                ResendGump(shooter);
        }

        public void OnDamage(int damage, Mobile from)
        {
            m_Hits -= damage;
            InvalidateDamageState(from);
        }

        public override void Delete()
        {
            if (Galleon != null)
                Galleon.RemoveCannon(this);

            base.Delete();
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            var g = m.FindGump(typeof(CannonGump));

            if (g != null && g is CannonGump && ((CannonGump)g).Cannon == this && !m.InRange(Location, 3))
                m.CloseGump(typeof(CannonGump));
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(Location, 3))
            {
                from.SendLocalizedMessage(1149687); //You are too far away.
            }
            else if (Galleon.GetSecurityLevel(from) >= SecurityLevel.Crewman)
            {
                ResendGump(from);

                switch (DamageState)
                {
                    case DamageLevel.Slightly:
                        from.SendLocalizedMessage(1116606);
                        break; //The cannon is lightly damaged and needs some minor repair.
                    case DamageLevel.Moderately:
                        from.SendLocalizedMessage(1116607);
                        break; //The cannon is moderately damaged and needs some repairs. This cannon is safe to fire.
                    case DamageLevel.Severely:
                        from.SendLocalizedMessage(1116608);
                        break; //The cannon is severely damaged. It needs major repairs before it is safe to fire.
                }
            }
            else
            {
                from.SendMessage("Only the ship crew can operate the cannon!");
            }
        }

        public virtual int GetDamage(AmmoInfo info)
        {
            return Utility.RandomMinMax(info.MinDamage, info.MaxDamage);
        }

        public virtual bool TryLoadAmmo(Item ammo)
        {
            return false;
        }

        public void TryLightFuse(Mobile from)
        {
            if (from == null)
                return;

            var pack = from.Backpack;

            if (pack != null)
            {
                var items = pack.FindItemsByType(typeof(Matches));

                if (items != null)
                    foreach (var item in items)
                        if (item is Matches && ((Matches)item).IsLight)
                        {
                            LightFuse(from);
                            return;
                        }

                items = pack.FindItemsByType(typeof(Torch));

                if (items != null)
                    foreach (var item in items)
                        if (item is Torch && ((Torch)item).Burning)
                        {
                            LightFuse(from);
                            return;
                        }
            }

            var i = from.FindItemOnLayer(Layer.TwoHanded);

            if (i != null && i is Matches && ((Matches)i).IsLight)
            {
                LightFuse(from);
                return;
            }

            if (i != null && i is Torch && ((Torch)i).Burning)
            {
                LightFuse(from);
                return;
            }

            AddAction(from, 1149669); //Need a lighted match.
        }

        public bool CheckRegion(Mobile from)
        {
            var r = Region.Find(from.Location, from.Map);

            if (r is GuardedRegion && !((GuardedRegion)r).IsDisabled())
            {
                from.SendMessage("You are forbidden from discharging cannons within the town limits.");
                return false;
            }

            return true;
        }

        private BaseGalleon FindValidBoatTarget(Point3D newPoint, Map map, AmmoInfo info)
        {
            var galleon = BaseGalleon.FindGalleonAt(newPoint, map);

            if (galleon != null && info.RequiresSurface)
            {
                var d = galleon is BritannianShip ? 3 : 2;
                switch (galleon.Facing)
                {
                    case Direction.North:
                    case Direction.South:
                        if (newPoint.X <= galleon.X - d || newPoint.X >= galleon.X + d)
                            return null;
                        break;
                    case Direction.East:
                    case Direction.West:
                        if (newPoint.Y <= galleon.Y - d || newPoint.Y >= galleon.Y + d)
                            return null;
                        break;
                }

                var tiles = map.Tiles.GetStaticTiles(newPoint.X, newPoint.Y, true);

                foreach (var tile in tiles)
                {
                    var id = TileData.ItemTable[tile.ID & TileData.MaxItemValue];
                    var isWater = tile.ID >= 0x1796 && tile.ID <= 0x17B2;

                    if (!isWater && id.Surface && !id.Impassable) return galleon;
                }

                return null;
            }

            return galleon;
        }

        public void DoShootEffects()
        {
            var p = Location;
            var map = Map;

            p.Z -= 3;

            switch (Facing)
            {
                case Direction.North: p.Y--; break;
                case Direction.East: p.X++; break;
                case Direction.South: p.Y++; break;
                case Direction.West: p.X--; break;
            }

            Effects.SendLocationEffect(p, map, 14120, 15, 10);
            Effects.PlaySound(p, map, 0x664);
        }

        public void InvalidateDamageState()
        {
            InvalidateDamageState(null);
        }

        public void InvalidateDamageState(Mobile from)
        {
            if (Durability >= 100)
                DamageState = DamageLevel.Pristine;
            else if (Durability >= 75.0)
                DamageState = DamageLevel.Slightly;
            else if (Durability >= 50.0)
                DamageState = DamageLevel.Moderately;
            else if (Durability >= 25.0)
                DamageState = DamageLevel.Heavily;
            else
                DamageState = DamageLevel.Severely;

            if (Durability <= 0)
            {
                DoAreaMessage(1116297, 5, null); //The ship cannon has been destroyed!
                Delete();

                if (from != null && from.InRange(Location, 5))
                    from.SendLocalizedMessage(1116297); //The ship cannon has been destroyed!
            }

            InvalidateProperties();
        }

        public void CheckDirty()
        {
            if (m_Cleansliness++ >= 10)
                Cleaned = false;
        }

        public void ClearCannon()
        {
            CheckDirty();
            Charged = false;
            Primed = false;
            AmmoType = AmmunitionType.Empty;
            LoadedAmmo = null;
            InvalidateProperties();
        }

        public virtual void OnShipHit(object obj)
        {
            var list = (object[])obj;
            var target = list[0] as BaseBoat;
            var pnt = (Point3D)list[1];
            var ammoInfo = list[2] as AmmoInfo;
            var shooter = list[3] as Mobile;

            if (target != null && Galleon != null)
            {
                var damage = (int)(GetDamage(ammoInfo) * Galleon.CannonDamageMod);
                damage /= 7;
                target.OnTakenDamage(shooter, damage);

                var z = target.ZSurface;

                if (target.TillerMan != null && target.TillerMan is IEntity) z = ((IEntity)target.TillerMan).Z;

                var d = Utility.GetDirection(this, pnt);
                var xOffset = 0;
                var yOffset = 0;
                var hit = pnt;

                if (!ammoInfo.RequiresSurface)
                    switch (d)
                    {
                        default:
                        case Direction.North:
                            xOffset = Utility.RandomMinMax(-1, 1);
                            yOffset = Utility.RandomMinMax(-2, 0);
                            hit = new Point3D(pnt.X + xOffset, pnt.Y + yOffset, z);
                            break;
                        case Direction.South:
                            xOffset = Utility.RandomMinMax(-1, 1);
                            yOffset = Utility.RandomMinMax(0, 2);
                            hit = new Point3D(pnt.X + xOffset, pnt.Y + yOffset, z);
                            break;
                        case Direction.East:
                            xOffset = Utility.RandomMinMax(0, 2);
                            yOffset = Utility.RandomMinMax(-1, 1);
                            hit = new Point3D(pnt.X + xOffset, pnt.Y + yOffset, z);
                            break;
                        case Direction.West:
                            xOffset = Utility.RandomMinMax(-2, 0);
                            yOffset = Utility.RandomMinMax(-1, 1);
                            hit = new Point3D(pnt.X + xOffset, pnt.Y + yOffset, z);
                            break;
                    }

                Effects.SendLocationEffect(hit, target.Map, Utility.RandomBool() ? 14000 : 14013, 15, 10);
                Effects.PlaySound(hit, target.Map, 0x207);

                var victim = target.Owner;

                if (victim != null && target.Contains(victim) && shooter.CanBeHarmful(victim, false))
                {
                    shooter.DoHarmful(victim);
                }
                else
                {
                    var candidates = new List<Mobile>();
                    var highest = SecurityLevel.Passenger;

                    foreach (var mob in target.GetMobilesOnBoard().OfType<PlayerMobile>()
                                 .Where(pm => shooter.CanBeHarmful(pm, false)))
                        if (Galleon.GetSecurityLevel(mob) > highest)
                            candidates.Insert(0, mob);
                        else
                            candidates.Add(mob);

                    if (candidates.Count > 0)
                        shooter.DoHarmful(candidates[0]);
                    else if (victim != null && shooter.IsHarmfulCriminal(victim))
                        shooter.CriminalAction(false);

                    ColUtility.Free(candidates);
                }

                if (Galleon.Map != null)
                {
                    IPooledEnumerable eable = Galleon.Map.GetItemsInRange(hit, 1);

                    foreach (Item item in eable)
                        if (item is BaseCannon && !Galleon.Contains(item))
                            ((BaseCannon)item).OnDamage(damage, shooter);

                    eable.Free();
                }
            }
        }

        public virtual void OnMobileHit(object obj)
        {
            var objects = (object[])obj;
            var mobsToHit = objects[0] as List<Mobile>;
            var pnt = (Point3D)objects[1];
            var info = objects[2] as AmmoInfo;
            var shooter = objects[3] as Mobile;

            var damage = (int)(Utility.RandomMinMax(info.MinDamage, info.MaxDamage) * Galleon.CannonDamageMod);

            if (info == null)
                return;

            Mobile toHit = null;

            if (!info.SingleTarget)
            {
                foreach (var mob in mobsToHit)
                {
                    toHit = mob;

                    if (toHit is BaseSeaChampion && info.AmmoType != AmmunitionType.Empty &&
                        info.AmmoType == AmmunitionType.Cannonball)
                        damage *= 100;

                    shooter.DoHarmful(toHit);
                    AOS.Damage(toHit, shooter, damage, info.PhysicalDamage, info.FireDamage, info.ColdDamage,
                        info.PoisonDamage, info.EnergyDamage);
                    Effects.SendLocationEffect(toHit.Location, toHit.Map, Utility.RandomBool() ? 14000 : 14013, 15, 10);
                    Effects.PlaySound(toHit.Location, toHit.Map, 0x207);
                }
            }
            else
            {
                toHit = mobsToHit[Utility.Random(mobsToHit.Count)];

                if (toHit != null)
                {
                    //only cannonballs will get the damage bonus
                    if (toHit is BaseSeaChampion && info.AmmoType != AmmunitionType.Empty &&
                        info.AmmoType == AmmunitionType.Cannonball)
                        damage *= 75;

                    shooter.DoHarmful(toHit);
                    AOS.Damage(toHit, shooter, damage, info.PhysicalDamage, info.FireDamage, info.ColdDamage,
                        info.PoisonDamage, info.EnergyDamage);
                    Effects.SendLocationEffect(toHit.Location, toHit.Map, Utility.RandomBool() ? 14000 : 14013, 15, 10);
                    Effects.PlaySound(toHit.Location, toHit.Map, 0x207);
                }
            }
        }

        public List<Mobile> FindMobiles(Mobile shooter, Point3D pnt, Map map, bool player, bool pet, bool monsters,
            bool seacreature)
        {
            var list = new List<Mobile>();

            if (map == null || map == Map.Internal || Galleon == null)
                return list;

            IPooledEnumerable eable = map.GetMobilesInRange(pnt, 0);

            foreach (Mobile mob in eable)
            {
                if (!shooter.CanBeHarmful(mob, false) || Galleon.Contains(mob))
                    continue;

                if (player && mob is PlayerMobile)
                    list.Add(mob);

                if (monsters && mob is BaseCreature && !((BaseCreature)mob).Controlled && !((BaseCreature)mob).Summoned)
                    list.Add(mob);

                if (pet && mob is BaseCreature && (((BaseCreature)mob).Controlled || ((BaseCreature)mob).Summoned))
                    list.Add(mob);

                if (seacreature && mob is BaseSeaChampion)
                    list.Add(mob);
            }

            eable.Free();

            return list;
        }

        public void TryRepairCannon(Mobile from)
        {
            var pack = from.Backpack;
            Container hold = Galleon.GalleonHold;

            if (pack == null)
                return;

            var ingotsNeeded = 36 * (100 - Durability);

            ingotsNeeded -= from.Skills[SkillName.Blacksmith].Value / 200.0 * ingotsNeeded;

            var min = ingotsNeeded / 10;
            double ingots1 = pack.GetAmount(typeof(IronIngot));
            double ingots2 = hold != null ? hold.GetAmount(typeof(IronIngot)) : 0;
            var ingots = ingots1 + ingots2;
            double ingotsUsed, percRepaired;

            if (ingots < min)
            {
                from.SendLocalizedMessage(1116603,
                    ((int)min).ToString()); //You need a minimum of ~1_METAL~ iron ingots to repair this cannon.
                return;
            }

            if (ingots >= ingotsNeeded)
            {
                ingotsUsed = ingotsNeeded;
                percRepaired = 100;
            }
            else
            {
                ingotsUsed = ingots;
                percRepaired = ingots / ingotsNeeded * 100;
            }

            double toConsume = 0;
            var temp = ingotsUsed;

            if (ingotsUsed > 0 && ingots1 > 0)
            {
                toConsume = Math.Min(ingotsUsed, ingots1);
                pack.ConsumeTotal(typeof(IronIngot), (int)toConsume);
                ingotsUsed -= toConsume;
            }

            if (hold != null && ingotsUsed > 0 && ingots2 > 0)
            {
                toConsume = Math.Min(ingotsUsed, ingots2);
                hold.ConsumeTotal(typeof(IronIngot), (int)toConsume);
            }

            m_Hits += (int)((MaxHits - m_Hits) * (percRepaired / 100));
            if (m_Hits > MaxHits) m_Hits = MaxHits;
            InvalidateDamageState();

            percRepaired += Durability;
            if (percRepaired > 100) percRepaired = 100;

            from.SendLocalizedMessage(1116605,
                string.Format("{0}\t{1}", ((int)temp).ToString(),
                    ((int)percRepaired)
                    .ToString())); //You make repairs to the cannon using ~1_METAL~ ingots. The cannon is now ~2_DMGPCT~% repaired.
        }

        public bool VerifyAmmo(Type type)
        {
            foreach (var ammoType in LoadTypes)
                if (type == ammoType || type.IsSubclassOf(ammoType))
                    return true;
            return false;
        }

        public bool TryClean(Mobile from)
        {
            if (Cleaned && m_Cleansliness == 0)
            {
                from.SendLocalizedMessage(1116007); //The cannon is already clean
            }
            else if (CheckForItem(typeof(Swab), from))
            {
                AddAction(from, 1149641); //Cleaning started.
                DoAreaMessage(1116034, 10, from); //~1_NAME~ begins cleaning the cannon with a cannon swab.
                Timer.DelayCall(ActionTime, new TimerStateCallback(Clean), from);
                return true;
            }
            else
            {
                AddAction(from, 1149659);
                from.SendLocalizedMessage(1149659); //You need a swab.
            }

            return false;
        }

        public bool TryCharge(Mobile from)
        {
            var charge = this is LightShipCannon ? typeof(LightPowderCharge) : typeof(HeavyPowderCharge);

            if (Charged)
            {
                from.SendLocalizedMessage(1116012); //The cannon is already charged.
            }
            else if (!Cleaned)
            {
                from.SendMessage("The cannon needs to be cleaned before you can use it again.");
            }
            else if (CheckForItem(charge, from))
            {
                AddAction(from, 1149644); //Charging started.
                DoAreaMessage(1116035, 10, from); //~1_NAME~ begins loading the cannon with a powder charge.
                Timer.DelayCall(ActionTime, new TimerStateCallback(Charge), new object[] { from, charge });
                return true;
            }
            else
            {
                AddAction(from, 1149665); //Need powder charge.
                from.SendLocalizedMessage(1149665);
            }

            return false;
        }

        public bool TryLoad(Mobile from)
        {
            if (!Charged)
            {
                from.SendLocalizedMessage(
                    1116018); //The cannon needs to be charged with a rammer and powder charge before it can be loaded.
            }

            else if (CheckForItem(typeof(Ramrod), from))
            {
                AddAction(from, 1149666); //Select Ammunition
                from.Target = new LoadCannonTarget(this);
                return true;
            }
            else
            {
                AddAction(from, 1149660);
                from.SendLocalizedMessage(1149660); //You need a ramrod.
            }

            return false;
        }

        public bool TryPrime(Mobile from)
        {
            if (Primed)
            {
                from.SendLocalizedMessage(1116019); //The cannon is already primed and ready to be fired.
            }

            else if (!Charged || AmmoType == AmmunitionType.Empty)
            {
                from.SendLocalizedMessage(1116021); //The cannon needs to be charged and loaded before it can be primed.
            }

            else if (CheckForItem(typeof(FuseCord), from))
            {
                AddAction(from, 1149650); //Priming started
                DoAreaMessage(1116038, 10, from); //~1_NAME~ begins priming the cannon with a cannon fuse.
                Timer.DelayCall(ActionTime, new TimerStateCallback(Prime), new object[] { from, typeof(FuseCord) });
                return true;
            }
            else
            {
                AddAction(from, 1149661);
                from.SendLocalizedMessage(1149661); //you need a fuse.
            }

            return false;
        }

        public void DoLoad(Mobile from, Item ammo)
        {
            Timer.DelayCall(ActionTime, new TimerStateCallback(Load), new object[] { from, ammo });
            var cliloc = ammo is ICannonAmmo && ((ICannonAmmo)ammo).AmmoType == AmmunitionType.Cannonball
                ? 1116036
                : 1116037;
            AddAction(from, 1149647); //loading started.
            DoAreaMessage(cliloc, 10, from);
        }

        public void Clean(object state)
        {
            var from = (Mobile)state;

            if (from.InRange(Location, 3))
            {
                Cleaned = true;
                m_Cleansliness = 0;
                AddAction(from, 1149643); //cleaning finished.
                DoAreaMessage(1116060, 10, from); //~1_NAME~ finishes cleaning the cannon.
            }
            else
            {
                AddAction(from, 1149642); //Cleaning canceled.
                DoAreaMessage(1116055, 10, from); //~1_NAME~ cancels the effort of cleaning the cannon.
            }

            if (from.HasGump(typeof(CannonGump)))
                ResendGump(from);

            InvalidateProperties();
        }

        public void Charge(object state)
        {
            var obj = (object[])state;
            var from = obj[0] as Mobile;
            var type = obj[1] as Type;

            if (from.InRange(Location, 3))
            {
                Charged = true;
                AddAction(from, 1149646); //Charging finished.
                DoAreaMessage(1116061, 10, from); //~1_NAME~ finishes charging the cannon.

                if (type != null && from.Backpack != null)
                    from.Backpack.ConsumeTotal(type, 1);
            }
            else
            {
                AddAction(from, 1149645); //Charging canceled.
                DoAreaMessage(1116056, 10,
                    from); //~1_NAME~ cancels the effort of charging the cannon and retrieves the powder charge.
            }

            if (from.HasGump(typeof(CannonGump)))
                ResendGump(from);

            InvalidateProperties();
        }

        public void Prime(object state)
        {
            var obj = (object[])state;
            var from = obj[0] as Mobile;
            var type = obj[1] as Type;

            if (from.InRange(Location, 3))
            {
                Primed = true;
                AddAction(from, 1149652); //Ready to Fire
                DoAreaMessage(1116064, 10, from); //~1_NAME~ finishes priming the cannon. It is ready to be fired!

                if (type != null && from.Backpack != null)
                    from.Backpack.ConsumeTotal(type, 1);
            }
            else
            {
                AddAction(from, 1149651); //Priming Canceled
                DoAreaMessage(1116059, 10,
                    from); //~1_NAME~ cancels the effort of priming the cannon and retrieves the cannon fuse.
            }

            if (from.HasGump(typeof(CannonGump)))
                ResendGump(from);

            InvalidateProperties();
        }

        public void Load(object state)
        {
            var obj = (object[])state;
            var from = obj[0] as Mobile;
            var ammo = obj[1] as Item;
            var cliloc = 1116062;

            if (ammo is ICannonAmmo && ((ICannonAmmo)ammo).AmmoType == AmmunitionType.Grapeshot)
                cliloc = 1116063;

            if (AmmoType != AmmunitionType.Empty)
            {
                AddAction(from, 1149663); //Must unload first.
                from.SendLocalizedMessage(1149663);
            }
            else if (from.InRange(Location, 3))
            {
                if (TryLoadAmmo(ammo) && ammo is ICannonAmmo)
                {
                    AddAction(from, 1149649); //Loading finished
                    DoAreaMessage(cliloc, 10, from); //~1_NAME~ finishes loading the cannon with a cannonball.
                    AmmoType = ((ICannonAmmo)ammo).AmmoType;
                    LoadedAmmo = ammo.GetType();
                    ammo.Consume();
                }
            }
            else
            {
                cliloc = ammo is ICannonAmmo && ((ICannonAmmo)ammo).AmmoType == AmmunitionType.Cannonball
                    ? 1116057
                    : 1116058;
                AddAction(from, 1149648); //Loading canceled.
                DoAreaMessage(cliloc, 10,
                    from); //~1_NAME~ cancels the effort of loading the cannon and retrieves the cannonball.
            }

            if (from.HasGump(typeof(CannonGump)))
                ResendGump(from);

            InvalidateProperties();
        }

        public void RemoveCharge(Mobile from)
        {
            if (from == null || !Charged)
                return;

            var type = this is LightShipCannon ? typeof(LightPowderCharge) : typeof(HeavyPowderCharge);

            var item = Loot.Construct(type);

            if (item != null)
            {
                var pack = from.Backpack;
                if (pack != null || !pack.TryDropItem(from, item, false))
                    item.MoveToWorld(from.Location, from.Map);
            }

            Charged = false;
            AddAction(from, 1149684); //Powder charge removed.
            DoAreaMessage(1116065, 10, from); //~1_NAME~ carefully removes the powder charge from the cannon.

            if (from.HasGump(typeof(CannonGump)))
                ResendGump(from);

            InvalidateProperties();
        }

        public void RemoveLoad(Mobile from)
        {
            if (from == null || AmmoType == AmmunitionType.Empty)
                return;

            if (Charged)
            {
                AddAction(from, 1149662); //Must remove charge first.
            }
            else
            {
                var cliloc = 0;

                switch (AmmoType)
                {
                    case AmmunitionType.Cannonball:
                        cliloc = 1116066; //~1_NAME~ carefully removes the cannonball from the cannon.
                        break;
                    case AmmunitionType.Grapeshot:
                        cliloc = 1116067; //~1_NAME~ carefully removes the grapeshot from the cannon.
                        break;
                }

                var item = Loot.Construct(LoadedAmmo);

                if (item != null)
                {
                    var pack = from.Backpack;
                    if (pack != null || !pack.TryDropItem(from, item, false))
                        item.MoveToWorld(from.Location, from.Map);
                }

                AmmoType = AmmunitionType.Empty;
                AddAction(from, 1149685); //Ammunition removed.
                LoadedAmmo = null;

                if (cliloc > 0)
                    DoAreaMessage(cliloc, 10, from); //~1_NAME~ carefully removes the powder charge from the cannon.
            }

            if (from.HasGump(typeof(CannonGump)))
                ResendGump(from);

            InvalidateProperties();
        }

        public void RemovePrime(Mobile from)
        {
            if (from == null || !Primed)
                return;

            if (AmmoType != AmmunitionType.Empty)
                AddAction(from, 1149663); //Must unload first.

            var item = Loot.Construct(typeof(FuseCord));

            if (item != null)
            {
                var pack = from.Backpack;
                if (pack != null || !pack.TryDropItem(from, item, false))
                    item.MoveToWorld(from.Location, from.Map);
            }

            Primed = false;
            AddAction(from, 1149686); //Fuse removed
            DoAreaMessage(1116068, 10, from); //~1_NAME~ carefully removes the cannon fuse from the cannon.

            if (from.HasGump(typeof(CannonGump)))
                ResendGump(from);

            InvalidateProperties();
        }

        public static bool CheckForItem(Type type, Mobile toCheck)
        {
            var pack = toCheck.Backpack;

            if (pack != null)
                return pack.GetAmount(type) >= 1;
            return false;
        }

        public void ResendGump(Mobile from)
        {
            from.CloseGump(typeof(CannonGump));
            from.SendGump(new CannonGump(this, from));
        }

        public void AddAction(Mobile from, int cliloc)
        {
            if (from == null)
                return;

            if (!Actions.ContainsKey(from))
                Actions[from] = new List<int>();

            var list = Actions[from];

            if (list.Count == 0 || list[list.Count - 1] != cliloc)
                list.Add(cliloc);
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1116025, string.Format("#{0}", Cleaned ? 1116031 : 1116032)); //Cleaned: ~1_VALUE~
            list.Add(1116026, string.Format("#{0}", Charged ? 1116031 : 1116032)); //Charged: ~1_VALUE~
            list.Add(1116027, AmmoInfo.GetAmmoName(this).ToString()); //Ammo: ~1_VALUE~
            list.Add(1116028, string.Format("#{0}", Primed ? 1116031 : 1116032)); //Primed: ~1_VALUE~
            list.Add(1116580 + (int)DamageState);
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (Galleon.GetSecurityLevel(from) >= SecurityLevel.Officer)
            {
                if (!Cleaned)
                    list.Add(new CleanContext(this, from));

                if (!Charged)
                    list.Add(new ChargeContext(this, from));

                if (AmmoType == AmmunitionType.Empty)
                    list.Add(new LoadContext(this, from));

                if (!Primed)
                    list.Add(new PrimeContext(this, from));

                list.Add(new DismantleContext(this, from));
                list.Add(new RepairContext(this, from));
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(2);

            writer.Write(m_Cleansliness);
            writer.Write(LoadedAmmo != null);
            if (LoadedAmmo != null)
                writer.Write(LoadedAmmo.Name);
            writer.Write(Cleaned);
            writer.Write(Charged);
            writer.Write(Primed);
            writer.Write(Galleon);
            writer.Write(m_Hits);
            writer.Write((int)AmmoType);
            writer.Write((int)Position);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            if (version > 1)
            {
                m_Cleansliness = reader.ReadInt();
            }
            else
            {
                Cleaned = true;
                m_Cleansliness = 0;
            }

            if (version > 0)
                if (reader.ReadBool())
                {
                    var name = reader.ReadString();
                    LoadedAmmo = ScriptCompiler.FindTypeByName(name);
                }

            Cleaned = reader.ReadBool();
            Charged = reader.ReadBool();
            Primed = reader.ReadBool();
            Galleon = reader.ReadItem() as BaseGalleon;
            m_Hits = reader.ReadInt();
            AmmoType = (AmmunitionType)reader.ReadInt();
            Position = (ShipPosition)reader.ReadInt();

            if (LoadedAmmo == null && AmmoType != AmmunitionType.Empty)
                AmmoType = AmmunitionType.Empty;

            InvalidateDamageState();

            if (Core.EJ) Timer.DelayCall(() => Replace());
        }

        private void Replace()
        {
            if (Galleon != null && !Galleon.Deleted)
            {
                BaseShipCannon newCannon = null;
                var loc = Location;
                Delete();

                if (this is HeavyShipCannon)
                    newCannon = new Carronade(Galleon);
                else if (this is LightShipCannon) newCannon = new Culverin(Galleon);

                if (newCannon != null)
                    if (!Galleon.TryAddCannon(null, loc, newCannon, null))
                    {
                        var deed = GetDeed;

                        if (deed != null) Galleon.GalleonHold.DropItem(deed);

                        newCannon.Delete();
                    }
            }
            else
            {
                Delete();
            }
        }

        private class CleanContext : ContextMenuEntry
        {
            private readonly BaseCannon m_Cannon;
            private readonly Mobile m_From;

            public CleanContext(BaseCannon cannon, Mobile from) : base(1149626, 3)
            {
                m_From = from;
                m_Cannon = cannon;
            }

            public override void OnClick()
            {
                if (m_Cannon != null)
                    m_Cannon.TryClean(m_From);
            }
        }

        private class ChargeContext : ContextMenuEntry
        {
            private readonly BaseCannon m_Cannon;
            private readonly Mobile m_From;

            public ChargeContext(BaseCannon cannon, Mobile from) : base(1149630, 3)
            {
                m_From = from;
                m_Cannon = cannon;
            }

            public override void OnClick()
            {
                if (m_Cannon != null)
                    m_Cannon.TryCharge(m_From);
            }
        }

        private class LoadContext : ContextMenuEntry
        {
            private readonly BaseCannon m_Cannon;
            private readonly Mobile m_From;

            public LoadContext(BaseCannon cannon, Mobile from)
                : base(1149635, 3)
            {
                m_From = from;
                m_Cannon = cannon;
            }

            public override void OnClick()
            {
                if (m_Cannon != null)
                    m_From.Target = new LoadCannonTarget(m_Cannon);
            }
        }

        private class PrimeContext : ContextMenuEntry
        {
            private readonly BaseCannon m_Cannon;
            private readonly Mobile m_From;

            public PrimeContext(BaseCannon cannon, Mobile from) : base(1149637, 3)
            {
                m_From = from;
                m_Cannon = cannon;
            }

            public override void OnClick()
            {
                if (m_Cannon != null)
                    m_Cannon.TryPrime(m_From);
            }
        }

        private class DismantleContext : ContextMenuEntry
        {
            private readonly BaseCannon m_Cannon;
            private readonly Mobile m_From;

            public DismantleContext(BaseCannon cannon, Mobile from)
                : base(1116069, 3)
            {
                m_From = from;
                m_Cannon = cannon;
            }

            public override void OnClick()
            {
                if (m_Cannon != null)
                {
                    if (!m_Cannon.Cleaned || m_Cannon.Primed || m_Cannon.Charged ||
                        m_Cannon.AmmoType != AmmunitionType.Empty)
                    {
                        m_From.SendLocalizedMessage(
                            1116321); //The ship cannon must be cleaned and fully unloaded before it can be dismantled.
                    }
                    else if (m_Cannon.DamageState != DamageLevel.Pristine)
                    {
                        m_From.SendLocalizedMessage(
                            1116322); //The ship cannon must be fully repaired before it can be dismantled.
                    }
                    else
                    {
                        var deed = m_Cannon.GetDeed;

                        m_Cannon.DoAreaMessage(1116073, 10, m_From); //~1_NAME~ dismantles the ship cannon.
                        m_Cannon.Delete();

                        var pack = m_From.Backpack;
                        if (pack == null || !pack.TryDropItem(m_From, deed, false))
                            deed.MoveToWorld(m_From.Location, m_From.Map);
                    }
                }
            }
        }

        private class RepairContext : ContextMenuEntry
        {
            private readonly BaseCannon m_Cannon;
            private readonly Mobile m_From;

            public RepairContext(BaseCannon cannon, Mobile from)
                : base(1116602, 3)
            {
                m_From = from;
                m_Cannon = cannon;
            }

            public override void OnClick()
            {
                if (m_Cannon.DamageState == DamageLevel.Pristine)
                    m_From.SendLocalizedMessage(
                        1116604); //The cannon is in pristine condition and does not need repairs.
                else
                    m_Cannon.TryRepairCannon(m_From);
            }
        }

        private class LoadCannonTarget : Target
        {
            private readonly BaseCannon m_Cannon;

            public LoadCannonTarget(BaseCannon cannon) : base(3, false, TargetFlags.None)
            {
                m_Cannon = cannon;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Cannon == null || !(targeted is Item))
                    return;

                var item = (Item)targeted;

                if (targeted is Item && m_Cannon.VerifyAmmo(item.GetType()))
                {
                    m_Cannon.DoLoad(from, item);
                }
                else
                {
                    from.SendMessage("You must target the proper ammunition for this type of cannon.");
                    m_Cannon.AddAction(from, 1149667); //Invalid target.
                    from.Target = new LoadCannonTarget(m_Cannon);
                }
            }
        }
    }

    public class LightShipCannon : BaseCannon
    {
        public override int Range => 8;

        public override ShipCannonDeed GetDeed => new LightShipCannonDeed();

        public override Type[] LoadTypes
        {
            get
            {
                return new[]
                {
                    typeof(LightCannonball), typeof(LightGrapeshot),
                    typeof(LightFlameCannonball), typeof(LightFrostCannonball)
                };
            }
        }

        public LightShipCannon(BaseGalleon g) : base(g)
        {
        }

        public LightShipCannon(Serial serial) : base(serial)
        {
        }

        public override bool TryLoadAmmo(Item ammo)
        {
            return ammo is LightCannonball || ammo is LightGrapeshot;
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
        }
    }

    public class HeavyShipCannon : BaseCannon
    {
        public override int Range => 10;
        public override TimeSpan ActionTime => TimeSpan.FromSeconds(2.0);

        public override int LabelNumber => 0;

        public override Type[] LoadTypes
        {
            get
            {
                return new[]
                {
                    typeof(HeavyCannonball), typeof(HeavyGrapeshot),
                    typeof(HeavyFrostCannonball), typeof(HeavyFlameCannonball)
                };
            }
        }

        public HeavyShipCannon(BaseGalleon g) : base(g)
        {
        }

        public HeavyShipCannon(Serial serial) : base(serial)
        {
        }

        public override bool TryLoadAmmo(Item ammo)
        {
            return ammo is HeavyCannonball || ammo is HeavyGrapeshot;
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
        }
    }
}
