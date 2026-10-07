using System;
using Server.Mobiles;
using Server.Network;

namespace Server.Items
{
    public class SecretPassageSwitch : Item
    {
        private int m_RequiredSkill;
        private Timer m_RevealTimer;

        [CommandProperty(AccessLevel.GameMaster)]
        public int RequiredSkill
        {
            get => m_RequiredSkill;
            set
            {
                m_RequiredSkill = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D TeleportTarget { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map TeleportMap { get; set; }

        [Constructable]
        public SecretPassageSwitch() : base(0x108F)
        {
            Name = "Странный камень";
            Movable = false;
            Visible = false; // Предмет скрыт от обычного взора

            m_RequiredSkill = 70;
            TeleportTarget = new Point3D(1495, 1629, 10);
            TeleportMap = Map.Felucca;
        }

        public SecretPassageSwitch(Serial serial) : base(serial)
        {
        }

        // =========================================================================
        // ИСПРАВЛЕННЫЙ МЕТОД ОБНАРУЖЕНИЯ
        // Вызывайте его из Scripts/Skills/DetectHidden.cs при сканировании области!
        // =========================================================================
        public void Discover(Mobile from)
        {
            if (from == null || !from.Player)
                return;

            // Проверяем навык
            if (from.Skills[SkillName.DetectHidden].Value < m_RequiredSkill)
                return;

            if (Visible)
                return;

            Visible = true;

            Effects.SendLocationParticles(EffectItem.Create(Location, Map, TimeSpan.FromSeconds(1.0)), 0x376A, 9, 32,
                5015);
            from.PlaySound(0x1F2);

            from.SendMessage(0x42, "Ваш наметанный глаз заметил странно шатающийся камень в кладке стены!");

            if (m_RevealTimer != null)
                m_RevealTimer.Stop();

            m_RevealTimer = Timer.DelayCall(TimeSpan.FromSeconds(15.0), HideSwitch);
        }

        private void HideSwitch()
        {
            if (!Deleted)
            {
                Visible = false;
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

            // Игроки могут нажать только если камень обнаружен (видим)
            if (!Visible && from.AccessLevel == AccessLevel.Player)
            {
                from.SendMessage("Здесь нет ничего примечательного.");
                return;
            }

            if (!from.InRange(GetWorldLocation(), 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, true, "Вы слишком далеко.");
                return;
            }

            if (m_RevealTimer != null)
                m_RevealTimer.Stop();

            Effects.PlaySound(Location, Map, 0x241);
            from.SendMessage(0x42, "Вы нажимаете на скрытый камень. Под вашими ногами разверзается тьма!");

            // Безопасный перенос петов и игрока
            BaseCreature.TeleportPets(from, TeleportTarget, TeleportMap);
            from.MoveToWorld(TeleportTarget, TeleportMap);

            Visible = false;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
            writer.Write(m_RequiredSkill);
            writer.Write(TeleportTarget);
            writer.Write(TeleportMap);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
            m_RequiredSkill = reader.ReadInt();
            TeleportTarget = reader.ReadPoint3D();
            TeleportMap = reader.ReadMap();
        }
    }
}
