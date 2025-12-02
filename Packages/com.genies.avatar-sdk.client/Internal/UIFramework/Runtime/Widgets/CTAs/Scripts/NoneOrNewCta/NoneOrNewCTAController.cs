using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Genies.UI.Widgets
{
    public enum CTAButtonType
    {
        NoneAndNewCTA,
        SingleNoneCTA,
        SingleCreateNewCTA,
        CustomizeCTA
    }

    /// <summary>
    /// A controller that provides 2 call to actions, a None Selected or a Create New
    /// - when none selected is pressed, it will dispatch an event for the collection to reset it's current selection
    /// - when create new is pressed, it will dispatch an event for the collection to create a new item.
    /// </summary>
    public class NoneOrNewCTAController : MonoBehaviour
    {
        public event Action NoneSelected;
        public event Action CreateNewSelected;

        [FormerlySerializedAs("_NoneOrNewCTAConfig")] [FormerlySerializedAs("_verticalNoneOrNewCTAConfig")]
        public NoneOrNewCTAButtonConfig NoneOrNewCTAConfig;
        [FormerlySerializedAs("_singleNoneCTAConfig")] public CTAButtonConfig SingleNoneCTAConfig;
        [FormerlySerializedAs("_singleCreateNewCTAConfig")] public CTAButtonConfig SingleCreateNewCTAConfig;
        [FormerlySerializedAs("_customizeCTAConfig")] public CTAButtonConfig CustomizeCTAConfig;

        [FormerlySerializedAs("_activeCTAButton")] public CTAButtonType ActiveCTAButton;

        public bool IsCollapsable
        {
            get
            {
                switch (ActiveCTAButton)
                {
                    case CTAButtonType.NoneAndNewCTA:
                        return NoneOrNewCTAConfig.NoneButton.IsCollapsible && NoneOrNewCTAConfig.CreateNewButton.IsCollapsible;
                    case CTAButtonType.SingleNoneCTA:
                        return SingleNoneCTAConfig.IsCollapsible;
                    case CTAButtonType.SingleCreateNewCTA:
                        return SingleCreateNewCTAConfig.IsCollapsible;
                    case CTAButtonType.CustomizeCTA:
                        return CustomizeCTAConfig.IsCollapsible;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void Awake()
        {
            SetAndEnableActiveCTAButton(ActiveCTAButton);

            AddListeners();
        }

        public void SetAndEnableActiveCTAButton(CTAButtonType newActiveCtaButton)
        {
            ActiveCTAButton = newActiveCtaButton;

            NoneOrNewCTAConfig.NoneButton.GameObject.SetActive(false);
            NoneOrNewCTAConfig.CreateNewButton.GameObject.SetActive(false);

            SingleNoneCTAConfig.GameObject.SetActive(false);
            CustomizeCTAConfig.GameObject.SetActive(false);

            SingleCreateNewCTAConfig.GameObject.SetActive(false);

            switch (ActiveCTAButton)
            {
                case CTAButtonType.NoneAndNewCTA:
                    NoneOrNewCTAConfig.NoneButton.GameObject.SetActive(true);
                    NoneOrNewCTAConfig.CreateNewButton.GameObject.SetActive(true);
                    break;
                case CTAButtonType.SingleNoneCTA:
                    SingleNoneCTAConfig.GameObject.SetActive(true);
                    break;
                case CTAButtonType.SingleCreateNewCTA:
                    SingleCreateNewCTAConfig.GameObject.SetActive(true);
                    break;
                case CTAButtonType.CustomizeCTA:
                    CustomizeCTAConfig.GameObject.SetActive(true);
                    break;
            }
        }

        private void AddListeners()
        {
            NoneOrNewCTAConfig.NoneButton.CTAButton.onClick.AddListener(OnNoneSelected);
            NoneOrNewCTAConfig.CreateNewButton.CTAButton.onClick.AddListener(OnCreateNewSelected);

            SingleNoneCTAConfig.CTAButton.onClick.AddListener(OnNoneSelected);
            CustomizeCTAConfig.CTAButton.onClick.AddListener(OnNoneSelected);

            SingleCreateNewCTAConfig.CTAButton.onClick.AddListener(OnCreateNewSelected);
        }

        private void RemoveListeners()
        {
            NoneOrNewCTAConfig.NoneButton.CTAButton.onClick.RemoveListener(OnNoneSelected);
            NoneOrNewCTAConfig.CreateNewButton.CTAButton.onClick.RemoveListener(OnCreateNewSelected);

            SingleNoneCTAConfig.CTAButton.onClick.RemoveListener(OnNoneSelected);
            CustomizeCTAConfig.CTAButton.onClick.RemoveListener(OnNoneSelected);

            SingleCreateNewCTAConfig.CTAButton.onClick.RemoveListener(OnCreateNewSelected);
        }

        public void ExpandAll()
        {
            switch (ActiveCTAButton)
            {
                case CTAButtonType.NoneAndNewCTA:
                    Expand(NoneOrNewCTAConfig.NoneButton);
                    Expand(NoneOrNewCTAConfig.CreateNewButton);
                    break;
                case CTAButtonType.SingleNoneCTA:
                    Expand(SingleNoneCTAConfig);
                    break;
                case CTAButtonType.SingleCreateNewCTA:
                    Expand(SingleCreateNewCTAConfig);
                    break;
                case CTAButtonType.CustomizeCTA:
                    Expand(CustomizeCTAConfig);
                    break;
            }
        }

        public void CollapseAll()
        {
            switch (ActiveCTAButton)
            {
                case CTAButtonType.NoneAndNewCTA:
                    Collapse(NoneOrNewCTAConfig.NoneButton);
                    Collapse(NoneOrNewCTAConfig.CreateNewButton);
                    break;
                case CTAButtonType.SingleNoneCTA:
                    Collapse(SingleNoneCTAConfig);
                    break;
                case CTAButtonType.SingleCreateNewCTA:
                    Collapse(SingleCreateNewCTAConfig);
                    break;
                case CTAButtonType.CustomizeCTA:
                    Collapse(CustomizeCTAConfig);
                    break;
            }
        }

        public void ResizeButtons(float normalizedFill)
        {
            switch (ActiveCTAButton)
            {
                case CTAButtonType.NoneAndNewCTA:
                    Resize(NoneOrNewCTAConfig.NoneButton,      normalizedFill);
                    Resize(NoneOrNewCTAConfig.CreateNewButton, normalizedFill);
                    break;
                case CTAButtonType.SingleNoneCTA:
                    Resize(SingleNoneCTAConfig, normalizedFill);
                    break;
                case CTAButtonType.SingleCreateNewCTA:
                    Resize(SingleCreateNewCTAConfig, normalizedFill);
                    break;
                case CTAButtonType.CustomizeCTA:
                    Resize(CustomizeCTAConfig, normalizedFill);
                    break;
            }
        }

        public void SetNoneSelected(bool isSelected)
        {
            switch (ActiveCTAButton)
            {
                case CTAButtonType.NoneAndNewCTA:
                    NoneOrNewCTAConfig.NoneButton.SetSelected(isSelected);
                    break;
                case CTAButtonType.SingleNoneCTA:
                    SingleNoneCTAConfig.SetSelected(isSelected);
                    break;
                case CTAButtonType.SingleCreateNewCTA:
                    break;
                case CTAButtonType.CustomizeCTA:
                    CustomizeCTAConfig.SetSelected(isSelected);
                    break;
            }
        }

        public void SetCTAActive(bool isActive)
        {
            switch (ActiveCTAButton)
            {
                case CTAButtonType.CustomizeCTA:
                    CustomizeCTAConfig.SetActive(isActive);
                    break;
            }
        }

        public float GetMaxExpandedSize(bool isVertical)
        {
            switch (ActiveCTAButton)
            {
                case CTAButtonType.NoneAndNewCTA:

                    float noneSize = 0;
                    float newSize  = 0;

                    noneSize = NoneOrNewCTAConfig.NoneButton.CTAButton.GetExpandedSize(isVertical);
                    newSize = NoneOrNewCTAConfig.CreateNewButton.CTAButton.GetExpandedSize(isVertical);

                    if ((isVertical && NoneOrNewCTAConfig.IsVerticallyStacked) || (!isVertical && !NoneOrNewCTAConfig.IsVerticallyStacked))
                    {
                        return noneSize + newSize + NoneOrNewCTAConfig.LayoutGroup.spacing * 2;
                    }

                    return Mathf.Max(noneSize, newSize);
                case CTAButtonType.SingleNoneCTA:
                    return SingleNoneCTAConfig.CTAButton.GetExpandedSize(isVertical);
                case CTAButtonType.SingleCreateNewCTA:
                    return SingleCreateNewCTAConfig.CTAButton.GetExpandedSize(isVertical);
                case CTAButtonType.CustomizeCTA:
                    return CustomizeCTAConfig.CTAButton.GetExpandedSize(isVertical);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public float GetMaxCollapsedSize(bool isVertical)
        {
            switch (ActiveCTAButton)
            {
                case CTAButtonType.NoneAndNewCTA:

                    float noneSize = 0;
                    float newSize  = 0;
                    noneSize = NoneOrNewCTAConfig.NoneButton.CTAButton.GetCollapsedSize();
                    newSize = NoneOrNewCTAConfig.CreateNewButton.CTAButton.GetCollapsedSize();

                    if ((isVertical && NoneOrNewCTAConfig.IsVerticallyStacked) || (!isVertical && !NoneOrNewCTAConfig.IsVerticallyStacked))
                    {
                        return noneSize + newSize + NoneOrNewCTAConfig.LayoutGroup.spacing * 2;
                    }

                    return Mathf.Max(noneSize, newSize);
                case CTAButtonType.SingleNoneCTA:
                    return SingleNoneCTAConfig.CTAButton.GetCollapsedSize();
                case CTAButtonType.SingleCreateNewCTA:
                    return SingleCreateNewCTAConfig.CTAButton.GetCollapsedSize();
                case CTAButtonType.CustomizeCTA:
                    return CustomizeCTAConfig.CTAButton.GetCollapsedSize();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ExpandOrCollapse(CTAButtonConfig ctaButtonConfig)
        {
            if (ctaButtonConfig.CTAButton.IsCollapsed)
            {
                Expand(ctaButtonConfig);
            }
            else
            {
                Collapse(ctaButtonConfig);
            }
        }

        private void Collapse(CTAButtonConfig ctaButtonConfig)
        {
            if (!ctaButtonConfig.IsCollapsible)
            {
                return;
            }

            ctaButtonConfig.CTAButton.Collapse();
        }

        private void Expand(CTAButtonConfig ctaButtonConfig)
        {
            if (!ctaButtonConfig.IsCollapsible)
            {
                return;
            }

            ctaButtonConfig.CTAButton.Expand();
        }

        private void Resize(CTAButtonConfig ctaButtonConfig, float fill)
        {
            if (!ctaButtonConfig.IsCollapsible)
            {
                return;
            }

            ctaButtonConfig.CTAButton.Resize(fill);
        }

        private void OnNoneSelected()
        {
            NoneSelected?.Invoke();
        }

        private void OnCreateNewSelected()
        {
            CreateNewSelected?.Invoke();
        }

        private void OnDestroy()
        {
            RemoveListeners();
        }
    }
}
