using System;
using Server;
using Server.Items;

namespace Server.Custom.SpellStudySystem
{
    public class MageSchoolSpellweaving : BaseMagicSchool
    {
        public override string SchoolName => "Ткачество Чар Эльфов (Spellweaving)";
        public override SkillName StudySkill => SkillName.Spellweaving;
        public override SkillName SupportSkill => SkillName.Focus;

        public override int StartSound => 0x4B0; // Звон кристаллов
        public override int SuccessSound => 0x4B2; // Волшебный всплеск энергии

        public override bool MatchBook(Spellbook book)
        {
            return book.SpellbookType == SpellbookType.Arcanist;
        }

        public override bool CheckRequirements(Mobile from, Spellbook book, int spellID, int offsetID)
        {
            double skill = from.Skills[SkillName.Spellweaving].Base;
            double req = Math.Min(100.0, 10.0 + (offsetID * 6.0));

            if (skill < req)
            {
                from.SendMessage(0x22, $"Эфирные нити природы рвутся в ваших руках. Требуется {req} Spellweaving.");
                return false;
            }
            return true;
        }
    }
}