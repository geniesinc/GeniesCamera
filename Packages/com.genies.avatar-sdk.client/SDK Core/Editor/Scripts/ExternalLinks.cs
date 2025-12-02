// We want a dependency to 'com.genies.sdk.bootstrap' but Unity's Asset Store
// validation tool will flag the dependency if it's not referenced and used.
// This script serves to prevent the validation tool from flagging the dependency.

namespace Genies.Sdk.Core.Editor
{
    public class ExternalLinks : Bootstrap.Editor.ExternalLinks
    {
    }
}
