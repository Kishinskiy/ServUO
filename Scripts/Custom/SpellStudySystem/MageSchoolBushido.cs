using System;
using Server;
using Server.Items;

namespace Server.Custom.SpellStudySystem
{
    public class MageSchoolBushido : BaseMagicSchool
    {
        public override string SchoolName => "Путь Самурая (Bushido)";
        public override SkillName StudySkill => SkillName.Bushido;
        public override SkillName SupportSkill => SkillName.Anatomy;

        public override int StartSound => 0x54; // Звук вытаскивания катаны
        public override int SuccessSound => 0x3C4; // Чистый триумфальный джингл

        public override bool MatchBook(Spellbook book)
        {
            return book.SpellbookType == SpellbookType.Samurai;
        }

        public override bool CheckRequirements(Mobile from, Spellbook book, int spellID, int offsetID)
        {
            double skill = from.Skills[SkillName.Bushido].Base;
            double req = Math.Min(95.0, 15.0 + (offsetID * 15.0));

            if (skill < req)
            {
                from.SendMessage(0x22, $"Кодекс чести скрыт от вас. Требуется {req} Bushido для осмысления этой техники.");
                return false;
            }
            return true;
        }
    }
}