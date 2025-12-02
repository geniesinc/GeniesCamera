/**
 * The IAssetsService is heavily inspired in the Addressables API, so we have load methods that allows to set the merge mode. But we want to create a
 * layer of abstraction between Addressables and IAssetsService, so I have created this enum outside of the Addressables namespace.
 */

namespace Genies.Assets.Services
{
    public enum MergingMode
    {
        None = 0,
        UseFirst = 0,
        Union,
        Intersection
    }
}
