using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Network;

namespace Server.Items
{
    public static class EpicMantraCore
    {
        private static Dictionary<Serial, DateTime> _speechCooldown = new Dictionary<Serial, DateTime>();

        public static void Initialize() { EventSink.Speech += OnPlayerSpeech; }

        private static void OnPlayerSpeech(SpeechEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            if (pm == null || !pm.Alive || pm.NetState == null || string.IsNullOrEmpty(e.Speech)) return;

            Point3D altarLoc = new Point3D(1903, 2087, 10);
            if (pm.Map != Map.Felucca || pm.GetDistanceToSqrt(altarLoc) > 5.0) return;

            string text = e.Speech.ToLower().Trim();
            bool isLight = text == "i am avatar" || text == "я аватар";
            bool isDark = text == "i am mondain" || text == "я рука мондейна" || text == "я мондейн";

            if (!isLight && !isDark) return;
            e.Blocked = true;

            if (_speechCooldown.TryGetValue(pm.Serial, out DateTime ready) && DateTime.UtcNow < ready) return;
            _speechCooldown[pm.Serial] = DateTime.UtcNow + TimeSpan.FromSeconds(1.0);

            if (Server.Custom.EpicAlignment.EpicAlignmentSystem.GetRank(pm) != Server.Custom.EpicAlignment.EpicRank.None) 
            { pm.SendMessage(0x35, "Мантра отражается эхом..."); return; }

            if (isLight && pm.Skills[SkillName.Necromancy].Value >= 50.0) { pm.SendMessage(0x22, "Некромант не может стать Аватаром!"); return; }
            if (isDark && pm.Skills[SkillName.Chivalry].Value >= 50.0) { pm.SendMessage(0x22, "Паладин не может присягнуть Тьме!"); return; }

            // ФИКС: Передаем isLight (выбранную сторону Света/Тьмы) в проверку и запуск
            if (!VerifyRequirements(pm, altarLoc, isLight)) return;

            Item fire = new Item(0x3709) { Hue = isLight ? 2418 : 1175, Movable = false };
            fire.MoveToWorld(new Point3D(altarLoc.X, altarLoc.Y, altarLoc.Z + 1), Map.Felucca);
            Timer.DelayCall(TimeSpan.FromSeconds(180.0), () => fire.Delete());

            pm.PlaySound(0x243);
            pm.SendMessage(0x59, "Древние узы Пагана сожжены! У вас есть 6 секунд форы до удара первой стихии...");
            
            // КВЕСТ АКТИВИРОВАН: Сторона зафиксирована раз и навсегда, больше Карма не считывается!
            Timer.DelayCall(TimeSpan.FromSeconds(6.0), () => { if (pm.Alive) EpicWaveSpawner.StartBattle(pm, altarLoc, isLight); });
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
                beacon.PublicOverheadMessage(MessageType.Regular, 0x59, false, "== ВЕЛИКОЕ ПРОРОЧЕСТВО СТИХИЙ ==");
                Timer.DelayCall(TimeSpan.FromMilliseconds(400), () => beacon.PublicOverheadMessage(MessageType.Regular, earth != null ? 0x35 : 0x22, false, earth != null ? "• Твердыня Литоса укрощена" : "• Земля содрогается... Сердце Литоса не найдено!"));
                Timer.DelayCall(TimeSpan.FromMilliseconds(800), () => beacon.PublicOverheadMessage(MessageType.Regular, flame != null ? 0x35 : 0x22, false, flame != null ? "• Ярость Пироса заперта" : "• Огонь требует крови... Язык Пироса не пожертвован!"));
                Timer.DelayCall(TimeSpan.FromMilliseconds(1200), () => beacon.PublicOverheadMessage(MessageType.Regular, seas != null ? 0x35 : 0x22, false, seas != null ? "• Бездны Гидроса высохли" : "• Воды штормят... Слеза Гидроса скрыта!"));
                Timer.DelayCall(TimeSpan.FromMilliseconds(1600), () => beacon.PublicOverheadMessage(MessageType.Regular, wind != null ? 0x35 : 0x22, false, wind != null ? "• Вихри Стратоса затихли" : "• Ветры безумствуют... Дыхание Стратоса упущено!"));
                return false;
            }
            earth.Delete(); flame.Delete(); seas.Delete(); wind.Delete();
            beacon.PublicOverheadMessage(MessageType.Regular, isLight ? 0x35 : 0x22, false, "[Жертва принята! Четыре Титана Пагана низвергнуты!]"); return true;
        }
    }
}
