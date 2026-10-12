using System;
using Server;
using Server.Items;

namespace Server.Custom.SpellStudySystem
{
    public class MageSchoolMysticism : BaseMagicSchool
    {
        public override string SchoolName => "Мистицизм Горгулий (Mysticism)";
        public override SkillName SupportSkill => SkillName.Focus;

        public override int StartSound => 0x5C3; // Каменный скрежет рун
        public override int SuccessSound => 0x5C5; // Сияние Горгулий

        public override bool MatchBook(Spellbook book)
        {
            return book.GetType().Name.Contains("Mysticism") || book.GetType().Name.Contains("Gargoyle");
        }

        public override bool CheckRequirements(Mobile from, Spellbook book, int spellID, int offsetID)
        {
            double skill = from.Skills[SkillName.Mysticism].Base;
            double req = Math.Min(100.0, 10.0 + (offsetID * 6.5));

            if (skill < req)
            {
                from.SendMessage(0x22, $"Ваш разум не может состыковаться с Эфиром Горгулий. Требуется {req} Mysticism.");
                return false;
            }
            return true;
        }
    }
}