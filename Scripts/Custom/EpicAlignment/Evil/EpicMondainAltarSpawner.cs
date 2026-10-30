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
    // 🏛️ МОНОЛИТНЫЙ КЛАСС: АЛТАРЬ МОНДЕЙНА (ПОЛНОСТЬЮ ИЗОЛИРОВАННЫЙ КЛАСС) 🎉
    // =========================================================================
    public class EpicMondainAltarSpawner : ChampionSpawn
    {
        private Timer m_LevelMonitor;

        [Constructable]
        public EpicMondainAltarSpawner() : base()
        {
            Hue = 1175; // Подсвечиваем кристалл ядовито-зеленым/некромантским цветом для ГМов
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
                from.SendMessage("Этот Алтарь Возвышения уже активен! Сражайтесь за право служить Тьме.");
                return;
            }

            PlayerMobile pm = from as PlayerMobile;
            if (pm != null && !ExecuteValidationChecks(pm))
            {
                return; // Если проверки провалены, прерываемся и не открываем Prompt мантры
            }

            // Если все условия и реликвии сданы, открываем нативный Prompt ввода мантры из ядра
            base.OnDoubleClick(from);
            from.SendMessage(0x22, "Древний Идол Бездны пробудился. Введите истинную мантру Тьмы [an tym] в появившемся окне...");
        }

        // =========================================================================
        // ⚖️ МЕТОД ПРОВЕРОК ДОБРОДЕТЕЛЕЙ, КАРМЫ И РЕСУРСОВ (ТЁМНЫЙ ВЕКТОР)
        // =========================================================================
        private bool ExecuteValidationChecks(PlayerMobile pm)
        {
            // Проверяем по всей базе данных аккаунтов, не занято ли уже место Руки Мондейна в мире
            if (EpicAvatarWorldBalance.IsMondainPlaceOccupied())
            {
                this.PublicOverheadMessage(MessageType.Regular, 0x22, false, "[Престол Тьмы в этом мире уже занят другим бессмертным Лордом!]");
                return false;
            }

            // 1. Проверка паломничества по 8 Алтарям вашей KarmaVirtueSystem
            int permanentlyActivatedCount = KarmaVirtueManager.GetTotalActivatedShrinesCount(pm);
            if (permanentlyActivatedCount < 8)
            {
                pm.PlaySound(0x5C3); // Звук магической неудачи
                this.PublicOverheadMessage(MessageType.Regular, 0x22, false, "[Твоя душа не готова. Алтарь Хаоса безмолвен.]");
                pm.SendMessage(0x22, $"Вы обязаны подчинить все 8 Алтарей Добродетели в мире, достигнув ранга Knight! У вас зачтено: {permanentlyActivatedCount} из 8.");
                return false;
            }

            // 2. Проверка законов Кармы и классовых ограничений (Зеркально Тёмные)
            if (pm.Karma > -15000)
            {
                // Наказываем вспышкой пламени Бездны за ложь
                pm.FixedParticles(0x3709, 10, 30, 5052, 1175, 0, EffectLayer.Waist);
                pm.Damage(15); 
                this.PublicOverheadMessage(MessageType.Regular, 0x22, false, "[В тебе слишком мало чистой скверны! Требуется Карма ниже -15,000!]");
                return false;
            }
            if (pm.Skills[SkillName.Chivalry].Value >= 50.0)
            {
                pm.SendMessage(0x22, "Священные догматы Паладинства (Chivalry) блокируют верность Владыке Мондейну!");
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

            // Эффекты успешного тёмного жертвоприношения
            pm.PlaySound(0x22F); // Звук пламени Бездны
            Effects.SendLocationEffect(Location, Map, 0x3709, 30, 10, 1175, 0); 
            
            // Находим центральный каменный блок на плите через GetItemsInRange и перекрашиваем его
            Item altarStone = this.GetItemsInRange(1).FirstOrDefault(i => i is ChampionAltar && i.Location == this.Location);
            if (altarStone != null)
            {
                altarStone.Hue = 1175; // Зажигаем ядовито-зеленый цвет Хаоса
            }

            this.PublicOverheadMessage(MessageType.Regular, 0x22, false, "[Жертва стихий осквернена! Алтарь Бездны готов принять мантру.]");
            return true;
        }

        // =========================================================================
        // ⏱️ ЕЖЕСЕКУНДНЫЙ МОНИТОРИНГ ДАНЖА (БЕЗОПАСНАЯ ПОДМЕНА ТЁМНОГО БОССА)
        // =========================================================================
        private void CheckDungeonProgress()
        {
            if (Deleted || !Active)
            {
                if (m_LevelMonitor != null) { m_LevelMonitor.Stop(); m_LevelMonitor = null; }
                return;
            }

            if (Level >= 16 || (Kills >= MaxKills && Level >= 15))
            {
                if (m_LevelMonitor != null) { m_LevelMonitor.Stop(); m_LevelMonitor = null; }

                PlayerMobile leader = GetMobilesInRange(16).OfType<PlayerMobile>().FirstOrDefault(p => p.Alive && !p.Deleted);
                
                if (leader != null)
                {
                    Active = false; // Отключаем стандартного босса ядра

                    leader.SendMessage(0x22, "[ФИНАЛЬНЫЙ РИТУАЛ ТЬМЫ]: Все 4 волны Света отбиты! Из астрального разлома материализуется Хранитель Печати Мондейна!");
                    World.Broadcast(0x22, true, $"[Квест] В Храме Возвышения Тьмы в Маласе начался финальный бой! Чернокнижник {leader.Name} сошелся в дуэли с Высшим Жнецом Бездны!");

                    // Физический спавн Тёмного Босса на алтаре Хаоса
                    EpicMondainBoss boss = new EpicMondainBoss();
                    boss.MoveToWorld(new Point3D(X, Y, Z + 1), Map);
                    boss.Combatant = leader;
                    boss.Warmode = true;

                    // Подключаем тёмную систему наград к мониторингу смерти Жнеца Бездны
                    EpicMondainRewardSystem.MonitorMondainBoss(boss, leader);
                }
                else
                {
                    Active = false;
                    this.PublicOverheadMessage(MessageType.Regular, 0x22, false, "[Испытание сорвано]: Претендент покинул тронный зал.");
                }
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();
            if (m_LevelMonitor != null) m_LevelMonitor.Stop();
        }

        public EpicMondainAltarSpawner(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}
