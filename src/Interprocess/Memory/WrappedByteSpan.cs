using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Cloudtoid.Interprocess
{
    public readonly ref struct WrappedByteSpan
    {
        private readonly Span<byte> first;
        private readonly Span<byte> second;

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WrappedByteSpan(Span<byte> first, Span<byte> second = default)
        {
            this.first = first;
            this.second = second;
        }

        /// <summary>
        /// The number of bytes in the span.
        /// </summary>
        public int Length
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => first.Length + second.Length;
        }

        /// <summary>
        /// Returns true if Length is 0.
        /// </summary>
        public bool IsEmpty
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Length <= 0;
        }

        public Span<byte> FirstPart
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => first;
        }

        public Span<byte> SecondPart
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => second;
        }

        public ref byte this[int offset]
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var firstLength = first.Length;
                if (offset < firstLength)
                    return ref first[offset];
                return ref second[offset - firstLength];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WrappedByteSpan Slice(int offset)
        {
            var firstLength = first.Length;
            if (offset < firstLength)
                return new(first.Slice(offset), second);
            var totalLength = firstLength + second.Length;
            if (offset == totalLength) return default;
            if (offset < totalLength)
                return new(second.Slice(offset - firstLength));
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private WrappedByteSpan SetLength(int length)
        {
            if (length == 0)
                return default;
            var firstLength = first.Length;
            if (length <= firstLength)
                return new(first.Slice(0, length));
            var totalLength = firstLength + second.Length;
            if (length <= totalLength)
                return new(first, second.Slice(length - firstLength));
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public WrappedByteSpan Slice(int offset, int length)
            => length == 0
                ? default
                : offset == 0
                    ? SetLength(length)
                    : Slice(offset).SetLength(length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReadOnlySpan<T> CreateReadOnlySpan<T>(in T item)
#if NETSTANDARD2_0
        {
            unsafe
            {
                return new(Unsafe.AsPointer(ref Unsafe.AsRef(item)), 1);
            }
        }
#else
            => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(item), 1);
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Span<T> CreateSpan<T>(in T item)
#if NETSTANDARD2_0
        {
            unsafe
            {
                return new(Unsafe.AsPointer(ref Unsafe.AsRef(item)), 1);
            }
        }
#else
            => MemoryMarshal.CreateSpan(ref Unsafe.AsRef(item), 1);
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReadOnlySpan<byte> AsBytes<T>(ReadOnlySpan<T> span)
            where T : struct
        {
            unsafe
            {
                return new(Unsafe.AsPointer(ref Unsafe.AsRef(span[0])),
                    checked(span.Length * Unsafe.SizeOf<T>()));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Span<byte> AsBytes<T>(Span<T> span)
            where T : struct
        {
            unsafe
            {
                return new(Unsafe.AsPointer(ref Unsafe.AsRef(span[0])),
                    checked(span.Length * Unsafe.SizeOf<T>()));
            }
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite<T>(in T item) where T : struct
            => TryWrite(CreateReadOnlySpan(item));

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite<T>(ReadOnlySpan<T> items) where T : struct
            => TryWrite(AsBytes(items));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWrite(ReadOnlySpan<byte> buffer)
        {
            var firstLength = first.Length;
            var longerThanFirst = buffer.Length > firstLength;
            if (longerThanFirst)
                if (buffer.Length > firstLength + second.Length)
                    return false;

            buffer.CopyTo(first);
            if (longerThanFirst)
                buffer.Slice(firstLength).CopyTo(second);

            return true;
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(in T item) where T : struct
            => Write(CreateReadOnlySpan(item));

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(ReadOnlySpan<T> items) where T : struct
            => Write(MemoryMarshal.AsBytes(items));

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ReadOnlySpan<byte> bytes)
        {
            if (!TryWrite(bytes))
                throw new ArgumentException("Too long to fit in buffer.", nameof(bytes));
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead<T>(out T item) where T : struct
        {
#if NET5_0_OR_GREATER || NETSTANDARD
            Unsafe.SkipInit(out item);
#else
            item = default;
#endif
            return TryRead(CreateSpan(item));
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead<T>(Span<T> items) where T : struct
            => TryRead(AsBytes(items));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(Span<byte> bytes)
        {
            var firstLength = first.Length;
            var longerThanFirst = bytes.Length > firstLength;
            if (longerThanFirst)
                if (bytes.Length > firstLength + second.Length)
                    return false;

            first.CopyTo(bytes);
            if (longerThanFirst)
                second.CopyTo(bytes.Slice(firstLength));

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Read<T>() where T : struct
            => TryRead(out T item) ? item : throw new ArgumentException("Too long to be in buffer.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Read<T>(Span<T> items) where T : struct
        {
            if (!TryRead(items)) throw new ArgumentException("Too long to be in buffer.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Read(Span<byte> bytes)
        {
            if (!TryRead(bytes)) throw new ArgumentException("Too long to be in buffer.");
        }

        public byte[] ToArray()
        {
            var bytes = new byte[Length];
            Read(bytes);
            return bytes;
        }
    }
}
