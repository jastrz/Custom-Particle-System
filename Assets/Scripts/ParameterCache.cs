
using System;
using UnityEngine;

namespace CPS
{
    // Class used for holding simulation data and binding ui to particle system
    [Serializable]
    public class ParameterCache
    {
        [Header("Simulation")]
        public int simulationsPerFrame = 3;
        public float gravity = 0f;
        public float friction = 0f;
        public float maxParticleSpeed = 8f;
        public float particleRenderSize = 8f;
        public Vector2 displacementRange = new Vector2(1, 10);
        public Vector2 dampingRange = new Vector2(0.5f, 0.9f);

        [Header("Interaction")]
        public int addAmount = 100;
        public int removeRadius = 10;
        public int attractionRadius = 350;
        public float attractionStrength = 50000f;
        public float attractionMultiplier = 1f;

        [Header("Colors")]
        public Color[] particleColors = new Color[]
        {
            new Color(1f, 0f, 0.1f, 1f),
            new Color(1f, 1f, 1f, 1f)
        };

        public ParameterCache() { }

        public ParameterCache(Particle2DSystem particleSystem)
        {
            if (particleSystem.colors != null && particleSystem.colors.Length > 0)
            {
                particleColors = new Color[particleSystem.colors.Length];
                System.Array.Copy(particleSystem.colors, particleColors, particleSystem.colors.Length);
            }
        }

        public void ApplyToParticleSystem(Particle2DSystem particleSystem)
        {
            // Apply cached parameters to the particle system, for now through reflection

            try
            {
                var type = typeof(Particle2DSystem);

                type.GetField("simulationsPerFrame", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(particleSystem, simulationsPerFrame);
                type.GetField("gravity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(particleSystem, gravity);
                type.GetField("friction", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(particleSystem, friction);
                type.GetField("maxParticleSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(particleSystem, maxParticleSpeed);
                type.GetField("displacementRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(particleSystem, displacementRange);
                type.GetField("dampingRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(particleSystem, dampingRange);
                type.GetField("addAmount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(particleSystem, addAmount);
                type.GetField("removeRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(particleSystem, removeRadius);
                type.GetField("attractionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(particleSystem, attractionRadius);
                type.GetField("attractionStrength", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(particleSystem, attractionStrength);
                type.GetField("attractionMultiplier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(particleSystem, attractionMultiplier);

                particleSystem.SetParticleSize(particleRenderSize);
                particleSystem.UpdateParticleColors(particleColors);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Could not apply some parameters: " + e.Message);
            }
        }
    }
}