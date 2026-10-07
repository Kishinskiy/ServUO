using System;

namespace Server.Engines.NewMagincia
{
    public class NewMaginciaMessage
    {
        public static readonly TimeSpan DefaultExpirePeriod = TimeSpan.FromDays(7);

        public TextDefinition Title { get; }

        public TextDefinition Body { get; }

        public string Args { get; }

        public DateTime Expires { get; }

        public bool Expired => Expires < DateTime.UtcNow;

        public NewMaginciaMessage(TextDefinition title, TextDefinition body)
            : this(title, body, DefaultExpirePeriod, null)
        {
        }

        public NewMaginciaMessage(TextDefinition title, TextDefinition body, string args)
            : this(title, body, DefaultExpirePeriod, args)
        {
        }

        public NewMaginciaMessage(TextDefinition title, TextDefinition body, TimeSpan expires)
            : this(title, body, expires, null)
        {
        }

        public NewMaginciaMessage(TextDefinition title, TextDefinition body, TimeSpan expires, string args)
        {
            Title = title;
            Body = body;
            Args = args;
            Expires = DateTime.UtcNow + expires;
        }

        public NewMaginciaMessage(GenericReader reader)
        {
            var v = reader.ReadInt();

            Title = TextDefinition.Deserialize(reader);
            Body = TextDefinition.Deserialize(reader);
            Expires = reader.ReadDateTime();
            Args = reader.ReadString();
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write(0);

            TextDefinition.Serialize(writer, Title);
            TextDefinition.Serialize(writer, Body);
            writer.Write(Expires);
            writer.Write(Args);
        }
    }
}
