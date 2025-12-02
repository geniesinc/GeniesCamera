
namespace Genies.Customization.Framework
{
    /// <summary>
    /// Configures a customization. Inherits <see cref="ICustomizationController"/> to reduce
    /// call chains (Decorator pattern).
    /// </summary>
    public interface ICustomizationConfig : ICustomizationController
    {
        
        /// <summary>
        /// The model of the customization controller
        /// </summary>
        public ICustomizationController CustomizationController { get; set; }
    }
}
