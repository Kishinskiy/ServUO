using System;
using Server;
using Server.Items;
using Server.Targeting;
using Server.Mobiles;
using Server.Engines.BulkOrders;

namespace Server.Custom.GarrisonSupply
{
    public class GarrisonSupplyCrate : Item
    {
        private bool m_IsSealed;
        private int m_RewardGold;
        private string m_ContentName;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsSealed { get => m_IsSealed; set { m_IsSealed = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int RewardGold { get => m_RewardGold; set { m_RewardGold = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public string ContentName { get => m_ContentName; set { m_ContentName = value; InvalidateProperties(); } }

        [Constructable]
        public GarrisonSupplyCrate() : base(0x09A8) // Графика деревянного ящика (Crate)
        {
            Name = "Ящик Снабжения Гарнизона (Пустой)";
            Weight = 5.0;
            Hue = 2418; // Стальной/железный оттенок дерева
            m_IsSealed = false;
            m_RewardGold = 0;
            m_ContentName = "Пусто";
        }

        // Тултип для вашей версии ServUO net48
        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_IsSealed)
            {
                list.Add($"Содержимое: {m_ContentName}");
                list.Add($"Выплата гарнизона: {m_RewardGold} золотых");
                list.Add("<BASEFONT COLOR=#69F0AE>[Дважды кликните, чтобы сдать Оружейнику]<BASEFONT COLOR=#FFFFFF>");
            }
            else
            {
                list.Add("<BASEFONT COLOR=#FFE082>Используйте этот ящик на полностью заполненный кузнечный BOD контракт, чтобы упаковать оружие для стражи города.<BASEFONT COLOR=#FFFFFF>");
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // Предмет должен быть в паке
                return;
            }

            if (m_IsSealed)
            {
                // Если ящик закрыт, даем прицел для сдачи Оружейнику!
                from.SendMessage("Выберите Оружейника (Weaponsmith) в развиваемом городе, чтобы передать ему припасы.");
                from.Target = new InternalDeliverTarget(this);
            }
            else
            {
                // Если ящик пустой, даем прицел на упаковку BOD
                from.SendMessage("Выберите полностью заполненный кузнечный BOD-контракт, чтобы упаковать его в ящик.");
                from.Target = new InternalPackTarget(this);
            }
        }

        // =========================================================================
        // ТАРГЕТ УПАКОВКИ BOD КОНТРАКТА В ЯЩИК 🛠️
        // =========================================================================
        private class InternalPackTarget : Target
        {
            private GarrisonSupplyCrate m_Crate;

            public InternalPackTarget(GarrisonSupplyCrate crate) : base(2, false, TargetFlags.None)
            {
                m_Crate = crate;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Crate == null || m_Crate.Deleted || !m_Crate.IsChildOf(from.Backpack) || m_Crate.IsSealed)
                    return;

                // Проверяем малый контракт в вашей версии ServUO
                if (targeted is SmallBOD bod)
                {
                    // ИСПРАВЛЕНО НА СТАНДАРТ: Проверяем тип контракта напрямую через перечисление BODType
                    if (bod.BODType == BODType.Smith)
                    {
                        if (bod.AmountCur < bod.AmountMax)
                        {
                            from.SendMessage(0x22, $"Этот контракт еще не завершен! Сначала докуйте предметы ({bod.AmountCur}/{bod.AmountMax}).");
                            return;
                        }

                        // Расчет награды золотом
                        int calculatedGold = bod.AmountMax * 150;

                        if (bod.Material != BulkMaterialType.None)
                            calculatedGold = (int)(calculatedGold * 2.0); // Х2 золота за цветной металл

                        if (bod.RequireExceptional)
                            calculatedGold = (int)(calculatedGold * 1.5); // +50% за мастерскую работу

                        m_Crate.IsSealed = true;
                        m_Crate.RewardGold = calculatedGold;

                        // Текстовое определение типа кованого предмета через тип из базы контракта
                        string itemTypeName = bod.Type != null ? bod.Type.Name : "Iron Equipment";

                        m_Crate.ContentName = $"{bod.AmountMax}x {(bod.RequireExceptional ? "Exceptional " : "")}{itemTypeName}";
                        m_Crate.Name = "Ящик со снаряжением для городского гарнизона";
                        m_Crate.ItemID = 0x09A9; // Меняем визуал на закрытый ящик

                        bod.Consume(); // Уничтожаем BOD свиток
                        from.PlaySound(0x13E); // Звук закрытия деревянной крышки
                        from.SendMessage(0x35, "Вы успешно укомплектовали и заколотили ящик снабжения!");
                    }
                    else
                    {
                        from.SendMessage(0x22, "Этот ящик принимает только контракты Кузнечного дела (Blacksmithing)!");
                    }
                }
                else if (targeted is LargeBOD)
                {
                    from.SendMessage(0x22, "Большие комплексные контракты (Large BOD) слишком огромны для этого ящика. Сдавайте только Small BOD.");
                }
                else
                {
                    from.SendMessage(0x22, "Сюда можно упаковать только заполненный кузнечный контракт!");
                }
            }
        }

        // =========================================================================
        // ТАРГЕТ СДАЧИ ЯЩИКА НПЦ ОРУЖЕЙНИКУ ПРЯМО В МИРЕ 🏙️💰
        // =========================================================================
        private class InternalDeliverTarget : Target
        {
            private GarrisonSupplyCrate m_Crate;

            public InternalDeliverTarget(GarrisonSupplyCrate crate) : base(3, false, TargetFlags.None)
            {
                m_Crate = crate;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Crate == null || m_Crate.Deleted || !m_Crate.IsChildOf(from.Backpack) || !m_Crate.IsSealed)
                    return;

                if (targeted is Weaponsmith vendor)
                {
                    if (!from.InRange(vendor.Location, 3))
                    {
                        from.SendMessage(0x22, "Вы слишком далеко от торговца!");
                        return;
                    }

                    if (vendor.Region != null && !string.IsNullOrEmpty(vendor.Region.Name))
                    {
                        foreach (Server.Engines.CityLoyalty.City c in Enum.GetValues(typeof(Server.Engines.CityLoyalty.City)))
                        {
                            if (vendor.Region.Name.IndexOf(c.ToString(), StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                // 1. Выдаем золото из казны игроку в пак
                                Gold gold = new Gold(m_Crate.RewardGold);
                                if (!from.Backpack.TryDropItem(from, gold, false))
                                {
                                    gold.MoveToWorld(from.Location, from.Map);
                                }

                                // 2. Начисляем очки развития этому городу (+50 очков опыта за ящик)
                                var cityProfile = Server.Custom.CityDevelopment.CityDevelopmentMod.GetCityProfile(c);
                                if (cityProfile != null)
                                {
                                    cityProfile.CurrentSupplies += 50.0;
                                }

                                // 3. Качаем Лояльность вендоров у игрока в этом регионе (+0.50 репутации)
                                Server.Custom.AdvancedNpcTrade.LoyaltyManager.AddPurchase(vendor, from, 0.50);

                                // Эффекты и фраза торговца
                                vendor.PlaySound(0x2E6); // Звук пересыпания монет
                                vendor.Say("Отлично! Эти мечи и доспехи помогут укрепить оборону нашего города. Вот ваша плата!");
                                from.SendMessage(0x35, $"Вы сдали оружие! Получено: {m_Crate.RewardGold} золотых. Лояльность торговцев {vendor.Name} выросла!");

                                m_Crate.Delete(); // Полностью уничтожаем ящик снабжения
                                return;
                            }
                        }
                    }

                    from.SendMessage(0x22, "Этот Оружейник находится вне зон кастомных городов. Отвезите ящик в Britain, Moonglow, Yew и т.д.");
                }
                else
                {
                    from.SendMessage(0x22, "Передать ящик снабжения можно только профессиональному Оружейнику (Weaponsmith)!");
                }
            }
        }

        public GarrisonSupplyCrate(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
            writer.Write(m_IsSealed);
            writer.Write(m_RewardGold);
            writer.Write(m_ContentName);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_IsSealed = reader.ReadBool();
            m_RewardGold = reader.ReadInt();
            m_ContentName = reader.ReadString();
        }
    }
}