using System;
using System.Collections.Generic;
using Server;
using Server.Gumps;
using Server.Network;
using Server.Mobiles;
using Server.Items;

namespace Server.Custom.MercenarySystem
{
    public class MercenaryBuybackGump : Gump
    {
        private readonly PlayerMobile m_Player;
        private readonly List<FallenMercenaryData> m_FallenList;

        public MercenaryBuybackGump(PlayerMobile from, List<FallenMercenaryData> list) : base(150, 100)
        {
            m_Player = from;
            m_FallenList = list;

            AddPage(0);
            AddBackground(0, 0, 380, 300, 0x13BE); // Кованая железная рамка гильдии
            AddAlphaRegion(10, 10, 360, 280);

            AddLabel(40, 20, 0x35, "Выкуп павших Наёмников из Эфира");
            AddImageTiled(30, 45, 320, 2, 0x2424);

            int y = 60;
            for (int i = 0; i < list.Count; i++)
            {
                FallenMercenaryData data = list[i];
                
                // Фиксированная цена страховки / выкупа: ровно 25,000 золотых за спасение прокачанной куклы
                int buybackCost = 25000;

                AddButton(25, y + 2, 0x15E1, 0x15E5, i + 1, GumpButtonType.Reply, 0);
                AddLabel(60, y, 0x58, $"{data.MercName} ({data.Class})");
                AddLabel(230, y, 0x3F, $"Ур. {data.Level} [{buybackCost}gp]");

                y += 35;
                if (y > 220) break; // Лимит вывода на страницу
            }

            AddButton(25, 250, 0xFB1, 0xFB2, 0, GumpButtonType.Reply, 0);
            AddLabel(60, 252, 0x22, "Закрыть меню");
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (m_Player == null || m_Player.Deleted || !m_Player.Alive || info.ButtonID == 0) return;

            int index = info.ButtonID - 1;
            if (index >= 0 && index < m_FallenList.Count)
            {
                FallenMercenaryData data = m_FallenList[index];
                int cost = 25000;

                // Списываем золото прямо из банка игрока нативно
                if (Server.Mobiles.Banker.Withdraw(m_Player, cost))
                {
                    m_Player.PlaySound(0x2E6); // Звук монет

                    // Воскрешаем наёмника со всей его историей!
                    BaseMercenary restoredMerc = new BaseMercenary(data.Class);
                    restoredMerc.Name = data.MercName;
                    
                    // Заливаем обратно уровень и опыт
                    restoredMerc.MercExp = data.Exp; 
                    
                    // Возвращаем все сохраненные вещи ему на тело!
                    foreach (Item item in data.EquippedItems)
                    {
                        item.Movable = false; // Возвращаем статус экипировки спутника
                        restoredMerc.AddItem(item);
                    }

                    restoredMerc.MoveToWorld(m_Player.Location, m_Player.Map);
                    restoredMerc.SetControlMaster(m_Player);
                    restoredMerc.ControlTarget = m_Player;
                    restoredMerc.ControlOrder = OrderType.Follow;

                    // Удаляем из реестра погибших
                    MercenaryRescueSystem.RemoveFromRoster(m_Player, data);

                    m_Player.SendMessage(0x35, $"[Успех]: Вы заплатили {cost}gp. Наёмник {data.MercName} ({data.Class}) 100% успешно возвращен в ваш отряд со всей экипировкой и уровнем!");
                }
                else
                {
                    m_Player.SendMessage(0x22, $"[Отказ]: В вашем банке не хватает {cost} золотых монет для оплаты выкупа контрактника.");
                }
            }
        }
    }
}
