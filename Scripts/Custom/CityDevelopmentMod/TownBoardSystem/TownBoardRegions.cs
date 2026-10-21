using System;
using System.Collections.Generic;
using Server.Engines.CityLoyalty;

namespace Server.Custom.TownBoards
{
    public static class TownBoardRegions
    {
        // Дополнительные регионы для охоты
        public static string[] GetAdditionalHuntRegions(City city)
        {
            switch (city)
            {
                case City.Moonglow:     return new[] { "Deceit" };
                case City.Britain:      return new[] { "Despise", "Despise Entrance", "Despise Passage", "Painted Caves", "Orc Cave" };
                case City.Jhelom:       return new[] { "Jhelom Islands" };
                case City.Yew:          return new[] { "Shame", "Heartwood", "Sanctuary", "Blighted Grove", "Orc Cave", "Despise", "Despise Entrance", "Despise Passage", "Yew Cave", "Sanctuary Entrance" };
                case City.Minoc:        return new[] { "Covetous", "Wrong" };
                case City.Trinsic:      return new[] { "Destard" };
                case City.NewMagincia:  return new[] { "Ocllo" };
                case City.Vesper:       return new[] { "Covetous" };
                case City.SkaraBrae:
                default:                return new string[0];
            }
        }

        // Цели для охоты по городам
        public static TownHuntTarget[] GetHuntTargets(City city)
        {
            switch (city)
            {
                case City.Moonglow:
                    return new[]
                    {
                        new TownHuntTarget("Skeleton", "скелетов"),
                        new TownHuntTarget("Zombie", "зомби"),
                        new TownHuntTarget("Ghoul", "гулей"),
                        new TownHuntTarget("Wraith", "призраков"),
                        new TownHuntTarget("Lich", "личей"),
                        new TownHuntTarget("Mongbat", "монгбатов")
                    };
                case City.Britain:
                    return new[]
                    {
                        new TownHuntTarget("Ettin", "эттинов"),
                        new TownHuntTarget("Ogre", "огров"),
                        new TownHuntTarget("Troll", "троллей"),
                        new TownHuntTarget("Orc", "орков"),
                        new TownHuntTarget("Ratman", "крысолюдов"),
                        new TownHuntTarget("Skeleton", "скелетов"),
                        new TownHuntTarget("Lizardman", "ящеролюдов")
                    };
                case City.Jhelom:
                    return new[]
                    {
                        new TownHuntTarget("Brigand", "разбойников"),
                        new TownHuntTarget("Mongbat", "монгбатов"),
                        new TownHuntTarget("Scorpion", "скорпионов"),
                        new TownHuntTarget("Orc", "орков")
                    };
                case City.Yew:
                    return new[]
                    {
                        new TownHuntTarget("Orc", "орков"),
                        new TownHuntTarget("Ratman", "крысолюдов"),
                        new TownHuntTarget("EarthElemental", "земляных элементалей"),
                        new TownHuntTarget("AirElemental", "воздушных элементалей"),
                        new TownHuntTarget("Lich", "личей"),
                        new TownHuntTarget("Ogre", "огров"),
                        new TownHuntTarget("Ettin", "эттинов"),
                        new TownHuntTarget("Troll", "троллей")
                    };
                case City.Minoc:
                    return new[]
                    {
                        new TownHuntTarget("Harpy", "гарпий"),
                        new TownHuntTarget("Gazer", "глаза-демона"),
                        new TownHuntTarget("Ogre", "огров"),
                        new TownHuntTarget("Ettin", "эттинов"),
                        new TownHuntTarget("Ratman", "крысолюдов"),
                        new TownHuntTarget("Lich", "личей")
                    };
                case City.Trinsic:
                    return new[]
                    {
                        new TownHuntTarget("Dragon", "драконов"),
                        new TownHuntTarget("Drake", "дрейков"),
                        new TownHuntTarget("Lizardman", "ящеролюдов"),
                        new TownHuntTarget("Ogre", "огров"),
                        new TownHuntTarget("Ettin", "эттинов")
                    };
                case City.SkaraBrae:
                    return new[]
                    {
                        new TownHuntTarget("Brigand", "разбойников"),
                        new TownHuntTarget("Mongbat", "монгбатов"),
                        new TownHuntTarget("Skeleton", "скелетов"),
                        new TownHuntTarget("Orc", "орков")
                    };
                case City.NewMagincia:
                    return new[]
                    {
                        new TownHuntTarget("Mongbat", "монгбатов"),
                        new TownHuntTarget("Brigand", "разбойников"),
                        new TownHuntTarget("Orc", "орков"),
                        new TownHuntTarget("Scorpion", "скорпионов"),
                        new TownHuntTarget("Skeleton", "скелетов")
                    };
                case City.Vesper:
                    return new[]
                    {
                        new TownHuntTarget("Harpy", "гарпий"),
                        new TownHuntTarget("Gazer", "глаза-демона"),
                        new TownHuntTarget("Ogre", "огров"),
                        new TownHuntTarget("Ettin", "эттинов"),
                        new TownHuntTarget("Ratman", "крысолюдов"),
                        new TownHuntTarget("Orc", "орков")
                    };
                default:
                    return new TownHuntTarget[0];
            }
        }
    }
}
