using System;
using UnityEngine;

namespace Genies.Customization.Framework.Actions
{
    [Serializable]
    public class ActionDrawerOption
    {
        public string displayName;
        public Action onClick;
        public Func<bool> getOptionEnabled;
        public bool riskOption = false;
        public Sprite icon;
    }
}
