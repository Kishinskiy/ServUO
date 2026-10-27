using System;
using Server;
using Server.Commands;
using Server.Mobiles;
using Server.Items;
using Server.Engines.CityLoyalty;

namespace Server.Commands
{
    public class ForceSpawnFeluccaNPC
    {
        public static void Initialize()
        {
            // Регистрируем нашу администраторскую команду
            CommandSystem.Register("SpawnFeluccaGovernment", AccessLevel.Administrator, new CommandEventHandler(OnCommand));
        }

        private static void OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null) return;

            int ministersSpawned = 0;
            int heraldsSpawned = 0;
            int stonesSpawned = 0;
            int boardsSpawned = 0;

            // Цикл строго по всем 9 фракционным городам из оригинального Enum
            foreach (City city in Enum.GetValues(typeof(City)))
            {
                CityLoyaltySystem sys = CityLoyaltySystem.GetCityInstance(city);
                if (sys == null || sys.Definition == null) continue;

                // 1. Проверяем наличие Торгового Министра СТРОГО в Felucca
                bool ministerExists = false;
                IPooledEnumerable eable = Map.Felucca.GetMobilesInRange(sys.Definition.TradeMinisterLocation, 5);
                foreach (Mobile m in eable)
                {
                    if (m is TradeMinister && ((TradeMinister)m).City == city)
                    {
                        ministerExists = true;
                        break;
                    }
                }
                eable.Free();

                if (!ministerExists)
                {
                    TradeMinister minister = new TradeMinister(city);
                    minister.MoveToWorld(sys.Definition.TradeMinisterLocation, Map.Felucca);
                    ministersSpawned++;
                }

                // 2. Проверяем наличие Глашатая (NPC-Губернатора) СТРОГО в Felucca
                bool heraldExists = false;
                IPooledEnumerable eableH = Map.Felucca.GetMobilesInRange(sys.Definition.HeraldLocation, 5);
                foreach (Mobile m in eableH)
                {
                    if (m is CityHerald && ((CityHerald)m).City == city)
                    {
                        heraldExists = true;
                        break;
                    }
                }
                eableH.Free();

                if (!heraldExists)
                {
                    CityHerald herald = new CityHerald(city);
                    herald.MoveToWorld(sys.Definition.HeraldLocation, Map.Felucca);
                    heraldsSpawned++;
                }

                // 3. Проверяем наличие Городского Камня (CityStone) СТРОГО в Felucca
                bool stoneExists = false;
                IPooledEnumerable eableItems = Map.Felucca.GetItemsInRange(sys.Definition.StoneLocation, 2);
                foreach (Item item in eableItems)
                {
                    if (item is CityStone && ((CityStone)item).City == sys)
                    {
                        stoneExists = true;
                        break;
                    }
                }
                eableItems.Free();

                if (!stoneExists)
                {
                    CityStone stone = new CityStone(sys);
                    stone.MoveToWorld(sys.Definition.StoneLocation, Map.Felucca);
                    stonesSpawned++;
                }

                // 4. Проверяем наличие Доски Объявлений (CityMessageBoard) СТРОГО в Felucca
                bool boardExists = false;
                IPooledEnumerable eableBoards = Map.Felucca.GetItemsInRange(sys.Definition.BoardLocation, 2);
                foreach (Item item in eableBoards)
                {
                    if (item is CityMessageBoard && ((CityMessageBoard)item).City == city)
                    {
                        boardExists = true;
                        break;
                    }
                }
                eableBoards.Free();

                if (!boardExists)
                {
                    CityMessageBoard board = new CityMessageBoard(city, 0xA0C5);
                    board.MoveToWorld(sys.Definition.BoardLocation, Map.Felucca);
                    boardsSpawned++;
                }
            }

            // Выводим админу подробный отчет в логи чата
            from.SendMessage(0x42, $"[Felucca Полная Инфраструктура]:");
            from.SendMessage(0x58, $"-> Заспавнено камней городов: {stonesSpawned}");
            from.SendMessage(0x58, $"-> Заспавнено досок объявлений: {boardsSpawned}");
            from.SendMessage(0x58, $"-> Восстановлено NPC министров: {ministersSpawned}");
            from.SendMessage(0x58, $"-> Восстановлено NPC глашатаев: {heraldsSpawned}");
        }
    }
}
