#region References

using System;
using Server;

#endregion

namespace CustomsFramework
{
    public struct CustomSerial : IComparable, IComparable<CustomSerial>
    {
        public static readonly CustomSerial MinusOne = new CustomSerial(-1);
        public static readonly CustomSerial Zero = new CustomSerial(0);

        private CustomSerial(int serial)
        {
            Value = serial;
        }

        public static CustomSerial LastCore { get; private set; } = Zero;

        public static CustomSerial NewCustom
        {
            get
            {
                while (World.GetData(LastCore = LastCore + 1) != null)
                {
                }

                return LastCore;
            }
        }

        public int Value { get; }

        public bool IsValid => Value > 0;

        public override int GetHashCode()
        {
            return Value;
        }

        public int CompareTo(CustomSerial other)
        {
            return Value.CompareTo(other.Value);
        }

        public int CompareTo(object other)
        {
            if (other is CustomSerial) return CompareTo((CustomSerial)other);

            if (other == null) return -1;

            throw new ArgumentException();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is CustomSerial)) return false;

            return ((CustomSerial)obj).Value == Value;
        }

        public override string ToString()
        {
            return string.Format("0x{0:X8}", Value);
        }

        public static bool operator ==(CustomSerial first, CustomSerial second)
        {
            return first.Value == second.Value;
        }

        public static bool operator !=(CustomSerial first, CustomSerial second)
        {
            return first.Value != second.Value;
        }

        public static bool operator >(CustomSerial first, CustomSerial second)
        {
            return first.Value > second.Value;
        }

        public static bool operator <(CustomSerial first, CustomSerial second)
        {
            return first.Value < second.Value;
        }

        public static bool operator >=(CustomSerial first, CustomSerial second)
        {
            return first.Value >= second.Value;
        }

        public static bool operator <=(CustomSerial first, CustomSerial second)
        {
            return first.Value <= second.Value;
        }

        public static implicit operator int(CustomSerial serial)
        {
            return serial.Value;
        }

        public static implicit operator CustomSerial(int serial)
        {
            return new CustomSerial(serial);
        }
    }
}
