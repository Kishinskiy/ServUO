using System;
using Server;
using Server.Mobiles;
using Server.Network;

namespace Server.Items
{
    // Добавляем наследование интерфейса IRevealableItem для интеграции с ядром
    public class SecretPassageSwitch : Item, IRevealableItem
    {
        private int m_RequiredSkill; // Минимальный уровень Detecting Hidden
        private Point3D m_TeleportTarget; // Куда переносить игрока
        private Map m_TeleportMap; // Карта подземелья
        private Timer m_RevealTimer; // Таймер скрытия

        [CommandProperty(AccessLevel.GameMaster)]
        public int RequiredSkill { get => m_RequiredSkill; set { m_RequiredSkill = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D TeleportTarget { get => m_TeleportTarget; set => m_TeleportTarget = value; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map TeleportMap { get => m_TeleportMap; set => m_TeleportMap = value; }

        [Constructable]
        public SecretPassageSwitch() : base(0x108F) 
        {
            Name = "Странный камень";
            Movable = false;
            Visible = false; // По умолчанию скрыт
            
            m_RequiredSkill = 70; 
            m_TeleportTarget = new Point3D(1495, 1629, 10); 
            m_TeleportMap = Map.Felucca;
        }

        // =========================================================================
        // РЕАЛИЗАЦИЯ ИНТЕРФЕЙСА IREVEALABLEITEM
        // =========================================================================

        // Флаг: проверять ли предмет, когда он скрыт (True)
        public bool CheckWhenHidden => true;

        // Активная проверка навыка (вызывается при клике таргетом навыка Detect Hidden)
        public bool CheckReveal(Mobile m)
        {
            if (m == null || !m.Player)
                return false;

            // Проверяем уровень навыка персонажа
            return m.Skills[SkillName.DetectHidden].Value >= m_RequiredSkill;
        }

        // Пассивная проверка навыка (когда игрок просто проходит мимо камня)
        public bool CheckPassiveDetect(Mobile m)
        {
            if (m == null || !m.Player)
                return false;

            // Для пассивного обнаружения сделаем проверку чуть строже (например, случайный шанс от навыка)
            double skill = m.Skills[SkillName.DetectHidden].Value;
            
            if (skill < m_RequiredSkill)
                return false;

            // Если навыка хватает, даем 20% шанс заметить тайник пассивно при шаге рядом
            return (Utility.RandomDouble() < 0.20);
        }

        // Событие, которое срабатывает при успешном обнаружении (активном)
        public void OnRevealed(Mobile m)
        {
            if (this.Visible)
                return;

            // Делаем рычаг видимым
            this.Visible = true;
            
            // Эффекты
            Effects.SendLocationParticles(EffectItem.Create(Location, Map, TimeSpan.FromSeconds(1.0)), 0x376A, 9, 32, 5015);
            m.PlaySound(0x1F2); 
            
            m.SendMessage(0x42, "Ваш наметанный глаз заметил странно шатающийся камень в кладке стены!");

            // Запуск таймера исчезновения на 15 секунд
            if (m_RevealTimer != null)
                m_RevealTimer.Stop();

            m_RevealTimer = Timer.DelayCall(TimeSpan.FromSeconds(15.0), new TimerCallback(HideSwitch));
        }

        // =========================================================================
        // ЛОГИКА ТАЙМЕРА И ИСПОЛЬЗОВАНИЯ
        // =========================================================================

        private void HideSwitch()
        {
            if (!this.Deleted)
            {
                this.Visible = false; // Снова прячем камень
                Effects.PlaySound(Location, Map, 0x11D); // Звук шороха
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !from.Player)
                return;

            // Игроки могут нажать только если камень обнаружен и виден
            if (!this.Visible && from.AccessLevel == AccessLevel.Player)
            {
                from.SendMessage("Здесь нет ничего необычного.");
                return;
            }

            if (!from.InRange(this.GetWorldLocation(), 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, true, "Вы слишком далеко.");
                return;
            }

            if (m_RevealTimer != null)
                m_RevealTimer.Stop();

            Effects.PlaySound(Location, Map, 0x241); // Звук механизма
            from.SendMessage(0x42, "Вы нажимаете на скрытый камень. Под вашими ногами разверзается тьма!");

            // Переносим игрока и его петов
            BaseCreature.TeleportPets(from, m_TeleportTarget, m_TeleportMap); 
            from.MoveToWorld(m_TeleportTarget, m_TeleportMap);

            // Мгновенно прячем камень обратно
            this.Visible = false;
        }

        // Сериализация
        public SecretPassageSwitch(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); writer.Write(m_RequiredSkill); writer.Write(m_TeleportTarget); writer.Write(m_TeleportMap); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); m_RequiredSkill = reader.ReadInt(); m_TeleportTarget = reader.ReadPoint3D(); m_TeleportMap = reader.ReadMap(); }
    }
}
