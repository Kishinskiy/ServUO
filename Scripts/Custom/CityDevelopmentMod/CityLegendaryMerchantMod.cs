using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Factions;
using Server.Engines.CityLoyalty;
using Server.Engines.Plants;
using Server.Custom.DiabloLoot;

namespace Server.Custom.CityDevelopment
{
    public class LegendaryMerchant : PlayerVendor
    {
        private int _ProfessionIndex;
        public int ProfessionIndex { get { return _ProfessionIndex; } }

        [Constructable]
        public LegendaryMerchant(string title, int professionIndex) : base(null, null)
        {
            _ProfessionIndex = professionIndex;
            Name = NameList.RandomName("male");
            Title = $"[{title}]";
            CantWalk = true;
            Blessed = true;
            this.HoldGold = 1000000;

            DressUp();
            GenerateLegendaryStock();

            // Таймер самоликвидации (проверка склада каждые 10 секунд)
            Timer.DelayCall(TimeSpan.FromSeconds(10.0), TimeSpan.FromSeconds(10.0), new TimerCallback(CheckStockAndCleanup));
        }

        private void DressUp()
        {
            int randomHue = Utility.RandomList(1150, 1153, 1284, 2123);
            this.AddItem(new FancyShirt(randomHue));
            this.AddItem(new LongPants(1157));
            this.AddItem(new ThighBoots(1175));
        }

        // Вспомогательный метод подсчета магических свойств для динамической цены
        private int CountProps(object attributesObject)
        {
            if (attributesObject == null) return 0;
            int count = 0;
            try
            {
                PropertyInfo[] props = attributesObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (PropertyInfo prop in props)
                {
                    if (prop.PropertyType == typeof(int))
                    {
                        int value = (int)prop.GetValue(attributesObject, null);
                        if (value > 0) count++;
                    }
                }
            }
            catch { }
            return count;
        }

private void GenerateLegendaryStock()
        {
            if (this.Backpack == null || this.Deleted) return;

            FieldInfo field = typeof(PlayerVendor).GetField("m_SellItems", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) return;

            Hashtable sellItems = field.GetValue(this) as Hashtable;
            if (sellItems == null) return;

            for (int i = 0; i < 5; i++)
            {
                Item spawnedItem = null;

                switch (_ProfessionIndex)
                {
                    case 0: case 5: // Оружейник / Лучник
                        spawnedItem = Loot.RandomWeapon();
                        break;
                    case 4: // Бронник
                        spawnedItem = Loot.RandomArmor();
                        if (spawnedItem == null) spawnedItem = Loot.RandomShield();
                        break;
                    case 1: // Маг / Писарь
                        spawnedItem = Loot.RandomScroll(50, 63, SpellbookType.Regular);
                        break;
                    case 2: case 3: // Алхимик / Трактирщик
                        spawnedItem = Loot.RandomPotion();
                        if (spawnedItem != null) spawnedItem.Hue = 1161; 
                        break;
                    case 7: // Семена Натуралиста
                        try
                        {
                            Seed rareSeed = new Seed();
                            rareSeed.PlantType = (PlantType)Utility.RandomMinMax(0, 16);
                            rareSeed.PlantHue = (PlantHue)Utility.RandomMinMax(0, 16);
                            spawnedItem = rareSeed;
                        }
                        catch { spawnedItem = new Candle(); }
                        break;
                    default:
                        spawnedItem = Loot.RandomArmorOrShieldOrWeaponOrJewelry(true, true, true);
                        break;
                }

                if (spawnedItem == null) spawnedItem = new Candle();

                if (spawnedItem != null)
                {
                    int propertyCount = 0;

                    // Накладываем магию рунических инструментов
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

                    // Выставляем ранг мощности для Diablo системы
                    var powerProp = spawnedItem.GetType().GetProperty("ItemPower");
                    if (powerProp != null && powerProp.CanWrite)
                    {
                        try { powerProp.SetValue(spawnedItem, Enum.ToObject(powerProp.PropertyType, 5), null); } catch { }
                    }

                    // Форматируем имя под палитру Diablo Loot
                    string originalName = spawnedItem.Name;
                    if (string.IsNullOrEmpty(originalName))
                    {
                        if (!string.IsNullOrEmpty(spawnedItem.ItemData.Name))
                            originalName = spawnedItem.ItemData.Name;
                        else
                            originalName = spawnedItem.GetType().Name;
                    }
                    spawnedItem.Name = DiabloLootColors.FormatName(spawnedItem, originalName);

                    // Сбрасываем вес, чтобы вендор не упал от перегруза
                    spawnedItem.Weight = 0;

                    // 1. СНАЧАЛА КЛАДЕМ ПРЕДМЕТ В BACKPACK (Пусть ядро ServUO зарегистрирует его по базовой цене)
                    this.Backpack.DropItem(spawnedItem);

                    // 2. РАССЧИТЫВАЕМ ДИНАМИЧЕСКУЮ ЦЕНУ НА ОСНОВЕ ЕГО РЕАЛЬНЫХ СВОЙСТВ
                    int finalPrice = 150000;

                    if (spawnedItem is Seed)
                    {
                        finalPrice = Utility.RandomMinMax(15000, 35000);
                    }
                    else
                    {
                        int goldValue = (int)(spawnedItem.Weight * 200) + (propertyCount * 35000);
                        goldValue = (int)(goldValue * Utility.RandomMinMax(0.85, 1.15));

                        // Ограничители баланса
                        if (goldValue < 45000) goldValue = 45000;
                        if (goldValue > 380000) goldValue = 380000;

                        finalPrice = goldValue;
                    }

                    // 3. ПРИНУДИТЕЛЬНО ПЕРЕЗАПИСЫВАЕМ ЦЕНУ НА ДИНАМИЧЕСКУЮ ЧЕРЕЗ СИСТЕМНУЮ ОБЕРТКУ VENDORITEM
                    if (sellItems.Contains(spawnedItem))
                    {
                        // Existing entry: replace with a new VendorItem having the updated price
                        VendorItem vi = sellItems[spawnedItem] as VendorItem;
                        if (vi != null)
                        {
                            // Preserve the original description if needed; otherwise set default
                            string description = vi.Description ?? "[Легендарный Артефакт 5 Уровня Мощности]";
                            VendorItem newVi = new VendorItem(spawnedItem, finalPrice, description, DateTime.UtcNow);
                            sellItems[spawnedItem] = newVi;
                        }
                    }
                    else
                    {
                        // If the core hasn't created the entry yet, create it manually
                        VendorItem vi = new VendorItem(spawnedItem, finalPrice, "[Легендарный Артефакт 5 Уровня Мощности]", DateTime.UtcNow);
                        sellItems[spawnedItem] = vi;
                    }

                    // 4. УЛЬТИМАТИВНЫЙ ФИКС: Вызываем штатный метод PlayerVendor для жесткой фиксации цены в памяти
                    this.InvalidateProperties();
                }
            }
        }

        private void CheckStockAndCleanup()
        {
            if (this.Deleted || this.Backpack == null) return;

            FieldInfo field = typeof(PlayerVendor).GetField("m_SellItems", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) return;

            Hashtable sellItems = field.GetValue(this) as Hashtable;
            if (sellItems != null && sellItems.Count == 0)
            {
                PublicOverheadMessage(Network.MessageType.Regular, 0x35, false, "Весь мой эксклюзивный товар распродан! Мне пора отправляться в путь. До встречи!");
                Timer.DelayCall(TimeSpan.FromSeconds(2.0), () => this.Delete());
            }
        }

        public LegendaryMerchant(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)1); writer.Write((int)_ProfessionIndex); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); _ProfessionIndex = reader.ReadInt(); }
    }

    public static class CityLegendaryMerchantMod
    {
        public static void CheckMerchantSpawn(Town town, int currentLevel)
        {
            if (town == null) return;

            RemoveMerchant(town);

            if (currentLevel < 5) return;

            if (Utility.RandomDouble() > 0.80) return;

            try
            {
                Map map = Map.Trammel;
                Point3D spawnLoc = Point3D.Zero;

                if (Enum.TryParse(town.ToString(), true, out City coreCity))
                {
                    CityLoyaltySystem sys = CityLoyaltySystem.GetCityInstance(coreCity);
                    if (sys != null && sys.Definition != null)
                    {
                        spawnLoc = sys.Definition.StoneLocation;
                        if (sys.Stone != null && sys.Stone.Map != null) map = sys.Stone.Map;
                    }
                }
                if (spawnLoc == Point3D.Zero) return;
                spawnLoc.X += 2;
                spawnLoc.Z = map.GetAverageZ(spawnLoc.X, spawnLoc.Y);
                string merchantTitle = "Странствующий Купец";
                int professionType = Utility.Random(8);
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
                LegendaryMerchant merchant = new LegendaryMerchant(merchantTitle, professionType);
                merchant.MoveToWorld(spawnLoc, map);
                merchant.PublicOverheadMessage(Network.MessageType.Regular, 0x35, false, $"Приветствую жителей! Я, [{merchantTitle}], прибыл в ваш великий megapolis.");
                Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x35, $"[Торговля] В развитый мегаполис {town} прибыл [{merchantTitle}] с эксклюзивными артефактами!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CityDevelopment] Критическая ошибка спавна купца: {ex.Message}\n{ex.StackTrace}");
            }
        }
        public static void RemoveMerchant(Town town)
        {
            if (town == null) return;

            // Чистый перебор существ в мире: никаких конфликтов с типами List и Town!
            List<Mobile> toDelete = new List<Mobile>();
            
            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m != null && !m.Deleted && m is LegendaryMerchant)
                {
                    Region mobRegion = Region.Find(m.Location, m.Map);
                    if (mobRegion != null && mobRegion.Name != null && 
                        mobRegion.Name.IndexOf(town.ToString(), StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        toDelete.Add(m);
                    }
                    else if (town.ToString().Contains("Moonglow") && m.X >= 4400 && m.X <= 4560)
                    {
                        toDelete.Add(m); // Жесткая страховка для доков Мунглоу
                    }
                }
            }

            foreach (Mobile m in toDelete) { m.Delete(); }
        }
    }
}