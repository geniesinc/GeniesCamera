// Assets/Scripts/Avatars/AvatarProfileData.cs
using System;
using Genies.Naf;
using UnityEngine;

namespace Genies.AvatarEditor
{
[Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
internal class AvatarProfileData
#else
    public class AvatarProfileData
#endif
    {
        public Genies.Naf.AvatarDefinition Definition;
        public string HeadshotPath;
    }
}
