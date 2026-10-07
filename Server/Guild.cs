#region References

using System.Collections.Generic;

#endregion

namespace Server.Guilds
{
    public enum GuildType
    {
        Regular,
        Chaos,
        Order
    }

    public abstract class BaseGuild : ISerializable
    {
        private static int m_NextID = 1;

        [CommandProperty(AccessLevel.Counselor)]
        public int Id { get; }

        public abstract string Abbreviation { get; set; }
        public abstract string Name { get; set; }
        public abstract GuildType Type { get; set; }
        public abstract bool Disbanded { get; }

        public static Dictionary<int, BaseGuild> List { get; } = new Dictionary<int, BaseGuild>();

        protected BaseGuild(int Id) //serialization ctor
        {
            this.Id = Id;
            List.Add(this.Id, this);
            if (this.Id + 1 > m_NextID) m_NextID = this.Id + 1;
        }

        protected BaseGuild()
        {
            Id = m_NextID++;
            List.Add(Id, this);
        }

        int ISerializable.TypeReference => 0;

        int ISerializable.SerialIdentity => Id;
        public abstract void Serialize(GenericWriter writer);

        public abstract void Deserialize(GenericReader reader);
        public abstract void OnDelete(Mobile mob);

        public static BaseGuild Find(int id)
        {
            BaseGuild g;

            List.TryGetValue(id, out g);

            return g;
        }

        public static BaseGuild FindByName(string name)
        {
            foreach (var g in List.Values)
                if (g.Name == name)
                    return g;

            return null;
        }

        public static BaseGuild FindByAbbrev(string abbr)
        {
            foreach (var g in List.Values)
                if (g.Abbreviation == abbr)
                    return g;

            return null;
        }

        public static List<BaseGuild> Search(string find)
        {
            var words = find.ToLower().Split(' ');
            var results = new List<BaseGuild>();

            foreach (var g in List.Values)
            {
                var match = true;
                var name = g.Name.ToLower();
                for (var i = 0; i < words.Length; i++)
                    if (name.IndexOf(words[i]) == -1)
                    {
                        match = false;
                        break;
                    }

                if (match) results.Add(g);
            }

            return results;
        }

        public override string ToString()
        {
            return string.Format("0x{0:X} \"{1} [{2}]\"", Id, Name, Abbreviation);
        }
    }
}
