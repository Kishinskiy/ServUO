using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Gumps;
using Server.Network;
using Server.Commands; // 🔥 ДОБАВЛЕНО: Пространство имен для фикса ошибки CommandEventHandler! 🎉⚙️🛠️
using Server.Custom.CityDevelopment;

namespace Server.Custom.CityPolitics
{
    public class PoliticalData
    {
        public int InfluencePoints { get; set; } = 100; 
        public string ActiveBoycottTarget { get; set; } = "None";
        public DateTime DecreeExpiry { get; set; } = DateTime.MinValue;
    }

    public static class CityPoliticalGames
    {
        private static readonly Dictionary<string, PoliticalData> _politicsDB = new Dictionary<string, PoliticalData>();

        public static void Initialize()
        {
            EventSink.WorldSave += (e) => RecalculateInfluencePoints();
            
            CommandSystem.Register("GovernorPanel", AccessLevel.Player, new CommandEventHandler(GovernorPanel_OnCommand));
        }

        public static PoliticalData GetPolData(Server.Engines.CityLoyalty.City city, Map map)
        {
            if (map == null || map == Map.Internal) map = Map.Trammel;
            string key = $"{city}_{map.Name}";

            if (!_politicsDB.TryGetValue(key, out PoliticalData data))
            {
                data = new PoliticalData();
                _politicsDB[key] = data;
            }
            return data;
        }

        private static void RecalculateInfluencePoints()
        {
            foreach (Map map in Map.AllMaps)
            {
                if (map == null || map == Map.Internal) continue;
                foreach (Server.Engines.CityLoyalty.City c in Enum.GetValues(typeof(Server.Engines.CityLoyalty.City)))
                {
                    CityProgress progress = CityDevelopmentMod.GetCityProfile(c, map);
                    PoliticalData polData = GetPolData(c, map);

                    if (progress != null && polData != null)
                    {
                        int stockBonus = (progress.IronStock + progress.LeatherStock + progress.WoodStock) / 50;
                        polData.InfluencePoints += (progress.Level * 10) + stockBonus;
                    }
                }
            }
        }

        private static void GovernorPanel_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || !from.Alive) return;

            Server.Engines.CityLoyalty.City governedCity = Server.Engines.CityLoyalty.City.Britain;
            bool isGovernor = false;

            foreach (Server.Engines.CityLoyalty.City c in Enum.GetValues(typeof(Server.Engines.CityLoyalty.City)))
            {
                var system = Server.Engines.CityLoyalty.CityLoyaltySystem.GetCityInstance(c);
                
                // 🔥 ИСПРАВЛЕНО: Заменено Leader на нативное свойство Governor твоей версии ServUO! 🏛️⚙️
                if (system != null && system.Governor == from)
                {
                    governedCity = c;
                    isGovernor = true;
                    break;
                }
            }

            if (!isGovernor && from.AccessLevel < AccessLevel.GameMaster)
            {
                from.SendMessage(0x22, "Этот пульт интриг доступен только действующему Губернаторы города!");
                return;
            }

            from.CloseGump(typeof(GovernorPoliticsGump));
            from.SendGump(new GovernorPoliticsGump(from, governedCity));
        }
    }

    public class GovernorPoliticsGump : Gump
    {
        private Server.Engines.CityLoyalty.City m_City;

        public GovernorPoliticsGump(Mobile from, Server.Engines.CityLoyalty.City city) : base(100, 100)
        {
            m_City = city;
            PoliticalData polData = CityPoliticalGames.GetPolData(city, from.Map);

            AddPage(0);
            AddBackground(0, 0, 450, 340, 0x13BE); 
            AddImageTiled(10, 10, 430, 20, 0xA40);
            AddLabel(20, 10, 0x35, $"КОЛЛЕГИЯ ГУБЕРНАТОРА: {city.ToString().ToUpper()}");
            
            AddLabel(20, 40, 0x58, $"Доступно Очков Влияния (PIP): {polData.InfluencePoints}");
            AddLabel(20, 60, 0x3B2, $"Активный Бойкот: {polData.ActiveBoycottTarget}");

            AddBackground(15, 90, 420, 50, 0x24AE);
            AddLabel(25, 105, 0x58, "📜 УКАЗ: Субсидии ремесленников (Цена: 30 PIP)");
            AddButton(390, 105, 0x254F, 0x2551, 1, GumpButtonType.Reply, 0);

            AddBackground(15, 150, 420, 50, 0x24AE);
            AddLabel(25, 165, 0x22, "⚔️ ИНТРИГА: Экономический Бойкот (Цена: 50 PIP)");
            AddButton(390, 165, 0x254F, 0x2551, 2, GumpButtonType.Reply, 0);

            AddBackground(15, 210, 420, 50, 0x24AE);
            AddLabel(25, 225, 0x35, "🛡️ ВОЕННЫЙ ПРИКАЗ: Нанять Охрану (Цена: 60 PIP)");
            AddButton(390, 225, 0x254F, 0x2551, 3, GumpButtonType.Reply, 0);
            
            AddLabel(15, 280, 0x5D, "Очки влияния растут от объема заполненности городских складов.");
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;
            if (from == null || !from.Alive) return;

            PoliticalData polData = CityPoliticalGames.GetPolData(m_City, from.Map);
            CityProgress progress = CityDevelopmentMod.GetCityProfile(m_City, from.Map);

            if (polData == null || progress == null) return;

            if (info.ButtonID == 1) 
            {
                if (polData.InfluencePoints < 30) { from.SendMessage(0x22, "Недостаточно очков политического влияния!"); return; }
                
                polData.InfluencePoints -= 30;
                polData.DecreeExpiry = DateTime.UtcNow + TimeSpan.FromDays(1.0);
                
                from.PlaySound(0x249);
                string msg = $"[Указ]: Губернатор города {m_City} объявил золотые субсидии для мастеров! Выплаты на доске объявлений временно повышены!";
                Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x35, msg);
            }
            else if (info.ButtonID == 2) 
            {
                if (polData.InfluencePoints < 50) { from.SendMessage(0x22, "Недостаточно очков влияния для плетения интриг!"); return; }
                
                polData.InfluencePoints -= 50;
                polData.ActiveBoycottTarget = "Minoc"; 
                
                from.PlaySound(0x5C3);
                string msg = $"[Интриги]: Пронырливый Губернатор {m_City} ввел жесткие торговые пошлины и объявил Экономический Бойкот против Минока!";
                Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x22, msg);
            }
            else if (info.ButtonID == 3) 
            {
                if (polData.InfluencePoints < 60) { from.SendMessage(0x22, "Недостаточно влияния для вызова личной гвардии!"); return; }
                if (progress.Treasury < 50000) { from.SendMessage(0x22, "В казне города нет денег на оплату контракта наемников (нужно 50к)!"); return; }

                polData.InfluencePoints -= 60;
                progress.Treasury -= 50000; 

                for (int i = 0; i < 2; i++)
                {
                    DreadCardGuard guard = new DreadCardGuard();
                    guard.MoveToWorld(from.Location, from.Map);
                }

                from.PlaySound(0xFB); 
                from.SendMessage(0x35, "Контракт подписан! Двое элитных наемных гвардейцев прибыли на защиту площади!");
            }
        }
    }

    public class DreadCardGuard : BaseCreature
    {
        public DreadCardGuard() : base(AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "Элитный Гвардеец Наемник";
            Title = "Защитник Казны";
            Body = 400;
            Hue = 1102;
            Blessed = false;
            
            SetStr(250); SetDex(120); SetInt(60); SetHits(600);
            SetDamage(18, 26);
            
            SetSkill(SkillName.Swords, 100.0); SetSkill(SkillName.Tactics, 100.0); SetSkill(SkillName.Parry, 90.0);
            
            AddItem(new PlateChest() { Hue = 1109, Movable = false });
            AddItem(new VikingSword() { Movable = false });
            AddItem(new ChaosShield() { Movable = false });
            
            CantWalk = false;
        }

        public DreadCardGuard(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}