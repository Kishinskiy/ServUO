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
    }

    public static class EpicAlignmentSystem
    {
        public static readonly int DatabaseVersion = 1;

        private static Dictionary<Serial, EpicProfile> _epicRegistry = new Dictionary<Serial, EpicProfile>();

        private static Dictionary<Serial, DateTime> _bossCooldown = new Dictionary<Serial, DateTime>();
        private static Dictionary<Serial, DateTime> _guardCooldown = new Dictionary<Serial, DateTime>();

        private static readonly string _saveFolder = Path.Combine(Core.BaseDirectory, "Saves/Custom");
        private static readonly string _savePath = Path.Combine(Core.BaseDirectory, "Saves/Custom/EpicAlignmentDatabase.bin");

        public static readonly Point3D PurgatoryLoc = new Point3D(5234, 120, 0);
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

        public static void SetRank(Mobile m, EpicRank rank)
        {
            if (m == null) return;

            if (rank == EpicRank.None)
            {
                if (_epicRegistry.TryGetValue(m.Serial, out EpicProfile oldProfile))
                {
                    if (oldProfile.AuraTimer != null) oldProfile.AuraTimer.Stop();
                }

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

            if (rank == EpicRank.HandOfMondain)
            {
                m.Title = "[Hand of Mondain]";
                m.HueMod = 1175;
                CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x22, $"[Тьма] Сосария содрогнулась! {m.Name} завершил Наследие Мондейна и занял Престол Тьмы (Жизней: 10)!");
            }
            else if (rank == EpicRank.Avatar)
            {
                m.Title = "[Avatar of Virtues]";
                m.HueMod = 2418;
                CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x35, $"[Свет] Пророчество сбылось! {m.Name} прошел Путь Аватара и стал Воплощением Добродетелей (Жизней: 10)!");
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

                BankCheck goldCheck = new BankCheck(1000000);
                killer.AddToBackpack(goldCheck);
                Item relic = Loot.RandomArmorOrShieldOrWeaponOrJewelry(true, true, true);
                if (relic != null)
                {
                    BaseRunicTool.ApplyAttributesTo(relic, 5, 100, 100);
                    relic.Name = $"[Трофей] Реликвия за свержение {victim.Name}";
                    relic.Hue = 1161;
                    killer.AddToBackpack(relic);
                }

                if (profile.Lives <= 0)
                {
                    string fallAlert = string.Format("[Низвержение] Невероятно! Игрок {0} нанес финальный смертельный удар {1}. Полубог потерял все свои силы и низвергнут в прах!", killer.Name, victim.Name);
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
            }

            List<Serial> toRemove = new List<Serial>();
            DateTime threshold = DateTime.UtcNow - TimeSpan.FromDays(90.0);

            foreach (var kvp in _epicRegistry)
            {
                if (kvp.Value.LastActive < threshold)
                {
                    toRemove.Add(kvp.Key);
                    Mobile target = World.FindMobile(kvp.Key);
                    if (target != null)
                    {
                        target.Title = "";
                        target.HueMod = -1;
                    }
                }
            }
            foreach (Serial s in toRemove)
            {
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
                m.SendMessage(0x35, $"Осталось жизней: {profile.Lives} из 10. Последняя активность обновлена.");
            }
        }
        // ИСПРАВЛЕНО: Полностью убрана путаница с именами коллекций, метод использует стандартную изолированную проверку онлайна
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
                    from.SendMessage(0x35, $"[СВЕТ] Аватар {m.Name} (Жизней: {profile.Lives}/10) — Находится: {locName} ({mapName})");
                }
                else if (profile.Rank == EpicRank.HandOfMondain)
                {
                    from.SendMessage(0x22, $"[ТЬМА] Воплощение Зла {m.Name} (Жизней: {profile.Lives}/10) — Находится: {locName} ({mapName})");
                }
            }
            if (epicOnlineCount == 0)
                from.SendMessage(0x59, "В данный момент Высшие Полюса спят или скрываются в тенях.");
            from.SendMessage(0x59, "=============================================");
        }
    }
}