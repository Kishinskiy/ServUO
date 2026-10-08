using System;
using Server.Custom.ExileHunterBestiary.Gumps;
using Server.Network;

namespace Server.Custom.ExileHunterBestiary.Items
{
    public class HunterBestiary : Item
    {
        public const int DefaultBestiaryHue = 1153; // Ancient Mythic Tome Slate

        [Constructable]
        public HunterBestiary() : base(0x2252)
        {
            Name = "The Grand Hunter's Bestiary";
            Hue = DefaultBestiaryHue;
            Weight = 2.0;
            LootType = LootType.Blessed;
        }

        public HunterBestiary(Serial serial) : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }

            from.CloseGump(typeof(HunterBestiaryGump));
            from.SendGump(new HunterBestiaryGump(from));
        }

        // =========================================================================
        // ИСПРАВЛЕНО: КРАСИВОЕ ОТОБРАЖЕНИЕ БОНУСОВ И РАНГОВ В ТУЛТИПЕ КНИГИ 🔍📖
        // =========================================================================
        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1070722, "<BASEFONT COLOR=#D1C4E9>Grand Grimoire of Monster Lore<BASEFONT COLOR=#FFFFFF>");
            list.Add(1070722, "<BASEFONT COLOR=#FFE082>Дважды кликните, чтобы открыть Бестиарий<BASEFONT COLOR=#FFFFFF>");
        }

        // Метод динамически пересчитывает ранги категорий и выводит сводку прямо на экран игрока
        public override void SendPropertiesTo(Mobile from)
        {
            var list = new ObjectPropertyList(this);
            GetProperties(list);

            var profile = HunterBestiaryEngine.GetProfile(from);
            int mastered = profile != null ? profile.TotalMastered : 0;
            int total = HunterBestiaryEngine.RegistryById.Count;

            // Выводим общее количество открытых записей
            list.Add(1070722, string.Format("<BASEFONT COLOR=#00FFCC>Исследовано монстров: {0} / {1}<BASEFONT COLOR=#FFFFFF>", mastered, total));

            // Построчно выводим текущий Ранг по каждой категории в стиле UO Outlands
            if (profile != null)
            {
                foreach (BestiaryCategory cat in Enum.GetValues(typeof(BestiaryCategory)))
                {
                    int lvl = profile.GetGroupLevel(cat);
                    
                    if (lvl > 0)
                    {
                        // Подсвечиваем прокачанные категории зеленым цветом и пишем итоговые бонусы
                        int damBonus = lvl * 2;
                        int goldBonus = lvl * 3;
                        list.Add(1070722, string.Format("<BASEFONT COLOR=#00E676>• {0}: Ранг {1}/5 (+{2}% урон, +{3}% золото)<BASEFONT COLOR=#FFFFFF>", cat, lvl, damBonus, goldBonus));
                    }
                    else
                    {
                        // Категории без прогресса оставляем серыми
                        list.Add(1070722, string.Format("<BASEFONT COLOR=#90A4AE>• {0}: Ранг 0/5 (Не изучено)<BASEFONT COLOR=#FFFFFF>", cat));
                    }
                }
            }

            list.Terminate();
            from.Send(list);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}
