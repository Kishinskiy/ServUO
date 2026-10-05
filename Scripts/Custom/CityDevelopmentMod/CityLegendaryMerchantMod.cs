using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Factions;
using Server.Custom.AdvancedNpcTrade;

namespace Server.Custom.CityDevelopment
{
    public class LegendaryMerchant : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();
        protected override List<SBInfo> SBInfos { get { return m_SBInfos; } }

        public override void InitSBInfo() { }

        public LegendaryMerchant(string title) : base(title)
        {
            Name = NameList.RandomName("male");
            Title = title;
            Hue = Utility.RandomSkinHue();
            CantWalk = true;
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
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public static class CityLegendaryMerchantMod
    {
        public static void CheckMerchantSpawn(Town town, int currentLevel)
        {
            if (town == null) return;

            RemoveMerchant(town);

            if (currentLevel < 5) return;
            if (Utility.RandomDouble() > 0.30) return;

            // ИСПРАВЛЕНО: Карта определяется динамически на основе TownStone
            Map map = Map.Trammel; 
            Point3D spawnLoc = Point3D.Zero;
            bool centerFound = false;

            foreach (Item item in World.Items.Values)
            {
                if (item != null && !item.Deleted && item.Map != null && item.Map != Map.Internal)
                {
                    string typeName = item.GetType().Name.ToLower();
                    if (typeName.Contains("factionstone") || typeName.Contains("townstone") || typeName.Contains("citystone"))
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

            LegendaryMerchant merchant = new LegendaryMerchant($"[{merchantTitle}]");
            merchant.MoveToWorld(spawnLoc, map);

            for (int i = 0; i < 5; i++)
            {
                var customArtifacts = AdvancedNpcTradeMod.GetLegendaryArtifacts(merchant);
                foreach (var art in customArtifacts)
                {
                    art.Amount = 1;
                    art.OnRestock();
                    merchant.BuyPack.DropItem(art.GetEntity() as Item);
                }
            }

            merchant.PublicOverheadMessage(Network.MessageType.Regular, 0x35, false, $"Приветствую жителей! Я, {merchantTitle}, прибыл в ваш великий мегаполис. Привез 5 редчайших товаров!");
        }

        public static void RemoveMerchant(Town town)
        {
            if (town == null) return;

            List<Mobile> toDelete = new List<Mobile>();
            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m != null && !m.Deleted && m is LegendaryMerchant)
                {
                    Region mobRegion = Region.Find(m.Location, m.Map);
                    if (Town.FromRegion(mobRegion) == town)
                    {
                        toDelete.Add(m);
                    }
                }
            }

            foreach (Mobile m in toDelete) { m.Delete(); }
        }
    }
}