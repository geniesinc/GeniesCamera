using System.Runtime.CompilerServices;

#if GENIES_SDK && !GENIES_INTERNAL
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk.Editor")]
[assembly: InternalsVisibleTo("Genies.Avatars.Sdk.Sample")]

[assembly: InternalsVisibleTo("Genies.AvatarEditor")]
[assembly: InternalsVisibleTo("Genies.Multiplayer")]
[assembly: InternalsVisibleTo("Genies.Sdk.Avatar")]
[assembly: InternalsVisibleTo("Genies.Sdk.Core")]
#endif

