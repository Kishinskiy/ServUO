using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Server;
using Server.Mobiles;

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

                string modName = "MercLevelUpHP_" + Level;
                int hpBonus = (mClass == MercenaryClass.Knight) ? 50 : 25;
                
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

        // 🔥 ДОБАВЛЕНО: Безопасный автоматический спавн Капитанов по всем городам мира! 🏙️⚔️🎉
        public static void AutoSpawnCaptains()
        {
            Console.WriteLine("[MercenarySystem] Проверка присутствия Капитанов Наёмников в мире...");
            
            // Собираем всех Торговых Министров ядра ServUO, которые уже стоят в городах
            List<Mobile> ministers = World.Mobiles.Values.Where(m => m is Server.Engines.CityLoyalty.TradeMinister && m.Map != null && m.Map != Map.Internal).ToList();
            
            foreach (Mobile minister in ministers)
            {
                // Проверяем, стоит ли уже Капитан в радиусе 6 тайлов от этого Министра (защита от дублирования при рестартах)
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

                // Если Капитана в этом городе ещё нет — спавним его рядышком!
                if (!captainExists)
                {
                    MercenaryCaptain captain = new MercenaryCaptain();
                    // Смещаем на 2 тайла на восток от министра, чтобы они не стояли друг в друге
                    Point3D spawnLoc = new Point3D(minister.X + 2, minister.Y, minister.Z);
                    
                    // Проверяем проходимость точки, если там стена — спавним прямо на министра
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
