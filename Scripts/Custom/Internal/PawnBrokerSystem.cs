using System;
using System.Collections.Generic;
using System.Reflection;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.ContextMenus;
using Server.Gumps;
using Server.Custom.CurseSystem;
using Server.Engines.XmlSpawner2;

namespace Server.Custom.Pawnshop
{
    // 🔥 ГЛОБАЛЬНОЕ ХРАНИЛИЩЕ ЛОМБАРДА (Сюда временно попадают вещи игроков)
    public static class PawnshopVault
    {
        public static List<Item> LoanedItems { get; } = new List<Item>();

        public static void AddItem(Item item)
        {
            if (item != null && !LoanedItems.Contains(item))
            {
                item.Internalize(); 
                LoanedItems.Add(item);
            }
        }

        public static Item ExtractRandomItem()
        {
            if (LoanedItems.Count == 0) return null;
            
            int index = Utility.Random(LoanedItems.Count);
            Item item = LoanedItems[index];
            LoanedItems.RemoveAt(index);
            return item;
        }
    }

    // 🔥 NPC: ГОРОДСКОЙ ЛОМБАРДЩИК / РОСТОВЩИК (PawnBroker)
    public class PawnBroker : BaseCreature
    {
        public override bool IsInvulnerable => true;

        [Constructable]
        public PawnBroker() : base(AIType.AI_Vendor, FightMode.None, 10, 1, 0.4, 0.2)
        {
            Name = "Шейлок";
            Title = "Содержатель Ломбарда";
            SpeechHue = 0x1F3; // Золотисто-коричневый цвет речи
            Body = 400;
            Hue = Utility.RandomSkinHue();

            AddItem(new FancyShirt(1420) { Movable = false });
            AddItem(new ShortPants(1157) { Movable = false });
            AddItem(new Shoes(1175) { Movable = false });
            
            Utility.AssignRandomHair(this);

            // 🔥 ИСПРАВЛЕНО: Свойства фиксации прописаны нативно в тело конструктора! 🎉⚙️🛠️
            CantWalk = true;
            Blessed = true;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.InRange(this.Location, 4))
            {
                from.SendMessage(0x35, "[Ломбард]: 'Брось мне в руки любое снаряжение, карты или свитки. Я оценю их и выдам быстрые наличные!'");
            }
        }

        // РИТУАЛ СКУПКИ (Игрок бросает вещь в Ростовщика)
        public override bool OnDragDrop(Mobile from, Item item)
        {
            if (from == null || item == null || this.Deleted) return false;

            int trueWorth = EvaluateItemWorth(item);

            if (trueWorth <= 0)
            {
                this.SayTo(from, "Этот мусор ничего не стоит! Уноси это с глаз моих.");
                return false;
            }

            // Выплачиваем игроку всего 35% от реальной стоимости статов вещи
            int pawnPayout = (int)(trueWorth * 0.35);
            if (pawnPayout < 50) pawnPayout = 50;

            from.AddToBackpack(new Gold(pawnPayout));
            this.PlaySound(0x2E6); 
            this.SayTo(from, $"Хм... Вещь неплохая. Моя экспертная цена с учетом рисков: {pawnPayout} золотых монет. По рукам!");

            // Отправляем её в Глобальный Пул Ломбарда для миграции по миру
            item.InvalidateProperties();
            PawnshopVault.AddItem(item);

            return true;
        }

        private int EvaluateItemWorth(Item item)
        {
            if (item == null) return 0;
            int worth = (int)(item.Weight * 50) + 150; 
            int propertiesCount = 0;
            int totalIntensity = 0;

            object attrs = null;
            if (item is BaseWeapon bw) attrs = bw.Attributes;
            else if (item is BaseArmor ba) attrs = ba.Attributes;
            else if (item is BaseJewel bj) attrs = bj.Attributes;

            if (attrs != null)
            {
                try
                {
                    PropertyInfo[] props = attrs.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (PropertyInfo prop in props)
                    {
                        if (prop.PropertyType == typeof(int))
                        {
                            int val = (int)prop.GetValue(attrs, null);
                            if (val > 0) { propertiesCount++; totalIntensity += val; }
                        }
                    }
                }
                catch { }
            }

            worth += (propertiesCount * 4500) + (totalIntensity * 120);

            if (item is TreasureMap tm) worth += tm.Level * 8000;
            if (item is SpellScroll scroll) worth += scroll.ItemID * 150;

            var curse = XmlAttach.FindAttachment(item, typeof(XmlCurseAttribute)) as XmlCurseAttribute;
            if (curse != null) worth /= 5; 

            if (propertiesCount == 0 && !(item is TreasureMap)) return Math.Min(350, worth);
            return Math.Max(200, Math.Min(45000, worth));
        }

        public PawnBroker(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}
