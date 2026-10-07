using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Server;
using Server.Mobiles;
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
    // ТАРГЕТ ПОСАДКИ ДЛЯ СЕМЯН (С ПОДДЕРЖКОЙ ДОМАШНИХ ТЕПЛИЦ)
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

            bool isStandardField = (cleanLandID >= 0x150 && cleanLandID <= 0x15C);
            bool isFurrowsField = (cleanLandID >= 0x9 && cleanLandID <= 0x15);
            bool isStaticField = (customGround != null && (customGround.ItemID >= 0x150 && customGround.ItemID <= 0x15C));

            // Проверяем, находится ли точка посадки внутри дома игрока
            bool isInsideHouse = (Server.Multis.BaseHouse.FindHouseAt(loc, map, 16) != null);

            // Если игрок НЕ в доме, то проверяем стандартную вспаханную землю на улице
            if (!isInsideHouse && !isCustomPlowed && !isStandardField && !isFurrowsField && !isStaticField)
            {
                from.SendMessage("Сажать овощи можно только на фермерском поле или внутри своего дома!");
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
    // УНИВЕРСАЛЬНЫЙ КУСТ С ПОДДЕРЖКОЙ ТУЛТИПОВ И УМНОГО КЛИМАТА В ДОМЕ
    // =========================================================================
    public class FarmVegetablePlant : Item
    {
        public static readonly double BaseWeedsChance = 0.30;

        private Mobile m_Planter;
        private int m_Stage;
        private VegetableType m_VegType;
        private Timer m_GrowthTimer;
        private int m_DryCount;
        private bool m_IsDead;
        private int m_DaysInStage;

        private int m_WaterLevel;
        private int m_FertilityLevel;
        private int m_WeedsLevel;
        private int m_PestsCount;
        private int m_GoodCareTicks;
        private bool m_IsWeedSource;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Planter => m_Planter;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsDead
        {
            get => m_IsDead;
            set { m_IsDead = value; UpdateName(); InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Stage
        {
            get => m_Stage;
            set { m_Stage = value; UpdateName(); InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int WaterLevel
        {
            get => m_WaterLevel;
            set { m_WaterLevel = Math.Max(0, Math.Min(100, value)); InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int FertilityLevel
        {
            get => m_FertilityLevel;
            set { m_FertilityLevel = Math.Max(0, Math.Min(100, value)); InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int WeedsLevel
        {
            get => m_WeedsLevel;
            set { m_WeedsLevel = Math.Max(0, Math.Min(100, value)); InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PestsCount
        {
            get => m_PestsCount;
            set { m_PestsCount = Math.Max(0, Math.Min(3, value)); InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsWeedSource
        {
            get => m_IsWeedSource;
            set { m_IsWeedSource = value; UpdateName(); InvalidateProperties(); }
        }

        public FarmVegetablePlant(Mobile planter, VegetableType type) : base(0x18E5)
        {
            m_Planter = planter;
            m_VegType = type;
            m_Stage = 0;
            m_WaterLevel = 50;
            m_DryCount = 0;
            m_IsDead = false;
            m_FertilityLevel = 0;
            m_WeedsLevel = 0;
            m_PestsCount = 0;
            m_GoodCareTicks = 0;
            m_IsWeedSource = false;
            Movable = false;

            UpdateName();

            m_GrowthTimer = Timer.DelayCall(TimeSpan.FromHours(2.0), TimeSpan.FromHours(2.0), new TimerCallback(GrowPerDay));
        }

        // Чистое красивое имя, вся техническая часть перенесена в тултипы
        public void UpdateName()
        {
            if (m_IsWeedSource)
            {
                Name = "Разросшийся куст сорняка [Очаг заражения]";
                return;
            }

            if (m_IsDead)
            {
                Name = "Засохшее погибшее растение (Завяло)";
                return;
            }

            string baseName = GetVegNameString();
            string stageStr = m_Stage == 2 ? "Ripe" : $"Phase {m_Stage + 1}";
            Name = $"{stageStr} {baseName}";
        }

        // =========================================================================
        // СИСТЕМА ТУЛТИПОВ ПРИ НАВЕДЕНИИ МЫШКИ 🔍
        // =========================================================================
        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list); // Выводит Name первой строчкой

            if (m_IsDead || m_IsWeedSource) return;

            list.Add($"Влажность почвы: {m_WaterLevel}%");
            list.Add($"Питательность почвы: {m_FertilityLevel}%");

            if (m_WeedsLevel > 0)
                list.Add($"* Сорняки: {m_WeedsLevel}% *");

            if (m_PestsCount > 0)
                list.Add($"* Вредители: {m_PestsCount}/3 *");

            if (m_WaterLevel < 25 && m_Stage < 2)
                list.Add("Нужен срочный полив!");
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
                default: return "Vegetable";
            }
        }

        private void GrowPerDay()
        {
            if (Deleted) return;

            if (m_IsWeedSource)
            {
                SpreadWeedsToNeighbors();
                return;
            }

            if (m_IsDead) return;

            int currentSeason = 0;
            if (this.Map != null && this.Map != Map.Internal)
            {
                currentSeason = this.Map.Season;
            }

            // УМНАЯ ПРОВЕРКА НА ТЕПЛИЦУ (Дом игрока) 🏡🌿
            Server.Multis.BaseHouse house = Server.Multis.BaseHouse.FindHouseAt(this.Location, this.Map, 16);
            bool isInGreenhouse = (house != null);

            // Если растение ЗИМОЙ и НА УЛИЦЕ (вне дома) — рост замораживается
            if (currentSeason == 3 && !isInGreenhouse)
            {
                if (m_Planter != null && m_Stage < 2)
                    m_Planter.SendMessage(0x5D, $"[Зимняя спячка] Рост вашего урожая ({GetVegNameString()}) заморожен. Перенесите грядку в дом или теплицу!");
                return;
            }

            // АВТОМАТИЧЕСКИЙ ПОЛИВ ДОЖДЕМ (Только на улице, под крышей дома дождя нет!)
            if (!isInGreenhouse && this.Map != null && this.Map != Map.Internal)
            {
                List<Server.Misc.Weather> weatherList = Server.Misc.Weather.GetWeatherList(this.Map);
                if (weatherList != null)
                {
                    bool isRainingNow = false;
                    for (int j = 0; j < weatherList.Count; ++j)
                    {
                        Server.Misc.Weather w = weatherList[j];
                        bool isActivePercipitation = false;
                        FieldInfo activeField = typeof(Server.Misc.Weather).GetField("m_Active", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (activeField != null) isActivePercipitation = (bool)activeField.GetValue(w);
                        if (isActivePercipitation && w.Temperature > 0 && w.IntersectsWith(new Rectangle2D(this.X, this.Y, 1, 1)))
                        {
                            isRainingNow = true;
                            break;
                        }
                    }
                    if (isRainingNow && m_WaterLevel < 20)
                    {
                        WaterLevel += 1;
                        if (m_Planter != null && m_Stage < 2)
                            m_Planter.SendMessage(0x5D, $"[Погода] Мелкий дождь увлажнил вашу грядку ({GetVegNameString()}) на 1%. Текущая влага: {m_WaterLevel}%.");
                    }
                }
            }
            // Настройка скорости роста (В доме всегда тепло, как весной!)
            int daysRequiredToGrow = (currentSeason == 0 || currentSeason == 2 || isInGreenhouse) ? 4 : 8;
            if (m_WaterLevel >= 60 && daysRequiredToGrow > 2) daysRequiredToGrow -= 1;
            // В доме влага испаряется МЕДЛЕННЕЕ (минус 10% вместо 25%), так как нет ветра и солнца
            int waterLoss = isInGreenhouse ? 10 : 25;
            m_WaterLevel = Math.Max(0, m_WaterLevel - waterLoss);
            m_FertilityLevel = Math.Max(0, m_FertilityLevel - 10);
            // Сорняки и вредители в домашней теплице (чуть активнее из-за застойного теплого воздуха)
            if (m_Stage < 2)
            {
                double weedChance = isInGreenhouse ? (BaseWeedsChance + 0.10) : BaseWeedsChance;
                if (Utility.RandomDouble() < weedChance)
                    m_WeedsLevel = Math.Min(100, m_WeedsLevel + 15);
                double pestsChance = isInGreenhouse ? 0.35 : 0.25;
                if (Utility.RandomDouble() < pestsChance)
                    m_PestsCount = Math.Min(3, m_PestsCount + 1);
            }
            if (m_WeedsLevel >= 100)
            {
                TurnIntoWeedBush();
                return;
            }
            if (m_WaterLevel <= 0 || m_PestsCount >= 3)
            {
                m_DryCount++;
                if (m_Stage < 2 && m_DryCount >= 3)
                {
                    WitherAndDie();
                    return;
                }
                if (m_Planter != null && m_Stage < 2)
                    m_Planter.SendMessage(0x22, $"Ваш урожай ({GetVegNameString()}) страдает от засухи или вредителей!");
                InvalidateProperties();
                return;
            }
            m_DryCount = 0;
            if (m_WaterLevel >= 50 && m_FertilityLevel >= 40 && m_WeedsLevel == 0 && m_PestsCount == 0)
            {
                m_GoodCareTicks++;
            }
            if (m_Stage < 2)
            {
                m_DaysInStage++;
                if (m_DaysInStage >= daysRequiredToGrow)
                {
                    m_Stage++;
                    m_DaysInStage = 0;
                    if (m_Stage == 1)
                    {
                        ItemID = 0xCB7;
                        if (m_VegType == VegetableType.Potato) Hue = 2411;
                        if (m_VegType == VegetableType.Pumpkin) ItemID = 0xCB5;
                        if (m_VegType == VegetableType.Cabbage) ItemID = 0xCB6;
                        if (m_VegType == VegetableType.Lettuce) ItemID = 0xC70;
                        if (m_Planter != null) m_Planter.SendMessage(88, $"Ваша грядка ({GetVegNameString()}) успешно перешла во вторую фазу роста!");
                    }
                    else if (m_Stage == 2)
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
                        if (m_Planter != null) m_Planter.SendMessage(0x35, $"Ваш урожай ({GetVegNameString()}) полностью созрел и готов к сбору!");
                    }
                    UpdateName();
                }
            }
            InvalidateProperties();
        }
        private void TurnIntoWeedBush()
        {
            m_IsWeedSource = true;
            m_IsDead = true;
            m_Stage = 0;
            ItemID = 0x0CB5;
            Hue = 1167;
            UpdateName();
            InvalidateProperties();
            if (m_Planter != null)
                m_Planter.SendMessage(0x22, "ВНИМАНИЕ! Из-за плохой прополки одна из ваших грядок ПОЛНОСТЬЮ ОБРОСЛА СОРНЯКОМ и теперь заражает соседние кусты!");
        }
        private void SpreadWeedsToNeighbors()
        {
            if (this.Map != null && this.Map != Map.Internal) return;
            IPooledEnumerable eable = this.Map.GetItemsInRange(this.Location, 1);
            foreach (Item item in eable)
            {
                if (item is FarmVegetablePlant neighborPlant && neighborPlant != this && !neighborPlant.IsDead && !neighborPlant.IsWeedSource)
                {
                    neighborPlant.WeedsLevel = Math.Min(100, neighborPlant.WeedsLevel + 25);
                    neighborPlant.InvalidateProperties();
                    Effects.SendLocationParticles(EffectItem.Create(neighborPlant.Location, neighborPlant.Map, TimeSpan.FromSeconds(0.5)), 0x376A, 9, 10, 5042);
                    if (neighborPlant.Planter != null)
                        neighborPlant.Planter.SendMessage(0x22, $"[!] Сорняки с соседнего заросшего куста перекинулись на вашу посадку ({neighborPlant.GetVegNameString()})! Нужна срочная прополка!");
                }
            }
            eable.Free();
        }
        private void WitherAndDie()
        {
            m_IsDead = true;
            if (m_GrowthTimer != null) m_GrowthTimer.Stop();
            ItemID = 0x0CBC;
            Hue = 0;
            UpdateName();
            InvalidateProperties();
            if (m_Planter != null)
                m_Planter.SendMessage("Один из ваших кустов на ферме полностью погиб из-за засухи или вредителей.");
        }
        public override void OnDoubleClick(Mobile from)
        {
            if (m_IsWeedSource)
            {
                Region region = Region.Find(Location, Map);
                if (region is FarmRegion && ((FarmRegion)region).Controller != null && ((FarmRegion)region).Controller.Owner != from && from.AccessLevel == AccessLevel.Player)
                {
                    from.SendMessage("Вы не можете трогать чужое засохшее растение.");
                    return;
                }
                from.SendMessage("Вы полностью выкорчевали огромный куст сорняка, очистив грядку.");
                from.PlaySound(0x13E);
                this.Delete();
                return;
            }
            if (m_IsDead)
            {
                Region region = Region.Find(Location, Map);
                if (region is FarmRegion && ((FarmRegion)region).Controller != null && ((FarmRegion)region).Controller.Owner != from && from.AccessLevel == AccessLevel.Player)
                {
                    from.SendMessage("Вы не можете трогать чужое засохшее растение.");
                    return;
                }
                Kindling dryTrash = new Kindling();
                if (from.Backpack.TryDropItem(from, dryTrash, false))
                {
                    from.SendMessage("Вы очистили грядку от погибшего сорняка.");
                    from.PlaySound(0x13E);
                    this.Delete();
                }
                else
                {
                    dryTrash.Delete();
                    from.SendMessage("Ваш инвентарь полон.");
                }
                return;
            }
            if (m_Stage < 2)
            {
                from.SendMessage(0x5D, $"--- Состояние грядки ({GetVegNameString()}) ---");
                from.SendMessage($"Влажность почвы: {m_WaterLevel}% / 100%");
                from.SendMessage($"Питательность почвы: {m_FertilityLevel}% / 100%");
                if (m_WeedsLevel > 0)
                    from.SendMessage(0x22, $"[!] Заражение сорняками: {m_WeedsLevel}% / 100% (Нужна тяпка)");
                else
                    from.SendMessage(0x42, "Сорняков на грядке не обнаружено.");
                if (m_PestsCount > 0)
                    from.SendMessage(0x22, $"[!] Атака вредителей: {m_PestsCount} из 3 шт. (Нужна дезинсекция!)");
                else
                    from.SendMessage(0x42, "Паразитов на листьях нет.");
                return;
            }
            Region reg = Region.Find(Location, Map);
            if (reg is FarmRegion)
            {
                FarmRegion farmReg = (FarmRegion)reg;
                if (farmReg.Controller != null && farmReg.Controller.Owner != from && from.AccessLevel == AccessLevel.Player)
                {
                    from.SendMessage("Вы не можете собирать чужой урожай!");
                    return;
                }
            }
            int baseYield = Utility.RandomMinMax(2, 3);
            int bonusItems = 0;
            if (m_GoodCareTicks >= 2) bonusItems += 1;
            if (m_GoodCareTicks >= 4) bonusItems += 1;
            if (m_GoodCareTicks >= 6) bonusItems += 1;
            if (m_FertilityLevel >= 30) bonusItems += 1;
            if (m_FertilityLevel >= 70) bonusItems += 1;
            int finalYield = baseYield + bonusItems;
            if (finalYield > 8) finalYield = 8;
            bool totalSuccess = true;
            for (int i = 0; i < finalYield; i++)
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
                if (finalYield >= 7)
                    from.SendMessage(0x35, $"Потрясающе! Благодаря вашему мастерскому уходу и богатой почве, вы собрали ЛЕГЕНДАРНЫЙ урожай: {finalYield} шт.!");
                else if (bonusItems > 0)
                    from.SendMessage(0x5D, $"Хорошая работа! Заботливый уход принес вам повышенный урожай: {finalYield} шт.");
                else
                    from.SendMessage($"Вы успешно собрали плоды в количестве {finalYield} шт.");
                from.PlaySound(0x13E);
                this.Delete();
            }
            else
            {
                from.SendMessage("Ваш инвентарь переполнен! Освободите место для овощей.");
            }
        }
        public void PourWater(Mobile from, int percent)
        {
            if (m_IsDead || m_IsWeedSource) return;
            if (m_WaterLevel >= 100) { from.SendMessage("Почва уже максимально напитана влагой."); return; }
            WaterLevel += percent;
            from.PlaySound(0x4E);
            from.SendMessage(0x42, $"Вы полили грядку. Уровень влаги: {m_WaterLevel}%.");
            InvalidateProperties();
        }
        public void ApplyFertilizer(Mobile from, int percent)
        {
            if (m_IsDead || m_IsWeedSource) return;
            if (m_FertilityLevel >= 100) { from.SendMessage("Почва уже максимально насыщена."); return; }
            FertilityLevel += percent;
            from.PlaySound(0x244);
            from.SendMessage(0x42, $"Вы внесли удобрения. Уровень питания: {m_FertilityLevel}%.");
            InvalidateProperties();
        }
        public void RemoveWeeds(Mobile from, int percent)
        {
            if (m_IsDead || m_IsWeedSource) return;
            if (m_WeedsLevel <= 0) { from.SendMessage("На этой грядке нет сорняков."); return; }
            WeedsLevel = Math.Max(0, m_WeedsLevel - percent);
            from.PlaySound(0x13E);
            from.SendMessage(0x42, $"Вы прошлись тяпкой. Уровень сорняков снижен до {m_WeedsLevel}%.");
            InvalidateProperties();
        }
        public void KillPests(Mobile from)
        {
            if (m_IsDead || m_IsWeedSource || m_PestsCount <= 0) return;
            m_PestsCount = 0;
            UpdateName();
            from.PlaySound(0x3B4);
            from.SendMessage(0x42, "Вы уничтожили всех паразитов.");
            InvalidateProperties();
        }
        public override void OnDelete()
        {
            if (m_GrowthTimer != null) m_GrowthTimer.Stop();
            base.OnDelete();
        }
        public FarmVegetablePlant(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)6);
            writer.Write(m_Planter);
            writer.Write(m_Stage);
            writer.Write((int)m_VegType);
            writer.Write(m_DryCount);
            writer.Write(m_IsDead);
            writer.Write(m_DaysInStage);
            writer.Write(m_WaterLevel);
            writer.Write(m_FertilityLevel);
            writer.Write(m_WeedsLevel);
            writer.Write(m_PestsCount);
            writer.Write(m_GoodCareTicks);
            writer.Write(m_IsWeedSource);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_Planter = reader.ReadMobile();
            m_Stage = reader.ReadInt();
            m_VegType = (VegetableType)reader.ReadInt();
            m_DryCount = reader.ReadInt();
            m_IsDead = reader.ReadBool();
            m_DaysInStage = reader.ReadInt();
            if (version >= 5)
            {
                m_WaterLevel = reader.ReadInt();
                m_FertilityLevel = reader.ReadInt();
                m_WeedsLevel = reader.ReadInt();
                m_PestsCount = reader.ReadInt();
                m_GoodCareTicks = reader.ReadInt();
            }
            if (version >= 6)
            {
                m_IsWeedSource = reader.ReadBool();
            }
            if (!m_IsDead || m_IsWeedSource)
                m_GrowthTimer = Timer.DelayCall(TimeSpan.FromHours(2.0), TimeSpan.FromHours(2.0), new TimerCallback(GrowPerDay));
            UpdateName();
            InvalidateProperties();
        }
    }
}