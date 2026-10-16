using System;
using System.Collections.Generic;
using Server;
using Server.Gumps;
using Server.Network;
using Server.Items;
using Server.Engines.XmlSpawner2;
using Server.Custom.GhostMechanics;

namespace Server.Custom.GhostMechanics
{
    public class EtherealTravelGump : Gump
    {
        private Mobile m_From;
        private List<EtherealShrine> m_AvailableShrines;
        private int m_Page;
        public static readonly int TeleportCost = 25; // Стоимость телепортации
        private const int PageSize = 8;
        private const int PreviousPageButton = 1001;
        private const int NextPageButton = 1002;

        public EtherealTravelGump(Mobile from, int page = 0) : base(150, 100)
        {
            m_From = from;
            m_AvailableShrines = new List<EtherealShrine>();
            m_Page = Math.Max(0, page);

            Closable = true;
            Disposable = true;
            Dragable = true;
            Resizable = false;

            // Достаем аттачмент открытых точек игрока
            var wpData = XmlAttach.FindAttachment(from, typeof(EtherealWaypointsAttachment), "DiscoveredWaypoints") as EtherealWaypointsAttachment;
            // Достаем баланс эссенции игрока
            XmlValue pointsData = XmlAttach.FindAttachment(from, typeof(XmlValue), "EtherealEssence") as XmlValue;
            int essence = pointsData != null ? pointsData.Value : 0;

            // Собираем все шрайны мира, которые игрок открыл при жизни
            if (wpData != null)
            {
                foreach (Item item in World.Items.Values)
                {
                    if (item is EtherealShrine shrine && wpData.HasShrine(shrine.ShrineName) && shrine.Map == from.Map)
                    {
                        m_AvailableShrines.Add(shrine);
                    }
                }
            }

            // РИСУЕМ КРАСИВЫЙ ПРИЗРАЧНЫЙ ИНТЕРФЕЙС
            AddPage(0);
            AddBackground(0, 0, 360, 320, 9270); // Темный мистический фон
            AddAlphaRegion(10, 10, 340, 300);

            AddLabel(30, 20, 0x482, "Хроники Духов: Эфирные Шрайны");
            AddLabel(30, 45, 0x35, $"Ваша Эссенция: {essence} ед. (Цена прыжка: {TeleportCost})");

            if (m_AvailableShrines.Count == 0)
            {
                AddLabel(30, 120, 0x22, "У вас нет доступных точек телепортации.");
                AddLabel(30, 140, 0x22, "Активируйте Шрайны при жизни в мире смертных!");
                return;
            }

            int pageCount = (m_AvailableShrines.Count + PageSize - 1) / PageSize;
            m_Page = Math.Min(m_Page, pageCount - 1);
            int startIndex = m_Page * PageSize;
            int endIndex = Math.Min(startIndex + PageSize, m_AvailableShrines.Count);

            // Список прокрутки точек
            int y = 80;
            for (int i = startIndex; i < endIndex; i++)
            {
                EtherealShrine target = m_AvailableShrines[i];
                
                // Кнопка телепорта
                AddButton(25, y + 2, 4005, 4007, i - startIndex + 1, GumpButtonType.Reply, 0);
                // Название шрайна
                AddLabel(60, y, 0x482, target.ShrineName);

                y += 25;
            }

            AddLabel(150, 285, 0x35, $"{m_Page + 1} / {pageCount}");
            if (m_Page > 0)
            {
                AddButton(25, 282, 4014, 4016, PreviousPageButton, GumpButtonType.Reply, 0);
            }

            if (m_Page < pageCount - 1)
            {
                AddButton(300, 282, 4005, 4007, NextPageButton, GumpButtonType.Reply, 0);
            }
        }

        // ИСПРАВЛЕНО: Тип аргумента заменен на абсолютно универсальный RelayInfo, поддерживаемый всеми ревизиями RunUO/ServUO
        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = m_From;
            if (from == null || from.Alive || info.ButtonID == 0) return;

            if (info.ButtonID == PreviousPageButton || info.ButtonID == NextPageButton)
            {
                int nextPage = m_Page + (info.ButtonID == NextPageButton ? 1 : -1);
                from.SendGump(new EtherealTravelGump(from, nextPage));
                return;
            }

            int index = (m_Page * PageSize) + info.ButtonID - 1;
            if (index >= 0 && index < m_AvailableShrines.Count)
            {
                EtherealShrine dest = m_AvailableShrines[index];
                if (dest == null || dest.Deleted) return;

                // Проверяем эссенцию перед самым прыжком
                XmlValue pointsData = XmlAttach.FindAttachment(from, typeof(XmlValue), "EtherealEssence") as XmlValue;
                int currentEssence = pointsData != null ? pointsData.Value : 0;

                if (currentEssence < TeleportCost)
                {
                    from.SendMessage(0x22, $"Вам не хватает призрачной энергии! Требуется: {TeleportCost}, у вас: {currentEssence}.");
                    return;
                }

                // Списываем плату
                pointsData.Value -= TeleportCost;

                // Переносим призрака к шрайну
                from.MoveToWorld(dest.Location, dest.Map);
                from.PlaySound(0x1FC); // Загробный звук телепортации
                
                // Визуальный партикл на точке прибытия
                Effects.SendLocationParticles(EffectItem.Create(dest.Location, dest.Map, TimeSpan.FromSeconds(1.0)), 0x3709, 10, 15, 0x482, 0, 5024, 0);
                from.SendMessage(0x482, $"Вы потратили {TeleportCost} эссенции. Вы перенесены к святилищу: {dest.ShrineName}.");
            }
        }
    }
}
