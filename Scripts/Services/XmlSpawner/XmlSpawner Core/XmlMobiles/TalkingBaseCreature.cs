using System;
using System.Collections;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Engines.XmlSpawner2;
using Server.Items;
using Server.Network;

/*
 ** TalkingBaseCreature
 ** A mobile that can be programmed with branching conversational sequences that are advanced by keywords at each sequence point.
 **
 ** 2/10/05
 ** modified to use the XmlDialog attachment
 */
namespace Server.Mobiles
{
    public class TalkingBaseCreature : BaseCreature
    {
        private DateTime lasteffect;


        public XmlDialog DialogAttachment { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int EItemID { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D EOffset { get; set; } = new Point3D(0, 0, 20);

        [CommandProperty(AccessLevel.GameMaster)]
        public int EDuration { get; set; } = 70;

        [CommandProperty(AccessLevel.GameMaster)]
        public int EHue { get; set; } = 68;

        [CommandProperty(AccessLevel.GameMaster)]
        public string TalkText { get; set; }

        // properties below are modified to access the equivalent XmlDialog properties
        // this is largely for backward compatibility, but it does also add some convenience

        public Mobile ActivePlayer
        {
            get
            {
                if (DialogAttachment != null)
                    return DialogAttachment.ActivePlayer;
                return null;
            }
            set
            {
                if (DialogAttachment != null)
                    DialogAttachment.ActivePlayer = value;
            }
        }

        public ArrayList SpeechEntries
        {
            get
            {
                if (DialogAttachment != null)
                    return DialogAttachment.SpeechEntries;
                return null;
            }
            set
            {
                if (DialogAttachment != null)
                    DialogAttachment.SpeechEntries = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan GameTOD
        {
            get
            {
                int hours;
                int minutes;

                Clock.GetTime(Map, Location.X, Location.Y, out hours, out minutes);
                return new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, hours, minutes, 0)
                    .TimeOfDay;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan RealTOD => DateTime.UtcNow.TimeOfDay;

        [CommandProperty(AccessLevel.GameMaster)]
        public int RealDay => DateTime.UtcNow.Day;

        [CommandProperty(AccessLevel.GameMaster)]
        public int RealMonth => DateTime.UtcNow.Month;

        [CommandProperty(AccessLevel.GameMaster)]
        public DayOfWeek RealDayOfWeek => DateTime.UtcNow.DayOfWeek;


        [CommandProperty(AccessLevel.GameMaster)]
        public MoonPhase MoonPhase => Clock.GetMoonPhase(Map, Location.X, Location.Y);

        [CommandProperty(AccessLevel.GameMaster)]
        public AccessLevel TriggerAccessLevel
        {
            get
            {
                if (DialogAttachment != null)
                    return DialogAttachment.TriggerAccessLevel;
                return AccessLevel.Player;
            }
            set
            {
                if (DialogAttachment != null)
                    DialogAttachment.TriggerAccessLevel = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastInteraction
        {
            get
            {
                if (DialogAttachment != null)
                    return DialogAttachment.LastInteraction;
                return DateTime.MinValue;
            }
            set
            {
                if (DialogAttachment != null)
                    DialogAttachment.LastInteraction = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool DoReset
        {
            get => false;
            set
            {
                if (DialogAttachment != null)
                    DialogAttachment.DoReset = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsActive
        {
            get
            {
                if (DialogAttachment != null)
                    return DialogAttachment.IsActive;
                return false;
            }
            set
            {
                if (DialogAttachment != null)
                    DialogAttachment.IsActive = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool AllowGhostTrig
        {
            get
            {
                if (DialogAttachment != null)
                    return DialogAttachment.AllowGhostTrig;
                return false;
            }
            set
            {
                if (DialogAttachment != null)
                    DialogAttachment.AllowGhostTrig = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Running
        {
            get
            {
                if (DialogAttachment != null)
                    return DialogAttachment.Running;
                return false;
            }
            set
            {
                if (DialogAttachment != null)
                    DialogAttachment.Running = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan ResetTime
        {
            get
            {
                if (DialogAttachment != null)
                    return DialogAttachment.ResetTime;
                return TimeSpan.Zero;
            }
            set
            {
                if (DialogAttachment != null)
                    DialogAttachment.ResetTime = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SpeechPace
        {
            get
            {
                if (DialogAttachment != null)
                    return DialogAttachment.SpeechPace;
                return 0;
            }
            set
            {
                if (DialogAttachment != null)
                    DialogAttachment.SpeechPace = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Keywords
        {
            get
            {
                if (DialogAttachment != null && DialogAttachment.CurrentEntry != null)
                    return DialogAttachment.CurrentEntry.Keywords;

                return null;
            }
            set
            {
                if (DialogAttachment != null && DialogAttachment.CurrentEntry != null)
                    DialogAttachment.CurrentEntry.Keywords = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Action
        {
            get
            {
                if (DialogAttachment != null && DialogAttachment.CurrentEntry != null)
                    return DialogAttachment.CurrentEntry.Action;

                return null;
            }
            set
            {
                if (DialogAttachment != null && DialogAttachment.CurrentEntry != null)
                    DialogAttachment.CurrentEntry.Action = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Condition
        {
            get
            {
                if (DialogAttachment != null && DialogAttachment.CurrentEntry != null)
                    return DialogAttachment.CurrentEntry.Condition;

                return null;
            }
            set
            {
                if (DialogAttachment != null && DialogAttachment.CurrentEntry != null)
                    DialogAttachment.CurrentEntry.Condition = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Text
        {
            get
            {
                if (DialogAttachment != null && DialogAttachment.CurrentEntry != null)
                    return DialogAttachment.CurrentEntry.Text;

                return null;
            }
            set
            {
                if (DialogAttachment != null && DialogAttachment.CurrentEntry != null)
                    DialogAttachment.CurrentEntry.Text = value;
            }
        }


        [CommandProperty(AccessLevel.GameMaster)]
        public string DependsOn
        {
            get
            {
                if (DialogAttachment != null && DialogAttachment.CurrentEntry != null)
                    return DialogAttachment.CurrentEntry.DependsOn;

                return "-1";
            }
            set
            {
                if (DialogAttachment != null && DialogAttachment.CurrentEntry != null)
                    DialogAttachment.CurrentEntry.DependsOn = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool LockConversation
        {
            get
            {
                if (DialogAttachment != null && DialogAttachment.CurrentEntry != null)
                    return DialogAttachment.CurrentEntry.LockConversation;

                return false;
            }
            set
            {
                if (DialogAttachment != null && DialogAttachment.CurrentEntry != null)
                    DialogAttachment.CurrentEntry.LockConversation = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public MessageType SpeechStyle
        {
            get
            {
                if (DialogAttachment != null && DialogAttachment.CurrentEntry != null)
                    return DialogAttachment.CurrentEntry.SpeechStyle;

                return MessageType.Regular;
            }
            set
            {
                if (DialogAttachment != null && DialogAttachment.CurrentEntry != null)
                    DialogAttachment.CurrentEntry.SpeechStyle = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool AllowNPCTrigger
        {
            get
            {
                if (DialogAttachment != null && DialogAttachment.CurrentEntry != null)
                    return DialogAttachment.CurrentEntry.AllowNPCTrigger;

                return false;
            }
            set
            {
                if (DialogAttachment != null && DialogAttachment.CurrentEntry != null)
                    DialogAttachment.CurrentEntry.AllowNPCTrigger = value;
            }
        }


        [CommandProperty(AccessLevel.GameMaster)]
        public int Pause
        {
            get
            {
                if (DialogAttachment != null && DialogAttachment.CurrentEntry != null)
                    return DialogAttachment.CurrentEntry.Pause;

                return -1;
            }
            set
            {
                if (DialogAttachment != null && DialogAttachment.CurrentEntry != null)
                    DialogAttachment.CurrentEntry.Pause = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PrePause
        {
            get
            {
                if (DialogAttachment != null && DialogAttachment.CurrentEntry != null)
                    return DialogAttachment.CurrentEntry.PrePause;

                return -1;
            }
            set
            {
                if (DialogAttachment != null && DialogAttachment.CurrentEntry != null)
                    DialogAttachment.CurrentEntry.PrePause = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ID
        {
            get
            {
                if (DialogAttachment != null && DialogAttachment.CurrentEntry != null)
                    return DialogAttachment.CurrentEntry.ID;

                return -1;
            }
            set
            {
                if (DialogAttachment != null && DialogAttachment.CurrentEntry != null)
                    DialogAttachment.CurrentEntry.ID = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int EntryNumber
        {
            get
            {
                if (DialogAttachment != null)
                    return DialogAttachment.EntryNumber;
                return -1;
            }
            set
            {
                if (DialogAttachment != null) DialogAttachment.EntryNumber = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ProximityRange
        {
            get
            {
                if (DialogAttachment != null)
                    return DialogAttachment.ProximityRange;
                return -1;
            }
            set
            {
                if (DialogAttachment != null) DialogAttachment.ProximityRange = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string ConfigFile
        {
            get
            {
                if (DialogAttachment != null)
                    return DialogAttachment.ConfigFile;
                return null;
            }
            set
            {
                if (DialogAttachment != null) DialogAttachment.ConfigFile = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool LoadConfig
        {
            get => false;
            set
            {
                if (value && DialogAttachment != null) DialogAttachment.DoLoadNPC(null, ConfigFile);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool SaveConfig
        {
            get => false;
            set
            {
                if (value && DialogAttachment != null)
                    DialogAttachment.DoSaveNPC(null, ConfigFile, false);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string TriggerOnCarried
        {
            get
            {
                if (DialogAttachment != null)
                    return DialogAttachment.TriggerOnCarried;
                return null;
            }
            set
            {
                if (DialogAttachment != null) DialogAttachment.TriggerOnCarried = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string NoTriggerOnCarried
        {
            get
            {
                if (DialogAttachment != null)
                    return DialogAttachment.NoTriggerOnCarried;
                return null;
            }
            set
            {
                if (DialogAttachment != null) DialogAttachment.NoTriggerOnCarried = value;
            }
        }

        public XmlDialog.SpeechEntry CurrentEntry
        {
            get
            {
                if (DialogAttachment != null)
                    return DialogAttachment.CurrentEntry;
                return null;
            }
            set
            {
                if (DialogAttachment != null) DialogAttachment.CurrentEntry = value;
            }
        }


        public TalkingBaseCreature(AIType ai,
            FightMode mode,
            int iRangePerception,
            int iRangeFight,
            double dActiveSpeed,
            double dPassiveSpeed) : base(ai, mode, iRangePerception, iRangeFight, dActiveSpeed, dPassiveSpeed)
        {
            // add the XmlDialog attachment
            DialogAttachment = new XmlDialog(default(string));
            XmlAttach.AttachTo(this, DialogAttachment);
        }

        public TalkingBaseCreature(Serial serial) : base(serial)
        {
        }

        public void DisplayHighlight()
        {
            if (EItemID > 0)
            {
                //SendOffsetTargetEffect(this, new Point3D(Location.X + EOffset.X, Location.Y + EOffset.Y, Location.Z + EOffset.Z), EItemID, 10, EDuration, EHue, 0);
                Effects.SendLocationEffect(
                    new Point3D(Location.X + EOffset.X, Location.Y + EOffset.Y, Location.Z + EOffset.Z), Map, EItemID,
                    EDuration, EHue, 0);

                lasteffect = DateTime.UtcNow;
            }
        }

        public static void SendOffsetTargetEffect(IEntity target, Point3D loc, int itemID, int speed, int duration,
            int hue, int renderMode)
        {
            if (target is Mobile)
                ((Mobile)target).ProcessDelta();

            Effects.SendPacket(loc, target.Map,
                new OffsetTargetEffect(target, loc, itemID, speed, duration, hue, renderMode));
        }

        public override void OnThink()
        {
            base.OnThink();

            if (lasteffect + TimeSpan.FromSeconds(1) < DateTime.UtcNow) DisplayHighlight();
        }

        public override bool Move(Direction d)
        {
            var didmove = base.Move(d);

            DisplayHighlight();

            return didmove;
        }

        public override bool OnDragDrop(Mobile from, Item item)
        {
            return XmlQuest.RegisterGive(from, this, item);

            //return base.OnDragDrop(from, item);
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            if (from.Alive)
                if (TalkText != null && TalkText.Length > 0 && DialogAttachment != null)
                    list.Add(new TalkEntry(this));

            base.GetContextMenuEntries(from, list);
        }

        public static void Initialize()
        {
            // reestablish the DialogAttachment assignment
            foreach (var m in World.Mobiles.Values)
                if (m is TalkingBaseCreature)
                {
                    var xa = XmlAttach.FindAttachment(m, typeof(XmlDialog)) as XmlDialog;
                    ((TalkingBaseCreature)m).DialogAttachment = xa;
                }
        }


        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(7); // version

            // version 7
            writer.Write(EItemID);
            writer.Write(EDuration);
            writer.Write(EOffset);
            writer.Write(EHue);

            // version 6
            writer.Write(TalkText);

            // Version 5
            // all serialized data now handled by the XmlDialog attachment
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (version < 5)
            {
                // have to add the XmlDialog attachment
                DialogAttachment = new XmlDialog(default(string));
                XmlAttach.AttachTo(this, DialogAttachment);
            }

            switch (version)
            {
                case 7:
                    EItemID = reader.ReadInt();
                    EDuration = reader.ReadInt();
                    EOffset = reader.ReadPoint3D();
                    EHue = reader.ReadInt();
                    goto case 6;
                case 6:
                    TalkText = reader.ReadString();
                    break;
                case 5:
                {
                    break;
                }
                case 4:
                {
                    var count = reader.ReadInt();

                    SpeechEntries = new ArrayList();
                    for (var i = 0; i < count; i++)
                    {
                        var newentry = new XmlDialog.SpeechEntry();

                        newentry.Condition = reader.ReadString();

                        SpeechEntries.Add(newentry);
                    }

                    goto case 3;
                }
                case 3:
                {
                    TriggerOnCarried = reader.ReadString();
                    NoTriggerOnCarried = reader.ReadString();
                    goto case 2;
                }
                case 2:
                {
                    SpeechPace = reader.ReadInt();

                    var count = reader.ReadInt();
                    if (version < 4) SpeechEntries = new ArrayList();
                    for (var i = 0; i < count; i++)
                        if (version < 4)
                        {
                            var newentry = new XmlDialog.SpeechEntry();

                            newentry.PrePause = reader.ReadInt();
                            newentry.LockConversation = reader.ReadBool();
                            newentry.AllowNPCTrigger = reader.ReadBool();
                            newentry.SpeechStyle = (MessageType)reader.ReadInt();

                            SpeechEntries.Add(newentry);
                        }
                        else
                        {
                            var newentry = (XmlDialog.SpeechEntry)SpeechEntries[i];

                            newentry.PrePause = reader.ReadInt();
                            newentry.LockConversation = reader.ReadBool();
                            newentry.AllowNPCTrigger = reader.ReadBool();
                            newentry.SpeechStyle = (MessageType)reader.ReadInt();
                        }

                    goto case 1;
                }
                case 1:
                {
                    ActivePlayer = reader.ReadMobile();
                    goto case 0;
                }
                case 0:
                {
                    IsActive = reader.ReadBool();
                    ResetTime = reader.ReadTimeSpan();
                    LastInteraction = reader.ReadDateTime();
                    AllowGhostTrig = reader.ReadBool();
                    ProximityRange = reader.ReadInt();
                    Running = reader.ReadBool();
                    ConfigFile = reader.ReadString();
                    var count = reader.ReadInt();
                    if (version < 2) SpeechEntries = new ArrayList();
                    for (var i = 0; i < count; i++)
                        if (version < 2)
                        {
                            var newentry = new XmlDialog.SpeechEntry();

                            newentry.EntryNumber = reader.ReadInt();
                            newentry.ID = reader.ReadInt();
                            newentry.Text = reader.ReadString();
                            newentry.Keywords = reader.ReadString();
                            newentry.Action = reader.ReadString();
                            newentry.DependsOn = reader.ReadInt().ToString();
                            newentry.Pause = reader.ReadInt();

                            SpeechEntries.Add(newentry);
                        }
                        else
                        {
                            var newentry = (XmlDialog.SpeechEntry)SpeechEntries[i];

                            newentry.EntryNumber = reader.ReadInt();
                            newentry.ID = reader.ReadInt();
                            newentry.Text = reader.ReadString();
                            newentry.Keywords = reader.ReadString();
                            newentry.Action = reader.ReadString();
                            newentry.DependsOn = reader.ReadInt().ToString();
                            newentry.Pause = reader.ReadInt();
                        }

                    // read in the current entry number. Note this will also set the current entry
                    EntryNumber = reader.ReadInt();
                    // restart the timer if it was active
                    var isrunning = reader.ReadBool();
                    if (isrunning)
                    {
                        var trigmob = reader.ReadMobile();
                        var delay = reader.ReadTimeSpan();
                        if (DialogAttachment != null)
                            DialogAttachment.DoTimer(delay, trigmob);
                    }

                    break;
                }
            }
        }

        public sealed class OffsetTargetEffect : HuedEffect
        {
            public OffsetTargetEffect(IEntity e, Point3D loc, int itemID, int speed, int duration, int hue,
                int renderMode)
                : base(EffectType.FixedFrom, e.Serial, Serial.Zero, itemID, loc, loc, speed, duration, true, false, hue,
                    renderMode)
            {
            }
        }

        private class TalkEntry : ContextMenuEntry
        {
            private readonly TalkingBaseCreature m_NPC;

            public TalkEntry(TalkingBaseCreature npc) : base(6146)
            {
                m_NPC = npc;
            }

            public override void OnClick()
            {
                var from = Owner.From;

                if (m_NPC == null || m_NPC.Deleted || !from.CheckAlive() || m_NPC.DialogAttachment == null)
                    return;

                // process the talk text
                //m_NPC.DialogAttachment.ProcessSpeech(from, m_NPC.TalkText);
                from.DoSpeech(m_NPC.TalkText, new int[] { }, MessageType.Regular, from.SpeechHue);
            }
        }
    }
}
