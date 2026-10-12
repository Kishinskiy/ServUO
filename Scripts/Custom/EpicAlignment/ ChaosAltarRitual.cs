using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Commands; 
using Server.Custom.EpicAlignment;
using Server.Custom.CityDevelopment;

namespace Server.Custom.ChaosAltar
{
    public static class ChaosAltarRitual
    {
        private static Mobile m_AvatarContender = null;
        private static Mobile m_MondainContender = null;
        private static bool m_CataclysmTriggered = false;

        // Координаты Алтаря Хаоса на фракционном острове
        private static readonly Point3D AltarLocation = new Point3D(1234, 567, 8); 

        public static void Initialize()
        {
            EventSink.Speech += new SpeechEventHandler(OnPlayerSpeech);
        }

        private static void OnPlayerSpeech(SpeechEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            if (pm == null || !pm.Alive || pm.Deleted || pm.AccessLevel > AccessLevel.Player)
                return;

            if (!pm.InRange(AltarLocation, 3) || pm.Map != Map.Felucca)
                return;

            string speech = e.Speech.ToLower();
            bool isAvatarCall = speech.Contains("я аватар");
            bool isMondainCall = speech.Contains("я мондейн") || speech.Contains("рука мондейна");

            if (isAvatarCall || isMondainCall)
            {
                e.Handled = true; 

                Container pack = pm.Backpack;
                if (pack == null) return;

                Item shards = pack.FindItemByType(ScriptCompiler.FindTypeByName("BossShard"));
                if (shards == null || shards.Amount < 5)
                {
                    pm.PlaySound(0x5C3);
                    pm.SendMessage(0x22, "[Алтарь]: Древние силы безмолвствуют. У вас нет 5 Осколков Боссов для призыва силы!");
                    return;
                }

                if (shards.Amount == 5) shards.Delete();
                else shards.Amount -= 5;

                if (isAvatarCall)
                {
                    m_AvatarContender = pm;
                    pm.SendMessage(0x58, "[Алтарь]: Светлые потоки Аватара активированы! Удерживайте позицию 4 секунды!");
                    Effects.PlaySound(pm.Location, pm.Map, 0x11C); 
                }
                else if (isMondainCall)
                {
                    m_MondainContender = pm;
                    pm.SendMessage(0x22, "[Алтарь]: Темные потоки Мондейна активированы! Удерживайте позицию 4 секунды!");
                    Effects.PlaySound(pm.Location, pm.Map, 0x22F); 
                }

                if (m_AvatarContender != null && m_MondainContender != null && !m_CataclysmTriggered)
                {
                    m_CataclysmTriggered = true;
                    TriggerChaosCataclysm(pm.Map);
                }

                Mobile currentContender = pm;
                bool modeAvatar = isAvatarCall;

                Timer.DelayCall(TimeSpan.FromSeconds(4.0), () =>
                {
                    if (currentContender != null && !currentContender.Deleted && currentContender.Alive && currentContender.InRange(AltarLocation, 3))
                    {
                        if (modeAvatar)
                        {
                            EpicAlignmentSystem.SetRank(currentContender, EpicRank.Avatar);
                            currentContender.PlaySound(0x5B2);
                            currentContender.SendMessage(0x35, "[ЭПИЧЕСКИЙ ТРИУМФ]: Вы выжили в катаклизме и стали АВАТАРОМ!");
                            CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x35, $"[Пророчество]: Потоки Хаоса содрогнулись! Персонаж {currentContender.Name} выжил в великой битве и принял титул АВАТАРА Света!");
                        }
                        else
                        {
                            EpicAlignmentSystem.SetRank(currentContender, EpicRank.HandOfMondain);
                            currentContender.PlaySound(0x5B2);
                            currentContender.SendMessage(0x22, "[ЭПИЧЕСКИЙ ТРИУМФ]: Вы выжили в катаклизме и стали РУКОЙ МОНДЕЙНА!");
                            CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x22, $"[Пророчество]: Тьма поглотила Алтарь! Персонаж {currentContender.Name} выжил в катаклизме и принял титул РУКИ МОНДЕЙНА!");
                        }
                    }

                    if (modeAvatar) m_AvatarContender = null;
                    else m_MondainContender = null;
                    
                    if (m_AvatarContender == null && m_MondainContender == null) m_CataclysmTriggered = false;
                });
            }
        }

        private static void TriggerChaosCataclysm(Map map)
        {
            CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x22, "[КАТАКЛИЗМ]: Аватар и Мондейн столкнулись у Алтаря Хаоса! Пространство разорвано, призванные армии Света и Тьмы сошлись в смертельной схватке!");

            Effects.PlaySound(AltarLocation, map, 0x22F);

            Server.Factions.Town townBritain = Server.Factions.Town.Towns.Find(t => t.ToString().ToLower().Contains("britain"));
            Server.Factions.Town townUmbra = Server.Factions.Town.Towns.Find(t => t.ToString().ToLower().Contains("umbra")) ?? Server.Factions.Town.Towns.Find(t => t.ToString().ToLower().Contains("moonglow"));

            // 1. СПАВНИМ СВЕТЛЫХ ПАЛАДИНОВ (Британия)
            for (int i = 0; i < 4; i++)
            {
                DevelopmentCityGuard lightGuard = new DevelopmentCityGuard(townBritain, 5);
                Point3D spawnLoc = GetSafeIslandSpawnOffset(map);
                
                lightGuard.MoveToWorld(spawnLoc, map);
                lightGuard.Warmode = true;
            }

            // 2. СПАВНИМ ТЕМНЫХ ВЛАДЫК ПРАХА (Умбра / Скрытый Некро-Лор)
            for (int i = 0; i < 4; i++)
            {
                DevelopmentCityGuard darkGuard = new DevelopmentCityGuard(townUmbra, 5);
                if (darkGuard.CityTown != null && !darkGuard.CityTown.ToString().ToLower().Contains("umbra"))
                {
                    darkGuard.Name = "Владыка праха";
                    darkGuard.Title = ", центурион бездны";
                }

                Point3D spawnLoc = GetSafeIslandSpawnOffset(map);
                darkGuard.MoveToWorld(spawnLoc, map);
                darkGuard.Warmode = true;
            }
        }

        // 🔥 ИСПРАВЛЕНО: УМНЫЙ ОСТРОВНОЙ СКАНЕР КООРДИНАТ С ЗАЩИТОЙ ОТ ПАДЕНИЯ В ВОДУ 🎉⚙️🛠
        private static Point3D GetSafeIslandSpawnOffset(Map map)
        {
            // Расширяем радиус боевых действий до 12 клеток вокруг алтаря
            int maxRadius = 12;

            for (int attempt = 0; attempt < 30; attempt++)
            {
                int rx = AltarLocation.X + Utility.RandomMinMax(-maxRadius, maxRadius);
                int ry = AltarLocation.Y + Utility.RandomMinMax(-maxRadius, maxRadius);
                int rz = map.GetAverageZ(rx, ry);

                Point3D testLoc = new Point3D(rx, ry, rz);

                // Нативная проверка ServUO: CanSpawnMobile проверяет, что тайл проходим 
                // и там нет глубокой воды, скал или непроходимых препятствий!
                if (map.CanSpawnMobile(testLoc) && map.CanFit(testLoc, 16, false, false))
                {
                    return testLoc; // Нашли идеальный сухой кусок земли на острове!
                }
            }

            // Если за 30 попыток рандом выдал сплошную воду вокруг острова — 
            // спавним моба вплотную к алтарю в качестве безопасного фолбэка
            return AltarLocation;
        }
    }
}
