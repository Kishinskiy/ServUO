#region References

using Server;
using Server.Gumps;

#endregion

namespace CustomsFramework
{
    public class BaseService : SaveData, ICustomsEntity, ISerializable
    {
        public virtual string Description => @"Base Service, inherit from this class and override the interface items.";
        public virtual string Version => "1.0";
        public virtual AccessLevel EditLevel => AccessLevel.Developer;
        public virtual Gump SettingsGump => null;

        public BaseService()
        {
        }

        public BaseService(CustomSerial serial)
            : base(serial)
        {
        }

        public override string Name => @"Base Service";

        public override void Prep()
        {
        }

        public override void Delete()
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            writer.WriteVersion(0);
            //Version 0
        }

        public override string ToString()
        {
            return Name;
        }

        public override void Deserialize(GenericReader reader)
        {
            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                {
                    break;
                }
            }
        }
    }
}
