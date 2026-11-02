using System;
using Server;
using Server.Engines.Quests;
using Server.Mobiles;

namespace Server.Custom.GuildQuests
{
    public class GuildmasterQuestSystem : QuestSystem
    {
        private static readonly Type[] m_TypeReferenceTable =
        {
            typeof(GuildmasterQuestObjective)
        };

        public SkillName Skill { get; private set; }
        public Type GuildmasterType { get; private set; }

        public GuildmasterQuestSystem(PlayerMobile from, Type guildmasterType, SkillName skill)
            : base(from)
        {
            GuildmasterType = guildmasterType;
            Skill = skill;
        }

        public GuildmasterQuestSystem()
        {
        }

        public override object Name
        {
            get { return $"Испытание навыка {SkillInfo.Table[(int)Skill].Name}"; }
        }

        public override object OfferMessage
        {
            get
            {
                Type[] sources = SkillCapBreakSystem.GetGenericQuestTrophySources(Skill);
                string sourceNames = String.Join(", ", Array.ConvertAll(sources, GuildmasterSkillQuestGump.GetSourceName));
                string lore = GuildmasterSkillQuestGump.GetGuildmasterLore(GuildmasterType);

                return $"<BASEFONT COLOR=#FFFFFF>{lore}<BR><BR>Одолей {sourceNames} и принеси гильдмастеру трофей испытания навыка {SkillInfo.Table[(int)Skill].Name}. После этого гильдия позволит развивать навык до 100.0.<BR><BR>Ты принимаешь это испытание?</BASEFONT>";
            }
        }

        public override int Picture
        {
            get { return 0x14ED; }
        }

        public override bool IsTutorial
        {
            get { return false; }
        }

        public override TimeSpan RestartDelay
        {
            get { return TimeSpan.Zero; }
        }

        public override Type[] TypeReferenceTable
        {
            get { return m_TypeReferenceTable; }
        }

        public override void Accept()
        {
            if (From == null || From.Quest != null)
                return;

            base.Accept();

            if (From.Quest != this)
                return;

            From.PlaySound(0x5B4);
            AddObjective(new GuildmasterQuestObjective(Skill));
        }

        public override void ChildSerialize(GenericWriter writer)
        {
            writer.WriteEncodedInt(0);
            writer.WriteEncodedInt((int)Skill);
            writer.Write(GuildmasterType == null ? null : GuildmasterType.FullName);
        }

        public override void ChildDeserialize(GenericReader reader)
        {
            reader.ReadEncodedInt();
            Skill = (SkillName)reader.ReadEncodedInt();
            GuildmasterType = ScriptCompiler.FindTypeByFullName(reader.ReadString(), false);
        }

        public bool Matches(Type guildmasterType, SkillName skill)
        {
            return GuildmasterType == guildmasterType && Skill == skill;
        }

        public static bool CompleteTrackedQuest(PlayerMobile player, Type guildmasterType, SkillName skill)
        {
            GuildmasterQuestSystem system = player.Quest as GuildmasterQuestSystem;

            if (system == null || !system.Matches(guildmasterType, skill))
                return false;

            GuildmasterQuestObjective objective =
                system.FindObjective(typeof(GuildmasterQuestObjective)) as GuildmasterQuestObjective;

            if (objective == null)
                return false;

            objective.CurProgress = objective.MaxProgress;
            return true;
        }
    }

    public class GuildmasterQuestObjective : QuestObjective
    {
        public SkillName Skill { get; private set; }

        public GuildmasterQuestObjective()
        {
        }

        public GuildmasterQuestObjective(SkillName skill)
        {
            Skill = skill;
        }

        public override object Message
        {
            get
            {
                string skillName = SkillInfo.Table[(int)Skill].Name;

                if (CurProgress == 0)
                    return $"Одолей указанное гильдией существо и добудь трофей испытания навыка {skillName}. Затем вернись к гильдмастеру.";

                return $"Трофей испытания навыка {skillName} у тебя. Вернись к гильдмастеру, чтобы завершить испытание.";
            }
        }

        public override int MaxProgress
        {
            get { return 2; }
        }

        public override void RenderProgress(BaseQuestGump gump)
        {
            gump.AddHtml(70, 260, 270, 100,
                $"<BASEFONT COLOR=#0000FF>Этап испытания: {CurProgress}/{MaxProgress}</BASEFONT>",
                false, false);
        }

        public override bool GetTimerEvent()
        {
            return CurProgress == 0;
        }

        public override void CheckProgress()
        {
            PlayerMobile player = System == null ? null : System.From;

            if (player == null || player.Backpack == null)
                return;

            foreach (GuildmasterSkillTrophy trophy in player.Backpack.FindItemsByType<GuildmasterSkillTrophy>())
            {
                if (trophy.Skill == Skill)
                {
                    CurProgress = 1;
                    player.PlaySound(0x5B6);
                    System.ShowQuestLogUpdated();
                    break;
                }
            }
        }

        public override void OnComplete()
        {
            if (System == null)
                return;

            System.From.PlaySound(0x5B5);
            System.From.SendLocalizedMessage(1072273);
            System.Complete();
        }

        public override void ChildSerialize(GenericWriter writer)
        {
            writer.WriteEncodedInt(0);
            writer.WriteEncodedInt((int)Skill);
        }

        public override void ChildDeserialize(GenericReader reader)
        {
            reader.ReadEncodedInt();
            Skill = (SkillName)reader.ReadEncodedInt();
        }
    }
}
