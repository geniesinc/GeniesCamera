using Genies.MakeupPresets;
using UnityEngine;

namespace Genies.Models.Makeup
{
    public class MakeupTemplate : DecoratedSkinTemplate, IDynamicAsset
    {
        public const int CurrentPipelineVersion = 0;
        public int PipelineVersion { get; set; } = CurrentPipelineVersion;

        [SerializeField] private string _guid; // The unique identifier for the FlairContainer instance.
        public virtual MakeupPresetCategory Category { get; set; }

        public string Guid
        {
            get => _guid;
            set => _guid = value;
        }
    }
}
