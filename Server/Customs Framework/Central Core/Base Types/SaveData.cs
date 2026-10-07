#region References

using System;
using Server;

#endregion

namespace CustomsFramework
{
    public class SaveData : ICustomsEntity, IComparable<SaveData>, ISerializable
    {
        private CustomSerial _Serial;

        internal int _TypeID;

        [CommandProperty(AccessLevel.Developer)]
        public bool Deleted { get; set; }

        public SaveData(CustomSerial serial)
        {
            _Serial = serial;

            var dataType = GetType();
            _TypeID = World._DataTypes.IndexOf(dataType);

            if (_TypeID == -1)
            {
                World._DataTypes.Add(dataType);
                _TypeID = World._DataTypes.Count - 1;
            }
        }

        public SaveData()
        {
            _Serial = CustomSerial.NewCustom;

            World.AddData(this);

            var dataType = GetType();
            _TypeID = World._DataTypes.IndexOf(dataType);

            if (_TypeID == -1)
            {
                World._DataTypes.Add(dataType);
                _TypeID = World._DataTypes.Count - 1;
            }
        }

        [CommandProperty(AccessLevel.Developer)]
        public CustomSerial Serial
        {
            get => _Serial;
            set => _Serial = value;
        }

        public virtual string Name => @"Save Data";

        public virtual void Prep()
        {
        }

        public virtual void Delete()
        {
        }

        int ISerializable.TypeReference => _TypeID;

        int ISerializable.SerialIdentity => _Serial;

        public virtual void Serialize(GenericWriter writer)
        {
            writer.WriteVersion(0);

            // Version 0
            writer.Write(Deleted);
        }

        public virtual void Deserialize(GenericReader reader)
        {
            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                {
                    Deleted = reader.ReadBool();
                    break;
                }
            }
        }

        #region CompareTo

        public int CompareTo(ICustomsEntity other)
        {
            if (other == null) return -1;

            return _Serial.CompareTo(other.Serial);
        }

        public int CompareTo(SaveData other)
        {
            return CompareTo((ICustomsEntity)other);
        }

        public int CompareTo(object other)
        {
            if (other == null || other is ICustomsEntity) return CompareTo((ICustomsEntity)other);

            throw new ArgumentException();
        }

        #endregion
    }
}
