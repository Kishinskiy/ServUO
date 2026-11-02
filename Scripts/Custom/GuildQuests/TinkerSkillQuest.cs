using System;
using System.Collections.Generic;
using Server;
using Server.Gumps;
using Server.Network;
using Server.Mobiles;
using Server.Items;

namespace Server.Custom.GuildQuests
{
    public static class TinkerSkillQuest
    {
        public static bool OnTinkerGuildmasterClick(PlayerMobile pm, Mobile master)
        {
            if (pm == null || master == null) return false;
            if (GuildmasterSkillQuest.OnGuildmasterClick(pm, master)) return true;

            Skill tinkerSkill = pm.Skills[SkillName.Tinkering];

            // 1. Проверяем, принёс ли инженер трофей на сдачу
            Item core = pm.Backpack.FindItemByType(typeof(ClockworkCore));

            if (core != null && !core.Deleted && SkillCapBreakSystem.CanCompleteQuest100(pm, SkillName.Tinkering))
            {
                core.Delete();
                
                master.PlaySound(0x241); // Звук взводящегося часового механизма
                master.PublicOverheadMessage(MessageType.Regular, 0x35, false, "*Тщательно изучает микросхемы ядра лупой*");
                
                // Вызываем наш кастомный класс прорыва лимитов
                SkillCapBreakSystem.GrantQuest100Unlock(pm, SkillName.Tinkering);

                // 🔥 ИСПРАВЛЕНО: Заменено с несуществующего 'skill.Name' на штатный 'tinkerSkill.Name' 🎉⚙️
                pm.SendGump(new TinkerQuestDetailsGump("Испытание Механики Завершено!", $"Изумительно! Твои пальцы смогли извлечь это ядро. Лимит твоего навыка {tinkerSkill.Name} успешно расширен до 100.0!"));
                return true;
            }

            // 2. ФАЗОВАЯ ПРОВЕРКА: Если навык уже выше 81.0 (Ветеран)
            if (tinkerSkill.Base >= 100.0)
            {
                master.Say("Приветствую тебя, великий изобретатель. Твои чертежи и механизмы уже вошли в историю нашей Гильдии. Твои лимиты открыты, и новые испытания тебе не требуются.");
                return true;
            }

            // 3. ФАЗОВАЯ ПРОВЕРКА: Если навык готов к прорыву (Ровно 80.0)
            if (SkillCapBreakSystem.CanCompleteQuest100(pm, SkillName.Tinkering))
            {
                pm.CloseGump(typeof(TinkerQuestDetailsGump));
                pm.SendGump(new TinkerQuestDetailsGump("Испытание Изобретателя (Tinkering 60.0+)", "Мастер, чтобы открыть Tinkering выше 60.0, найди древнего механического голема, отключи его и принеси мне Хронометрическое Ядро."));
                return true;
            }

            // 4. ФАЗОВАЯ ПРОВЕРКА: Если навык меньше 80.0 (Подмастерье)
            if (tinkerSkill.Base < 60.0)
            {
                master.Say("Приветствую, жестянщик. Продолжай практиковаться, купи обучение у тренера и возвращайся, когда Tinkering достигнет 60.0.");
                return true;
            }

            master.Say("Твоё испытание уже пройдено. Продолжай развивать Tinkering до 100.0.");
            return true;
        }
    }

    // =========================================================================
    // 🎨 ГАМП КВЕСТОВ ИНЖЕНЕРОВ
    // =========================================================================
    public class TinkerQuestDetailsGump : Gump
    {
        public TinkerQuestDetailsGump(string title, string text) : base(120, 120)
        {
            AddPage(0); 
            AddBackground(0, 0, 420, 320, 0x13BE); // Кузнечный/инженерный металлический интерфейс [из предыдущих ответов]
            AddAlphaRegion(15, 15, 390, 290);

            AddLabel(40, 25, 0x35, title); 
            AddImageTiled(30, 50, 360, 2, 0x2424);

            AddHtml(40, 70, 340, 180, $"<BASEFONT COLOR=#FFFFFF>{text}</BASEFONT>", false, true);

            AddButton(170, 265, 0x15E1, 0x15E5, 0, GumpButtonType.Reply, 0); 
            AddLabel(202, 267, 0x35, "За работу!");
        }
    }
}
