using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Commands; // ИСПРАВЛЕНО: Добавлен namespace для глобального вещания сообщений

namespace Server.Custom.EpicAlignment
{
    public class BookOfDestiny : Item
    {
        [Constructable]
        public BookOfDestiny() : base(0x22C5) 
        {
            Name = "Книга Судьбы: Эндгейм Контроллер";
            Weight = 1.0;
            LootType = LootType.Blessed; 
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null) return;

            if (!from.InRange(this.Location, 2) || this.Parent != from.Backpack)
            {
                from.SendMessage("Книга должна лежать в вашем рюкзаке.");
                return;
            }

            EpicRank rank = EpicAlignmentSystem.GetRank(from);

            if (rank == EpicRank.None)
            {
                from.SendMessage(0x22, "Эта книга пуста для вас. Вы не достигли Эпического ранга.");
                return;
            }

            from.SendMessage(0x59, "Книга Судьбы активирована. Выберите цель.");
            from.Target = new InternalEpicActionTarget(this, rank);
        }

        private class InternalEpicActionTarget : Server.Targeting.Target
        {
            private BookOfDestiny m_Book;
            private EpicRank m_Rank;

            public InternalEpicActionTarget(BookOfDestiny book, EpicRank rank) : base(10, true, Server.Targeting.TargetFlags.None)
            {
                m_Book = book;
                m_Rank = rank;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (from == null || m_Book == null || m_Book.Deleted) return;

                // ИСПРАВЛЕНО: Вывод лорных сообщений теперь использует валидный контекст "from" вместо ошибочного "book.From"
                if (m_Rank == EpicRank.HandOfMondain && targeted == from)
                    from.SendMessage(0x22, "Вы открыли Врата Бездны для призыва Стражи Тьмы.");
                else if (m_Rank == EpicRank.Avatar && targeted == from)
                    from.SendMessage(0x35, "Вы призвали силы Небесного Хранителя.");

                IPoint3D p = targeted as IPoint3D;
                if (p == null) return;

                // Действие 1: Нажатие на себя — Призыв Гарнизона
                if (targeted == from)
                {
                    if (!EpicAlignmentSystem.CanSummonGuards(from))
                    {
                        from.SendMessage(0x22, "Войска еще восстанавливают силы после прошлой битвы!");
                        return;
                    }

                    EpicAlignmentSystem.RegisterGuardSummon(from);
                    from.PlaySound(0x213);

                    int count = 3;
                    for (int i = 0; i < count; i++)
                    {
                        BaseCreature guard;
                        if (m_Rank == EpicRank.HandOfMondain)
                        {
                            guard = new Lich();
                            guard.Name = $"Присягнувший Тьме Лич ({from.Name})";
                        }
                        else
                        {
                            guard = new Golem(); 
                            guard.Name = $"Благословенный Страж ({from.Name})";
                        }

                        guard.Controlled = true;
                        guard.ControlMaster = from;
                        guard.MoveToWorld(new Point3D(from.X + Utility.RandomMinMax(-1, 1), from.Y + Utility.RandomMinMax(-1, 1), from.Z), from.Map);
                    }
                    from.SendMessage("Ваша личная элитная гвардия прибыла!");
                }
                // Действие 2: Нажатие на землю — Призыв Фракционного Босса
                else
                {
                    if (!EpicAlignmentSystem.CanSummonBoss(from))
                    {
                        from.SendMessage(0x22, "Энергия портала восстанавливается. Призыв босса доступен 1 раз в реальные сутки.");
                        return;
                    }

                    EpicAlignmentSystem.RegisterBossSummon(from);
                    Point3D loc = new Point3D(p.X, p.Y, p.Z);

                    BaseCreature boss;
                    if (m_Rank == EpicRank.HandOfMondain)
                    {
                        // ИСПРАВЛЕНО: Класс Demon заменен на штатный ServUO класс Daemon
                        boss = new Daemon(); 
                        boss.Name = "Теневой Вестник Мондейна [BOSS]";
                        boss.Hue = 1175;
                        boss.HitsMaxSeed = 10000; boss.Hits = 10000;
                        boss.SetDamage(35, 55);
                        
                        // ИСПРАВЛЕНО: Вызовы трансляции теперь используют корректный путь CommandHandlers движка
                        CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x22, $"[ВНИМАНИЕ] Рука Мондейна ({from.Name}) призвал в мир Теневого Вестника! Игроки Света, объединяйтесь для уничтожения чудовища!");
                    }
                    else
                    {
                        boss = new AncientWyrm(); 
                        boss.Name = "Древний Дракон Добродетели [BOSS]";
                        boss.Hue = 2418;
                        boss.HitsMaxSeed = 10000; boss.Hits = 10000;
                        boss.SetDamage(35, 55);
                        CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x35, $"[ВНИМАНИЕ] Аватар ({from.Name}) призвал Дракона Добродетели для освящения земель! Игроки Тьмы, убейте защитника Света!");
                    }

                    boss.MoveToWorld(loc, from.Map);
                    
                    Timer.DelayCall(TimeSpan.FromMinutes(15.0), () =>
                    {
                        if (boss != null && !boss.Deleted && boss.Alive)
                        {
                            boss.Delete();
                            CommandHandlers.BroadcastMessage(AccessLevel.Player, 0x59, "[Событие] Время действия призванного существа истекло, оно вернулось в свой план бытия.");
                        }
                    });
                }
            }
        }

        public BookOfDestiny(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
}
