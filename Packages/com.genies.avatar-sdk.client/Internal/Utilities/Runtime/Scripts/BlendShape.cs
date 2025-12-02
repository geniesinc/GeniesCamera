using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace Genies.Utilities
{
    public readonly struct BlendShape
    {
        public int FrameCount => Frames.Length;
        
        public readonly string            Name;
        public readonly BlendShapeFrame[] Frames;
        
        public BlendShape(string name, int frameCount)
            : this(name, new BlendShapeFrame[frameCount]) { }
        
        public BlendShape(string name, IEnumerable<BlendShapeFrame> frames)
            : this(name, frames.ToArray()) { }
        
        public BlendShape(string name, params BlendShapeFrame[] frames)
        {
            Name = name;
            Frames = frames;
        }
        
        /// <summary>
        /// Creates a new blend shape instance that reuses the frame buffer from another blend shape.
        /// </summary>
        public BlendShape(string name, BlendShape source)
        {
            Name = name;
            Frames = source.Frames;
        }
        
        public async UniTask BakeAsync(MeshBakeData data, float weight)
        {
            if (weight == 0.0f)
            {
                return;
            }

            await OperationQueue.EnqueueAsync(OperationCost.Low);
            Bake(data, weight);
        }
        
        /// <summary>
        /// Bakes this blend shape into the given data for the given weight value.
        /// </summary>
        public void Bake(MeshBakeData data, float weight)
        {
            /**
             * This skip if weight is zero is technically not correct since there can be negative weights, but this is
             * how Unity works for this case so we want to mimic its exact implementation.
             */
            if (weight == 0.0f)
            {
                return;
            }

            float interpolation;
            float currentWeightRange = 0.0f;
            float previousFrameWeight = 0.0f;
            
            // we are assuming that frames are properly ordered from lower to higher weights, and that no frames share the same weight
            for (int i = 0; i < Frames.Length; ++i)
            {
                BlendShapeFrame frame = Frames[i];
                currentWeightRange = frame.Weight - previousFrameWeight;
                
                if (weight > frame.Weight)
                {
                    previousFrameWeight = frame.Weight;
                    continue;
                }
                
                interpolation = (weight - previousFrameWeight) / currentWeightRange;

                if (i > 0)
                {
                    Frames[i - 1].Bake(data, frame, interpolation);
                }
                else
                {
                    frame.Bake(data, interpolation);
                }

                return;
            }
            
            // edge case when the given weight is above the last frame weight. This code mimics how Unity works for this cases
            
            if (Frames.Length == 0)
            {
                return;
            }

            interpolation = 1.0f + (weight - previousFrameWeight) / currentWeightRange;
            Frames[^1].Bake(data, interpolation);
        }

        /// <summary>
        /// Adds the deltas from the other blend shape to this one. The other blend shape must have the same number of
        /// frames with all of them having the same weight values, properties and vertex count.
        /// </summary>
        public void MergeWith(BlendShape other)
        {
            if (FrameCount != other.FrameCount)
            {
                throw new Exception("Cannot merge blend shapes because they have a different frame count");
            }

            for (int i = 0; i < Frames.Length; ++i)
            {
                BlendShapeFrame frame = Frames[i];
                BlendShapeFrame otherFrame = other.Frames[i];

                if (frame.Weight != otherFrame.Weight)
                {
                    throw new Exception(
                        "Cannot merge blend shapes because at least one of their frames have different weights");
                }

                if (frame.VertexCount != otherFrame.VertexCount)
                {
                    throw new Exception(
                        "Cannot merge blend shapes because at least one of their frames have a different vertex count");
                }

                if (frame.Properties != otherFrame.Properties)
                {
                    throw new Exception(
                        "Cannot merge blend shapes because at least one of their frames have different properties");
                }
            }

            // merge frames
            for (int i = 0; i < Frames.Length; ++i)
            {
                Frames[i].MergeWith(other.Frames[i]);
            }
        }
    }
}
