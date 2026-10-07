using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Server.Commands;
using Server.Custom.DiabloLoot;
using Server.Engines.CityLoyalty;
using Server.Engines.Plants;
using Server.Factions;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.CityDevelopment
{
    public class LegendaryMerchant : PlayerVendor
    {
        public int ProfessionIndex { get; private set; }

        [Constructable]
        public LegendaryMerchant(string title, int professionIndex) : base(null, null)
        {
            ProfessionIndex = professionIndex;
            Name = NameList.RandomName("male");
            Title = $"[{title}]";
            CantWalk = true;
            Blessed = true;
            HoldGold = 1000000;

            DressUp();
            GenerateLegendaryStock();

            // Таймер самоликвидации (проверка склада каждые 10 секунд)
            Timer.DelayCall(TimeSpan.FromSeconds(10.0), TimeSpan.FromSeconds(10.0), CheckStockAndCleanup);

            // ДОБАВЛЕНО: Купец стоит в городе максимум 4 часа. Если не раскупили — он уходит, уступая место новому каравану!
            Timer.DelayCall(TimeSpan.FromHours(4.0), LeaveTownDueToNoSales);
        }

        public LegendaryMerchant(Serial serial) : base(serial)
        {
        }

        // Метод ухода по истечении времени
        private void LeaveTownDueToNoSales()
        {
            if (Deleted) return;

            PublicOverheadMessage(MessageType.Regular, 0x35, false,
                "Торговля в этом городе идет вяло... Я отправляюсь в другой регион. До встречи, честные господа!");

            // Чисто удаляем предметы из его сумки, чтобы они не забивали память сервера
            if (Backpack != null)
            {
                var toDelete = new List<Item>();
                foreach (var item in Backpack.Items) toDelete.Add(item);
                foreach (var item in toDelete) item.Delete();
            }

            Timer.DelayCall(TimeSpan.FromSeconds(2.0), () => Delete());
        }

        private void DressUp()
        {
            var randomHue = Utility.RandomList(1150, 1153, 1284, 2123);
            AddItem(new FancyShirt(randomHue));
            AddItem(new LongPants(1157));
            AddItem(new ThighBoots(1175));
        }

        private int CountProps(object attributesObject)
        {
            if (attributesObject == null) return 0;
            var count = 0;
            try
            {
                var props = attributesObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in props)
                    if (prop.PropertyType == typeof(int))
                    {
                        var value = (int)prop.GetValue(attributesObject, null);
                        if (value > 0) count++;
                    }
            }
            catch
            {
            }

            return count;
        }

        private void GenerateLegendaryStock()
        {
            if (Backpack == null || Deleted) return;

            var field = typeof(PlayerVendor).GetField("m_SellItems", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) return;

            var sellItems = field.GetValue(this) as Hashtable;
            if (sellItems == null) return;

            for (var i = 0; i < 15; i++)
            {
                Item spawnedItem = null;

                switch (ProfessionIndex)
                {
                    case 0:
                    case 5: // Оружейник / Лучник
                        spawnedItem = Loot.RandomWeapon();
                        break;
                    case 4: // Бронник
                        spawnedItem = Loot.RandomArmor();
                        if (spawnedItem == null) spawnedItem = Loot.RandomShield();
                        break;
                    case 1: // Маг / Писарь
                        spawnedItem = Loot.RandomScroll(50, 63, SpellbookType.Regular);
                        break;
                    case 2:
                    case 3: // Алхимик / Трактирщик
                        spawnedItem = Loot.RandomPotion();
                        if (spawnedItem != null) spawnedItem.Hue = 1161;
                        break;
                    case 7: // Семена Натуралиста
                        try
                        {
                            var rareSeed = new Seed();
                            rareSeed.PlantType = (PlantType)Utility.RandomMinMax(0, 16);
                            rareSeed.PlantHue = (PlantHue)Utility.RandomMinMax(0, 16);
                            spawnedItem = rareSeed;
                        }
                        catch
                        {
                            spawnedItem = new Candle();
                        }

                        break;
                    default:
                        spawnedItem = Loot.RandomArmorOrShieldOrWeaponOrJewelry(true, true, true);
                        break;
                }

                if (spawnedItem == null) spawnedItem = new Candle();

                if (spawnedItem != null)
                {
                    var propertyCount = 0;

                    if (spawnedItem is BaseWeapon weapon)
                    {
                        BaseRunicTool.ApplyAttributesTo(weapon, 5, 95, 100);
                        propertyCount = CountProps(weapon.Attributes) + CountProps(weapon.WeaponAttributes);
                    }
                    else if (spawnedItem is BaseArmor armor)
                    {
                        BaseRunicTool.ApplyAttributesTo(armor, 5, 95, 100);
                        propertyCount = CountProps(armor.Attributes) + CountProps(armor.ArmorAttributes);
                    }
                    else if (spawnedItem is BaseJewel jewel)
                    {
                        BaseRunicTool.ApplyAttributesTo(jewel, 5, 95, 100);
                        propertyCount = CountProps(jewel.Attributes);
                    }

                    var powerProp = spawnedItem.GetType().GetProperty("ItemPower");
                    if (powerProp != null && powerProp.CanWrite)
                        try
                        {
                            powerProp.SetValue(spawnedItem, Enum.ToObject(powerProp.PropertyType, 5), null);
                        }
                        catch
                        {
                        }

                    var originalName = spawnedItem.Name;
                    if (string.IsNullOrEmpty(originalName))
                    {
                        if (!string.IsNullOrEmpty(spawnedItem.ItemData.Name))
                            originalName = spawnedItem.ItemData.Name;
                        else
                            originalName = spawnedItem.GetType().Name;
                    }

                    spawnedItem.Name = DiabloLootColors.FormatName(spawnedItem, originalName);

                    spawnedItem.Weight = 0;

                    // Укладываем в сумку
                    Backpack.DropItem(spawnedItem);

                    // Рассчитываем динамическую цену
                    var finalPrice = 150000;

                    if (spawnedItem is Seed)
                    {
                        finalPrice = Utility.RandomMinMax(15000, 35000);
                    }
                    else
                    {
                        var goldValue = (int)(spawnedItem.Weight * 200) + propertyCount * 35000;
                        goldValue = (int)(goldValue * Utility.RandomMinMax(0.85, 1.15));

                        if (goldValue < 45000) goldValue = 45000;
                        if (goldValue > 380000) goldValue = 380000;

                        finalPrice = goldValue;
                    }

                    // УЛЬТИМАТИВНЫЙ ФИКС ЦЕНЫ: Используем штатный метод PlayerVendor.System,
                    // либо принудительно подменяем свойства внутри Hashtable, переписывая структуру
                    var vi = sellItems[spawnedItem] as VendorItem;
                    if (vi == null)
                    {
                        vi = new VendorItem(spawnedItem, finalPrice, "[Легендарный Артефакт 5 Уровня Мощности]",
                            DateTime.UtcNow);
                        sellItems[spawnedItem] = vi;
                    }
                    else
                    {
                        // Since VendorItem.Price is read‑only, recreate the entry with the new price
                        var updatedVi = new VendorItem(spawnedItem, finalPrice,
                            "[Легендарный Артефакт 5 Уровня Мощности]", DateTime.UtcNow);
                        sellItems[spawnedItem] = updatedVi;
                    }

                    // Насильно заставляем ядро комиссионки обновить прайс-лист в пакетах для клиента
                    InvalidateProperties();
                }
            }
        }

        private void CheckStockAndCleanup()
        {
            if (Deleted || Backpack == null) return;

            var field = typeof(PlayerVendor).GetField("m_SellItems", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) return;

            var sellItems = field.GetValue(this) as Hashtable;
            if (sellItems != null && sellItems.Count == 0)
            {
                PublicOverheadMessage(MessageType.Regular, 0x35, false,
                    "Весь мой эксклюзивный товар распродан! Мне пора отправляться в путь. До встречи!");
                Timer.DelayCall(TimeSpan.FromSeconds(2.0), () => Delete());
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);
            writer.Write(ProfessionIndex);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
            ProfessionIndex = reader.ReadInt();
        }
    }

    public static class CityLegendaryMerchantMod
    {
        // Вспомогательный метод проверки: стоит ли сейчас купец в данном городе
        public static bool IsMerchantExists(Town town)
        {
            if (town == null) return false;
            foreach (var m in World.Mobiles.Values)
                if (m != null && !m.Deleted && m is LegendaryMerchant)
                {
                    var mobRegion = Region.Find(m.Location, m.Map);
                    if (mobRegion != null && mobRegion.Name != null &&
                        mobRegion.Name.IndexOf(town.ToString(), StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }

            return false;
        }

        public static void CheckMerchantSpawn(Town town, int currentLevel)
        {
            if (town == null) return;

            // Если уровень меньше 5 — жестко стираем купца и выходим
            if (currentLevel < 5)
            {
                RemoveMerchant(town);
                return;
            }

            // Если купец уже стоит на площади — повторно его не спавним, чтобы не двоился
            if (IsMerchantExists(town)) return;
            // Бросок костей на приезд каравана (80% шанс успеха по вашей настройке)
            if (Utility.RandomDouble() > 0.80) return;
            try
            {
                var map = Map.Trammel;
                var spawnLoc = Point3D.Zero;
                if (Enum.TryParse(town.ToString(), true, out City coreCity))
                {
                    var sys = CityLoyaltySystem.GetCityInstance(coreCity);
                    if (sys != null && sys.Definition != null)
                    {
                        spawnLoc = sys.Definition.StoneLocation;
                        if (sys.Stone != null && sys.Stone.Map != null) map = sys.Stone.Map;
                    }
                }

                if (spawnLoc == Point3D.Zero) return;
                spawnLoc.X += 2;
                spawnLoc.Z = map.GetAverageZ(spawnLoc.X, spawnLoc.Y);
                var merchantTitle = "Странствующий Купец";
                var professionType = Utility.Random(8);
                switch (professionType)
                {
                    case 0: merchantTitle = "Легендарный Оружейник"; break;
                    case 1: merchantTitle = "Древний Магистр Магии"; break;
                    case 2: merchantTitle = "Магистр Алхимии"; break;
                    case 3: merchantTitle = "Королевский Интендант"; break;
                    case 4: merchantTitle = "Придворный Модельер"; break;
                    case 5: merchantTitle = "Мастер-Лучник Охоты"; break;
                    case 6: merchantTitle = "Главный Инженер Гильдии"; break;
                    case 7: merchantTitle = "Великий Натуралист Земель"; break;
                }

                var merchant = new LegendaryMerchant(merchantTitle, professionType);
                merchant.MoveToWorld(spawnLoc, map);
                merchant.PublicOverheadMessage(MessageType.Regular, 0x35, false,
                    $"Приветствую жителей! Я, [{merchantTitle}], прибыл в ваш великий megapolis.");
                CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x35,
                    $"[Торговля] В развитый мегаполис {town} прибыл [{merchantTitle}] с эксклюзивными артефактами!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CityDevelopment] Критическая ошибка спавна купца: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public static void RemoveMerchant(Town town)
        {
            if (town == null) return;
            var toDelete = new List<Mobile>();
            foreach (var m in World.Mobiles.Values)
                if (m != null && !m.Deleted && m is LegendaryMerchant)
                {
                    var mobRegion = Region.Find(m.Location, m.Map);
                    if (mobRegion != null && mobRegion.Name != null &&
                        mobRegion.Name.IndexOf(town.ToString(), StringComparison.OrdinalIgnoreCase) >= 0)
                        toDelete.Add(m);
                    else if (town.ToString().Contains("Moonglow") && m.X >= 4400 && m.X <= 4560) toDelete.Add(m);
                }

            foreach (var m in toDelete) m.Delete();
        }
    }
}
