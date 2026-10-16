using System;
using Server;
using Server.Items;

namespace Server.Custom.SpellStudySystem
{
    public class MageSchoolChivalry : BaseMagicSchool
    {
        public override string SchoolName => "Паладинство (Chivalry)";
        public override SkillName StudySkill => SkillName.Chivalry;
        public override SkillName SupportSkill => SkillName.Focus;

        public override int StartSound => 0x5C9; // Святой гул
        public override int SuccessSound => 0xF7; // Ангельский хор

        public override bool MatchBook(Spellbook book)
        {
            return book.SpellbookType == SpellbookType.Paladin;
        }

        public override bool CheckRequirements(Mobile from, Spellbook book, int spellID, int offsetID)
        {
            double skill = from.Skills[SkillName.Chivalry].Base;
            double req = Math.Min(95.0, 10.0 + (offsetID * 8.5));

            if (skill < req)
            {
                from.SendMessage(0x22, $"Ваша святая вера слишком слаба для этой клятвы. Требуется {req} Chivalry.");
                return false;
            }
            return true;
        }
    }
}
