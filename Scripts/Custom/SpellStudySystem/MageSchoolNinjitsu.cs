using System;
using Server;
using Server.Items;

namespace Server.Custom.SpellStudySystem
{
    public class MageSchoolNinjitsu : BaseMagicSchool
    {
        public override string SchoolName => "Тайные Искусства Ниндзя (Ninjitsu)";
        public override SkillName SupportSkill => SkillName.Stealth;

        public override int StartSound => 0x510; // Шелест дымовой шашки
        public override int SuccessSound => 0x512; // Скрытный глухой удар

        public override bool MatchBook(Spellbook book)
        {
            return book.GetType().Name.Contains("Ninjitsu") || book.GetType().Name.Contains("Ninja");
        }

        public override bool CheckRequirements(Mobile from, Spellbook book, int spellID, int offsetID)
        {
            double skill = from.Skills[SkillName.Ninjitsu].Base;
            double req = Math.Min(95.0, 15.0 + (offsetID * 15.0));

            if (skill < req)
            {
                from.SendMessage(0x22, $"Ваши движения слишком неуклюжи. Требуется {req} Ninjitsu.");
                return false;
            }
            return true;
        }
    }
}