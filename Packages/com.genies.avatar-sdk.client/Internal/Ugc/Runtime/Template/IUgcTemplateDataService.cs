using Cysharp.Threading.Tasks;

namespace Genies.Ugc
{
    /// <summary>
    /// Defines the contract for fetching UGC template data used in the creation and configuration of user-generated content.
    /// This interface provides methods for retrieving template information, split configurations, and element data
    /// that define how UGC items can be created and styled.
    /// </summary>
    public interface IUgcTemplateDataService
    {
        /// <summary>
        /// Fetches the complete template data for the specified template ID.
        /// Template data contains the overall configuration and metadata for a UGC template.
        /// </summary>
        /// <param name="templateId">The unique identifier of the template to fetch.</param>
        /// <returns>A task that completes with the template data for the specified template.</returns>
        UniTask<UgcTemplateData> FetchTemplateDataAsync(string templateId);
        
        /// <summary>
        /// Fetches split data for a specific split within a template.
        /// Split data defines how different regions and elements can be styled within the template.
        /// </summary>
        /// <param name="splitIndex">The zero-based index of the split within the template.</param>
        /// <param name="templateId">The unique identifier of the template containing the split.</param>
        /// <returns>A task that completes with the split data for the specified split and template.</returns>
        UniTask<UgcTemplateSplitData> FetchSplitDataAsync(int splitIndex, string templateId);
        
        /// <summary>
        /// Fetches element data for a specific element by its ID.
        /// Element data contains the configuration and properties of individual UGC elements.
        /// </summary>
        /// <param name="elementId">The unique identifier of the element to fetch.</param>
        /// <returns>A task that completes with the element data for the specified element.</returns>
        UniTask<UgcTemplateElementData> FetchElementDataAsync(string elementId);
    }
}
