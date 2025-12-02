using System;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.UI.Widgets
{
    /// <summary>
    /// Config class for a given pair of None and Create New CTA buttons.
    /// </summary>
    [Serializable]
    public class NoneOrNewCTAButtonConfig
    {
        [SerializeField] private CTAButtonConfig _noneButton;
        [SerializeField] private CTAButtonConfig _createNewButton;
        [SerializeField] private HorizontalOrVerticalLayoutGroup _layoutGroup;

        [SerializeField] private bool _isVerticallyStacked = true;
        public CTAButtonConfig NoneButton => _noneButton;
        public CTAButtonConfig CreateNewButton => _createNewButton;
        public bool IsVerticallyStacked => _isVerticallyStacked;

        public HorizontalOrVerticalLayoutGroup LayoutGroup => _layoutGroup;
    }
}
