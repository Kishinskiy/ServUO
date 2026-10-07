using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Server;
using Server.Items;
using Server.Regions;
using Server.Targeting;

namespace Server.Custom.Farms
{
    public enum VegetableType
    {
        Onion,
        Potato,
        Cabbage,
        Carrot,
        Lettuce,
        Pumpkin
    }


    // =========================================================================
    // 2. УНИВЕРСАЛЬНЫЙ ТАРГЕТ ПОСАДКИ ДЛЯ СЕМЯН
    // =========================================================================
    public class FarmVegetableTarget : Target
    {
        private Item m_Seed;
        private VegetableType m_Type;

        public FarmVegetableTarget(Item seed, VegetableType type) : base(3, true, TargetFlags.None)
        {
            m_Seed = seed;
            m_Type = type;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_Seed == null || m_Seed.Deleted || !m_Seed.IsChildOf(from.Backpack)) return;

            IPoint3D p = targeted as IPoint3D;
            if (p == null) return;

            Point3D loc = new Point3D(p.X, p.Y, p.Z);
            Map map = from.Map;

            if (!from.InRange(loc, 3))
            {
                from.SendLocalizedMessage(500446); // Вы слишком далеко.
                return;
            }
            LandTile landTile = map.Tiles.GetLandTile(loc.X, loc.Y);
            int cleanLandID = landTile.ID & 0x3FFF;
            Static customGround = targeted as Static;
            bool isCustomPlowed = (customGround != null && customGround.ItemID == 0x31F4);

            bool isStandardField = (cleanLandID >= 0x150 && cleanLandID <= 0x15C); // Стандартная пашня UO
            bool isFurrowsField = (cleanLandID >= 0x9 && cleanLandID <= 0x15);      // Поля типа "furrows" (Ваш случай!)
            bool isStaticField = (customGround != null && (customGround.ItemID >= 0x150 && customGround.ItemID <= 0x15C));

            if (!isCustomPlowed && !isStandardField && !isFurrowsField && !isStaticField)
            {
                from.SendMessage("Сажать овощи можно только на вспаханной пашне фермерского поля!");
                return;
            }
            
            Region reg = Region.Find(loc, map);
            if (reg is FarmRegion)
            {
                FarmRegion farmReg = (FarmRegion)reg;
                if (farmReg.Controller != null && farmReg.Controller.Owner != from && from.AccessLevel == AccessLevel.Player)
                {
                    from.SendMessage("Вы не можете сажать семена на чужом приватном поле!");
                    return;
                }
            }
            IPooledEnumerable eable = map.GetItemsInRange(loc, 0);
            foreach (Item item in eable)
            {
                if (item is FarmVegetablePlant)
                {
                    from.SendMessage("На этой грядке уже что-то растет!");
                    eable.Free();
                    return;
                }
            }
            eable.Free();
            int targetZ = (customGround != null) ? customGround.Z : landTile.Z;
            FarmVegetablePlant plant = new FarmVegetablePlant(from, m_Type);
            plant.MoveToWorld(new Point3D(loc.X, loc.Y, targetZ + 1), map);

            m_Seed.Consume();
            from.PlaySound(0x243);
            from.SendMessage("Вы успешно посадили семена.");
        }
    }

    // =========================================================================
    // УНИВЕРСАЛЬНЫЙ КУСТ С ПОДДЕРЖКОЙ ПОЛИВА И ФАЗ РОСТА
    // =========================================================================
    public class FarmVegetablePlant : Item
    {
        private Mobile m_Planter;
        private int m_Stage;
        private VegetableType m_VegType;
        private Timer m_GrowthTimer;
        private bool m_Watered;
        private int m_DryCount; // Счетчик пропущенных поливов
        private bool m_IsDead; // Статус: погибло ли растение

        private int m_DaysInStage; // Сколько дней растение успешно росло на ТЕКУЩЕЙ стадии


        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Planter
        {
            get { return m_Planter; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsDead
        {
            get { return m_IsDead; }
            set
            {
                m_IsDead = value;
                UpdateName();
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Watered
        {
            get { return m_Watered; }
            set
            {
                if (m_IsDead) return; // Мёртвое растение поливать бесполезно

                // УЛЬТИМАТИВНАЯ БЛОКИРОВКА ПОЛИВА ЗИМОЙ:
                // Проверяем текущий сезон карты. Если зима (ID: 3) — блокируем установку флага полива!
                if (this.Map != null && this.Map != Map.Internal && this.Map.Season == 3)
                {
                    if (m_Planter != null)
                        m_Planter.SendMessage(0x22, "Земля на грядке промёрзла насквозь! Вода мгновенно превращается в лёд, полив невозможен.");
                    return;
                }

                m_Watered = value;
                if (m_Watered) m_DryCount = 0; // Сбрасываем счётчик засухи при успешном поливе
                UpdateName();
                InvalidateProperties();
            }
        }


        [CommandProperty(AccessLevel.GameMaster)]
        public int Stage
        {
            get { return m_Stage; }
            set
            {
                m_Stage = value;
                InvalidateProperties();
            }
        }

        public FarmVegetablePlant(Mobile planter, VegetableType type) : base(0x18E5)
        {
            m_Planter = planter;
            m_VegType = type;
            m_Stage = 0;
            m_Watered = false;
            m_DryCount = 0;
            m_IsDead = false;
            Movable = false;

            UpdateName();

            m_GrowthTimer = Timer.DelayCall(TimeSpan.FromHours(2.0), TimeSpan.FromHours(2.0),
                new TimerCallback(GrowPerDay));
        }


        public void UpdateName()
        {
            if (m_IsDead)
            {
                Name = "Засохшее погибшее растение (Завяло)";
                return;
            }

            string baseName = GetVegNameString();

            if (m_Stage == 0) Name = $"{baseName} Plant (Phase 1)";
            else if (m_Stage == 1) Name = $"{baseName} Plant (Phase 2)";
            else if (m_Stage == 2) Name = $"Ripe {baseName} (Ready to Harvest)";

            if (!m_Watered && m_Stage < 2)
            {
                Name += " [Нужен полив!]";
            }
        }

        private string GetVegNameString()
        {
            switch (m_VegType)
            {
                case VegetableType.Onion: return "Onion";
                case VegetableType.Potato: return "Potato";
                case VegetableType.Cabbage: return "Cabbage";
                case VegetableType.Carrot: return "Carrot";
                case VegetableType.Lettuce: return "Lettuce";
                case VegetableType.Pumpkin: return "Pumpkin";
            }

            return "Vegetable";
        }

        private void GrowPerDay()
        {
            if (Deleted || m_IsDead) return;

            // =========================================================================
            // СИНХРОНИЗАЦИЯ С МОДОМ СЕЗОНОВ (Через Map.Season)
            // =========================================================================
            int currentSeason = 0; // По умолчанию весна (0)
            if (this.Map != null && this.Map != Map.Internal)
            {
                currentSeason = this.Map.Season; // Читаем ID сезона (0-Весна, 1-Лето, 2-Осень, 3-Зима)
            }

            // ЗИМА (ID: 3): Растения засыпают под снегом. Счётчики замирают, грядки не сохнут и не растут
            if (currentSeason == 3)
            {
                if (m_Planter != null && m_Stage < 2)
                    m_Planter.SendMessage(0x5D, $"[Зимняя спячка] Рост вашего урожая ({GetVegNameString()}) полностью заморожен до весенней оттепели.");

                return; // Полный выход из метода, засуха не наступает
            }

            // =========================================================================
            // АВТОМАТИЧЕСКИЙ ПОЛИВ ДОЖДЕМ (РАБОТАЕТ: ВЕСНА, ЛЕТО, ОСЕНЬ)
            // =========================================================================
            if (this.Map != null && this.Map != Map.Internal)
            {
                // Запрашиваем список всех активных погодных зон для текущей карты (Trammel / Felucca)
                List<Server.Misc.Weather> weatherList = Server.Misc.Weather.GetWeatherList(this.Map);

                if (weatherList != null)
                {
                    bool isRainingNow = false;

                    // Сканируем каждую погодную зону на сервере
                    for (int j = 0; j < weatherList.Count; ++j)
                    {
                        Server.Misc.Weather w = weatherList[j];

                        // Проверяем: активна ли зона, идут ли осадки и теплая ли температура (выше нуля = Дождь)
                        // Используем рефлексию для чтения закрытого флага m_Active, если у него нет public свойства
                        bool isActivePercipitation = false;
                        FieldInfo activeField = typeof(Server.Misc.Weather).GetField("m_Active", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (activeField != null)
                        {
                            isActivePercipitation = (bool)activeField.GetValue(w);
                        }

                        if (isActivePercipitation && w.Temperature > 0)
                        {
                            // Проверяем, входят ли координаты грядки в прямоугольные зоны этого шторма/дождя
                            if (w.IntersectsWith(new Rectangle2D(this.X, this.Y, 1, 1)))
                            {
                                isRainingNow = true;
                                break; // Дождь найден, прекращаем поиск
                            }
                        }
                    }

                    // Если над грядкой бушует дождь — земля впитывает влагу
                    if (isRainingNow)
                    {
                        m_Watered = true;
                        m_DryCount = 0; // Сбрасываем счетчик засухи

                        if (m_Planter != null && m_Stage < 2)
                            m_Planter.SendMessage(0x5D, $"[Погода] Проливной дождь напитал вашу грядку ({GetVegNameString()}) благодатной влагой!");
                    }
                }
            }
            // =========================================================================


            // Настройка скорости созревания в зависимости от климата
            int daysRequiredToGrow = 7; // ВЕСНА (База): 7 тиков на фазу (14 часов реального времени)

            if (currentSeason == 1)
            {
                daysRequiredToGrow = 4; // ЛЕТО: Жара ускоряет рост! Нужно всего 4 тика (8 часов реального времени)
            }
            else if (currentSeason == 2)
            {
                daysRequiredToGrow = 12; // ОСЕНЬ: Заморозки замедляют рост. Нужно 12 тиков (24 часа реального времени)
            }

            // =========================================================================
            // 1. ПРОВЕРКА НА ЗАСУХУ (РАБОТАЕТ ВЕСНОЙ, ЛЕТОМ И ОСЕНЬЮ)
            // =========================================================================
            if (!m_Watered)
            {
                m_DryCount++;

                // Растущие фазы погибают за 3 пропущенных дня засухи
                if (m_Stage < 2 && m_DryCount >= 3)
                {
                    WitherAndDie();
                    return;
                }
                // Спелый плод сгниет за 14 дней
                else if (m_Stage == 2 && m_DryCount >= 14)
                {
                    WitherAndDie();
                    return;
                }

                if (m_Planter != null && m_Stage < 2)
                    m_Planter.SendMessage(0x22, $"Ваш урожай ({GetVegNameString()}) засыхает! Вы не полили его за эти сутки!");

                UpdateName();
                return;
            }

            // =========================================================================
            // 2. РАСТЕНИЕ УСПЕШНО ПОЛИТО
            // =========================================================================
            m_Watered = false; // Сбрасываем полив на следующие сутки

            if (m_Stage < 2)
            {
                m_DaysInStage++; // Увеличиваем счетчик успешных дней

                // Проверяем динамический порог созревания (4 дня летом, 7 весной, 12 осенью)
                if (m_DaysInStage >= daysRequiredToGrow)
                {
                    m_Stage++;
                    m_DaysInStage = 0; // Сбрасываем дни для новой стадии

                    if (m_Stage == 1) // Переход на 2 фазу (ботва)
                    {
                        ItemID = 0xCB7;
                        if (m_VegType == VegetableType.Potato) Hue = 2411;
                        if (m_VegType == VegetableType.Pumpkin) ItemID = 0xCB5;
                        if (m_VegType == VegetableType.Cabbage) ItemID = 0xCB6;
                        if (m_VegType == VegetableType.Lettuce) ItemID = 0xC70;

                        if (m_Planter != null)
                            m_Planter.SendMessage(88, $"Ваша грядка ({GetVegNameString()}) впитала соки сезона и перешла во вторую фазу роста!");
                    }
                    else if (m_Stage == 2) // Полное созревание
                    {
                        switch (m_VegType)
                        {
                            case VegetableType.Pumpkin: ItemID = 0xC6A; Hue = 0; break;
                            case VegetableType.Cabbage: ItemID = 0xC7B; Hue = 0; break;
                            case VegetableType.Lettuce: ItemID = 0xC70; Hue = 0; break;
                            case VegetableType.Carrot: ItemID = 0xCB7; Hue = 43; break;
                            case VegetableType.Onion: ItemID = 0x1846; Hue = 0; break;
                            case VegetableType.Potato: ItemID = 0x1846; Hue = 52; break;
                        }

                        if (m_Planter != null)
                            m_Planter.SendMessage(0x35, $"Поздравляем! Ваш урожай ({GetVegNameString()}) полностью созрел под солнцем и готов к сбору!");
                    }
                }
                else
                {
                    if (m_Planter != null)
                    {
                        string seasonName = "весенней оттепелью";
                        if (currentSeason == 1) seasonName = "летним солнцем";
                        if (currentSeason == 2) seasonName = "осенней прохладой";

                        m_Planter.SendMessage($"Прошли еще одни сутки под {seasonName}. Урожай ({GetVegNameString()}) впитал воду. До фазы: {m_DaysInStage}/{daysRequiredToGrow}");
                    }
                }
            }

            UpdateName();
        }

        private void Grow()
        {
            if (Deleted || m_IsDead) return;

            // ЗЕМЛЯ СУХАЯ: Включаем логику увядания
            if (!m_Watered)
            {
                m_DryCount++; // Увеличиваем счетчик засухи

                // На 3-й пропущенный цикл полива растение окончательно засыхает и превращается в мусор
                if (m_DryCount >= 3)
                {
                    WitherAndDie();
                    return;
                }

                if (m_Planter != null)
                    m_Planter.SendMessage(
                        $"Ваш урожай ({GetVegNameString()}) засыхает без воды! Если не полить сейчас, он погибнет!");

                return;
            }

            // ЗЕМЛЯ ПОЛИТА: Растем дальше
            m_Stage++;
            m_Watered = false;

            if (m_Stage == 1)
            {
                switch (m_VegType)
                {
                    case VegetableType.Pumpkin: ItemID = 0xCB5; break;
                    case VegetableType.Cabbage: ItemID = 0xCB6; break;
                    case VegetableType.Lettuce: ItemID = 0xC70; break;
                    default: ItemID = 0xCB7; break;
                }

                if (m_VegType == VegetableType.Potato) Hue = 2411;
                if (m_Planter != null)
                    m_Planter.SendMessage($"Вашие посадки ({GetVegNameString()}) впитали воду и подросли.");
            }
            else if (m_Stage == 2)
            {
                switch (m_VegType)
                {
                    case VegetableType.Pumpkin:
                        ItemID = 0xC6A;
                        Hue = 0;
                        break;
                    case VegetableType.Cabbage:
                        ItemID = 0xC7B;
                        Hue = 0;
                        break;
                    case VegetableType.Lettuce:
                        ItemID = 0xC70;
                        Hue = 0;
                        break;
                    case VegetableType.Carrot:
                        ItemID = 0xCB7;
                        Hue = 43;
                        break;
                    case VegetableType.Onion:
                        ItemID = 0x1846;
                        Hue = 0;
                        break;
                    case VegetableType.Potato:
                        ItemID = 0x1846;
                        Hue = 52;
                        break;
                }

                if (m_Planter != null) m_Planter.SendMessage($"Ваш урожай ({GetVegNameString()}) полностью созрел!");
                m_GrowthTimer.Stop();
            }

            UpdateName();
        }

        // МЕТОД ГИБЕЛИ РАСТЕНИЯ
        private void WitherAndDie()
        {
            m_IsDead = true;
            m_GrowthTimer.Stop(); // Процесс роста останавливается навсегда

            ItemID = 0x0CBC; // Превращаем куст в сухую коричневую корягу
            Hue = 0; // Сбрасываем цвета

            UpdateName();

            if (m_Planter != null)
                m_Planter.SendMessage("Один из ваших кустов на ферме полностью засох и погиб из-за нехватки воды.");
        }

        public override void OnDoubleClick(Mobile from)
        {
            // ПРОВЕРКА: Если растение мертвое, даем игроку очистить грядку и получить хворост
            if (m_IsDead)
            {
                // Проверка прав на регион фермы
                Region region = Region.Find(Location, Map);
                if (region is FarmRegion && ((FarmRegion)region).Controller != null &&
                    ((FarmRegion)region).Controller.Owner != from && from.AccessLevel == AccessLevel.Player)
                {
                    from.SendMessage("Вы не можете трогать чужое засохшее растение.");
                    return;
                }

                Kindling dryTrash = new Kindling(); // Хворост для костра (идеальный мусор)
                if (from.Backpack.TryDropItem(from, dryTrash, false))
                {
                    from.SendMessage("Вы выдернули засохший куст и выбросили сухие ветки в инвентарь.");
                    from.PlaySound(0x13E);
                    this.Delete(); // Очищаем грядку
                }
                else
                {
                    dryTrash.Delete();
                    from.SendMessage("Ваш инвентарь полон, освободите место, чтобы убрать сухой куст.");
                }

                return;
            }

            // Дальше идет стандартный оригинальный код сбора живых овощей...
            if (m_Stage < 2)
            {
                from.SendMessage("Это растение еще не созрело. Ему нужно время.");
                return;
            }

            Region reg = Region.Find(Location, Map);
            if (reg is FarmRegion)
            {
                FarmRegion farmReg = (FarmRegion)reg;
                if (farmReg.Controller != null && farmReg.Controller.Owner != from &&
                    from.AccessLevel == AccessLevel.Player)
                {
                    from.SendMessage("Вы не можете собирать чужой урожай!");
                    return;
                }
            }

            // ИЗМЕНЕНО: РАНДОМНЫЙ СБОР ПЛОДОВ ОТ 1 ДО 3 ШТУК
            int yieldCount = Utility.RandomMinMax(1, 3);
            bool totalSuccess = true;

            for (int i = 0; i < yieldCount; i++)
            {
                Item vegetable;
                if (m_VegType == VegetableType.Onion) vegetable = new Onion();
                else if (m_VegType == VegetableType.Cabbage) vegetable = new Cabbage();
                else if (m_VegType == VegetableType.Carrot) vegetable = new Carrot();
                else if (m_VegType == VegetableType.Lettuce) vegetable = new Lettuce();
                else if (m_VegType == VegetableType.Pumpkin) vegetable = new Pumpkin();
                else vegetable = new FarmPotato();

                if (!from.Backpack.TryDropItem(from, vegetable, false))
                {
                    vegetable.Delete();
                    totalSuccess = false;
                    break;
                }
            }

            if (totalSuccess)
            {
                from.SendMessage($"Вы успешно собрали богатый урожай в количестве {yieldCount} шт.!");
                from.PlaySound(0x13E);
                this.Delete();
            }
            else
            {
                from.SendMessage("Часть урожая не поместилась, так как ваш инвентарь переполнен! Освободите место.");
            }
        }

        public override void OnDelete()
        {
            if (m_GrowthTimer != null) m_GrowthTimer.Stop();
            base.OnDelete();
        }

        public FarmVegetablePlant(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)4); // Поднимаем версию сохранения до 4
            writer.Write(m_Planter);
            writer.Write(m_Stage);
            writer.Write((int)m_VegType);
            writer.Write(m_Watered);
            writer.Write(m_DryCount);
            writer.Write(m_IsDead);
            writer.Write(m_DaysInStage); // Сохраняем прожитые дни на текущей стадии
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_Planter = reader.ReadMobile();
            m_Stage = reader.ReadInt();
            m_VegType = (VegetableType)reader.ReadInt();
            m_Watered = reader.ReadBool();
            m_DryCount = reader.ReadInt();
            m_IsDead = reader.ReadBool();
            if (version >= 4)
                m_DaysInStage = reader.ReadInt();
            else
                m_DaysInStage = 0;
            // Восстановление таймеров с шагом в 2 реальных часа (1 игровые сутки)
            if (!m_IsDead)
                m_GrowthTimer = Timer.DelayCall(TimeSpan.FromHours(2.0), TimeSpan.FromHours(2.0),
                    new TimerCallback(GrowPerDay));
            UpdateName();
        }
    }
}
