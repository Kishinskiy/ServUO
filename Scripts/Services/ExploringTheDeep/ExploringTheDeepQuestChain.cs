using System;

namespace Server.Engines.Quests
{
    public enum ExploringTheDeepQuestChain
    {
        None,
        HeplerPaulson,
        HeplerPaulsonComplete,
        CusteauPerronHouse,
        CusteauPerron,
        Sorcerers,
        CollectTheComponent,
        CollectTheComponentComplete
    }

    public class ExploringTheDeepBaseChain
    {
        public Type CurrentQuest { get; set; }

        public ExploringTheDeepBaseChain(Type currentQuest, Type quester)
        {
            CurrentQuest = currentQuest;
        }
    }
}
