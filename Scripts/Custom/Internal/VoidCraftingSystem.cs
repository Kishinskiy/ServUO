using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Gumps;
using Server.Network;

namespace Server.Custom.VoidCraft
{
    // 🔥 РЕСУРС: ОСКОЛОК СКВЕРНЫ (Превращен в крафтовый предмет)
    public class CurseShardCraftable : Item
    {
        [Constructable]
        public CurseShardCraftable() : base(0x573A) 
        {
            Name = "Осколок Скверны [Редкий Магический Ресурс]";
            Hue = 1175;
            Weight = 0.1;
            LootType = LootType.Blessed;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(this.Location, 2))
            {
                from.SendLocalizedMessage(500295); // Ты слишком далеко.
                return;
            }

            // Проверяем, что крафтер достиг ранга Грандмастера
            if (from.Skills[SkillName.Tailoring].Value < 100.0 && from.Skills[SkillName.Blacksmith].Value < 100.0)
            {
                from.SendMessage(0x22, "Только Грандмастер Портной или Кузнец (100.0) способен направить энергию Осколка в крафт!");
                return;
            }

            // Открываем кастомное меню выбора элитного рецепта Тьмы
            from.SendGump(new VoidCraftGump(from, this));
        }

        public CurseShardCraftable(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // =========================================================================
    // 🎨 ГРАФИЧЕСКОЕ ИНТЕРФЕЙСНОЕ МЕНЮ РЕЦЕПТОВ ТЬМЫ
    // =========================================================================
    public class VoidCraftGump : Gump
    {
        private Mobile m_Crafter;
        private Item m_Shard;

        public VoidCraftGump(Mobile crafter, Item shard) : base(150, 100)
        {
            m_Crafter = crafter;
            m_Shard = shard;

            AddPage(0);
            AddBackground(0, 0, 360, 240, 9270); // Благородный темный средневековый фон
            AddAlphaRegion(10, 10, 340, 220);

            AddLabel(50, 20, 0x22, "=== КУЗНЯ ЗАПРЕТНЫХ РЕЦЕПТОВ ТЬМЫ ===");

            // Кнопка 1: Мантия Порочного Лорда
            AddButton(30, 75, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddLabel(70, 75, 0x58, "Мантия Порочного Лорда (Маг/Некромант)");
            AddLabel(70, 95, 0x59, "Требует: 15 Осколков Скверны, 40 Призрачной ткани");

            // Кнопка 2: Теневой Капюшон Ассасина
            AddButton(30, 145, 4005, 4007, 2, GumpButtonType.Reply, 0);
            AddLabel(70, 145, 0x22, "Теневой Капюшон Ассасина (Крит/Яды)");
            AddLabel(70, 165, 0x59, "Требует: 10 Осколков Скверны, 20 Кожи");
        }

        // 🔥 ИСПРАВЛЕНО: Сигнатура приведена к нативной RelayInfo эмулятора ServUO! 🎉⚙️🛠️
        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (m_Crafter == null || m_Shard == null || m_Shard.Deleted || !m_Crafter.InRange(m_Shard.Location, 2)) return;

            Container pack = m_Crafter.Backpack;
            if (pack == null) return;

            int shardCount = pack.GetAmount(m_Shard.GetType());

            if (info.ButtonID == 1) // Крафт Мантии Порочного Лорда
            {
                if (m_Crafter.Skills[SkillName.Tailoring].Value < 100.0)
                {
                    m_Crafter.SendMessage(0x22, "Вам необходим навык Tailoring 100.0 (Grandmaster) для пошива мантии лорда!");
                    return;
                }

                Item cloth = pack.FindItemByType(ScriptCompiler.FindTypeByName("WraithCloth"));
                int clothCount = cloth != null ? cloth.Amount : 0;

                if (shardCount < 15 || clothCount < 40)
                {
                    m_Crafter.SendMessage(0x22, $"Недостаточно ресурсов! У вас: Осколков [{shardCount}/15], Ткани [{clothCount}/40]");
                    return;
                }

                if (m_Shard.Amount == 15) m_Shard.Delete(); else m_Shard.Amount -= 15;
                if (cloth.Amount == 40) cloth.Delete(); else cloth.Amount -= 40;

                Robe robe = new Robe();
                robe.Name = "Мантия Порочного Лорда [Легендарный Фракционный Артефакт]";
                robe.Hue = 1175; 
                robe.LootType = LootType.Blessed;
                robe.Attributes.SpellDamage = 15; 
                robe.Attributes.RegenMana = 3;    

                m_Crafter.AddToBackpack(robe);
                m_Crafter.PlaySound(0x248); 
                m_Crafter.SendMessage(0x58, "Вы успешно сплели нити Скверны! Великая Мантия Порочного Лорда создана!");
            }
            else if (info.ButtonID == 2) // Крафт Теневого Капюшона Ассасина
            {
                if (m_Crafter.Skills[SkillName.Tailoring].Value < 100.0)
                {
                    m_Crafter.SendMessage(0x22, "Вам необходим навык Tailoring 100.0 (Grandmaster) для создания капюшона ассасина!");
                    return;
                }

                Item regularLeather = pack.FindItemByType(typeof(Leather));
                int leatherCount = regularLeather != null ? regularLeather.Amount : 0;

                if (shardCount < 10 || leatherCount < 20)
                {
                    m_Crafter.SendMessage(0x22, $"Недостаточно ресурсов! У вас: Осколков [{shardCount}/10], Кожи [{leatherCount}/20]");
                    return;
                }

                if (m_Shard.Amount == 10) m_Shard.Delete(); else m_Shard.Amount -= 10;
                if (regularLeather.Amount == 20) regularLeather.Delete(); else regularLeather.Amount -= 20;

                LeatherCap cap = new LeatherCap();
                cap.Name = "Теневой Капюшон Ассасина [Легендарный Фракционный Артефакт]";
                cap.Hue = 1157; 
                cap.LootType = LootType.Blessed;
                cap.Attributes.AttackChance = 10; 
                cap.ArmorAttributes.MageArmor = 1; 

                m_Crafter.AddToBackpack(cap);
                m_Crafter.PlaySound(0x56); 
                m_Crafter.SendMessage(0x58, "Запретная ковка завершена! Теневой Капюшон Ассасина упал в вашу сумку.");
            }
        }
    }
}
