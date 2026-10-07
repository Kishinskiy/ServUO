#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

#endregion

namespace Server.Diagnostics
{
    public abstract class BasePacketProfile : BaseProfile
    {
        public long TotalLength { get; private set; }

        public double AverageLength => (double)TotalLength / Math.Max(1, Count);

        protected BasePacketProfile(string name)
            : base(name)
        {
        }

        public void Finish(int length)
        {
            Finish();

            TotalLength += length;
        }

        public override void WriteTo(TextWriter op)
        {
            base.WriteTo(op);

            op.Write("\t{0,12:F2} {1,-12:N0}", AverageLength, TotalLength);
        }
    }

    public class PacketSendProfile : BasePacketProfile
    {
        private static readonly Dictionary<Type, PacketSendProfile> _profiles =
            new Dictionary<Type, PacketSendProfile>();

        private long _created;

        public static IEnumerable<PacketSendProfile> Profiles => _profiles.Values;

        public PacketSendProfile(Type type)
            : base(type.FullName)
        {
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static PacketSendProfile Acquire(Type type)
        {
            PacketSendProfile prof;

            if (!_profiles.TryGetValue(type, out prof)) _profiles.Add(type, prof = new PacketSendProfile(type));

            return prof;
        }

        public void Increment()
        {
            Interlocked.Increment(ref _created);
        }

        public override void WriteTo(TextWriter op)
        {
            base.WriteTo(op);

            op.Write("\t{0,12:N0}", _created);
        }
    }

    public class PacketReceiveProfile : BasePacketProfile
    {
        private static readonly Dictionary<int, PacketReceiveProfile> _profiles =
            new Dictionary<int, PacketReceiveProfile>();

        public static IEnumerable<PacketReceiveProfile> Profiles => _profiles.Values;

        public PacketReceiveProfile(int packetId)
            : base(string.Format("0x{0:X2}", packetId))
        {
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static PacketReceiveProfile Acquire(int packetId)
        {
            PacketReceiveProfile prof;

            if (!_profiles.TryGetValue(packetId, out prof))
                _profiles.Add(packetId, prof = new PacketReceiveProfile(packetId));

            return prof;
        }
    }
}
