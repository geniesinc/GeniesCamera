using System;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace Genies.Utilities.Editor.Inspector
{
    public class AccordionSection
    {
        /// <summary>
        /// The title shown in the header.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Controls the expansion state of the section.
        /// </summary>
        public AnimBool Expanded { get; private set; }

        // Custom style for the container (the outer rounded rectangle with an outline).
        private GUIStyle _ContainerStyle;

        // Custom style for the header button.
        private GUIStyle _HeaderStyle;

        /// <summary>
        /// Creates a new AccordionSection.
        /// </summary>
        /// <param name="title">The header text for the section.</param>
        /// <param name="animationSpeed">The speed multiplier for the expansion animation.</param>
        /// <param name="initiallyExpanded">Whether the section starts expanded.</param>
        public AccordionSection(string title, float animationSpeed = 3f, bool initiallyExpanded = false)
        {
            Title = title;
            Expanded = new AnimBool(initiallyExpanded);
            Expanded.speed = animationSpeed;
            Expanded.valueChanged.RemoveAllListeners();
            Expanded.valueChanged.AddListener(RepaintWindow);
            SetupStyles();
        }

        public AccordionSection()
        {
            Title = "Default";
            Expanded = new AnimBool(false)
            {
                speed = 3f
            };
            Expanded.valueChanged.RemoveAllListeners();
            Expanded.valueChanged.AddListener(RepaintWindow);
            SetupStyles();
        }

        public void Initialize(string title, float animationSpeed = 3f)
        {
            Title = title;
            Expanded.speed = animationSpeed;
        }

        /// <summary>
        /// Draws the entire accordion section. The container expands to wrap both the header and the content.
        /// </summary>
        /// <param name="drawContent">Callback to draw the section's content.</param>
        public void Draw(Action drawContent)
        {
            EditorGUILayout.BeginVertical(_ContainerStyle);

            var arrow = Expanded.target ? "▼ " : "▶ ";
            if (GUILayout.Button(arrow + Title, _HeaderStyle))
            {
                Expanded.target = !Expanded.target;
            }

            using (var fadeGroup = new EditorGUILayout.FadeGroupScope(Expanded.faded))
            {
                if (fadeGroup.visible)
                {
                    drawContent?.Invoke();
                }
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Configures the custom GUIStyles for the container and header.
        /// </summary>
        private void SetupStyles()
        {
            // Container style: based on helpBox to get rounded corners and an outline,
            // but with reduced margin and padding so the stroke is tighter.
            _ContainerStyle = new GUIStyle(EditorStyles.helpBox)
            {
                margin = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(4, 4, 2, 2),
                border = new RectOffset(8, 8, 8, 8),
                fixedHeight = 0 // Allow height to expand to fit content.
            };

            // Header style: based on miniButton but with a larger fixed height.
            _HeaderStyle = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(0, 0, 0, 4),
                padding = new RectOffset(10, 10, 4, 4),
                fixedHeight = EditorGUIUtility.singleLineHeight + 20, // Increased height to avoid clipping
                fontStyle = FontStyle.Bold
            };
        }

        /// <summary>
        /// Forces the focused EditorWindow to repaint.
        /// </summary>
        private void RepaintWindow()
        {
            if (EditorWindow.focusedWindow != null)
            {
                EditorWindow.focusedWindow.Repaint();
            }
        }
    }
}
