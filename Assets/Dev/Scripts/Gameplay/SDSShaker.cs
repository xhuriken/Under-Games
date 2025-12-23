using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class SDSShaker : MonoBehaviour
{
    public static SDSShaker Instance;

    private void Awake()
    {
        // Simple singleton: allows calling SDSShaker.Instance.Shake(...)
        Instance = this;
    }

    /// <summary>
    /// One independent "Shake simulator".
    /// It simulates a 1D spring-damper (position + velocity),
    /// then projects it into 2D using a direction vector.
    /// </summary>
    [System.Serializable]
    private class SDSProcessor
    {
        // Tuning parameters:
        public float Spring;     // how strong it pulls back to 0 (higher = faster oscillations)
        public float Damper;     // how strong it slows down velocity (higher = stops quicker)
        public float Shake;      // impulse value when triggered
        public Vector2 Direction; // where the shake goes in 2D

        // Dynamic state:
        private float cursor = 0f;       // position (offset) along the 1D axis
        private float currentShake = 0f; // velocity along the 1D axis

        public SDSProcessor(float spring, float damper, float shake, Vector2 direction)
        {
            Spring = spring;
            Damper = damper;
            Shake = shake;
            Direction = direction;
        }

        /// <summary>
        /// Adds an impulse to the system.
        /// In physics terms: we add to the velocity to "kick" the motion.
        /// </summary>
        public void ReloadShake(float shake)
        {
            currentShake += shake;
        }

        /// <summary>
        /// Simulates ONE frame of a spring-damper system.
        /// Math (beginner-friendly):
        /// - spring force  = Spring * position
        /// - damper force  = Damper * velocity
        /// - acceleration  = -(spring force + damper force)
        /// - velocity     += acceleration * dt
        /// - position     += velocity * dt
        /// </summary>
        public Vector2 Process()
        {
            // If velocity is almost zero, we consider the shake finished.
            if (Mathf.Abs(currentShake) <= 0.0001f)
            {
                currentShake = 0f;
                cursor = 0f;
                return Vector2.zero;
            }

            // Forces from spring and damper
            float springForce = Spring * cursor;        // pulls back toward center
            float damperForce = Damper * currentShake;  // resists motion (friction)

            // Acceleration points in the opposite direction of the forces.
            float acceleration = -(springForce + damperForce);

            // Integrate (Euler method): update velocity then position
            currentShake += acceleration * Time.deltaTime; // velocity
            cursor += currentShake * Time.deltaTime;       // position

            // Convert the 1D offset into a 2D offset
            return cursor * Direction;
        }
    }

    // All active shake processors
    private readonly List<SDSProcessor> processors = new List<SDSProcessor>();

    private void Update()
    {
        Vector2 shaking = Vector2.zero;
        List<SDSProcessor> toRemove = new List<SDSProcessor>();

        foreach (SDSProcessor processor in processors)
        {
            // IMPORTANT: call Process() once per frame per processor
            Vector2 offset = processor.Process();

            // If this processor has basically stopped, we remove it
            if (offset.magnitude <= 0.00001f)
                toRemove.Add(processor);
            else
                shaking += offset; // add its contribution to the final shake
        }

        // Remove finished processors
        processors.RemoveAll(x => toRemove.Contains(x));

        // Apply the final shake to the transform (keep Z unchanged)
        transform.localPosition = new Vector3(shaking.x, shaking.y, transform.localPosition.z);
    }

    /// <summary>
    /// Public API: trigger a shake.
    /// - spring/damper define the "feel"
    /// - shake is the impulse strength
    /// - direction defines the 2D direction of the motion
    /// </summary>
    [Button]
    public void Shake(float spring, float damper, float shake, Vector2 direction)
    {
        SDSProcessor chosenProc = null;

        // Optional player setting scaling
        shake *= PlayerPrefs.GetFloat("Shake", 5f) / 5f;

        // Try to reuse an existing processor with the same parameters
        foreach (SDSProcessor processor in processors)
        {
            if (processor.Spring == spring &&
                processor.Damper == damper &&
                processor.Shake == shake &&
                processor.Direction == direction)
            {
                chosenProc = processor;
                break;
            }
        }

        // If none found, create a new one
        if (chosenProc == null)
        {
            chosenProc = new SDSProcessor(spring, damper, shake, direction);
            processors.Add(chosenProc);
        }

        // Kick it (add impulse to velocity)
        chosenProc.ReloadShake(shake);
    }
}
