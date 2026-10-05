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

            RemoveMerchant(town);

            // Торговец приходит только на максимальном 5-м уровне города
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

            string merchantTitle = "Странствующий Купец";
            int professionType = Utility.Random(4); // 0 - Кузнец, 1 - Маг, 2 - Алхимик, 3 - Повар

            switch (professionType)
            {
                case 0: merchantTitle = "Легендарный Оружейник"; break;
                case 1: merchantTitle = "Древний Магистр Магии"; break;
                case 2: merchantTitle = "Магистр Алхимии"; break;
                case 3: merchantTitle = "Королевский Интендант"; break;
            }

            LegendaryMerchant merchant = new LegendaryMerchant($"[{merchantTitle}]");
            merchant.MoveToWorld(spawnLoc, map);

            // Наполняем магазин 5 лимитированными артефактами
            for (int i = 0; i < 5; i++)
            {
                // ИСПРАВЛЕНО: Теперь AdvancedNpcTradeMod успешно распознается компилятором
                var customArtifacts = AdvancedNpcTradeMod.GetLegendaryArtifacts(merchant);
                foreach (var art in customArtifacts)
                {
                    art.Amount = 1;
                    art.OnRestock();
                    merchant.BuyPack.DropItem(art.GetEntity() as Item);
                }
            }

            merchant.PublicOverheadMessage(Network.MessageType.Regular, 0x35, false, $"Приветствую жителей! Я прибыл в ваш великий мегаполис и привез 5 редчайших артефактов на продажу!");
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
