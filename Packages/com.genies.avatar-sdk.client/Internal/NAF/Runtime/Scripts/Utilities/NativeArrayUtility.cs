using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Genies.Naf
{
    public static class NativeArrayUtility
    {
        public static NativeArray<T> PtrToNativeArray<T>(IntPtr pointer, int size)
            where T : struct
        {
            if (pointer == IntPtr.Zero || size == 0)
            {
                return new NativeArray<T>(0, Allocator.Persistent);
            }

            NativeArray<T> array;
            unsafe
            {
                array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>((void*)pointer, size, Allocator.None);
            }
        
            // set the safety handle (required for safety checks in the editor)
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, AtomicSafetyHandle.Create());
#endif
        
            return array;
        }
    }
}