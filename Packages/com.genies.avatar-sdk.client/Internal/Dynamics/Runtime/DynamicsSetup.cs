using Genies.Avatars;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Genies.Dynamics;

namespace Genies.Components.Dynamics
{
    /// <summary>
    /// Contains a utilities for setting up dynamics on hierarchies that contain properly named dynamics joints.
    /// See <see cref="DynamicsNaming"/> for more information on joint naming conventions.
    /// </summary>
    public static class DynamicsSetup
    {
        // The name of the joint in the rig where the dynamics structure container will be placed.
        public const string DefaultHierarchyRootName = "Root";

        // The name of the game object that will serve as a container for dynamics structures.
        public const string DefaultDynamicsContainerName = "Dynamics_Structures";

        /// <summary>
        /// Finds the root of a hierarchy by searching for a game object with the default root name.
        /// </summary>
        /// <param name="startingObject">The object to start the search from.</param>
        /// <returns>The root object if found, otherwise null.</returns>
        public static GameObject FindRoot(GameObject startingObject)
        {
            if (startingObject == null)
            {
                return null;
            }

            // Check self
            if (startingObject.name == DynamicsSetup.DefaultHierarchyRootName)
            {
                return startingObject;
            }

            // Check ancestors and siblings
            Transform parent = startingObject.transform.parent;
            while (parent != null)
            {
                if (parent.gameObject.name == DynamicsSetup.DefaultHierarchyRootName)
                {
                    return parent.gameObject;
                }
                
                var siblingRoot = parent.transform.Find(DynamicsSetup.DefaultHierarchyRootName);

                if(siblingRoot != null)
                {
                    return siblingRoot.gameObject;
                }

                parent = parent.parent;
            }

            // Check descendants
            foreach (Transform child in startingObject.transform)
            {
                if (child.gameObject.name == DynamicsSetup.DefaultHierarchyRootName)
                {
                    return child.gameObject;
                }

                // Recursively check the descendants of the child
                GameObject childResult = FindRoot(child.gameObject);
                if (childResult != null)
                {
                    return childResult;
                }
            }

            return null;
        }

        public static List<DynamicsStructure> AddDynamicsToHierarchy(GameObject root)
        {
            var structures = new List<DynamicsStructure>();

            // Check for valid dynamics joints first.
            var particles = new List<DynamicsParticle>();
            DynamicsSetup.AddDynamicsParticleRecursive(root.transform, particles);
            if (particles.Count == 0)
            {
                return structures;
            }

            // Separate out particles by structure name.
            Dictionary<string, List<DynamicsParticle>> structureParticleMap = new Dictionary<string, List<DynamicsParticle>>();
            foreach (DynamicsParticle particle in particles)
            {
                string structureName = DynamicsNaming.GetStructureNameFromJointName(particle.gameObject.name);
                if (!structureParticleMap.ContainsKey(structureName))
                {
                    structureParticleMap.Add(structureName, new List<DynamicsParticle>());
                }
                structureParticleMap[structureName].Add(particle);
            }

            Transform container = root.transform.Find(DefaultDynamicsContainerName);
            if (container == null)
            {
                container = new GameObject(DefaultDynamicsContainerName).transform;
                container.parent = root.transform;
            }

            var links = new List<DynamicsLink>();
            DynamicsSetup.AddLinkConstraintsRecursive(root.transform, links);

            // Create a dynamics structure for each structure found.
            foreach (KeyValuePair<string, List<DynamicsParticle>> structureParticles in structureParticleMap)
            {
                GameObject structureGameObject = new GameObject($"{structureParticles.Key}DynamicsStructure");
                structureGameObject.transform.parent = container.transform;
                DynamicsStructure structure = structureGameObject.AddComponent<DynamicsStructure>();
                
                structure.Particles = structureParticles.Value;

                structure.Links = links.Where(link => structureParticles.Value.Contains(link.StartParticle) && structureParticles.Value.Contains(link.EndParticle)).ToList();

                // Find the longest chain and set iterations to an appropriate value.
                var maxChainLength = 0;
                foreach (DynamicsParticle particle in structureParticles.Value)
                {
                    if (int.TryParse(particle.gameObject.name.Last().ToString(), out var placeInChain) && placeInChain > maxChainLength)
                    {
                        maxChainLength = placeInChain;
                    }
                }
                structure.Iterations = maxChainLength / 4 + 1;

                // Add an empty collider list (prevents null ref exceptions.)
                // TODO: Add automatic collider set determination.
                structure.Colliders = new List<DynamicsCollider>();

                structures.Add(structure);
            }

            return structures;
        }

        /// <summary>
        /// Traverses a hierarchy and adds <see cref="DynamicsParticle"/> components to any joints whose name fits the dynamics naming convention.
        /// </summary>
        /// <param name="transform">The top-most transform to start the traversal.</param>
        /// <param name="particles">A list of particles to add to as particles are created.</param>
        public static void AddDynamicsParticleRecursive(Transform transform, List<DynamicsParticle> particles)
        {
            foreach (Transform child in transform)
            {
                if (Regex.IsMatch(child.gameObject.name, "(Left|Right|Center)(\\w+)(\\d+)Dyn(\\d+)"))
                {
                    DynamicsParticle particle = child.gameObject.AddComponent<DynamicsParticle>();
                    particle.Initialize();

                    particle.PositionAnchor = 0.5f;
                    particle.RotationAnchor = 0.5f;

                    particles.Add(particle);
                    AddDynamicsParticleRecursive(child, particles); // Keep chains in order
                    continue;
                }
                else
                {
                    AddDynamicsParticleRecursive(child, particles);
                }
            }
        }

        /// <summary>
        /// Traverses a hierarchy and adds <see cref="DynamicsLink"/> components to any joint pairs whose name fits the dynamics convention
        /// and signifies that a link is needed.
        /// </summary>
        /// <param name="transform">The top-most transform to start the traversal.</param>
        /// <param name="links">A list of links to add to as links are created.</param>
        public static void AddLinkConstraintsRecursive(Transform transform, List<DynamicsLink> links)
        {
            transform.TryGetComponent(out DynamicsParticle startParticle);

            if (startParticle == null)
            {
                foreach (Transform endTransform in transform)
                {
                    AddLinkConstraintsRecursive(endTransform, links);
                }

                return;
            }

            foreach (Transform endTransform in startParticle.transform)
            {
                if (endTransform.TryGetComponent(out DynamicsParticle endParticle))
                {
                    DynamicsLink link = startParticle.gameObject.AddComponent<DynamicsLink>();
                    links.Add(link);
                    link.StartParticle = startParticle;
                    link.EndParticle = endParticle;
                    link.MaintainEndParticleRotation = true;

                    var linkDistance = Vector3.Distance(startParticle.transform.position, endParticle.transform.position);

                    startParticle.CollisionRadius = linkDistance * 0.5f;
                    endParticle.CollisionRadius = linkDistance * 0.5f;

                    // Fully anchor the first particle in a chain.
                    if (startParticle.gameObject.name.Contains("0Dyn0"))
                    {
                        startParticle.PositionAnchor = 1f;
                        startParticle.RotationAnchor = 1f;
                        startParticle.CollisionEnabled = false; // Anchoring overrides collision so this will save resources.
                    }

                    endParticle.PositionAnchor = 0f;
                    endParticle.RotationAnchor = 0f;

                    link.Initialize();

                    AddLinkConstraintsRecursive(endTransform, links);
                }
            }
        }
    
        /// <summary>
        /// Populates a <see cref="DynamicsRecipe"/> with data from a <see cref="DynamicsStructure"/>.
        /// This allows the structure to be recreated from the serialized recipe data.
        /// </summary>
        /// <param name="recipe">The recipe to populate.</param>
        /// <param name="structure">The structure to populate the recipe with.</param>
        public static void PopulateRecipeData(DynamicsRecipe recipe, DynamicsStructure structure)
        {

            if (structure == null || recipe == null)
            {
                return;
            }

            // Global parameters
            recipe.StructureName = structure.name;
            recipe.DynamicsUpdateMethod = structure.DynamicsUpdateMethod;
            recipe.SfpsDynamicsFPS = structure.SfpsDynamicsFPS;
            recipe.SfpsUpdateFPS = structure.SfpsUpdateFPS;
            recipe.Iterations = structure.Iterations;
            recipe.Gravity = structure.Gravity;
            recipe.Friction = structure.Friction;
            recipe.ParticleToParticleCollision = structure.ParticleToParticleCollision;
            recipe.CollisionComputeMethod = structure.CollisionComputeMethod;

            // Particles
            recipe.ParticleRecipes = new List<ParticleRecipe>();
            if (structure.Particles != null)
            {
                foreach (DynamicsParticle particle in structure.Particles)
                {
                    if (particle == null)
                    {
                        continue;
                    }

                    var particleRecipe = new ParticleRecipe()
                    {
                        CollisionEnabled = particle.CollisionEnabled,
                        CollisionRadius = particle.CollisionRadius,
                        CollisionOffset = particle.CollisionOffset,
                        PositionAnchor = particle.PositionAnchor,
                        RotationAnchor = particle.RotationAnchor,
                        AffectsTransform = particle.AffectsTransform,
                        TargetObjectName = particle.gameObject.name
                    };

                    recipe.ParticleRecipes.Add(particleRecipe);
                }
            }

            // Links
            recipe.LinkRecipes = new List<LinkRecipe>();
            if (structure.Links != null)
            {
                foreach (DynamicsLink link in structure.Links)
                {
                    if (link == null)
                    {
                        continue;
                    }

                    var linkRecipe = new LinkRecipe()
                    {
                        StartParticleObjectName = link.StartParticle.gameObject.name,
                        EndParticleObjectName = link.EndParticle.gameObject.name,
                        MaintainStartParticleRotation = link.MaintainStartParticleRotation,
                        MaintainEndParticleRotation = link.MaintainEndParticleRotation,
                        AnchorToStartParticleRotation = link.AnchorToStartParticleRotation,
                        AngleLimiting = link.AngleLimiting,
                        Stretchiness = link.Stretchiness
                    };

                    recipe.LinkRecipes.Add(linkRecipe);
                }
            }

            // Colliders
            recipe.SphereColliderRecipes = new List<SphereColliderRecipe>();
            recipe.CapsuleColliderRecipes = new List<CapsuleColliderRecipe>();
            if (structure.Colliders != null)
            {
                foreach (DynamicsCollider collider in structure.Colliders)
                {
                    if (collider == null)
                    {
                        continue;
                    }

                    if (collider is DynamicsSphereCollider sphereCollider)
                    {
                        var sphereColliderRecipe = new SphereColliderRecipe()
                        {
                            TargetObjectName = collider.gameObject.name,
                            Offset = sphereCollider.Offset,
                            CollisionRadius = sphereCollider.CollisionRadius
                        };

                        recipe.SphereColliderRecipes.Add(sphereColliderRecipe);
                    }
                    else if (collider is DynamicsCapsuleCollider capsuleCollider)
                    {
                        var capsuleColliderRecipe = new CapsuleColliderRecipe()
                        {
                            TargetObjectName = capsuleCollider.gameObject.name,
                            Height = capsuleCollider.Height,
                            Offset = capsuleCollider.Offset,
                            Rotation = capsuleCollider.Rotation,
                            CollisionRadius = capsuleCollider.CollisionRadius
                        };

                        recipe.CapsuleColliderRecipes.Add(capsuleColliderRecipe);
                    }
                    else
                    {
                        Debug.Log($"ERROR: Collider type not implemented {collider.GetType()}");
                    }
                }
            }
        }

        /// <summary>
        /// Removes all dynamics components from a hierarchy.
        /// </summary>
        public static void RemoveDynamicsFromHierarchy(GameObject root)
        {
            foreach (DynamicsStructure structure in root.GetComponentsInChildren<DynamicsStructure>())
            {
                GameObject.DestroyImmediate(structure.gameObject);
            }

            foreach (DynamicsLink link in root.GetComponentsInChildren<DynamicsLink>())
            {
                Object.DestroyImmediate(link);
            }

            foreach (DynamicsParticle particle in root.GetComponentsInChildren<DynamicsParticle>())
            {
                Object.DestroyImmediate(particle);
            }

            foreach (DynamicsCollider collider in root.GetComponentsInChildren<DynamicsCollider>())
            {
                Object.DestroyImmediate(collider);
            }
        }

        public static void AddHumanoidCollider(ColliderConfiguration.HumanoidColliderLocation location, DynamicsStructure structure)
        {
            if (structure == null) 
            {
                Debug.LogError("Dynamics structure is null. Cannot add humanoid collider.");
                return;
            }

            var rigRoot = FindRoot(structure.gameObject);

            if (rigRoot == null)
            {
                Debug.LogError("Could not find rig root. Cannot add humanoid collider.");
                return;
            }

            if (!ColliderConfiguration.HumanoidColliderLocationToJointName.TryGetValue(location, out string jointName))
            {
                Debug.LogError($"Could not find joint name for {location}. Cannot add humanoid collider.");
                return;
            }

            var joint = rigRoot.transform.FindDeepChild(jointName);

            if (joint == null)
            {
                Debug.LogError($"Could not find joint {jointName}. Cannot add humanoid collider.");
                return;
            }

            // Utilize the collider setup function with parameters defined in <see cref="ColliderConfiguration"/>.
            var addCollider = ColliderConfiguration.HumanoidColliderLocationToColliderSetupDelegate[location];
            addCollider(structure, joint.gameObject);
        }

        public static void AddCapsuleCollider(DynamicsStructure structure, GameObject target, float height, float collisionRadius, Vector3 offset, Vector3 rotation)
        {
            DynamicsCapsuleCollider collider = target.AddComponent<DynamicsCapsuleCollider>();
            collider.Height = height;
            collider.CollisionRadius = collisionRadius;
            collider.Offset = offset;
            collider.Rotation = rotation;

            structure.Colliders.Add(collider);
        }

        public static void AddSphereCollider(DynamicsStructure structure, GameObject target, float collisionRadius, Vector3 offset)
        {
            DynamicsSphereCollider collider = target.AddComponent<DynamicsSphereCollider>();
            collider.CollisionRadius = collisionRadius;
            collider.Offset = offset;

            structure.Colliders.Add(collider);
        }
    }
}