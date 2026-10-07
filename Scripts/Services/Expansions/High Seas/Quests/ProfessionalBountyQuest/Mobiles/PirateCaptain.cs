using System;
using System.Collections.Generic;
using Server.Engines.Quests;
using Server.Items;
using Server.Multis;

namespace Server.Mobiles
{
    public class PirateCaptain : BaseShipCaptain
    {
        public static List<string> m_RedNames;

        private DateTime m_NextTalk;

        public int PirateName { get; private set; }

        public int Adjective { get; private set; }

        public int Noun { get; private set; }

        public override bool AlwaysMurderer => true;
        public override bool Commandable => false;

        [Constructable]
        public PirateCaptain()
            : this(null)
        {
        }

        public PirateCaptain(BaseGalleon galleon)
            : base(galleon, AIType.AI_Melee, FightMode.Weakest, 25, 1, .2, .4)
        {
            PickRandomName();

            if (PirateName > 0 && (PirateName == 1116679 || PirateName == 1116680 || PirateName == 1116683))
                Female = true;

            SpeechHue = Utility.RandomDyedHue();
            Title = "the dread pirate";
            Hue = Race.RandomSkinHue();

            Body = Female ? 0x191 : 0x190;

            SetStr(500, 750);
            SetDex(125, 175);
            SetInt(61, 75);

            SetHits(4500, 5000);

            SetDamage(23, 35);

            SetSkill(SkillName.Fencing, 115.0, 120.0);
            SetSkill(SkillName.Macing, 115.0, 120.0);
            SetSkill(SkillName.MagicResist, 115.0, 120.0);
            SetSkill(SkillName.Swords, 115.0, 120.0);
            SetSkill(SkillName.Tactics, 115.0, 120.0);
            SetSkill(SkillName.Wrestling, 115.0, 120.0);
            SetSkill(SkillName.Anatomy, 115.0, 120.0);

            Item hat;
            Item fancyShirt = new FancyShirt();
            Item shirt = new Shirt(GetRandomShirtHue());
            shirt.Layer = Layer.OuterTorso;

            if (Utility.RandomBool())
                hat = new Bandana();
            else
                hat = new TricorneHat();

            hat.Hue = Utility.RandomNeutralHue();

            AddItem(new Boots());
            AddItem(shirt);
            AddItem(fancyShirt);
            AddItem(hat);
            AddItem(new Cloak(Utility.RandomNeutralHue()));

            switch (Utility.Random(7))
            {
                case 0: AddItem(new Longsword()); break;
                case 1: AddItem(new Cutlass()); break;
                case 2: AddItem(new Broadsword()); break;
                case 5: AddItem(new Dagger()); break;
            }

            Utility.AssignRandomHair(this);

            Fame = 22000;
            Karma = -22000;

            if (IsSoulboundEnemies)
                IsSoulbound = true;
        }

        public PirateCaptain(Serial serial)
            : base(serial)
        {
        }

        public static void Initialize()
        {
            m_RedNames = new List<string>();

            foreach (var mob in World.Mobiles.Values)
                if (mob != null && mob is PlayerMobile && mob.Murderer)
                    m_RedNames.Add(mob.Name);
        }

        public static int GetRandomShirtHue()
        {
            return Utility.RandomMinMax(2498, 2644);
        }

        public override void GenerateLoot()
        {
            AddLoot(LootPack.SuperBoss, 2);
        }

        public void PickRandomName()
        {
            Adjective = Utility.RandomMinMax(1116631, 1116650);
            Noun = Utility.RandomMinMax(1116651, 1116670);

            if (m_RedNames.Count == 0 || 0.90 > Utility.RandomDouble())
            {
                PirateName = Utility.RandomMinMax(1116671, 1116686);
            }
            else
            {
                PirateName = -1;
                Name = GetRandomRedName();
            }
        }

        public string GetRandomRedName()
        {
            return m_RedNames[Utility.Random(m_RedNames.Count)];
        }

        public override void AddNameProperties(ObjectPropertyList list)
        {
            string args;

            if (PirateName > 0)
                args = string.Format("#{0}\t#{1}\t#{2}", Adjective, Noun, PirateName);
            else
                args = string.Format("#{0}\t#{1}\t{2}", Adjective, Noun, Name);

            list.Add(1116690, args);
        }

        public override void OnThink()
        {
            base.OnThink();

            if (!IsCaught || m_NextTalk > DateTime.UtcNow)
                return;

            IPooledEnumerable eable = GetMobilesInRange(7);
            foreach (Mobile mob in eable)
                if (mob is PlayerMobile)
                {
                    OnTalk();
                    break;
                }

            eable.Free();
        }

        public void OnTalk()
        {
            Say(Utility.RandomMinMax(1149701, 1149720));
            m_NextTalk = DateTime.UtcNow + TimeSpan.FromMinutes(1);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);

            writer.Write(IsCaught);
            writer.Write(Adjective);
            writer.Write(Noun);
            writer.Write(PirateName);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            IsCaught = reader.ReadBool();
            Adjective = reader.ReadInt();
            Noun = reader.ReadInt();
            PirateName = reader.ReadInt();

            if (IsCaught)
                Frozen = true;

            m_NextTalk = DateTime.UtcNow;
        }

        #region Bounty Quest

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsCaught { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public ProfessionalBountyQuest Quest { get; set; }

        #endregion

        #region Quest Stuff

        public bool TryBound(Mobile from, BaseQuest quest)
        {
            if (from == null || Galleon == null || !Galleon.Contains(this) || quest == null)
                return false;

            if (IsCaught)
            {
                from.SendMessage("That pirate is already bound to a ship!");
                return false;
            }

            Combatant = null;
            Warmode = false;
            IsCaught = true;
            return true;
        }

        public void OnBound(ProfessionalBountyQuest quest)
        {
            if (quest == null || quest.Pole == null)
                return;

            var pole = quest.Pole;

            var x = pole.X;
            var y = pole.Y;

            while (x == pole.X && y == pole.Y)
            {
                x = Utility.RandomMinMax(pole.X - 1, pole.X + 1);
                y = Utility.RandomMinMax(pole.Y - 1, pole.Y + 1);
            }

            Frozen = true;

            var toDisarm = FindItemOnLayer(Layer.OneHanded);
            if (toDisarm == null || !toDisarm.Movable)
                toDisarm = FindItemOnLayer(Layer.TwoHanded);

            if (toDisarm != null)
            {
                if (Backpack != null)
                    Backpack.DropItem(toDisarm);
                else
                    toDisarm.Delete();
            }

            Quest = quest;

            if (quest != null && quest.Galleon != null)
                quest.Galleon.CapturedCaptain = this;

            Timer.DelayCall(TimeSpan.FromSeconds(2.5), new TimerStateCallback(MoveCaptainToShip),
                new object[] { x, y, pole });
        }

        private void MoveCaptainToShip(object obj)
        {
            var objs = (object[])obj;
            var x = (int)objs[0];
            var y = (int)objs[1];
            var pole = objs[2] as Item;

            if (pole != null)
                MoveToWorld(new Point3D(x, y, pole.Z), pole.Map);

            Blessed = true;
            Title = "[Captured Captain]";
        }

        public override bool OnBeforeDeath()
        {
            var hasQuest = new List<PlayerMobile>();
            var rights = GetLootingRights();
            for (var i = 0; i < rights.Count; i++)
            {
                if (!rights[i].m_HasRight)
                    continue;

                var mob = rights[i].m_Mobile;

                //if they have the quest and looting rights, give them a certificate
                if (mob is PlayerMobile && mob.NetState != null &&
                    QuestHelper.GetQuest((PlayerMobile)mob, typeof(ProfessionalBountyQuest)) != null)
                    hasQuest.Add((PlayerMobile)mob);
            }

            if (hasQuest.Count > 0)
            {
                var questee = hasQuest[Utility.Random(hasQuest.Count)];
                var q = QuestHelper.GetQuest(questee, typeof(ProfessionalBountyQuest));

                if (q != null && q is ProfessionalBountyQuest)
                {
                    ((ProfessionalBountyQuest)q).OnPirateDeath(this);
                    questee.AddToBackpack(new DeathCertificate(this));
                }
            }

            if (IsCaught && Quest != null)
                for (var i = 0; i < Quest.Objectives.Count; i++)
                    if (Quest.Objectives[i] is BountyQuestObjective &&
                        ((BountyQuestObjective)Quest.Objectives[i]).CapturedCaptain == this)
                    {
                        ((BountyQuestObjective)Quest.Objectives[i]).CapturedCaptain = null;
                        ((BountyQuestObjective)Quest.Objectives[i]).Captured = false;
                        Quest = null;
                    }

            return base.OnBeforeDeath();
        }

        #endregion
    }
}
