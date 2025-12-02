namespace Genies.UI.Widgets
{
    /// <summary>
    /// Interface for UI widgets that can be selected and deselected.
    /// </summary>
    public interface ISelectable
    {
        /// <summary>
        /// Gets or sets a value indicating whether this widget is currently selected.
        /// </summary>
        bool IsSelected { get; set; }
    }
}