using System;
using System.Collections.Generic;
using UnityEngine;

namespace CPS
{
    [Serializable]
    public struct AttractorData
    {
        public Vector2 position;
        public float attractionForce;
    }

    public class Particle2DSystem : MonoBehaviour
    {
        [Header("Grid Settings")]
        public int gridWidth = 1024;
        public int gridHeight = 512;
        [SerializeField] private float cellSize = 0.1f;

        [Header("Rendering & Background")]
        public Color[] colors;
        public Color boundsColor = Color.yellow;
        [SerializeField] private Material particleMaterial;


        [Header("Simulation")]
        [Range(1, 500)]
        [SerializeField] private int simulationsPerFrame = 3;
        [SerializeField] private float gravity = 1f;
        [SerializeField] private float friction = 0.3f;
        [SerializeField] private float maxParticleSpeed = 15.0f;
        [SerializeField] private Vector2 displacementRange = new Vector2(1, 10);
        [SerializeField] private Vector2 dampingRange = new Vector2(.5f, 0.9f);

        [Header("Interaction")]
        [SerializeField] private int removeRadius = 10;
        [SerializeField] private int addAmount = 100;
        [SerializeField] private int attractionRadius = 100;
        [SerializeField] private float attractionStrength = 1;
        [SerializeField] private float attractionMultiplier = 1.0f;
        [SerializeField] private bool useAttractorTexture = true;
        [SerializeField] private Texture2D attractorsTexture;
        [SerializeField] private ComputeShader simulationShader;

        [Header("Rendering")]


        [Header("Input")]
        public bool inputEnabled = true;

        // Texture management
        private TextureSet currentTextures;
        private TextureSet nextTextures;
        private RenderTexture sandPositionDisplay;
        private RenderTexture sandColorDisplay;

        // Compute shader kernels
        private ComputeKernels kernels;

        // State management
        private int frameNum = 0;
        private bool proceed = false;
        private bool isStepping = false;

        // Resources
        public List<AttractorData> attractors = new List<AttractorData>();
        private Material clearMaterial;
        private LineRenderer boundsRenderer;
        private ComputeBuffer attractorBuffer;
        private ComputeBuffer colorBuffer;

        private const float DEFAULT_ATTRACTOR_STRENGTH = 15000;
        private const int DISPATCH_SIZE = 8;

        #region Initialization

        private void Start()
        {
            if (!ValidateComponents()) return;

            InitializeResources();
            InitializeTextures();
            InitializeShaders();
            InitializeAttractors();
            InitializeParticles();
            SetupCamera();
            CreateBoundsRenderer();

            Application.targetFrameRate = 60;
        }

        private bool ValidateComponents()
        {
            if (simulationShader == null)
            {
                Debug.LogError("Simulation shader is not assigned!");
                return false;
            }

            if (particleMaterial == null)
            {
                Debug.LogError("Particle material is not assigned!");
                return false;
            }

            return true;
        }

        private void InitializeResources()
        {
            clearMaterial = new Material(Shader.Find("Hidden/ClearTexture"));
            kernels = new ComputeKernels(simulationShader);
        }

        private void InitializeTextures()
        {
            currentTextures = new TextureSet(gridWidth, gridHeight);
            nextTextures = new TextureSet(gridWidth, gridHeight);

            sandPositionDisplay = CreateRenderTexture(RenderTextureFormat.ARGBFloat);
            sandColorDisplay = CreateRenderTexture(RenderTextureFormat.ARGB32);
        }

        private RenderTexture CreateRenderTexture(RenderTextureFormat format)
        {
            var rt = new RenderTexture(gridWidth, gridHeight, 0, format)
            {
                enableRandomWrite = true,
                useMipMap = false,
                filterMode = FilterMode.Point
            };
            rt.Create();
            return rt;
        }

        private void InitializeShaders()
        {
            SetupKernelTextures();
            SetSimulationParameters();
            SetMaterialProperties();
        }

        private void SetupKernelTextures()
        {
            // Set textures for each kernel
            kernels.SetTexturesForInit(nextTextures);
            kernels.SetTexturesForSimulate(currentTextures, nextTextures);
            kernels.SetTexturesForAdd(nextTextures);
            kernels.SetTexturesForRemove(nextTextures);
            kernels.SetTexturesForAttract(currentTextures, nextTextures);
            kernels.SetTexturesForAttractFromData(currentTextures, nextTextures);
        }

        private void SetSimulationParameters()
        {
            simulationShader.SetFloat("Gravity", gravity);
            simulationShader.SetFloat("Friction", friction);
            simulationShader.SetInt("GridWidth", gridWidth);
            simulationShader.SetInt("GridHeight", gridHeight);
            simulationShader.SetFloat("MaxSpeed", maxParticleSpeed);
            simulationShader.SetVector("DisplacementRange", displacementRange);
            simulationShader.SetVector("DampingRange", dampingRange);
        }

        private void SetMaterialProperties()
        {
            particleMaterial.SetTexture("_MainTex", sandPositionDisplay);
            particleMaterial.SetTexture("_ColorTex", sandColorDisplay);
            particleMaterial.SetFloat("_CellSize", cellSize);
        }

        private void InitializeAttractors()
        {
            // Initialize attractors from a texture if existent 
            if (useAttractorTexture && attractorsTexture != null)
            {
                var scaler = new Vector2(
                    (float)gridWidth / attractorsTexture.width,
                    (float)gridHeight / attractorsTexture.height
                );
                attractors = TextureAttractorMapper.MapTextureToAttractors(
                    attractorsTexture, scaler, DEFAULT_ATTRACTOR_STRENGTH
                );
            }

            // Initialize buffer (will be recreated as needed in UpdateAttractors)
            if (attractors.Count > 0)
            {
                attractorBuffer = new ComputeBuffer(attractors.Count, sizeof(float) * 3);
                attractorBuffer.SetData(attractors.ToArray());
                simulationShader.SetBuffer(kernels.AttractFromData, "Attractors", attractorBuffer);
            }
        }

        private void InitializeParticles()
        {
            SetupColorBuffer();

            simulationShader.SetFloat("Time", GetRandomSeed());

            DispatchKernel(kernels.Init);
        }

        private void SetupColorBuffer()
        {
            Vector4[] colorArray = GetColorArray();

            colorBuffer = new ComputeBuffer(colorArray.Length, sizeof(float) * 4);
            colorBuffer.SetData(colorArray);

            simulationShader.SetBuffer(kernels.Init, "Colors", colorBuffer);
            simulationShader.SetBuffer(kernels.Simulate, "Colors", colorBuffer);
            simulationShader.SetInt("ColorCount", colorArray.Length);
        }

        private Vector4[] GetColorArray()
        {
            if (colors != null && colors.Length > 0)
            {
                var colorArray = new Vector4[colors.Length];

                for (int i = 0; i < colors.Length; i++)
                {
                    colorArray[i] = colors[i];
                }

                return colorArray;
            }

            // Default colors
            return new Vector4[]
            {
                new Vector4(1f, 0f, 0.1f, 1f),
                new Vector4(1f, 1f, 1f, 1f),
            };
        }

        private float GetRandomSeed()
        {
            return (float)DateTime.UtcNow.Millisecond;
        }

        #endregion

        #region Update Loop

        private void Update()
        {
            UpdateAttractors();
            HandleSteppingControls();

            if (!ShouldSimulate()) return;

            RunSimulation();
            UpdateFrameCounter();
            UpdateShaderParameters();
        }

        private void UpdateAttractors()
        {
            if (attractors.Count == 0)
            {
                // Clean up buffer if no attractors
                if (attractorBuffer != null)
                {
                    attractorBuffer.Release();
                    attractorBuffer = null;
                }
                return;
            }

            // Check if buffer needs to be recreated (size changed)
            if (attractorBuffer == null || attractorBuffer.count != attractors.Count)
            {
                // Release old buffer
                if (attractorBuffer != null)
                {
                    attractorBuffer.Release();
                }

                // Create new buffer with correct size
                attractorBuffer = new ComputeBuffer(attractors.Count, sizeof(float) * 3);
                simulationShader.SetBuffer(kernels.AttractFromData, "Attractors", attractorBuffer);
            }

            // Update buffer data
            attractorBuffer.SetData(attractors.ToArray());
        }

        private void HandleSteppingControls()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                proceed = true;

            if (Input.GetKeyDown(KeyCode.S))
                isStepping = !isStepping;
        }

        private bool ShouldSimulate()
        {
            return proceed || !isStepping;
        }

        private void RunSimulation()
        {
            for (int i = 0; i < simulationsPerFrame; i++)
            {
                // if (inputEnabled)
                // {
                //     HandleInput();
                // }

                HandleInput();

                if (attractors.Count > 0)
                {
                    DispatchKernel(kernels.AttractFromData);
                }

                SwapTextures();
                DispatchKernel(kernels.Simulate);

                simulationShader.SetFloat("FrameNum", frameNum * (i + 1));
            }

            proceed = false;
        }

        private void UpdateFrameCounter()
        {
            frameNum++;
        }

        private void UpdateShaderParameters()
        {
            simulationShader.SetFloat("DeltaTime", Time.deltaTime);
            simulationShader.SetFloat("Gravity", gravity);
            simulationShader.SetFloat("Friction", friction);
            simulationShader.SetFloat("MaxSpeed", maxParticleSpeed);
            simulationShader.SetVector("DisplacementRange", displacementRange);
            simulationShader.SetVector("DampingRange", dampingRange);
            simulationShader.SetFloat("Time", GetRandomSeed());
            simulationShader.SetFloat("AttractionMultiplier", attractionMultiplier);
            simulationShader.SetInt("AttractorsCount", attractors.Count);
            simulationShader.SetFloat("AttractionRadius", attractionRadius);
        }

        #endregion

        #region Input Handling

        private void HandleInput()
        {
            Vector2 gridPosition = ScreenToGridPosition(Input.mousePosition);

            if (Input.GetMouseButton(2))
                AddParticle(gridPosition, addAmount);

            if (Input.GetKey(KeyCode.W))
                AddParticle(gridPosition, addAmount, 2, 1);

            if (Input.GetMouseButton(1))
                RemoveParticle(gridPosition, removeRadius);

            if (Input.GetKey(KeyCode.Q))
                Attract(gridPosition, attractionRadius, attractionStrength);
        }

        public Vector2 ScreenToGridPosition(Vector2 screenPosition)
        {
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, 0)
            );

            Vector3 simulationBottomLeft = transform.position -
                new Vector3(gridWidth * cellSize * 0.5f, gridHeight * cellSize * 0.5f, 0);

            Vector2 relativePosition = worldPosition - simulationBottomLeft;

            return new Vector2(
                Mathf.Clamp(relativePosition.x / cellSize, 0, gridWidth - 1),
                Mathf.Clamp(relativePosition.y / cellSize, 0, gridHeight - 1)
            );
        }

        #endregion

        #region Particle Operations

        private void AddParticle(Vector2 position, int amount, int type = 1, int addMethod = 0)
        {
            simulationShader.SetVector("AddPosition", position);
            simulationShader.SetInt("AddAmount", amount);
            simulationShader.SetInt("AddType", type);
            simulationShader.SetInt("AddMethod", addMethod);
            simulationShader.SetInt("AddRadius", 5);

            simulationShader.Dispatch(kernels.Add, 1, 1, 1);
        }

        public void RemoveParticle(Vector2 position, float radius)
        {
            simulationShader.SetVector("RemovePosition", position);
            simulationShader.SetFloat("RemoveRadius", radius);

            simulationShader.Dispatch(kernels.Remove, 1, 1, 1);
        }

        public void Attract(Vector2 position, float radius, float strength)
        {
            simulationShader.SetVector("AttractPosition", position);
            simulationShader.SetFloat("AttractionRadius", radius);
            simulationShader.SetFloat("AttractionStrength", strength);

            DispatchKernel(kernels.Attract);
        }

        #endregion

        #region Texture Management

        private void SwapTextures()
        {
            (currentTextures, nextTextures) = (nextTextures, currentTextures);
            ResetTextures();
            UpdateShaderTextureReferences();
            UpdateMaterialTextures();
        }

        private void ResetTextures()
        {
            Graphics.Blit(null, nextTextures.Position, clearMaterial, 0);
            Graphics.Blit(null, nextTextures.Velocity, clearMaterial, 0);
            Graphics.Blit(null, nextTextures.Occupancy, clearMaterial, 1);
        }

        private void UpdateShaderTextureReferences()
        {
            kernels.SetTexturesForSimulate(currentTextures, nextTextures);
        }

        private void UpdateMaterialTextures()
        {
            particleMaterial.SetTexture("_MainTex", currentTextures.Position);
            particleMaterial.SetTexture("_ColorTex", currentTextures.Color);
        }

        #endregion

        #region Utility Methods

        public void SetParticleSize(float size)
        {
            particleMaterial.SetFloat("_Size", size);
        }

        private void DispatchKernel(int kernelId)
        {
            simulationShader.Dispatch(
                kernelId,
                Mathf.CeilToInt((float)gridWidth / DISPATCH_SIZE),
                Mathf.CeilToInt((float)gridHeight / DISPATCH_SIZE),
                1
            );
        }

        public Vector2 WorldToGridPosition(Vector2 worldPos)
        {
            Vector3 localPos = transform.InverseTransformPoint(new Vector3(worldPos.x, worldPos.y, 0));

            float gridCenterX = gridWidth * 0.5f * cellSize;
            float gridCenterY = gridHeight * 0.5f * cellSize;

            return new Vector2(
                (localPos.x + gridCenterX) / cellSize,
                (localPos.y + gridCenterY) / cellSize
            );
        }

        public Vector2 GridToWorldPosition(Vector2 gridPos)
        {
            float gridCenterX = gridWidth * 0.5f * cellSize;
            float gridCenterY = gridHeight * 0.5f * cellSize;

            Vector3 localPos = new Vector3(
                gridPos.x * cellSize - gridCenterX,
                gridPos.y * cellSize - gridCenterY,
                0
            );

            Vector3 worldPos = transform.TransformPoint(localPos);
            return new Vector2(worldPos.x, worldPos.y);
        }

        #endregion

        #region Camera and Rendering

        public void UpdateBackgroundColor(Color color)
        {
            Camera.main.backgroundColor = color;
        }
        public void UpdateParticleColors(Color[] newColors)
        {
            if (newColors == null || newColors.Length == 0) return;

            colors = new Color[newColors.Length];
            System.Array.Copy(newColors, colors, newColors.Length);

            // Update the color buffer
            Vector4[] colorArray = GetColorArray();

            if (colorBuffer != null)
            {
                colorBuffer.Release();
            }

            colorBuffer = new ComputeBuffer(colorArray.Length, sizeof(float) * 4);
            colorBuffer.SetData(colorArray);

            // Update shader buffers
            simulationShader.SetBuffer(kernels.Init, "Colors", colorBuffer);
            simulationShader.SetBuffer(kernels.Simulate, "Colors", colorBuffer);
            simulationShader.SetInt("ColorCount", colorArray.Length);
        }

        public void UpdateBoundsColor(Color newBoundsColor)
        {
            boundsColor = newBoundsColor;
            if (boundsRenderer != null)
            {
                boundsRenderer.startColor = boundsColor;
                boundsRenderer.endColor = boundsColor;
            }
        }

        private void SetupCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("Main camera not found!");
                return;
            }

            float worldWidth = gridWidth * cellSize;
            float worldHeight = gridHeight * cellSize;

            mainCamera.transform.position = new Vector3(transform.position.x, transform.position.y, -10f);

            float screenRatio = (float)Screen.width / Screen.height;
            float targetRatio = worldWidth / worldHeight;

            if (screenRatio >= targetRatio)
            {
                mainCamera.orthographicSize = worldHeight / 2;
            }
            else
            {
                float differenceInSize = targetRatio / screenRatio;
                mainCamera.orthographicSize = worldHeight / 2 * differenceInSize;
            }

            mainCamera.orthographic = true;
        }

        private void CreateBoundsRenderer()
        {
            boundsRenderer = gameObject.AddComponent<LineRenderer>();
            boundsRenderer.positionCount = 5;
            boundsRenderer.useWorldSpace = false;
            boundsRenderer.startWidth = 1f;
            boundsRenderer.endWidth = 1f;
            boundsRenderer.material = new Material(Shader.Find("Sprites/Default"));

            UpdateBoundsRenderer();
        }

        private void UpdateBoundsRenderer()
        {
            float halfWidth = gridWidth * cellSize * 0.5f;
            float halfHeight = gridHeight * cellSize * 0.5f;

            Vector3[] positions = new Vector3[5]
            {
                new Vector3(-halfWidth, -halfHeight, 0),
                new Vector3(halfWidth, -halfHeight, 0),
                new Vector3(halfWidth, halfHeight, 0),
                new Vector3(-halfWidth, halfHeight, 0),
                new Vector3(-halfWidth, -halfHeight, 0)
            };

            boundsRenderer.startColor = boundsColor;
            boundsRenderer.endColor = boundsColor;

            boundsRenderer.SetPositions(positions);
        }

        private void OnRenderObject()
        {
            if (particleMaterial != null)
            {
                particleMaterial.SetPass(0);
                Graphics.DrawProceduralNow(MeshTopology.Triangles, 6, gridWidth * gridHeight);
            }
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            CleanupTextures();
            CleanupBuffers();
            CleanupMaterials();
        }

        private void CleanupTextures()
        {
            currentTextures?.Dispose();
            nextTextures?.Dispose();

            if (sandPositionDisplay != null)
            {
                sandPositionDisplay.Release();
                sandPositionDisplay = null;
            }

            if (sandColorDisplay != null)
            {
                sandColorDisplay.Release();
                sandColorDisplay = null;
            }
        }

        private void CleanupBuffers()
        {
            if (attractorBuffer != null)
            {
                attractorBuffer.Release();
                attractorBuffer = null;
            }

            if (colorBuffer != null)
            {
                colorBuffer.Release();
                colorBuffer = null;
            }
        }

        private void CleanupMaterials()
        {
            if (clearMaterial != null)
            {
                DestroyImmediate(clearMaterial);
                clearMaterial = null;
            }
        }

        #endregion
    }
}

