using GnWrappers;
using Unity.Collections;

namespace Genies.Naf
{
    public static class DynamicAccessorExtensions
    {
        public static unsafe string GetDataAsUTF8String(this DynamicAccessor accessor)
        {
            return System.Text.Encoding.UTF8.GetString((byte*)accessor.Data(), (int)accessor.Size());
        }

        public static void CopyTo<T>(this DynamicAccessor accessor, NativeArray<T> destination)
            where T : struct
        {
            using NativeArray<T> source = AsNativeArray<T>(accessor);
            NativeArray<T>.Copy(source, destination);
        }
        
        public static void CopyTo<T>(this DynamicAccessor accessor, T[] destination)
            where T : struct
        {
            using NativeArray<T> source = AsNativeArray<T>(accessor);
            NativeArray<T>.Copy(source, destination);
        }

        public static NativeArray<T> AsNativeArray<T>(this DynamicAccessor accessor)
            where T : struct
        {
            return NativeArrayUtility.PtrToNativeArray<T>(accessor.Data(), (int)accessor.Size());
        }
    }
}