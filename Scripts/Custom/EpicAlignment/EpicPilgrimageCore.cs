using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Commands;

namespace Server.Custom.EpicAlignment
{
    public static class EpicPilgrimageCore
    {
        public static void Initialize()
        {
            // Регистрируем короткую и удобную команду для игроков у Алтарей
            CommandSystem.Register("shrine", AccessLevel.Player, new CommandEventHandler(OnShrine_OnCommand));
        }

        private static void OnShrine_OnCommand(CommandEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            if (pm == null || pm.NetState == null) return;

            if (!pm.Alive)
            { pm.SendMessage(0x22, "Призраки не могут взывать к силе живых Добродетелей! Воскресните."); return; }

            // Проверяем, стоит ли игрок вплотную к Алтарю (ItemID 0x1183, 0x1184 или кастомный шрайн)
            bool standingAtAltar = false;
            Point3D altarLoc = pm.Location;

            IPooledEnumerable itemEnum = pm.Map.GetItemsInRange(pm.Location, 2);
            foreach (Item item in itemEnum)
            {
                string name = item.GetType().Name.ToLower();
                if (item.ItemID == 0x1183 || item.ItemID == 0x1184 || name.Contains("shrine"))
                {
                    standingAtAltar = true;
                    altarLoc = item.Location;
                    break;
                }
            }
            itemEnum.Free();

            if (!standingAtAltar)
            { pm.SendMessage(0x22, "Вы должны стоять вплотную к Алтарю Добродетелей, чтобы запечатать его силу!"); return; }

            string virtueType = GetVirtueTypeByLocation(altarLoc);
            EpicProfile profile = EpicAlignmentSystem.GetProfile(pm);
            
            if (profile == null) return;

            if (profile.Rank != EpicRank.None)
            { pm.SendMessage(0x35, "Вы уже обрели Высшую Силу. Алтари покорно склоняются перед Полубогом."); return; }

            if (!profile.DiscoveredShrines.Contains(virtueType))
            {
                profile.DiscoveredShrines.Add(virtueType);
                EpicAlignmentSystem.Save(); // Мгновенный сейв в JSON-файл

                pm.PlaySound(0x1F7); 
                Effects.SendLocationParticles(EffectItem.Create(pm.Location, pm.Map, TimeSpan.FromSeconds(1.5)), 0x375A, 9, 20, 2418, 0, 5027, 0);

                pm.SendMessage(0x35, $"[Паломничество] Сила Добродетели {virtueType.ToUpper()} успешно запечатана в вашу душу!");
                pm.SendMessage(0x59, $"Пройдено святынь: {profile.DiscoveredShrines.Count} из 8. Продолжайте кругосветное путешествие!");
            }
            else
            {
                pm.SendMessage(0x482, $"Вы уже сонастроены со святыней {virtueType.ToUpper()}. Энергия этого места стабильна.");
            }
        }

        private static string GetVirtueTypeByLocation(Point3D loc)
        {
            if (loc.X > 1400 && loc.X < 1500 && loc.Y > 1600 && loc.Y < 1700) return "compassion";
            if (loc.X > 2400 && loc.X < 2600 && loc.Y > 500 && loc.Y < 600) return "honesty";
            if (loc.X > 1900 && loc.X < 2000 && loc.Y > 2600 && loc.Y < 2700) return "honor";
            if (loc.X > 500 && loc.X < 650 && loc.Y > 2100 && loc.Y < 2200) return "humility";
            if (loc.X > 2800 && loc.X < 3100 && loc.Y > 700 && loc.Y < 950) return "justice";
            if (loc.X > 3300 && loc.X < 3500 && loc.Y > 2400 && loc.Y < 2600) return "sacrifice";
            if (loc.X > 4400 && loc.X < 4600 && loc.Y > 1000 && loc.Y < 1200) return "spirituality";
            return "valor"; 
        }
    }
}
