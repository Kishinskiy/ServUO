using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Custom.CityDevelopment;

namespace Server.Custom.CityPolitics
{
    public static class CityGovernorElectionMod
    {
        // 🔥 АБСОЛЮТНО АВТОНОМНЫЙ МЕТОД: Полностью очищен от нативных вызовов ядра ServUO! 🎉⚙️🛠️
        public static bool CheckCandidateEligibility(Mobile from, Server.Engines.CityLoyalty.City city)
        {
            if (from == null || from.Deleted) return false;

            // 1. Проверка на убийцу (Красный статус персонажа)
            if (from.Kills > 0 || from.Criminal)
            {
                from.SendMessage(0x22, "Криминальные элементы и убийцы не допускаются к выборам в городской совет!");
                return false;
            }

            // 2. БРОНИРОВАННЫЙ ЭКОНОМИЧЕСКИЙ ЦЕНЗ: Проверяем уровень развития города в твоем моде!
            // Выборы открываются только в городах, которые игроки развили до 3-го уровня и выше!
            CityProgress progress = CityDevelopmentMod.GetCityProfile(city, from.Map);
            
            if (progress == null || progress.Level < 3)
            {
                from.SendMessage(0x22, $"Этот город еще слишком слаб! Выборы в Губернаторы открываются только на 3-м уровне развития города.");
                if (progress != null)
                    from.SendMessage(0x22, $"Текущий уровень города: {progress.Level}/5. Развивайте город через караваны и доски объявлений!");
                return false;
            }

            // 3. Проверка финансового залога (100к золотых избирательный залог в бюджет города)
            long electionFee = 100000; 

            if (from.Backpack == null || from.Backpack.ConsumeTotal(typeof(Gold), (int)electionFee) == false)
            {
                // Если наличных нет в сумке — списываем напрямую со счета в банке
                if (Banker.Withdraw(from, (int)electionFee) == false)
                {
                    from.SendMessage(0x22, $"Для регистрации вашей кандидатуры требуется внести избирательный залог в размере {electionFee} золотых монет!");
                    return false;
                }
            }

            // Зачисляем залог кандидата прямо в бюджет города на развитие! 🪙🏛️📈
            progress.Treasury += electionFee;

            from.PlaySound(0x57); // Звук ссыпания золотых монет
            string globalMsg = $"[Политика]: Персонаж {from.Name} официально зарегистрирован кандидатом в Губернаторы города {city} (Уровень города: {progress.Level})!";
            Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x35, globalMsg);

            return true;
        }
    }
}
