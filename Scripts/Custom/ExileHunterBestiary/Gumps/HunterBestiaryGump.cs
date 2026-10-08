using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.ExileHunterBestiary.Gumps
{
    public class HunterBestiaryGump : Gump
    {
        private Mobile m_Player;
        private BestiaryCategory m_Category;
        private int m_Page;

        private const int EntriesPerPage = 8;

        public HunterBestiaryGump(Mobile player, BestiaryCategory category = BestiaryCategory.Undead, int page = 0)
            : base(100, 60)
        {
            m_Player = player;
            m_Category = category;
            m_Page = page;

            Closable = true;
            Disposable = true;
            Dragable = true;
            Resizable = false;

            AddPage(0);

            var profile = HunterBestiaryEngine.GetProfile(player);
            int totalMastered = profile != null ? profile.TotalMastered : 0;
            int grandTotal = HunterBestiaryEngine.RegistryById.Count;
            double grandPct = grandTotal > 0 ? (totalMastered * 100.0 / grandTotal) : 0.0;

            // Outer Frame
            AddBackground(0, 0, 640, 520, 9270);
            AddAlphaRegion(10, 10, 620, 500);

            // Inner Work Panel
            AddBackground(14, 14, 612, 492, 9200);
            AddAlphaRegion(18, 18, 604, 484);

            // Header Banner
            AddImageTiled(20, 72, 600, 2, 9354);

            // Book Icon
            AddItem(25, 20, 0x2252, 1153);

            // Header Titles
            AddHtml(85, 20, 320, 24, "<BASEFONT COLOR=#FFFFFF SIZE=5>The Grand Hunter's Bestiary</BASEFONT>", false, false);
            AddHtml(85, 46, 320, 20, "<BASEFONT COLOR=#D1C4E9 SIZE=3>Изучение Анатомии и Рангов Монстров</BASEFONT>", false, false);

            // Overall Progress Badge
            string overallStats = string.Format("<BASEFONT COLOR=#ECEFF1>Всего изучено: <BASEFONT COLOR=#00FFCC>{0}/{1}</BASEFONT><BASEFONT COLOR=#ECEFF1> ({2:F0}%)</BASEFONT>", totalMastered, grandTotal, grandPct);
            AddHtml(380, 24, 230, 22, overallStats, false, false);
            AddHtml(380, 46, 230, 20, "<BASEFONT COLOR=#FFE082>Ранги 1-5 даруют бонусы к урону и золоту</BASEFONT>", false, false);

            // Sidebar / Content Vertical Divider
            AddImageTiled(185, 78, 2, 388, 9354);

            // Sidebar: Categories (Вывод текущего Ранга всей группы)
            int catY = 82;
            foreach (BestiaryCategory cat in Enum.GetValues(typeof(BestiaryCategory)))
            {
                bool isSelected = cat == m_Category;
                int catGroupLevel = profile != null ? profile.GetGroupLevel(cat) : 0;

                int btnId = 100 + (int)cat;
                int normalBtn = isSelected ? 4006 : 4005;
                int pressBtn = 4007;

                AddButton(24, catY + 4, normalBtn, pressBtn, btnId, GumpButtonType.Reply, 0);

                // Окрашиваем текст: если ранг максимальный (5) — подсвечиваем сочным зеленым
                string catColor = isSelected ? "#00FFCC" : (catGroupLevel == 5 ? "#69F0AE" : "#ECEFF1");
                string catLabel = string.Format("<BASEFONT COLOR={0}>{1}</BASEFONT> <BASEFONT COLOR=#90A4AE>[Ранг {2}/5]</BASEFONT>", catColor, cat, catGroupLevel);
                AddHtml(45, catY + 5, 140, 20, catLabel, false, false);

                catY += 36;
            }

            // Right Panel: Category Content
            var creatureList = HunterBestiaryEngine.EntriesByCategory[m_Category];
            int catCount = creatureList.Count;
            int currentActiveGroupLevel = profile != null ? profile.GetGroupLevel(m_Category) : 0;

            int currentGroupDamage = currentActiveGroupLevel * 2;
            int currentGroupGold = currentActiveGroupLevel * 3;

            // Category Subheader
            AddHtml(200, 82, 400, 24, string.Format("<BASEFONT COLOR=#FFFFFF SIZE=4>Категория: {0}</BASEFONT>", m_Category), false, false);
            
            // Наглядно выводим текущие боевые бонусы для выбранной вкладки
            string groupBonusText = string.Format("<BASEFONT COLOR=#ECEFF1>Текущий Ранг: <BASEFONT COLOR=#00FFCC>{0} / 5</BASEFONT> <BASEFONT COLOR=#69F0AE>(+{1}% урон, +{2}% золото)</BASEFONT></BASEFONT>", currentActiveGroupLevel, currentGroupDamage, currentGroupGold);
            AddHtml(200, 106, 400, 20, groupBonusText, false, false);

            AddImageTiled(195, 128, 420, 2, 9354);

            // Pagination Calculations
            int maxPages = Math.Max(1, (int)Math.Ceiling(catCount / (double)EntriesPerPage));
            if (m_Page >= maxPages) m_Page = maxPages - 1;
            if (m_Page < 0) m_Page = 0;

            int startIndex = m_Page * EntriesPerPage;
            int endIndex = Math.Min(startIndex + EntriesPerPage, catCount);

            int entryY = 138;
            for (int i = startIndex; i < endIndex; i++)
            {
                var creature = creatureList[i];
                
                // Получаем точный опыт конкретного моба (0 - 300)
                int currentXp = profile != null ? profile.GetProgress(creature.Id) : 0;
                bool hasAnyProgress = currentXp > 0;
                bool isMaxed = currentXp >= 300;

                // Entry Frame
                AddBackground(195, entryY, 418, 34, isMaxed ? 9200 : 9300);
                AddAlphaRegion(197, entryY + 2, 414, 30);

                // Status Icon / Bullet
                if (isMaxed)
                {
                    AddImage(202, entryY + 6, 4017); // Зеленая звезда (Максимум изучено)
                }
                else if (hasAnyProgress)
                {
                    AddImage(202, entryY + 6, 4023); // Желтый маркер (В процессе изучения)
                }
                else
                {
                    AddImage(202, entryY + 6, 4020); // Серый пустой маркер
                }

                // Creature Display Name
                string nameColor = isMaxed ? "#69F0AE" : (hasAnyProgress ? "#FFFFFF" : "#B0BEC5");
                AddHtml(235, entryY + 7, 168, 20, string.Format("<BASEFONT COLOR={0}>{1}</BASEFONT>", nameColor, creature.DisplayName), false, false);

                // Progress & Experience Display
                if (isMaxed)
                {
                    AddHtml(410, entryY + 7, 195, 20, "<BASEFONT COLOR=#69F0AE>[ИЗУЧЕНО: 300/300 XP]</BASEFONT>", false, false);
                }
                else if (hasAnyProgress)
                {
                    string progressStr = string.Format("<BASEFONT COLOR=#00E5FF>[Прогресс: {0}/300 XP]</BASEFONT>", currentXp);
                    AddHtml(410, entryY + 7, 195, 20, progressStr, false, false);
                }
                else
                {
                    AddHtml(410, entryY + 7, 195, 20, "<BASEFONT COLOR=#78909C>[Не изучено: 0/300 XP]</BASEFONT>", false, false);
                }

                entryY += 38;
            }

            // Bottom Divider
            AddImageTiled(20, 468, 600, 2, 9354);

            // Page Navigation Controls
            if (m_Page > 0)
            {
                AddButton(200, 476, 4014, 4016, 11, GumpButtonType.Reply, 0);
                AddHtml(236, 478, 60, 20, "<BASEFONT COLOR=#ECEFF1>Previous</BASEFONT>", false, false);
            }

            if (m_Page < maxPages - 1)
            {
                AddButton(550, 476, 4005, 4007, 10, GumpButtonType.Reply, 0);
                AddHtml(510, 478, 40, 20, "<BASEFONT COLOR=#ECEFF1>Next</BASEFONT>", false, false);
            }

            AddHtml(360, 478, 120, 20, string.Format("<BASEFONT COLOR=#B0BEC5>Страница {0} из {1}</BASEFONT>", m_Page + 1, maxPages), false, false);

            // Close Button
            AddButton(30, 476, 4017, 4019, 0, GumpButtonType.Reply, 0);
            AddHtml(65, 478, 50, 20, "<BASEFONT COLOR=#FFFFFF>Close</BASEFONT>", false, false);
        }

        // Поддержка вашей рабочей версии NetState из Варианта 2
        public override void OnResponse(Server.Network.NetState sender, RelayInfo info)
        {
            if (sender == null) return;
            Mobile from = sender.Mobile;
            if (from == null) return;

            int id = info.ButtonID;

            if (id == 0)
            {
                return; // Закрыть окно
            }

            if (id == 10)
            {
                from.SendGump(new HunterBestiaryGump(from, m_Category, m_Page + 1));
                return;
            }

            if (id == 11)
            {
                from.SendGump(new HunterBestiaryGump(from, m_Category, Math.Max(0, m_Page - 1)));
                return;
            }

            if (id >= 100 && id < 150)
            {
                BestiaryCategory newCat = (BestiaryCategory)(id - 100);
                from.SendGump(new HunterBestiaryGump(from, newCat, 0));
                return;
            }
        }
    }
}
