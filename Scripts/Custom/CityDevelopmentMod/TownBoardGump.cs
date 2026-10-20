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
            DateTime offerDate = DateTime.UtcNow;
            m_HuntOffer = TownBulletinBoardSystem.CreateHuntOffer(city, map, offerDate);
            m_SupplyOffer = TownBulletinBoardSystem.CreateSupplyOffer(city, map, offerDate);
            int huntStage = TownBulletinBoardSystem.GetHuntDifficultyStage(DateTime.UtcNow);

            // Увеличена общая высота гампа с 545 до 630
            AddBackground(0, 0, 540, 630, 0x13BE);
            AddImageTiled(12, 12, 516, 32, 0xA40);

            // Заголовок: Насыщенный Gold (#FFD700)
            AddHtml(24, 16, 492, 25,
                $"<BASEFONT COLOR=#FFD700 SIZE=5><CENTER><B>ГОРОДСКИЕ ЗАКАЗЫ: {city.ToString().ToUpper()}</B></CENTER></BASEFONT>",
                false, false);

            // Подзаголовок: Светло-серый (#FFF0F5)
            AddHtml(24, 48, 492, 22,
                "<BASEFONT COLOR=#FFF0F5 SIZE=3><CENTER>Объявления обновляются раз в сутки. Принятый заказ остается у вас.</CENTER></BASEFONT>",
                false, false);

            // --- БЛОК 1: ОХОТА ---
            AddBackground(16, 78, 508, 156, 0x24AE); // Высота увеличена со 146 до 156
            AddHtml(28, 86, 480, 24, "<BASEFONT COLOR=#FFA500 SIZE=4><B>ЗАДАНИЕ НА ОХОТУ</B></BASEFONT>", false, false);
            if (m_HuntOffer != null)
            {
                AddHtml(32, 116, 470, 26,
                    $"<BASEFONT COLOR=#FFFFFF SIZE=4><B>Убить {m_HuntOffer.RequiredCount} {TownContractScroll.GetTargetName(m_HuntOffer.TargetCreature)}</B></BASEFONT>",
                    false, false);
                AddHtml(32, 148, 470, 22,
                    $"<BASEFONT COLOR=#FFFACD SIZE=3>Награда: {m_HuntOffer.RewardGold:N0} золота, {m_HuntOffer.RewardLoyalty} очков лояльности, {m_HuntOffer.RewardTreasury:N0} в казну.</BASEFONT>",
                    false, false);
                AddHtml(32, 174, 430, 22,
                    $"<BASEFONT COLOR=#98FB98 SIZE=2>Сложность {huntStage}/4. Уровень повышается каждые 30 дней; учитываются области охоты города.</BASEFONT>",
                    false, false);
                AddButton(468, 137, 0xFA5, 0xFA7, 1, GumpButtonType.Reply, 0); // Кнопка смещена чуть ниже для центровки
                AddHtml(32, 204, 470, 18,
                    "<BASEFONT COLOR=#A9A9A9 SIZE=2>Цель и объем выбираются случайно из местного пула.</BASEFONT>",
                    false, false);
            }

            // --- БЛОК 2: СНАБЖЕНИЕ ---
            // Сдвинут по Y со 232 на 250, высота увеличена со 112 до 120
            AddBackground(16, 250, 508, 120, 0x24AE);
            AddHtml(28, 258, 480, 24, "<BASEFONT COLOR=#00FFFF SIZE=4><B>ЗАДАНИЕ НА СНАБЖЕНИЕ</B></BASEFONT>", false, false);
            AddHtml(32, 288, 430, 25,
                $"<BASEFONT COLOR=#FFFFFF SIZE=3>Доставить {m_SupplyOffer.RequiredCount} ед. ресурса: {GetResourceName(m_SupplyOffer.ResourceClass)}.</BASEFONT>",
                false, false);
            AddHtml(32, 318, 430, 22,
                $"<BASEFONT COLOR=#FFFACD SIZE=2>Награда: {m_SupplyOffer.RewardGold:N0} золота, {m_SupplyOffer.RewardLoyalty} очков лояльности и {m_SupplyOffer.RewardTreasury:N0} в казну.</BASEFONT>",
                false, false);
            AddButton(468, 295, 0xFA5, 0xFA7, 2, GumpButtonType.Reply, 0); // Кнопка смещена

            // --- БЛОК 3: РЕМЕСЛО ---
            // Сдвинут по Y со 352 на 386, высота увеличена со 142 до 180
            AddBackground(16, 386, 508, 180, 0x24AE);
            AddHtml(28, 394, 480, 24, "<BASEFONT COLOR=#3ADF00 SIZE=4><B>РЕМЕСЛЕННЫЕ ЗАКАЗЫ</B></BASEFONT>", false, false);
            AddHtml(28, 422, 480, 34,
                "<BASEFONT COLOR=#E0E0E0 SIZE=3>Выберите категорию: объем заказа случайный (от 1 до 4 базовых объемов); за крупную поставку выше и выплата.</BASEFONT>",
                false, false);

            // Кнопки категорий смещены ниже под новый текст
            AddCategoryButton(32, 470, 3, "Сырье", 90);
            AddCategoryButton(144, 470, 4, "Оружие / броня", 115);
            AddCategoryButton(278, 470, 5, "Боеприпасы", 100);
            AddCategoryButton(390, 470, 6, "Провизия", 100);
            AddCategoryButton(32, 510, 7, "Магические материалы", 180);

            // --- ПОДВАЛ ГАМПА ---
            // Сдвинут по Y со 504 на 585
            AddHtml(28, 585, 490, 22,
                $"<BASEFONT COLOR=#B0C4DE SIZE=2>Ремесленная репутация: {ArtisanSupplySystem.GetReputation(from)}  |  Городской бонус к крафту: +{CityDevelopmentMod.GetCraftingChanceBonus(from) * 100:0}%  |  Ремесленный заказ: раз в 24 ч.</BASEFONT>",
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
                $"<BASEFONT COLOR=#FFF0C0 SIZE=2><B>{label}</B></BASEFONT>",
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
                        ? $"Принят контракт: убить {contract.RequiredCount} {TownContractScroll.GetTargetName(contract.TargetCreature)}."
                        : $"Принят контракт: доставить {contract.RequiredCount} ед. ресурса [{contract.ResourceClass}].");
                    from.SendMessage(0x35,
                        $"Награда: {contract.RewardGold:N0} золота, {contract.RewardLoyalty} очков лояльности и {contract.RewardTreasury:N0} золота в городскую казну.");
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
            if (from.Backpack != null && from.Backpack.FindItemByType(typeof(TownContractScroll)) != null)
            {
                from.SendMessage(0x22, "Сначала завершите активный городской контракт.");
                return true;
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
