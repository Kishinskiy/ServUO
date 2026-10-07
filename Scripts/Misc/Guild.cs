#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Server.Commands;
using Server.Commands.Generic;
using Server.Engines.Points;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

#endregion

namespace Server.Guilds
{
    #region Ranks

    [Flags]
    public enum RankFlags
    {
        None = 0x00000000,
        CanInvitePlayer = 0x00000001,
        AccessGuildItems = 0x00000002,
        RemoveLowestRank = 0x00000004,
        RemovePlayers = 0x00000008,
        CanPromoteDemote = 0x00000010,
        ControlWarStatus = 0x00000020,
        AllianceControl = 0x00000040,
        CanSetGuildTitle = 0x00000080,
        CanVote = 0x00000100,

        All =
            Member | CanInvitePlayer | RemovePlayers | CanPromoteDemote | ControlWarStatus | AllianceControl |
            CanSetGuildTitle,
        Member = RemoveLowestRank | AccessGuildItems | CanVote
    }

    public class RankDefinition
    {
        public static RankDefinition[] Ranks =
        {
            new RankDefinition(1062963, 0, RankFlags.None), //Ronin
            new RankDefinition(1062962, 1, RankFlags.Member), //Member
            new RankDefinition(
                1062961,
                2,
                RankFlags.Member | RankFlags.RemovePlayers | RankFlags.CanInvitePlayer | RankFlags.CanSetGuildTitle |
                RankFlags.CanPromoteDemote), //Emmissary
            new RankDefinition(1062960, 3, RankFlags.Member | RankFlags.ControlWarStatus), //Warlord
            new RankDefinition(1062959, 4, RankFlags.All) //Leader
        };

        public static RankDefinition Leader => Ranks[4];
        public static RankDefinition Member => Ranks[1];
        public static RankDefinition Lowest => Ranks[0];

        public TextDefinition Name { get; }

        public int Rank { get; }

        public RankFlags Flags { get; private set; }

        public RankDefinition(TextDefinition name, int rank, RankFlags flags)
        {
            Name = name;
            Rank = rank;
            Flags = flags;
        }

        public bool GetFlag(RankFlags flag)
        {
            return (Flags & flag) != 0;
        }

        public void SetFlag(RankFlags flag, bool value)
        {
            if (value)
                Flags |= flag;
            else
                Flags &= ~flag;
        }
    }

    #endregion

    #region Alliances

    public class AllianceInfo
    {
        private readonly List<Guild> m_PendingMembers;
        private Guild m_Leader;

        public static Dictionary<string, AllianceInfo> Alliances { get; } = new Dictionary<string, AllianceInfo>();

        public string Name { get; }

        public Guild Leader
        {
            get
            {
                CheckLeader();
                return m_Leader;
            }
            set
            {
                if (m_Leader != value && value != null)
                    AllianceMessage(1070765, value.Name); // Your Alliance is now led by ~1_GUILDNAME~

                m_Leader = value;

                if (m_Leader == null) CalculateAllianceLeader();
            }
        }

        public List<Guild> Members { get; }

        public AllianceInfo(Guild leader, string name, Guild partner)
        {
            m_Leader = leader;
            Name = name;

            Members = new List<Guild>();
            m_PendingMembers = new List<Guild>();

            leader.Alliance = this;
            partner.Alliance = this;

            if (!Alliances.ContainsKey(Name.ToLower())) Alliances.Add(Name.ToLower(), this);
        }

        public AllianceInfo(GenericReader reader)
        {
            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                {
                    Name = reader.ReadString();
                    m_Leader = reader.ReadGuild() as Guild;

                    Members = reader.ReadStrongGuildList<Guild>();
                    m_PendingMembers = reader.ReadStrongGuildList<Guild>();

                    break;
                }
            }
        }

        public void CalculateAllianceLeader()
        {
            m_Leader = Members.Count >= 2 ? Members[Utility.Random(Members.Count)] : null;
        }

        public void CheckLeader()
        {
            if (m_Leader == null || m_Leader.Disbanded)
            {
                CalculateAllianceLeader();

                if (m_Leader == null) Disband();
            }
        }

        public bool IsPendingMember(Guild g)
        {
            if (g.Alliance != this) return false;

            return m_PendingMembers.Contains(g);
        }

        public bool IsMember(Guild g)
        {
            if (g.Alliance != this) return false;

            return Members.Contains(g);
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write(0); //Version

            writer.Write(Name);
            writer.Write(m_Leader);

            writer.WriteGuildList(Members, true);
            writer.WriteGuildList(m_PendingMembers, true);

            if (!Alliances.ContainsKey(Name.ToLower())) Alliances.Add(Name.ToLower(), this);
        }

        public void AddPendingGuild(Guild g)
        {
            if (g.Alliance != this || m_PendingMembers.Contains(g) || Members.Contains(g)) return;

            m_PendingMembers.Add(g);
        }

        public void TurnToMember(Guild g)
        {
            if (g.Alliance != this || !m_PendingMembers.Contains(g) || Members.Contains(g)) return;

            g.GuildMessage(1070760, Name); // Your Guild has joined the ~1_ALLIANCENAME~ Alliance.
            AllianceMessage(1070761, g.Name); // A new Guild has joined your Alliance: ~1_GUILDNAME~

            m_PendingMembers.Remove(g);
            Members.Add(g);
            g.Alliance.InvalidateMemberProperties();
        }

        public void RemoveGuild(Guild g)
        {
            if (m_PendingMembers.Contains(g)) m_PendingMembers.Remove(g);

            if (Members.Contains(g)) //Sanity, just incase someone with a custom script adds a character to BOTH arrays
            {
                Members.Remove(g);
                g.InvalidateMemberProperties();

                g.GuildMessage(1070763, Name); // Your Guild has been removed from the ~1_ALLIANCENAME~ Alliance.
                AllianceMessage(1070764, g.Name); // A Guild has left your Alliance: ~1_GUILDNAME~
            }

            //g.Alliance = null;	//NO G.Alliance call here.  Set the Guild's Alliance to null, if you JUST use RemoveGuild, it removes it from the alliance, but doesn't remove the link from the guild to the alliance.  setting g.Alliance will call this method.
            //to check on OSI: have 3 guilds, make 2 of them a member, one pending.  remove one of the memebers.  alliance still exist?
            //ANSWER: NO

            if (g == m_Leader) CalculateAllianceLeader();
            /*
                if( m_Leader == null ) //only when m_members.count < 2
                Disband();
                else
                AllianceMessage( 1070765, m_Leader.Name ); // Your Alliance is now led by ~1_GUILDNAME~
                */
            if (Members.Count < 2) Disband();
        }

        public void Disband()
        {
            AllianceMessage(1070762); // Your Alliance has dissolved.

            for (var i = 0; i < m_PendingMembers.Count; i++) m_PendingMembers[i].Alliance = null;

            for (var i = 0; i < Members.Count; i++) Members[i].Alliance = null;

            AllianceInfo aInfo = null;

            Alliances.TryGetValue(Name.ToLower(), out aInfo);

            if (aInfo == this) Alliances.Remove(Name.ToLower());
        }

        public void InvalidateMemberProperties()
        {
            InvalidateMemberProperties(false);
        }

        public void InvalidateMemberProperties(bool onlyOPL)
        {
            for (var i = 0; i < Members.Count; i++)
            {
                var g = Members[i];

                g.InvalidateMemberProperties(onlyOPL);
            }
        }

        public void InvalidateMemberNotoriety()
        {
            for (var i = 0; i < Members.Count; i++) Members[i].InvalidateMemberNotoriety();
        }

        public class AllianceRosterGump : GuildDiplomacyGump
        {
            private readonly AllianceInfo m_Alliance;
            protected override bool AllowAdvancedSearch => false;

            public AllianceRosterGump(PlayerMobile pm, Guild g, AllianceInfo alliance)
                : base(pm, g, true, "", 0, alliance.Members, alliance.Name)
            {
                m_Alliance = alliance;
            }

            public AllianceRosterGump(
                PlayerMobile pm,
                Guild g,
                AllianceInfo alliance,
                IComparer<Guild> currentComparer,
                bool ascending,
                string filter,
                int startNumber)
                : base(pm, g, currentComparer, ascending, filter, startNumber, alliance.Members, alliance.Name)
            {
                m_Alliance = alliance;
            }

            public override Gump GetResentGump(
                PlayerMobile pm, Guild g, IComparer<Guild> comparer, bool ascending, string filter, int startNumber)
            {
                return new AllianceRosterGump(pm, g, m_Alliance, comparer, ascending, filter, startNumber);
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                if (info.ButtonID != 8) //So that they can't get to the AdvancedSearch button
                    base.OnResponse(sender, info);
            }
        }

        #region Alliance[Text]Message(...)

        public void AllianceMessage(int num, bool append, string format, params object[] args)
        {
            AllianceMessage(num, append, string.Format(format, args));
        }

        public void AllianceMessage(int number)
        {
            for (var i = 0; i < Members.Count; ++i) Members[i].GuildMessage(number);
        }

        public void AllianceMessage(int number, string args)
        {
            AllianceMessage(number, args, 0x3B2);
        }

        public void AllianceMessage(int number, string args, int hue)
        {
            for (var i = 0; i < Members.Count; ++i) Members[i].GuildMessage(number, args, hue);
        }

        public void AllianceMessage(int number, bool append, string affix)
        {
            AllianceMessage(number, append, affix, "", 0x3B2);
        }

        public void AllianceMessage(int number, bool append, string affix, string args)
        {
            AllianceMessage(number, append, affix, args, 0x3B2);
        }

        public void AllianceMessage(int number, bool append, string affix, string args, int hue)
        {
            for (var i = 0; i < Members.Count; ++i) Members[i].GuildMessage(number, append, affix, args, hue);
        }

        public void AllianceTextMessage(string text)
        {
            AllianceTextMessage(0x3B2, text);
        }

        public void AllianceTextMessage(string format, params object[] args)
        {
            AllianceTextMessage(0x3B2, string.Format(format, args));
        }

        public void AllianceTextMessage(int hue, string text)
        {
            for (var i = 0; i < Members.Count; ++i) Members[i].GuildTextMessage(hue, text);
        }

        public void AllianceTextMessage(int hue, string format, params object[] args)
        {
            AllianceTextMessage(hue, string.Format(format, args));
        }

        public void AllianceChat(Mobile from, int hue, string text)
        {
            Packet p = null;
            for (var i = 0; i < Members.Count; i++)
            {
                var g = Members[i];

                for (var j = 0; j < g.Members.Count; j++)
                {
                    var m = g.Members[j];

                    var state = m.NetState;

                    if (state != null)
                    {
                        if (p == null)
                            p =
                                Packet.Acquire(
                                    new UnicodeMessage(from.Serial, from.Body, MessageType.Alliance, hue, 3,
                                        from.Language, from.Name, text));

                        state.Send(p);
                    }
                }
            }

            Packet.Release(p);
        }

        public void AllianceChat(Mobile from, string text)
        {
            var pm = from as PlayerMobile;

            AllianceChat(from, pm == null ? 0x3B2 : pm.AllianceMessageHue, text);
        }

        #endregion
    }

    #endregion

    #region Wars

    public enum WarStatus
    {
        InProgress = -1,
        Win,
        Lose,
        Draw,
        Pending
    }

    public class WarDeclaration
    {
        public int Kills { get; set; }

        public int MaxKills { get; set; }

        public TimeSpan WarLength { get; set; }

        public Guild Opponent { get; }

        public Guild Guild { get; }

        public DateTime WarBeginning { get; set; }

        public bool WarRequester { get; set; }

        public WarStatus Status
        {
            get
            {
                if (Opponent == null || Opponent.Disbanded) return WarStatus.Win;

                if (Guild == null || Guild.Disbanded) return WarStatus.Lose;

                var w = Opponent.FindActiveWar(Guild);

                if (Opponent.FindPendingWar(Guild) != null && Guild.FindPendingWar(Opponent) != null)
                    return WarStatus.Pending;

                if (w == null) return WarStatus.Win;

                if (WarLength != TimeSpan.Zero && WarBeginning + WarLength < DateTime.UtcNow)
                {
                    if (Kills > w.Kills) return WarStatus.Win;

                    if (Kills < w.Kills) return WarStatus.Lose;

                    return WarStatus.Draw;
                }

                if (MaxKills > 0)
                {
                    if (Kills >= MaxKills) return WarStatus.Win;

                    if (w.Kills >= w.MaxKills) return WarStatus.Lose;
                }

                return WarStatus.InProgress;
            }
        }

        public WarDeclaration(Guild g, Guild opponent, int maxKills, TimeSpan warLength, bool warRequester)
        {
            Guild = g;
            MaxKills = maxKills;
            Opponent = opponent;
            WarLength = warLength;
            WarRequester = warRequester;
        }

        public WarDeclaration(GenericReader reader)
        {
            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                {
                    Kills = reader.ReadInt();
                    MaxKills = reader.ReadInt();

                    WarLength = reader.ReadTimeSpan();
                    WarBeginning = reader.ReadDateTime();

                    Guild = reader.ReadGuild() as Guild;
                    Opponent = reader.ReadGuild() as Guild;

                    WarRequester = reader.ReadBool();

                    break;
                }
            }
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write(0); //version

            writer.Write(Kills);
            writer.Write(MaxKills);

            writer.Write(WarLength);
            writer.Write(WarBeginning);

            writer.Write(Guild);
            writer.Write(Opponent);

            writer.Write(WarRequester);
        }
    }

    public class WarTimer : Timer
    {
        private static readonly TimeSpan InternalDelay = TimeSpan.FromMinutes(1.0);

        public WarTimer()
            : base(InternalDelay, InternalDelay)
        {
            Priority = TimerPriority.FiveSeconds;
        }

        public static void Initialize()
        {
            if (Guild.NewGuildSystem) new WarTimer().Start();
        }

        protected override void OnTick()
        {
            foreach (Guild g in BaseGuild.List.Values) g.CheckExpiredWars();
        }
    }

    #endregion

    public class Guild : BaseGuild
    {
        public static readonly int RegistrationFee = 25000;
        public static readonly int AbbrevLimit = 4;
        public static readonly int NameLimit = 40;
        public static readonly int MajorityPercentage = 66;
        public static readonly TimeSpan InactiveTime = TimeSpan.FromDays(30);

        public static bool NewGuildSystem => Core.SE;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Leader
        {
            get
            {
                if (m_Leader == null || m_Leader.Deleted || m_Leader.Guild != this) CalculateGuildmaster();

                return m_Leader;
            }
            set
            {
                if (value != null) AddMember(value); //Also removes from old guild.

                if (m_Leader is PlayerMobile && m_Leader.Guild == this)
                    ((PlayerMobile)m_Leader).GuildRank = RankDefinition.Member;

                m_Leader = value;

                if (m_Leader is PlayerMobile) ((PlayerMobile)m_Leader).GuildRank = RankDefinition.Leader;
            }
        }

        public override bool Disbanded => m_Leader == null || m_Leader.Deleted;

        public Guild(Mobile leader, string name, string abbreviation)
        {
            #region Ctor mumbo-jumbo

            m_Leader = leader;

            Members = new List<Mobile>();
            Allies = new List<Guild>();
            Enemies = new List<Guild>();
            WarDeclarations = new List<Guild>();
            WarInvitations = new List<Guild>();
            AllyDeclarations = new List<Guild>();
            AllyInvitations = new List<Guild>();
            Candidates = new List<Mobile>();
            Accepted = new List<Mobile>();

            LastFealty = DateTime.UtcNow;

            m_Name = name;
            m_Abbreviation = abbreviation;

            TypeLastChange = DateTime.MinValue;

            AddMember(m_Leader);

            if (m_Leader is PlayerMobile) ((PlayerMobile)m_Leader).GuildRank = RankDefinition.Leader;

            AcceptedWars = new List<WarDeclaration>();
            PendingWars = new List<WarDeclaration>();

            #endregion
        }

        public Guild(int id)
            : base(id) //serialization ctor
        {
        }

        public static void Configure()
        {
            EventSink.CreateGuild += EventSink_CreateGuild;
            EventSink.GuildGumpRequest += EventSink_GuildGumpRequest;

            CommandSystem.Register("GuildProps", AccessLevel.Counselor, GuildProps_OnCommand);
        }

        public void InvalidateMemberProperties()
        {
            InvalidateMemberProperties(false);
        }

        public void InvalidateMemberProperties(bool onlyOPL)
        {
            if (Members != null)
                for (var i = 0; i < Members.Count; i++)
                {
                    var m = Members[i];
                    m.InvalidateProperties();

                    if (!onlyOPL) m.Delta(MobileDelta.Noto);
                }
        }

        public void InvalidateMemberNotoriety()
        {
            if (Members != null)
                for (var i = 0; i < Members.Count; i++)
                    Members[i].Delta(MobileDelta.Noto);
        }

        public void InvalidateWarNotoriety()
        {
            var g = GetAllianceLeader(this);

            if (g.Alliance != null)
                g.Alliance.InvalidateMemberNotoriety();
            else
                g.InvalidateMemberNotoriety();

            if (g.AcceptedWars == null) return;

            foreach (var warDec in g.AcceptedWars)
            {
                var opponent = warDec.Opponent;

                if (opponent.Alliance != null)
                    opponent.Alliance.InvalidateMemberNotoriety();
                else
                    opponent.InvalidateMemberNotoriety();
            }
        }

        public override void OnDelete(Mobile mob)
        {
            RemoveMember(mob);
        }

        public void Disband()
        {
            m_Leader = null;

            List.Remove(Id);

            foreach (var m in Members)
            {
                m.SendLocalizedMessage(502131); // Your guild has disbanded.

                if (m is PlayerMobile) ((PlayerMobile)m).GuildRank = RankDefinition.Lowest;

                m.Guild = null;
                PointsSystem.ViceVsVirtue.OnResign(m, true);
            }

            Members.Clear();

            for (var i = Allies.Count - 1; i >= 0; --i)
                if (i < Allies.Count)
                    RemoveAlly(Allies[i]);

            for (var i = Enemies.Count - 1; i >= 0; --i)
                if (i < Enemies.Count)
                    RemoveEnemy(Enemies[i]);

            if (!NewGuildSystem && Guildstone != null) Guildstone.Delete();

            Guildstone = null;

            CheckExpiredWars();

            Alliance = null;
        }

        #region GuildProps

        [Usage("GuildProps")]
        [Description(
            "Opens a menu where you can view and edit guild properties of a targeted player or guild stone.  If the new Guild system is active, also brings up the guild gump."
        )]
        private static void GuildProps_OnCommand(CommandEventArgs e)
        {
            var arg = e.ArgString.Trim();
            var from = e.Mobile;

            if (arg.Length == 0)
            {
                e.Mobile.Target = new GuildPropsTarget();
            }
            else
            {
                Guild g = null;

                int id;

                if (int.TryParse(arg, out id)) g = Find(id) as Guild;

                if (g == null)
                {
                    g = FindByAbbrev(arg) as Guild;

                    if (g == null) g = FindByName(arg) as Guild;
                }

                if (g != null)
                {
                    from.SendGump(new PropertiesGump(from, g));

                    if (NewGuildSystem && from.AccessLevel >= AccessLevel.GameMaster && from is PlayerMobile)
                        from.SendGump(new GuildInfoGump((PlayerMobile)from, g));
                }
            }
        }

        private class GuildPropsTarget : Target
        {
            public GuildPropsTarget()
                : base(-1, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object o)
            {
                if (!BaseCommand.IsAccessible(from, o))
                {
                    from.SendMessage("That is not accessible.");
                    return;
                }

                Guild g = null;

                if (o is Guildstone)
                {
                    var stone = o as Guildstone;
                    if (stone.Guild == null || stone.Guild.Disbanded)
                    {
                        from.SendMessage("The guild associated with that Guildstone no longer exists");
                        return;
                    }

                    g = stone.Guild;
                }
                else if (o is Mobile)
                {
                    g = ((Mobile)o).Guild as Guild;
                }

                if (g != null)
                {
                    from.SendGump(new PropertiesGump(from, g));

                    if (NewGuildSystem && from.AccessLevel >= AccessLevel.GameMaster && from is PlayerMobile)
                        from.SendGump(new GuildInfoGump((PlayerMobile)from, g));
                }
                else
                {
                    from.SendMessage("That is not in a guild!");
                }
            }
        }

        #endregion

        #region EventSinks

        public static void EventSink_GuildGumpRequest(GuildGumpRequestArgs args)
        {
            var pm = args.Mobile as PlayerMobile;
            if (!NewGuildSystem || pm == null) return;

            if (pm.Guild == null)
                pm.SendGump(new CreateGuildGump(pm));
            else
                pm.SendGump(new GuildInfoGump(pm, pm.Guild as Guild));
        }

        public static void EventSink_CreateGuild(CreateGuildEventArgs args)
        {
            args.Guild = new Guild(args.Id);
        }

        #endregion

        #region New Alliances

        public AllianceInfo Alliance
        {
            get
            {
                if (m_AllianceInfo != null) return m_AllianceInfo;

                if (m_AllianceLeader != null) return m_AllianceLeader.m_AllianceInfo;

                return null;
            }
            set
            {
                var current = Alliance;

                if (value == current) return;

                if (current != null) current.RemoveGuild(this);

                if (value != null)
                {
                    if (value.Leader == this)
                        m_AllianceInfo = value;
                    else
                        m_AllianceLeader = value.Leader;

                    value.AddPendingGuild(this);
                }
                else
                {
                    m_AllianceInfo = null;
                    m_AllianceLeader = null;
                }
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public string AllianceName
        {
            get
            {
                var al = Alliance;
                if (al != null) return al.Name;

                return null;
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public Guild AllianceLeader
        {
            get
            {
                var al = Alliance;

                if (al != null) return al.Leader;

                return null;
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public bool IsAllianceMember
        {
            get
            {
                var al = Alliance;

                if (al != null) return al.IsMember(this);

                return false;
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public bool IsAlliancePendingMember
        {
            get
            {
                var al = Alliance;

                if (al != null) return al.IsPendingMember(this);

                return false;
            }
        }

        public static Guild GetAllianceLeader(Guild g)
        {
            var alliance = g.Alliance;

            if (alliance != null && alliance.Leader != null && alliance.IsMember(g)) return alliance.Leader;

            return g;
        }

        #endregion

        #region New Wars

        public List<WarDeclaration> PendingWars { get; private set; }

        public List<WarDeclaration> AcceptedWars { get; private set; }

        public WarDeclaration FindPendingWar(Guild g)
        {
            for (var i = 0; i < PendingWars.Count; i++)
            {
                var w = PendingWars[i];

                if (w.Opponent == g) return w;
            }

            return null;
        }

        public WarDeclaration FindActiveWar(Guild g)
        {
            for (var i = 0; i < AcceptedWars.Count; i++)
            {
                var w = AcceptedWars[i];

                if (w.Opponent == g) return w;
            }

            return null;
        }

        public void CheckExpiredWars()
        {
            for (var i = 0; i < AcceptedWars.Count; i++)
            {
                var w = AcceptedWars[i];
                var g = w.Opponent;

                var status = w.Status;

                if (status != WarStatus.InProgress)
                {
                    var myAlliance = Alliance;
                    var inAlliance = myAlliance != null && myAlliance.IsMember(this);

                    var otherAlliance = g != null ? g.Alliance : null;
                    var otherInAlliance = otherAlliance != null && otherAlliance.IsMember(this);

                    if (inAlliance)
                    {
                        myAlliance.AllianceMessage(
                            1070739 + (int)status,
                            g == null ? "a deleted opponent" : otherInAlliance ? otherAlliance.Name : g.Name);
                        myAlliance.InvalidateMemberProperties();
                    }
                    else
                    {
                        GuildMessage(
                            1070739 + (int)status,
                            g == null ? "a deleted opponent" : otherInAlliance ? otherAlliance.Name : g.Name);
                        InvalidateMemberProperties();
                    }

                    AcceptedWars.Remove(w);

                    if (g != null)
                    {
                        if (status != WarStatus.Draw) status = (WarStatus)((int)status + 1 % 2);

                        if (otherInAlliance)
                        {
                            otherAlliance.AllianceMessage(1070739 + (int)status, inAlliance ? Alliance.Name : Name);
                            otherAlliance.InvalidateMemberProperties();
                        }
                        else
                        {
                            g.GuildMessage(1070739 + (int)status, inAlliance ? Alliance.Name : Name);
                            g.InvalidateMemberProperties();
                        }

                        g.AcceptedWars.Remove(g.FindActiveWar(this));
                    }
                }
            }

            for (var i = 0; i < PendingWars.Count; i++)
            {
                var w = PendingWars[i];
                var g = w.Opponent;

                if (w.Status != WarStatus.Pending)
                {
                    //All sanity in here
                    PendingWars.Remove(w);

                    if (g != null) g.PendingWars.Remove(g.FindPendingWar(this));
                }
            }
        }

        public static void HandleDeath(Mobile victim)
        {
            HandleDeath(victim, null);
        }

        public static void HandleDeath(Mobile victim, Mobile killer)
        {
            if (!NewGuildSystem) return;

            if (killer == null) killer = victim.FindMostRecentDamager(false);

            if (killer == null || victim.Guild == null || killer.Guild == null) return;

            var victimGuild = GetAllianceLeader(victim.Guild as Guild);
            var killerGuild = GetAllianceLeader(killer.Guild as Guild);

            var war = killerGuild.FindActiveWar(victimGuild);

            if (war == null) return;

            war.Kills++;

            if (war.Opponent == victimGuild)
                killerGuild.CheckExpiredWars();
            else
                victimGuild.CheckExpiredWars();
        }

        #endregion

        #region Var declarations

        private Mobile m_Leader;

        private string m_Name;
        private string m_Abbreviation;

        private GuildType m_Type;

        private AllianceInfo m_AllianceInfo;
        private Guild m_AllianceLeader;

        #endregion

        #region Is<something>(...)

        public bool IsMember(Mobile m)
        {
            return Members.Contains(m);
        }

        public bool IsAlly(Guild g)
        {
            if (g == null) return false;

            if (NewGuildSystem) return Alliance != null && Alliance.IsMember(this) && Alliance.IsMember(g);

            return Allies.Contains(g);
        }

        public bool IsEnemy(Guild g)
        {
            if (g == null) return false;

            if (NewGuildSystem) return IsWar(g);

            if (m_Type != GuildType.Regular && g.m_Type != GuildType.Regular && m_Type != g.m_Type) return true;

            return Enemies.Contains(g);
        }

        public bool IsWar(Guild g)
        {
            if (g == null) return false;

            if (NewGuildSystem)
            {
                var guild = GetAllianceLeader(this);
                var otherGuild = GetAllianceLeader(g);

                if (guild.FindActiveWar(otherGuild) != null) return true;

                return false;
            }

            return Enemies.Contains(g);
        }

        #endregion

        #region Serialization

        public override void Serialize(GenericWriter writer)
        {
            if (LastFealty + TimeSpan.FromDays(1.0) < DateTime.UtcNow) CalculateGuildmaster();

            CheckExpiredWars();

            if (Alliance != null) Alliance.CheckLeader();

            writer.Write(5); //version

            #region War Serialization

            writer.Write(PendingWars.Count);

            for (var i = 0; i < PendingWars.Count; i++) PendingWars[i].Serialize(writer);

            writer.Write(AcceptedWars.Count);

            for (var i = 0; i < AcceptedWars.Count; i++) AcceptedWars[i].Serialize(writer);

            #endregion

            #region Alliances

            var isAllianceLeader = m_AllianceLeader == null && m_AllianceInfo != null;
            writer.Write(isAllianceLeader);

            if (isAllianceLeader)
                m_AllianceInfo.Serialize(writer);
            else
                writer.Write(m_AllianceLeader);

            #endregion

            //

            writer.WriteGuildList(AllyDeclarations, true);
            writer.WriteGuildList(AllyInvitations, true);

            writer.Write(TypeLastChange);

            writer.Write((int)m_Type);

            writer.Write(LastFealty);

            writer.Write(m_Leader);
            writer.Write(m_Name);
            writer.Write(m_Abbreviation);

            writer.WriteGuildList(Allies, true);
            writer.WriteGuildList(Enemies, true);
            writer.WriteGuildList(WarDeclarations, true);
            writer.WriteGuildList(WarInvitations, true);

            writer.Write(Members, true);
            writer.Write(Candidates, true);
            writer.Write(Accepted, true);

            writer.Write(Guildstone);
            writer.Write(Teleporter);

            writer.Write(Charter);
            writer.Write(Website);
        }

        public override void Deserialize(GenericReader reader)
        {
            var version = reader.ReadInt();

            switch (version)
            {
                case 5:
                {
                    var count = reader.ReadInt();

                    PendingWars = new List<WarDeclaration>();
                    for (var i = 0; i < count; i++) PendingWars.Add(new WarDeclaration(reader));

                    count = reader.ReadInt();
                    AcceptedWars = new List<WarDeclaration>();
                    for (var i = 0; i < count; i++) AcceptedWars.Add(new WarDeclaration(reader));

                    var isAllianceLeader = reader.ReadBool();

                    if (isAllianceLeader)
                        m_AllianceInfo = new AllianceInfo(reader);
                    else
                        m_AllianceLeader = reader.ReadGuild() as Guild;

                    goto case 4;
                }
                case 4:
                {
                    AllyDeclarations = reader.ReadStrongGuildList<Guild>();
                    AllyInvitations = reader.ReadStrongGuildList<Guild>();

                    goto case 3;
                }
                case 3:
                {
                    TypeLastChange = reader.ReadDateTime();

                    goto case 2;
                }
                case 2:
                {
                    m_Type = (GuildType)reader.ReadInt();

                    goto case 1;
                }
                case 1:
                {
                    LastFealty = reader.ReadDateTime();

                    goto case 0;
                }
                case 0:
                {
                    m_Leader = reader.ReadMobile();

                    if (m_Leader is PlayerMobile) ((PlayerMobile)m_Leader).GuildRank = RankDefinition.Leader;

                    m_Name = reader.ReadString();
                    m_Abbreviation = reader.ReadString();

                    Allies = reader.ReadStrongGuildList<Guild>();
                    Enemies = reader.ReadStrongGuildList<Guild>();
                    WarDeclarations = reader.ReadStrongGuildList<Guild>();
                    WarInvitations = reader.ReadStrongGuildList<Guild>();

                    Members = reader.ReadStrongMobileList();
                    Candidates = reader.ReadStrongMobileList();
                    Accepted = reader.ReadStrongMobileList();

                    Guildstone = reader.ReadItem();
                    Teleporter = reader.ReadItem();

                    Charter = reader.ReadString();
                    Website = reader.ReadString();

                    break;
                }
            }

            if (AllyDeclarations == null) AllyDeclarations = new List<Guild>();

            if (AllyInvitations == null) AllyInvitations = new List<Guild>();

            if (AcceptedWars == null) AcceptedWars = new List<WarDeclaration>();

            if (PendingWars == null) PendingWars = new List<WarDeclaration>();

            /*
            if ( ( !NewGuildSystem && m_Guildstone == null )|| m_Members.Count == 0 )
            Disband();
            */

            Timer.DelayCall(TimeSpan.Zero, VerifyGuild_Callback);
        }

        private void VerifyGuild_Callback()
        {
            if ((!NewGuildSystem && Guildstone == null) || Members.Count == 0) Disband();

            CheckExpiredWars();

            var alliance = Alliance;

            if (alliance != null) alliance.CheckLeader();

            alliance = Alliance; //CheckLeader could possibly change the value of this.Alliance

            if (alliance != null && !alliance.IsMember(this) && !alliance.IsPendingMember(this))
                //This block is there to fix a bug in the code in an older version.  
                Alliance = null;
            //Will call Alliance.RemoveGuild which will set it null & perform all the pertient checks as far as alliacne disbanding
        }

        #endregion

        #region Add/Remove Member/Old Ally/Old Enemy

        public void AddMember(Mobile m)
        {
            if (!Members.Contains(m))
            {
                if (m.Guild != null && m.Guild != this) ((Guild)m.Guild).RemoveMember(m);

                Members.Add(m);
                m.Guild = this;

                EventSink.InvokeJoinGuild(new JoinGuildEventArgs(m, this));

                if (!NewGuildSystem)
                    m.GuildFealty = m_Leader;
                else
                    m.GuildFealty = null;

                if (m is PlayerMobile) ((PlayerMobile)m).GuildRank = RankDefinition.Lowest;

                var guild = m.Guild as Guild;

                if (guild != null) guild.InvalidateWarNotoriety();
            }
        }

        public void RemoveMember(Mobile m)
        {
            RemoveMember(m, 1018028); // You have been dismissed from your guild.
        }

        public void RemoveMember(Mobile m, int message)
        {
            if (Members.Contains(m))
            {
                Members.Remove(m);

                var guild = m.Guild as Guild;

                m.Guild = null;

                if (m is PlayerMobile) ((PlayerMobile)m).GuildRank = RankDefinition.Lowest;

                if (message > 0) m.SendLocalizedMessage(message);

                if (m == m_Leader)
                {
                    CalculateGuildmaster();

                    if (m_Leader == null) Disband();
                }

                if (Members.Count == 0) Disband();

                if (guild != null) guild.InvalidateWarNotoriety();

                PointsSystem.ViceVsVirtue.OnResign(m, message == 1063411);

                m.Delta(MobileDelta.Noto);
            }
        }

        public void AddAlly(Guild g)
        {
            if (!Allies.Contains(g))
            {
                Allies.Add(g);

                g.AddAlly(this);
            }
        }

        public void RemoveAlly(Guild g)
        {
            if (Allies.Contains(g))
            {
                Allies.Remove(g);

                g.RemoveAlly(this);
            }
        }

        public void AddEnemy(Guild g)
        {
            if (!Enemies.Contains(g))
            {
                Enemies.Add(g);

                g.AddEnemy(this);
            }
        }

        public void RemoveEnemy(Guild g)
        {
            if (Enemies != null && Enemies.Contains(g))
            {
                Enemies.Remove(g);

                g.RemoveEnemy(this);
            }
        }

        #endregion

        #region Guild[Text]Message(...)

        public void GuildMessage(int num, bool append, string format, params object[] args)
        {
            GuildMessage(num, append, string.Format(format, args));
        }

        public void GuildMessage(int number)
        {
            foreach (var m in OnlineMembers) m.SendLocalizedMessage(number);
        }

        public void GuildMessage(int number, string args)
        {
            GuildMessage(number, args, 0x3B2);
        }

        public void GuildMessage(int number, string args, int hue)
        {
            foreach (var m in OnlineMembers) m.SendLocalizedMessage(number, args, hue);
        }

        public void GuildMessage(int number, bool append, string affix)
        {
            GuildMessage(number, append, affix, "", 0x3B2);
        }

        public void GuildMessage(int number, bool append, string affix, string args)
        {
            GuildMessage(number, append, affix, args, 0x3B2);
        }

        public void GuildMessage(int number, bool append, string affix, string args, int hue)
        {
            foreach (var m in OnlineMembers) m.SendLocalizedMessage(number, append, affix, args, hue);
        }

        public void GuildTextMessage(string text)
        {
            GuildTextMessage(0x3B2, text);
        }

        public void GuildTextMessage(string format, params object[] args)
        {
            GuildTextMessage(0x3B2, string.Format(format, args));
        }

        public void GuildTextMessage(int hue, string text)
        {
            foreach (var m in OnlineMembers) m.SendMessage(hue, text);
        }

        public void GuildTextMessage(int hue, string format, params object[] args)
        {
            GuildTextMessage(hue, string.Format(format, args));
        }

        public void GuildChat(Mobile from, int hue, string text)
        {
            Packet p = null;

            foreach (var m in OnlineMembers)
            {
                var state = m.NetState;

                if (state != null)
                {
                    if (p == null)
                        p =
                            Packet.Acquire(
                                new UnicodeMessage(from.Serial, from.Body, MessageType.Guild, hue, 3, from.Language,
                                    from.Name, text));

                    state.Send(p);
                }
            }

            Packet.Release(p);
        }

        public void GuildChat(Mobile from, string text)
        {
            var pm = from as PlayerMobile;

            GuildChat(from, pm == null ? 0x3B2 : pm.GuildMessageHue, text);
        }

        #endregion

        #region Voting

        public bool CanVote(Mobile m)
        {
            if (NewGuildSystem)
            {
                var pm = m as PlayerMobile;
                if (pm == null || !pm.GuildRank.GetFlag(RankFlags.CanVote)) return false;
            }

            return m != null && !m.Deleted && m.Guild == this;
        }

        public bool CanBeVotedFor(Mobile m)
        {
            if (NewGuildSystem)
            {
                var pm = m as PlayerMobile;
                if (pm == null || pm.LastOnline + InactiveTime < DateTime.UtcNow) return false;
            }

            return m != null && !m.Deleted && m.Guild == this;
        }

        public void CalculateGuildmaster()
        {
            var votes = new Dictionary<Mobile, int>();

            var votingMembers = 0;

            for (var i = 0; Members != null && i < Members.Count; ++i)
            {
                var memb = Members[i];

                if (!CanVote(memb)) continue;

                var m = memb.GuildFealty;

                if (!CanBeVotedFor(m))
                {
                    if (m_Leader != null && !m_Leader.Deleted && m_Leader.Guild == this)
                        m = m_Leader;
                    else
                        m = memb;
                }

                if (m == null) continue;

                int v;

                if (!votes.TryGetValue(m, out v))
                    votes[m] = 1;
                else
                    votes[m] = v + 1;

                votingMembers++;
            }

            Mobile winner = null;
            var highVotes = 0;

            foreach (var kvp in votes)
            {
                var m = kvp.Key;
                var val = kvp.Value;

                if (winner == null || val > highVotes)
                {
                    winner = m;
                    highVotes = val;
                }
            }

            if (NewGuildSystem && highVotes * 100 / Math.Max(votingMembers, 1) < MajorityPercentage &&
                m_Leader != null &&
                winner != m_Leader && !m_Leader.Deleted && m_Leader.Guild == this)
                winner = m_Leader;

            if (m_Leader != winner && winner != null)
                GuildMessage(1018015, true, winner.Name); // Guild Message: Guildmaster changed to:

            Leader = winner;
            LastFealty = DateTime.UtcNow;
        }

        #endregion

        #region Getters & Setters

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Guildstone { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Teleporter { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public override string Name
        {
            get => m_Name;
            set
            {
                m_Name = value;

                InvalidateMemberProperties(true);

                if (Guildstone != null) Guildstone.InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Website { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public override string Abbreviation
        {
            get => m_Abbreviation;
            set
            {
                m_Abbreviation = value;

                InvalidateMemberProperties(true);

                if (Guildstone != null) Guildstone.InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Charter { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public override GuildType Type
        {
            get => m_Type;
            set
            {
                if (m_Type != value)
                {
                    m_Type = value;
                    TypeLastChange = DateTime.UtcNow;

                    InvalidateMemberProperties();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastFealty { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime TypeLastChange { get; private set; }

        public List<Guild> Allies { get; private set; }

        public List<Guild> Enemies { get; private set; }

        public List<Guild> AllyDeclarations { get; private set; }

        public List<Guild> AllyInvitations { get; private set; }

        public List<Guild> WarDeclarations { get; private set; }

        public List<Guild> WarInvitations { get; private set; }

        public List<Mobile> Candidates { get; private set; }

        public List<Mobile> Accepted { get; private set; }

        public List<Mobile> Members { get; private set; }

        public IEnumerable<Mobile> OnlineMembers
        {
            get { return Members.Where(o => o.NetState != null && o.NetState.Running); }
        }

        public int OnlineMembersCount
        {
            get { return Members.Count(o => o.NetState != null && o.NetState.Running); }
        }

        #endregion
    }
}
