using System;
using Server;
using Server.Items;

namespace Server.Custom.DiabloLoot
{
    public static class DiabloLootColors
    {
        // Палитра цветов в стиле Diablo 2 / Diablo 3
        public const string NormalColor    = "#FFFFFF"; // Белый (Обычный)
        public const string MagicColor     = "#4169E1"; // Синий (Магический)
        public const string RareColor      = "#FFFF00"; // Желтый (Редкий)
        public const string LegendaryColor = "#FF8C00"; // Оранжевый (Легендарный)

        public static string GetRarityColor(Item item)
        {
            if (item == null)
                return NormalColor;

            // Проверяем, есть ли у оружия или брони встроенное свойство ItemPower (из системы рефоржинга/лута ServUO)
            // Используем динамическое приведение, чтобы скрипт подходил и под BaseWeapon, и под BaseArmor
            if (item is BaseWeapon weapon)
            {
                int power = (int)weapon.ItemPower;
                if (power > 0)
                {
                    if (power >= 5) return LegendaryColor; // Высокие артефакты (Legendary, Mythical)
                    if (power >= 3) return RareColor;      // Средние артефакты (Greater, Major)
                    return MagicColor;                     // Низкие артефакты (Minor)
                }
            }

            // Глобальный флаг артефакта ServUO (для кастомных вещей)
            if (item.IsArtifact)
                return LegendaryColor;

            // Резервный подсчет для вещей, у которых ItemPower равен None (старый крафт или обычный дроп)
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

            // Градация по свойствам, если ItemPower пустой
            if (propsCount >= 5) return LegendaryColor;
            if (propsCount >= 3) return RareColor;
            if (propsCount >= 1) return MagicColor;

            return NormalColor;
        }

        // Метод для оборачивания имени в HTML теги
        public static string FormatName(Item item, string originalName)
        {
            string color = GetRarityColor(item);
            // Use matching BASEFONT tags for proper HTML rendering in client
            return String.Format("<BASEFONT COLOR={0}>{1}</BASEFONT>", color, originalName);
        }
    }
}
