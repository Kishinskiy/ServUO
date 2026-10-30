using System;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Engines.CannedEvil; // Подключаем оригинальную Champion-систему
using Server.Custom.KarmaVirtueSystem; // Подключаем вашу родную систему паломничества

namespace Server.Custom.EpicAlignment
{
    // =========================================================================
    // 🏛️ МОНОЛИТНЫЙ КЛАСС: АЛТАРЬ АВАТАРА (ПОЛНОСТЬЮ СОВМЕСТИМЫЙ С ВАШИМ ЯДРОМ) 🎉
    // =========================================================================
    public class EpicAvatarAltarSpawner : ChampionSpawn
    {
        private Timer m_LevelMonitor;

        [Constructable]
        public EpicAvatarAltarSpawner() : base()
        {
            Hue = 2418; // Подсвечиваем кристалл золотым цветом Аватара для ГМов
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !from.Alive) return;

            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                base.OnDoubleClick(from); // ГМам открываем стандартную панель [props
                return;
            }

            if (Active)
            {
                from.SendMessage("Этот Алтарь Возвышения уже активен! Защищайте курган.");
                return;
            }

            PlayerMobile pm = from as PlayerMobile;
            if (pm != null && !ExecuteValidationChecks(pm))
            {
                return; // Если проверки провалены, прерываемся и не открываем Prompt мантры
            }

            // Если все условия и реликвии сданы, открываем нативный Prompt ввода мантры из ядра
            base.OnDoubleClick(from);
            from.SendMessage(0x35, "Древний золотой идол ожил. Введите истинную мантру Света [om omni] в появившемся окне...");
        }

        // =========================================================================
        // ⚖️ МЕТОД ПРОВЕРОК ДОБРОДЕТЕЛЕЙ, КАРМЫ И РЕСУРСОВ
        // =========================================================================
        private bool ExecuteValidationChecks(PlayerMobile pm)
        {
            // Проверяем по всей базе данных аккаунтов, не занято ли уже место Аватара в мире
            if (EpicAvatarWorldBalance.IsAvatarPlaceOccupied())
            {
                this.PublicOverheadMessage(MessageType.Regular, 0x22, false, "[Место Аватара Света в этом мире уже занято другим бессмертным!]");
                return false;
            }

            // 1. Проверка паломничества по 8 Алтарям вашей KarmaVirtueSystem
            int permanentlyActivatedCount = KarmaVirtueManager.GetTotalActivatedShrinesCount(pm);
            if (permanentlyActivatedCount < 8)
            {
                pm.PlaySound(0x5C3); // Звук магической неудачи
                this.PublicOverheadMessage(MessageType.Regular, 0x22, false, "[Твоя душа не готова. Алтарь безмолвен.]");
                pm.SendMessage(0x22, $"Вы обязаны подчинить все 8 Алтарей Добродетели в мире, достигнув ранга Knight! У вас зачтено: {permanentlyActivatedCount} из 8.");
                return false;
            }

            // 2. Проверка законов Кармы и классовых ограничений
            if (pm.Karma < 15000)
            {
                pm.BoltEffect(0); pm.Damage(15);
                this.PublicOverheadMessage(MessageType.Regular, 0x22, false, "[В тебе мало чистого Света! Карма должна быть выше +15,000!]");
                return false;
            }
            if (pm.Skills[SkillName.Necromancy].Value >= 50.0)
            {
                pm.SendMessage(0x22, "Запретные темные знания Некромантии блокируют чистую энергию Аватара Света!");
                return false;
            }

            // 3. Проверка и сжигание 4 Реликвий Пагана из сумки
            Container pack = pm.Backpack;
            if (pack == null) return false;

            Item earth = pack.FindItemByType(typeof(HeartOfEarth)); 
            Item flame = pack.FindItemByType(typeof(TongueOfFlame));
            Item seas = pack.FindItemByType(typeof(TearOfSeas)); 
            Item wind = pack.FindItemByType(typeof(BreathOfWind));

            if (earth == null || flame == null || seas == null || wind == null)
            {
                pm.SendMessage(0x59, "== ПРОРОЧЕСТВО ПРОТИВОСТОЯНИЯ ==");
                pm.SendMessage(0x22, "Жертва отклонена. В вашем рюкзаке не найдено 4 Осколка Стихий: HeartOfEarth, TongueOfFlame, TearOfSeas и BreathOfWind!");
                return false;
            }

            // Успешный ритуал: поглощаем ресурсы стихий
            earth.Delete(); flame.Delete(); seas.Delete(); wind.Delete();

            // Запускаем независимый радар отслеживания волн для последующей подмены босса
            if (m_LevelMonitor != null) m_LevelMonitor.Stop();
            m_LevelMonitor = Timer.DelayCall(TimeSpan.FromSeconds(2.0), TimeSpan.FromSeconds(2.0), new TimerCallback(CheckDungeonProgress));

            // Эффекты успешного жертвоприношения
            pm.PlaySound(0x243); 
            Effects.SendLocationEffect(Location, Map, 0x3709, 30, 10, 2418, 0); 
            
            // 🔥 НАДЁЖНОЕ ИСПРАВЛЕНИЕ: Находим центральный каменный блок на плите через GetItemsInRange 
            // и перекрашиваем его, полностью обходя приватность поля m_Altar базового класса!
            Item altarStone = this.GetItemsInRange(1).FirstOrDefault(i => i is ChampionAltar && i.Location == this.Location);
            if (altarStone != null)
            {
                altarStone.Hue = 2418; // Зажигаем неоново-синий цвет Света
            }

            this.PublicOverheadMessage(MessageType.Regular, 0x35, false, "[Жертва стихий поглощена! Алтарь Возвышения готов принять мантру.]");
            return true;
        }

        // =========================================================================
        // ⏱️ ЕЖЕСЕКУНДНЫЙ МОНИТОРИНГ ДАНЖА (БЕЗОПАСНАЯ ПОДМЕНА БОССА ПОД КОНЕЦ 4 ВОЛНЫ)
        // =========================================================================
        private void CheckDungeonProgress()
        {
            if (Deleted || !Active)
            {
                if (m_LevelMonitor != null) { m_LevelMonitor.Stop(); m_LevelMonitor = null; }
                return;
            }

            // На основе вашего файла ядра: Level доходит до 16, либо Kills превышает MaxKills на 15 уровне
            if (Level >= 16 || (Kills >= MaxKills && Level >= 15))
            {
                if (m_LevelMonitor != null) { m_LevelMonitor.Stop(); m_LevelMonitor = null; }

                // Ищем живого лидера рейда в радиусе кургана
                PlayerMobile leader = GetMobilesInRange(16).OfType<PlayerMobile>().FirstOrDefault(p => p.Alive && !p.Deleted);
                
                if (leader != null)
                {
                    // Выключаем спавнер, чтобы ядро не успело вызвать ванильного босса из таблицы
                    Active = false; 

                    leader.SendMessage(0x35, "[ФИНАЛЬНЫЙ РИТУАЛ]: Все 4 волны Скверны отбиты! Из праха павших монстров материализуется ваше Отражение Судьбы!");
                    World.Broadcast(0x35, true, $"[Квест] В Храме Возвышения Света начался финальный бой! Герой {leader.Name} сошелся в дуэли с Высшим Судией!");

                    // Физически спавним нашего кастомного Высшего Босса на алтаре
                    EpicAvatarBoss boss = new EpicAvatarBoss();
                    boss.MoveToWorld(new Point3D(X, Y, Z + 1), Map);
                    boss.Combatant = leader;
                    boss.Warmode = true;

                    // Натравливаем систему наград на мониторинг смерти босса
                    EpicAvatarRewardSystem.MonitorAvatarBoss(boss, leader);
                }
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();
            if (m_LevelMonitor != null) m_LevelMonitor.Stop();
        }

        public EpicAvatarAltarSpawner(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}
