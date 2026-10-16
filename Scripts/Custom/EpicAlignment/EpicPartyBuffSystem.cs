using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Engines.PartySystem; // Подключаем нативную систему отрядов ServUO
using Server.Custom.EpicAlignment;

namespace Server.Custom.EpicPartyBuffs
{
    public static class EpicPartyBuffSystem
    {
        private static readonly Dictionary<Serial, string> ActiveAuras = new Dictionary<Serial, string>();

        public static void Initialize()
        {
            // Запускаем циклический радар отрядов раз в 3 секунды
            Timer.DelayCall(TimeSpan.FromSeconds(3.0), TimeSpan.FromSeconds(3.0), new TimerCallback(ProcessPartyRadar));
        }

        private static void ProcessPartyRadar()
        {
            HashSet<Serial> onlinePlayers = new HashSet<Serial>();
            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m is PlayerMobile pm && pm.Alive && !pm.Deleted && pm.NetState != null)
                {
                    onlinePlayers.Add(pm.Serial);
                    Party party = Party.Get(pm);

                    bool hasAvatarInParty = false;
                    bool hasMondainInParty = false;

                    if (party != null)
                    {
                        foreach (PartyMemberInfo info in party.Members)
                        {
                            Mobile member = info.Mobile;
                            if (member != null && member.Alive && member.Map == pm.Map && pm.InRange(member.Location, 12))
                            {
                                EpicRank rank = EpicAlignmentSystem.GetRank(member);
                                if (rank == EpicRank.Avatar) hasAvatarInParty = true;
                                if (rank == EpicRank.HandOfMondain) hasMondainInParty = true;
                            }
                        }
                    }

                    ApplyDynamicPartyBonus(pm, hasAvatarInParty, hasMondainInParty);
                }
            }

            List<Serial> stalePlayers = new List<Serial>();
            foreach (Serial serial in ActiveAuras.Keys)
            {
                if (!onlinePlayers.Contains(serial))
                    stalePlayers.Add(serial);
            }
            foreach (Serial serial in stalePlayers)
                ActiveAuras.Remove(serial);
        }

        private static void ApplyDynamicPartyBonus(PlayerMobile pm, bool hasAvatar, bool hasMondain)
        {
            string aura = hasAvatar ? (hasMondain ? "balance" : "avatar") : (hasMondain ? "mondain" : null);
            ActiveAuras.TryGetValue(pm.Serial, out string previousAura);

            if (aura == null)
            {
                ActiveAuras.Remove(pm.Serial);
                if (previousAura != null)
                    pm.SendMessage(0x59, "Эпическая групповая аура угасла.");
                return;
            }

            if (aura == "balance")
            {
                pm.Hits = Math.Min(pm.HitsMax, pm.Hits + 3);
                pm.Stam = Math.Min(pm.StamMax, pm.Stam + 5);
            }
            else if (aura == "avatar")
            {
                pm.Hits = Math.Min(pm.HitsMax, pm.Hits + 2);
            }
            else
                pm.Stam = Math.Min(pm.StamMax, pm.Stam + 4);

            if (aura != previousAura)
            {
                string message = aura == "balance"
                    ? "[Резонанс]: Ауры Аватара и Мондейна восстанавливают 3 здоровья и 5 выносливости каждые 3 секунды."
                    : aura == "avatar"
                        ? "[Аура Аватара]: Восстанавливается 2 здоровья каждые 3 секунды."
                        : "[Аура Мондейна]: Восстанавливается 4 выносливости каждые 3 секунды.";
                pm.SendMessage(aura == "mondain" ? 0x22 : 0x35, message);
                ActiveAuras[pm.Serial] = aura;
            }
        }
    }
}
