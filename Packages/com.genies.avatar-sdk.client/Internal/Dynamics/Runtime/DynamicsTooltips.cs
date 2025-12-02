namespace Genies.Components.Dynamics
{
    public static class DynamicsTooltips
    {
        public const string UpdateMethod =
            "The method by which the dynamics system runs update steps.\n\n" +
            "Fixed_Update runs dynamics updates from the FixedUpdate method. This matches Unity's physics engine.\n\n" +
            "Update runs dynamics updates from the Update method.\n\n" +
            "Specified FPS allows both the dynamics update steps as well as the update of Unity transforms to run at a specified FPS.";

        public const string ComputeMethod =
            "The method by which the dynamics system will update its state\n\n" +
            "CPU_Single_Thread updates via the CPU and single threaded\n\n" +
            "CPU_JOBS updates via CPU, but threads the particle update\n\n" +
            "GPU_Compute_Shader uses the GPU, but is currently not stable";
        
        public const string Iterations =
            "Each iteration further divides the dynamics calculation interval. " +
            "This can provide a more stable and accurate simulation, however, the computation cost will be multiplied by the number of iterations." +
            "Long chains may require higher iterations to prevent each link from stretching.";

        public const string PreWarmTime = "The amount of time, in seconds, that the dynamics should be run off-screen in order to settle into a neutral state.";

        public const string SfpsUpdateFPS = "The rate at wich dynamics transforms are pushed back to their corresponding unity object transforms when in specified FPS mode.";

        public const string SfpsDynamicsFPS = "The rate at wich dynamics calculations are refreshed when in specified FPS mode.";

        public const string Friction = "Represents the thickness of the medium the particles will travel in. (0 = total vacuum, 0.5 = water, 1 = wet cement)";

        public const string ParticleToParticleCollision = "Whether particles will collide with one another. Note that this is an expensive feature to be used with simple structures.";

        public const string StructureName = "A game object with a dynamic structure will be created at the root of the avatar hierarchy with this name.";

        public const string PositionAnchor = "How much the particle tends to stick to its original non-dynamic position.";

        public const string RotationAnchor = "How much the particle tends to stick to its original non-dynamic rotation.";

        public const string AffectsTransform = "If enabled, the transform of the game object containing this particle will move with the particle's motion.";

        public const string TargetObjectName = "The name of the game object where this component will be added.";

        public const string CollisionEnabled = "Whether the particle will collide with colliders, or other particles (requires particle-particle collision setting)";

        public const string ParticleCollisionRadius = "The radius of the sphere collider that represents the particle.";

        public const string ParticleCollisionOffset = "The local space offset of the center of the sphere collider that represents the particle.";

        public const string PauseStructure = "Used to achieve static poses in the dynamics structure. Freezes all particles in the structure when enabled.";

        public const string LinkStartParticleObjectName = "The name of the game object containing the start particle for this link component.";

        public const string LinkEndParticleObjectName = "The name of the game object containing the end particle for this link component.";

        public const string LinkMaintainStartRotation = "Maintains the relative rotation between the link direction and the start particle.";

        public const string LinkMaintainEndRotation = "Maintains the relative rotation between the link direction and the end particle.";

        public const string LinkStretchiness = "Allows the length of the link to adjust. (0 = solid, 0.5 = rubber band, 1 = no length constraint)";

        public const string MaxParticleVelocity = "The maximum velocity a particle can have in m/s. This value is above the maximum conceivable velocity a human can produce on one of their limbs, and low enough to correct pops from spliced animations.";

        public const string AnchorToStartParticleRotation = "Rotates the link to match the original orientation with the start particle. (0 = free hinge, 0.5 = floppy spring, 1 = firm anchor)";

        public const string AngleLimiting = "Prevents the link from pivoting more than a specified amount from its original orientation with the start particle. (0 = no limit, 0.5 = 90 degree limit, 1 = fully locked to original)";

        public const string ShowHomeTransforms = "Displays the local position and rotation of each particle as they were at the start of the simulation.";

        public const string ShowStatistics = "Displays a summary of performance statistics.";

        public const string DynamicsRootName = "Name of the game object within the hierarchy from which to search for objects to attach dynamics components to.\n\nThe root must be high enough in the hiearchy to contain all particles, links, and colliders within its descendents.";

        public const string DynamicsHolderName = "A game object with this name will be created under the dynamics root. All dynamics structures will be added as children under this holder object.";
    }
}
