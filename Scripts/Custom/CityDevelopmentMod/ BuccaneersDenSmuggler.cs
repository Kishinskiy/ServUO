using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using Server.ContextMenus;
using Server.Gumps;
using Server.Network;
using Server.Custom.CityDevelopment;
using Server.Custom.CurseSystem;
using Server.Engines.XmlSpawner2;

namespace Server.Custom.BuccaneersDen
{
    // 🔥 NPC: СКУПЩИК КОНТРАБАНДЫ И ДИВЕРСАНТ (Smuggler)
    public class Smuggler : BaseCreature
    {
        public override bool IsInvulnerable => true;

        [Constructable]
        public Smuggler() : base(AIType.AI_Vendor, FightMode.None, 10, 1, 0.4, 0.2)
        {
            Name = "Гарри";
            Title = "Скупщик Контрабанды";
            SpeechHue = 0x22; 
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

        private class SellContrabandEntry : ContextMenuEntry
        {
            private Mobile m_Player;
            private Smuggler m_Smuggler;

            public SellContrabandEntry(Mobile player, Smuggler smuggler) : base(1151729, 3) 
            {
                this.m_Player = player;
                this.m_Smuggler = smuggler;
            }

            public override void OnClick()
            {
                if (m_Player != null && m_Smuggler != null && !m_Smuggler.Deleted)
                {
                    m_Player.SendMessage("Выберите караванный ящик, проклятый предмет или BOD-контракт для продажи Черному Рынку.");
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

                    string typeName = item.GetType().Name.ToLower();

                    // 🏴‍☠️ СЦЕНАРИЙ А: Сдача ворованного фракционного каравана (Грабёж)
                    if (typeName.Contains("tradeordercrate") || typeName.Contains("crate"))
                    {
                        item.Delete(); 
                        int dirtyGold = 8000; 
                        Banker.Deposit(from, dirtyGold, true);

                        from.PlaySound(0x57);
                        m_Smuggler.PublicOverheadMessage(MessageType.Regular, 0x22, false, "Ха-ха! Свежая контрабанда из имперских портов! Вот твоё золото!");

                        // 30% Шанс перехватить патент при ограблении каравана
                        if (Utility.RandomDouble() < 0.30)
                        {
                            Item hijackedBOD = new Server.Engines.BulkOrders.LargeSmithBOD();
                            from.AddToBackpack(hijackedBOD);
                            from.SendMessage(0x5D, "[Контрабанда]: Внутри разбитого ящика вы нашли неиспользованный Имперский Патент (BOD)!");
                        }
                        return;
                    }

                    // 🔮 СЦЕНАРИЙ Б: Продажа проклятого Diablo-лута
                    var curse = XmlAttach.FindAttachment(item, typeof(XmlCurseAttribute)) as XmlCurseAttribute;
                    if (curse != null)
                    {
                        item.Delete(); 
                        int reward = Utility.RandomMinMax(2000, 5000);
                        Banker.Deposit(from, reward, true);

                        from.PlaySound(0x3C4);
                        m_Smuggler.PublicOverheadMessage(MessageType.Regular, 0x22, false, "О-о-о, от этой стали так и разит темной некромантией... Мои колдуны купят это!");
                        return;
                    }

                    // ⚔️ СЦЕНАРИЙ В: ТВОЁ ТЗ — СДАЧА ЛЮБОГО ВАНИЛЬНОГО, КУПЛЕННОГО ИЛИ УКРАДЕННОГО ЧЕРЕЗ STEALING BOD-КОНТРАКТА!
                    // 🔥 ИСПРАВЛЕНО: Проверка переведена на универсальный и неуязвимый строковый парсер во избежание конфликтов классов! 🎉⚙️
                    if (typeName.Contains("deed") && (typeName.Contains("smith") || typeName.Contains("tailor") || typeName.Contains("bulk") || typeName.Contains("order")))
                    {
                        Server.Engines.CityLoyalty.City targetCity = Utility.RandomDouble() < 0.50 ? Server.Engines.CityLoyalty.City.Britain : Server.Engines.CityLoyalty.City.Trinsic;

                        CityProgress progress = CityDevelopmentMod.GetCityProfile(targetCity, from.Map);
                        if (progress != null)
                        {
                            progress.IronStock = Math.Max(0, progress.IronStock - 35);
                            progress.WoodStock = Math.Max(0, progress.WoodStock - 35);
                            progress.Treasury = Math.Max(0, progress.Treasury - 30000); // Выкачиваем 30к из бюджета города!
                        }

                        item.Delete();

                        int sabotageReward = 55000; 
                        Banker.Deposit(from, sabotageReward, true);

                        from.PlaySound(0x5C3);
                        m_Smuggler.PublicOverheadMessage(MessageType.Regular, 0x22, false, $"Превосходно! Казна и склады города {targetCity} понесли тяжелые убытки. Вот твой чистый куш: {sabotageReward} золотых!");
                        return;
                    }

                    from.SendMessage(0x22, "Скупщик качает головой: 'Мне нужна чистая контрабанда, проклятые шмотки или BOD-патенты для диверсий!'");
                }
            }
        }

        public Smuggler(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class SmugglerGump : Gump
    {
        private Smuggler m_NPC;

        public SmugglerGump(Mobile from, Smuggler npc) : base(150, 150)
        {
            m_NPC = npc;

            AddPage(0);
            AddBackground(0, 0, 380, 260, 0x13BE);
            AddLabel(20, 15, 0x22, "🏴‍☠️ ЧЕРНЫЙ РЫНОК BUCCANEER'S DEN");
            
            AddHtml(20, 45, 340, 100, 
                "<BASEFONT COLOR=#FF8C00>Приветствую, пират! Я скупаю краденые ящики караванов и проклятую сталь Тьмы. Также у меня есть <Basefont color=#FFFF00>Поддельные Имперские Патенты (BOD)</Basefont> для подрыва экономики городов. Колдунам Бухты нужны Эссенции Душ для темных ритуалов, а мне — золото за риск!</BASEFONT>", 
                false, false);
            
            AddBackground(20, 150, 340, 50, 0x24AE);
            AddLabel(30, 165, 0x35, "Купить Патент (30,000 gp + 2 Soul Essence)");
            AddButton(310, 165, 0x254F, 0x2551, 1, GumpButtonType.Reply, 0);

            AddLabel(20, 220, 0x5D, "Сдать контрабанду или BOD можно через контекстное меню Гарри.");
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;
            if (from == null || !from.Alive || m_NPC == null) return;

            if (info.ButtonID == 1) 
            {
                Container pack = from.Backpack;
                if (pack == null) return;

                Item soulEssence = pack.FindItemByType(ScriptCompiler.FindTypeByName("SoulEssence"));
                
                if (soulEssence == null || soulEssence.Amount < 2)
                {
                    from.SendMessage(0x22, "Гарри ворчит: 'У тебя нет 2-х Эссенций Душ! Колдуны Бухты не дадут мне патенты без них!'");
                    return;
                }

                if (pack.ConsumeTotal(typeof(Gold), 30000) == false)
                {
                    if (Banker.Withdraw(from, 30000) == false)
                    {
                        from.SendMessage(0x22, "Гарри качает головой: 'Цена этого патента — 30,000 золотых монет. У тебя нет таких денег!'");
                        return;
                    }
                }

                if (soulEssence.Amount == 2) soulEssence.Delete();
                else soulEssence.Amount -= 2;

                Item realBOD = new Server.Engines.BulkOrders.LargeSmithBOD(); 
                from.AddToBackpack(realBOD);

                from.PlaySound(0x57); 
                from.SendMessage(0x35, "Вы успешно купили Поддельный Патент (BOD)! Заполните его или сдайте Гарри через контекстное меню для мгновенной диверсии!");
            }
        }
    }
}