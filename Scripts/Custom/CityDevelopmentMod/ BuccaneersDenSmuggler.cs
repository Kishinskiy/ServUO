using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using Server.ContextMenus;
using Server.Gumps;
using Server.Custom.CityDevelopment;
using Server.Custom.CurseSystem;
using Server.Engines.XmlSpawner2;

namespace Server.Custom.BuccaneersDen
{
    // 🔥 NPC: СКУПЩИК КОНТРАБАНДЫ (Smuggler)
    public class Smuggler : BaseCreature
    {
        public override bool IsInvulnerable => true;

        [Constructable]
        public Smuggler() : base(AIType.AI_Vendor, FightMode.None, 10, 1, 0.4, 0.2)
        {
            Name = "Гарри";
            Title = "Скупщик Контрабанды";
            SpeechHue = 0x22; // Зловещий красный цвет речи
            Body = 400;
            Hue = Utility.RandomSkinHue();

            AddItem(new TricorneHat() { Hue = 1109, Movable = false });
            AddItem(new Doublet() { Hue = 1109, Movable = false });
            AddItem(new Cutlass() { Movable = false });
            AddItem(new Boots() { Hue = 1109, Movable = false });

            Utility.AssignRandomHair(this);

            CantWalk = true;
            Blessed = true;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(this.Location, 4))
            {
                from.SendLocalizedMessage(500295);
                return;
            }

            from.CloseGump(typeof(SmugglerGump));
            from.SendGump(new SmugglerGump(from, this));
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);
            list.Add(new SellContrabandEntry(from, this));
        }

        // 🔥 ИСПРАВЛЕНО: Лишнее двойное вложение класса полностью удалено! 🎉⚙️🛠️
        private class SellContrabandEntry : ContextMenuEntry
        {
            private Mobile m_Player;
            private Smuggler m_Smuggler;

            public SellContrabandEntry(Mobile player, Smuggler smuggler) : base(1151729, 3) // Нативный рабочий Cliloc-индекс сдачи контрактов (Turn in)
            {
                this.m_Player = player;
                this.m_Smuggler = smuggler;
            }

            public override void OnClick()
            {
                if (m_Player != null && m_Smuggler != null && !m_Smuggler.Deleted)
                {
                    m_Player.SendMessage("Выберите караванный ящик или проклятый предмет для продажи Черному Рынку.");
                    m_Player.Target = new SmugglerTarget(m_Smuggler);
                }
            }
        }

        private class SmugglerTarget : Target
        {
            private Smuggler m_Smuggler;

            public SmugglerTarget(Smuggler smuggler) : base(3, false, TargetFlags.None)
            {
                m_Smuggler = smuggler;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Item item)
                {
                    if (item.RootParent != from)
                    {
                        from.SendMessage(0x22, "Предмет должен быть у вас в руках или рюкзаке!");
                        return;
                    }

                    // СЦЕНАРИЙ А: Игрок притащил ворованный караван фракций!
                    string typeName = item.GetType().Name;
                    if (typeName.Contains("TradeOrderCrate") || typeName.Contains("Crate"))
                    {
                        item.Delete();

                        int dirtyGold = 8000;
                        Banker.Deposit(from, dirtyGold, true);

                        from.PlaySound(0x57);
                        m_Smuggler.PublicOverheadMessage(Network.MessageType.Regular, 0x22, false, "Ха-ха! Отличный товар. Свежая контрабанда из портов! Вот твоё золото!");
                        from.SendMessage(0x22, $"[Черный Рынок]: Контрабанда сдана! Вы получили {dirtyGold} золотых на счет в банк.");
                        return;
                    }

                    // СЦЕНАРИЙ Б: Игрок продает ПРОКЛЯТЫЙ ДИABLO-ЛУТ без очищения
                    var curse = XmlAttach.FindAttachment(item, typeof(XmlCurseAttribute)) as XmlCurseAttribute;
                    if (curse != null)
                    {
                        item.Delete();

                        int reward = Utility.RandomMinMax(2000, 5000);
                        Banker.Deposit(from, reward, true);

                        from.PlaySound(0x3C4);
                        m_Smuggler.PublicOverheadMessage(Network.MessageType.Regular, 0x22, false, "О-о-о, я чувствую сильное проклятие на этой стали... Колдуны с Buccaneer's Den найдут применение этой скверне!");
                        from.SendMessage(0x58, $"Вы выгодно сбыли проклятую вещь на Черном Рынке за {reward} золотых!");
                        return;
                    }

                    from.SendMessage(0x22, "Скупщик качает головой: 'Этот хлам легален, продай его обычным торговцам. Мне нужна контрабанда или вещи с проклятиями Тьмы!'");
                }
            }
        }

        public Smuggler(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class SmugglerGump : Gump
    {
        public SmugglerGump(Mobile from, Smuggler npc) : base(150, 150)
        {
            AddPage(0);
            AddBackground(0, 0, 360, 220, 0x13BE);
            AddLabel(20, 15, 0x22, "🏴‍☠️ БУХТА КОНТРАБАНДИСТОВ BUC_DEN");

            AddHtml(20, 45, 320, 100,
                "<BASEFONT COLOR=#FF8C00>Приветствую, сухопутная крыса! Если у тебя есть караванные ящики, которые ты перехватил в Felucca, или проклятый шмот, от которого разит некромантией Маласа — тащи их мне через контекстное меню. Городская стража сюда не сунется!</BASEFONT>",
                false, false);

            AddLabel(20, 165, 0x35, "Используйте контекстное меню NPC: 'Сдать контрабанду'");
        }
    }
}
