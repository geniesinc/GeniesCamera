using System.Collections.Generic;
using RectpackSharp;
using UnityEngine;

namespace Genies.Avatars
{
    public sealed class AtlasLayoutBuilder
    {
        public Vector2Int AtlasSize    => _atlasSize;
        public int        RectCount    => _rects.Count;
        
        private readonly List<PackingRectangle> _rects = new();
        
        private Vector2Int _atlasSize;
        private Vector2Int _maxAtlasSize;
        private bool       _allowNpotAtlas;
        
        public Vector2Int GetPackedRectPosition(int index)
            => new((int)_rects[index].X, (int)_rects[index].Y);
        
        public Vector2Int GetPackedRectSize(int index)
            => new((int)_rects[index].Width, (int)_rects[index].Height);
        
        /// <summary>
        /// Starts an atlas layout build. Use the <see cref="AddRect"/> to add rectangles to build, then use
        /// <see cref="EndBuild"/> to finish and fetch the build data with <see cref="AtlasSize"/>,
        /// <see cref="GetPackedRectPosition"/> and <see cref="GetPackedRectSize"/>.
        /// </summary>
        /// <param name="maxAtlasWidth">Max width allowed for the final atlas size</param>
        /// <param name="maxAtlasHeight">Max height allowed for the final atlas size</param>
        /// <param name="allowNpotAtlas">Enable this to allow non-power-of-two atlas sizes. Keep in mind that if this
        /// is disabled, but you set a NPOT max atlas size, you can still get a NPOT result</param>
        public void BeginBuild(int maxAtlasWidth, int maxAtlasHeight, bool allowNpotAtlas = false)
        {
            _rects.Clear();
            _atlasSize = Vector2Int.zero;
            _maxAtlasSize = new Vector2Int(maxAtlasWidth, maxAtlasHeight);
            _allowNpotAtlas = allowNpotAtlas;
        }

        public void AddRect(int width, int height)
        {
            var rect = new PackingRectangle(0, 0, (uint)width, (uint)height, _rects.Count);
            _rects.Add(rect);
        }

        public void EndBuild()
        {
            // pack added rectangles
            PackingRectangle[] rectangles = _rects.ToArray();
            RectanglePacker.Pack(
                rectangles,
                out PackingRectangle bounds,
                packingHint: PackingHints.MostlySquared,
                stepSize: 32
            );
            
            // update main rects list with the results sorted by index
            for (int i = 0; i < rectangles.Length; ++i)
            {
                _rects[rectangles[i].Id] = rectangles[i];
            }

            // get the container bounds, which is the minimum bounds that encapsulate the packed rectangles
            int boundsWidth = (int)bounds.Width;
            int boundsHeight = (int)bounds.Height;
            
            if (_allowNpotAtlas)
            {
                FinishNpotAtlasBuild(boundsWidth, boundsHeight);
            }
            else
            {
                FinishPotAtlasBuild(boundsWidth, boundsHeight);
            }
        }

        // finishes a build with a power-of-two atlas size
        private void FinishPotAtlasBuild(int boundsWidth, int boundsHeight)
        {
            // calculate the minimum power-of-two atlas size to encapsulate the bounds
            _atlasSize.x = 2;
            _atlasSize.y = 2;
            
            while (_atlasSize.x < boundsWidth)
            {
                _atlasSize.x *= 2;
            }

            while (_atlasSize.y < boundsHeight)
            {
                _atlasSize.y *= 2;
            }

            // if the calculated PoT atlas size fits into the max size, then we are done
            if (_atlasSize.x <= _maxAtlasSize.x && _atlasSize.y <= _maxAtlasSize.y)
            {
                return;
            }

            // update max atlas size for the bounds fitting method, so we preserve any PoT dimension that still fits the max size
            if (_atlasSize.x < _maxAtlasSize.x)
            {
                _maxAtlasSize.x = _atlasSize.x;
            }

            if (_atlasSize.y < _maxAtlasSize.y)
            {
                _maxAtlasSize.y = _atlasSize.y;
            }

            // if the calculated PoT atlas size does not fit the max atlas size, then just fit the bounds to it
            FitBoundsToMaxAtlasSize(boundsWidth, boundsHeight);
        }
        
        // finishes a build with a non-power-of-two atlas size
        private void FinishNpotAtlasBuild(int boundsWidth, int boundsHeight)
        {
            if (_maxAtlasSize.x > boundsWidth)
            {
                _maxAtlasSize.x = boundsWidth;
            }

            if (_maxAtlasSize.y > boundsHeight)
            {
                _maxAtlasSize.y = boundsHeight;
            }

            FitBoundsToMaxAtlasSize(boundsWidth, boundsHeight);
        }

        private void FitBoundsToMaxAtlasSize(int boundsWidth, int boundsHeight)
        {
            _atlasSize = _maxAtlasSize;
            
            // we only do downscaling to fit a dimension that is out of bounds
            bool needsFitX = boundsWidth > _maxAtlasSize.x;
            bool needsFitY = boundsHeight > _maxAtlasSize.y;
            
            if (!needsFitX && !needsFitY)
            {
                return;
            }

            float scaleX = (float)_atlasSize.x / boundsWidth;
            float scaleY = (float)_atlasSize.y / boundsHeight;

            // apply scale to packed rects
            for (int i = 0; i < _rects.Count; ++i)
            {
                PackingRectangle rectangle = _rects[i];

                if (needsFitX)
                {
                    rectangle.X = (uint)(rectangle.X * scaleX);
                    rectangle.Width = (uint)(rectangle.Width * scaleX);
                }

                if (needsFitY)
                {
                    rectangle.Y = (uint)(rectangle.Y * scaleY);
                    rectangle.Height = (uint)(rectangle.Height * scaleY);
                }
                
                _rects[i] = rectangle;
            }
        }
    }
}