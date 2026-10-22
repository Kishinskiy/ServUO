using System;
using System.Collections.Generic;
using System.IO;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Services.Virtues;

namespace Server.Custom.KarmaVirtueSystem
{
    public static class KarmaVirtueManager
    {
        private static readonly string _savePath = Path.Combine(Core.BaseDirectory, "Saves/Custom/KarmaVirtueSystem.bin");
        private static Dictionary<Mobile, Dictionary<int, DateTime>> _shrineCooldowns = new Dictionary<Mobile, Dictionary<int, DateTime>>();
        private static Dictionary<Mobile, Dictionary<int, int>> _activeBuffs = new Dictionary<Mobile, Dictionary<int, int>>();

        // Координаты всех 8 алтарей добродетелей в Felucca (X, Y, Z, Map, VirtueName, Mantra)
        public static readonly ShrineData[] Shrines = new ShrineData[]
        {
            new ShrineData(0, VirtueName.Valor,        "RA",   2493, 3931, 5,  Map.Felucca),
            new ShrineData(1, VirtueName.Spirituality, "OM",   1595, 2490, 20, Map.Felucca),
            new ShrineData(2, VirtueName.Honor,        "SUMM", 1398, 3765, 18, Map.Felucca),
            new ShrineData(3, VirtueName.Sacrifice,    "CAH",  3355, 290,  9,  Map.Felucca),
            new ShrineData(4, VirtueName.Justice,      "BEH",  1302, 1080, 0,  Map.Felucca),
            new ShrineData(5, VirtueName.Honesty,      "AHM",  4217, 563,  36, Map.Felucca),
            new ShrineData(6, VirtueName.Compassion,   "MU",   1855, 865,  10, Map.Felucca),
            new ShrineData(7, VirtueName.Humility,     "LUM",  4274, 3711, 0,  Map.Felucca)
        };

        public static void Initialize()
        {
            EventSink.Speech += OnSpeech;
            EventSink.WorldSave += e => Save();
            EventSink.Shutdown += e => Save();
            EventSink.ServerStarted += Load;
        }

        private static void OnSpeech(SpeechEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || !from.Alive || from.Map == null)
                return;

            string speech = e.Speech.Trim().ToUpper();

            foreach (ShrineData shrine in Shrines)
            {
                if (speech == shrine.Mantra)
                {
                    if (from.Map == shrine.Map && from.InRange(new Point3D(shrine.X, shrine.Y, shrine.Z), 4))
                    {
                        e.Handled = true; // Перехватываем речь, чтобы мантры не спамились в общий чат
                        ProcessShrineActivation(from, shrine);
                        return;
                    }
                }
            }
        }

        private static void ProcessShrineActivation(Mobile from, ShrineData shrine)
        {
            // 1. ПРОВЕРКА ДОСТУПА: Должен быть хотя бы 1 ранг (Seeker), полученный за действия в мире
            int virtuePoints = VirtueHelper.GetLevel(from, shrine.Virtue);
            if (virtuePoints == 0)
            {
                from.PlaySound(0x5C3);
                from.SendMessage(0x22, $"Алтарь безмолвен. Вы еще не совершили в мире деяний, чтобы пробудить силу добродетели {shrine.Virtue}!");
                return;
            }

            // 2. ПРОВЕРКА КУЛДАУНА (Раз в 22 часа на каждый алтарь)
            if (!_shrineCooldowns.ContainsKey(from))
                _shrineCooldowns[from] = new Dictionary<int, DateTime>();

            if (_shrineCooldowns[from].TryGetValue(shrine.ID, out DateTime nextUse))
            {
                if (DateTime.UtcNow < nextUse)
                {
                    TimeSpan rem = nextUse - DateTime.UtcNow;
                    from.SendMessage(0x22, $"Этот алтарь истощен. Энергия восстановится через {rem.Hours} ч. {rem.Minutes} мин.");
                    return;
                }
            }

            // 3. ПРОВЕРКА КАРМЫ И НАЛОЖЕНИЕ СООТВЕТСТВУЮЩЕГО БАФФА
            bool isDark = from.Karma < 0;

            if (isDark)
            {
                // Эффекты тьмы
                from.PlaySound(0x5A1); // Зловещий скрежет
                Effects.SendLocationLightningEffect(from.Location, from.Map);
                Effects.SendLocationEffect(from.Location, from.Map, 0x3709, 30, 10, 0x497, 0); // Пурпурный огонь

                if (shrine.Virtue == VirtueName.Valor)
                {
                    from.SendMessage(0x22, "Вы осквернили алтарь Доблести! Тёмный бафф порочной Жадности (Greed) окутывает вас на 2 часа.");
                    ApplyBuffInternal(from, shrine.ID, true);
                }
                else
                {
                    from.SendMessage(0x22, $"Тёмные помыслы искажают энергию {shrine.Virtue}. Вы получили порочный бафф школы Хаоса.");
                    ApplyBuffInternal(from, shrine.ID, true);
                }
            }
            else
            {
                // Эффекты света
                from.PlaySound(0xF7); // Ангельский хор
                Effects.SendLocationLightningEffect(from.Location, from.Map);
                Effects.SendLocationEffect(from.Location, from.Map, 0x375A, 9, 20, 0x5D, 0); // Золотое сияние

                from.SendMessage(0x5D, $"Вы успешно медитируете. Высшее благословение {shrine.Virtue} снизошло на вас на 2 часа!");
                ApplyBuffInternal(from, shrine.ID, false);
            }

            // Записываем кулдаун (22 часа)
            _shrineCooldowns[from][shrine.ID] = DateTime.UtcNow + TimeSpan.FromHours(22.0);
        }

        private static void ApplyBuffInternal(Mobile from, int shrineID, bool isDark)
        {
            if (!_activeBuffs.ContainsKey(from))
                _activeBuffs[from] = new Dictionary<int, int>();

            // Кодируем сторону: положительные ID — свет, отрицательные — тьма
            int buffKey = isDark ? -(shrineID + 1) : (shrineID + 1);
            _activeBuffs[from][buffKey] = 100;

            // Запуск таймера удаления баффа через 2 часа
            Timer.DelayCall(TimeSpan.FromHours(2.0), () =>
            {
                if (from != null && _activeBuffs.ContainsKey(from))
                {
                    _activeBuffs[from].Remove(buffKey);
                    from.SendMessage(0x35, "Действие благословения алтаря подошло к концу.");
                }
            });
        }

        public static bool HasBuffActive(Mobile m, int shrineID, bool isDark)
        {
            if (m == null || !_activeBuffs.ContainsKey(m))
                return false;

            int buffKey = isDark ? -(shrineID + 1) : (shrineID + 1);
            return _activeBuffs[m].ContainsKey(buffKey);
        }

        // ХУК ЭКОНОМИКИ: Удваивает золото и проклинает шмот (Вызывать из BaseCreature.cs)
        public static void HandleMonsterLoot(Mobile victim, Container lootContainer)
        {
            if (victim == null || lootContainer == null)
                return;

            Mobile killer = victim.FindMostRecentDamager(false);
            if (killer is BaseCreature)
                killer = ((BaseCreature)killer).GetMaster();

            if (killer == null || !killer.Player)
                return;

            // Проверяем тёмный бафф Жадности (Valor ID = 0, Dark = true)
            if (HasBuffActive(killer, 0, true))
            {
                List<Gold> goldPiles = new List<Gold>();
                foreach (Item item in lootContainer.Items)
                {
                    if (item is Gold) goldPiles.Add((Gold)item);
                }

                foreach (Gold gold in goldPiles)
                {
                    gold.Amount *= 2; // Золотая лихорадка!
                }

                foreach (Item item in lootContainer.Items)
                {
                    if (item is BaseWeapon || item is BaseArmor || item is BaseJewel || item is Spellbook)
                    {
                        if (Utility.RandomBool()) // 50% шанс проклятия вещи
                        {
                            item.LootType = LootType.Cursed;
                            item.Hue = 0x497; // Перекрашиваем в фиолетово-порочный цвет
                        }
                    }
                }
            }
        }

        // МЕТОД ДЛЯ ФИНАЛЬНОГО КВЕСТА: Проверяет, является ли игрок Аватаром (все 8 добродетелей на 3 ранге - Knight)
        public static bool CheckAvatarStatus(Mobile from, bool sendMessages)
        {
            if (from == null) return false;

            foreach (ShrineData shrine in Shrines)
            {
                if (VirtueHelper.GetLevel(from, shrine.Virtue) < 3) // 3 = Ранг Knight в ядре ServUO
                {
                    if (sendMessages)
                    {
                        from.SendMessage(0x22, $"Вы еще не достигли идеала (Knight) в добродетели {shrine.Virtue}! Проход закрыт.");
                    }
                    return false;
                }
            }

            if (sendMessages)
            {
                from.SendMessage(0x5D, "Великолепно! Вы познали все 8 столпов мира и заслужили право предстать перед финальным испытанием!");
            }
            return true;
        }

        public static void Save()
        {
            GenericWriter writer = null;
            try
            {
                string folder = Path.GetDirectoryName(_savePath);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                writer = new BinaryFileWriter(_savePath, false);
                writer.Write((int)0); // версия
            }
            catch { }
            finally { if (writer != null) writer.Close(); }
        }

        public static void Load() { if (!File.Exists(_savePath)) return; }
    }

    public class ShrineData
    {
        public int ID { get; private set; }
        public VirtueName Virtue { get; private set; }
        public string Mantra { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Z { get; private set; }
        public Map Map { get; private set; }

        public ShrineData(int id, VirtueName virtue, string mantra, int x, int y, int z, Map map)
        {
            ID = id;
            Virtue = virtue;
            Mantra = mantra;
            X = x;
            Y = y;
            Z = z;
            Map = map;
        }
    }
}