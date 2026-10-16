using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Commands;
using Server.Gumps;
using Server.Network;

namespace Server.Custom.EpicAlignment
{
    public class BookOfDestiny : Item
    {
        [Constructable]
        public BookOfDestiny() : base(0x22C5) 
        {
            Name = "Книга Судьбы: Эндгейм Контроллер";
            Weight = 1.0; LootType = LootType.Blessed; 
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null) return;
            if (from.Backpack == null || !this.IsChildOf(from.Backpack))
            { from.SendMessage(0x22, "Книга Судьбы должна находиться в вашем рюкзаке."); return; }

            EpicRank rank = EpicAlignmentSystem.GetRank(from);
            if (rank == EpicRank.None)
            { from.SendMessage(0x22, "Эта книга пуста для вас. Вы не достигли Эпического ранга."); return; }

            from.CloseGump(typeof(EpicBookGump));
            from.SendGump(new EpicBookGump(from, this, rank));
        }

        private class EpicBookGump : Gump
        {
            private Mobile m_From; BookOfDestiny m_Book; EpicRank m_Rank;

            public EpicBookGump(Mobile from, BookOfDestiny book, EpicRank rank) : base(150, 150)
            {
                m_From = from; m_Book = book; m_Rank = rank;
                bool isDark = (rank == EpicRank.HandOfMondain);

                AddPage(0);
                AddBackground(0, 0, 320, 240, 0x13BE); 
                AddImageTiled(10, 10, 300, 220, 0xA40); 

                string title = isDark ? "=== ГРИМУАР ВЕЧНОЙ ТЬМЫ ===" : "=== ФОЛИАНТ СВЯТОГО СВЕТА ===";
                AddLabel(45, 20, isDark ? 0x22 : 0x35, title);
                AddLabel(30, 60, 0x59, "Выберите проявление высшей силы:");

                AddButton(30, 110, 0xFA5, 0xFA7, 1, GumpButtonType.Reply, 0);
                AddLabel(70, 110, 0x481, "Призвать Могучую Армию (1 раз в сутки)");

                AddButton(30, 160, 0xFA5, 0xFA7, 2, GumpButtonType.Reply, 0);
                AddLabel(70, 160, 0x481, "Вызвать Мирового Босса (1 раз в сутки)");
                
                AddLabel(20, 205, 0x35, "Статус Полубога: Сохранен на диск");
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                if (m_From == null || m_From.Deleted || !m_From.Alive || m_From.Map == null ||
                    m_From.Map == Map.Internal || m_Book == null || m_Book.Deleted ||
                    m_From.Backpack == null || !m_Book.IsChildOf(m_From.Backpack) ||
                    EpicAlignmentSystem.GetRank(m_From) != m_Rank)
                    return;
                bool isDark = (m_Rank == EpicRank.HandOfMondain);

                // ⚔️ КНОПКА 1: ПРИЗЫВ ЛИЧНОЙ АРМИИ НА СУТКИ Реального Времени!
                if (info.ButtonID == 1)
                {
                    // ИСПРАВЛЕНО: Теперь армия, как и босс, блокируется ровно на 24 часа реального времени! ⏱️
                    if (!EpicAlignmentSystem.CanSummonGuards(m_From))
                    { m_From.SendMessage(0x22, "Ваше войско восстанавливает силы. Призыв армии доступен 1 раз в сутки."); return; }

                    int spawnCount = 6;
                    List<BaseCreature> armyList = new List<BaseCreature>();
                    List<Point3D> spawnLocations = new List<Point3D>();

                    for (int i = 0; i < spawnCount; i++)
                    {
                        BaseCreature soldier = isDark 
                            ? (Utility.RandomBool() ? (BaseCreature)new Succubus() : (BaseCreature)new RottingCorpse())
                            : (Utility.RandomBool() ? (BaseCreature)new Gargoyle() : (BaseCreature)new Efreet());

                        Point3D spawnLocation;
                        if (!TryFindSpawnLocation(m_From.Map, m_From.Location, 3, out spawnLocation))
                        {
                            soldier.Delete();
                            DeleteCreatures(armyList);
                            m_From.SendMessage(0x22, "Рядом недостаточно свободного места для призыва армии.");
                            return;
                        }

                        soldier.Karma = 0; soldier.Fame = 0;
                        soldier.Name = isDark ? $"[Гвардия Тьмы] {soldier.Name}" : $"[Легион Света] {soldier.Name}";
                        soldier.Hue = isDark ? 1175 : 2418;
                        soldier.ActiveSpeed = 0.1;
                        armyList.Add(soldier);
                        spawnLocations.Add(spawnLocation);
                    }

                    int requiredSlots = 0;
                    foreach (BaseCreature soldier in armyList)
                        requiredSlots += soldier.ControlSlots;

                    if (m_From.Followers + requiredSlots > m_From.FollowersMax)
                    {
                        DeleteCreatures(armyList);
                        m_From.SendMessage(0x22, "Для призыва всего легиона недостаточно свободных слотов последователей.");
                        return;
                    }

                    for (int i = 0; i < armyList.Count; i++)
                    {
                        if (!BaseCreature.Summon(armyList[i], false, m_From, spawnLocations[i], 0x213, TimeSpan.FromMinutes(3.0)))
                        {
                            DeleteCreatures(armyList);
                            return;
                        }
                    }

                    EpicAlignmentSystem.RegisterGuardSummon(m_From);
                    m_From.PlaySound(0x213);
                    m_From.SendMessage(isDark ? 0x22 : 0x35, $"Ваш личный легион ({spawnCount} бойцов) призван на 3 минуты!");
                }
                else if (info.ButtonID == 2)
                {
                    if (!EpicAlignmentSystem.CanSummonBoss(m_From))
                    { m_From.SendMessage(0x22, "Портал нестабилен. Призыв Мирового Босса доступен 1 раз в сутки."); return; }

                    m_From.SendMessage(0x35, "Выберите точку на земле для открытия разлома...");
                    m_From.Target = new InternalBossTarget(m_Book, m_Rank);
                }
            }
        }

        private class InternalBossTarget : Server.Targeting.Target
        {
            private BookOfDestiny m_Book; EpicRank m_Rank;
            public InternalBossTarget(BookOfDestiny book, EpicRank rank) : base(10, true, Server.Targeting.TargetFlags.None)
            { m_Book = book; m_Rank = rank; }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (from == null || from.Deleted || !from.Alive || from.Map == null ||
                    from.Map == Map.Internal || from.Backpack == null || m_Book == null ||
                    m_Book.Deleted || !m_Book.IsChildOf(from.Backpack) ||
                    EpicAlignmentSystem.GetRank(from) != m_Rank)
                    return;

                if (!EpicAlignmentSystem.CanSummonBoss(from))
                {
                    from.SendMessage(0x22, "Портал нестабилен. Призыв Мирового Босса доступен 1 раз в сутки.");
                    return;
                }

                IPoint3D p = targeted as IPoint3D; if (p == null) return;
                bool isDark = (m_Rank == EpicRank.HandOfMondain);

                Point3D targetLocation = new Point3D(p.X, p.Y, from.Map.GetAverageZ(p.X, p.Y));
                Point3D loc;
                if (!TryFindSpawnLocation(from.Map, targetLocation, 3, out loc))
                {
                    from.SendMessage(0x22, "В этой точке нельзя безопасно открыть разлом.");
                    return;
                }
                BaseCreature boss = isDark ? (BaseCreature)new Daemon() : (BaseCreature)new AncientWyrm(); 
                boss.Karma = 0; boss.Fame = 0; boss.HitsMaxSeed = 35000; boss.Hits = 35000; boss.SetDamage(55, 85); 
                boss.Tamable = false;

                boss.Name = isDark ? "Харроуэр: Абсолютное Теневое Зло [МИРОВОЙ БОСС]" : "Слэшер Сияния: Высший Судия [МИРОВОЙ БОСС]";
                boss.Hue = isDark ? 1175 : 2418;

                World.Broadcast(isDark ? 0x22 : 0x35, true, $"[ВНИМАНИЕ] Высший Полубог ({from.Name}) призвал в мир {boss.Name}!");

                Container backpack = boss.Backpack;
                if (backpack == null) { backpack = new Backpack(); backpack.Movable = false; boss.AddItem(backpack); }
                backpack.DropItem(new Gold(Utility.RandomMinMax(10000, 15000)));

                boss.MoveToWorld(loc, from.Map);
                boss.Combatant = from;
                boss.Warmode = true;

                EpicAlignmentSystem.RegisterBossSummon(from);
                Timer.DelayCall(TimeSpan.FromMinutes(15.0), () => { if (boss != null && !boss.Deleted && boss.Alive) boss.Delete(); });
            }
        }

        private static bool TryFindSpawnLocation(Map map, Point3D center, int radius, out Point3D location)
        {
            for (int attempt = 0; attempt < 30; attempt++)
            {
                int x = center.X + Utility.RandomMinMax(-radius, radius);
                int y = center.Y + Utility.RandomMinMax(-radius, radius);
                Point3D candidate = new Point3D(x, y, map.GetAverageZ(x, y));
                if (map.CanSpawnMobile(candidate) && map.CanFit(candidate, 16, false, false))
                {
                    location = candidate;
                    return true;
                }
            }

            location = Point3D.Zero;
            return false;
        }

        private static void DeleteCreatures(List<BaseCreature> creatures)
        {
            foreach (BaseCreature creature in creatures)
            {
                if (creature != null && !creature.Deleted)
                    creature.Delete();
            }
        }

        public BookOfDestiny(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
}