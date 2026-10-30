using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Gumps;
using Server.Network;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.EpicAlignment
{
    // =========================================================================
    // 👑 1. УНИКАЛЬНЫЙ ИНТЕРФЕЙС: СВЯЩЕННЫЙ ФОЛИАНТ АВАТАРА СВЕТА ☀️
    // =========================================================================
    public class EpicAvatarBookGump : Gump
    {
        private readonly PlayerMobile m_Player;
        private readonly EpicBookOfDestiny m_Book;

        public EpicAvatarBookGump(PlayerMobile from, EpicBookOfDestiny book) : base(100, 100)
        {
            m_Player = from;
            m_Book = book;

            AddPage(0);

            // Нативный фон раскрытой книги в золоченом эльфийском стиле
            AddBackground(0, 0, 560, 400, 0x1E46);

            // --- ЛЕВАЯ СТРАНИЦА: СИЛЫ ПОРЯДКА ---
            AddLabel(70, 40, 2418, "ФОЛИАНТ СВЯТОГО СВЕТА");
            AddImageTiled(55, 60, 180, 2, 0x2424); // Золотой разделитель

            // Способность 1: Призыв Армии Света
            AddButton(55, 90, 0x0845, 0x0846, 1, GumpButtonType.Reply, 0);
            AddLabel(90, 92, 0x481, "Призвать Легион Света");
            DisplayCooldown(55, 115, m_Book.NextGuardSummon);

            // Способность 2: Вызов Мирового Судии (Дракона)
            AddButton(55, 160, 0x0845, 0x0846, 2, GumpButtonType.Reply, 0);
            AddLabel(90, 162, 0x481, "Вызвать Мирового Судию");
            DisplayCooldown(55, 185, m_Book.NextBossSummon);

            // Статус Полубога
            AddLabel(55, 290, 0x3F, "Зачет паломничеств: 8 из 8");
            AddLabel(55, 310, 2418, "Вектор Души: Истинный Аватар");

            // --- ПРАВАЯ СТРАНИЦА: ЭПИЧЕСКИЕ СЛОТЫ СВЕТА ---
            AddLabel(340, 40, 2418, "ЭПИЧЕСКИЕ РИТУАЛЫ ПОРЯДКА");
            AddImageTiled(315, 60, 180, 2, 0x2424);

            int startY = 90;
            for (int i = 0; i < 4; i++)
            {
                if (m_Book.CustomSpells[i])
                {
                    AddButton(315, startY, 0x15E1, 0x15E5, 10 + i, GumpButtonType.Reply, 0);
                    AddLabel(345, startY + 2, 0x35, $"Заклинание Света V.{i + 1}");
                }
                else
                {
                    AddImage(315, startY + 3, 0x011A); // Иконка замка
                    AddLabel(345, startY, 0x3A3, "[ Пустой слот руны Порядка ]");
                }
                startY += 55;
            }
        }

        private void DisplayCooldown(int x, int y, DateTime nextUse)
        {
            if (DateTime.UtcNow >= nextUse)
                AddLabel(x, y, 0x58, "Статус: Способность Готова!");
            else
            {
                TimeSpan remain = nextUse - DateTime.UtcNow;
                AddLabel(x, y, 0x22, $"Перезарядка: {remain.Hours}ч. {remain.Minutes}м.");
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (m_Player == null || m_Player.Deleted || !m_Player.Alive || m_Book == null || m_Book.Deleted) return;
            if (m_Player.Backpack == null || !m_Book.IsChildOf(m_Player.Backpack)) return;

            if (info.ButtonID == 1) // Призыв гвардии Света
            {
                if (DateTime.UtcNow < m_Book.NextGuardSummon && m_Player.AccessLevel == AccessLevel.Player)
                {
                    m_Player.SendMessage(0x22, "Ваш легион еще восстанавливает силы.");
                    return;
                }

                for (int i = 0; i < 4; i++)
                {
                    BaseCreature soldier = Utility.RandomBool() ? (BaseCreature)new Pixie() : (BaseCreature)new Wisp();
                    soldier.Name = $"[Воин Света] {soldier.Name}";
                    soldier.Hue = 2418;
                    BaseCreature.Summon(soldier, false, m_Player, m_Player.Location, 0x213, TimeSpan.FromMinutes(3.0));
                }

                m_Book.NextGuardSummon = DateTime.UtcNow + TimeSpan.FromDays(1.0);
                m_Player.SendMessage(0x35, "Небеса разверзлись! Световые сущности прибыли на вашу защиту на 3 минуты!");
                m_Player.SendGump(new EpicAvatarBookGump(m_Player, m_Book));
            }
            else if (info.ButtonID == 2) // Призыв Судии Света
            {
                if (DateTime.UtcNow < m_Book.NextBossSummon && m_Player.AccessLevel == AccessLevel.Player)
                {
                    m_Player.SendMessage(0x22, "Портал нестабилен. Призыв доступен раз в 24 часа.");
                    return;
                }

                BaseCreature worldBoss = new AncientWyrm();
                worldBoss.Name = "Дракон Сияния: Высший Судия Света [ МИРОВОЙ БОСС ]";
                worldBoss.Hue = 2418;
                worldBoss.HitsMaxSeed = 30000; worldBoss.Hits = 30000;
                worldBoss.MoveToWorld(m_Player.Location, m_Player.Map);
                worldBoss.Combatant = m_Player;

                m_Book.NextBossSummon = DateTime.UtcNow + TimeSpan.FromDays(1.0);
                World.Broadcast(0x35, true, $"[ВНИМАНИЕ] Высший Аватар {m_Player.Name} призвал Мирового Босса Света!");
                m_Player.SendGump(new EpicAvatarBookGump(m_Player, m_Book));
            }
        }
    }

    // =========================================================================
    // 💀 2. УНИКАЛЬНЫЙ ИНТЕРФЕЙС: ДРЕВНИЙ ГРИМУАР СКВЕРНЫ МОНДЕЙНА 🖤
    // =========================================================================
    public class EpicMondainBookGump : Gump
    {
        private readonly PlayerMobile m_Player;
        private readonly EpicGrimoireOfChaos m_Book;

        public EpicMondainBookGump(PlayerMobile from, EpicGrimoireOfChaos book) : base(100, 100)
        {
            m_Player = from;
            m_Book = book;

            AddPage(0);

            // Зловещий, темный готический фон книги с чумными/некромантскими узорами
            AddBackground(0, 0, 560, 400, 0x1E22);

            // --- ЛЕВАЯ СТРАНИЦА: СИЛЫ ХАОСА ---
            AddLabel(70, 40, 1175, "ГРИМУАР ВЕЧНОЙ СКВЕРНЫ");
            AddImageTiled(55, 60, 180, 2, 0x242A); // Темно-багровый разделитель

            // Способность 1: Призыв Воинства Мертвых
            AddButton(55, 90, 0x0938, 0x0939, 1, GumpButtonType.Reply, 0); // Череп-кнопка
            AddLabel(90, 92, 1175, "Поднять Легион Смерти");
            DisplayCooldown(55, 115, m_Book.NextUndeadSummon);

            // Способность 2: Вызов Вестника Бездны
            AddButton(55, 160, 0x0938, 0x0939, 2, GumpButtonType.Reply, 0);
            AddLabel(90, 162, 1175, "Вызвать Палача Бездны");
            DisplayCooldown(55, 185, m_Book.NextBossSummon);

            // Статус Полубога
            AddLabel(55, 290, 0x22, "Печати подчинения: 8 из 8");
            AddLabel(55, 310, 1175, "Вектор Души: Наместник Мондейна");

            // --- ПРАВАЯ СТРАНИЦА: ЭПИЧЕСКИЕ СЛОТЫ ХАОСА ---
            AddLabel(340, 40, 1175, "РИТУАЛЫ ЧЕРНОЙ МАГИИ");
            AddImageTiled(315, 60, 180, 2, 0x242A);

            int startY = 90;
            for (int i = 0; i < 4; i++)
            {
                if (m_Book.CustomSpells[i])
                {
                    AddButton(315, startY, 0x15E1, 0x15E5, 10 + i, GumpButtonType.Reply, 0);
                    AddLabel(345, startY + 2, 0x22, $"Темный Ритуал V.{i + 1}");
                }
                else
                {
                    AddImage(315, startY + 5, 0x0A2A); // Пентаграмма / Костяная руна
                    AddLabel(345, startY, 0x3A3, "[ Пустой слот руны Тьмы ]");
                }
                startY += 55;
            }
        }

        private void DisplayCooldown(int x, int y, DateTime nextUse)
        {
            if (DateTime.UtcNow >= nextUse)
                AddLabel(x, y, 0x3F, "Статус: Тьма готова к призыву!");
            else
            {
                TimeSpan remain = nextUse - DateTime.UtcNow;
                AddLabel(x, y, 0x22, $"Перезарядка: {remain.Hours}ч. {remain.Minutes}м.");
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (m_Player == null || m_Player.Deleted || !m_Player.Alive || m_Book == null || m_Book.Deleted) return;
            if (m_Player.Backpack == null || !m_Book.IsChildOf(m_Player.Backpack)) return;

            if (info.ButtonID == 1) // Призыв нежити
            {
                if (DateTime.UtcNow < m_Book.NextUndeadSummon && m_Player.AccessLevel == AccessLevel.Player)
                {
                    m_Player.SendMessage(0x22, "Кладбищенская земля еще не накопила прах.");
                    return;
                }

                for (int i = 0; i < 4; i++)
                {
                    BaseCreature undead = Utility.RandomBool() ? (BaseCreature)new BoneKnight() : (BaseCreature)new Lich();
                    undead.Name = $"[Легион Мондейна] {undead.Name}";
                    undead.Hue = 1175;
                    BaseCreature.Summon(undead, false, m_Player, m_Player.Location, 0x214, TimeSpan.FromMinutes(3.0));
                }

                m_Book.NextUndeadSummon = DateTime.UtcNow + TimeSpan.FromDays(1.0);
                m_Player.SendMessage(0x22, "Земля содрогнулась! Древние костяные лорды восстали из могил на вашим приказом на 3 минуты!");
                m_Player.SendGump(new EpicMondainBookGump(m_Player, m_Book));
            }
            else if (info.ButtonID == 2) // Призыв Мирового Босса Тьмы
            {
                if (DateTime.UtcNow < m_Book.NextBossSummon && m_Player.AccessLevel == AccessLevel.Player)
                {
                    m_Player.SendMessage(0x22, "Разлом Хаоса еще не остыл. Призыв доступен раз в сутки.");
                    return;
                }

                BaseCreature worldBoss = new Daemon();
                worldBoss.Name = "Разрушитель Миров: Высший Палач Бездны [ МИРОВОЙ БОСС ]";
                worldBoss.Hue = 1175;
                worldBoss.HitsMaxSeed = 30000; worldBoss.Hits = 30000;
                worldBoss.MoveToWorld(m_Player.Location, m_Player.Map);
                worldBoss.Combatant = m_Player;
                m_Book.NextBossSummon = DateTime.UtcNow + TimeSpan.FromDays(1.0);
                World.Broadcast(0x22, true, $"[КРИЗИС] Нечестивый Лорд {m_Player.Name} разорвал эфирные цепи и призвал Мирового Босса Тьмы!");
                m_Player.SendGump(new EpicMondainBookGump(m_Player, m_Book));
            }
        }
    }
}