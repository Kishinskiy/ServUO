using System;
using System.Collections.Generic;
using Server;

namespace Server.Custom.MercenarySystem
{
    public enum MercenaryClass { Knight, Mage, Ranger }

    public class MercenaryProgression
    {
        public int Level { get; set; } = 1;
        public int Experience { get; set; } = 0;
        public int NextLevelExperience { get; set; } = 500;

        public void CheckLevelUp(Mobile merc, MercenaryClass mClass)
        {
            if (Level >= 10) return; 

            if (Experience >= NextLevelExperience)
            {
                Experience -= NextLevelExperience;
                Level++;
                NextLevelExperience = (int)(500 * Math.Pow(1.8, Level - 1));

                // ИСПРАВЛЕНО: Накладываем ХП через нативный и безошибочный StatMod на силу/выносливость! ⚙️🛠️🎉
                string modName = "MercLevelUpHP_" + Level;
                int hpBonus = (mClass == MercenaryClass.Knight) ? 50 : 25;
                
                // В ядре ServUO добавление специального StatMod автоматически расширяет HitsMax человека!
                merc.AddStatMod(new StatMod(StatType.Str, modName, hpBonus * 2, TimeSpan.Zero));

                merc.RawStr += (mClass == MercenaryClass.Knight) ? 15 : 5;
                merc.RawInt += (mClass == MercenaryClass.Mage) ? 15 : 5;
                merc.RawDex += (mClass == MercenaryClass.Ranger) ? 15 : 5;

                merc.Hits = merc.HitsMax;
                merc.Mana = merc.ManaMax;
                merc.Stam = merc.StamMax;

                foreach (Skill skill in merc.Skills)
                {
                    if (skill.Base > 0)
                    {
                        skill.Cap += 10.0;
                        skill.Base += 10.0;
                    }
                }

                merc.PlaySound(0x5B6); 
                merc.PublicOverheadMessage(Network.MessageType.Regular, 0x35, false, $"*[Уровень Повышен! Ранг: {GetRankTitle(Level)}]*");
            }
        }

        public static string GetRankTitle(int level)
        {
            switch (level)
            {
                case 1: return "Рекрут (Apprentice)";
                case 2: return "Послушник (Novice)";
                case 3: return "Ополченец (Footman)";
                case 4: return "Воин (Warrior)";
                case 5: return "Ветеран (Veteran)";
                case 6: return "Гвардеец (Guardsman)";
                case 7: return "Рыцарь (Knight)";
                case 8: return "Командор (Commander)";
                case 9: return "Чемпион (Champion)";
                case 10: return "Легенда (Hero)";
                default: return "Наёмник";
            }
        }
    }
}
