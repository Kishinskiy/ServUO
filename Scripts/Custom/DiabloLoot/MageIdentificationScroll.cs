using System;
using Server;
using Server.Items;
using Server.Targeting;
using Server.Engines.XmlSpawner2;
using Server.Custom.DiabloLoot;
using Server.Custom.MagicProfessions;

namespace Server.Custom.CurseSystem
{
    public class MageIdentificationScroll : BaseMageCurseScroll
    {
        protected override string ScrollName { get { return "Свиток Распознавания Предметов"; } }
        protected override int ScrollHue { get { return 1365; } }

        [Constructable]
        public MageIdentificationScroll() : base(0x14F0)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !from.Alive) return;

            if (from.Backpack == null || !IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); 
                return;
            }

            from.SendMessage("Выберите неопознанный предмет для распознавания его скрытых свойств.");
            from.Target = new MageIdentificationTarget(this);
        }

        public MageIdentificationScroll(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { SerializeScroll(writer); }
        public override void Deserialize(GenericReader reader) { DeserializeScroll(reader); }
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
            if (m_Scroll == null || m_Scroll.Deleted || from == null || !from.Alive || from.Backpack == null || !m_Scroll.IsChildOf(from.Backpack))
                return;

            if (targeted is MagicProfessionGrimoire grimoire)
            {
                if (grimoire.HasIdentify)
                {
                    from.SendMessage("Этот ритуал уже вплетен в структуру вашего гримуара.");
                    return;
                }

                grimoire.HasIdentify = true;
                grimoire.Name = "Гримуар Магических Профессий"; // Переименовываем из "Пустого"
                
                from.PlaySound(0x249); // Звук магической записи
                Effects.SendTargetParticles(grimoire, 0x375A, 9, 20, 1266, EffectLayer.Waist);
                from.SendMessage(0x35, "Вы успешно вплели свиток и изучили Ритуал Распознавания в Гримуаре!");
                
                m_Scroll.Delete(); // Свиток расходуется на изучение
                return;
            }
            
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
                double difficulty = rarityClass * 12.0;
                double identifyChance = BaseMageCurseScroll.GetSuccessChance(from, m_Scroll.QualityTier, difficulty);

                if (Utility.RandomDouble() < identifyChance)
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

                m_Scroll.Delete(); // Свиток расходуется в любом случае при использовании
            }
            else
            {
                from.SendMessage(0x22, "Распознавать можно только предметы экипировки.");
            }
        }
    }
}
