using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Network;
using Server.Custom.EpicAlignment; 

namespace Server.Items
{
    public static class EpicMantraCore
    {
        public static readonly Point3D TrialAltarLocation = new Point3D(1234, 567, 8);
        private static Dictionary<Serial, DateTime> _speechCooldown = new Dictionary<Serial, DateTime>();

        public static void Initialize() { EventSink.Speech += OnPlayerSpeech; }

        private static void OnPlayerSpeech(SpeechEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            if (pm == null || !pm.Alive || pm.NetState == null || string.IsNullOrEmpty(e.Speech)) return;

            Point3D altarLoc = TrialAltarLocation;
            if (pm.Map != Map.Felucca || pm.GetDistanceToSqrt(altarLoc) > 5.0) return;

            string text = e.Speech.ToLower().Trim();
            bool isLight = text == "i am avatar" || text == "я аватар";
            bool isDark = text == "i am mondain" || text == "я рука мондейна" || text == "я мондейн";

            if (!isLight && !isDark) return;
            e.Blocked = true;

            if (_speechCooldown.TryGetValue(pm.Serial, out DateTime ready) && DateTime.UtcNow < ready) return;
            _speechCooldown[pm.Serial] = DateTime.UtcNow + TimeSpan.FromSeconds(1.0);

            if (EpicAlignmentSystem.GetRank(pm) != EpicRank.None) 
            { pm.SendMessage(0x35, "Мантра отражается эхом от стен... Вы уже познали Истинную Силу и не можете запустить Алтарь повторно!"); return; }

            // =========================================================================
            // 🏛️ НАДЁЖНАЯ ПРОВЕРКА НА ТИПЫ ДАННЫХ ДОБРОДЕТЕЛЕЙ ЯДРА ⭐
            // =========================================================================
            int missingShrines = 0;
            
            // Проверяем все 8 добродетелей по их индексам, чтобы избежать конфликтов пространств имен [1]
            for (int i = 0; i < 8; i++)
            {
                if (pm.Virtues.GetValue(i) < 2000) // В ядре ServUO 2000+ очков — это первая зажженная звезда Seeker [1]
                {
                    missingShrines++;
                }
            }

            if (missingShrines > 0)
            {
                pm.SendMessage(0x22, $"[Отказ] Древний Алтарь Хаоса отвергает твою мантру! Твоя душа не готова.");
                pm.SendMessage(0x22, $"Вы обязаны активировать мантры у Алтарей и зажечь минимум по ОДНОЙ звезде для всех 8 Добродетелей! Недостает святынь: {missingShrines} из 8.");
                pm.SendMessage(0x59, "Используйте встроенное меню Добродетелей ClassicUO или команду [CheckEpicStatus для проверки.");
                return;
            }
            // =========================================================================

            if (isLight && pm.Skills[SkillName.Necromancy].Value >= 50.0) { pm.SendMessage(0x22, "Некромант не может стать Аватаром!"); return; }
            if (isDark && pm.Skills[SkillName.Chivalry].Value >= 50.0) { pm.SendMessage(0x22, "Паладин не может присягнуть Тьме!"); return; }

            Item checkBeacon = new Item(0x1183) { Name = "Altar of Chaos", Movable = false, Visible = false };
            checkBeacon.MoveToWorld(new Point3D(altarLoc.X, altarLoc.Y, altarLoc.Z + 2), Map.Felucca);
            Timer.DelayCall(TimeSpan.FromSeconds(5.0), () => checkBeacon.Delete());

            if (isLight && EpicAlignmentSystem.IsAvatarActive())
            {
                checkBeacon.PublicOverheadMessage(MessageType.Regular, 0x22, false, "[Место Воплощения Добродетелей уже занято другим Аватаром Света!]");
                return;
            }
            if (isDark && EpicAlignmentSystem.IsMondainActive())
            {
                checkBeacon.PublicOverheadMessage(MessageType.Regular, 0x22, false, "[Престол Тьмы уже занят другим Наместником Мондейна!]");
                return;
            }

            if (!EpicWaveSpawner.TryReserveBattle(pm, isLight))
            {
                pm.SendMessage(0x22, "Испытание вашей фракции уже идет у Алтаря Хаоса. Испытание противоположной фракции может проходить одновременно.");
                return;
            }

            if (!VerifyRequirements(pm, altarLoc, isLight))
            {
                EpicWaveSpawner.CancelBattleReservation(pm);
                return;
            }

            Item fire = new Item(0x3709) { Hue = isLight ? 2418 : 1175, Movable = false };
            fire.MoveToWorld(new Point3D(altarLoc.X, altarLoc.Y, altarLoc.Z + 1), Map.Felucca);
            Timer.DelayCall(TimeSpan.FromSeconds(180.0), () => fire.Delete());

            pm.PlaySound(0x243);
            pm.SendMessage(0x59, "Осколки поглощены! Через 6 секунд начнется финальное испытание у Алтаря Хаоса.");
            
            Timer.DelayCall(TimeSpan.FromSeconds(6.0), () =>
            {
                if (!pm.Deleted && pm.Alive && pm.Map == Map.Felucca)
                    EpicWaveSpawner.StartBattle(pm, altarLoc, isLight, fire);
                else
                {
                    EpicWaveSpawner.CancelBattleReservation(pm);
                    if (!pm.Deleted)
                        pm.SendMessage(0x22, "Испытание не началось: инициатор должен оставаться живым в мире до начала волн.");
                }
            });
        }

        private static bool VerifyRequirements(PlayerMobile pm, Point3D altarLoc, bool isLight)
        {
            Item beacon = new Item(0x1183) { Name = "Altar of Chaos", Movable = false, Visible = false };
            beacon.MoveToWorld(new Point3D(altarLoc.X, altarLoc.Y, altarLoc.Z + 2), Map.Felucca);
            Timer.DelayCall(TimeSpan.FromSeconds(15.0), () => beacon.Delete());

            if (isLight && pm.Karma < 15000) { beacon.PublicOverheadMessage(MessageType.Regular, 0x22, false, "[Твой дух нечист. Храм отвергает тебя!]"); return false; }
            if (!isLight && pm.Karma > -15000) { beacon.PublicOverheadMessage(MessageType.Regular, 0x22, false, "[В тебе мало скверны. Ты недостоин Тьмы!]"); return false; }

            Container pack = pm.Backpack; if (pack == null) return false;
            Item earth = pack.FindItemByType(typeof(HeartOfEarth)); Item flame = pack.FindItemByType(typeof(TongueOfFlame));
            Item seas = pack.FindItemByType(typeof(TearOfSeas)); Item wind = pack.FindItemByType(typeof(BreathOfWind));

            if (earth == null || flame == null || seas == null || wind == null)
            {
                beacon.PublicOverheadMessage(MessageType.Regular, 0x59, false, "== ПРОРОЧЕСТВО ПРОТИВОСТОЯНИЯ ==");
                Timer.DelayCall(TimeSpan.FromMilliseconds(400), () => beacon.PublicOverheadMessage(MessageType.Regular, earth != null ? 0x35 : 0x22, false, earth != null ? "• Осколок Первого Источника запечатан" : "• Первый Осколок Предназначения утерян!"));
                Timer.DelayCall(TimeSpan.FromMilliseconds(800), () => beacon.PublicOverheadMessage(MessageType.Regular, flame != null ? 0x35 : 0x22, false, flame != null ? "• Осколок Второго Источника запечатан" : "• Второй Осколок Предназначения утерян!"));
                Timer.DelayCall(TimeSpan.FromMilliseconds(1200), () => beacon.PublicOverheadMessage(MessageType.Regular, seas != null ? 0x35 : 0x22, false, seas != null ? "• Осколок Третьего Источника запечатан" : "• Третий Осколок Предназначения утерян!"));
                Timer.DelayCall(TimeSpan.FromMilliseconds(1600), () => beacon.PublicOverheadMessage(MessageType.Regular, wind != null ? 0x35 : 0x22, false, wind != null ? "• Осколок Четвертого Источника запечатан" : "• Четвертый Осколок Предназначения утерян!"));
                return false;
            }
            earth.Delete(); flame.Delete(); seas.Delete(); wind.Delete();
            beacon.PublicOverheadMessage(MessageType.Regular, isLight ? 0x35 : 0x22, false, "[Жертва принята! Измерения Полюсов разрываются!]"); return true;
        }
    }
}
