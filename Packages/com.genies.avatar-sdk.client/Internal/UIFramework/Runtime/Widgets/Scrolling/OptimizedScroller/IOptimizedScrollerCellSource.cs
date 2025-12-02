using UnityEngine;

namespace Genies.UI.Scroller
{
    /// <summary>
    /// Data source for providing cell view data.
    /// </summary>
    public interface IOptimizedScrollerCellSource
    {
        /// <summary>
        /// Request to get a new instance for a cell view
        /// </summary>
        /// <param name="index"> Index of the cell </param>
        public GameObject GetCellInstance(int index);

        /// <summary>
        /// Return the cell instance (recycle it.)
        /// </summary>
        /// <param name="index"> Index of the returned cell </param>
        /// <param name="cell"> The cell to return </param>
        public void ReturnCellInstance(int index, GameObject cell);

        /// <summary>
        /// Get the cell size, this is important as it is what is used to
        /// layout the content of the scroller
        /// </summary>
        /// <param name="index"> index of the cell </param>
        public Vector2 GetCellSize(int index);

        /// <summary>
        /// Used for the source to initialize the cell after its been created/placed.
        /// </summary>
        /// <param name="index"> Cell index </param>
        /// <param name="cell"> Cell view </param>
        public void InitializeCell(int index, GameObject cell);
    }
}
