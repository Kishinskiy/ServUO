using System;
using Server.Mobiles;
using Server.Items;

namespace Server.Custom.HunterQuest
{
    public class HunterQuestSystem
    {
        // Оставляем пустой метод, чтобы сервер не ругался, если его кто-то вызывает
        public static void Initialize()
        {
        }

        // Этот метод мы будем вызывать напрямую из самого монстра
        public static void CheckQuestProgress(BaseCreature killed)
        {
            if (killed == null)
                return;

            // Определяем, кто нанес последний/смертельный урон
            Mobile killer = killed.LastKiller;

            // Если убийцы нет в LastKiller, ищем через список нанесших урон (DamageEntries)
            if (killer == null && killed.DamageEntries != null)
            {
                foreach (DamageEntry de in killed.DamageEntries)
                {
                    if (de.Damager != null && de.Damager.Player)
                    {
                        killer = de.Damager;
                        break;
                    }
                }
            }

            if (killer == null)
                return;

            // Если убил питомец/сумон, перенаправляем награду хозяину-игроку
            if (killer is BaseCreature && ((BaseCreature)killer).Controlled && ((BaseCreature)killer).ControlMaster != null)
                killer = ((BaseCreature)killer).ControlMaster;

            if (killer.Backpack == null)
                return;

            // Ищем контракт в сумке игрока
            Item contractItem = killer.Backpack.FindItemByType(typeof(HunterKillQuestToken));
            if (contractItem is HunterKillQuestToken)
            {
                HunterKillQuestToken contract = (HunterKillQuestToken)contractItem;

                // Проверяем, совпадает ли C# класс убитого существа с целью квеста
                if (contract.CurrentKills < contract.RequiredKills && 
                    string.Equals(killed.GetType().Name, contract.TargetMonster, StringComparison.OrdinalIgnoreCase))
                {
                    contract.CurrentKills++;
                    killer.SendMessage(0x35, $"Quest Progress: {contract.Name} ({contract.CurrentKills}/{contract.RequiredKills})");
                }
            }
        }
    }
}
