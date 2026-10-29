using System;
using Server;
using Server.Items;
using Server.Targeting;
using Server.Engines.XmlSpawner2;
using Server.Custom.DiabloLoot;

namespace Server.Custom.CurseSystem
{
    public class MageIdentificationScroll : Item
    {
        [Constructable]
        public MageIdentificationScroll() : base(0x14F0)
        {
            Name = "Свиток Распознавания Предметов";
            Hue = 1365; // Синий магический цвет
            Weight = 1.0;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !from.Alive) return;

            if (from.Backpack == null || !IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // Предмет должен быть в рюкзаке
                return;
            }

            from.SendMessage("Выберите неопознанный предмет для распознавания его скрытых свойств.");
            from.Target = new MageIdentificationTarget(this);
        }

        public MageIdentificationScroll(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class MageIdentificationTarget : Target
    {
        private readonly MageIdentificationScroll m_Scroll;

        public MageIdentificationTarget(MageIdentificationScroll scroll) : base(2, false, TargetFlags.None)
        {
            m_Scroll = scroll;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_Scroll == null || m_Scroll.Deleted || from == null || !from.Alive) return;

            if (targeted is Item item)
            {
                XmlCurseAttribute curse = XmlAttach.FindAttachment(item, typeof(XmlCurseAttribute)) as XmlCurseAttribute;
                if (curse == null)
                {
                    from.SendMessage("Этот предмет не имеет скрытых магических свойств.");
                    return;
                }

                if (curse.IsIdentified)
                {
                    from.SendMessage("Этот предмет уже полностью распознан.");
                    return;
                }

                int rarityClass = DiabloLootColors.GetRarityClass(item);
                double evalIntel = from.Skills[SkillName.EvalInt].Value; 
                int playerInt = from.Int; 

                // Расчет сложности на основе класса редкости Diablo-лута
                double reqEval = 30.0 + (rarityClass * 15.0);
                int reqInt = 40 + (rarityClass * 10);

                if (evalIntel < reqEval || playerInt < reqInt)
                {
                    from.PlaySound(0x5C3);
                    from.SendMessage(0x22, "Вашего интеллекта и ментального фокуса недостаточно, чтобы расшифровать руны на этом предмете.");
                    return;
                }

                // Расчет шанса успеха. На высоком уровне (100+) — гарантированный 100% успех
                double identifyChance = (evalIntel / (reqEval * 1.5));

                if (Utility.RandomDouble() < identifyChance || evalIntel >= 100.0)
                {
                    curse.IsIdentified = true;

                    if (item is BaseWeapon weapon) weapon.Identified = true;
                    else if (item is BaseArmor armor) armor.Identified = true;

                    item.InvalidateProperties();
                    item.SendPropertiesTo(from);
                    
                    from.PlaySound(0x249);
                    from.SendMessage(0x35, "Ритуал прошел успешно. Свойства предмета распознаны.");
                }
                else
                {
                    from.PlaySound(0x54A);
                    from.SendMessage(0x22, "Вам не удалось распознать свойства предмета, магические руны ускользнули от вашего разума.");
                }

                m_Scroll.Delete(); // Свиток расходуется при любом исходе
            }
            else
            {
                from.SendMessage(0x22, "Распознавать можно только предметы экипировки.");
            }
        }
    }
}
