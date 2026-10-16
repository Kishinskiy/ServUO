using System;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.TreasureRecycling
{
    public static class TreasureMapRecycling
    {
        public static void Initialize()
        {
            // Модуль полностью автономен и готов к расширению в будущем
        }

        // 🔥 МЕТОД УТИЛИЗАЦИИ: Разрезает выполненные карты сокровищ на чистые пергаменты магов 🎉📜✂️
        public static bool TryCutCompletedMap(Mobile from, Item item)
        {
            if (from == null || item == null) 
                return false;

            // Проверяем текстовый тип предмета во избежание конфликтов пространств имен ядра
            string typeName = item.GetType().Name.ToLower();

            if (typeName.Contains("treasuremap"))
            {
                // Используем рефлексию C#, чтобы safely прочитать нативное свойство Completed из ядра
                var completedProp = item.GetType().GetProperty("Completed");
                if (completedProp != null)
                {
                    bool isCompleted = (bool)completedProp.GetValue(item, null);

                    if (isCompleted)
                    {
                        // 🔥 ИСПРАВЛЕНО: SkillName.Inscription заменен на нативный SkillName.Inscribe ядра ServUO! 🎉⚙️🛠️
                        if (from.Skills[SkillName.Inscribe].Value < 40.0 && from.Skills[SkillName.Tailoring].Value < 40.0)
                        {
                            from.SendMessage(0x22, "Вашего мастерства Начертания или Портняжного дела (нужно 40.0) недостаточно, чтобы ровно нарезать этот пергамент!");
                            return true; 
                        }

                        // Уничтожаем отработанный пергамент
                        item.Delete();

                        // Выдаем 2 чистых свитка для создания свитков опознания Diablo-лута
                        from.AddToBackpack(new BlankScroll(2));
                        from.PlaySound(0x249); // Звук шуршания бумаги
                        from.SendMessage(0x58, "Вы успешно разрезали старую карту сокровищ на 2 чистых свитка (Blank Scrolls)!");
                        return true; 
                    }
                    else
                    {
                        from.SendMessage(0x22, "Эта карта еще не выполнена! Саботировать ненайденные сокровища глупо.");
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
