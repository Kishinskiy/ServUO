using System;
using System.Collections.Generic;
using Server.Items;
using Server.ContextMenus;

namespace Server.Mobiles
{
    public class HunterQuestGiver : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();
        protected override List<SBInfo> SBInfos => m_SBInfos;
        public override void InitSBInfo() { } // Он ничего не продает через стандартное меню

        // Пулл монстров для квеста: { "Имя_Класса_C#", "Красивое отображаемое имя" }
        private static readonly Dictionary<string, string> QuestPool = new Dictionary<string, string>
        {
            { "Mongbat", "Mongbats" },
            { "TimberWolf", "Timber Wolves" },
            { "Orc", "Orcs" },
            { "Lizardman", "Lizardmen" },
            { "Ratman", "Ratmen" }
        };

        [Constructable]
        public HunterQuestGiver() : base("the hunter")
        {
            Name = "Artemis";
            Title = "the Grand Hunter";
        }

        public HunterQuestGiver(Serial serial) : base(serial)
        {
        }

        // Клик по NPC двумя щелчками (Общение)
        public override void OnDoubleClick(Mobile from)
        {
            if (!from.Alive || !from.InRange(this, 3))
                return;

            Custom.HunterQuest.HunterKillQuestToken contract = from.Backpack?.FindItemByType(typeof(Custom.HunterQuest.HunterKillQuestToken)) as Custom.HunterQuest.HunterKillQuestToken;

            if (contract == null)
            {
                // Выдаем новый случайный квест
                AssignRandomQuest(from);
            }
            else if (contract.CurrentKills >= contract.RequiredKills)
            {
                // Сдаем выполненный квест
                CompleteQuest(from, contract);
            }
            else
            {
                // Квест еще в процессе
                SayTo(from, "You still have an active contract. Return to me when your hunt is complete.");
            }
        }

        private void AssignRandomQuest(Mobile to)
        {
            if (to.Backpack == null) return;

            // Выбираем случайного монстра из нашего словаря
            List<string> keys = new List<string>(QuestPool.Keys);
            string randomClass = keys[Utility.Random(keys.Count)];
            string displayName = QuestPool[randomClass];

            // Вариативность количества: от 5 до 15 штук
            int count = Utility.RandomMinMax(5, 15);
            // Вариативность награды: например, 200 золотых за каждого моба
            int rewardGold = count * 200;

            var newToken = new Custom.HunterQuest.HunterKillQuestToken(randomClass, count, rewardGold);
            newToken.Name = $"Hunter's Contract: {displayName}";

            to.Backpack.DropItem(newToken);
            
            SayTo(to, $"Greetings! I have a task for you. Take this contract and bring me peace by hunting down {count} {displayName}.");
        }

        private void CompleteQuest(Mobile to, Custom.HunterQuest.HunterKillQuestToken contract)
        {
            contract.Delete(); // Удаляем выполненный свиток
            
            int reward = contract.GoldReward;
            to.Backpack.DropItem(new Gold(reward));

            SayTo(to, $"Excellent work, hunter! Here is your reward of {reward} gold pieces.");
            
            // Сюда же можно прописать выдачу кожи/шкур, если нужно:
            // to.Backpack.DropItem(new PackagedHide(10)); 
        }

        public override void Serialize(GenericWriter writer) => base.Serialize(writer);
        public override void Deserialize(GenericReader reader) => base.Deserialize(reader);
    }
}
