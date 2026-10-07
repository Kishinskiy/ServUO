using System.Text.RegularExpressions;

namespace Server.Custom.PowerScrollsAdditional
{
    public abstract class BaseAdditionalPowerScroll : Item
    {
        private int m_Value; // Величина прибавки (+10, +20, +40, +50)

        [CommandProperty(AccessLevel.GameMaster)]
        public SkillName Skill { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Value
        {
            get => m_Value;
            set
            {
                m_Value = value;
                InvalidateProperties();
            }
        }

        public BaseAdditionalPowerScroll(SkillName skill, int value) : base(0x14F0)
        {
            Skill = skill;
            m_Value = value;
            Weight = 1.0;

            if (value <= 20) Hue = 1153;
            else if (value <= 40) Hue = 1161;
            else Hue = 1175;
        }

        public BaseAdditionalPowerScroll(Serial serial) : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            var skillFriendlyName = Skill.ToString();
            skillFriendlyName = Regex.Replace(skillFriendlyName, "([a-z])([A-Z])", "$1 $2");

            list.Add(1060658, "Тип\tМифический Свиток Прироста");
            list.Add(1060659, $"Навык\t{skillFriendlyName}");
            list.Add(1060660, $"Увеличивает лимит на\t+{m_Value}.0");
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            var skill = from.Skills[Skill];
            if (skill == null)
            {
                from.SendMessage(0x22, "Этот навык недоступен для вашего персонажа.");
                return;
            }

            // ИСПРАВЛЕНО CS0841: Создаем красивое имя навыка в самом начале метода,
            // чтобы переменная существовала в контексте ДО всех проверок и условий!
            var skillFriendlyName = Skill.ToString();
            skillFriendlyName = Regex.Replace(skillFriendlyName, "([a-z])([A-Z])", "$1 $2");

            // КРИТИЧЕСКОЕ УСЛОВИЕ БАЛАНСА: ПРОВЕРКА РЕАЛЬНОЙ ПРОКАЧКИ ДО 120.0
            if (skill.Base < 120.0 && from.AccessLevel == AccessLevel.Player)
            {
                from.SendMessage(0x22,
                    $"Вы не можете использовать этот свиток! Сначала вы должны честно развить навык {skillFriendlyName} до 120.0 реального значения.");
                return;
            }

            var currentCap = (int)skill.Cap;

            if (currentCap >= 250)
            {
                from.SendMessage(0x22, "Лимит этого навыка уже достиг абсолютного максимума (250.0).");
                return;
            }

            var newCap = currentCap + m_Value;
            if (newCap > 250) newCap = 250;

            skill.Cap = newCap;

            from.PlaySound(0x1F7);
            from.FixedParticles(0x373A, 1, 15, 9907, Hue, 0, EffectLayer.Head);
            from.SendMessage(88,
                $"Вы успешно использовали свиток! Лимит навыка {skillFriendlyName} увеличен с {currentCap}.0 до {newCap}.0!");

            Consume();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);
            writer.Write((int)Skill);
            writer.Write(m_Value);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
            Skill = (SkillName)reader.ReadInt();
            m_Value = reader.ReadInt();
        }
    }
}
