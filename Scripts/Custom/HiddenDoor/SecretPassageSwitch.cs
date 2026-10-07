using System;
using Server;
using Server.Mobiles;
using Server.Network;

namespace Server.Items
{
    public class SecretPassageSwitch : Item
    {
        private int m_RequiredSkill; // Минимальный уровень Detecting Hidden (например, 70.0)
        private Point3D m_TeleportTarget; // Куда переносить игрока (в подземелье)
        private Map m_TeleportMap; // Карта подземелья (обычно та же, что и у замка)
        private Timer m_RevealTimer; // Таймер, который снова скроет рычаг, если по нему не кликнули

        [CommandProperty(AccessLevel.GameMaster)]
        public int RequiredSkill { get => m_RequiredSkill; set { m_RequiredSkill = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D TeleportTarget { get => m_TeleportTarget; set => m_TeleportTarget = value; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map TeleportMap { get => m_TeleportMap; set => m_TeleportMap = value; }

        [Constructable]
        public SecretPassageSwitch() : base(0x108F) // Графика маленького камня в стене или рычага
        {
            Name = "Странный камень";
            Movable = false;
            Visible = false; // ПО УМОЛЧАНИЮ ПРЕДМЕТ НЕВИДИМ ДЛЯ ВСЕХ 
            
            m_RequiredSkill = 70; // Требуется 70.0 Detecting Hidden по умолчанию
            m_TeleportTarget = new Point3D(1495, 1629, 10); // Заглушка (координаты подземелья)
            m_TeleportMap = Map.Felucca;
        }

        // =========================================================================
        // ГЛАВНЫЙ МЕТОД ЯДРА: Вызывается при использовании Detecting Hidden
        // =========================================================================
        public override void OnDetectedHidden(Mobile from)
        {
            base.OnDetectedHidden(from);

            if (from == null || !from.Player)
                return;

            // Проверяем уровень навыка игрока (значение от 0 до 100)
            if (from.Skills[SkillName.DetectHidden].Value < m_RequiredSkill)
            {
                // Игрок использовал навык, но его мастерства не хватило, чтобы заметить тайник
                return;
            }

            // Если рычаг уже кем-то обнаружен и виден, ничего не делаем
            if (this.Visible)
                return;

            // Делаем рычаг видимым
            this.Visible = true;
            
            // Эффекты обнаружения (звук искр и мистические частицы вокруг камня)
            Effects.SendLocationParticles(EffectItem.Create(Location, Map, TimeSpan.FromSeconds(1.0)), 0x376A, 9, 32, 5015);
            from.PlaySound(0x1F2); 
            
            from.SendMessage(0x42, "Ваш наметанный глаз заметил странно шатающийся камень в кладке стены!");

            // Запускаем таймер на 15 секунд. Если никто не нажмет рычаг, он снова исчезнет
            if (m_RevealTimer != null)
                m_RevealTimer.Stop();

            m_RevealTimer = Timer.DelayCall(TimeSpan.FromSeconds(15.0), new TimerCallback(HideSwitch));
        }

        private void HideSwitch()
        {
            if (!this.Deleted)
            {
                this.Visible = false; // Снова исчезает из мира
                // Звук тихого шороха камня
                Effects.PlaySound(Location, Map, 0x11D); 
            }
        }

        // =========================================================================
        // ИСПОЛЬЗОВАНИЕ РЫЧАГА (DoubleClick)
        // =========================================================================
        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !from.Player)
                return;

            // ГМ-ы могут кликать всегда, игроки — только если рычаг обнаружен (видим)
            if (!this.Visible && from.AccessLevel == AccessLevel.Player)
                return;

            if (!from.InRange(this.GetWorldLocation(), 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, true, "Вы слишком далеко.");
                return;
            }

            // Останавливаем таймер скрытия, так как рычаг успешно использован
            if (m_RevealTimer != null)
                m_RevealTimer.Stop();

            // Эффект нажатия (звук тяжелого каменного механизма)
            Effects.PlaySound(Location, Map, 0x241);
            from.SendMessage(0x42, "Вы нажимаете на скрытый камень. Под вашими ногами разверзается тьма!");

            // Телепортируем игрока в подземелье
            BaseCreature.TeleportPets(from, m_TeleportTarget, m_TeleportMap); // Переносим и игрока, и его питомцев (коней)
            from.MoveToWorld(m_TeleportTarget, m_TeleportMap);

            // Сразу же возвращаем рычагу невидимость для следующего исследователя
            this.Visible = false;
        }

        public SecretPassageSwitch(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); writer.Write(m_RequiredSkill); writer.Write(m_TeleportTarget); writer.Write(m_TeleportMap); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); m_RequiredSkill = reader.ReadInt(); m_TeleportTarget = reader.ReadPoint3D(); m_TeleportMap = reader.ReadMap(); }
    }
}
