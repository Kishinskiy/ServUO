using System;
using Server;
using Server.Targeting;
using Server.Mobiles;
using Server.Custom.CityDevelopment;

namespace Server.Custom.CityDevelopment
{
    public class FactionSabotageKit : Item
    {
        [Constructable]
        public FactionSabotageKit() : base(0x1EBA) // Графика набора инструментов
        {
            Name = "Набор для фракционного саботажа снабжения";
            Hue = 1175; // Системный ядовито-зеленый цвет фракционных диверсий
            Weight = 2.0;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || from.Deleted || !from.Alive) return;

            if (from.Backpack == null || !IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // Предмет должен быть в вашем рюкзаке
                return;
            }

            // ТРЕБОВАНИЕ 1: Игрок обязан быть невидимым (Hiding или заклинание Invisibility)
            if (!from.Hidden)
            {
                from.SendMessage(0x22, "Вы должны находиться в состоянии невидимости или скрытности, чтобы начать саботаж.");
                return;
            }

            from.SendMessage("Выберите Городской Камень (City Stone) для проведения скрытной диверсии на складах.");
            from.Target = new InternalSabotageTarget(this);
        }

        private class InternalSabotageTarget : Target
        {
            private readonly FactionSabotageKit m_Kit;

            public InternalSabotageTarget(FactionSabotageKit kit) : base(3, false, TargetFlags.None)
            {
                m_Kit = kit;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Kit == null || m_Kit.Deleted || from == null || !from.Alive || targeted == null) 
                    return;

                if (from.Backpack == null || m_Kit.Map != from.Map || !m_Kit.IsChildOf(from.Backpack))
                {
                    from.SendMessage(0x22, "Набор для саботажа должен находиться в вашем рюкзаке.");
                    return;
                }

                // Снова проверяем скрытность непосредственно в момент применения таргета
                if (!from.Hidden)
                {
                    from.SendMessage(0x22, "Вас заметили! Диверсия сорвана до начала.");
                    return;
                }

                // Проверяем тип целевого объекта (поддерживает базовый CityStone)
                // Если ваш кастомный камень называется иначе, замените Item на ваш класс камня
                Item cityStone = targeted as Item; 
                if (cityStone == null || cityStone.Deleted || !from.InRange(cityStone.GetWorldLocation(), 3))
                {
                    from.SendMessage(0x22, "Целью должен быть Городской Камень в пределах досягаемости.");
                    return;
                }

                // Пытаемся определить город по региону, в котором установлен камень
                Region stoneRegion = Region.Find(cityStone.Location, cityStone.Map);
                if (cityStone.Map == null || cityStone.Map == Map.Internal || stoneRegion == null)
                {
                    from.SendMessage(0x22, "Не удалось определить регион городского камня.");
                    return;
                }

                Server.Engines.CityLoyalty.City targetCity = Server.Engines.CityLoyalty.City.Britain;
                bool cityFound = false;

                foreach (Server.Engines.CityLoyalty.City c in Enum.GetValues(typeof(Server.Engines.CityLoyalty.City)))
                {
                    var cityInstance = Server.Engines.CityLoyalty.CityLoyaltySystem.GetCityInstance(c);
                    if (cityInstance != null && cityInstance.Definition != null && cityInstance.Definition.Region != null &&
                        cityInstance.Definition.Region.IsPartOf(stoneRegion))
                    {
                        targetCity = c;
                        cityFound = true;
                        break;
                    }
                }

                if (!cityFound)
                {
                    from.SendMessage(0x22, "Этот камень не принадлежит ни одному известному городу лояльности.");
                    return;
                }

                var cityProfile = CityDevelopmentMod.GetCityProfile(targetCity, cityStone.Map);
                if (cityProfile == null) return;

                // =========================================================================
                // 🧮 РАСЧЕТ ШАНСА УСПЕХА НА ОСНОВЕ SKILLS (STEALTH + STEALING)
                // =========================================================================
                double stealthSkill = from.Skills[SkillName.Stealth].Value;
                double stealingSkill = from.Skills[SkillName.Stealing].Value;

                // Базовый шанс: среднее арифметическое двух навыков, деленное на 3
                // (При ГМ Стелсе 100 и ГМ Снилинге 100 -> (100 + 100) / 2 / 3 = ~33.3%)
                double successChance = ((stealthSkill + stealingSkill) / 2.0) / 300.0;

                // Жесткий кап: шанс успешной активации не может превышать 40% (0.40)
                if (successChance > 0.40)
                {
                    successChance = 0.40;
                }

                // Если у игрока вообще нет этих воровских навыков, даем минимальный шанс в 1%
                if (successChance <= 0.0)
                {
                    successChance = 0.01;
                }

                // Кубики судьбы
                if (Utility.RandomDouble() < successChance)
                {
                    // =========================================================================
                    // 🎉 УСПЕШНЫЙ САБОТАЖ
                    // =========================================================================
                    cityProfile.CurrentSupplies -= 40.0;
                    if (cityProfile.CurrentSupplies < 0.0) cityProfile.CurrentSupplies = 0.0;

                    // Отправляем локальный шпионский алерт в регион
                    string alert = string.Format("Внимание! Тайные лазутчики совершили диверсию у городского камня {0} ({1})!", targetCity, cityStone.Map.Name);
                    CityNotificationSystem.LocalCityBroadcast(targetCity, cityStone.Map, alert);

                    from.PlaySound(0x3C4); // Звук порчи/взрыва механизмов
                    from.SendMessage(0x35, $"Диверсия успешна! Город {targetCity} потерял -40 очков снабжения.");

                    m_Kit.Delete(); // Уничтожаем использованный начертательный инструмент саботажа
                }
                else
                {
                    // =========================================================================
                    // 🛑 НЕУДАЧА И НАКАЗАНИЕ (КРИМИНАЛ)
                    // =========================================================================
                    from.Hidden = false; // Игрок мгновенно выходит из инвиза/хайдинга
                    from.CriminalAction(true); // Навешиваем статус серого криминала (Гварды фас!)
                    
                    from.PlaySound(0x54A); // Тревожный звук провала
                    from.SendMessage(0x22, "Ваши инструменты сорвались с лязгом! Вы раскрыли себя и объявлены преступником гарнизона!");
                    
                    // Оповещаем находящихся рядом NPC или игроков криком камня
                    cityStone.PublicOverheadMessage(Network.MessageType.Regular, 0x22, false, "Система безопасности: Обнаружена попытка взлома!");
                }
            }
        }

        public FactionSabotageKit(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
}
