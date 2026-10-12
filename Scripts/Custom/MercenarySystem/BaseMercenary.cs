using System;
using System.Collections.Generic;
using Server;
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
                AddItem(new PlateChest() { Movable = false }); AddItem(new VikingSword() { Movable = false }); AddItem(new BronzeShield() { Movable = false });
            }
            else if (mClass == MercenaryClass.Mage)
            {
                AI = AIType.AI_Mage;
                SetStr(80); SetDex(80); SetInt(150); SetHits(150); SetMana(300); SetDamage(10, 15);
                SetSkill(SkillName.Magery, 80.0); SetSkill(SkillName.EvalInt, 80.0); SetSkill(SkillName.Meditation, 80.0); SetSkill(SkillName.Wrestling, 50.0);
                AddItem(new Robe() { Movable = false, Hue = 2418 }); AddItem(new GnarledStaff() { Movable = false });
            }
            else if (mClass == MercenaryClass.Ranger)
            {
                AI = AIType.AI_Archer;
                SetStr(100); SetDex(150); SetInt(50); SetHits(200); SetDamage(20, 30);
                SetSkill(SkillName.Archery, 80.0); SetSkill(SkillName.Tactics, 70.0); SetSkill(SkillName.Anatomy, 70.0);
                AddItem(new LeatherChest() { Movable = false }); AddItem(new Bow() { Movable = false });
            }
            else if (mClass == MercenaryClass.Bard)
            {
                AI = AIType.AI_Mage;
                SetStr(100); SetDex(100); SetInt(100); SetHits(220); SetDamage(12, 18);
                SetSkill(SkillName.Musicianship, 80.0); SetSkill(SkillName.Discordance, 80.0); SetSkill(SkillName.Magery, 60.0);
                AddItem(new JesterSuit() { Movable = false, Hue = 1161 }); AddItem(new Tambourine() { Movable = false });
            }
            else if (mClass == MercenaryClass.Thief)
            {
                AI = AIType.AI_Melee;
                SetStr(90); SetDex(160); SetInt(50); SetHits(200); SetDamage(18, 28);
                SetSkill(SkillName.Fencing, 85.0); SetSkill(SkillName.Tactics, 75.0); SetSkill(SkillName.Stealth, 80.0);
                AddItem(new LeatherChest() { Movable = false, Hue = 1109 }); AddItem(new Dagger() { Movable = false });
            }
            else if (mClass == MercenaryClass.Paladin)
            {
                AI = AIType.AI_Melee;
                SetStr(140); SetDex(90); SetInt(70); SetHits(320); SetDamage(20, 30);
                SetSkill(SkillName.Swords, 80.0); SetSkill(SkillName.Tactics, 75.0); SetSkill(SkillName.Chivalry, 75.0); SetSkill(SkillName.Focus, 70.0);
                AddItem(new PlateChest() { Movable = false, Hue = 1266 }); AddItem(new Broadsword() { Movable = false });
            }
            else if (mClass == MercenaryClass.Necromancer)
            {
                AI = AIType.AI_Mage;
                SetStr(90); SetDex(80); SetInt(140); SetHits(180); SetMana(280); SetDamage(12, 20);
                SetSkill(SkillName.Necromancy, 80.0); SetSkill(SkillName.SpiritSpeak, 80.0); SetSkill(SkillName.MagicResist, 70.0);
                AddItem(new BoneChest() { Movable = false, Hue = 1102 }); AddItem(new Scythe() { Movable = false });
            }
            else if (mClass == MercenaryClass.Samurai)
            {
                AI = AIType.AI_Melee;
                SetStr(130); SetDex(110); SetInt(60); SetHits(260); SetDamage(22, 32);
                SetSkill(SkillName.Swords, 85.0); SetSkill(SkillName.Bushido, 80.0); SetSkill(SkillName.Tactics, 80.0);
                AddItem(new LeatherDo() { Movable = false, Hue = 2211 }); AddItem(new NoDachi() { Movable = false });
            }
            else if (mClass == MercenaryClass.ElfMage)
            {
                AI = AIType.AI_Mage;
                Body = 605;
                Name = NameList.RandomName("elf_male");
                SetStr(80); SetDex(90); SetInt(160); SetHits(170); SetMana(350); SetDamage(10, 16);
                SetSkill(SkillName.Magery, 90.0); SetSkill(SkillName.EvalInt, 90.0); SetSkill(SkillName.Meditation, 90.0);
                AddItem(new Robe() { Movable = false, Hue = 1270 }); AddItem(new WildStaff() { Movable = false });
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
                Item gChest = new Item(0x4054) { Name = "Gargoyle Kilt Armor", Movable = false, Hue = 1115, Layer = Layer.InnerTorso };
                AddItem(gChest);

                // Даем расовые латные поножи горгулий через графический ID клиента (0x405C)
                Item gLegs = new Item(0x405C) { Name = "Gargoyle Loincloth", Movable = false, Hue = 1115, Layer = Layer.Pants };
                AddItem(gLegs);

                // Выдаем массивный Горгулий Каменный Боевой Меч через ID графики (0x48A0) строго в правую руку!
                Item gSword = new Item(0x48A0) { Name = "Gargoyle Stone War Sword", Movable = false, Layer = Layer.OneHanded };
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

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (from == ControlMaster && (dropped is BaseWeapon || dropped is BaseArmor || dropped is BaseClothing))
            {
                Item current = FindItemOnLayer(dropped.Layer);
                if (current != null && current.Movable)
                {
                    from.AddToBackpack(current);
                }

                AddItem(dropped);
                from.PlaySound(0x57);
                PublicOverheadMessage(Network.MessageType.Regular, 0x35, false, "*Спасибо за снаряжение, командир!*");
                return true;
            }
            return base.OnDragDrop(from, dropped);
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

            Title = $"[{MercenaryProgression.GetRankTitle(m_Progression.Level)}]";
        }
    }
}