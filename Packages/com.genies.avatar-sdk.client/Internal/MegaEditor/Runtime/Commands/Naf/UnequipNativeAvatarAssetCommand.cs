using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Customization.Framework;
using Genies.Naf;

namespace Genies.Looks.Customization.Commands
{
    public class UnequipNativeAvatarAssetCommand : ICommand
    {
        private readonly NativeUnifiedGenieController _controller;
        private readonly string                       _assetGuid;
        private readonly List<string>                 _previousEquippedAssetGuids;

        public UnequipNativeAvatarAssetCommand(string assetGuid, NativeUnifiedGenieController controller)
        {
            _controller     = controller;
            _assetGuid      = assetGuid;
            _previousEquippedAssetGuids = controller.GetEquippedAssetIds();
        }

        public UniTask ExecuteAsync(CancellationToken cancellationToken = default)
        {
            return _controller.UnequipAssetAsync(_assetGuid);
        }

        public UniTask UndoAsync(CancellationToken cancellationToken = default)
        {
            var prevIds = _previousEquippedAssetGuids.Select(id => (id, new Dictionary<string, string>())).ToList();
            return _controller.SetEquippedAssetsAsync(prevIds);
        }
    }
}
