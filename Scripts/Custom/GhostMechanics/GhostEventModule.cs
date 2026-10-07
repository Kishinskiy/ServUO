using Server.Engines.XmlSpawner2;
using Server.Mobiles;

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
            var pm = e.Mobile as PlayerMobile;
            if (pm == null) return;

            var pointsData = XmlAttach.FindAttachment(pm, typeof(XmlValue), "EtherealEssence") as XmlValue;
            var currentEssence = pointsData != null ? pointsData.Value : 0;

            pm.SendMessage(0x482, $"Вы вошли в мир духов. Ваша призрачная эссенция: {currentEssence}.");
            pm.SendMessage(0x482, "Ищите эфирные аномалии, чтобы накопить энергию для телепортации.");
        }
    }
}
