using System;
using Server.Items;

namespace Server.Engines.XmlSpawner2
{
    public class XmlSkill : XmlAttachment
    {
        private TimeSpan m_Duration = TimeSpan.FromMinutes(30.0); // 30 min default duration for effects

        // note that support for player identification requires modification of the identification skill (see the installation notes for details)
        private bool
            m_Identified; // optional identification flag that can suppress application of the mod until identified when applied to items

        [CommandProperty(AccessLevel.GameMaster)]
        // this property can be set allowing individual items to determine whether they must be identified for the mod to be activatable
        public bool RequireIdentification { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Value { get; set; } = 10;

        [CommandProperty(AccessLevel.GameMaster)]
        public SkillName Skill { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Duration
        {
            get => m_Duration;
            set => m_Duration = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string ActivationWord { get; set; }


        public override bool HandlesOnSpeech => true;

        // These are the various ways in which the message attachment can be constructed.
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public XmlSkill(ASerial serial) : base(serial)
        {
        }

        [Attachable]
        public XmlSkill(string name, string skill)
        {
            Name = name;
            try
            {
                Skill = (SkillName)Enum.Parse(typeof(SkillName), skill, true);
            }
            catch
            {
            }
        }

        [Attachable]
        public XmlSkill(string name, string skill, int value)
        {
            Name = name;
            try
            {
                Skill = (SkillName)Enum.Parse(typeof(SkillName), skill, true);
            }
            catch
            {
            }

            Value = value;
        }

        [Attachable]
        public XmlSkill(string name, string skill, int value, double duration)
        {
            Name = name;
            try
            {
                Skill = (SkillName)Enum.Parse(typeof(SkillName), skill, true);
            }
            catch
            {
            }

            Value = value;
            m_Duration = TimeSpan.FromMinutes(duration);
        }

        [Attachable]
        public XmlSkill(string name, string skill, int value, double duration, string word)
        {
            Name = name;
            try
            {
                Skill = (SkillName)Enum.Parse(typeof(SkillName), skill, true);
            }
            catch
            {
            }

            Value = value;
            m_Duration = TimeSpan.FromMinutes(duration);
            ActivationWord = word;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
            // version 0
            writer.Write(ActivationWord);
            writer.Write((int)Skill);
            writer.Write(Value);
            writer.Write(m_Duration);
            writer.Write(RequireIdentification);
            writer.Write(m_Identified);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
            // version 0
            ActivationWord = reader.ReadString();
            Skill = (SkillName)reader.ReadInt();
            Value = reader.ReadInt();
            m_Duration = reader.ReadTimeSpan();
            RequireIdentification = reader.ReadBool();
            m_Identified = reader.ReadBool();
        }

        public override string OnIdentify(Mobile from)
        {
            if (AttachedTo is BaseArmor || AttachedTo is BaseWeapon)
            {
                // can force identification before the skill mods can be applied
                if (from != null && from.AccessLevel == AccessLevel.Player) m_Identified = true;
                return string.Format("activated by {0} : skill {1} mod of {2} when equipped", ActivationWord, Skill,
                    Value);
            }

            return string.Format("activated by {0} : skill {1} mod of {2} lasting {3} mins", ActivationWord, Skill,
                Value, m_Duration.TotalMinutes);
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            base.OnSpeech(e);

            if (e.Mobile == null || e.Mobile.AccessLevel > AccessLevel.Player) return;

            // dont respond to other players speech if this is attached to a mob
            if (AttachedTo is Mobile && (Mobile)AttachedTo != e.Mobile) return;

            if (e.Speech == ActivationWord) OnTrigger(null, e.Mobile);
        }

        public override void OnAttach()
        {
            base.OnAttach();

            // apply the mod immediately
            if (AttachedTo is Mobile && ActivationWord == null)
            {
                OnTrigger(null, (Mobile)AttachedTo);
                // and then remove the attachment
                Timer.DelayCall(TimeSpan.Zero, Delete);
                //Delete();
            }
            else if (AttachedTo is Item && ActivationWord == null)
            {
                // no way to activate if it is on an item and is not speech activated so just delete it
                Delete();
            }
        }

        public override void OnTrigger(object activator, Mobile m)
        {
            if (m == null || (RequireIdentification && !m_Identified)) return;

            if ((AttachedTo is BaseArmor || AttachedTo is BaseWeapon) && ((Item)AttachedTo).Layer != Layer.Invalid)
            {
                // when activated via speech will apply mod when equipped by the speaker
                SkillMod sm = new EquipedSkillMod(Skill, true, Value, (Item)AttachedTo, m);
                m.AddSkillMod(sm);
                // and then remove the attachment
                Delete();
            }
            else
            {
                // when activated it will apply the skill mod that will last for the specified duration
                SkillMod sm = new TimedSkillMod(Skill, true, Value, m_Duration);
                m.AddSkillMod(sm);
                // and then remove the attachment
                Delete();
            }
        }
    }
}
