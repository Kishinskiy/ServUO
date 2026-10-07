using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Server
{
    public class Firewall
    {
        public static List<IFirewallEntry> List { get; }

        static Firewall()
        {
            List = new List<IFirewallEntry>();

            var path = "firewall.cfg";

            if (File.Exists(path))
                using (var ip = new StreamReader(path))
                {
                    string line;

                    while ((line = ip.ReadLine()) != null)
                    {
                        line = line.Trim();

                        if (line.Length == 0)
                            continue;

                        List.Add(ToFirewallEntry(line));
                        /*
                        object toAdd;
                        IPAddress addr;
                        if( IPAddress.TryParse( line, out addr ) )
                        toAdd = addr;
                        else
                        toAdd = line;
                        m_Blocked.Add( toAdd.ToString() );
                        * */
                    }
                }
        }

        public static IFirewallEntry ToFirewallEntry(object entry)
        {
            if (entry is IFirewallEntry)
                return (IFirewallEntry)entry;
            if (entry is IPAddress)
                return new IPFirewallEntry((IPAddress)entry);
            if (entry is string)
                return ToFirewallEntry((string)entry);

            return null;
        }

        public static IFirewallEntry ToFirewallEntry(string entry)
        {
            IPAddress addr;

            if (IPAddress.TryParse(entry, out addr))
                return new IPFirewallEntry(addr);

            //Try CIDR parse
            var str = entry.Split('/');

            if (str.Length == 2)
            {
                IPAddress cidrPrefix;

                if (IPAddress.TryParse(str[0], out cidrPrefix))
                {
                    int cidrLength;

                    if (int.TryParse(str[1], out cidrLength))
                        return new CIDRFirewallEntry(cidrPrefix, cidrLength);
                }
            }

            return new WildcardIPFirewallEntry(entry);
        }

        public static void RemoveAt(int index)
        {
            List.RemoveAt(index);
            Save();
        }

        public static void Remove(object obj)
        {
            var entry = ToFirewallEntry(obj);

            if (entry != null)
            {
                List.Remove(entry);
                Save();
            }
        }

        public static void Add(object obj)
        {
            if (obj is IPAddress)
                Add((IPAddress)obj);
            else if (obj is string)
                Add((string)obj);
            else if (obj is IFirewallEntry)
                Add((IFirewallEntry)obj);
        }

        public static void Add(IFirewallEntry entry)
        {
            if (!List.Contains(entry))
                List.Add(entry);

            Save();
        }

        public static void Add(string pattern)
        {
            var entry = ToFirewallEntry(pattern);

            if (!List.Contains(entry))
                List.Add(entry);

            Save();
        }

        public static void Add(IPAddress ip)
        {
            IFirewallEntry entry = new IPFirewallEntry(ip);

            if (!List.Contains(entry))
                List.Add(entry);

            Save();
        }

        public static void Save()
        {
            var path = "firewall.cfg";

            using (var op = new StreamWriter(path))
            {
                for (var i = 0; i < List.Count; ++i)
                    op.WriteLine(List[i]);
            }
        }

        public static bool IsBlocked(IPAddress ip)
        {
            for (var i = 0; i < List.Count; i++)
                if (List[i].IsBlocked(ip))
                    return true;

            return false;
            /*
            bool contains = false;
            for ( int i = 0; !contains && i < m_Blocked.Count; ++i )
            {
            if ( m_Blocked[i] is IPAddress )
            contains = ip.Equals( m_Blocked[i] );
            else if ( m_Blocked[i] is String )
            {
            string s = (string)m_Blocked[i];
            contains = Utility.IPMatchCIDR( s, ip );
            if( !contains )
            contains = Utility.IPMatch( s, ip );
            }
            }
            return contains;
            * */
        }

        #region Firewall Entries

        public interface IFirewallEntry
        {
            bool IsBlocked(IPAddress address);
        }

        public class IPFirewallEntry : IFirewallEntry
        {
            private readonly IPAddress m_Address;

            public IPFirewallEntry(IPAddress address)
            {
                m_Address = address;
            }

            public bool IsBlocked(IPAddress address)
            {
                return m_Address.Equals(address);
            }

            public override string ToString()
            {
                return m_Address.ToString();
            }

            public override bool Equals(object obj)
            {
                if (obj is IPAddress) return obj.Equals(m_Address);

                if (obj is string)
                {
                    IPAddress otherAddress;

                    if (IPAddress.TryParse((string)obj, out otherAddress))
                        return otherAddress.Equals(m_Address);
                }
                else if (obj is IPFirewallEntry)
                {
                    return m_Address.Equals(((IPFirewallEntry)obj).m_Address);
                }

                return false;
            }

            public override int GetHashCode()
            {
                return m_Address.GetHashCode();
            }
        }

        public class CIDRFirewallEntry : IFirewallEntry
        {
            private readonly int m_CIDRLength;
            private readonly IPAddress m_CIDRPrefix;

            public CIDRFirewallEntry(IPAddress cidrPrefix, int cidrLength)
            {
                m_CIDRPrefix = cidrPrefix;
                m_CIDRLength = cidrLength;
            }

            public bool IsBlocked(IPAddress address)
            {
                return Utility.IPMatchCIDR(m_CIDRPrefix, address, m_CIDRLength);
            }

            public override string ToString()
            {
                return string.Format("{0}/{1}", m_CIDRPrefix, m_CIDRLength);
            }

            public override bool Equals(object obj)
            {
                if (obj is string)
                {
                    var entry = (string)obj;

                    var str = entry.Split('/');

                    if (str.Length == 2)
                    {
                        IPAddress cidrPrefix;

                        if (IPAddress.TryParse(str[0], out cidrPrefix))
                        {
                            int cidrLength;

                            if (int.TryParse(str[1], out cidrLength))
                                return m_CIDRPrefix.Equals(cidrPrefix) && m_CIDRLength.Equals(cidrLength);
                        }
                    }
                }
                else if (obj is CIDRFirewallEntry)
                {
                    var entry = obj as CIDRFirewallEntry;

                    return m_CIDRPrefix.Equals(entry.m_CIDRPrefix) && m_CIDRLength.Equals(entry.m_CIDRLength);
                }

                return false;
            }

            public override int GetHashCode()
            {
                return m_CIDRPrefix.GetHashCode() ^ m_CIDRLength.GetHashCode();
            }
        }

        public class WildcardIPFirewallEntry : IFirewallEntry
        {
            private readonly string m_Entry;

            private bool m_Valid = true;

            public WildcardIPFirewallEntry(string entry)
            {
                m_Entry = entry;
            }

            public bool IsBlocked(IPAddress address)
            {
                if (!m_Valid)
                    return false; //Why process if it's invalid?  it'll return false anyway after processing it.

                return Utility.IPMatch(m_Entry, address, ref m_Valid);
            }

            public override string ToString()
            {
                return m_Entry;
            }

            public override bool Equals(object obj)
            {
                if (obj is string)
                    return obj.Equals(m_Entry);
                if (obj is WildcardIPFirewallEntry)
                    return m_Entry.Equals(((WildcardIPFirewallEntry)obj).m_Entry);

                return false;
            }

            public override int GetHashCode()
            {
                return m_Entry.GetHashCode();
            }
        }

        #endregion
    }
}
