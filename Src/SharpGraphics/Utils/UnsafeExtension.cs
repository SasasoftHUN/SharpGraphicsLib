using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGraphics.Utils
{
    public static unsafe class UnsafeExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr AsIntPtr<T>(ref T obj) => new IntPtr(Unsafe.AsPointer(ref obj));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr AsIntPtr<T>(in Span<T> obj) => new IntPtr(Unsafe.AsPointer(ref obj[0]));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr AsIntPtr<T>(in ReadOnlySpan<T> obj) => new IntPtr(Unsafe.AsPointer(ref Unsafe.AsRef(in obj[0])));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string? ParseByteString(byte* str) => Marshal.PtrToStringAnsi(new IntPtr(str));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<T>(this Span<T> data, IntPtr pointer) => data.CopyTo(new Span<T>(pointer.ToPointer(), data.Length));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<T>(this ReadOnlySpan<T> data, IntPtr pointer) => data.CopyTo(new Span<T>(pointer.ToPointer(), data.Length));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyWithAlignment<T>(this IntPtr destination, T[] data, int elementOffset) where T : unmanaged
            => CopyWithAlignment(destination, new ReadOnlySpan<T>(data), elementOffset, Marshal.SizeOf<T>());
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyWithAlignment<T>(this IntPtr destination, T[] data, int elementOffset, int elementSize) where T : unmanaged
            => CopyWithAlignment(destination, new ReadOnlySpan<T>(data), elementOffset, elementSize);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyWithAlignment<T>(this IntPtr destination, in Span<T> data, int elementOffset) where T : unmanaged
            => CopyWithAlignment(destination, data, elementOffset, Marshal.SizeOf<T>());
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyWithAlignment<T>(this IntPtr destination, in Span<T> data, int elementOffset, int elementSize) where T : unmanaged
        {
            fixed (T* ptr = data)
            {
                byte* sourcePtr = (byte*)ptr;
                byte* destinationPtr = (byte*)destination.ToPointer();

                for (int i = 0; i < data.Length; i++)
                {
                    Buffer.MemoryCopy(sourcePtr, destinationPtr, elementSize, elementSize);
                    sourcePtr += elementSize;
                    destinationPtr += elementOffset;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyWithAlignment<T>(this IntPtr destination, in ReadOnlySpan<T> data, int elementOffset) where T : unmanaged
            => CopyWithAlignment(destination, data, elementOffset, Marshal.SizeOf<T>());
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyWithAlignment<T>(this IntPtr destination, in ReadOnlySpan<T> data, int elementOffset, int elementSize) where T : unmanaged
        {
            fixed (T* ptr = data)
            {
                byte* sourcePtr = (byte*)ptr;
                byte* destinationPtr = (byte*)destination.ToPointer();

                for (int i = 0; i < data.Length; i++)
                {
                    Buffer.MemoryCopy(sourcePtr, destinationPtr, elementSize, elementSize);
                    sourcePtr += elementSize;
                    destinationPtr += elementOffset;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyWithAlignment<T>(this IntPtr destination, in Memory<T> data, int elementOffset) where T : unmanaged
            => CopyWithAlignment(destination, data, elementOffset, Marshal.SizeOf<T>());
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyWithAlignment<T>(this IntPtr destination, in Memory<T> data, int elementOffset, int elementSize) where T : unmanaged
        {
            using PinnedObjectReference<T> pinnedData = new PinnedObjectReference<T>(data);
            byte* sourcePtr = (byte*)pinnedData.RawPointer;
            byte* destinationPtr = (byte*)destination.ToPointer();

            for (int i = 0; i < data.Length; i++)
            {
                Buffer.MemoryCopy(sourcePtr, destinationPtr, elementSize, elementSize);
                sourcePtr += elementSize;
                destinationPtr += elementOffset;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyWithAlignment<T>(this IntPtr destination, in ReadOnlyMemory<T> data, int elementOffset) where T : unmanaged
            => CopyWithAlignment(destination, data, elementOffset, Marshal.SizeOf<T>());
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyWithAlignment<T>(this IntPtr destination, in ReadOnlyMemory<T> data, int elementOffset, int elementSize) where T : unmanaged
        {
            using PinnedObjectReference<T> pinnedData = new PinnedObjectReference<T>(data);
            byte* sourcePtr = (byte*)pinnedData.RawPointer;
            byte* destinationPtr = (byte*)destination.ToPointer();

            for (int i = 0; i < data.Length; i++)
            {
                Buffer.MemoryCopy(sourcePtr, destinationPtr, elementSize, elementSize);
                sourcePtr += elementSize;
                destinationPtr += elementOffset;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] GatherAlignedElements<T>(this IntPtr data, int elementCount, int elementOffset) where T : unmanaged
            => data.GatherAlignedElements<T>(elementCount, elementOffset, Marshal.SizeOf<T>());
        public static T[] GatherAlignedElements<T>(this IntPtr data, int elementCount, int elementOffset, int elementSize) where T : unmanaged
        {
            if (elementOffset == elementSize)
                return new ReadOnlySpan<T>(data.ToPointer(), elementCount).ToArray();
            else
            {
                T[] result = new T[elementCount];
                for (int i = 0; i < elementCount; i++)
                {
                    result[i] = Marshal.PtrToStructure<T>(data);
                    data += elementOffset;
                }
                return result;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GatherAlignedElements<T>(this IntPtr data, int elementCount, int elementOffset, Span<T> output) where T : unmanaged
            => data.GatherAlignedElements(elementCount, elementOffset, Marshal.SizeOf<T>(), output);
        public static void GatherAlignedElements<T>(this IntPtr data, int elementCount, int elementOffset, int elementSize, Span<T> output) where T : unmanaged
        {
            if (elementOffset == elementSize)
                new ReadOnlySpan<T>(data.ToPointer(), elementCount).CopyTo(output);
            else
            {
                for (int i = 0; i < elementCount; i++)
                {
                    output[i] = Marshal.PtrToStructure<T>(data);
                    data += elementOffset;
                }
            }
        }

    }
}