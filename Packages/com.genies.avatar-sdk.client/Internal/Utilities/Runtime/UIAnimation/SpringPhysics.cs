using UnityEngine;

namespace Genies.UI.Animations
{
    /// <summary>
    /// Spring physics calculator for natural, physics-based animations
    /// Uses damped spring system: more organic than easing curves
    /// </summary>
    public static class SpringPhysics
    {
        /// <summary>
        /// Common spring presets for quick configuration
        /// </summary>
        public static class Presets
        {
            // Smooth, non-bouncy spring (good for UI)
            public static readonly SpringConfig Smooth = new SpringConfig(200f, 20f);

            // Gentle bounce (subtle overshoot)
            public static readonly SpringConfig Gentle = new SpringConfig(120f, 12f);

            // Bouncy spring (noticeable overshoot)
            public static readonly SpringConfig Bouncy = new SpringConfig(200f, 10f);

            // Snappy, quick response
            public static readonly SpringConfig Snappy = new SpringConfig(300f, 20f);

            // Slow, smooth motion
            public static readonly SpringConfig Slow = new SpringConfig(100f, 15f);

            // Very bouncy (fun, playful)
            public static readonly SpringConfig Wobbly = new SpringConfig(180f, 6f);
        }

        /// <summary>
        /// Spring configuration parameters
        /// </summary>
        public struct SpringConfig
        {
            public float Stiffness;  // How tight the spring is (higher = faster)
            public float Damping;    // How much the spring resists motion (higher = less bounce)
            public float Mass;       // Mass of the object (usually 1.0)

            public SpringConfig(float stiffness, float damping, float mass = 1f)
            {
                Stiffness = stiffness;
                Damping = damping;
                Mass = mass;
            }
        }

        /// <summary>
        /// Spring state for a single float value
        /// </summary>
        public class SpringState
        {
            public float Position;
            public float Velocity;
            public float Target;

            public SpringState(float initialPosition, float target)
            {
                Position = initialPosition;
                Velocity = 0f;
                Target = target;
            }
        }

        /// <summary>
        /// Updates spring state using semi-implicit Euler integration
        /// Returns true if spring has settled (reached equilibrium)
        /// </summary>
        public static bool UpdateSpring(SpringState state, SpringConfig config, float deltaTime)
        {
            // Spring force: F = -k * x (Hooke's law)
            float displacement = state.Position - state.Target;
            float springForce = -config.Stiffness * displacement;

            // Damping force: F = -c * v
            float dampingForce = -config.Damping * state.Velocity;

            // Total force
            float totalForce = springForce + dampingForce;

            // Acceleration: a = F / m
            float acceleration = totalForce / config.Mass;

            // Update velocity and position (semi-implicit Euler)
            state.Velocity += acceleration * deltaTime;
            state.Position += state.Velocity * deltaTime;

            // Check if spring has settled (within threshold and low velocity)
            const float positionThreshold = 0.001f;
            const float velocityThreshold = 0.001f;

            return Mathf.Abs(displacement) < positionThreshold &&
                   Mathf.Abs(state.Velocity) < velocityThreshold;
        }

        /// <summary>
        /// Calculate spring for Vector2
        /// </summary>
        public static bool UpdateSpringVector2(ref Vector2 position, ref Vector2 velocity, Vector2 target,
            SpringConfig config, float deltaTime)
        {
            // Calculate spring force for each axis
            Vector2 displacement = position - target;
            Vector2 springForce = -config.Stiffness * displacement;
            Vector2 dampingForce = -config.Damping * velocity;

            Vector2 acceleration = (springForce + dampingForce) / config.Mass;

            // Update
            velocity += acceleration * deltaTime;
            position += velocity * deltaTime;

            // Check if settled
            const float threshold = 0.001f;
            return displacement.sqrMagnitude < threshold * threshold &&
                   velocity.sqrMagnitude < threshold * threshold;
        }

        /// <summary>
        /// Calculate spring for Vector3
        /// </summary>
        public static bool UpdateSpringVector3(ref Vector3 position, ref Vector3 velocity, Vector3 target,
            SpringConfig config, float deltaTime)
        {
            Vector3 displacement = position - target;
            Vector3 springForce = -config.Stiffness * displacement;
            Vector3 dampingForce = -config.Damping * velocity;

            Vector3 acceleration = (springForce + dampingForce) / config.Mass;

            velocity += acceleration * deltaTime;
            position += velocity * deltaTime;

            const float threshold = 0.001f;
            return displacement.sqrMagnitude < threshold * threshold &&
                   velocity.sqrMagnitude < threshold * threshold;
        }

        /// <summary>
        /// Calculate spring for Color
        /// </summary>
        public static bool UpdateSpringColor(ref Color color, ref Vector4 velocity, Color target,
            SpringConfig config, float deltaTime)
        {
            // Treat color as Vector4 for spring calculations
            Vector4 current = new Vector4(color.r, color.g, color.b, color.a);
            Vector4 targetVec = new Vector4(target.r, target.g, target.b, target.a);

            Vector4 displacement = current - targetVec;
            Vector4 springForce = -config.Stiffness * displacement;
            Vector4 dampingForce = -config.Damping * velocity;

            Vector4 acceleration = (springForce + dampingForce) / config.Mass;

            velocity += acceleration * deltaTime;
            current += velocity * deltaTime;

            // Clamp color values
            color = new Color(
                Mathf.Clamp01(current.x),
                Mathf.Clamp01(current.y),
                Mathf.Clamp01(current.z),
                Mathf.Clamp01(current.w)
            );

            const float threshold = 0.001f;
            return displacement.sqrMagnitude < threshold * threshold &&
                   velocity.sqrMagnitude < threshold * threshold;
        }

        /// <summary>
        /// Calculate spring for Quaternion (rotation)
        /// </summary>
        public static bool UpdateSpringQuaternion(ref Quaternion rotation, ref Vector3 angularVelocity,
            Quaternion target, SpringConfig config, float deltaTime)
        {
            // Convert to angle-axis representation
            Quaternion displacement = target * Quaternion.Inverse(rotation);
            displacement.ToAngleAxis(out float angle, out Vector3 axis);

            // Normalize angle to [-180, 180]
            if (angle > 180f)
            {
                angle -= 360f;
            }

            Vector3 torque = -config.Stiffness * (angle * axis * Mathf.Deg2Rad);
            Vector3 damping = -config.Damping * angularVelocity;

            Vector3 angularAcceleration = (torque + damping) / config.Mass;

            angularVelocity += angularAcceleration * deltaTime;

            // Apply rotation
            Quaternion deltaRotation = Quaternion.AngleAxis(
                angularVelocity.magnitude * Mathf.Rad2Deg * deltaTime,
                angularVelocity.normalized
            );
            rotation = deltaRotation * rotation;

            const float threshold = 0.001f;
            return Mathf.Abs(angle) < threshold && angularVelocity.sqrMagnitude < threshold;
        }
    }
}

