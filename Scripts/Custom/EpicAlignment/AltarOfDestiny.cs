using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Commands;

namespace Server.Custom.EpicAlignment
{
    public class AltarOfDestiny : Item
    {
        [Constructable]
        public AltarOfDestiny() : base(0x1180) 
        {
            Name = "Алтарь Предназначения [Эндгейм Квест]";
            Movable = false;
            Hue = 1151;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null) return;

            if (!from.InRange(this.Location, 3))
            {
                from.SendMessage("Вы слишком далеко от Алтаря.");
                return;
            }

            EpicRank currentRank = EpicAlignmentSystem.GetRank(from);
            if (currentRank != EpicRank.None)
            {
                from.SendMessage(0x35, "Вы уже завершили свой Эпический Путь и подчинили себе стихии!");
                return;
            }

            bool isDarkCandidate = from.Karma <= -15000;
            bool isLightCandidate = from.Karma >= 15000;

            if (!isDarkCandidate && !isLightCandidate)
            {
                from.SendMessage(0x22, "Алтарь молчит. Требуется абсолютная чистота духа (+15,000 кармы) или абсолютное погружение во Тьму (-15,000 кармы).");
                return;
            }

            Container pack = from.Backpack;
            if (pack == null) return;

            Item earth = pack.FindItemByType(typeof(HeartOfEarth));
            Item flame = pack.FindItemByType(typeof(TongueOfFlame));
            Item seas = pack.FindItemByType(typeof(TearOfSeas));
            Item wind = pack.FindItemByType(typeof(BreathOfWind));

            if (earth == null || flame == null || seas == null || wind == null)
            {
                from.SendMessage(0x59, "=== РИТУАЛ ПАГАНА: ТРЕБОВАНИЕ СТИХИЙ ===");
                from.SendMessage(earth != null ? 0x35 : 0x22, $"- Сердце Земли: {(earth != null ? "Найдено" : "Отсутствует")}");
                from.SendMessage(flame != null ? 0x35 : 0x22, $"- Язык Пламени: {(flame != null ? "Найдено" : "Отсутствует")}");
                from.SendMessage(seas != null ? 0x35 : 0x22, $"- Слеза Морей:  {(seas != null ? "Найдено" : "Отсутствует")}");
                from.SendMessage(wind != null ? 0x35 : 0x22, $"- Дыхание Ветра: {(wind != null ? "Найдено" : "Отсутствует")}");
                from.SendMessage(0x59, "Соберите все 4 артефакта Пагана с сильнейших существ мира, чтобы призвать Сюжетного Хранителя!");
                return;
            }

            earth.Delete(); flame.Delete(); seas.Delete(); wind.Delete();

            from.PlaySound(0x243); 
            Effects.SendLocationParticles(EffectItem.Create(this.Location, this.Map, EffectItem.DefaultDuration), 0x3709, 9, 32, 5013);

            BaseCreature boss;
            if (isDarkCandidate)
            {
                boss = new Daemon();
                boss.Name = "Страж Бездны: Испытание Мондейна [QUEST BOSS]";
                boss.Hue = 1175;
                CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x22, $"[Квест] {from.Name} запустил ритуал Тьмы на Алтаре! Из разлома явился Страж Бездны!");
            }
            else
            {
                boss = new AncientWyrm();
                boss.Name = "Небесный Судия: Испытание Аватара [QUEST BOSS]";
                boss.Hue = 2418;
                CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x35, $"[Квест] {from.Name} запустил ритуал Света на Алтаре! С небес спустился Небесный Судия!");
            }

            boss.HitsMaxSeed = 15000; boss.Hits = 15000;
            boss.SetDamage(40, 60);

            Point3D spawnLoc = new Point3D(this.X + 2, this.Y + 2, this.Map.GetAverageZ(this.X + 2, this.Y + 2));
            boss.MoveToWorld(spawnLoc, this.Map);

            // ИСПРАВЛЕНО: Запуск штатного таймера мониторинга ServUO
            new EpicBossMonitorTimer(boss, from, isDarkCandidate ? EpicRank.HandOfMondain : EpicRank.Avatar).Start();
        }

        // ИСПРАВЛЕНО: Кастомный подкласс таймера, полностью совместимый с архитектурой ServUO
        private class EpicBossMonitorTimer : Timer
        {
            private BaseCreature m_Boss;
            private Mobile m_Candidate;
            private EpicRank m_TargetRank;

            public EpicBossMonitorTimer(BaseCreature boss, Mobile candidate, EpicRank targetRank) : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
            {
                m_Boss = boss;
                m_Candidate = candidate;
                m_TargetRank = targetRank;
                Priority = TimerPriority.OneSecond;
            }

            protected override void OnTick()
            {
                if (m_Boss == null || m_Boss.Deleted)
                {
                    Stop();
                    return;
                }

                if (!m_Boss.Alive)
                {
                    Stop();

                    if (m_Candidate != null && !m_Candidate.Deleted && m_Candidate.Alive)
                    {
                        EpicAlignmentSystem.SetRank(m_Candidate, m_TargetRank);
                        m_Candidate.AddToBackpack(new BookOfDestiny());
                        
                        if (m_TargetRank == EpicRank.HandOfMondain)
                        {
                            m_Candidate.SendMessage(0x22, "Вы сокрушили Хранителя! Элементальные силы Пагана подчинены. Вы стали Правой Рукой Мондейна!");
                            Effects.PlaySound(m_Candidate.Location, m_Candidate.Map, 0x5C8);
                        }
                        else
                        {
                            m_Candidate.SendMessage(0x35, "Вы выдержали судейство! Добродетели Сосарии слились с вашим телом. Вы — истинный Аватар!");
                            Effects.PlaySound(m_Candidate.Location, m_Candidate.Map, 0x5C9);
                        }
                    }
                }
            }
        }

        public AltarOfDestiny(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
}
