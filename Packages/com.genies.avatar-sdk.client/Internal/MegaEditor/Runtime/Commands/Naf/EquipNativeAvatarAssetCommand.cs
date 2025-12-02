using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting;
using Genies.Customization.Framework;
using Genies.Naf;
using Genies.Naf.Content;
using Genies.ServiceManagement;

namespace Genies.Looks.Customization.Commands
{
    public class EquipNativeAvatarAssetCommand : ICommand
    {
        private readonly NativeUnifiedGenieController _controller;
        private readonly string                       _assetGuid;
        private readonly List<string>                 _previousEquippedAssetGuids;
        private readonly IAssetIdConverter            _idConverter;

        public EquipNativeAvatarAssetCommand(string assetGuid, NativeUnifiedGenieController controller)
        {
            _controller     = controller;
            _assetGuid      = assetGuid;
            _previousEquippedAssetGuids = controller.GetEquippedAssetIds();
            _idConverter = this.GetService<IAssetIdConverter>();
        }

        public async UniTask ExecuteAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var assetId = await _idConverter.ConvertToUniversalIdAsync(_assetGuid);
                var parameters = await _controller.AssetParamsService.FetchParamsAsync(_assetGuid);
                await _controller.EquipAssetAsync(assetId, parameters);
            }
            catch (Exception e)
            {
                // Log the error but don't crash the entire customization flow
                CrashReporter.Log($"Failed to equip asset '{_assetGuid}': {e.Message}", LogSeverity.Warning);
            }
        }

        public async UniTask UndoAsync(CancellationToken cancellationToken = default)
        {
            var fetchTasks = _previousEquippedAssetGuids
                .Select(async id => (id, await _controller.AssetParamsService.FetchParamsAsync(id)))
                .ToArray();
            var prevIds = (await UniTask.WhenAll(fetchTasks)).ToList();

            await _controller.SetEquippedAssetsAsync(prevIds);
        }
    }
}
