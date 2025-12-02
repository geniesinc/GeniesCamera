using System.Collections.Generic;
using System.Linq;

namespace Genies.Utilities
{
    /// <summary>
    /// Can find the shortest conversion paths for a given conversion target.
    /// </summary>
    public sealed class DefinitionConversionPathFinder
    {
        /// <summary>
        /// The maximum length that any conversion path could take within the targets provided to this path finder.
        /// </summary>
        public int MaxPathLength => _targetsBySourceVersion.Count;

        /// <summary>
        /// All the conversion paths that can be produced by this finder.
        /// </summary>
        public IReadOnlyCollection<DefinitionConversionTarget> SupportedTargets => _supportedTargets;

        private readonly Dictionary<string, HashSet<string>> _targetsBySourceVersion;
        private readonly HashSet<DefinitionConversionTarget> _supportedTargets;
        private readonly Stack<List<DefinitionConversionTarget>> _pathPool;
        private readonly HashSet<string> _checkedSourceVersions;

        public DefinitionConversionPathFinder(IEnumerable<DefinitionConversionTarget> targets)
        {
            _targetsBySourceVersion = new Dictionary<string, HashSet<string>>();
            _pathPool = new Stack<List<DefinitionConversionTarget>>();
            _checkedSourceVersions = new HashSet<string>();

            foreach (DefinitionConversionTarget target in targets)
            {
                foreach (var targetSourceVersion in target.SourceVersions)
                {
                    if (!_targetsBySourceVersion.TryGetValue(targetSourceVersion, out HashSet<string> targetVersions))
                    {
                        _targetsBySourceVersion.Add(targetSourceVersion, targetVersions = new HashSet<string>());
                    }

                    targetVersions.Add(target.TargetVersion);
                }
            }

            FillSupportedTargets();
        }

        public bool HasConversionPath(DefinitionConversionTarget target)
        {
            return _supportedTargets.Contains(target);
        }

        public bool HasConversionPath(string sourceVersion, string targetVersion)
        {
            return HasConversionPath(new DefinitionConversionTarget(new List<string>(){sourceVersion}, targetVersion));
        }

        /// <summary>
        /// Given a conversion target it tries to find the shortest path within the targets contained by this path
        /// finder instance. If successful, the target path will be added in the proper order to the given
        /// <see cref="targetPath"/> collection.
        /// </summary>
        public bool TryFindConversionPath(string sourceVersion, string targetVersion, ICollection<DefinitionConversionTarget> targetPath = null)
        {
            // check the supported targets first as it is much faster
            if (!HasConversionPath(sourceVersion, targetVersion))
            {
                return false;
            }

            if (!TryFindPath(sourceVersion, targetVersion, out List<DefinitionConversionTarget> path))
            {
                return false;
            }

            // we found a path but we were given a null collection to add the targets to
            if (targetPath is null)
            {
                ReleasePath(path);
                return true;
            }

            // add the targets inverting the order as our internal method will return the path inverted
            for (int i = path.Count - 1; i >= 0; --i)
            {
                targetPath.Add(path[i]);
            }

            ReleasePath(path);
            return true;
        }

        /// <summary>
        /// Tries to find the shortest target path between the given source and target versions. The returned target
        /// list is inverted.
        /// </summary>
        private bool TryFindPath(string sourceVersion, string targetVersion, out List<DefinitionConversionTarget> path)
        {
            _checkedSourceVersions.Clear();
            bool result = TryFindPathRecursive(sourceVersion, targetVersion, out path);
            _checkedSourceVersions.Clear();

            return result;
        }

        private bool TryFindPathRecursive(string sourceVersion, string targetVersion, out List<DefinitionConversionTarget> path)
        {
            path = null;

            // if we already tried this source version it means that we have infinite loops within the possible paths. This way we can break the loop
            if (!_checkedSourceVersions.Add(sourceVersion))
            {
                return false;
            }

            // check if the given source version has any conversion targets
            if (!_targetsBySourceVersion.TryGetValue(sourceVersion, out HashSet<string> targetVersions))
            {
                return false;
            }

            // try to find the path for each available conversion target for the given source version
            foreach (string availableTargetVersion in targetVersions)
            {
                // if the available target version is the same final target then just create a new path list and return
                if (availableTargetVersion == targetVersion)
                {
                    path = GetPath();
                    path.Add(new DefinitionConversionTarget(new List<string>(){sourceVersion}, targetVersion));
                    return true;
                }

                // recursively call this method for our initial target and the current available target as the source version
                if (!TryFindPath(availableTargetVersion, targetVersion, out List<DefinitionConversionTarget> currentPath))
                {
                    continue;
                }

                // if this is the first path we found then just assign it to the path variable
                if (path is null)
                {
                    path = currentPath;
                    continue;
                }

                // if this is not the first path we found, but it is shorter than the last one then replace it
                if (currentPath.Count < path.Count)
                {
                    ReleasePath(path);
                    path = currentPath;
                }
                else
                {
                    // just release the path we just found as it is longer
                    ReleasePath(currentPath);
                }
            }

            if (path is null)
            {
                return false;
            }

            // we have to add the current conversion target to the path (the final path result will be inverted)
            var currentTarget = new DefinitionConversionTarget(new List<string>(){sourceVersion}, path[^1].SourceVersions.Last());
            path.Add(currentTarget);

            return true;
        }

        private List<DefinitionConversionTarget> GetPath()
        {
            return _pathPool.Count > 0 ? _pathPool.Pop() : new List<DefinitionConversionTarget>(MaxPathLength);
        }

        private void ReleasePath(List<DefinitionConversionTarget> path)
        {
            path.Clear();
            _pathPool.Push(path);
        }

        // this method fills all supported conversion targets taking into account multi-path conversions
        private void FillSupportedTargets()
        {
            foreach (string sourceVersion in _targetsBySourceVersion.Keys)
            {
                FillSupportedTargetsFor(sourceVersion, sourceVersion);
            }

            void FillSupportedTargetsFor(string sourceVersion, string currentSourceVersion)
            {
                if (!_targetsBySourceVersion.TryGetValue(currentSourceVersion, out HashSet<string> targetVersions))
                {
                    return;
                }

                foreach (string targetVersion in targetVersions)
                {
                    _supportedTargets.Add(new DefinitionConversionTarget(new List<string>(){sourceVersion}, targetVersion));
                    FillSupportedTargetsFor(sourceVersion, targetVersion);
                }
            }
        }
    }
}
