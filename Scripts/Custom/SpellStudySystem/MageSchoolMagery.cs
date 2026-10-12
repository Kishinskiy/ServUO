using System;
using Server;
using Server.Items;

namespace Server.Custom.SpellStudySystem
{
    public class MageSchoolMagery : BaseMagicSchool
    {
        public override string SchoolName => "Классическая Магия";
        public override SkillName SupportSkill => SkillName.EvalInt;

        public override bool MatchBook(Spellbook book)
        {
            return book.SpellbookType == SpellbookType.Regular;
        }

        public override bool CheckRequirements(Mobile from, Spellbook book, int spellID, int offsetID)
        {
            int circleIndex = offsetID / 8;
            double magerySkill = from.Skills[SkillName.Magery].Base;
            double requiredSkill = 0.0;

            switch (circleIndex)
            {
                case 0: requiredSkill = 0.0; break;
                case 1: requiredSkill = 12.5; break;
                case 2: requiredSkill = 25.0; break;
                case 3: requiredSkill = 37.5; break;
                case 4: requiredSkill = 50.0; break;
                case 5: requiredSkill = 62.5; break;
                case 6: requiredSkill = 75.0; break;
                case 7: requiredSkill = 87.5; break;
            }

            if (magerySkill < requiredSkill)
            {
                from.SendMessage(0x22, $"Ваш разум не готов к осмыслению {circleIndex + 1}-го круга! Требуется {requiredSkill} Magery.");
                return false;
            }
            return true;
        }

        public override void GetProgressGain(int offsetID, out int minGain, out int maxGain)
        {
            int circleIndex = offsetID / 8;
            minGain = Math.Max(5, 30 - (circleIndex * 3));
            maxGain = Math.Max(10, 45 - (circleIndex * 5));
        }
    }
}