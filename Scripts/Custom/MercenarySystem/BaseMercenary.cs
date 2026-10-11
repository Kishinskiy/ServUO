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
            EventSink.ServerStarted += () => {
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
                SetStr(150); SetDex(80); SetInt(50);
                SetHits(300);
                SetDamage(15, 25);
                
                // ИСПРАВЛЕНО: SkillName.Parrying заменен на каноничный SkillName.Parry! 🛡️🎉
                SetSkill(SkillName.Swords, 70.0); SetSkill(SkillName.Tactics, 70.0); SetSkill(SkillName.Anatomy, 50.0); SetSkill(SkillName.Parry, 60.0);
                AddItem(new PlateChest() { Movable = false }); AddItem(new VikingSword() { Movable = false }); AddItem(new BronzeShield() { Movable = false });
            }
            else if (mClass == MercenaryClass.Mage)
            {
                AI = AIType.AI_Mage;
                SetStr(80); SetDex(80); SetInt(150);
                SetHits(150); SetMana(300);
                SetDamage(10, 15);
                SetSkill(SkillName.Magery, 80.0); SetSkill(SkillName.EvalInt, 80.0); SetSkill(SkillName.Meditation, 80.0); SetSkill(SkillName.Wrestling, 50.0);
                
                // ИСПРАВЛЕНО: Заменили MagicianRobe на нативную стандартную Robe ядра эмулятора! 🧙‍♂️📜
                AddItem(new Robe() { Movable = false, Hue = 2418 }); AddItem(new GnarledStaff() { Movable = false });
            }
            else if (mClass == MercenaryClass.Ranger)
            {
                AI = AIType.AI_Archer;
                SetStr(100); SetDex(150); SetInt(50);
                SetHits(200);
                SetDamage(20, 30);
                SetSkill(SkillName.Archery, 80.0); SetSkill(SkillName.Tactics, 70.0); SetSkill(SkillName.Anatomy, 70.0);
                AddItem(new LeatherChest() { Movable = false }); AddItem(new Bow() { Movable = false });
            }

            SetResistance(ResistanceType.Physical, 40);
            SetResistance(ResistanceType.Fire, 30);
            SetResistance(ResistanceType.Cold, 30);
            SetResistance(ResistanceType.Poison, 30);
            SetResistance(ResistanceType.Energy, 30);
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
