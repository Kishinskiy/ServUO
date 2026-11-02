using System;
using System.Collections.Generic;
using Server;
using Server.ContextMenus;
using Server.Mobiles;
using Server.Items;

namespace Server.Custom.MercenarySystem
{
    public class BaseMercenary : BaseCreature
    {
        private MercenaryClass m_Class;
        private MercenaryProgression m_Progression;

        [CommandProperty(AccessLevel.GameMaster)]
        public MercenaryClass Class { get { return m_Class; } set { m_Class = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MercLevel { get { return m_Progression.Level; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MercExp { get { return m_Progression.Experience; } set { m_Progression.Experience = value; m_Progression.CheckLevelUp(this, m_Class); } }

        public static void Initialize()
        {
            EventSink.ServerStarted += () =>
            {
                Timer.DelayCall(TimeSpan.FromSeconds(6.0), new TimerCallback(MercenaryProgression.AutoSpawnCaptains));
            };
        }

        public BaseMercenary(MercenaryClass mClass) : base(AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            m_Class = mClass;
            m_Progression = new MercenaryProgression();

            Name = NameList.RandomName("male");
            Title = $"[{MercenaryProgression.GetRankTitle(1)}]";

            Body = 400;
            Hue = Utility.RandomSkinHue();
            BaseSoundID = 0;
            ControlSlots = 2;

            if (mClass == MercenaryClass.Knight)
            {
                AI = AIType.AI_Melee;
                SetStr(150); SetDex(80); SetInt(50); SetHits(300); SetDamage(15, 25);
                SetSkill(SkillName.Swords, 70.0); SetSkill(SkillName.Tactics, 70.0); SetSkill(SkillName.Anatomy, 50.0); SetSkill(SkillName.Parry, 60.0);
                AddItem(new PlateChest() { Movable = true }); AddItem(new VikingSword() { Movable = true }); AddItem(new BronzeShield() { Movable = true });
            }
            else if (mClass == MercenaryClass.Mage)
            {
                AI = AIType.AI_Mage;
                SetStr(80); SetDex(80); SetInt(150); SetHits(150); SetMana(300); SetDamage(10, 15);
                SetSkill(SkillName.Magery, 80.0); SetSkill(SkillName.EvalInt, 80.0); SetSkill(SkillName.Meditation, 80.0); SetSkill(SkillName.Wrestling, 50.0);
                AddItem(new Robe() { Movable = true, Hue = 2418 }); AddItem(new GnarledStaff() { Movable = true });
            }
            else if (mClass == MercenaryClass.Ranger)
            {
                AI = AIType.AI_Archer;
                SetStr(100); SetDex(150); SetInt(50); SetHits(200); SetDamage(20, 30);
                SetSkill(SkillName.Archery, 80.0); SetSkill(SkillName.Tactics, 70.0); SetSkill(SkillName.Anatomy, 70.0);
                AddItem(new LeatherChest() { Movable = true }); AddItem(new Bow() { Movable = true });
            }
            else if (mClass == MercenaryClass.Bard)
            {
                AI = AIType.AI_Mage;
                SetStr(100); SetDex(100); SetInt(100); SetHits(220); SetDamage(12, 18);
                SetSkill(SkillName.Musicianship, 80.0); SetSkill(SkillName.Discordance, 80.0); SetSkill(SkillName.Magery, 60.0);
                AddItem(new JesterSuit() { Movable = true, Hue = 1161 }); AddItem(new Tambourine() { Movable = true });
            }
            else if (mClass == MercenaryClass.Thief)
            {
                AI = AIType.AI_Melee;
                SetStr(90); SetDex(160); SetInt(50); SetHits(200); SetDamage(18, 28);
                SetSkill(SkillName.Fencing, 85.0); SetSkill(SkillName.Tactics, 75.0); SetSkill(SkillName.Stealth, 80.0);
                AddItem(new LeatherChest() { Movable = true, Hue = 1109 }); AddItem(new Dagger() { Movable = true });
            }
            else if (mClass == MercenaryClass.Paladin)
            {
                AI = AIType.AI_Melee;
                SetStr(140); SetDex(90); SetInt(70); SetHits(320); SetDamage(20, 30);
                SetSkill(SkillName.Swords, 80.0); SetSkill(SkillName.Tactics, 75.0); SetSkill(SkillName.Chivalry, 75.0); SetSkill(SkillName.Focus, 70.0);
                AddItem(new PlateChest() { Movable = true, Hue = 1266 }); AddItem(new Broadsword() { Movable = true });
            }
            else if (mClass == MercenaryClass.Necromancer)
            {
                AI = AIType.AI_Mage;
                SetStr(90); SetDex(80); SetInt(140); SetHits(180); SetMana(280); SetDamage(12, 20);
                SetSkill(SkillName.Necromancy, 80.0); SetSkill(SkillName.SpiritSpeak, 80.0); SetSkill(SkillName.MagicResist, 70.0);
                AddItem(new BoneChest() { Movable = true, Hue = 1102 }); AddItem(new Scythe() { Movable = true });
            }
            else if (mClass == MercenaryClass.Samurai)
            {
                AI = AIType.AI_Melee;
                SetStr(130); SetDex(110); SetInt(60); SetHits(260); SetDamage(22, 32);
                SetSkill(SkillName.Swords, 85.0); SetSkill(SkillName.Bushido, 80.0); SetSkill(SkillName.Tactics, 80.0);
                AddItem(new LeatherDo() { Movable = true, Hue = 2211 }); AddItem(new NoDachi() { Movable = true });
            }
            else if (mClass == MercenaryClass.ElfMage)
            {
                AI = AIType.AI_Mage;
                Body = 605;
                Name = NameList.RandomName("elf_male");
                SetStr(80); SetDex(90); SetInt(160); SetHits(170); SetMana(350); SetDamage(10, 16);
                SetSkill(SkillName.Magery, 90.0); SetSkill(SkillName.EvalInt, 90.0); SetSkill(SkillName.Meditation, 90.0);
                AddItem(new Robe() { Movable = true, Hue = 1270 }); AddItem(new WildStaff() { Movable = true });
            }
            // 🔥 НЕУЯЗВИМЫЙ И ПОЛНОЦЕННЫЙ СПАВН КЛАССА ГАРГУЛЬИ ЧЕРЕЗ ID КЛИЕНТА! 🪓🛡️⚙️
            else if (mClass == MercenaryClass.GargoyleWarrior)
            {
                AI = AIType.AI_Melee;
                Body = 666;
                Name = NameList.RandomName("gargoyle_male");
                BaseSoundID = 1345;

                SetStr(175); SetDex(100); SetInt(55); SetHits(380); SetDamage(24, 34);
                SetSkill(SkillName.Swords, 85.0); SetSkill(SkillName.Tactics, 80.0); SetSkill(SkillName.Anatomy, 70.0); SetSkill(SkillName.Parry, 70.0);

                // Генерируем Латный Горгулий Доспех напрямую через ID графики (0x4054), минуя проверку классов ядра!
                Item gChest = new Item(0x4054) { Name = "Gargoyle Kilt Armor", Movable = true, Hue = 1115, Layer = Layer.InnerTorso };
                AddItem(gChest);

                // Даем расовые латные поножи горгулий через графический ID клиента (0x405C)
                Item gLegs = new Item(0x405C) { Name = "Gargoyle Loincloth", Movable = true, Hue = 1115, Layer = Layer.Pants };
                AddItem(gLegs);

                // Выдаем массивный Горгулий Каменный Боевой Меч через ID графики (0x48A0) строго в правую руку!
                Item gSword = new Item(0x48A0) { Name = "Gargoyle Stone War Sword", Movable = true, Layer = Layer.OneHanded };
                AddItem(gSword);
            }

            SetResistance(ResistanceType.Physical, 45);
            SetResistance(ResistanceType.Fire, 35);
            SetResistance(ResistanceType.Cold, 35);
            SetResistance(ResistanceType.Poison, 35);
            SetResistance(ResistanceType.Energy, 35);
        }

        public override void OnDamage(int amount, Mobile from, bool willKill)
        {
            base.OnDamage(amount, from, willKill);

            if (willKill && this.Combatant is BaseCreature && m_Progression.Level < 10)
            {
                BaseCreature killed = this.Combatant as BaseCreature;
                if (killed != null && ControlMaster != null)
                {
                    int expEarned = Math.Max(15, killed.HitsMax / 3);
                    m_Progression.Experience += expEarned;
                    m_Progression.CheckLevelUp(this, m_Class);

                    Title = $"[{MercenaryProgression.GetRankTitle(m_Progression.Level)}]";
                    InvalidateProperties();
                }
            }
        }

        public override bool CanBeHarmedBy(Mobile from, bool message)
        {
            if (from != null && from.Player)
                return false;

            return base.CanBeHarmedBy(from, message);
        }

        public override int Damage(int amount, Mobile from, bool informMount, bool checkDisrupt)
        {
            if (from != null && from.Player)
                return 0;

            return base.Damage(amount, from, informMount, checkDisrupt);
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (IsMercenaryOwner(from) && (dropped is BaseWeapon || dropped is BaseArmor || dropped is BaseClothing))
            {
                Item current = FindItemOnLayer(dropped.Layer);
                if (current != null && current.Movable && !from.AddToBackpack(current))
                {
                    from.SendMessage("Освободите место в рюкзаке перед заменой экипировки.");
                    return false;
                }

                AddItem(dropped);
                from.PlaySound(0x57);
                PublicOverheadMessage(Network.MessageType.Regular, 0x35, false, "*Спасибо за снаряжение, командир!*");
                return true;
            }
            return base.OnDragDrop(from, dropped);
        }

        private bool IsMercenaryOwner(Mobile from)
        {
            return from != null && !from.Deleted && Controlled && ControlMaster == from;
        }

        public override bool AllowEquipFrom(Mobile from)
        {
            return IsMercenaryOwner(from);
        }

        public override bool CheckNonlocalLift(Mobile from, Item item)
        {
            return IsMercenaryOwner(from);
        }

        public override bool CheckNonlocalDrop(Mobile from, Item item, Item target)
        {
            return IsMercenaryOwner(from);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsMercenaryOwner(from) && from.InRange(this, 2))
                DisplayPaperdollTo(from);
            else
                base.OnDoubleClick(from);
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (IsMercenaryOwner(from) && from.Alive && from.InRange(this, 2))
                list.Add(new OpenBackpackEntry(from, this));
        }

        private class OpenBackpackEntry : ContextMenuEntry
        {
            private readonly Mobile m_From;
            private readonly BaseMercenary m_Mercenary;

            public OpenBackpackEntry(Mobile from, BaseMercenary mercenary) : base(3006145, 2)
            {
                m_From = from;
                m_Mercenary = mercenary;
            }

            public override void OnClick()
            {
                if (m_Mercenary != null && !m_Mercenary.Deleted &&
                    m_Mercenary.IsMercenaryOwner(m_From) && m_From.Alive && m_From.InRange(m_Mercenary, 2) &&
                    m_Mercenary.Backpack != null)
                {
                    m_Mercenary.Backpack.DisplayTo(m_From);
                }
            }
        }

        public override bool ClickTitle { get { return true; } }

        public BaseMercenary(Serial s) : base(s) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
            writer.Write((int)m_Class);
            writer.Write((int)m_Progression.Level);
            writer.Write((int)m_Progression.Experience);
            writer.Write((int)m_Progression.NextLevelExperience);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_Class = (MercenaryClass)reader.ReadInt();

            m_Progression = new MercenaryProgression();
            m_Progression.Level = reader.ReadInt();
            m_Progression.Experience = reader.ReadInt();
            m_Progression.NextLevelExperience = reader.ReadInt();

            foreach (Item item in Items)
            {
                if (item != null && !item.Deleted && item.Layer != Layer.Backpack)
                    item.Movable = true;
            }

            Title = $"[{MercenaryProgression.GetRankTitle(m_Progression.Level)}]";
        }
    }
}