using System;
using Genies.UIFramework;
using UnityEngine;

namespace Genies.Customization.MegaEditor.UGCTemplates
{
    public class CtaUgcTemplateItemPickerCellView : MonoBehaviour
    {
        [SerializeField]
        private GeniesButton ctaButton;

        public event Action CtaClicked;
        private void Awake()
        {
            ctaButton.onClick.AddListener(()=> CtaClicked?.Invoke());
        }
    }
}
