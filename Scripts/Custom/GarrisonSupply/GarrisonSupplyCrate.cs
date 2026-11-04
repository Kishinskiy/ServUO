using System;
using Server;
using Server.Items;
using Server.Targeting;
using Server.Mobiles; // Явное подключение всех NPC торговцев
using Server.Engines.BulkOrders; // Подключение BOD системы
using Server.Custom.CityDevelopment;
using Server.Custom.TownBoards;

namespace Server.Custom.GarrisonSupply
{
    public enum SupplyType
    {
        Blacksmith, // Кузнец
        Carpentry,  // Плотник
        Tailor,     // Портной
        Cooking     // Повар
    }

    public class GarrisonSupplyCrate : Item
    {
        private bool m_IsSealed;
        private int m_RewardGold;
        private string m_ContentName;
        private SupplyType m_SupplyType;
        private int m_FoodCount;
        private int m_ResourceCount;
        private TownContractScroll m_TownContract;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsSealed { get => m_IsSealed; set { m_IsSealed = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int RewardGold { get => m_RewardGold; set { m_RewardGold = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public string ContentName { get => m_ContentName; set { m_ContentName = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public SupplyType ContainerType { get => m_SupplyType; set { m_SupplyType = value; UpdateAppearance(); } }

        [Constructable]
        public GarrisonSupplyCrate() : this(SupplyType.Blacksmith)
        {
        }

        [Constructable]
        public GarrisonSupplyCrate(SupplyType type) : base(0x09A8) // Графика деревянного ящика
        {
            m_SupplyType = type;
            m_IsSealed = false;
            m_RewardGold = 0;
            m_ContentName = "Пусто";
            m_FoodCount = 0;
            Weight = 5.0;

            UpdateAppearance();
        }

        public void UpdateAppearance()
        {
            if (m_IsSealed) return;

            // Настройка графики, цветов и названий под каждую профессию ремесленника
            switch (m_SupplyType)
            {
                case SupplyType.Blacksmith:
                    ItemID = 0x09A8; Hue = 2418; Name = "Ящик Снабжения Гарнизона (Кузнечный - Пустой)";
                    break;
                case SupplyType.Carpentry:
                    ItemID = 0x09A8; Hue = 1133; Name = "Ящик Строительных Припасов (Плотник - Пустой)";
                    break;
                case SupplyType.Tailor:
                    ItemID = 0x0E7A; Hue = 2417; Name = "Тюк Армейского Обмундирования (Портной - Пустой)";
                    break;
                case SupplyType.Cooking:
                    ItemID = 0x0FAE; Hue = 1424; Name = "Бочка Городского Провианта (Повар - Пустая)";
                    break;
            }
            InvalidateProperties();
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_IsSealed)
            {
                list.Add($"Содержимое: {m_ContentName}");
                if (m_TownContract != null)
                    list.Add($"Городской заказ: {m_ResourceCount}/{m_TownContract.RequiredCount} ед. ресурса");
                else
                    list.Add($"Выплата гарнизона: {m_RewardGold} золотых");
                list.Add("<BASEFONT COLOR=#69F0AE>[Дважды кликните, чтобы сдать профильному NPC]<BASEFONT COLOR=#FFFFFF>");
            }
            else
            {
                if (m_SupplyType == SupplyType.Cooking)
                    list.Add($"Заполнено провиантом: {m_FoodCount} / 100 шт. (Кликните на бочку и цельтесь в готовую еду)");
                else if (m_TownContract != null)
                    list.Add($"Городской заказ: {m_ResourceCount} / {m_TownContract.RequiredCount} ед. ресурса");
                else
                    list.Add("<BASEFONT COLOR=#FFE082>Используйте этот контейнер на заполненный малый BOD контракт вашей профессии или заполните сырьем по активному заказу с городской доски.<BASEFONT COLOR=#FFFFFF>");
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.Backpack == null || !IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            if (m_IsSealed)
            {
                from.SendMessage(m_TownContract != null
                    ? "Выберите Торгового Министра города из контракта, чтобы сдать контейнер вместе с заказом."
                    : "Выберите Торгового Министра развиваемого города, чтобы передать ему снабжение гарнизона.");
                from.Target = new InternalDeliverTarget(this);
            }
            else
            {
                TownContractScroll contract = GetActiveSupplyContract(from);
                if (contract != null)
                {
                    if (!IsCorrectCrateForResource(contract.ResourceClass))
                    {
                        from.SendMessage(0x22, $"Этот заказ требует контейнер другого торговца для ресурса [{contract.ResourceClass}].");
                        return;
                    }

                    from.SendMessage($"Выберите стопку ресурса [{contract.ResourceClass}] для упаковки в контейнер.");
                }
                else
                {
                    from.SendMessage(m_SupplyType == SupplyType.Cooking ? "Выберите готовую еду для загрузки в бочку." : "Выберите полностью заполненный малый BOD-контракт.");
                }
                from.Target = new InternalPackTarget(this);
            }
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (GetActiveSupplyContract(from) != null)
                return TryPackTownBoardResource(from, dropped, false);

            return false;
        }

        private static TownContractScroll GetActiveSupplyContract(Mobile from)
        {
            if (from?.Backpack == null)
                return null;

            Item[] contracts = from.Backpack.FindItemsByType(typeof(TownContractScroll));
            foreach (TownContractScroll contract in contracts)
            {
                if (!contract.Deleted && contract.ContractType == TownContractType.Supplies)
                {
                    if (contract.CheckExpiry(from))
                        continue;

                    return contract;
                }
            }

            return null;
        }

        private bool IsCorrectCrateForResource(string resourceClass)
        {
            return m_SupplyType == SupplyType.Blacksmith && resourceClass == "iron" ||
                   m_SupplyType == SupplyType.Tailor && resourceClass == "leather" ||
                   m_SupplyType == SupplyType.Carpentry && resourceClass == "wood";
        }

        private bool TryPackTownBoardResource(Mobile from, Item item, bool continueTargeting)
        {
            TownContractScroll contract = GetActiveSupplyContract(from);
            if (contract == null)
                return false;

            if (from.Backpack == null || !IsChildOf(from.Backpack) || m_IsSealed)
                return false;

            if (m_TownContract != null &&
                (m_TownContract.Deleted || m_TownContract.CheckExpiry(from)))
            {
                from.SendMessage(0x22, "Связанный с контейнером городской контракт больше недействителен.");
                return false;
            }

            if (!IsCorrectCrateForResource(contract.ResourceClass))
            {
                from.SendMessage(0x22, $"Этот заказ требует контейнер другого торговца для ресурса [{contract.ResourceClass}].");
                return false;
            }

            if (m_TownContract != null && m_TownContract != contract)
            {
                from.SendMessage(0x22, "Этот контейнер уже привязан к другому городскому заказу.");
                return false;
            }

            foreach (Item found in from.Backpack.FindItemsByType(typeof(GarrisonSupplyCrate)))
            {
                GarrisonSupplyCrate other = found as GarrisonSupplyCrate;
                if (other != null && other != this && other.m_TownContract == contract)
                {
                    from.SendMessage(0x22, "Для этого заказа уже используется другой BOD-контейнер.");
                    return false;
                }
            }

            if (item == null || item == this || !item.IsChildOf(from.Backpack) || !item.Stackable ||
                !MatchesResource(contract.ResourceClass, item))
            {
                from.SendMessage(0x22, $"В этот контейнер принимается только ресурс [{contract.ResourceClass}] из вашего рюкзака.");
                return false;
            }

            int amountToPack = Math.Min(item.Amount, contract.RequiredCount - m_ResourceCount);
            if (amountToPack <= 0)
                return false;

            m_TownContract = contract;
            m_ResourceCount += amountToPack;
            item.Amount -= amountToPack;
            if (item.Amount <= 0)
                item.Delete();

            m_ContentName = $"{m_ResourceCount} ед. ресурса [{contract.ResourceClass}]";
            InvalidateProperties();
            from.PlaySound(0x249);

            if (m_ResourceCount >= contract.RequiredCount)
            {
                m_IsSealed = true;
                if (m_SupplyType == SupplyType.Blacksmith || m_SupplyType == SupplyType.Carpentry)
                    ItemID = 0x09A9;
                Name = m_SupplyType == SupplyType.Blacksmith
                    ? "Запечатанный ящик сырья для городского заказа"
                    : m_SupplyType == SupplyType.Carpentry
                        ? "Запечатанный ящик строительных припасов для городского заказа"
                        : "Запечатанный тюк сырья для городского заказа";
                from.SendMessage(0x58, "BOD-контейнер заполнен и запечатан! Отнесите его вместе с контрактом Торговому Министру.");
            }
            else
            {
                from.SendMessage(0x35, $"Ресурс упакован: {m_ResourceCount}/{contract.RequiredCount}.");
                if (continueTargeting)
                    from.Target = new InternalPackTarget(this);
            }

            return true;
        }

        private static bool MatchesResource(string resourceClass, Item item)
        {
            return resourceClass == "iron" && item is BaseIngot ||
                   resourceClass == "leather" && (item is BaseLeather || item is BaseHides) ||
                   resourceClass == "wood" && (item is BaseWoodBoard || item is BaseLog);
        }

        // =========================================================================
        // ТАРГЕТ УПАКОВКИ КОНТРАКТОВ ИЛИ ЕДЫ 🛠️ (ВОССТАНОВЛЕН К ОРИГИНАЛУ)
        // =========================================================================
        private class InternalPackTarget : Target
        {
            private GarrisonSupplyCrate m_Container;

            public InternalPackTarget(GarrisonSupplyCrate container) : base(2, false, TargetFlags.None)
            {
                m_Container = container;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Container == null || m_Container.Deleted || from.Backpack == null ||
                    !m_Container.IsChildOf(from.Backpack) || m_Container.IsSealed)
                    return;

                if (GetActiveSupplyContract(from) != null)
                {
                    m_Container.TryPackTownBoardResource(from, targeted as Item, true);
                    return;
                }

                // ЛОГИКА ДЛЯ ПОВАРОВ (Загрузка еды напрямую)
                if (m_Container.ContainerType == SupplyType.Cooking)
                {
                    if (targeted is Food food &&
                        (food.RootParent == from || food.IsChildOf(from.Backpack)))
                    {
                        int amountToPack = Math.Min(food.Amount, 100 - m_Container.m_FoodCount);
                        if (amountToPack <= 0)
                        {
                            from.SendMessage(0x22, "Бочка уже заполнена.");
                            return;
                        }

                        m_Container.m_FoodCount += amountToPack;
                        m_Container.RewardGold += amountToPack * 15;
                        food.Consume(amountToPack);
                        from.PlaySound(0x57);

                        if (m_Container.m_FoodCount >= 100)
                        {
                            m_Container.IsSealed = true;
                            m_Container.ContentName = "100x Армейский Рацион Провианта";
                            m_Container.Name = "Запечатанная Бочка Городского Провианта";
                            from.SendMessage(0x35, "Бочка провианта полностью заполнена и заколочена!");
                        }
                        else
                        {
                            from.SendMessage($"Вы положили еду в бочку. Прогресс: {m_Container.m_FoodCount}/100.");
                        }
                        m_Container.InvalidateProperties();
                    }
                    else
                    {
                        from.SendMessage(0x22, "В эту бочку можно складывать только вашу готовую еду из рюкзака или руки.");
                    }
                    return;
                }

                // ЛОГИКА ДЛЯ ПРОФЕССИЙ С BOD КОНТРАКТАМИ
                if (targeted is SmallBOD bod &&
                    (bod.RootParent == from || bod.IsChildOf(from.Backpack)))
                {
                    bool isValidBOD = false;

                    if (m_Container.ContainerType == SupplyType.Blacksmith && bod.BODType == BODType.Smith) isValidBOD = true;
                    if (m_Container.ContainerType == SupplyType.Carpentry && bod.BODType == BODType.Carpentry) isValidBOD = true;
                    if (m_Container.ContainerType == SupplyType.Tailor && bod.BODType == BODType.Tailor) isValidBOD = true;

                    if (!isValidBOD)
                    {
                        from.SendMessage(0x22, "Этот контейнер не принимает контракты данной профессии!");
                        return;
                    }

                    if (bod.AmountMax <= 0 || !bod.Complete)
                    {
                        from.SendMessage(0x22, $"Этот контракт еще не завершен! ({bod.AmountCur}/{bod.AmountMax})");
                        return;
                    }

                    int calculatedGold = bod.AmountMax * 150;

                    if (bod.Material != BulkMaterialType.None) calculatedGold *= 2;
                    if (bod.RequireExceptional) calculatedGold = (int)(calculatedGold * 1.5);

                    m_Container.IsSealed = true;
                    m_Container.RewardGold = calculatedGold;

                    string itemTypeName = bod.Type != null ? bod.Type.Name : "Craft Equipment";
                    m_Container.ContentName = $"{bod.AmountMax}x {(bod.RequireExceptional ? "Exceptional " : "")}{itemTypeName}";

                    if (m_Container.ContainerType == SupplyType.Blacksmith) { m_Container.ItemID = 0x09A9; m_Container.Name = "Ящик со снаряжением для городского гарнизона"; }
                    else if (m_Container.ContainerType == SupplyType.Carpentry) { m_Container.ItemID = 0x09A9; m_Container.Name = "Заколоченный ящик строительных припасов"; }
                    else { m_Container.Name = "Завязанный тюк армейского обмундирования"; }

                    bod.Consume();
                    from.PlaySound(0x13E);
                    from.SendMessage(0x35, "Вы успешно упаковали припасы снабжения!");
                }
                else if (targeted is LargeBOD)
                {
                    from.SendMessage(0x22, "Большие комплексные контракты слишком огромны для этого ящика.");
                }
                else
                {
                    from.SendMessage(0x22, "Сюда можно упаковать только ваш заполненный малый BOD из рюкзака или руки!");
                }
            }
        }

        // =========================================================================
        // ТАРГЕТ СДАЧИ КОНТЕНТА ТОРГОВОМУ МИНИСТРУ 🏙️💰 (ОБНОВЛЕННЫЙ)
        // =========================================================================
        private class InternalDeliverTarget : Target
        {
            private GarrisonSupplyCrate m_Container;

            public InternalDeliverTarget(GarrisonSupplyCrate container) : base(3, false, TargetFlags.None)
            {
                m_Container = container;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Container == null || m_Container.Deleted || from.Backpack == null ||
                    !m_Container.IsChildOf(from.Backpack) || !m_Container.IsSealed)
                    return;

                // Изменено: теперь проверяем, что цель — именно Торговый Министр
                if (targeted is Server.Engines.CityLoyalty.TradeMinister minister)
                {
                    if (minister.Deleted || minister.Map != from.Map ||
                        !from.InRange(minister.Location, 3) || m_Container.Map != from.Map)
                    {
                        from.SendMessage(0x22, "Вы должны находиться рядом с Торговым Министром на той же карте.");
                        return;
                    }

                    TownContractScroll townContract = m_Container.m_TownContract;
                    if (townContract != null)
                    {
                        if (townContract.Deleted || !townContract.IsChildOf(from.Backpack) ||
                            townContract.CheckExpiry(from))
                        {
                            from.SendMessage(0x22, "Для сдачи контейнера нужен действующий городской контракт в вашем рюкзаке.");
                            return;
                        }

                        if (townContract.ContractType != TownContractType.Supplies ||
                            m_Container.m_ResourceCount < townContract.RequiredCount ||
                            !m_Container.IsCorrectCrateForResource(townContract.ResourceClass) ||
                            minister.City != townContract.City ||
                            townContract.ContractMap != from.Map)
                        {
                            from.SendMessage(0x22, "Контейнер не соответствует заполненному контракту или городу сдачи.");
                            return;
                        }

                        var townProfile = Server.Custom.CityDevelopment.CityDevelopmentMod.GetCityProfile(townContract.City, from.Map);
                        if (townProfile == null)
                            return;

                        switch (townContract.ResourceClass)
                        {
                            case "iron": townProfile.IronStock += townContract.RequiredCount; break;
                            case "leather": townProfile.LeatherStock += townContract.RequiredCount; break;
                            case "wood": townProfile.WoodStock += townContract.RequiredCount; break;
                            default:
                                from.SendMessage(0x22, "В контракте указан неизвестный тип сырья.");
                                return;
                        }

                        Server.Custom.CityDevelopment.CityDevelopmentMod.AddTreasuryRevenue(
                            townContract.City, from.Map, townContract.RewardTreasury);
                        Banker.Deposit(from, townContract.RewardGold, true);
                        Server.Engines.CityLoyalty.CityLoyaltySystem.GetCityInstance(townContract.City)
                            ?.AwardLove(from, townContract.RewardLoyalty);

                        minister.PublicOverheadMessage(
                            Server.Network.MessageType.Regular, 0x35, false, "Превосходно! Сырье принято на городской склад.");
                        from.SendMessage(
                            0x58,
                            $"[Доска Заказов]: Поставка принята. Получено {townContract.RewardGold:N0} золота и {townContract.RewardLoyalty} очков любви городу {townContract.City}.");
                        from.PlaySound(0x57);

                        townContract.Delete();
                        m_Container.Delete();
                        return;
                    }

                    // Напрямую берем город, к которому привязан данный Министр (минуя баги регионов)
                    Server.Engines.CityLoyalty.City targetCity = minister.City;

                    var cityProfile = Server.Custom.CityDevelopment.CityDevelopmentMod.GetCityProfile(targetCity, minister.Map);
                    if (cityProfile != null)
                    {
                        // 1. Начисляем золото игроку за работу
                        Gold gold = new Gold(m_Container.RewardGold);
                        if (!from.Backpack.TryDropItem(from, gold, false)) 
                            gold.MoveToWorld(from.Location, from.Map);

                        // 2. Пополняем складские запасы города напрямую в экономику из типа ящика
                        switch (m_Container.ContainerType)
                        {
                            case SupplyType.Blacksmith: 
                                cityProfile.EquipmentStock += 20; // Кузнечный ящик дает готовое снаряжение
                                break;
                            case SupplyType.Carpentry: 
                                cityProfile.WoodStock += 50;      // Строительный — доски
                                break;
                            case SupplyType.Tailor: 
                                cityProfile.EquipmentStock += 20; // Тюк одежды — обмундирование
                                break;
                            case SupplyType.Cooking: 
                                cityProfile.ProvisionsStock += 50; // Бочка еды — провизию
                                break;
                        }

                        // 3. Качаем опыт и уровень города (+50.0 supplies)
                        cityProfile.CurrentSupplies += 50.0;
                        if (Server.Custom.CityDevelopment.CityDevelopmentMod.ProcessCityLevelUps(targetCity, minister.Map))
                        {
                            World.Broadcast(0x35, true, $"[Развитие городов] Город {targetCity} достиг {cityProfile.Level} уровня благодаря поставкам гарнизона!");
                        }


                        // 5. Выдача набора саботажа (Шанс 10%)
                        if (Utility.RandomDouble() < 0.10)
                        {
                            FactionSabotageKit sabotageKit = new FactionSabotageKit();
                            if (from.Backpack == null || !from.Backpack.TryDropItem(from, sabotageKit, false))
                            {
                                sabotageKit.MoveToWorld(from.Location, from.Map);
                            }
                            from.SendMessage(0x35, "В благодарность за ценный вклад, городская разведка делится с вами набором для фракционного саботажа!");
                        }

                        // Финальные эффекты и сообщения
                        minister.PlaySound(0x2E6);
                        minister.Say("Превосходно! Припасы для нашего гарнизона доставлены в целости. Вот ваша плата ремесленника!");
                        from.SendMessage(0x35, $"Припасы успешно переданы Министру! Получено: {m_Container.RewardGold} золотых. Лояльность в городе {targetCity} выросла!");
                        
                        m_Container.Delete(); // Удаляем ящик из сумки
                        return;
                    }
                }
                else
                {
                    from.SendMessage(0x22, "Припасы для гарнизона можно сдать только Торговому Министру.");
                }
            }
        }
        public GarrisonSupplyCrate(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1);
            writer.Write(m_IsSealed);
            writer.Write(m_RewardGold);
            writer.Write(m_ContentName);
            writer.Write((int)m_SupplyType);
            writer.Write(m_FoodCount);
            writer.Write(m_ResourceCount);
            writer.Write((Item)m_TownContract);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_IsSealed = reader.ReadBool();
            m_RewardGold = reader.ReadInt();
            m_ContentName = reader.ReadString();
            m_SupplyType = (SupplyType)reader.ReadInt();
            m_FoodCount = reader.ReadInt();
            if (version >= 1)
            {
                m_ResourceCount = reader.ReadInt();
                m_TownContract = reader.ReadItem() as TownContractScroll;
            }
        }
    }
}