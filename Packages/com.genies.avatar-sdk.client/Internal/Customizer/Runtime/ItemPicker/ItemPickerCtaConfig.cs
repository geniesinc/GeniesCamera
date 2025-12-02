using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.UI.Widgets;

namespace Genies.Customization.Framework.ItemPicker
{
    public class ItemPickerCtaConfig
    {
        public delegate UniTask<bool> NoneSelectedAsyncDelegate(CancellationToken cancellationToken);

        public readonly CTAButtonType CtaType;
        public readonly Action CreateNewAction;
        public readonly NoneSelectedAsyncDelegate NoneSelectedAsync;
        public NoneOrNewCTAController HorizontalLayoutCtaOverride;
        public NoneOrNewCTAController GridLayoutCtaOverride;

        public ItemPickerCtaConfig(
            CTAButtonType ctaType,
            NoneSelectedAsyncDelegate noneSelectedDelegate = null,
            Action createNewAction = null,
            NoneOrNewCTAController horizontalLayoutCtaOverride = null,
            NoneOrNewCTAController gridLayoutCtaOverride = null)
        {
            CtaType = ctaType;
            NoneSelectedAsync = noneSelectedDelegate;
            CreateNewAction = createNewAction;
            HorizontalLayoutCtaOverride = horizontalLayoutCtaOverride;
            GridLayoutCtaOverride = gridLayoutCtaOverride;
        }
    }
}
