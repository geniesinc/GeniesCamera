using System.Collections.Generic;
using UnityEngine;

namespace Genies.Services.DynamicConfigs
{
    /// <summary>
    /// A collection of sensitive data info of the dynamic configs
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "DynamicConfigDataInfo", menuName = "GeniesParty/Dynamic Configs/DynamicConfigDataInfo")]
#endif
    public class DynamicConfigDataInfo : ScriptableObject
    {
        [SerializeField] private List<string> _data;
        public List<string> Data => _data;
    }
}
