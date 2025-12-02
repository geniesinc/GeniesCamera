using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Genies.Utilities
{
    public static class ArrayExtensions
    {
        /// <summary>
        /// Creates a NativeArray from a managed array. The managed array will be pinned in memory until the returned
        /// handle is disposed. You must also dispose the returned native array.
        /// </summary>
        public static unsafe NativeArray<T> AsNativeArray<T>(this T[] array, out GCHandle handle)
            where T : struct
        {
            handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            return CreateNativeArray<T>((void*)handle.AddrOfPinnedObject(), array.Length);
        }
        
        /// <summary>
        /// Creates a NativeArray from a given pointer and length.
        /// </summary>
        public static unsafe NativeArray<T> AsNativeArray<T>(this IntPtr pointer, int length)
            where T : struct
        {
            return CreateNativeArray<T>((void*) pointer, length);
        }
        
        /// <summary>
        /// Creates a NativeArray from a given unsafe pointer and length.
        /// </summary>
        public static unsafe NativeArray<T> CreateNativeArray<T>(void* pointer, int length)
            where T : struct
        {
            NativeArray<T> nativeArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(
                pointer,
                length,
                Allocator.None
            );

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref nativeArray, AtomicSafetyHandle.Create());
#endif

            return nativeArray;
        }
    }
}
