using System.Collections.Generic;
using UnityEngine;

namespace CPS
{
    public static class TextureAttractorMapper
    {
        /// <summary>
        /// Maps a Texture2D to a list of AttractorData where non-black pixels become attractors.
        /// The attraction force is proportional to the brightness of the pixel.
        /// </summary>
        /// <param name="texture">The input texture to process</param>
        /// <param name="forceMultiplier">Multiplier for the attraction force (default: 1.0f)</param>
        /// <param name="useAlpha">Whether to include alpha channel in force calculation (default: false)</param>
        /// <returns>List of AttractorData structures</returns>
        public static List<AttractorData> MapTextureToAttractors(Texture2D texture, Vector2 scaler, float forceMultiplier = 1.0f, bool useAlpha = false)
        {
            if (texture == null)
            {
                Debug.LogError("Texture is null!");
                return new List<AttractorData>();
            }

            // Ensure texture is readable
            if (!texture.isReadable)
            {
                Debug.LogError("Texture is not readable! Please enable Read/Write in import settings.");
                return new List<AttractorData>();
            }

            List<AttractorData> attractors = new List<AttractorData>();
            Color[] pixels = texture.GetPixels();

            int width = texture.width;
            int height = texture.height;

            // Slow af - change if will be needed
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    Color pixel = pixels[index];

                    // Check if pixel is not black
                    if (pixel.r > 0f || pixel.g > 0f || pixel.b > 0f)
                    {
                        // Calculate attraction force based on pixel brightness
                        float brightness = useAlpha ?
                            (pixel.r + pixel.g + pixel.b) * pixel.a / 3f :
                            (pixel.r + pixel.g + pixel.b) / 3f;

                        // Skip if brightness is effectively zero
                        if (brightness > 0.001f)
                        {
                            AttractorData attractor = new AttractorData
                            {
                                // Convert pixel coordinates to normalized position (0-1 range)
                                position = new Vector2(x * scaler.x, y * scaler.y),

                                // attractionForce = brightness * forceMultiplier
                                attractionForce = forceMultiplier
                            };

                            attractors.Add(attractor);
                        }
                    }
                }
            }

            return attractors;
        }

        /// <summary>
        /// Maps a Texture2D to attractors using a specific color channel for force calculation.
        /// </summary>
        /// <param name="texture">The input texture to process</param>
        /// <param name="channel">Which color channel to use (0=Red, 1=Green, 2=Blue, 3=Alpha)</param>
        /// <param name="forceMultiplier">Multiplier for the attraction force</param>
        /// <returns>List of AttractorData structures</returns>
        public static List<AttractorData> MapTextureToAttractorsByChannel(Texture2D texture, int channel = 0, float forceMultiplier = 1.0f)
        {
            if (texture == null)
            {
                Debug.LogError("Texture is null!");
                return new List<AttractorData>();
            }

            if (!texture.isReadable)
            {
                Debug.LogError("Texture is not readable! Please enable Read/Write in import settings.");
                return new List<AttractorData>();
            }

            List<AttractorData> attractors = new List<AttractorData>();
            Color[] pixels = texture.GetPixels();

            int width = texture.width;
            int height = texture.height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    Color pixel = pixels[index];

                    // Get the specified channel value
                    float channelValue = channel switch
                    {
                        0 => pixel.r,
                        1 => pixel.g,
                        2 => pixel.b,
                        3 => pixel.a,
                        _ => pixel.r
                    };

                    // Skip black/transparent pixels
                    if (channelValue > 0.001f)
                    {
                        AttractorData attractor = new AttractorData
                        {
                            position = new Vector2((float)x / width, (float)y / height),
                            attractionForce = channelValue * forceMultiplier
                        };

                        attractors.Add(attractor);
                    }
                }
            }

            return attractors;
        }

        /// <summary>
        /// Maps a Texture2D to attractors with world space coordinates instead of normalized coordinates.
        /// </summary>
        /// <param name="texture">The input texture to process</param>
        /// <param name="worldSize">The world space size the texture should map to</param>
        /// <param name="worldOffset">Offset in world space (default: Vector2.zero)</param>
        /// <param name="forceMultiplier">Multiplier for the attraction force</param>
        /// <returns>List of AttractorData structures with world space positions</returns>
        public static List<AttractorData> MapTextureToAttractorsWorldSpace(Texture2D texture, Vector2 worldSize, Vector2 worldOffset = default, float forceMultiplier = 1.0f)
        {
            List<AttractorData> attractors = MapTextureToAttractors(texture, Vector2.one, forceMultiplier);

            // Convert normalized coordinates to world space
            for (int i = 0; i < attractors.Count; i++)
            {
                AttractorData attractor = attractors[i];
                attractor.position = new Vector2(
                    attractor.position.x * worldSize.x + worldOffset.x,
                    attractor.position.y * worldSize.y + worldOffset.y
                );
                attractors[i] = attractor;
            }

            return attractors;
        }
    }

    public class AttractorMapperExample : MonoBehaviour
    {
        [SerializeField] private Texture2D sourceTexture;
        [SerializeField] private float forceMultiplier = 1.0f;
        [SerializeField] private Vector2 worldSize = new Vector2(10f, 10f);

        private List<AttractorData> attractors;

        void Start()
        {
            if (sourceTexture != null)
            {
                // Basic mapping with normalized coordinates
                attractors = TextureAttractorMapper.MapTextureToAttractors(sourceTexture, Vector2.one, forceMultiplier);

                // Or with world space coordinates
                // attractors = TextureAttractorMapper.MapTextureToAttractorsWorldSpace(sourceTexture, worldSize, Vector2.zero, forceMultiplier);

                // Or using a specific color channel
                // attractors = TextureAttractorMapper.MapTextureToAttractorsByChannel(sourceTexture, 0, forceMultiplier); // Red channel

                Debug.Log($"Generated {attractors.Count} attractors from texture");
            }
        }

        // Visualize attractors in Scene view
        void OnDrawGizmos()
        {
            if (attractors != null)
            {
                foreach (var attractor in attractors)
                {
                    Gizmos.color = Color.Lerp(Color.white, Color.red, attractor.attractionForce);
                    Vector3 worldPos = new Vector3(attractor.position.x * worldSize.x, attractor.position.y * worldSize.y, 0);
                    Gizmos.DrawWireSphere(worldPos, 0.1f);
                }
            }
        }
    }
}

