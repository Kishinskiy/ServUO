using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Factions;
using Server.Custom.AdvancedNpcTrade; // ИСПРАВЛЕНО: Подключено пространство имен для доступа к AdvancedNpcTradeMod

namespace Server.Custom.CityDevelopment
{
    // Кастомный класс для живого NPC торговца-легенды
    public class LegendaryMerchant : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();

        protected override List<SBInfo> SBInfos { get { return m_SBInfos; } }

        public override void InitSBInfo()
        {
            // Не заполняем стандартные витрины
        }

        public LegendaryMerchant(string title) : base(title)
        {
            Name = NameList.RandomName("male");
            Title = title;
            Hue = Utility.RandomSkinHue();

            CantWalk = true;

            // ИСПРАВЛЕНО: Так как IsInvulnerable доступен только для чтения,
            // делаем NPC неуязвимым через стандартное свойство Blessed ядра ServUO
            Blessed = true;
        }

        public override bool OnBuyItems(Mobile buyer, List<BuyItemResponse> list)
        {
            bool success = base.OnBuyItems(buyer, list);

            if (success)
            {
                var buyInfo = GetBuyInfo();
                bool hasStock = false;
                foreach (var bii in buyInfo)
                {
                    if (bii.Amount > 0) { hasStock = true; break; }
                }

                if (!hasStock)
                {
                    PublicOverheadMessage(Network.MessageType.Regular, 0x35, false, "Весь мой эксклюзивный товар распродан! Мне пора отправляться в путь. До встречи!");
                    Timer.DelayCall(TimeSpan.FromSeconds(2.0), () => Delete());
                }
            }

            return success;
        }

        public LegendaryMerchant(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    // МЕНЕДЖЕР УПРАВЛЕНИЯ СПАВНОМ ТОРГОВЦЕВ
    public static class CityLegendaryMerchantMod
    {
        public static void CheckMerchantSpawn(Town town, int currentLevel)
        {
            if (town == null) return;

            // Очищаем старого торговца в этом городе
            RemoveMerchant(town);

            // Торговец приходит ТОЛЬКО на максимальном 5-м уровне города
            if (currentLevel < 5)
                return;

            // Бросаем кубик на 30% шанс появления
            if (Utility.RandomDouble() > 0.30)
                return;

            Map map = Map.Felucca;
            Point3D spawnLoc = Point3D.Zero;
            bool centerFound = false;

            foreach (Item item in World.Items.Values)
            {
                if (item != null && !item.Deleted && item.Map != null && item.Map != Map.Internal)
                {
                    string typeName = item.GetType().Name.ToLower();
                    if (typeName.Contains("factionstone") || typeName.Contains("townstone"))
                    {
                        Region itemRegion = Region.Find(item.Location, item.Map);
                        if (Town.FromRegion(itemRegion) == town)
                        {
                            spawnLoc = item.Location;
                            map = item.Map;
                            centerFound = true;
                            break;
                        }
                    }
                }
            }

            if (!centerFound || spawnLoc == Point3D.Zero) return;

            spawnLoc.X += 2;
            spawnLoc.Z = map.GetAverageZ(spawnLoc.X, spawnLoc.Y);

            // ИСПРАВЛЕНО: Выбираем случайную профессию из 7 возможных вариантов (от 0 до 6)
            string merchantTitle = "Странствующий Купец";
            int professionType = Utility.Random(8);

            // Переменная-заглушка, которая подскажет основному моду лояльности, какой тип лута генерировать
            string fakeClassName = "blacksmith";

            switch (professionType)
            {
                case 0:
                    merchantTitle = "Легендарный Оружейник";
                    fakeClassName = "blacksmith"; // Генерирует: Оружие / Тяжелую металлическую броню
                    break;
                case 1:
                    merchantTitle = "Древний Магистр Магии";
                    fakeClassName = "mage"; // Генерирует: Высокоуровневые свитки заклинаний
                    break;
                case 2:
                    merchantTitle = "Магистр Алхимии";
                    fakeClassName = "alchemist"; // Генерирует: Концентрированные зелья и эликсиры
                    break;
                case 3:
                    merchantTitle = "Королевский Интендант";
                    fakeClassName = "cook"; // Генерирует: Элитные вина и восстанавливающие экстракты
                    break;
                case 4:
                    // НОВОЕ: Для Портных и Кожевников (Tailoring / Leatherworking)
                    merchantTitle = "Придворный Модельер";
                    fakeClassName = "tailor"; // Генерирует: Зачарованную одежду, плащи, кожаные и шипованные доспехи
                    break;
                case 5:
                    // НОВОЕ: Для Лукарей и Охотников (Bowcraft / Fletching)
                    merchantTitle = "Мастер-Лучник Охоты";
                    fakeClassName = "bowyer"; // Генерирует: Композитные луки, элитные арбалеты с руническими статами
                    break;
                case 6:
                    // НОВОЕ: Для Плотников и Инженеров-Тинкеров (Carpentry / Tinkering)
                    merchantTitle = "Главный Инженер Гильдии";
                    fakeClassName = "carpenter"; // Генерирует: Магические деревянные щиты, редкие детали, инструменты и приборы
                    break;
                case 7:
                    // НОВОЕ: Для фермеров, тамеров, ветеринаров и рыболовов!
                    merchantTitle = "Великий Натуралист Земель";
                    break;
            }

            // Создаем и спавним NPC в мире
            LegendaryMerchant merchant = new LegendaryMerchant($"[{merchantTitle}]");
            merchant.MoveToWorld(spawnLoc, map);

            // Наполняем магазин 5 лимитированными артефактами на основе выбранного направления крафта
            for (int i = 0; i < 5; i++)
            {
                // Наше кастомное ядро AdvancedNpcTradeMod.GetLegendaryArtifacts считает имя класса
                // заспавненного мерчанта и сгенерирует строго тематический лут!
                var customArtifacts = AdvancedNpcTradeMod.GetLegendaryArtifacts(merchant);
                foreach (var art in customArtifacts)
                {
                    art.Amount = 1;
                    art.OnRestock();
                    merchant.BuyPack.DropItem(art.GetEntity() as Item);
                }
            }

            // Оповещаем регион о прибытии торговца
            merchant.PublicOverheadMessage(Network.MessageType.Regular, 0x35, false, $"Приветствую жителей! Я, {merchantTitle}, прибыл в ваш мегаполис. Привез 5 редчайших товаров!");
        }

        public static void RemoveMerchant(Town town)
        {
            if (town == null) return;

            List<Mobile> toDelete = new List<Mobile>();
            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m != null && !m.Deleted && m is LegendaryMerchant && Town.FromRegion(m.Region) == town)
                {
                    toDelete.Add(m);
                }
            }

            foreach (Mobile m in toDelete)
            {
                m.Delete();
            }
        }
    }
}
