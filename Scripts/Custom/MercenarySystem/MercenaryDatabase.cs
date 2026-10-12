using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Server;
using Server.Mobiles;

namespace Server.Custom.MercenarySystem
{
    // Расширяем енам всеми новыми классами и расами
    public enum MercenaryClass 
    { 
        Knight, Mage, Ranger, 
        Bard, Thief, Paladin, Necromancer, Samurai,
        ElfMage, GargoyleWarrior 
    }

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

                string modName = "MercLevelUpHP_" + Level;
                
                // Танкам и паладинам даем больше живучести
                int hpBonus = 25;
                if (mClass == MercenaryClass.Knight || mClass == MercenaryClass.Paladin || mClass == MercenaryClass.GargoyleWarrior)
                    hpBonus = 50;
                
                merc.AddStatMod(new StatMod(StatType.Str, modName, hpBonus * 2, TimeSpan.Zero));

                // Распределяем прирост характеристик в зависимости от архетипа
                if (mClass == MercenaryClass.Knight || mClass == MercenaryClass.Samurai || mClass == MercenaryClass.GargoyleWarrior)
                    merc.RawStr += 15;
                else if (mClass == MercenaryClass.Mage || mClass == MercenaryClass.Necromancer || mClass == MercenaryClass.ElfMage)
                    merc.RawInt += 15;
                else if (mClass == MercenaryClass.Ranger || mClass == MercenaryClass.Thief)
                    merc.RawDex += 15;
                else if (mClass == MercenaryClass.Paladin || mClass == MercenaryClass.Bard)
                {
                    merc.RawStr += 8;
                    merc.RawInt += 8;
                }

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

        // Автоспавн Капитанов по городам
        public static void AutoSpawnCaptains()
        {
            Console.WriteLine("[MercenarySystem] Проверка присутствия Капитанов Наёмников в мире...");
            
            List<Mobile> ministers = World.Mobiles.Values.Where(m => m is Server.Engines.CityLoyalty.TradeMinister && m.Map != null && m.Map != Map.Internal).ToList();
            
            foreach (Mobile minister in ministers)
            {
                bool captainExists = false;
                IPooledEnumerable eable = minister.Map.GetMobilesInRange(minister.Location, 6);
                foreach (Mobile m in eable)
                {
                    if (m is MercenaryCaptain)
                    {
                        captainExists = true;
                        break;
                    }
                }
                eable.Free();

                if (!captainExists)
                {
                    MercenaryCaptain captain = new MercenaryCaptain();
                    Point3D spawnLoc = new Point3D(minister.X + 2, minister.Y, minister.Z);
                    
                    if (!minister.Map.CanFit(spawnLoc.X, spawnLoc.Y, spawnLoc.Z, 16, false, false))
                        spawnLoc = minister.Location;

                    captain.MoveToWorld(spawnLoc, minister.Map);
                    Console.WriteLine($"[MercenarySystem] Spawn: Mercenary Captain spawned in city {minister.Region?.Name} ({minister.Map.Name})");
                }
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
