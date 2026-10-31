using System;
using Server;
using Server.Items;

namespace Server.Custom.JewelryCrafting
{
    public class JewelryGem : Item
    {
        private string m_GemEffect;
        private double m_GemQuality = 1.0;

        [CommandProperty(AccessLevel.GameMaster)]
        public string GemEffect { get { return m_GemEffect; } set { m_GemEffect = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public double GemQuality { get { return m_GemQuality; } set { m_GemQuality = value; InvalidateProperties(); } }

        [Constructable]
        public JewelryGem(string gemEffect, double quality, int itemID, int hue) : base(itemID)
        {
            Weight = 0.1;
            m_GemEffect = gemEffect;
            m_GemQuality = quality;
            Hue = hue;

            UpdateGemNameAndTier();
        }

        private void UpdateGemNameAndTier()
        {
            // Лорные категории в зависимости от финального множителя силы
            string tier = "Обычный";
            if (m_GemQuality >= 3.0) tier = "Легендарный";
            else if (m_GemQuality >= 2.0) tier = "Совершенный";
            else if (m_GemQuality >= 1.5) tier = "Безупречный";
            else if (m_GemQuality < 1.0) tier = "Треснувший";

            Name = $"{tier} {GetGemBaseName(m_GemEffect)}";
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            
            int percent = (int)Math.Round(m_GemQuality * 100);
            list.Add($"Чистота огранки: {percent}%");
            
            // Выводим динамически рассчитанный бонус прямо в свойства предмета! 🎉
            list.Add(0x35, $"Эффект: {GetGemBonusDescription(m_GemEffect, m_GemQuality)}");
        }

        private string GetGemBaseName(string effect)
        {
            switch (effect)
            {
                case "sapphire": return "Сапфир Скорости";
                case "emerald": return "Изумруд Точности";
                case "diamond": return "Алмаз Защиты";
                case "citrine": return "Цитрин Выносливости";
                case "amber": return "Янтарь Снабжения";
                case "ruby": return "Рубин Урона";
                case "amethyst": return "Аметист Экономии";
                case "tourmaline": return "Турмалин Потока";
                case "starsapphire": return "Звёздный Сапфир Воли";
                case "perfectemerald": return "Изумруд Жизни";
                case "darksapphire": return "Сапфир Астрала";
                case "bluediamond": return "Голубой Алмаз Времени";
                case "turquoise": return "Бирюза Грома";
                case "ecrucitrine": return "Экру-Цитрин Знахаря";
                case "vamp_shard": return "Осколок Вампиризма";
                default: return "Драгоценный Камень";
            }
        }

        // 🔥 ГЛАВНАЯ ЛОГИКА: Перемножаем базовый бафф на индивидуальный коэффициент крафтера!
        private string GetGemBonusDescription(string effect, double quality)
        {
            switch (effect)
            {
                case "sapphire": 
                    return $"+{(int)Math.Round(5.0 * quality)}% SSI (Скорость атаки)";
                case "emerald": 
                    return $"+{(int)Math.Round(5.0 * quality)}% HCI (Шанс попадания)";
                case "diamond": 
                    return $"+{(int)Math.Round(5.0 * quality)}% DCI (Шанс защиты)";
                case "citrine": 
                    return $"+{(int)Math.Round(5.0 * quality)} Стамина-Реген";
                case "amber": 
                    return $"+{(int)Math.Round(10.0 * quality)}% LRC (Экономия реагентов)";
                case "ruby": 
                    return $"+{(int)Math.Round(15.0 * quality)}% DI (Увеличение урона)";
                case "amethyst": 
                    return $"-{(int)Math.Round(5.0 * quality)}% LMC (Снижение расхода маны)";
                case "tourmaline": 
                    return $"+{(int)Math.Round(3.0 * quality)} Мана-Реген";
                case "starsapphire": 
                    return $"+{(int)Math.Round(15.0 * quality)} Magic Resist";
                case "perfectemerald": 
                    return $"+{(int)Math.Round(10.0 * quality)} Макс. Здоровье (Hits)";
                case "darksapphire": 
                    return $"+{(int)Math.Round(15.0 * quality)} Макс. Мана (Mana)";
                case "bluediamond": 
                    return $"+{(int)Math.Round(1.0 * quality)} Faster Casting";
                case "turquoise": 
                    return $"+{(int)Math.Round(35.0 * quality)}% Шанс удара молнией";
                case "ecrucitrine": 
                    return $"+{(int)Math.Round(50.0 * quality)}% Усиление лечебных зелий";
                case "vamp_shard": 
                    return $"+{(int)Math.Round(20.0 * quality)}% Вампиризм (Восстановление HP при ударе)";
                default: 
                    return "Магический резонанс не настроен";
            }
        }

        public JewelryGem(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); writer.Write(m_GemEffect); writer.Write(m_GemQuality); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); m_GemEffect = reader.ReadString(); m_GemQuality = reader.ReadDouble(); UpdateGemNameAndTier(); }
    }
}
