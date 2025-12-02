using Cysharp.Threading.Tasks;

namespace Genies.Looks.Customization.UI
{
    public interface IPatternPickerController
    {
        /// <summary>
        /// Invoked when the user wants to edit a pattern.
        /// </summary>
        void OnPatternEditRequested(string patternId);

        /// <summary>
        /// Call when pattern creation is requested
        /// </summary>
        void OnCreatePatternRequested();

        /// <summary>
        /// Called when a pattern ID has been selected. Must return true if the selection
        /// operation was performed successfully (i.e.: the pattern was applied to the region).
        /// </summary>
        UniTask<bool> OnPatternSelectedAsync(string patternId);

        bool HasPatternEditor { get; }

    }
}
