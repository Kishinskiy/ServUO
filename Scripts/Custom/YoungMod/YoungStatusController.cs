using Server.Mobiles;

namespace Server.Custom.Mechanics
{
    public static class YoungStatusController
    {
        public static void Initialize()
        {
            // Подписываемся на стандартный ивент движения (срабатывает при каждом шаге)
            EventSink.Movement += OnPlayerMovement;
        }

        private static void OnPlayerMovement(MovementEventArgs e)
        {
            var pm = e.Mobile as PlayerMobile;

            // Базовые проверки безопасности: только живые игроки со статусом Young
            if (pm == null || !pm.Alive || !pm.Young || pm.AccessLevel > AccessLevel.Player)
                return;

            var currentRegion = pm.Region;
            if (currentRegion == null)
                return;

            var isInsideHaven = false;
            var regionName = currentRegion.Name != null ? currentRegion.Name.ToLower() : "";

            // Проверяем, находится ли игрок сейчас в Haven
            if (regionName.Contains("haven") || regionName.Contains("newhaven") || regionName.Contains("new haven") ||
                currentRegion.IsPartOf("NewHaven") || currentRegion.IsPartOf("Haven")) isInsideHaven = true;

            // Условие 1: Игрок вышел за пределы Нью-Хейвена в мир Trammel пешком/на корабле
            var walkedOut = !isInsideHaven && pm.Map == Map.Trammel;

            // Условие 2: Игрок улетел через Moongate/Recall на другой материк (Felucca, Malas, Ilshenar и т.д.)
            var teleportedOut = pm.Map != Map.Trammel && pm.Map != Map.Internal;

            if (walkedOut || teleportedOut)
            {
                // Снимаем статус Young силами ядра ServUO
                pm.Young = false;

                // Мистический звук взросления персонажа
                pm.PlaySound(0x1F7);

                // Оповещения выводятся на русском языке благодаря нашему исправлению в World.cs!
                pm.SendMessage(0x22, "Вы покинули стартовый остров Нью-Хейвен и вступили во взрослый мир Сосарии.");
                pm.SendMessage(0x5D,
                    "Статус 'Young' автоматически снят. Будьте осторожны — теперь вы уязвимы для всех опасностей мира!");
            }
        }
    }
}
