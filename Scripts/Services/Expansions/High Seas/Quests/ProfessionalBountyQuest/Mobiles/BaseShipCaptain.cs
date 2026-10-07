using System;
using System.Collections.Generic;
using System.Linq;
using Server.Engines.Quests;
using Server.Items;
using Server.Multis;

namespace Server.Mobiles
{
    public class BaseShipCaptain : BaseCreature
    {
        public static readonly TimeSpan DeleteAfterDeath = TimeSpan.FromMinutes(15);
        public static readonly TimeSpan ResumeTime = TimeSpan.FromSeconds(Utility.RandomMinMax(30, 45));
        public static readonly TimeSpan DecayRetry = TimeSpan.FromSeconds(30);

        private readonly Dictionary<IShipCannon, DateTime> m_ShootTable = new Dictionary<IShipCannon, DateTime>();
        private DateTime m_ActionTime;

        private DateTime m_StopTime;

        private BaseGalleon m_TargetBoat;
        private bool m_WillResume;

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseGalleon Galleon { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool OnCourse { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextCannonShot { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextMoveCheck { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextCrewCheck { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public SpawnZone Zone { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Blockade { get; set; }

        public List<Mobile> Crew { get; } = new List<Mobile>();

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool Aggressive => true;

        public override bool PlayerRangeSensitive => false;

        public override double TreasureMapChance => 0.05;
        public override int TreasureMapLevel => 7;

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual TimeSpan ShootFrequency => TimeSpan.FromSeconds(Math.Min(20, 20.0 - Crew.Count * 2.5));

        [CommandProperty(AccessLevel.GameMaster)]
        public string AddHoldItem
        {
            get => null;
            set
            {
                var str = value;

                var type = ScriptCompiler.FindTypeByName(str);

                if (type != null && (type == typeof(Item) || type.IsSubclassOf(typeof(Item))))
                {
                    var item = Loot.Construct(type);

                    if (item != null)
                        HoldItem = item;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item HoldItem
        {
            get => null;
            set
            {
                var item = value;

                if (item != null)
                    AddItemToHold(item);
            }
        }

        [Constructable]
        public BaseShipCaptain() : this(null)
        {
        }

        public BaseShipCaptain(BaseGalleon galleon)
            : this(galleon, AIType.AI_Melee, FightMode.Weakest, 10, 1, .2, .4)
        {
        }

        public BaseShipCaptain(BaseGalleon galleon, AIType ai, FightMode fm, int per, int range, double passive,
            double active)
            : base(ai, fm, per, range, passive, active)
        {
            Galleon = galleon;
            OnCourse = true;
            m_StopTime = DateTime.MinValue;

            if (Female = Utility.RandomBool())
            {
                Body = 0x191;
                Name = NameList.RandomName("female");
                AddItem(new Skirt(Utility.RandomNeutralHue()));
            }
            else
            {
                Body = 0x190;
                Name = NameList.RandomName("male");
                AddItem(new ShortPants(Utility.RandomNeutralHue()));
            }

            SetStr(500, 750);
            SetDex(125, 175);
            SetInt(61, 75);

            SetHits(4500, 5000);

            SetDamage(23, 35);

            SetSkill(SkillName.Fencing, 115.0, 120.0);
            SetSkill(SkillName.Macing, 115.0, 120.0);
            SetSkill(SkillName.MagicResist, 115.0, 120.0);
            SetSkill(SkillName.Swords, 115.0, 120.0);
            SetSkill(SkillName.Tactics, 115.0, 120.0);
            SetSkill(SkillName.Wrestling, 115.0, 120.0);
            SetSkill(SkillName.Anatomy, 115.0, 120.0);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 45, 55);
            SetResistance(ResistanceType.Fire, 45, 55);
            SetResistance(ResistanceType.Cold, 45, 55);
            SetResistance(ResistanceType.Poison, 45, 55);
            SetResistance(ResistanceType.Energy, 45, 55);

            if (galleon == null)
                Timer.DelayCall(TimeSpan.FromSeconds(.5), SpawnShip);
        }

        public BaseShipCaptain(Serial serial)
            : base(serial)
        {
        }

        public void SpawnShip()
        {
            BaseGalleon gal;

            if (this is PirateCaptain)
                gal = new OrcishGalleon(Direction.North);
            else if (Map == Map.Tokuno)
                gal = new TokunoGalleon(Direction.North);
            else
                gal = new GargishGalleon(Direction.North);

            var p = Location;
            var map = Map;

            // Move this sucka out of the way!
            Internalize();

            if (gal.CanFit(p, map, gal.ItemID))
            {
                gal.Owner = this;
                gal.MoveToWorld(p, map);
                Galleon = gal;

                BountyQuestSpawner.FillHold(Galleon);
                MoveToWorld(new Point3D(p.X, p.Y - 1, gal.Z + gal.ZSurface), map);

                var crewCount = Utility.RandomMinMax(3, 5);

                for (var j = 0; j < crewCount; j++)
                {
                    Mobile crew = new PirateCrew();

                    if (j == 0 && this is PirateCaptain)
                        crew.Title = "the orc captain";

                    AddToCrew(crew);
                    crew.MoveToWorld(
                        new Point3D(gal.X + Utility.RandomList(-1, 1), gal.Y + Utility.RandomList(-1, 0, 1),
                            gal.ZSurface), map);
                }

                gal.AutoAddCannons(this);
            }
            else
            {
                gal.Delete();
                Delete();
            }
        }

        public void OnShipDelete()
        {
            if (Alive && !Deleted)
                Kill();

            for (var i = 0; i < Crew.Count; i++)
            {
                var mob = Crew[i];

                if (mob != null && !mob.Deleted)
                    mob.Kill();
            }
        }

        public override void Delete()
        {
            if (BountyQuestSpawner.Instance != null)
                BountyQuestSpawner.Instance.HandleDeath(this);

            if (Galleon != null && !Galleon.Deleted)
            {
                Galleon.TimeOfDecay = DateTime.UtcNow + TimeSpan.FromMinutes(30);
                Timer.DelayCall(DeleteAfterDeath, new TimerStateCallback(TryDecayGalleon), Galleon);
            }

            base.Delete();
        }

        public override void OnCombatantChange()
        {
            if (Combatant == null)
                CantWalk = true;
            else
                CantWalk = false;

            base.OnCombatantChange();
        }

        public void TryDecayGalleon(object obj)
        {
            var gal = obj as BaseGalleon;

            if (gal == null)
                return;

            if (gal.PlayerCount() > 0)
            {
                Timer.DelayCall(DecayRetry, new TimerStateCallback(TryDecayGalleon), gal);
                return;
            }

            if (gal != null && !gal.Deleted)
                gal.ForceDecay();
        }

        public void AddToCrew(Mobile mob)
        {
            if (!Crew.Contains(mob))
                Crew.Add(mob);
        }

        public void ResumeCourseTimed(TimeSpan ts, bool check)
        {
            if (!m_WillResume)
            {
                Timer.DelayCall(ts, ResumeCourse);
                m_WillResume = true;
            }
        }

        public void ResumeCourse()
        {
            if (Galleon != null)
            {
                Galleon.StartCourse(false, false);
                m_WillResume = false;
                OnCourse = true;
            }
        }

        public override void OnThink()
        {
            base.OnThink();

            if (Galleon == null)
                return;

            if (Galleon.Deleted)
                OnShipDelete();

            // Ship is fucked without his captain!!!
            if (!Galleon.Contains(this))
                return;

            if (NextCrewCheck < DateTime.UtcNow) CheckCrew();

            if (!IsInBattle())
            {
                if (!OnCourse)
                {
                    ResumeCourse();
                }
                else if (OnCourse && !Galleon.IsMoving && m_ActionTime < DateTime.UtcNow)
                {
                    ResumeCourseTimed(ResumeTime, true);
                    m_ActionTime = DateTime.UtcNow + ResumeTime;
                }

                return;
            }

            OnCourse = false;

            var focusMob = GetFocusMob();

            if (m_TargetBoat == null || !InRange(m_TargetBoat.Location, 25))
                m_TargetBoat = GetFocusBoat(focusMob);

            if (focusMob == null && m_TargetBoat == null)
                return;

            if (NextMoveCheck < DateTime.UtcNow && !Galleon.Scuttled && !Blockade)
            {
                var pnt = m_TargetBoat != null ? m_TargetBoat.Location : focusMob.Location;

                var dist = (int)GetDistanceToSqrt(pnt);

                if (!Aggressive && dist < 25)
                {
                    MoveBoat(pnt);
                }
                else if (Aggressive && dist >= 10 && dist <= 35)
                {
                    MoveBoat(pnt);
                }
                else
                {
                    Galleon.StopMove(false);
                    ResumeCourseTimed(TimeSpan.FromMinutes(2), false); //Loiter
                }
            }

            if (m_TargetBoat != null && !m_TargetBoat.Scuttled)
                ShootCannons(focusMob, true);
            else
                ShootCannons(focusMob, false);
        }

        public Mobile GetFocusMob()
        {
            var focus = Combatant as Mobile;

            if (focus == null || focus.Deleted || !focus.Alive)
            {
                var closest = 25;

                foreach (var mob in Crew)
                    if (mob.Alive && mob.Combatant is Mobile)
                        if (focus == null || (int)focus.GetDistanceToSqrt(mob) < closest)
                        {
                            focus = mob.Combatant as Mobile;
                            closest = (int)focus.GetDistanceToSqrt(mob);
                        }
            }

            return focus;
        }

        public BaseGalleon GetFocusBoat(Mobile focusMob)
        {
            if (focusMob == null || focusMob.Deleted || focusMob.Map == null || focusMob.Map == Map.Internal)
                return null;

            var g = BaseGalleon.FindGalleonAt(focusMob, focusMob.Map);

            return g != Galleon ? g : null;
        }

        public void MoveBoat(Point3D p)
        {
            if (Galleon == null || Galleon.Contains(p))
                return;

            var x = p.X;
            var y = p.Y;
            int speed;
            var flee = Aggressive ? 1 : -1;

            //Direction d = Utility.GetDirectionTo(p.X, p.Y);
            var dir = Galleon.GetMovementFor(x, y, out speed);
            var f = Galleon.Facing;

            if (!Aggressive)
                dir = (Direction)(((int)dir + -4) & 0x7);

            if (dir == Direction.West || dir == Direction.Left || dir == Direction.South)
            {
                Galleon.Turn(-2 * flee, true);
                NextMoveCheck = DateTime.UtcNow + TimeSpan.FromSeconds(Galleon.TurnDelay);
                return;
            }

            if (dir == Direction.East || dir == Direction.Down)
            {
                Galleon.Turn(2 * flee, true);
                NextMoveCheck = DateTime.UtcNow + TimeSpan.FromSeconds(Galleon.TurnDelay);
                return;
            }

            Galleon.StartMove(dir, true);
        }

        public void ShootCannons(Mobile focus, bool shootAtBoat)
        {
            var cannons = new List<Item>(Galleon.Cannons.Where(i => !i.Deleted));

            foreach (var cannon in cannons.OfType<IShipCannon>())
                if (cannon != null)
                {
                    if (m_ShootTable.ContainsKey(cannon) && m_ShootTable[cannon] > DateTime.UtcNow)
                        continue;

                    var facing = cannon.GetFacing();

                    if (shootAtBoat && HasTarget(focus, cannon, true))
                        cannon.AmmoType = AmmunitionType.Cannonball;
                    //cannon.LoadedAmmo = cannon.LoadTypes[0];
                    else if (!shootAtBoat && HasTarget(focus, cannon, false))
                        cannon.AmmoType = AmmunitionType.Grapeshot;
                    //cannon.LoadedAmmo = cannon.LoadTypes[1];
                    else
                        continue;

                    cannon.Shoot(this);
                    m_ShootTable[cannon] = DateTime.UtcNow + ShootFrequency +
                                           TimeSpan.FromSeconds(Utility.RandomMinMax(0, 3));
                }
        }

        private bool HasTarget(Mobile focus, IShipCannon cannon, bool shootatboat)
        {
            if (cannon == null || cannon.Deleted || cannon.Map == null || cannon.Map == Map.Internal ||
                Galleon == null || Galleon.Deleted)
                return false;

            var d = cannon.GetFacing();
            var xOffset = 0;
            var yOffset = 0;
            var cannonrange = cannon.Range;
            var currentRange = 0;
            var pnt = cannon.Location;

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

            while (currentRange++ <= cannonrange)
            {
                xOffset = xo;
                yOffset = yo;

                for (var i = -1; i <= 1; i++)
                {
                    Point3D newPoint;

                    if (xOffset == 0)
                        newPoint = new Point3D(pnt.X + xOffset + i, pnt.Y + yOffset * currentRange, pnt.Z);
                    else
                        newPoint = new Point3D(pnt.X + xOffset * currentRange, pnt.Y + yOffset + i, pnt.Z);

                    if (shootatboat)
                    {
                        var g = BaseGalleon.FindGalleonAt(newPoint, Map);

                        if (g != null && g == m_TargetBoat && g != Galleon)
                            return true;
                    }
                    else
                    {
                        if (focus == null)
                            return false;

                        if (newPoint.X == focus.X && newPoint.Y == focus.Y) return true;
                    }
                }
            }

            return false;
        }

        private bool IsInBattle()
        {
            if (Combatant != null)
                return true;

            foreach (var mob in Crew)
                if (mob.Alive && mob.Combatant != null)
                    return true;
            return false;
        }

        public void AddItemToHold(Item item)
        {
            if (item == null)
                return;

            if (Galleon != null && Galleon.GalleonHold != null)
                Galleon.GalleonHold.DropItem(item);
            else
                item.Delete();
        }

        public void CheckCrew()
        {
            if (Galleon == null || Crew == null || Map == null || Map == Map.Internal)
                return;

            var crew = new List<Mobile>(Crew.Where(m =>
                m != null && m.Alive && m.Map != null && m.Map != Map.Internal));

            if (Galleon.GalleonPilot != null)
                crew.Add(Galleon.GalleonPilot);

            crew.Add(this);

            foreach (var crewman in crew)
                if (!Galleon.Contains(crewman))
                    crewman.MoveToWorld(
                        new Point3D(Galleon.X + Utility.RandomList(-1, 1), Galleon.Y + Utility.RandomList(-1, 0, 1),
                            Galleon.ZSurface), Map);

            ColUtility.Free(crew);
            NextCrewCheck = DateTime.UtcNow + TimeSpan.FromMinutes(30);
        }

        public void CheckBlock(StaticTile tile, Point3D p)
        {
            var check = BaseBoat.FindBoatAt(p, Map);

            if (check != null)
                CheckBlock(check, p);
        }

        public void CheckBlock(IEntity e, Point3D loc)
        {
            if (Galleon == null || !OnCourse || IsInBattle()) return;

            if (loc == Point3D.Zero) loc = e.Location;

            var check = BaseBoat.FindBoatAt(new Point3D(e), Map);

            if ((check != null && check != Galleon) || e is BaseCreature)
            {
                var d = Utility.GetDirection(Galleon, e);

                var blockX = e.X;
                var blockY = e.Y;

                var x = Galleon.X;
                var y = Galleon.Y;
                var toMove = Direction.North;

                switch (Galleon.Facing)
                {
                    case Direction.North:
                        toMove = blockX > x ? Direction.West : Direction.East;
                        break;
                    case Direction.East:
                        toMove = blockY > y ? Direction.North : Direction.South;
                        break;
                    case Direction.South:
                        toMove = blockX < x ? Direction.East : Direction.West;
                        break;
                    case Direction.West:
                        toMove = blockY < y ? Direction.South : Direction.North;
                        break;
                }

                Movement.Movement.Offset(toMove, ref x, ref y);

                int speed;
                toMove = Galleon.GetMovementFor(x, y, out speed);

                Timer.DelayCall(TimeSpan.FromSeconds(1), () => { Galleon.StartMove(toMove, false); });

                ResumeCourseTimed(TimeSpan.FromSeconds(11), true);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);

            writer.Write(Blockade);

            writer.Write((int)Zone);
            writer.Write(m_StopTime);
            writer.Write(Galleon);
            writer.Write(OnCourse);

            writer.Write(Crew.Count);
            foreach (var mob in Crew)
                writer.Write(mob);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            Blockade = reader.ReadBool();
            Zone = (SpawnZone)reader.ReadInt();
            m_StopTime = reader.ReadDateTime();
            Galleon = reader.ReadItem() as BaseGalleon;
            OnCourse = reader.ReadBool();

            var count = reader.ReadInt();
            for (var i = 0; i < count; i++)
            {
                var mob = reader.ReadMobile();
                if (mob != null && !mob.Deleted && mob.Alive)
                    Crew.Add(mob);
            }

            if (!Blockade)
                ResumeCourseTimed(TimeSpan.FromSeconds(15), true);

            if (Galleon != null) Timer.DelayCall(TimeSpan.FromSeconds(30), CheckCrew);

            NextMoveCheck = DateTime.UtcNow;
            NextCannonShot = DateTime.UtcNow;
        }
    }
}
