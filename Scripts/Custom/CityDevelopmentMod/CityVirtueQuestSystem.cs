using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Commands;
using Server.Engines.CityLoyalty;

namespace Server.Custom.CityDevelopment
{
    public static class CityVirtueQuestSystem
    {
        // Каноничное сопоставление городов и мантр ядра ServUO
        private static readonly Dictionary<City, Tuple<string, string>> VirtueMap = new Dictionary<City, Tuple<string, string>>()
        {
            { City.Britain,     new Tuple<string, string>("Compassion (Сострадание)", "МУ") },
            { City.Yew,         new Tuple<string, string>("Justice (Справедливость)", "БЕХ") },
            { City.Minoc,       new Tuple<string, string>("Sacrifice (Жертвенность)", "КАХ") },
            { City.Moonglow,    new Tuple<string, string>("Honesty (Честность)", "АХМ") },
            { City.Trinsic,     new Tuple<string, string>("Honor (Честь)", "СУММ") },
            { City.SkaraBrae,   new Tuple<string, string>("Spirituality (Духовность)", "ОМ") },
            { City.Jhelom,      new Tuple<string, string>("Valor (Доблесть)", "РА") },
            { City.NewMagincia, new Tuple<string, string>("Humility (Смирение)", "ЛУМ") }
        };

        public static void Initialize()
        {
            // Регистрируем короткую команду для игроков в черте города
            CommandSystem.Register("CityMantra", AccessLevel.Player, new CommandEventHandler(OnCityMantra_OnCommand));
        }

        private static void OnCityMantra_OnCommand(CommandEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            if (pm == null) return;

            Region reg = Region.Find(pm.Location, pm.Map);
            if (reg == null || string.IsNullOrEmpty(reg.Name))
            {
                pm.SendMessage(0x22, "Вы должны находиться в черте одного из городов Добродетелей!");
                return;
            }

            City currentCity = City.Britain;
            bool insideValidCity = false;

            foreach (City c in Enum.GetValues(typeof(City)))
            {
                string checkName = (c == City.NewMagincia) ? "magincia" : c.ToString().ToLower();
                if (reg.Name.ToLower().Contains(checkName))
                {
                    currentCity = c;
                    insideValidCity = true;
                    break;
                }
            }

            if (!insideValidCity)
            {
                pm.SendMessage(0x22, $"Регион '{reg.Name}' не связан с Великими Добродетелями.");
                return;
            }

            Tuple<string, string> virtueInfo = VirtueMap[currentCity];
            if (virtueInfo == null) return;

            // ИСПРАВЛЕНО: Безошибочно считываем уровень города из ТВОЕЙ базы данных CityDevelopmentMod! 🪙📜🎉
            CityProgress progress = CityDevelopmentMod.GetCityProfile(currentCity, pm.Map);
            int cityLevel = progress != null ? progress.Level : 1;

            pm.SendMessage(0x35, $"=== Торговая Гильдия города {currentCity} ===");

            // ТВОЁ ТЗ: Если город развит (Level >= 2) благодаря караванам — мантра открыта для всех! ✨
            if (cityLevel >= 2)
            {
                pm.SendMessage(0x58, $"Благодаря караванам торговцев, город процветает! Нам открылась мудрость {virtueInfo.Item1}.");
                pm.SendMessage(0x58, $"Древнее слово силы для активации местного Алтаря: \"{virtueInfo.Item2}\"");
                pm.SendMessage(0x59, "Подойдите к Алтарю вплотную и произнесите это слово в обычный чат игры.");
                
                Effects.SendLocationParticles(EffectItem.Create(pm.Location, pm.Map, TimeSpan.FromSeconds(0.5)), 0x376A, 9, 32, 5015);
            }
            else
            {
                pm.SendMessage(0x22, $"Город {currentCity} находится в упадке (Уровень 1). Жители забыли свои корни...");
                pm.SendMessage(0x22, $"Развивайте город и возите караваны Министра, чтобы восстановить наследие {virtueInfo.Item1} и узнать мантру!");
                pm.SendMessage(0x59, "💡 СОВЕТ: Сдавайте заказы Торговому Министру в этом регионе, чтобы поднять уровень города.");
            }
        }
    }
}
