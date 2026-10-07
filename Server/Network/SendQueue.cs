#region References

using System;
using System.Collections.Concurrent;
using System.Threading;

#endregion

namespace Server.Network
{
    public class SendQueue
    {
        private const int PendingCap = 0x200000;

        private static int m_CoalesceBufferSize = 512;
        private static BufferPool m_UnusedBuffers = new BufferPool("Coalesced", 2048, m_CoalesceBufferSize);

        private readonly ConcurrentQueue<Gram> _pending = new ConcurrentQueue<Gram>();

        private volatile Gram _buffered;

        public static int CoalesceBufferSize
        {
            get => m_CoalesceBufferSize;
            set
            {
                if (m_CoalesceBufferSize == value) return;

                m_UnusedBuffers?.Free();

                m_CoalesceBufferSize = value;
                m_UnusedBuffers = new BufferPool("Coalesced", 2048, m_CoalesceBufferSize);
            }
        }

        public bool IsFlushReady => _pending.Count == 0 && _buffered != null;

        public bool IsEmpty => _pending.Count == 0 && _buffered == null;

        public static byte[] AcquireBuffer()
        {
            return m_UnusedBuffers.AcquireBuffer();
        }

        public static void ReleaseBuffer(byte[] buffer)
        {
            if (buffer != null && buffer.Length == m_CoalesceBufferSize) m_UnusedBuffers.ReleaseBuffer(buffer);
        }

        public Gram CheckFlushReady()
        {
            var gram = Interlocked.Exchange(ref _buffered, null);

            _pending.Enqueue(gram);

            return gram;
        }

        public Gram Dequeue()
        {
            if (_pending.TryDequeue(out var gram))
            {
                gram?.Release();
                gram = null;

                if (_pending.TryPeek(out var peek))
                    gram = peek;
            }

            return gram;
        }

        public Gram Enqueue(byte[] buffer, int length)
        {
            return Enqueue(buffer, 0, length);
        }

        public Gram Enqueue(byte[] buffer, int offset, int length)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");

            if (!(offset >= 0 && offset < buffer.Length))
                throw new ArgumentOutOfRangeException(
                    "offset", offset,
                    "Offset must be greater than or equal to zero and less than the size of the buffer.");

            if (length < 0 || length > buffer.Length)
                throw new ArgumentOutOfRangeException(
                    "length", length, "Length cannot be less than zero or greater than the size of the buffer.");

            if (buffer.Length - offset < length)
                throw new ArgumentException("Offset and length do not point to a valid segment within the buffer.");

            var existingBytes = _pending.Count * m_CoalesceBufferSize + (_buffered?.Length ?? 0);

            if (existingBytes + length > PendingCap) throw new CapacityExceededException();

            Gram gram = null;

            while (length > 0)
            {
                if (_buffered == null)
                    // nothing yet buffered
                    _buffered = Gram.Acquire();

                var bytesWritten = _buffered.Write(buffer, offset, length);

                offset += bytesWritten;
                length -= bytesWritten;

                if (_buffered.IsFull)
                {
                    if (_pending.Count == 0) gram = _buffered;

                    _pending.Enqueue(_buffered);
                    _buffered = null;
                }
            }

            return gram;
        }

        public void Clear()
        {
            if (_buffered != null)
            {
                _buffered.Release();
                _buffered = null;
            }

            while (_pending.TryDequeue(out var gram)) gram?.Release();
        }

        public class Gram
        {
            private static readonly ConcurrentQueue<Gram> _pool = new ConcurrentQueue<Gram>();

            public byte[] Buffer { get; private set; }

            public int Length { get; private set; }

            public int Available => Buffer.Length - Length;

            public bool IsFull => Length == Buffer.Length;

            private Gram()
            {
            }

            public static Gram Acquire()
            {
                if (!_pool.TryDequeue(out var gram)) gram = new Gram();

                gram.Buffer = AcquireBuffer();
                gram.Length = 0;

                return gram;
            }

            public int Write(byte[] buffer, int offset, int length)
            {
                var write = Math.Min(length, Available);

                System.Buffer.BlockCopy(buffer, offset, Buffer, Length, write);

                Length += write;

                return write;
            }

            public void Release()
            {
                _pool.Enqueue(this);

                ReleaseBuffer(Buffer);
            }
        }
    }

    [Serializable]
    public sealed class CapacityExceededException : Exception
    {
        public CapacityExceededException()
            : base("Too much data pending.")
        {
        }
    }
}
