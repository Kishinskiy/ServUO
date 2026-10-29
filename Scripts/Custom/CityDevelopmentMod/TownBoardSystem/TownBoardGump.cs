using System;
using Server;
using Server.Gumps;
using Server.Network;
using Server.Custom.CityDevelopment;

namespace Server.Custom.TownBoards
{
    public class TownBoardGump : Gump
    {
        private Server.Engines.CityLoyalty.City m_City;
        private Map m_Map;
        private TownBoardOffer m_HuntOffer;
        private TownBoardOffer m_SupplyOffer;

        public TownBoardGump(Mobile from, Server.Engines.CityLoyalty.City city, Map map) : base(100, 100)
        {
            m_City = city;
            m_Map = map;
            AddPage(0);
            
            // Генерируем предложения, передавая объект игрока для расчета масштабирования от лояльности
            m_HuntOffer = TownBulletinBoardSystem.CreateHuntOffer(city, map, from);
            m_SupplyOffer = TownBulletinBoardSystem.CreateSupplyOffer(city, map, from);

            int w = 580;
            int h = 600;

            AddBackground(0, 0, w, h, 0xBB8);
            AddAlphaRegion(2, 2, w - 4, h - 4);

            AddImageTiled(10, 12, w - 20, 32, 0xA40);
            AddAlphaRegion(10, 12, w - 20, 32);

            AddHtml(20, 16, w - 40, 25,
                $"<BASEFONT COLOR=#FFFF00 SIZE=5><CENTER><B>ГОРОДСКИЕ ЗАКАЗЫ: {city.ToString().ToUpper()}</B></CENTER></BASEFONT>",
                false, false);

            AddHtml(20, 52, w - 40, 25,
                "<BASEFONT COLOR=#F0F0F0 SIZE=3><CENTER>Объявления обновляются раз в сутки. Принятый заказ остается у вас.</CENTER></BASEFONT>",
                false, false);

            // ==========================================
            // --- БЛОК 1: ОХОТА (БЕЗ РЕГИОНОВ) ---
            // ==========================================
            AddAlphaRegion(15, 85, w - 30, 135);
            AddHtml(25, 90, 500, 24, "<BASEFONT COLOR=#FF9900 SIZE=4><B>ЗАДАНИЕ НА ОХОТУ</B></BASEFONT>", false, false);

            if (m_HuntOffer != null)
            {
                AddHtml(30, 118, 480, 26,
                    $"<BASEFONT COLOR=#FFFFFF SIZE=4><B>Убить {m_HuntOffer.RequiredCount} {TownContractScroll.GetTargetName(m_HuntOffer.TargetCreature)}</B></BASEFONT>",
                    false, false);

                AddHtml(30, 146, 480, 24,
                    $"<BASEFONT COLOR=#00FF00 SIZE=3>Награда: {m_HuntOffer.RewardGold:N0} золота, {m_HuntOffer.RewardLoyalty} лояльности, {m_HuntOffer.RewardTreasury:N0} в казну.</BASEFONT>",
                    false, false);

                // Вычисляем и выводим множитель сложности от лояльности
                double loyaltyPoints = TownBulletinBoardSystem.GetPlayerLoyaltyPoints(city, from);
                int difficultyStage = TownBulletinBoardSystem.GetDifficultyStageByLoyalty(loyaltyPoints);
                
                AddHtml(30, 172, 450, 24,
                    $"<BASEFONT COLOR=#87CEFA SIZE=2>Сложность: Этап {difficultyStage} (Множитель: x{1.0 + (difficultyStage - 1) * 0.5:F1}). Растет от очков лояльности города.</BASEFONT>",
                    false, false);

                AddButton(525, 120, 0xFA5, 0xFA7, 1, GumpButtonType.Reply, 0);
            }

            AddImageTiled(15, 228, w - 30, 2, 0xA40);

            // ==========================================
            // --- БЛОК 2: СНАБЖЕНИЕ ---
            // ==========================================
            AddAlphaRegion(15, 238, w - 30, 100);
            AddHtml(25, 242, 500, 24, "<BASEFONT COLOR=#00FFFF SIZE=4><B>ЗАДАНИЕ НА СНАБЖЕНИЕ</B></BASEFONT>", false, false);

            if (m_SupplyOffer != null)
            {
                AddHtml(30, 270, 480, 26,
                    $"<BASEFONT COLOR=#FFFFFF SIZE=3>Доставить {m_SupplyOffer.RequiredCount} ед. ресурса: <B>{GetResourceName(m_SupplyOffer.ResourceClass)}</B>.</BASEFONT>",
                    false, false);
                AddHtml(30, 298, 480, 24,
                    $"<BASEFONT COLOR=#00FF00 SIZE=2>Награда: {m_SupplyOffer.RewardGold:N0} золота, {m_SupplyOffer.RewardLoyalty} лояльности и {m_SupplyOffer.RewardTreasury:N0} в казну.</BASEFONT>",
                    false, false);

                AddButton(525, 272, 0xFA5, 0xFA7, 2, GumpButtonType.Reply, 0);
            }

            AddImageTiled(15, 346, w - 30, 2, 0xA40);

            // ==========================================
            // --- БЛОК 3: РЕМЕСЛО ---
            // ==========================================
            AddAlphaRegion(15, 356, w - 30, 180);
            AddHtml(25, 360, 500, 24, "<BASEFONT COLOR=#FF66CC SIZE=4><B>РЕМЕСЛЕННЫЕ ЗАКАЗЫ</B></BASEFONT>", false, false);

            AddHtml(25, 386, w - 50, 40,
                "<BASEFONT COLOR=#E0E0E0 SIZE=3>Выберите категорию:</BASEFONT>",
                false, false);

            AddCategoryButton(40, 432, 3, "Сырье", 120);
            AddCategoryButton(205, 432, 4, "Оружие / броня", 160);
            AddCategoryButton(390, 432, 5, "Боеприпасы", 130);

            AddCategoryButton(40, 472, 6, "Провизия", 120);
            AddCategoryButton(205, 472, 7, "Магические материалы", 220);

            AddImageTiled(15, 546, w - 30, 2, 0xA40);

            // ==========================================
            // --- ПОДВАЛ ГАМПА ---
            // ==========================================
            AddHtml(20, 558, w - 40, 30,
                $"<BASEFONT COLOR=#ADD8E6 SIZE=3><CENTER>Репутация: <B>{ArtisanSupplySystem.GetReputation(from)}</B>  |  Бонус к крафту: <B>+{CityDevelopmentMod.GetCraftingChanceBonus(from) * 100:0}%</B>  |  Заказ: раз в 24 ч.</CENTER></BASEFONT>",
                false, false);
        }

        private static string GetResourceName(string resource)
        {
            switch (resource)
            {
                case "iron": return "железо";
                case "leather": return "кожа";
                default: return "древесина";
            }
        }

        private void AddCategoryButton(int x, int y, int buttonId, string label, int width)
        {
            AddButton(x, y, 0xFA5, 0xFA7, buttonId, GumpButtonType.Reply, 0);
            AddHtml(x + 22, y - 1, width, 28,
                $"<BASEFONT COLOR=#FFFFFF SIZE=2><B>{label}</B></BASEFONT>",
                false, false);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;
            if (from == null || !from.Alive) return;

            if (info.ButtonID >= 3 && info.ButtonID <= 7)
            {
                if (!ArtisanSupplySystem.CanTakeOrder(from))
                {
                    from.SendMessage(0x22, "Городская управа принимает новый ремесленный заказ раз в 24 часа.");
                    return;
                }

                if (HasActiveTownContract(from))
                    return;

                ArtisanSupplySystem.CreateOrder(from, m_City, (ArtisanSupplyCategory)(info.ButtonID - 3));
                return;
            }

            if (info.ButtonID == 1 || info.ButtonID == 2)
            {
                if (HasActiveTownContract(from))
                    return;

                bool bounty = info.ButtonID == 1;
                TownBoardOffer offer = bounty ? m_HuntOffer : m_SupplyOffer;

                if (offer == null)
                    return;

                TownContractScroll contract = new TownContractScroll(
                    m_City,
                    bounty ? TownContractType.Bounty : TownContractType.Supplies,
                    offer.ResourceClass,
                    offer.RequiredCount,
                    offer.TargetCreature,
                    offer.RewardGold,
                    offer.RewardLoyalty,
                    offer.RewardTreasury,
                    m_Map);

                if (from.AddToBackpack(contract))
                {
                    from.SendMessage(0x35, bounty
                        ? $"Принят контракт: убить {contract.RequiredCount} {TownContractScroll.GetTargetName(contract.TargetCreature)} в любом регионе мира."
                        : $"Принят контракт: доставить {contract.RequiredCount} ед. ресурса [{contract.ResourceClass}].");
                }
                else
                {
                    contract.Delete();
                    from.SendMessage(0x22, "Освободите место в рюкзаке для городского контракта.");
                }
            }
        }

        private static bool HasActiveTownContract(Mobile from)
        {
            if (from.Backpack != null)
            {
                TownContractScroll contract = from.Backpack.FindItemByType(typeof(TownContractScroll)) as TownContractScroll;
                if (contract != null)
                {
                    if (contract.IsExpired)
                    {
                        contract.Delete();
                        from.SendMessage(0x22, "Ваш активный контракт истек и был удален.");
                        return false;
                    }
                    else
                    {
                        from.SendMessage(0x22, "Сначала завершите активный городской контракт.");
                        return true;
                    }   
                }
            }

            if (ArtisanSupplySystem.HasActiveDeed(from))
            {
                from.SendMessage(0x22, "Сначала выполните или удалите активный ремесленный заказ.");
                return true;
            }

            return false;
        }
    }
}
