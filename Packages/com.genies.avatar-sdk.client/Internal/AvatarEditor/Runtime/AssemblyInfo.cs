using System.Runtime.CompilerServices;

#if GENIES_SDK && !GENIES_INTERNAL
[assembly: InternalsVisibleTo("Genies.Sdk.Avatar")]
[assembly: InternalsVisibleTo("Genies.Sdk.Core")]
#endif
