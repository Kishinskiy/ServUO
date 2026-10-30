using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.MercenarySystem
{
    // Данные павшего наёмника, ожидающего выкупа в банке данных Капитана
    public class FallenMercenaryData
    {
        public string OwnerAccount { get; set; }
        public string MercName { get; set; }
        public MercenaryClass Class { get; set; }
        public int Level { get; set; }
        public int Exp { get; set; }
        public List<Item> EquippedItems { get; set; } = new List<Item>();
    }

    public static class MercenaryRescueSystem
    {
        // Глобальная база данных погибших наёмников (Имя аккаунта хозяина -> список слуг)
        private static readonly Dictionary<string, List<FallenMercenaryData>> _rescueRoster 
            = new Dictionary<string, List<FallenMercenaryData>>();

        public static void Initialize()
        {
            // Хукаем смерть существ в мире
            EventSink.CreatureDeath += OnCreatureDeath;
        }

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            BaseMercenary merc = e.Creature as BaseMercenary;
            
            // Если погиб наш кастомный наёмник и у него есть хозяин
            if (merc != null && !merc.Deleted && merc.ControlMaster is PlayerMobile pm)
            {
                if (pm.Account == null) return;
                string accName = pm.Account.Username.ToLowerInvariant().Trim();

                // Формируем пакет сохранения для выкупа
                FallenMercenaryData fallenData = new FallenMercenaryData
                {
                    OwnerAccount = accName,
                    MercName = merc.RawName,
                    Class = merc.Class,
                    Level = merc.MercLevel,
                    Exp = merc.MercExp
                };

                // Спасаем его одетую экипировку, перенося её в контейнер хранения
                List<Item> wornItems = new List<Item>(merc.Items);
                foreach (Item item in wornItems)
                {
                    if (item != null && !item.Deleted && item.Movable == false && item.Layer != Layer.Backpack)
                    {
                        // Делаем вещь временно перемещаемой, чтобы сохранить её, и снимаем с трупа
                        item.Movable = true;
                        fallenData.EquippedItems.Add(item);
                        item.Internalize(); 
                    }
                }

                if (!_rescueRoster.TryGetValue(accName, out List<FallenMercenaryData> list))
                {
                    list = new List<FallenMercenaryData>();
                    _rescueRoster[accName] = list;
                }

                list.Add(fallenData);

                // Оповещаем хозяина
                pm.PlaySound(0x5C3); // Грустный звук
                pm.SendMessage(0x22, $"[Гильдия Наёмников]: Ваш спутник {merc.Name} пал в бою! Его снаряжение и душа спасены Вербовщиками. Вы можете выкупить его у любого Капитана Наёмников в городах.");
                
                // Удаляем нативного моба, чтобы он не оставлял голый лут в данже
                Timer.DelayCall(TimeSpan.FromMilliseconds(50), () => merc.Delete());
            }
        }

        // Метод проверки наличия слуг на выкуп у конкретного игрока
        public static List<FallenMercenaryData> GetFallenMercenaries(PlayerMobile pm)
        {
            if (pm == null || pm.Account == null) return null;
            string accName = pm.Account.Username.ToLowerInvariant().Trim();

            _rescueRoster.TryGetValue(accName, out List<FallenMercenaryData> list);
            return list;
        }

        public static void RemoveFromRoster(PlayerMobile pm, FallenMercenaryData data)
        {
            if (pm == null || pm.Account == null || data == null) return;
            string accName = pm.Account.Username.ToLowerInvariant().Trim();

            if (_rescueRoster.TryGetValue(accName, out List<FallenMercenaryData> list))
            {
                list.Remove(data);
                if (list.Count == 0) _rescueRoster.Remove(accName);
            }
        }
    }
}
