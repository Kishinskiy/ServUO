using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Custom.EpicAlignment;

namespace Server.Items
{
    public static class EpicWaveSpawner
    {
        public static readonly int BaseWaveCount = 4;       
        public static readonly int MonstersPerPlayer = 5;    
        public static readonly int StaticGuardCount = 3;     
        public static readonly double DetectionRadius = 12.0; 
        public static readonly TimeSpan TimeBetweenWaves = TimeSpan.FromMinutes(15.0);
        
        public static readonly double ParagonChance = 0.15;      
        public static readonly double BossParagonChance = 0.25;  

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
                
                string sideName = isLight ? "ТЬМЫ И СКВЕРНЫ" : "СВЕТА И ДОБРОДЕТЕЛИ";
                leader.SendMessage(0x22, $"[ВНИМАНИЕ] Натиск {sideName}! Наступает ВОЛНА {waveStep} ИЗ {BaseWaveCount}!");

                for (int i = 0; i < totalSpawn; i++)
                {
                    BaseCreature spawn;
                    if (isLight)
                    {
                        if (waveStep == 1) spawn = new Gargoyle(); else if (waveStep == 2) spawn = new Succubus();
                        else if (waveStep == 3) spawn = new LichLord(); else spawn = new Daemon();
                    }
                    else
                    {
                        if (waveStep == 1) spawn = new Pixie(); else if (waveStep == 2) spawn = new Wisp();
                        else if (waveStep == 3) spawn = new Centaur(); else spawn = new Kirin();
                    }

                    spawn.Karma = 0; spawn.Fame = 0;

                    if (Utility.RandomDouble() < ParagonChance)
                    {
                        spawn.Name = (isLight ? "[Искаженный Страж] " : "[Священный Каратель] ") + spawn.Name + " [Paragon]";
                        spawn.Hue = 1281; spawn.HitsMaxSeed *= 3; spawn.Hits = spawn.HitsMaxSeed;
                        spawn.ActiveSpeed *= 0.8; spawn.SetDamage(spawn.DamageMin + 15, spawn.DamageMax + 25);
                    }
                    else
                    {
                        spawn.Name = (isLight ? "[Армия Скверны] " : "[Воин Света] ") + spawn.Name;
                        spawn.Hue = isLight ? 1175 : 2418;
                    }

                    Point3D loc = new Point3D(center.X + Utility.RandomMinMax(-4, 4), center.Y + Utility.RandomMinMax(-4, 4), center.Z);
                    loc.Z = map.GetAverageZ(loc.X, loc.Y);
                    spawn.MoveToWorld(loc, map); spawn.Combatant = leader;
                }
                new EpicTimeAttackTimer(leader, center, isLight, waveStep + 1, fireEffect).Start();
            }
            else if (waveStep == BaseWaveCount + 1) { SpawnFirstBoss(leader, center, isLight); }
        }

        private static void SpawnFirstBoss(PlayerMobile leader, Point3D center, bool isLight)
        {
            Map map = Map.Felucca;
            leader.SendMessage(0x35, "Время испытания стихий истекло! Грядёт пробуждение Хранителя Полюса!");
            
            BaseCreature boss = isLight ? (BaseCreature)new Daemon() : (BaseCreature)new AncientWyrm();
            boss.HitsMaxSeed = 25000; boss.Hits = 25000; boss.SetDamage(50, 75); 
            boss.Karma = 0; boss.Fame = 0; 

            Container backpack = boss.Backpack;
            if (backpack == null) { backpack = new Backpack(); backpack.Movable = false; boss.AddItem(backpack); }

            if (Utility.RandomDouble() < BossParagonChance)
            {
                boss.Name = isLight ? "Слэшер Бездны [Эпический Фракционный Paragon]" : "Небесный Судия [Эпический Фракционный Paragon]";
                boss.Hue = 1281; boss.HitsMaxSeed *= 2; boss.Hits = boss.HitsMaxSeed; boss.ActiveSpeed *= 0.82;
                backpack.DropItem(new Gold(Utility.RandomMinMax(6000, 9000)));
            }
            else
            {
                boss.Name = isLight ? "Владыка Преисподней: Воплощение Скверны" : "Архангел Сияния: Высший Судия Света";
                boss.Hue = isLight ? 1175 : 2418;
                backpack.DropItem(new Gold(Utility.RandomMinMax(3000, 5000)));
            }

            Point3D loc = new Point3D(center.X, center.Y - 2, center.Z); loc.Z = map.GetAverageZ(loc.X, loc.Y);
            boss.MoveToWorld(loc, map); boss.Combatant = leader;
            World.Broadcast(isLight ? 0x22 : 0x35, true, $"[Квест] В Храме Хаоса из бушующего разлома явился грозный {boss.Name}!");
            
            new FirstBossDeathMonitor(boss, leader, center, isLight).Start();
        }

        private class FirstBossDeathMonitor : Timer
        {
            private BaseCreature m_FirstBoss; PlayerMobile m_Leader; Point3D m_Center; bool m_IsLight;
            public FirstBossDeathMonitor(BaseCreature firstBoss, PlayerMobile leader, Point3D center, bool isLight) : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
            { m_FirstBoss = firstBoss; m_Leader = leader; m_Center = center; m_IsLight = isLight; }

            protected override void OnTick()
            {
                if (m_FirstBoss == null || m_FirstBoss.Deleted || !m_FirstBoss.Alive)
                {
                    Stop();
                    if (m_Leader == null || m_Leader.Deleted || !m_Leader.Alive) return;

                    Effects.SendLocationParticles(EffectItem.Create(m_Leader.Location, m_Leader.Map, TimeSpan.FromSeconds(1.5)), 0x3709, 10, 30, m_IsLight ? 2418 : 1175, 0, 5004, 0);
                    m_Leader.PlaySound(0x5C0); 

                    m_Leader.SendMessage(0x22, "Хранитель сокрушен, но Алтарь Хаоса вытягивает твое собственное отражение...");
                    m_Leader.SendMessage(0x35, "ФИНАЛЬНОЕ ИСПЫТАНИЕ: Сразись со своими грехами! Из праха восстает Травести!");

                    BaseCreature travesty = new Travesty();
                    travesty.HitsMaxSeed = 30000; travesty.Hits = 30000;
                    travesty.SetDamage(55, 80);
                    travesty.Karma = 0; travesty.Fame = 0;
                    travesty.Name = $"Травести: Эфирное Зеркало {m_Leader.Name} [TRUE BOSS]";

                    Container pack = travesty.Backpack;
                    if (pack == null) { pack = new Backpack(); pack.Movable = false; travesty.AddItem(pack); }
                    pack.DropItem(new Gold(Utility.RandomMinMax(8000, 12000))); 
                    for (int i = 0; i < 6; i++)
                    {
                        Item magicItem = Loot.RandomArmorOrShieldOrJewelry();
                        if (magicItem != null) { BaseRunicTool.ApplyAttributesTo(magicItem, 5, 95, 100); pack.DropItem(magicItem); }
                    }

                    Point3D loc = new Point3D(m_Center.X, m_Center.Y - 2, m_Center.Z); loc.Z = m_Leader.Map.GetAverageZ(loc.X, loc.Y);
                    travesty.MoveToWorld(loc, m_Leader.Map);
                    travesty.Combatant = m_Leader; 

                    // ИСПРАВЛЕНО: Вызываем награды с каноничным типом глобального EpicRank
                    EpicRewardSystem.MonitorBoss(travesty, m_Leader, m_IsLight ? EpicRank.Avatar : EpicRank.HandOfMondain);
                }
            }
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
