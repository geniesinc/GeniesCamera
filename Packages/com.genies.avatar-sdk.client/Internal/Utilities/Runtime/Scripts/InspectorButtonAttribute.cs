using System;

namespace Genies.Utilities
{
    /// <summary>
    /// Attribute for methods in MonoBehaviour classes that creates a button in the Inspector to invoke the method.
    /// The decorated method must not take any parameters.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class InspectorButtonAttribute : Attribute
    {
        /// <summary>
        /// Defines when an InspectorButton can be invoked.
        /// </summary>
        public enum ExecutionMode
        {
            /// <summary>
            /// Button can be invoked in both Edit Mode and Play Mode.
            /// </summary>
            Any,

            /// <summary>
            /// Button can only be invoked in Edit Mode (when not playing).
            /// </summary>
            EditMode,

            /// <summary>
            /// Button can only be invoked in Play Mode (when playing).
            /// </summary>
            PlayMode
        }

        /// <summary>
        /// The text displayed on the button. If null or empty, uses the method name.
        /// </summary>
        public string ButtonText { get; }

        /// <summary>
        /// Defines when the button can be invoked (Edit Mode, Play Mode, or Any).
        /// </summary>
        public ExecutionMode Mode { get; }

        /// <summary>
        /// Creates an InspectorButtonAttribute with default button text (method name) and mode (Any).
        /// </summary>
        public InspectorButtonAttribute()
        {
            ButtonText = null;
            Mode = ExecutionMode.Any;
        }

        /// <summary>
        /// Creates an InspectorButtonAttribute with custom button text and default mode (Any).
        /// </summary>
        /// <param name="buttonText">The text to display on the button</param>
        public InspectorButtonAttribute(string buttonText)
        {
            ButtonText = buttonText;
            Mode = ExecutionMode.Any;
        }

        /// <summary>
        /// Creates an InspectorButtonAttribute with custom button text and execution mode.
        /// </summary>
        /// <param name="buttonText">The text to display on the button</param>
        /// <param name="mode">When the button can be invoked</param>
        public InspectorButtonAttribute(string buttonText, ExecutionMode mode)
        {
            ButtonText = buttonText;
            Mode = mode;
        }

        /// <summary>
        /// Creates an InspectorButtonAttribute with default button text (method name) and custom execution mode.
        /// </summary>
        /// <param name="mode">When the button can be invoked</param>
        public InspectorButtonAttribute(ExecutionMode mode)
        {
            ButtonText = null;
            Mode = mode;
        }
    }
}
