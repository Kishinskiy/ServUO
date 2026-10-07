#region References

using Server;
using Server.Gumps;

#endregion

namespace CustomsFramework
{
    public class BaseCore : SaveData, ICustomsEntity, ISerializable
    {
        private bool _Enabled;

        [CommandProperty(AccessLevel.Developer)]
        public bool Enabled
        {
            get => _Enabled;
            set
            {
                if (value != _Enabled)
                {
                    _Enabled = value;

                    if (OnEnabledChanged != null) OnEnabledChanged(new BaseCoreEventArgs(this));
                }
            }
        }

        public virtual string Description => @"Base Core, inherit from this class and override the interface items.";
        public virtual string Version => "1.0";

        public virtual AccessLevel EditLevel => AccessLevel.Developer;

        // TODO: Implement Custom Systems Control
        public virtual Gump SettingsGump => null;
        public virtual bool ShardControlEnabled => false;

        public BaseCore()
        {
        }

        public BaseCore(CustomSerial serial)
            : base(serial)
        {
        }

        public override string Name => @"Base Core";

        public override void Prep()
        {
        }

        public override void Delete()
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            writer.WriteVersion(0);

            // Version 0
            writer.Write(_Enabled);
        }

        public static event BaseCoreEventHandler OnEnabledChanged;

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
                    _Enabled = reader.ReadBool();
                    break;
                }
            }
        }
    }
}
