using System;
using System.Collections.Generic;
using System.IO;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Commands;
using Server.Network;


namespace Server.Custom.EpicAlignment
{
    public enum EpicRank : byte
    {
        None = 0,
        Avatar = 1,
        HandOfMondain = 2
    }

    public class EpicProfile
    {
        public EpicRank Rank { get; set; }
        public int Lives { get; set; } = 10;
        public DateTime LastActive { get; set; } = DateTime.UtcNow;
        public Timer AuraTimer { get; set; }
        public string DetectedClass { get; set; } = "Универсал";
    }

    public static class EpicAlignmentSystem
    {
        public static readonly int DatabaseVersion = 1;

        private static Dictionary<Serial, EpicProfile> _epicRegistry = new Dictionary<Serial, EpicProfile>();

        private static Dictionary<Serial, DateTime> _bossCooldown = new Dictionary<Serial, DateTime>();
        private static Dictionary<Serial, DateTime> _guardCooldown = new Dictionary<Serial, DateTime>();

        private static readonly string _saveFolder = Path.Combine(Core.BaseDirectory, "Saves/Custom");
        private static readonly string _savePath = Path.Combine(Core.BaseDirectory, "Saves/Custom/EpicAlignmentDatabase.bin");

        public static readonly Point3D PurgatoryLoc = new Point3D(6430, 1250, 13);
        public static readonly Point3D LightFortress = new Point3D(1495, 1629, 10);
        public static readonly Point3D DarkFortress = new Point3D(2114, 1312, -49);

                public static void Initialize()
        {
            CommandSystem.Register("CheckEpicStatus", AccessLevel.Player, new CommandEventHandler(CheckEpicStatus_OnCommand));
            CommandSystem.Register("EpicList", AccessLevel.Player, new CommandEventHandler(EpicList_OnCommand));

            EventSink.PlayerDeath += OnPlayerDeath;
            EventSink.Login += OnPlayerLogin;

            EventSink.WorldSave += (e) => Save();
            EventSink.Shutdown += (e) => Save();
            EventSink.ServerStarted += new ServerStartedEventHandler(Load);

            // ИСПРАВЛЕНО: Запуск нового разбитого модуля мантр Храма Хаоса! 🌌
            Server.Items.EpicMantraCore.Initialize();
        }



        public static EpicRank GetRank(Mobile m)
        {
            if (m == null) return EpicRank.None;
            if (_epicRegistry.TryGetValue(m.Serial, out EpicProfile profile))
                return profile.Rank;
            return EpicRank.None;
        }

        public static EpicProfile GetProfile(Mobile m)
        {
            if (m == null) return null;
            _epicRegistry.TryGetValue(m.Serial, out EpicProfile profile);
            return profile;
        }

        public static void ApplyClassBonus(Mobile m, EpicProfile profile)
        {
            if (m == null || profile == null || profile.Rank == EpicRank.None) return;

            m.RemoveStatMod("EpicClass_Str");
            m.RemoveStatMod("EpicClass_Dex");
            m.RemoveStatMod("EpicClass_Int");

            double magery = m.Skills[SkillName.Magery].Value;
            double necromancy = m.Skills[SkillName.Necromancy].Value;
            double archery = m.Skills[SkillName.Archery].Value;
            double swords = m.Skills[SkillName.Swords].Value;
            double fencing = m.Skills[SkillName.Fencing].Value;
            double macing = m.Skills[SkillName.Macing].Value;
            double chiv = m.Skills[SkillName.Chivalry].Value;

            double meleeMax = Math.Max(swords, Math.Max(fencing, macing));

            if (necromancy >= 80.0 && necromancy >= magery && necromancy >= meleeMax)
            {
                profile.DetectedClass = "Повелитель Смерти (Некромант)";
                m.AddStatMod(new StatMod(StatType.Int, "EpicClass_Int", 25, TimeSpan.Zero));
                m.AddStatMod(new StatMod(StatType.Str, "EpicClass_Str", 15, TimeSpan.Zero));
                m.SendMessage(0x22, $"[Сила Тьмы] Вы распознаны как {profile.DetectedClass}. Вам даровано +25 Инты и +15 Силы!");
            }
            else if (magery >= 80.0 && magery >= meleeMax && magery >= archery)
            {
                profile.DetectedClass = "Архимаг Мудрости";
                m.AddStatMod(new StatMod(StatType.Int, "EpicClass_Int", 40, TimeSpan.Zero));
                m.SendMessage(0x35, $"[Сила Света] Вы распознаны как {profile.DetectedClass}. Вам даровано +40 Интеллекта!");
            }
            else if (archery >= 80.0 && archery >= meleeMax)
            {
                profile.DetectedClass = "Снайпер Эфира (Лучник)";
                m.AddStatMod(new StatMod(StatType.Dex, "EpicClass_Dex", 30, TimeSpan.Zero));
                m.AddStatMod(new StatMod(StatType.Str, "EpicClass_Str", 10, TimeSpan.Zero));
                m.SendMessage(0x58, $"[Эпик] Вы распознаны как {profile.DetectedClass}. Вам даровано +30 Ловкости и +10 Силы!");
            }
            else if (meleeMax >= 80.0)
            {
                profile.DetectedClass = "Великий Гладиатор (Боец ближнего боя)";
                m.AddStatMod(new StatMod(StatType.Str, "EpicClass_Str", 30, TimeSpan.Zero));
                m.AddStatMod(new StatMod(StatType.Dex, "EpicClass_Dex", 15, TimeSpan.Zero));
                m.SendMessage(0x58, $"[Эпик] Вы распознаны как {profile.DetectedClass}. Вам даровано +30 Силы и +15 Ловкости!");
            }
            else if (chiv >= 60.0)
            {
                profile.DetectedClass = "Священный Паладин Ордена";
                m.AddStatMod(new StatMod(StatType.Str, "EpicClass_Str", 20, TimeSpan.Zero));
                m.AddStatMod(new StatMod(StatType.Int, "EpicClass_Int", 20, TimeSpan.Zero));
                m.SendMessage(0x35, $"[Сила Света] Вы распознаны как {profile.DetectedClass}. Вам даровано +20 Силы и +20 Инты!");
            }
            else
            {
                profile.DetectedClass = "Эпический Универсал";
                m.AddStatMod(new StatMod(StatType.Str, "EpicClass_Str", 15, TimeSpan.Zero));
                m.AddStatMod(new StatMod(StatType.Dex, "EpicClass_Dex", 15, TimeSpan.Zero));
                m.AddStatMod(new StatMod(StatType.Int, "EpicClass_Int", 15, TimeSpan.Zero));
                m.SendMessage(0x59, $"[Эпик] Вы распознаны как {profile.DetectedClass}. Вам дарован подъем всех статов на +15!");
            }
        }

        public static void SetRank(Mobile m, EpicRank rank)
        {
            if (m == null) return;

            if (rank == EpicRank.None)
            {
                if (_epicRegistry.TryGetValue(m.Serial, out EpicProfile oldProfile))
                {
                    if (oldProfile.AuraTimer != null) oldProfile.AuraTimer.Stop();
                }

                m.RemoveStatMod("EpicClass_Str");
                m.RemoveStatMod("EpicClass_Dex");
                m.RemoveStatMod("EpicClass_Int");

                _epicRegistry.Remove(m.Serial);
                m.Title = "";
                m.HueMod = -1;
                m.Delta(MobileDelta.Hue);

                if (m.Backpack != null)
                {
                    Item book = m.Backpack.FindItemByType(typeof(BookOfDestiny));
                    if (book != null) book.Delete();
                }
                return;
            }

            EpicProfile profile = new EpicProfile { Rank = rank, Lives = 10, LastActive = DateTime.UtcNow };

            if (_epicRegistry.TryGetValue(m.Serial, out EpicProfile existing))
            {
                if (existing.AuraTimer != null) existing.AuraTimer.Stop();
            }

            _epicRegistry[m.Serial] = profile;

            profile.AuraTimer = new EpicAuraParticleTimer(m, rank);
            profile.AuraTimer.Start();

            ApplyClassBonus(m, profile);

            if (rank == EpicRank.HandOfMondain)
            {
                m.Title = "[Hand of Mondain]";
                m.HueMod = 1175;
                CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x22, $"[Тьма] Сосария содрогнулась! {m.Name} завершил Наследие Мондейна, признан как {profile.DetectedClass} и занял Престол Тьмы (Жизней: 10)!");
            }
            else if (rank == EpicRank.Avatar)
            {
                m.Title = "[Avatar of Virtues]";
                m.HueMod = 2418;
                CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x35, $"[Свет] Пророчество сбылось! {m.Name} прошел Путь Аватара, признан как {profile.DetectedClass} и стал Воплощением Добродетелей (Жизней: 10)!");
            }
            m.Delta(MobileDelta.Hue);
        }

        private class EpicAuraParticleTimer : Timer
        {
            private Mobile m_Owner;
            private EpicRank m_Rank;

            public EpicAuraParticleTimer(Mobile owner, EpicRank rank) : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.2))
            {
                m_Owner = owner;
                m_Rank = rank;
                Priority = TimerPriority.OneSecond;
            }

            protected override void OnTick()
            {
                if (m_Owner == null || m_Owner.Deleted || EpicAlignmentSystem.GetRank(m_Owner) != m_Rank)
                {
                    Stop();
                    return;
                }

                if (m_Owner.Alive && m_Owner.Map != Map.Internal && m_Owner.Map != null && m_Owner.AccessLevel == AccessLevel.Player)
                {
                    if (m_Rank == EpicRank.Avatar)
                    {
                        Effects.SendLocationParticles(EffectItem.Create(m_Owner.Location, m_Owner.Map, TimeSpan.FromSeconds(0.5)), 0x373A, 10, 15, 2418, 0, 5012, 0);
                        if (Utility.RandomBool())
                            Effects.SendLocationParticles(EffectItem.Create(new Point3D(m_Owner.X, m_Owner.Y, m_Owner.Z + 10), m_Owner.Map, TimeSpan.FromSeconds(0.3)), 0x375A, 9, 10, 1150, 0, 5027, 0);
                    }
                    else if (m_Rank == EpicRank.HandOfMondain)
                    {
                        Effects.SendLocationParticles(EffectItem.Create(m_Owner.Location, m_Owner.Map, TimeSpan.FromSeconds(0.5)), 0x3709, 10, 20, 1175, 0, 5004, 0);
                        if (Utility.RandomBool())
                            Effects.SendLocationParticles(EffectItem.Create(m_Owner.Location, m_Owner.Map, TimeSpan.FromSeconds(0.3)), 0x376A, 9, 12, 1157, 0, 5002, 0);
                    }
                }
            }
        }
        private static void OnPlayerDeath(PlayerDeathEventArgs e)
        {
            Mobile victim = e.Mobile;
            if (victim == null) return;
            EpicProfile profile = GetProfile(victim);
            if (profile == null || profile.Rank == EpicRank.None) return;
            profile.LastActive = DateTime.UtcNow;
            Mobile killer = victim.LastKiller;
            if (killer != null && killer.Player && killer != victim)
            {
                profile.Lives--;
                BankCheck goldCheck = new BankCheck(10000);
                killer.AddToBackpack(goldCheck);
                Item relic = Loot.RandomArmorOrShieldOrJewelry();
                if (relic != null)
                {
                    BaseRunicTool.ApplyAttributesTo(relic, 5, 100, 100);
                    relic.Name = $"[Эфирный Осколок] Реликвия за повержение Полубога {victim.Name}";
                    relic.Hue = (profile.Rank == EpicRank.Avatar) ? 2418 : 1175;
                    killer.AddToBackpack(relic);
                }
                if (Utility.RandomDouble() < 0.20)
                {
                    BaseWeapon legendWeapon = null;
                    if (profile.Rank == EpicRank.Avatar)
                    {
                        legendWeapon = new Broadsword();
                        legendWeapon.Name = $"[Sword of Virtues] Клинок поверженного Аватара {victim.Name}";
                        legendWeapon.Hue = 2418;
                        legendWeapon.WeaponAttributes.HitLightning = 50;
                        legendWeapon.Slayer = SlayerName.Silver;
                    }
                    else if (profile.Rank == EpicRank.HandOfMondain)
                    {
                        legendWeapon = new ExecutionersAxe();
                        legendWeapon.Name = $"[Mondain's Retribution] Топор Ярости Мондейна от {victim.Name}";
                        legendWeapon.Hue = 1175;
                        legendWeapon.WeaponAttributes.HitLeechMana = 50;
                        legendWeapon.WeaponAttributes.HitPoisonArea = 30;
                    }
                    if (legendWeapon != null)
                    {
                        BaseRunicTool.ApplyAttributesTo(legendWeapon, 5, 95, 100);
                        killer.AddToBackpack(legendWeapon);
                        killer.SendMessage(0x35, $"Легенда! Вам улыбнулась удача, и вы заполучили личное фракционное оружие Полубога: {legendWeapon.Name}!");
                    }
                }
                if (profile.Lives <= 0)
                {
                    string fallAlert = string.Format("[Низвержение] Невероятно! Игрок {0} нанес финальный смертельный удар {1}. Полубог потерял все свои силы, класс {2} аннулирован и он низвергнут в прах!", killer.Name, victim.Name, profile.DetectedClass);
                    CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x22, fallAlert);
                    SetRank(victim, EpicRank.None);
                    return;
                }
                else
                {
                    string pvpAlert = string.Format("[Битва] {0} убил {1}! У Полубога осталось {2} из 10 жизней! Он отправлен в Мир Посмертия.", killer.Name, victim.Name, profile.Lives);
                    CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x59, pvpAlert);
                    Timer.DelayCall(TimeSpan.FromSeconds(1.0), () =>
                    {
                        victim.MoveToWorld(PurgatoryLoc, Map.Felucca);
                        victim.SendMessage(0x22, "Вы пали от руки смертного. Силы Посмертия сковали вас на этой территории!");
                    });
                }
            }
            else
            {
                victim.SendMessage(0x22, "Вы пали в бою с монстрами. Ваша сущность разорвана, вы телепортированы в цитадель фракции.");
                Timer.DelayCall(TimeSpan.FromSeconds(1.0), () =>
                {
                    Point3D spawnFort = (profile.Rank == EpicRank.Avatar) ? LightFortress : DarkFortress;
                    victim.MoveToWorld(spawnFort, (profile.Rank == EpicRank.Avatar) ? Map.Felucca : Map.Malas);
                    _bossCooldown[victim.Serial] = DateTime.UtcNow + TimeSpan.FromMinutes(30.0);
                    victim.SendMessage(0x22, "Вы ослаблены. Способности Книги Судьбы заблокированы на 30 минут!");
                });
            }
        }
        // ИСПРАВЛЕНО: Заменили универсальный generic-список "List" на нативный массив фиксированной длины "Serial[]"
        // Это полностью убирает путаницу имен и гарантирует чистую компиляцию на net48
        private static void OnPlayerLogin(LoginEventArgs e)
        {
            Mobile m = e.Mobile;
            if (m == null) return;
            EpicProfile profile = GetProfile(m);
            if (profile != null && profile.Rank != EpicRank.None)
            {
                profile.LastActive = DateTime.UtcNow;
                if (profile.AuraTimer != null) profile.AuraTimer.Stop();
                profile.AuraTimer = new EpicAuraParticleTimer(m, profile.Rank);
                profile.AuraTimer.Start();
                ApplyClassBonus(m, profile);
            }
            DateTime threshold = DateTime.UtcNow - TimeSpan.FromDays(90.0);
            int expiredCount = 0;
            foreach (var kvp in _epicRegistry)
            {
                if (kvp.Value.LastActive < threshold)
                    expiredCount++;
            }
            if (expiredCount > 0)
            {
                Serial[] toRemoveArray = new Serial[expiredCount];
                int idx = 0;
                foreach (var kvp in _epicRegistry)
                {
                    if (kvp.Value.LastActive < threshold)
                    {
                        toRemoveArray[idx] = kvp.Key;
                        idx++;
                        Mobile target = World.FindMobile(kvp.Key);
                        if (target != null)
                        {
                            target.Title = "";
                            target.HueMod = -1;
                        }
                    }
                }
                for (int i = 0; i < toRemoveArray.Length; i++)
                {
                    Serial s = toRemoveArray[i];
                    if (_epicRegistry.TryGetValue(s, out EpicProfile expiredProf))
                    {
                        if (expiredProf.AuraTimer != null) expiredProf.AuraTimer.Stop();
                    }
                    _epicRegistry.Remove(s);
                    Mobile ghost = World.FindMobile(s);
                    if (ghost != null && ghost.Backpack != null)
                    {
                        Item b = ghost.Backpack.FindItemByType(typeof(BookOfDestiny));
                        if (b != null) b.Delete();
                    }
                }
            }
        }
        public static void Save()
        {
            if (_epicRegistry.Count == 0) return;
            GenericWriter writer = null;
            try
            {
                if (!Directory.Exists(_saveFolder))
                    Directory.CreateDirectory(_saveFolder);
                writer = new BinaryFileWriter(_savePath, false);
                writer.Write((int)DatabaseVersion);
                writer.Write((int)_epicRegistry.Count);
                foreach (var kvp in _epicRegistry)
                {
                    writer.Write((int)kvp.Key.Value);
                    writer.Write((byte)kvp.Value.Rank);
                    writer.Write((int)kvp.Value.Lives);
                    writer.Write((string)kvp.Value.LastActive.ToString("o"));
                }
                Console.WriteLine($"[EpicAlignment] Успешно сохранено {_epicRegistry.Count} эпических профилей.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EpicAlignment] Ошибка сохранения базы данных: {ex.Message}");
            }
            finally
            {
                if (writer != null) writer.Close();
            }
        }
        public static void Load()
        {
            if (!File.Exists(_savePath)) return;
            GenericReader reader = null;
            FileStream stream = null;
            BinaryReader binReader = null;
            try
            {
                stream = new FileStream(_savePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                binReader = new BinaryReader(stream);
                reader = new BinaryFileReader(binReader);
                int version = reader.ReadInt();
                int count = reader.ReadInt();
                _epicRegistry.Clear();
                for (int i = 0; i < count; i++)
                {
                    Serial serial = (Serial)reader.ReadInt();
                    EpicRank rank = (EpicRank)reader.ReadByte();
                    int lives = reader.ReadInt();
                    DateTime lastActive = DateTime.Parse(reader.ReadString());
                    EpicProfile profile = new EpicProfile
                    {
                        Rank = rank,
                        Lives = lives,
                        LastActive = lastActive
                    };
                    _epicRegistry[serial] = profile;
                    Mobile onlineMob = World.FindMobile(serial);
                    if (onlineMob != null && onlineMob.NetState != null)
                    {
                        profile.AuraTimer = new EpicAuraParticleTimer(onlineMob, rank);
                        profile.AuraTimer.Start();
                        ApplyClassBonus(onlineMob, profile);
                    }
                }
                Console.WriteLine($"[EpicAlignment] Успешно загружено {_epicRegistry.Count} эпических профилей из базы данных.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EpicAlignment] Ошибка загрузки базы данных: {ex.Message}");
            }
            finally
            {
                if (binReader != null) binReader.Close();
                if (stream != null) stream.Close();
            }
        }
        public static bool CanSummonBoss(Mobile m)
        {
            if (_bossCooldown.TryGetValue(m.Serial, out DateTime readyTime))
                return DateTime.UtcNow >= readyTime;
            return true;
        }
        public static void RegisterBossSummon(Mobile m)
        {
            _bossCooldown[m.Serial] = DateTime.UtcNow + TimeSpan.FromDays(1.0);
        }
        public static bool CanSummonGuards(Mobile m)
        {
            if (_guardCooldown.TryGetValue(m.Serial, out DateTime readyTime))
                return DateTime.UtcNow >= readyTime;
            return true;
        }
        public static void RegisterGuardSummon(Mobile m)
        {
            _guardCooldown[m.Serial] = DateTime.UtcNow + TimeSpan.FromHours(2.0);
        }
        private static void CheckEpicStatus_OnCommand(CommandEventArgs e)
        {
            Mobile m = e.Mobile;
            if (m == null) return;
            EpicProfile profile = GetProfile(m);
            if (profile == null || profile.Rank == EpicRank.None)
            {
                m.SendMessage(0x59, "=== Ваш Эпический Статус: None ===");
                m.SendMessage($"Для начала квеста ваша Карма должна быть +15000 (Аватар) или -15000 (Мондейн).");
            }
            else
            {
                m.SendMessage(0x59, $"=== Ваш Эпический Статус: {profile.Rank} ===");
                m.SendMessage(0x58, $"Обнаруженный архетип класса: {profile.DetectedClass}");
                m.SendMessage(0x35, $"Осталось жизней: {profile.Lives} из 10. Последняя активность обновлена.");
            }
        }
        private static void EpicList_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null) return;
            from.SendMessage(0x59, "=== ВЕСТНИК МИРОВОЗЗРЕНИЙ: СТАТУС ПОЛЮСОВ ===");
            int epicOnlineCount = 0;
            foreach (NetState ns in NetState.Instances)
            {
                Mobile m = ns.Mobile;
                if (m == null || !m.Alive || m.AccessLevel > AccessLevel.Player) continue;
                EpicProfile profile = GetProfile(m);
                if (profile == null || profile.Rank == EpicRank.None) continue;
                epicOnlineCount++;
                string locName = (m.Region != null && !string.IsNullOrEmpty(m.Region.Name)) ? m.Region.Name : "Дикие земли";
                string mapName = m.Map != null ? m.Map.Name : "Неизвестно";
                if (profile.Rank == EpicRank.Avatar)
                {
                    from.SendMessage(0x35, $"[СВЕТ] Аватар {m.Name} ({profile.DetectedClass}) [Жизней: {profile.Lives}/10] — {locName} ({mapName})");
                }
                else if (profile.Rank == EpicRank.HandOfMondain)
                {
                    from.SendMessage(0x22, $"[ТЬМА] Воплощение Зла {m.Name} ({profile.DetectedClass}) [Жизней: {profile.Lives}/10] — {locName} ({mapName})");
                }
            }
            if (epicOnlineCount == 0)
                from.SendMessage(0x59, "В данный момент Высшие Полюса спят или скрываются в тенях.");
            from.SendMessage(0x59, "=============================================");
        }
    }
}