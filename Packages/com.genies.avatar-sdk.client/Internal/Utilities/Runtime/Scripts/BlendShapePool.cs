using System.Collections.Generic;

namespace Genies.Utilities
{
    /// <summary>
    /// Utility pool for blend shapes with varying frame counts but same vertex count and properties.
    /// </summary>
    public sealed class BlendShapePool
    {
        public readonly int VertexCount;
        public readonly BlendShapeProperties Properties;
        
        private Stack<BlendShape>[] _poolsByFrameCount;

        public BlendShapePool(int vertexCount, BlendShapeProperties properties = BlendShapeProperties.All)
        {
            VertexCount = vertexCount;
            Properties = properties;
            _poolsByFrameCount = new Stack<BlendShape>[256];
        }
        
        public BlendShape Get(int frameCount)
        {
            return Get(string.Empty, frameCount);
        }

        public BlendShape Get(string name, int frameCount)
        {
            Stack<BlendShape> pool = GetPool(frameCount);
            
            if (pool.Count > 0)
            {
                return new BlendShape(name, pool.Pop());
            }
            else
            {
                return New(name, frameCount);
            }
        }

        public void Release(BlendShape blendShape)
        {
            if (IsBlendShapeInvalid(blendShape))
            {
                return;
            }

            Stack<BlendShape> pool = GetPool(blendShape.FrameCount);
            pool.Push(blendShape);
        }

        public int GetCount()
        {
            int count = 0;
            foreach (Stack<BlendShape> pool in _poolsByFrameCount)
            {
                if (pool is not null)
                {
                    count += pool.Count;
                }
            }
            
            return count;
        }

        private BlendShape New(string name, int frameCount)
        {
            var frames = new BlendShapeFrame[frameCount];
            for (int i = 0; i < frameCount; ++i)
            {
                frames[i] = new BlendShapeFrame(0.0f, VertexCount, Properties);
            }

            return new BlendShape(name, frames);
        }
        
        private Stack<BlendShape> GetPool(int frameCount)
        {
            EnsureFrameCount(frameCount);
            return _poolsByFrameCount[frameCount] ??= new Stack<BlendShape>();
        }

        private bool IsBlendShapeInvalid(BlendShape blendShape)
        {
            foreach (BlendShapeFrame frame in blendShape.Frames)
            {
                if (frame.VertexCount != VertexCount || frame.Properties != Properties)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        private void EnsureFrameCount(int frameCount)
        {
            if (frameCount < _poolsByFrameCount.Length)
            {
                return;
            }

            var newPools = new Stack<BlendShape>[frameCount + 64];
            for (int i = 0; i < _poolsByFrameCount.Length; ++i)
            {
                newPools[i] = _poolsByFrameCount[i];
            }

            _poolsByFrameCount = newPools;
        }
    }
}
