using System;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Mobiles
{
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

        public NecromancerGuildmaster(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}
