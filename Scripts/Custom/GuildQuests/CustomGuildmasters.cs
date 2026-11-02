using System;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Mobiles
{
    public class CookGuildmaster : BaseGuildmaster
    {
        public override NpcGuild NpcGuild => NpcGuild.MerchantsGuild;

        [Constructable]
        public CookGuildmaster() : base("Повар")
        {
            Name = NameList.RandomName("male");
            Title = "[Глава Гильдии Кулинаров]";

            SetSkill(SkillName.Cooking, 90.0, 100.0);
            SetSkill(SkillName.TasteID, 75.0, 98.0);
            AddItem(new HalfApron(0x59B));
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from is PlayerMobile pm && pm.Alive && pm.InRange(Location, 2))
            {
                Server.Custom.GuildQuests.CookingSkillQuest.OnGuildmasterClick(pm, this);
                return;
            }

            base.OnDoubleClick(from);
        }

        public CookGuildmaster(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }
    }

    // =========================================================================
    // 🎣 1. ГЛАВА МОРСКОЙ РЫБОЛОВНОЙ ГИЛЬДИИ (ДЛЯ НАВЫКА FISHING)
    // =========================================================================
    public class FishermanGuildmaster : BaseGuildmaster
    {
        public override NpcGuild NpcGuild => NpcGuild.FishermensGuild;

        [Constructable]
        public FishermanGuildmaster() : base("Рыболов")
        {
            Name = NameList.RandomName("male");
            Title = "[Глава Рыболовной Гильдии]";
            
            AddItem(new ThighBoots() { Hue = 1044 });
            AddItem(new ShortPants() { Hue = 1133 });
            AddItem(new FancyShirt() { Hue = 1022 });
            AddItem(new TricorneHat() { Hue = 1044 });
            
            AddItem(new FishingPole());
        }

        // Прямой вызов изолированного квеста при клике
        public override void OnDoubleClick(Mobile from)
        {
            if (from is PlayerMobile pm && pm.Alive && pm.InRange(this.Location, 2))
            {
                Server.Custom.GuildQuests.FishermanSkillQuest.OnFishermanClick(pm, this);
                return;
            }

            base.OnDoubleClick(from);
        }

        public FishermanGuildmaster(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // =========================================================================
    // ⚔️ 2. ВЕРХОВНЫЙ ПАЛАДИН ОРДЕНА (ДЛЯ НАВЫКА CHIVALRY)
    // =========================================================================
    public class PaladinGuildmaster : BaseGuildmaster
    {
        public override NpcGuild NpcGuild => NpcGuild.WarriorsGuild; 

        [Constructable]
        public PaladinGuildmaster() : base("Паладин")
        {
            Name = NameList.RandomName("male");
            Title = "[Глава Ордена Паладинов]";
            
            // 🔥 ИСПРАВЛЕНО: Заменили нативные имена классов лат на валидные для ServUO
            AddItem(new Boots() { Hue = 1266 });
            AddItem(new PlateLegs() { Hue = 1266 });
            AddItem(new PlateChest() { Hue = 1266 });
            AddItem(new PlateArms() { Hue = 1266 });
            AddItem(new PlateGloves() { Hue = 1266 });
            
            AddItem(new Cloak() { Hue = 1150 }); 
            AddItem(new VikingSword() { Hue = 1266 });
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from is PlayerMobile pm && pm.Alive && pm.InRange(Location, 2))
            {
                Server.Custom.GuildQuests.PaladinSkillQuest.OnPaladinClick(pm, this);
                return;
            }

            base.OnDoubleClick(from);
        }

        public PaladinGuildmaster(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    // =========================================================================
    // 💀 3. ВЕРХОВНЫЙ НЕКРОМАНТ УМБРЫ (ДЛЯ НАВЫКА NECROMANCY)
    // =========================================================================
    public class NecromancerGuildmaster : BaseGuildmaster
    {
        public override NpcGuild NpcGuild => NpcGuild.MagesGuild; 

        [Constructable]
        public NecromancerGuildmaster() : base("Некромант")
        {
            Name = NameList.RandomName("male");
            Title = "[Верховный Некромант Умбры]";
            
            AddItem(new Shoes() { Hue = 1175 });
            AddItem(new Robe() { Hue = 1175 });
            AddItem(new WizardsHat() { Hue = 1175 });
            AddItem(new Cloak() { Hue = 1 }); 
            
            AddItem(new GnarledStaff() { Hue = 1175 });
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from is PlayerMobile pm && pm.Alive && pm.InRange(Location, 2))
            {
                Server.Custom.GuildQuests.NecroSkillQuest.OnNecromancerClick(pm, this);
                return;
            }

            base.OnDoubleClick(from);
        }

        public NecromancerGuildmaster(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}
