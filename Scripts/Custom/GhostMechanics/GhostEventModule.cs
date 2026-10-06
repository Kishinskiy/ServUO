using System;
using Server;
using Server.Mobiles;
using Server.Engines.XmlSpawner2;

namespace Server.Misc
{
    public class GhostEventModule
    {
        public static void Initialize()
        {
            EventSink.PlayerDeath += OnPlayerDeath;
        }

        private static void OnPlayerDeath(PlayerDeathEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            if (pm == null) return;

            XmlValue pointsData = XmlAttach.FindAttachment(pm, typeof(XmlValue), "EtherealEssence") as XmlValue;
            int currentEssence = pointsData != null ? pointsData.Value : 0;

            pm.SendMessage(0x482, $"Вы вошли в мир духов. Ваша призрачная эссенция: {currentEssence}.");
            pm.SendMessage(0x482, "Ищите эфирные аномалии, чтобы накопить энергию для телепортации.");
        }
    }
}
