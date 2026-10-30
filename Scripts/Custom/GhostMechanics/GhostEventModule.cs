using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Server.Mobiles;
using Server.Items;
using Server.Commands;
using Server.Gumps;
using Server.Network;
using Server.Engines.XmlSpawner2;

namespace Server.Misc
{
    public static class GhostEventModule
    {
        public static readonly TimeSpan PermaDeathDuration = TimeSpan.FromDays(3.0);
        public static readonly Point3D PurgatoryLoc = new Point3D(6430, 1236, 10); // Координаты Чистилища

        private static readonly Dictionary<Mobile, Timer> _permaDeathTimers = new Dictionary<Mobile, Timer>();

        public static void Initialize()
        {
            EventSink.PlayerDeath += OnPlayerDeath;
            EventSink.Movement += OnPlayerMovement;
            EventSink.Login += OnPlayerLogin;

            CommandSystem.Register("gt", AccessLevel.Player, new CommandEventHandler(GhostTravel_OnCommand));
        }

        private static void OnPlayerDeath(PlayerDeathEventArgs e)
        {
            PlayerMobile pm = e.Mobile as PlayerMobile;
            if (pm == null) return;

            // Нативная детекция Аватара по капу Magery
            bool isAvatar = pm.Skills[SkillName.Magery].Cap >= 130.0;

            XmlValue pointsData = XmlAttach.FindAttachment(pm, typeof(XmlValue), "EtherealEssence") as XmlValue;
            int currentEssence = pointsData != null ? pointsData.Value : 0;

            StartPermaDeathTimer(pm, true);

            if (isAvatar)
            {
                pm.SendMessage(0x22, "=== ВЫ ПАЛИ В БОЮ И ВЕНЧАНЫ С ЧИСТИЛИЩЕМ ===");
                pm.SendMessage(0x482, $"Ваша текущая призрачная эссенция: {currentEssence} / {EtherealAnomaly.MaxEssence}.");
                pm.SendMessage(0x482, "Поглощайте Эфирные Аномалии на кладбищах Felucca для восстановления сил.");
            }
            else
            {
                pm.SendMessage(0x482, $"Вы вошли в мир духов. Напишите [gt для открытия путей эфирного перемещения.");
                Timer.DelayCall(TimeSpan.FromSeconds(1.0), () =>
                {
                    if (pm != null && !pm.Alive && pm.NetState != null)
                    {
                        pm.CloseGump(typeof(EtherealTravelGump));
                        pm.SendGump(new EtherealTravelGump(pm));
                    }
                });
            }
        }

        private static void OnPlayerLogin(LoginEventArgs e)
        {
            Mobile m = e.Mobile;
            if (m == null) return;

            if (m.Alive)
                StopPermaDeathTimer(m, true);
            else
                StartPermaDeathTimer(m as PlayerMobile, false);
        }

        private static void StartPermaDeathTimer(PlayerMobile mobile, bool newDeath)
        {
            if (mobile == null || mobile.Deleted || mobile.Alive) return;

            GhostPermaDeathAttachment deathData = XmlAttach.FindAttachment(
                mobile, typeof(GhostPermaDeathAttachment), "GhostPermaDeath") as GhostPermaDeathAttachment;

            if (deathData == null)
            {
                deathData = new GhostPermaDeathAttachment();
                XmlAttach.AttachTo(mobile, deathData);
            }

            if (newDeath || deathData.DeathTimeUtc == DateTime.MinValue)
                deathData.DeathTimeUtc = DateTime.UtcNow;

            if (_permaDeathTimers.TryGetValue(mobile, out Timer oldTimer))
                oldTimer.Stop();

            TimeSpan remaining = PermaDeathDuration - (DateTime.UtcNow - deathData.DeathTimeUtc);
            if (remaining <= TimeSpan.Zero)
            {
                ExecutePermaDeath(mobile);
                return;
            }

            _permaDeathTimers[mobile] = Timer.DelayCall(remaining, () => ExecutePermaDeath(mobile));

            if (newDeath)
                mobile.SendMessage(0x22, $"[ВНИМАНИЕ] Ваша призрачная оболочка нестабильна! Если вы не воскреснете в течение 3 дней (72 ч), ваш персонаж полностью растворится в астрале.");
        }

        private static void StopPermaDeathTimer(Mobile mobile, bool notify)
        {
            if (mobile == null || !mobile.Alive) return;

            bool hadTimer = _permaDeathTimers.TryGetValue(mobile, out Timer timer);
            if (hadTimer)
            {
                timer.Stop();
                _permaDeathTimers.Remove(mobile);
            }

            GhostPermaDeathAttachment deathData = XmlAttach.FindAttachment(
                mobile, typeof(GhostPermaDeathAttachment), "GhostPermaDeath") as GhostPermaDeathAttachment;
            if (deathData != null && deathData.DeathTimeUtc != DateTime.MinValue)
            {
                deathData.DeathTimeUtc = DateTime.MinValue;
                hadTimer = true;
            }

            if (notify && hadTimer)
                mobile.SendMessage(0x35, "Ваша материальная оболочка восстановлена. Таймер растворения души успешно остановлен.");
        }

        public static void OnPlayerResurrected(PlayerMobile mobile)
        {
            StopPermaDeathTimer(mobile, true);
        }

        private static void ExecutePermaDeath(Mobile m)
        {
            if (m == null) return;

            if (m.Deleted)
            {
                _permaDeathTimers.Remove(m);
                return;
            }

            if (m.Alive)
            {
                _permaDeathTimers.Remove(m);
                GhostPermaDeathAttachment deathData = XmlAttach.FindAttachment(
                    m, typeof(GhostPermaDeathAttachment), "GhostPermaDeath") as GhostPermaDeathAttachment;
                if (deathData != null)
                    deathData.DeathTimeUtc = DateTime.MinValue;
                return;
            }

            _permaDeathTimers.Remove(m);

            string name = m.Name;
            string accName = (m.Account != null) ? m.Account.Username : "Unknown";

            Console.WriteLine($"[PermaDeath] Персонаж {name} (Аккаунт: {accName}) провёл в мире духов более 3 дней и был удален!");
            World.Broadcast(0x22, true, $"[Посмертие] Душа призрака {name} растворилась в эфире... Персонаж стерт из летописей мира!");

            m.Title = null;
            m.HueMod = -1;
            m.Skills[SkillName.Magery].Cap = 120.0;
            m.Skills[SkillName.EvalInt].Cap = 120.0;

            m.Delete();
        }

        private static void OnPlayerMovement(MovementEventArgs e)
        {
            Mobile m = e.Mobile;
            if (m == null || m.AccessLevel > AccessLevel.Player) return;

            if (m.Alive && _permaDeathTimers.ContainsKey(m))
                StopPermaDeathTimer(m, true);

            bool isAvatar = m.Skills[SkillName.Magery].Cap >= 130.0;
            if (!isAvatar) return;

            if (m.Alive && m.Map == Map.Felucca && m.GetDistanceToSqrt(PurgatoryLoc) <= 100.0)
            {
                m.Kill();
                m.SendMessage(0x22, "Высшие силы Посмертия карают за попытку ложного воскрешения! Используйте только Врата Перерождения.");
            }
        }

        private static void GhostTravel_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null) return;
            if (from.Alive) { from.SendMessage("Вы должны быть мертвы, чтобы использовать пути эфирного перемещения."); return; }

            from.CloseGump(typeof(EtherealTravelGump));
            from.SendGump(new EtherealTravelGump(from));
        }
    }

    // =========================================================================
    // 🌌 ИНТЕРФЕЙС ЭФИРНОГО ПЕРЕМЕЩЕНИЯ ДЛЯ ПРИЗРАКОВ КЛАДБИЩ (СИНХРОНИЗИРОВАННЫЙ)
    // =========================================================================
    public class EtherealTravelGump : Gump
    {
        private Mobile m_From;
        private List<EtherealShrine> m_AvailableShrines;
        private int m_Page;
        public static readonly int TeleportCost = 25;
        private const int PageSize = 8;
        private const int PreviousPageButton = 1001;
        private const int NextPageButton = 1002;

        public EtherealTravelGump(Mobile from, int page = 0) : base(150, 100)
        {
            m_From = from;
            m_AvailableShrines = new List<EtherealShrine>();
            m_Page = Math.Max(0, page);

            Closable = true;
            Disposable = true;
            Dragable = true;

            var wpData = XmlAttach.FindAttachment(from, typeof(Server.Custom.GhostMechanics.EtherealWaypointsAttachment), "DiscoveredWaypoints") as Server.Custom.GhostMechanics.EtherealWaypointsAttachment;
            XmlValue pointsData = XmlAttach.FindAttachment(from, typeof(XmlValue), "EtherealEssence") as XmlValue;
            int essence = pointsData != null ? pointsData.Value : 0;

            if (wpData != null)
            {
                foreach (Item item in World.Items.Values)
                {
                    if (item is EtherealShrine shrine && wpData.HasShrine(shrine.ShrineName) && shrine.Map == from.Map)
                    {
                        m_AvailableShrines.Add(shrine);
                    }
                }
            }

            AddPage(0);
            AddBackground(0, 0, 360, 320, 9270);
            AddAlphaRegion(10, 10, 340, 300);

            AddLabel(30, 20, 0x482, "Хроники Духов: Эфирные Шрайны");
            AddLabel(30, 45, 0x35, $"Ваша Эссенция: {essence} ед. (Цена прыжка: {TeleportCost})");

            if (m_AvailableShrines.Count == 0)
            {
                AddLabel(30, 120, 0x22, "У вас нет доступных точек телепортации.");
                AddLabel(30, 140, 0x22, "Активируйте Шрайны при жизни в мире смертных!");
                return;
            }

            int pageCount = (m_AvailableShrines.Count + PageSize - 1) / PageSize;
            m_Page = Math.Min(m_Page, pageCount - 1);
            int startIndex = m_Page * PageSize;
            int endIndex = Math.Min(startIndex + PageSize, m_AvailableShrines.Count);

            int y = 80;
            for (int i = startIndex; i < endIndex; i++)
            {
                EtherealShrine target = m_AvailableShrines[i];
                AddButton(25, y + 2, 4005, 4007, i - startIndex + 1, GumpButtonType.Reply, 0);
                AddLabel(60, y, 0x482, target.ShrineName);
                y += 25;
            }
            AddLabel(150, 285, 0x35, $"{m_Page + 1} / {pageCount}");
            if (m_Page > 0) AddButton(25, 282, 4014, 4016, PreviousPageButton, GumpButtonType.Reply, 0);
            if (m_Page < pageCount - 1) AddButton(300, 282, 4005, 4007, NextPageButton, GumpButtonType.Reply, 0);
        }
        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = m_From;
            if (from == null || from.Alive || info.ButtonID == 0) return;
            if (info.ButtonID == PreviousPageButton || info.ButtonID == NextPageButton)
            {
                int nextPage = m_Page + (info.ButtonID == NextPageButton ? 1 : -1);
                from.SendGump(new EtherealTravelGump(from, nextPage));
                return;
            }
            int index = (m_Page * PageSize) + info.ButtonID - 1;
            if (index >= 0 && index < m_AvailableShrines.Count)
            {
                EtherealShrine dest = m_AvailableShrines[index];
                if (dest == null || dest.Deleted) return;
                XmlValue pointsData = XmlAttach.FindAttachment(from, typeof(XmlValue), "EtherealEssence") as XmlValue;
                int currentEssence = pointsData != null ? pointsData.Value : 0;
                if (currentEssence < TeleportCost)
                {
                    from.SendMessage(0x22, $"Вам не хватает призрачной энергии! Требуется: {TeleportCost}.");
                    return;
                }
                pointsData.Value -= TeleportCost;
                from.MoveToWorld(dest.Location, dest.Map);
                from.PlaySound(0x1FC);
                Effects.SendLocationParticles(EffectItem.Create(dest.Location, dest.Map, TimeSpan.FromSeconds(1.0)), 0x3709, 10, 15, 0x482, 0, 5024, 0);
                from.SendMessage(0x482, $"Вы перенесены к святилищу: {dest.ShrineName}.");
            }
        }
    }
    public class GhostPermaDeathAttachment : XmlAttachment
    {
        public DateTime DeathTimeUtc { get; set; }
        public GhostPermaDeathAttachment() { Name = "GhostPermaDeath"; DeathTimeUtc = DateTime.MinValue; }
        public GhostPermaDeathAttachment(ASerial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); writer.Write(DeathTimeUtc); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); DeathTimeUtc = reader.ReadDateTime(); }
    }
}
namespace Server.Mobiles
{
    public partial class PlayerMobile
    {
        public override void OnAfterResurrect()
        {
            base.OnAfterResurrect();
            Server.Misc.GhostEventModule.OnPlayerResurrected(this);
        }
    }
}