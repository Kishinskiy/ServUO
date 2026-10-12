using System;
using Server;
using Server.Items;

namespace Server.Custom.SpellStudySystem
{
    public class MageSchoolBard : BaseMagicSchool
    {
        public override string SchoolName => "Песнопения Бардов (Bardic Songs)";
        public override SkillName SupportSkill => SkillName.Musicianship;

        public override int StartSound => 0x383; // Настройка лютни
        public override int SuccessSound => 0x386; // Красивая арфовая партитура

        public override bool MatchBook(Spellbook book)
        {
            return book.GetType().Name.Contains("Bard") || book.GetType().Name.Contains("BookOfDiscord");
        }

        public override bool CheckRequirements(Mobile from, Spellbook book, int spellID, int offsetID)
        {
            double skill = from.Skills[SkillName.Discordance].Base; // Базовый бардовский навык
            double req = Math.Min(100.0, 15.0 + (offsetID * 12.0));

            if (skill < req)
            {
                from.SendMessage(0x22, $"Ваш слух недостаточно чуток, чтобы уловить эти ноты. Требуется {req} Discordance.");
                return false;
            }
            return true;
        }
    }
}