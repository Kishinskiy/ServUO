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
    public class EpicBookGump : Gump
    {
        private readonly PlayerMobile m_Player;
        private readonly EpicBookOfDestiny m_Book;

        public EpicBookGump(PlayerMobile from, EpicBookOfDestiny book) : base(100, 100)
        {
            m_Player = from;
            m_Book = book;

            AddPage(0);
            
            // Красивый графический фон открытой книги эльфов/магов UO
            AddBackground(0, 0, 560, 400, 0x1E46); 

            // =========================================================================
            // 📖 ЛЕВАЯ СТРАНИЦА: ВШИТЫЕ СИЛЫ АВАТАРА (БАЗА)
            // =========================================================================
            AddLabel(70, 40, 2418, "ФОЛИАНТ СВЯТОГО СВЕТА");
            AddImageTiled(55, 60, 180, 2, 0x2424); // Золотой разделитель

            // Нативные кнопки вызова способностей
            // Способность 1: Призыв армии
            AddButton(55, 90, 0x0845, 0x0846, 1, GumpButtonType.Reply, 0);
            AddLabel(90, 92, 0x481, "Призвать Легион Света");
            DisplayCooldown(55, 115, m_Book.NextGuardSummon);

            // Способность 2: Вызов Мирового Босса
            AddButton(55, 160, 0x0845, 0x0846, 2, GumpButtonType.Reply, 0);
            AddLabel(90, 162, 0x481, "Вызвать Мирового Судию");
            DisplayCooldown(55, 185, m_Book.NextBossSummon);

            // Игровой статус игрока
            AddLabel(55, 290, 0x3F, $"Зачет паломничеств: 8 из 8");
            AddLabel(55, 310, 0x58, $"Вектор Души: Истинный Аватар");

            // =========================================================================
            // 🔮 ПРАВАЯ СТРАНИЦА: КАСТОМНЫЕ СЛОТЫ ДЛЯ БУДУЩИХ ЗАКЛИНАНИЙ
            // =========================================================================
            AddLabel(340, 40, 0x59, "ЭПИЧЕСКИЕ РИТУАЛЫ");
            AddImageTiled(315, 60, 180, 2, 0x2424);

            int startY = 90;
            for (int i = 0; i < 4; i++)
            {
                if (m_Book.CustomSpells[i])
                {
                    // Если заклинание изучено в будущем — рисуем активную кнопку спелла
                    AddButton(315, startY, 0x15E1, 0x15E5, 10 + i, GumpButtonType.Reply, 0);
                    AddLabel(345, startY + 2, 0x35, $"Эпический Ритуал V.{i + 1}");
                }
                else
                {
                    // Если слот пуст — выводим антуражную закрытую надпись
                    AddImage(315, startY + 3, 0x011A); // Иконка закрытого замочка/точки
                    AddLabel(345, startY, 0x3A3, "[ Пустой слот заклинания ]");
                }
                startY += 55;
            }
        }

        private void DisplayCooldown(int x, int y, DateTime nextUse)
        {
            if (DateTime.UtcNow >= nextUse)
            {
                AddLabel(x, y, 0x58, "Статус: Способность Готова!");
            }
            else
            {
                TimeSpan remain = nextUse - DateTime.UtcNow;
                AddLabel(x, y, 0x22, $"Перезарядка: {remain.Hours}ч. {remain.Minutes}м.");
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (m_Player == null || m_Player.Deleted || !m_Player.Alive || m_Book == null || m_Book.Deleted)
                return;

            if (m_Player.Backpack == null || !m_Book.IsChildOf(m_Player.Backpack))
                return;

            // ⚔️ ОБРАБОТКА БАЗОВОЙ КНОПКИ 1: ПРИЗЫВ ЛЕГИОНА СВЕТА
            if (info.ButtonID == 1)
            {
                if (DateTime.UtcNow < m_Book.NextGuardSummon && m_Player.AccessLevel == AccessLevel.Player)
                {
                    m_Player.SendMessage(0x22, "Ваш легион еще восстанавливает силы после прошлого призыва.");
                    return;
                }

                // Запускаем призыв 4 нативных существ Света на 3 минуты
                int spawnCount = 4;
                for (int i = 0; i < spawnCount; i++)
                {
                    BaseCreature soldier = Utility.RandomBool() ? (BaseCreature)new Gargoyle() : (BaseCreature)new Efreet();
                    soldier.Name = $"[Легион Аватара] {soldier.Name}";
                    soldier.Hue = 2418;

                    // Нативно суммоним монстра вокруг игрока
                    BaseCreature.Summon(soldier, false, m_Player, m_Player.Location, 0x213, TimeSpan.FromMinutes(3.0));
                }

                // Выставляем кулдаун на 24 часа реального времени
                m_Book.NextGuardSummon = DateTime.UtcNow + TimeSpan.FromDays(1.0);
                m_Player.SendMessage(0x35, "Небеса разверзлись! Четыре Эфирных Воина Света прибыли на вашу защиту на 3 минуты!");
                
                // Переоткрываем книгу, чтобы обновить таймер перезарядки
                m_Player.SendGump(new EpicBookGump(m_Player, m_Book));
            }

            // ⚔️ ОБРАБОТКА БАЗОВОЙ КНОПКИ 2: ВЫЗОВ МИРОВОГО БОССА ДЛЯ ТЕСТА / ФАРМА
            else if (info.ButtonID == 2)
            {
                if (DateTime.UtcNow < m_Book.NextBossSummon && m_Player.AccessLevel == AccessLevel.Player)
                {
                    m_Player.SendMessage(0x22, "Портал нестабилен. Призыв Высшего Судии доступен раз в 24 часа.");
                    return;
                }

                // Призываем Древнего Дракона в качестве подконтрольного испытания
                BaseCreature worldBoss = new AncientWyrm();
                worldBoss.Name = "Слэшер Сияния: Высший Судия Света [ МИРОВОЙ БОСС ]";
                worldBoss.Hue = 2418;
                worldBoss.HitsMaxSeed = 30000;
                worldBoss.Hits = 30000;

                worldBoss.MoveToWorld(m_Player.Location, m_Player.Map);
                worldBoss.Combatant = m_Player;

                m_Book.NextBossSummon = DateTime.UtcNow + TimeSpan.FromDays(1.0);
                World.Broadcast(0x35, true, $"[ВНИМАНИЕ] Высший Аватар {m_Player.Name} открыл пространственный разлом и призвал Мирового Босса!");
                
                m_Player.SendGump(new EpicBookGump(m_Player, m_Book));
            }

            // ОБРАБОТКА КНОПОК БУДУЩИХ ЗАКЛИНАНИЙ (Слоты 10, 11, 12, 13)
            else if (info.ButtonID >= 10 && info.ButtonID <= 13)
            {
                int spellIndex = info.ButtonID - 10;
                m_Player.SendMessage(0x5A, $"Вы активировали Эпический Ритуал из слота №{spellIndex + 1}! (Логика заклинания будет добавлена в игру позже).");
            }
        }
    }
}
