using System;
using Server;
using Server.Items;

namespace Server.Custom.SpellStudySystem
{
    public class MageSchoolNecromancy : BaseMagicSchool
    {
        public override string SchoolName => "Некромантия (Necromancy)";
        public override SkillName StudySkill => SkillName.Necromancy;
        public override SkillName SupportSkill => SkillName.SpiritSpeak;

        public override int StartSound => 0x5A1; // Замогильный скрежет страниц
        public override int SuccessSound => 0x1D0; // Шепот духов при успехе
        public override int FailSound => 0x1FB; // Угасание душ при физзле

        public override string FailMessage => "Тёмная энергия вырвалась из-под контроля! Манускрипт рассыпался в прах.";
        public override string CompleteMessage => "Запретные знания покорились вам! Заклинание навсегда внесено в Некрономикон!";

        public override bool MatchBook(Spellbook book)
        {
            // Бронированная проверка под любые ревизии ServUO
            return book.SpellbookType == SpellbookType.Necromancer || book.GetType().Name.Contains("Necro");
        }

        public override bool CheckRequirements(Mobile from, Spellbook book, int spellID, int offsetID)
        {
            double necroskill = from.Skills[StudySkill].Base;
            double reqNecro;

            switch (offsetID)
            {
                case 0: reqNecro = 40.0; break; // Animate Dead
                case 1: reqNecro = 20.0; break; // Blood Oath
                case 2: reqNecro = 20.0; break; // Corpse Skin
                case 3: reqNecro = 0.0; break; // Curse Weapon
                case 4: reqNecro = 20.0; break; // Evil Omen
                case 5: reqNecro = 40.0; break; // Horrific Beast
                case 6: reqNecro = 70.0; break; // Lich Form
                case 7: reqNecro = 30.0; break; // Mind Rot
                case 8: reqNecro = 20.0; break; // Pain Spike
                case 9: reqNecro = 50.0; break; // Poison Strike
                case 10: reqNecro = 65.0; break; // Strangle
                case 11: reqNecro = 30.0; break; // Summon Familiar
                case 12: reqNecro = 99.0; break; // Vampiric Embrace
                case 13: reqNecro = 80.0; break; // Vengeful Spirit
                case 14: reqNecro = 60.0; break; // Wither
                case 15: reqNecro = 20.0; break; // Wraith Form
                case 16:
                    if (book.BookCount <= 16)
                    {
                        from.SendMessage(0x22, "Это заклинание недоступно в вашей версии некромантии.");
                        return false;
                    }
                    reqNecro = 80.0; // Exorcism
                    break;
                default:
                    from.SendMessage(0x22, "Неверный номер заклинания в некромантии.");
                    return false;
            }

            if (necroskill < reqNecro)
            {
                from.SendMessage(0x22, $"Древние манускрипты мёртвых не поддаются вам. Требуется {reqNecro} Necromancy.");
                return false;
            }
            return true;
        }
    }
}