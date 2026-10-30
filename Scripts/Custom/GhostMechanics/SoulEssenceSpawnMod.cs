using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Regions;

namespace Server.Custom.SoulEssences
{
    public class SoulEssence : Item
    {
        private const int EssenceItemID = 0x3198;

        public SoulEssence() : base(EssenceItemID)
        {
            Name = "Эссенция Души (Soul Essence)";
            Hue = 1365; 
            Weight = 0.1;
        }

        public SoulEssence(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); ItemID = EssenceItemID; Hue = 1365; Weight = 0.1; }
    }

    public static class SoulEssenceSpawnMod
    {
        public static readonly double CemeteryDropChance = 0.05; // 5% шанс из обычных мобов кладбищ Фелукки
        public static readonly double BossDropChance = 1.00;     // 100% из Чемпионов

        public static void Initialize()
        {
            EventSink.CreatureDeath += new CreatureDeathEventHandler(OnCreatureDeath);
        }

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            BaseCreature bc = e.Creature as BaseCreature;
            if (bc == null || bc.Map != Map.Felucca || bc.Backpack == null) return; 

            Region reg = bc.Region;
            bool isCemetery = reg != null && reg.Name != null && reg.Name.IndexOf("cemetery", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isDespise = reg != null && reg.Name != null && reg.Name.IndexOf("despise", StringComparison.OrdinalIgnoreCase) >= 0;

            // Проверяем, является ли убитый моб Боссом-Чемпионом
            bool isChampion = bc is BaseChampion;

            // Дроп с Чемпионов (Барракун и т.д.) — 100% шанс, от 1 до 3 штук падает в рюкзак
            if (isChampion && Utility.RandomDouble() < BossDropChance)
            {
                int count = Utility.RandomMinMax(1, 3);
                for (int i = 0; i < count; i++)
                {
                    bc.Backpack.DropItem(new SoulEssence());
                }
                return;
            }

            // Дроп из обычных кладбищенских или данжевых мобов Felucca
            if ((isCemetery || isDespise) && Utility.RandomDouble() < CemeteryDropChance)
            {
                bc.Backpack.DropItem(new SoulEssence());
            }
        }
    }
}