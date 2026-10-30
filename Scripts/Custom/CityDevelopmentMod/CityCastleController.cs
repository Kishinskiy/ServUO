using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Items;
using System.Linq;

namespace Server.Custom.CityDevelopment
{
    public class CityCastleController : Item
    {
        private Server.Engines.CityLoyalty.City m_City;
        private List<Mobile> m_SpawnedNPCs = new List<Mobile>();
        private Item m_DungeonTeleporter; // Ссылка на скрытый проход
        private Timer m_CheckTimer;

        [CommandProperty(AccessLevel.GameMaster)]
        public Server.Engines.CityLoyalty.City TargetCity
        {
            get { return m_City; }
            set { m_City = value; UpdateCastle(); }
        }

        // Координаты точки внутри подземелья (куда телепортирует люк)
        // ГМ может изменить их прямо из игры через [props на контроллер!
        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D DungeonDestLoc { get; set; } = new Point3D(6000, 1000, 0);

        [CommandProperty(AccessLevel.GameMaster)]
        public Map DungeonDestMap { get; set; } = Map.Felucca;

        [Constructable]
        public CityCastleController() : base(0x1F13)
        {
            Name = "Контроллер Заселения Замка Резиденции";
            Movable = false;
            Visible = false;

            m_CheckTimer = Timer.DelayCall(TimeSpan.FromMinutes(2.0), TimeSpan.FromMinutes(2.0), new TimerCallback(UpdateCastle));
        }

        public void UpdateCastle()
        {
            if (Deleted || Map == null || Map == Map.Internal) return;

            CityProgress progress = CityDevelopmentMod.GetCityProfile(m_City, Map);
            if (progress == null) return;

            CleanOldNPCs();

            // Удаляем старый телепорт, если он был, чтобы пересчитать его статус
            if (m_DungeonTeleporter != null && !m_DungeonTeleporter.Deleted)
            {
                m_DungeonTeleporter.Delete();
                m_DungeonTeleporter = null;
            }

            int currentLevel = progress.Level;

            // =========================================================================
            // 🏛️ ЭТАПЫ ОХРАНЫ И ЗАСЕЛЕНИЯ В ЗАВИСИМОСТИ ОТ УРОВНЯ
            // =========================================================================
            if (currentLevel >= 3)
            {
                SpawnNPC(new Banker(), new Point3D(X + 2, Y + 2, Z));
                // SpawnNPC(new Herald(), new Point3D(X - 2, Y + 2, Z));
            }

            if (currentLevel >= 4)
            {
                // SpawnNPC(new EliteCastleGuard(m_City), new Point3D(X + 5, Y, Z));
                // SpawnNPC(new EliteCastleGuard(m_City), new Point3D(X - 5, Y, Z));
                // SpawnNPC(new Quartermaster(), new Point3D(X, Y - 4, Z));
            }

            if (currentLevel >= 5)
            {
                // Высший уровень: Спавним Губернатора
                SpawnNPC(new TownGovernorNPC(m_City), new Point3D(X, Y, Z));

                // 🔥 ОТКРЫТИЕ ПОДЗЕМЕЛЬЯ: Создаем физический люк прямо перед троном (на 2 тайла южнее контроллера)
                CastleDungeonTeleporter tele = new CastleDungeonTeleporter(this);
                tele.MoveToWorld(new Point3D(X, Y + 2, Z), this.Map);
                m_DungeonTeleporter = tele;
                
                Effects.PlaySound(tele.Location, tele.Map, 0x1FA); // Звук открытия тяжелой плиты/двери
            }
        }

        private void SpawnNPC(Mobile mob, Point3D loc)
        {
            if (mob == null) return;
            mob.MoveToWorld(loc, this.Map);
            if (mob is BaseCreature bc)
            {
                bc.Home = loc;
                bc.RangeHome = 3;
            }
            m_SpawnedNPCs.Add(mob);
        }

        private void CleanOldNPCs()
        {
            foreach (Mobile mob in m_SpawnedNPCs.Where(m => m != null && !m.Deleted))
                mob.Delete();
            m_SpawnedNPCs.Clear();
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();
            if (m_CheckTimer != null) m_CheckTimer.Stop();
            if (m_DungeonTeleporter != null) m_DungeonTeleporter.Delete();
            CleanOldNPCs();
        }

        public CityCastleController(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) 
        { 
            base.Serialize(writer); 
            writer.Write((int)1); // Версия 1 (с поддержкой координат данжа)
            writer.Write((int)m_City); 
            writer.Write(m_SpawnedNPCs, true); 
            writer.Write(m_DungeonTeleporter);
            writer.Write(DungeonDestLoc);
            writer.Write(DungeonDestMap);
        }
        public override void Deserialize(GenericReader reader) 
        { 
            base.Deserialize(reader); 
            int version = reader.ReadInt(); 
            m_City = (Server.Engines.CityLoyalty.City)reader.ReadInt(); 
            m_SpawnedNPCs = reader.ReadStrongMobileList(); 
            m_DungeonTeleporter = reader.ReadItem();
            
            if (version >= 1)
            {
                DungeonDestLoc = reader.ReadPoint3D();
                DungeonDestMap = reader.ReadMap();
            }
            m_CheckTimer = Timer.DelayCall(TimeSpan.FromMinutes(2.0), TimeSpan.FromMinutes(2.0), new TimerCallback(UpdateCastle)); 
        }
    }

    // =========================================================================
    // 🌌 ВНУТРЕННИЙ КЛАСС: ТЕЛЕПОРТ В ПОДЗЕМЕЛЬЕ С ПРОВЕРКОЙ РЕПУТАЦИИ ГОРОДА
    // =========================================================================
    public class CastleDungeonTeleporter : Item
    {
        private CityCastleController m_Controller;

        public CastleDungeonTeleporter(CityCastleController controller) : base(0x0247) // Графика железной решетки/люка в полу
        {
            m_Controller = controller;
            Name = "Спуск в Подземелье Резиденции";
            Movable = false;
            Hue = 1102; // Темный антуражный цвет порчи/камня
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (m == null || !m.Alive || m_Controller == null || m_Controller.Deleted) 
                return true;

            if (m is PlayerMobile pm)
            {
                double loyaltyPoints = TownBoards.TownBulletinBoardSystem.GetPlayerLoyaltyPoints(m_Controller.TargetCity, pm);

                if (loyaltyPoints < 5000.0 && pm.AccessLevel == AccessLevel.Player)
                {
                    pm.PlaySound(0x5C3);
                    pm.SendMessage(0x22, $"[Замковая Стража]: Спуск в катакомбы города {m_Controller.TargetCity} разрешен только Почтенным Гражданам (Требуется 5,000+ лояльности).");
                    return false; 
                }

                // 🔥 РОГАЛИК-МЕХАНИКА: Если лабиринт еще не сгенерирован — строим его!
                // Размер 40х40 тайлов — идеальный компактный размер для соло-зачистки
                Point3D startRoomInMaze = RogueDungeonGenerator.IsGenerated 
                    ? RogueDungeonGenerator.StartRegion // Если уже создан кем-то, кидаем на старт
                    : RogueDungeonGenerator.Generate(40); 

                // Переносим игрока в динамически созданную стартовую комнату лабиринта!
                pm.PlaySound(0x1FE); 
                pm.MoveToWorld(startRoomInMaze, RogueDungeonGenerator.DungeonMap);
                pm.SendMessage(0x5A, "Вы сошли по ступеням в подземелье... Стены и коридоры этого места изменились до неузнаваемости!");
                
                // Запускаем автоматический таймер зачистки: через 40 минут данж исчезнет и сотрется, если игроки уйдут
                Timer.DelayCall(TimeSpan.FromMinutes(40.0), () => RogueDungeonGenerator.ClearDungeon());

                return false;
            }

            return true;
        }

        public CastleDungeonTeleporter(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); writer.Write(m_Controller); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); m_Controller = reader.ReadItem() as CityCastleController; }
    }

    // --- Классы заглушки NPC остаются без изменений ---
    // public class EliteCastleGuard : Guard { public EliteCastleGuard(Server.Engines.CityLoyalty.City city) { Name = $"Рыцарь гвардии {city}"; Title = "[Замковая Стража]"; HueMod = 2418; } public EliteCastleGuard(Serial serial) : base(serial) { } public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); } public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); } }
    public class TownGovernorNPC : Mobile { public TownGovernorNPC(Server.Engines.CityLoyalty.City city) { Name = "Лорд-Губернатор"; Title = $"[Правитель {city}]"; CantWalk = true; Body = 0x190; System.Threading.Tasks.Task.Run(() => Utility.AssignRandomHair(this)); } public override void OnDoubleClick(Mobile from) { if (from.Alive && from.InRange(this.Location, 3)) { from.SendMessage(0x54, $"{Name}: Приветствую тебя, верный гражданин! Наш замок процветает благодаря твоим поставкам и преданности городу."); } } public TownGovernorNPC(Serial serial) : base(serial) { } public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); } public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); } }
}
