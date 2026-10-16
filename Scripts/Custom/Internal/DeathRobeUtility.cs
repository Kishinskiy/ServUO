using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Regions;

namespace Server.Custom.DeathRobes
{
    // 🔥 КАСТОМНАЯ ПРИЗРАЧНАЯ ТКАНЬ ДЛЯ ТОП-КРАФТА 🧵✨
    public class WraithCloth : Item
    {
        [Constructable]
        public WraithCloth() : this(1) { }

        [Constructable]
        public WraithCloth(int amount) : base(0x175D) 
        {
            Name = "Призрачная Ткань (Wraith Cloth)";
            Hue = 1150; 
            Stackable = true;
            Amount = amount;
            Weight = 0.1;
        }

        public WraithCloth(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // 🔥 ГЛОБАЛЬНЫЙ КРАФТОВЫЙ И ЭКОНОМИЧЕСКИЙ ДВИЖОК РОБЫ (АБСОЛЮТНО АВТОНОМНЫЙ)
    public static class DeathRobeUtility
    {
        public static void Initialize()
        {
            // Хукаем глобальное создание предметов на сервере
            EventSink.ItemCreated += new ItemCreatedEventHandler(OnItemCreated);
        }

        private static void OnItemCreated(ItemCreatedEventArgs e)
        {
            Item item = e.Item;
            if (item != null && item is DeathRobe)
            {
                // 🔥 ТВОЁ ТЗ: Моментально превращаем бесполезную робу смерти в Саван Защиты! 🎉⚙️🛠️
                string colorHex = "#4A6B82"; 
                item.Name = $"<BASEFONT COLOR={colorHex}>Саван Защиты (Protective Shroud)</BASEFONT>";
                item.Hue = 1156;

                // Запускаем безопасный отложенный цикл проверки владельца
                Item targetRobe = item;
                Timer.DelayCall(TimeSpan.FromSeconds(1.0), () =>
                {
                    if (targetRobe != null && !targetRobe.Deleted)
                    {
                        Mobile owner = targetRobe.RootParent as Mobile;
                        if (owner is PlayerMobile pm)
                        {
                            pm.SendMessage(0x59, "[Саван]: Вы получили временную защиту +20% ко всем резистам на 3 минуты, чтобы вернуть свой лут!");
                            pm.SendMessage(0x59, "[Саван]: Портные могут разрезать этот саван ножницами на ценную Призрачную Ткань.");

                            // Запускаем таймер самоликвидации робы ровно через 3 минуты
                            Timer.DelayCall(TimeSpan.FromMinutes(3.0), () =>
                            {
                                if (targetRobe != null && !targetRobe.Deleted)
                                {
                                    targetRobe.Delete();
                                    pm.SendMessage(0x22, "[Саван]: Время действия защитного савана истекло, и он рассыпался в прах.");
                                }
                            });
                        }
                    }
                });
            }
        }

        // МЕТОД ПЕРЕХВАТА ДЛЯ НОЖНИЦ (Вызывается нативно из Scissors.cs)
        public static bool TryCutRobe(Mobile from, Item item)
        {
            if (from == null || item == null || !(item is DeathRobe)) return false;

            if (from.Skills[SkillName.Tailoring].Value < 60.0)
            {
                from.SendMessage(0x22, "Вашего навыка Портняжного дела (нужно 60.0) недостаточно, чтобы аккуратно собрать призрачные нити!");
                return true; 
            }

            item.Delete();
            from.AddToBackpack(new WraithCloth(Utility.RandomMinMax(4, 6))); 
            from.PlaySound(0x248); 
            from.SendMessage(0x58, "Вы успешно разрезали Саван Смерти на редкую Призрачную Ткань (Wraith Cloth)!");

            return true;
        }
    }
}
