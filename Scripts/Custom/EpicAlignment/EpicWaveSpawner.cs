using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Items;

namespace Server.Items
{
    public static class EpicWaveSpawner
    {
        // =========================================================================
        // НАСТРОЙКИ СУРВИВАЛ-ИСПЫТАНИЯ ХРАМА ХАОСА (TIME ATTACK) ⏱️
        // =========================================================================
        public static readonly int BaseWaveCount = 4;       
        public static readonly int MonstersPerPlayer = 5;    
        public static readonly int StaticGuardCount = 3;     
        public static readonly double DetectionRadius = 12.0; 
        public static readonly TimeSpan TimeBetweenWaves = TimeSpan.FromMinutes(15.0);
        
        public static readonly double ParagonChance = 0.15;      // Подняли шанс обычного Парагона до 15%!
        public static readonly double BossParagonChance = 0.25;  // Подняли шанс Босс-Парагона до 25%!

        public static void StartBattle(PlayerMobile pm, Point3D center, bool isLight)
        { SpawnWaveInstance(pm, center, isLight, 1, null); }

        private static void SpawnWaveInstance(PlayerMobile leader, Point3D center, bool isLight, int waveStep, Item fireEffect)
        {
            Map map = Map.Felucca;
            if (leader == null || leader.Deleted || !leader.Alive || leader.Map != map || leader.GetDistanceToSqrt(center) > 30.0)
            {
                if (fireEffect != null && !fireEffect.Deleted) fireEffect.Delete();
                Item failBeacon = new Item(0x1183) { Name = "Altar of Chaos", Movable = false, Visible = false };
                failBeacon.MoveToWorld(new Point3D(center.X, center.Y, center.Z + 2), map);
                failBeacon.PublicOverheadMessage(Network.MessageType.Regular, 0x22, false, "[Инициатор пал или покинул Храм! Ритуал Хаоса сброшен!]");
                Timer.DelayCall(TimeSpan.FromSeconds(8.0), () => failBeacon.Delete()); return;
            }

            if (waveStep == 1 && fireEffect == null)
            {
                IPooledEnumerable itemEnum = map.GetItemsInRange(center, 1);
                foreach (Item item in itemEnum) { if (item.ItemID == 0x3709) { fireEffect = item; break; } }
                itemEnum.Free();
            }

            if (waveStep <= BaseWaveCount)
            {
                string waveName = waveStep == 1 ? "ЗЕМЛЯ" : waveStep == 2 ? "ОГОНЬ" : waveStep == 3 ? "ВОДА" : "ВОЗДУХ";
                int playerCount = 0;
                IPooledEnumerable playerEnum = map.GetMobilesInRange(center, (int)DetectionRadius);
                foreach (Mobile m in playerEnum) { if (m.Player && m.Alive) playerCount++; }
                playerEnum.Free();

                if (playerCount < 1) playerCount = 1;
                int totalSpawn = (playerCount * MonstersPerPlayer) + StaticGuardCount;
                leader.SendMessage(0x22, $"[ВНИМАНИЕ] Врата Стихий содрогаются! Наступает ВОЛНА {waveStep} ИЗ {BaseWaveCount} — СТИХИЯ {waveName} ({totalSpawn} мобов)!");

                for (int i = 0; i < totalSpawn; i++)
                {
                    BaseCreature spawn;
                    // ИСПРАВЛЕНО: Интегрировали новых, сочных и интересных монстров под каждую полярность квеста!
                    if (isLight)
                    {
                        if (waveStep == 1) spawn = new Golem(); else if (waveStep == 2) spawn = new FireSteed();
                        else if (waveStep == 3) spawn = new Kraken(); else spawn = new Efreet();
                    }
                    else
                    {
                        if (waveStep == 1) spawn = new SkeletalKnight(); else if (waveStep == 2) spawn = new Succubus();
                        else if (waveStep == 3) spawn = new RottingCorpse(); else spawn = new ShadowIronElemental();
                    }

                    // УЛЬТИМАТИВНАЯ ЗАЩИТА: Намертво выставляем Карму и Сраву мобов в ноль, чтобы у лидера ничего не отлетало! 🛡️
                    spawn.Karma = 0; spawn.Fame = 0;

                    if (Utility.RandomDouble() < ParagonChance)
                    {
                        spawn.Name = $"Элитный Эфирный Страж {waveName} [Paragon]"; spawn.Hue = 1281;
                        spawn.HitsMaxSeed *= 3; spawn.Hits = spawn.HitsMaxSeed;
                        spawn.ActiveSpeed *= 0.8; spawn.SetDamage(spawn.DamageMin + 15, spawn.DamageMax + 25);
                    }
                    else
                    {
                        spawn.Name = $"Страж Стихии {waveName}"; spawn.Hue = isLight ? 2418 : 1175;
                    }

                    Point3D loc = new Point3D(center.X + Utility.RandomMinMax(-4, 4), center.Y + Utility.RandomMinMax(-4, 4), center.Z);
                    loc.Z = map.GetAverageZ(loc.X, loc.Y);
                    spawn.MoveToWorld(loc, map); spawn.Combatant = leader;
                }
                new EpicTimeAttackTimer(leader, center, isLight, waveStep + 1, fireEffect).Start();
            }
            else if (waveStep == BaseWaveCount + 1) { SpawnBoss(leader, center, isLight); }
        }

        private static void SpawnBoss(PlayerMobile leader, Point3D center, bool isLight)
        {
            Map map = Map.Felucca;
            leader.SendMessage(0x35, "Время испытания стихий истекло! Грядёт пробуждение Хранителя!");
            
            BaseCreature boss = isLight ? (BaseCreature)new AncientWyrm() : (BaseCreature)new Daemon();
            boss.HitsMaxSeed = 20000; boss.Hits = 20000; boss.SetDamage(45, 65);
            boss.Karma = 0; boss.Fame = 0; // Тотальное обнуление кармы Босса

            Container backpack = boss.Backpack;
            if (backpack == null) { backpack = new Backpack(); backpack.Movable = false; boss.AddItem(backpack); }

            if (Utility.RandomDouble() < BossParagonChance)
            {
                boss.Name = isLight ? "Золотой Судия [Эпический Paragon]" : "Древний Разрушитель [Эпический Paragon]";
                boss.Hue = 1281; boss.HitsMaxSeed *= 2; boss.Hits = boss.HitsMaxSeed; boss.ActiveSpeed *= 0.85;
                
                backpack.DropItem(new Gold(Utility.RandomMinMax(5000, 8000))); // Подняли золото до 8к!
                for (int i = 0; i < 6; i++) // Больше предметов рейду помощников
                {
                    Item magicItem = Loot.RandomArmorOrShieldOrJewelry();
                    if (magicItem != null) { BaseRunicTool.ApplyAttributesTo(magicItem, 5, 85, 100); backpack.DropItem(magicItem); }
                }
            }
            else
            {
                boss.Name = isLight ? "Небесный Судия [QUEST BOSS]" : "Страж Бездны [QUEST BOSS]"; boss.Hue = isLight ? 2418 : 1175;
                
                backpack.DropItem(new Gold(Utility.RandomMinMax(2500, 4000)));
                for (int i = 0; i < 4; i++)
                {
                    Item magicItem = Loot.RandomArmorOrShieldOrJewelry();
                    if (magicItem != null) { BaseRunicTool.ApplyAttributesTo(magicItem, 4, 70, 95); backpack.DropItem(magicItem); }
                }
            }

            Point3D loc = new Point3D(center.X, center.Y - 2, center.Z); loc.Z = map.GetAverageZ(loc.X, loc.Y);
            boss.MoveToWorld(loc, map); boss.Combatant = leader;
            World.Broadcast(isLight ? 0x35 : 0x22, true, $"[Квест] Время вышло! В Храме Хаоса из полыхающей чаши восстал великий {boss.Name}!");
            
            EpicRewardSystem.MonitorBoss(boss, leader, isLight ? Server.Custom.EpicAlignment.EpicRank.Avatar : Server.Custom.EpicAlignment.EpicRank.HandOfMondain);
        }

        private class EpicTimeAttackTimer : Timer
        {
            private PlayerMobile m_Leader; Point3D m_Center; bool m_IsLight; int m_WaveStep; Item m_FireEffect;
            public EpicTimeAttackTimer(PlayerMobile leader, Point3D center, bool isLight, int waveStep, Item fire) : base(EpicWaveSpawner.TimeBetweenWaves) 
            { m_Leader = leader; m_Center = center; m_IsLight = isLight; m_WaveStep = waveStep; m_FireEffect = fire; }
            protected override void OnTick() { EpicWaveSpawner.SpawnWaveInstance(m_Leader, m_Center, m_IsLight, m_WaveStep, m_FireEffect); }
        }
    }
}
