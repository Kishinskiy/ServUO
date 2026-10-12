using System;
using Server;
using Server.Items;

namespace Server.Custom.DiabloLoot
{
    public static class DiabloLootColors
    {
        public const string NormalColor    = "#FFFFFF"; // Белый (Обычный)
        public const string MagicColor     = "#4169E1"; // Синий (Магический)
        public const string RareColor      = "#FFFF00"; // Желтый (Редкий)
        public const string LegendaryColor = "#FF8C00"; // Оранжевый (Легендарный)

        // Цифровой класс редкости (0 = Обычный, 3 = Легендарный)
        public static int GetRarityClass(Item item)
        {
            if (item == null) return 0;

            if (item is BaseWeapon weapon)
            {
                int power = (int)weapon.ItemPower;
                if (power > 0)
                {
                    if (power >= 5) return 3; // Легендарный
                    if (power >= 3) return 2; // Редкий
                    return 1;                 // Магический
                }
            }

            if (item.IsArtifact)
                return 3;

            int propsCount = 0;
            if (item is BaseWeapon w)
            {
                if (w.Attributes != null && !w.Attributes.IsEmpty) propsCount += 2;
                if (w.WeaponAttributes != null && !w.WeaponAttributes.IsEmpty) propsCount += 2;
                if ((int)w.AccuracyLevel > 0) propsCount++;
                if ((int)w.DamageLevel > 0) propsCount++;
                if ((int)w.DurabilityLevel > 0) propsCount++;
            }
            else if (item is BaseArmor a)
            {
                if (a.Attributes != null && !a.Attributes.IsEmpty) propsCount += 2;
                if (a.ArmorAttributes != null && !a.ArmorAttributes.IsEmpty) propsCount += 2;
                if ((int)a.ProtectionLevel > 0) propsCount++;
                if ((int)a.Durability > 0) propsCount++;
            }

            if (propsCount >= 5) return 3;
            if (propsCount >= 3) return 2;
            if (propsCount >= 1) return 1;

            return 0;
        }

        public static string GetRarityColor(Item item)
        {
            int rClass = GetRarityClass(item);
            switch (rClass)
            {
                case 3: return LegendaryColor;
                case 2: return RareColor;
                case 1: return MagicColor;
                default: return NormalColor;
            }
        }

        public static string FormatName(Item item, string originalName)
        {
            string color = GetRarityColor(item);
            return String.Format("<BASEFONT COLOR={0}>{1}</BASEFONT>", color, originalName);
        }
    }
}
