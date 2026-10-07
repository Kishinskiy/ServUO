#region References

using System;
using CustomsFramework;

#endregion

namespace Server
{
    public class LastEditedBy
    {
        [CommandProperty(AccessLevel.Decorator)]
        public Mobile Mobile { get; set; }

        [CommandProperty(AccessLevel.Decorator)]
        public DateTime Time { get; set; }

        public LastEditedBy(Mobile mobile)
        {
            Mobile = mobile;
            Time = DateTime.UtcNow;
        }

        public LastEditedBy(GenericReader reader)
        {
            Deserialize(reader);
        }

        public void Serialize(GenericWriter writer)
        {
            writer.WriteVersion(0);

            // Version 0
            writer.Write(Mobile);
            writer.Write(Time);
        }

        private void Deserialize(GenericReader reader)
        {
            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                {
                    Mobile = reader.ReadMobile();
                    Time = reader.ReadDateTime();
                    break;
                }
            }
        }
    }
}
