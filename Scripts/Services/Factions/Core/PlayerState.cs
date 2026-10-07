using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Factions
{
    public class PlayerState : IComparable
    {
        private Town m_Finance;
        private bool m_InvalidateRank = true;
        private int m_KillPoints;
        private MerchantTitle m_MerchantTitle;
        private RankDefinition m_Rank;
        private int m_RankIndex = -1;
        private Town m_Sheriff;

        public Mobile Mobile { get; }

        public Faction Faction { get; }

        public List<PlayerState> Owner { get; }

        public MerchantTitle MerchantTitle
        {
            get => m_MerchantTitle;
            set
            {
                m_MerchantTitle = value;
                Invalidate();
            }
        }

        public Town Sheriff
        {
            get => m_Sheriff;
            set
            {
                m_Sheriff = value;
                Invalidate();
            }
        }

        public Town Finance
        {
            get => m_Finance;
            set
            {
                m_Finance = value;
                Invalidate();
            }
        }

        public List<SilverGivenEntry> SilverGiven { get; private set; }

        public int KillPoints
        {
            get => m_KillPoints;
            set
            {
                if (m_KillPoints != value)
                {
                    if (value > m_KillPoints)
                    {
                        if (m_KillPoints <= 0)
                        {
                            if (value <= 0)
                            {
                                m_KillPoints = value;
                                Invalidate();
                                return;
                            }

                            Owner.Remove(this);
                            Owner.Insert(Faction.ZeroRankOffset, this);

                            m_RankIndex = Faction.ZeroRankOffset;
                            Faction.ZeroRankOffset++;
                        }

                        while (m_RankIndex - 1 >= 0)
                        {
                            var p = Owner[m_RankIndex - 1];
                            if (value > p.KillPoints)
                            {
                                Owner[m_RankIndex] = p;
                                Owner[m_RankIndex - 1] = this;
                                RankIndex--;
                                p.RankIndex++;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (value <= 0)
                        {
                            if (m_KillPoints <= 0)
                            {
                                m_KillPoints = value;
                                Invalidate();
                                return;
                            }

                            while (m_RankIndex + 1 < Faction.ZeroRankOffset)
                            {
                                var p = Owner[m_RankIndex + 1];
                                Owner[m_RankIndex + 1] = this;
                                Owner[m_RankIndex] = p;
                                RankIndex++;
                                p.RankIndex--;
                            }

                            m_RankIndex = -1;
                            Faction.ZeroRankOffset--;
                        }
                        else
                        {
                            while (m_RankIndex + 1 < Faction.ZeroRankOffset)
                            {
                                var p = Owner[m_RankIndex + 1];
                                if (value < p.KillPoints)
                                {
                                    Owner[m_RankIndex + 1] = this;
                                    Owner[m_RankIndex] = p;
                                    RankIndex++;
                                    p.RankIndex--;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }

                    m_KillPoints = value;
                    Invalidate();
                }
            }
        }

        public int RankIndex
        {
            get => m_RankIndex;
            set
            {
                if (m_RankIndex != value)
                {
                    m_RankIndex = value;
                    m_InvalidateRank = true;
                }
            }
        }

        public RankDefinition Rank
        {
            get
            {
                if (m_InvalidateRank)
                {
                    var ranks = Faction.Definition.Ranks;
                    int percent;

                    if (Owner.Count == 1)
                        percent = 1000;
                    else if (m_RankIndex == -1)
                        percent = 0;
                    else
                        percent = (Faction.ZeroRankOffset - m_RankIndex) * 1000 / Faction.ZeroRankOffset;

                    for (var i = 0; i < ranks.Length; i++)
                    {
                        var check = ranks[i];

                        if (percent >= check.Required)
                        {
                            m_Rank = check;
                            m_InvalidateRank = false;
                            break;
                        }
                    }

                    Invalidate();
                }

                return m_Rank;
            }
        }

        public DateTime LastHonorTime { get; set; }

        public DateTime Leaving { get; set; }

        public bool IsLeaving => Leaving > DateTime.MinValue;

        public bool IsActive { get; set; }

        public PlayerState(Mobile mob, Faction faction, List<PlayerState> owner)
        {
            Mobile = mob;
            Faction = faction;
            Owner = owner;

            Attach();
            Invalidate();
        }

        public PlayerState(GenericReader reader, Faction faction, List<PlayerState> owner)
        {
            Faction = faction;
            Owner = owner;

            var version = reader.ReadEncodedInt();

            switch (version)
            {
                case 1:
                {
                    IsActive = reader.ReadBool();
                    LastHonorTime = reader.ReadDateTime();
                    goto case 0;
                }
                case 0:
                {
                    Mobile = reader.ReadMobile();

                    m_KillPoints = reader.ReadEncodedInt();
                    m_MerchantTitle = (MerchantTitle)reader.ReadEncodedInt();

                    Leaving = reader.ReadDateTime();

                    break;
                }
            }

            Attach();
        }

        public int CompareTo(object obj)
        {
            return ((PlayerState)obj).m_KillPoints - m_KillPoints;
        }

        public static PlayerState Find(Mobile mob)
        {
            if (mob is PlayerMobile)
                return ((PlayerMobile)mob).FactionPlayerState;

            return null;
        }

        public bool CanGiveSilverTo(Mobile mob)
        {
            if (SilverGiven == null)
                return true;

            for (var i = 0; i < SilverGiven.Count; ++i)
            {
                var sge = SilverGiven[i];

                if (sge.IsExpired)
                    SilverGiven.RemoveAt(i--);
                else if (sge.GivenTo == mob)
                    return false;
            }

            return true;
        }

        public void OnGivenSilverTo(Mobile mob)
        {
            if (SilverGiven == null)
                SilverGiven = new List<SilverGivenEntry>();

            SilverGiven.Add(new SilverGivenEntry(mob));
        }

        public void Invalidate()
        {
            if (Mobile is PlayerMobile)
            {
                var pm = (PlayerMobile)Mobile;
                pm.InvalidateProperties();
            }
        }

        public void Attach()
        {
            if (Settings.Enabled && Mobile is PlayerMobile)
                ((PlayerMobile)Mobile).FactionPlayerState = this;
        }

        public void Serialize(GenericWriter writer)
        {
            writer.WriteEncodedInt(1); // version

            writer.Write(IsActive);
            writer.Write(LastHonorTime);

            writer.Write(Mobile);

            writer.WriteEncodedInt(m_KillPoints);
            writer.WriteEncodedInt((int)m_MerchantTitle);

            writer.Write(Leaving);
        }
    }
}
