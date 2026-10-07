#region References

using System;
using Server;
using Server.Gumps;

#endregion

namespace CustomsFramework
{
    public class BaseModule : SaveData, ICustomsEntity, ISerializable
    {
        private Item _LinkedItem;
        private Mobile _LinkedMobile;

        public virtual string Description => "Base Module, inherit from this class and override all interface items.";

        public virtual string Version => "1.0";

        public virtual AccessLevel EditLevel => AccessLevel.Developer;

        public virtual Gump SettingsGump => null;

        [CommandProperty(AccessLevel.Administrator)]
        public Mobile LinkedMobile
        {
            get => _LinkedMobile;
            set => LinkMobile(value);
        }

        [CommandProperty(AccessLevel.Administrator)]
        public Item LinkedItem
        {
            get => _LinkedItem;
            set => LinkItem(value);
        }

        [CommandProperty(AccessLevel.Administrator)]
        public DateTime CreatedTime { get; private set; }

        [CommandProperty(AccessLevel.Administrator)]
        public DateTime LastEditedTime { get; private set; }

        public BaseModule()
        {
        }

        public BaseModule(Mobile from)
        {
            LinkMobile(from);
        }

        public BaseModule(Item item)
        {
            LinkItem(item);
        }

        public BaseModule(CustomSerial serial)
            : base(serial)
        {
        }

        public override string Name => @"Base Module";

        public override void Prep()
        {
        }

        public override void Delete()
        {
            if (_LinkedMobile != null)
            {
                _LinkedMobile.Modules.Remove(this);
                _LinkedMobile = null;
            }

            if (_LinkedItem != null)
            {
                _LinkedItem.Modules.Remove(this);
                _LinkedItem = null;
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            writer.WriteVersion(0);

            // Version 0
            writer.Write(_LinkedMobile);
            writer.Write(_LinkedItem);
            writer.Write(CreatedTime);
            writer.Write(LastEditedTime);
        }

        public override string ToString()
        {
            return Name;
        }

        public void Update()
        {
            LastEditedTime = DateTime.UtcNow;
        }

        public bool LinkMobile(Mobile from)
        {
            if (_LinkedMobile != null || _LinkedMobile == from) return false;

            if (!from.Modules.Contains(this)) from.Modules.Add(this);

            _LinkedMobile = from;
            Update();
            return true;
        }

        public bool LinkItem(Item item)
        {
            if (_LinkedItem != null || _LinkedItem == item) return false;

            if (!item.Modules.Contains(this)) item.Modules.Add(this);

            _LinkedItem = item;
            Update();
            return true;
        }

        public bool UnlinkMobile()
        {
            if (_LinkedMobile == null) return false;

            if (_LinkedMobile.Modules.Contains(this)) _LinkedMobile.Modules.Remove(this);

            _LinkedMobile = null;
            Update();
            return true;
        }

        public bool UnlinkItem()
        {
            if (_LinkedItem == null) return false;

            if (_LinkedItem.Modules.Contains(this)) _LinkedItem.Modules.Remove(this);

            _LinkedItem = null;
            Update();
            return true;
        }

        public override void Deserialize(GenericReader reader)
        {
            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                {
                    LinkedMobile = reader.ReadMobile();
                    LinkedItem = reader.ReadItem();
                    CreatedTime = reader.ReadDateTime();
                    LastEditedTime = reader.ReadDateTime();
                    break;
                }
            }
        }
    }
}
