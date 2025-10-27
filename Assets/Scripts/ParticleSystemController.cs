using UnityEngine;
using UnityEngine.SceneManagement;

namespace CPS
{
    // Basic UI to control particle system
    public class ParticleSystemController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Particle2DSystem particleSystem;

        [Header("GUI Settings")]
        [SerializeField] private bool showGUI = true;
        [SerializeField] private GUISkin guiSkin;
        [SerializeField] private float guiWidth = 300f;
        [SerializeField] private float spacing = 10f;

        // GUI State
        private bool showSimulationParams = true;
        private bool showInteractionParams = true;
        private bool showAttractorEditor = true;
        private bool showControls = true;
        private Vector2 scrollPosition;

        // Attractor Management
        private int selectedAttractorIndex = -1;
        private bool isDragging = false;
        private bool showColorParams = true;
        private AttractorData newAttractor;

        // Parameter Caches
        [SerializeField] private ParameterCache paramCache;

        // GUI Styles
        private GUIStyle headerStyle;
        private GUIStyle boxStyle;
        private GUIStyle buttonStyle;
        private GUIStyle toggleStyle;
        private bool stylesInitialized = false;

        private const float MAX_ATTRACTION = 100000f;
        private const float MIN_ATTRACTION = -100000f;
        private const float SCROLL_DELTA = 20000f;

        private void Start()
        {
            if (particleSystem == null)
            {
                particleSystem = FindAnyObjectByType<Particle2DSystem>();
                if (particleSystem == null)
                {
                    Debug.LogError("No Particle2DSystem found in scene!");
                    enabled = false;
                    return;
                }
            }

            paramCache = new ParameterCache(particleSystem);
        }

        private void Update()
        {
            HandleAttractorInteraction();
            UpdateParametersFromCache();
        }

        private void InitializeStyles()
        {
            if (stylesInitialized) return;

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 5, 5),

            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                padding = new RectOffset(10, 10, 5, 5)
            };

            toggleStyle = new GUIStyle(GUI.skin.toggle)
            {
                fontSize = 12
            };

            stylesInitialized = true;

        }

        #region GUI Drawing

        private void OnGUI()
        {
            if (particleSystem == null) return;

            if (!showGUI)
            {
                // particleSystem.inputEnabled = true;
                if (GUILayout.Button("ShowUI"))
                {
                    showGUI = true;
                }
                return;
            }

            particleSystem.inputEnabled = false;

            if (guiSkin != null) GUI.skin = guiSkin;
            InitializeStyles();

            Rect guiRect = new Rect(10, 10, guiWidth, Screen.height - 20);

            GUILayout.BeginArea(guiRect, boxStyle);
            GUI.backgroundColor = Color.grey;

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            DrawSimulationParameters();
            DrawInteractionParameters();
            DrawAttractorEditor();
            DrawControls();
            DrawColorParameters();
            DrawInstructions();

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            DrawAttractorGizmos();
        }

        private void DrawHeader()
        {
            GUILayout.Label("Particle System Control", headerStyle);
            GUILayout.Space(spacing);

            showGUI = GUILayout.Toggle(showGUI, "Show GUI", toggleStyle);
            GUILayout.Space(spacing);
        }

        private void DrawSimulationParameters()
        {
            showSimulationParams = DrawFoldout("Simulation Parameters", showSimulationParams);
            if (!showSimulationParams) return;

            GUILayout.BeginVertical(boxStyle);

            paramCache.simulationsPerFrame = (int)DrawSlider("Simulations/Frame", (float)paramCache.simulationsPerFrame, 1, 10);
            paramCache.gravity = DrawSlider("Gravity", paramCache.gravity, -50f, 50f);
            paramCache.friction = DrawSlider("Friction", paramCache.friction, 0f, 5f);
            paramCache.maxParticleSpeed = DrawSlider("Max Speed", paramCache.maxParticleSpeed, 1f, 50f);

            GUILayout.Label("Displacement Range:");
            paramCache.displacementRange = DrawVector2Slider(paramCache.displacementRange, 0f, 20f);

            GUILayout.Label("Damping Range:");
            paramCache.dampingRange = DrawVector2Slider(paramCache.dampingRange, 0f, 10f);

            GUILayout.Label("Particle render size:");
            paramCache.particleRenderSize = DrawSlider("Render Size", paramCache.particleRenderSize, 1f, 15f);

            GUILayout.EndVertical();
            GUILayout.Space(spacing);
        }

        private void DrawInteractionParameters()
        {
            showInteractionParams = DrawFoldout("Interaction Parameters", showInteractionParams);
            if (!showInteractionParams) return;

            GUILayout.BeginVertical(boxStyle);

            paramCache.addAmount = (int)DrawSlider("Add Amount", paramCache.addAmount, 1, 500);
            paramCache.removeRadius = (int)DrawSlider("Remove Radius", paramCache.removeRadius, 1, 100);
            paramCache.attractionRadius = (int)DrawSlider("Attraction Radius", paramCache.attractionRadius, 10, 2000);
            paramCache.attractionStrength = DrawSlider("Attraction Strength", paramCache.attractionStrength, MIN_ATTRACTION, MAX_ATTRACTION);
            paramCache.attractionMultiplier = DrawSlider("Attraction Multiplier", paramCache.attractionMultiplier, -5f, 5f);

            GUILayout.EndVertical();
            GUILayout.Space(spacing);
        }

        private void DrawAttractorEditor()
        {
            showAttractorEditor = DrawFoldout("Attractor Editor", showAttractorEditor);

            if (!showAttractorEditor) return;

            GUILayout.BeginVertical(boxStyle);

            GUILayout.Label($"Attractors: {particleSystem.attractors.Count}", headerStyle);

            // Attractor creation mode
            newAttractor.attractionForce = DrawSlider("New Attractor Force", newAttractor.attractionForce, -100000f, 100000f);

            GUILayout.Space(spacing / 2);

            // Attractor list
            for (int i = 0; i < particleSystem.attractors.Count; i++)
            {
                DrawAttractorItem(i);
            }

            GUILayout.Space(spacing / 2);

            // Attractor controls
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear All", buttonStyle))
            {
                particleSystem.attractors.Clear();
                selectedAttractorIndex = -1;
            }

            if (GUILayout.Button("Random Pattern", buttonStyle))
            {
                GenerateRandomAttractors(10);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.Space(spacing);
        }

        private void DrawAttractorItem(int index)
        {
            AttractorData attractor = particleSystem.attractors[index];
            bool isSelected = selectedAttractorIndex == index;

            Color originalColor = GUI.backgroundColor;
            if (isSelected) GUI.backgroundColor = Color.cyan;

            GUILayout.BeginVertical(boxStyle);

            GUILayout.BeginHorizontal();

            string buttonText = $"#{index} ({attractor.position.x:F0}, {attractor.position.y:F0})";
            if (GUILayout.Button(buttonText, buttonStyle))
            {
                selectedAttractorIndex = isSelected ? -1 : index;
            }

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                RemoveAttractor(index);
            }

            GUILayout.EndHorizontal();

            if (isSelected)
            {
                attractor.attractionForce = DrawSlider("Force", attractor.attractionForce, MIN_ATTRACTION, MAX_ATTRACTION);

                GUILayout.BeginHorizontal();
                GUILayout.Label($"Pos: ({attractor.position.x:F1}, {attractor.position.y:F1})");
                if (GUILayout.Button("Center", GUILayout.Width(60)))
                {
                    attractor.position = new Vector2(
                        particleSystem.gridWidth / 2f,
                        particleSystem.gridHeight / 2f
                    );
                }
                GUILayout.EndHorizontal();

                particleSystem.attractors[index] = attractor;
            }

            GUILayout.EndVertical();
            GUI.backgroundColor = originalColor;
        }

        private void RemoveAttractor(int index)
        {
            particleSystem.attractors.RemoveAt(index);
            if (selectedAttractorIndex == index) selectedAttractorIndex = -1;
            else if (selectedAttractorIndex > index) selectedAttractorIndex--;
        }

        private void DrawControls()
        {
            showControls = DrawFoldout("Controls", showControls);
            if (!showControls) return;

            GUILayout.BeginVertical(boxStyle);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset Simulation", buttonStyle))
            {
                ResetSimulation();
            }

            if (GUILayout.Button("Pause/Resume", buttonStyle))
            {
                Time.timeScale = Time.timeScale > 0 ? 0 : 1;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Preset", buttonStyle))
            {
                SaveParameterPreset();
            }

            if (GUILayout.Button("Load Preset", buttonStyle))
            {
                LoadParameterPreset();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.Space(spacing);
        }

        private void DrawColorParameters()
        {
            showColorParams = DrawFoldout("Color Parameters", showColorParams);
            if (!showColorParams) return;

            GUILayout.BeginVertical(boxStyle);

            GUILayout.Label($"Particle Colors ({paramCache.particleColors.Length}):");

            for (int i = 0; i < paramCache.particleColors.Length; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Color {i + 1}:", GUILayout.Width(60));
                paramCache.particleColors[i] = DrawColorField(paramCache.particleColors[i]);
                GUILayout.EndHorizontal();
                GUILayout.Space(15);
            }

            GUILayout.EndVertical();
            GUILayout.Space(spacing);
        }

        private Color DrawColorField(Color color)
        {
            GUILayout.BeginHorizontal();

            // Color preview
            Color oldBgColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
            GUILayout.Box("", GUILayout.Width(30), GUILayout.Height(20));
            GUI.backgroundColor = oldBgColor;

            // RGBA sliders
            GUILayout.BeginVertical();
            color.r = GUILayout.HorizontalSlider(color.r, 0f, 1f);
            color.g = GUILayout.HorizontalSlider(color.g, 0f, 1f);
            color.b = GUILayout.HorizontalSlider(color.b, 0f, 1f);
            color.a = GUILayout.HorizontalSlider(color.a, 0f, 1f);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            return color;
        }

        private void DrawInstructions()
        {
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("Instructions:", headerStyle);

            GUILayout.Label("• Right Mouse Button: Select attractor");
            GUILayout.Label("• Middle Mouse Button: Add particles");
            GUILayout.Label("• Right Mouse Button: Remove particles");
            GUILayout.Label("• A Key: Add another attractor");
            GUILayout.Label("• W Key: Add different particle type (wall)");
            GUILayout.Label("• Q Key: Attract particles to cursor");

            GUILayout.Label("• Space: Step simulation (when paused)");
            GUILayout.Label("• S: Toggle step mode");
            GUILayout.EndVertical();
        }

        #endregion

        #region GUI Helpers

        private bool DrawFoldout(string label, bool foldout)
        {
            GUILayout.BeginHorizontal();
            string arrow = foldout ? "▼" : "▶";
            bool result = GUILayout.Button($"{arrow} {label}", headerStyle, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            return result ? !foldout : foldout;
        }

        private float DrawSlider(string label, float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}: {value:F2}", GUILayout.Width(150));
            float result = GUILayout.HorizontalSlider(value, min, max);
            GUILayout.EndHorizontal();
            return result;
        }

        private Vector2 DrawVector2Slider(Vector2 value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"X: {value.x:F2}", GUILayout.Width(70));
            value.x = GUILayout.HorizontalSlider(value.x, min, max);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Y: {value.y:F2}", GUILayout.Width(70));
            value.y = GUILayout.HorizontalSlider(value.y, min, max);
            GUILayout.EndHorizontal();

            return value;
        }

        #endregion

        #region Attractor Interaction

        private void HandleAttractorInteraction()
        {
            if (!showGUI) return;

            HandleAttractorCreation();
            HandleAttractorSelection();

            if (selectedAttractorIndex >= 0)
            {
                HandleSelectedAttractor();
            }
        }

        private void HandleAttractorCreation()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                Vector2 gridPos = particleSystem.ScreenToGridPosition(Input.mousePosition);
                newAttractor.position = gridPos;

                if (newAttractor.attractionForce == 0)
                    newAttractor.attractionForce = 5000f;

                particleSystem.attractors.Add(newAttractor);
                selectedAttractorIndex = particleSystem.attractors.Count - 1;
            }
        }

        private void HandleAttractorSelection()
        {
            Vector2 mouseGridPos = particleSystem.ScreenToGridPosition(Input.mousePosition);

            // Select nearest attractor
            if (Input.GetMouseButtonDown(0))
            {
                int currentAttractorIndex = FindNearestAttractor(mouseGridPos, 100f);

                if (currentAttractorIndex >= 0)
                {
                    selectedAttractorIndex = currentAttractorIndex;
                }

                isDragging = currentAttractorIndex >= 0;
                particleSystem.inputEnabled = isDragging;
            }
        }

        private void HandleSelectedAttractor()
        {
            Vector2 mouseGridPos = particleSystem.ScreenToGridPosition(Input.mousePosition);

            // Drag selected
            if (isDragging)
            {
                // Adjust attraction
                float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
                if (scrollDelta != 0f)
                {
                    AttractorData attractor = particleSystem.attractors[selectedAttractorIndex];

                    // Adjust attraction force
                    float forceChange = scrollDelta * SCROLL_DELTA;
                    attractor.attractionForce += forceChange;

                    attractor.attractionForce = Mathf.Clamp(attractor.attractionForce, MIN_ATTRACTION, MAX_ATTRACTION);

                    particleSystem.attractors[selectedAttractorIndex] = attractor;
                }

                // Adjust position
                if (Input.GetMouseButton(0))
                {
                    AttractorData attractor = particleSystem.attractors[selectedAttractorIndex];
                    attractor.position = mouseGridPos;
                    particleSystem.attractors[selectedAttractorIndex] = attractor;
                }
                else
                {
                    isDragging = false;
                }
            }

            if (Input.GetKeyDown(KeyCode.Delete))
            {
                RemoveAttractor(selectedAttractorIndex);
            }
        }


        private int FindNearestAttractor(Vector2 position, float maxDistance)
        {
            int nearest = -1;
            float minDistance = maxDistance;

            for (int i = 0; i < particleSystem.attractors.Count; i++)
            {
                float distance = Vector2.Distance(position, particleSystem.attractors[i].position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = i;
                }
            }

            return nearest;
        }

        private void DrawAttractorGizmos()
        {
            if (!showGUI) return;

            // Draw attractors as circles on screen
            for (int i = 0; i < particleSystem.attractors.Count; i++)
            {
                AttractorData attractor = particleSystem.attractors[i];
                Vector2 worldPos = particleSystem.GridToWorldPosition(attractor.position);
                Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
                screenPos.y = Screen.height - screenPos.y; // Flip Y for GUI coordinates

                Color color = attractor.attractionForce > 0 ? Color.green : Color.red;
                if (selectedAttractorIndex == i)
                {
                    color = Color.yellow;
                }

                float radius = Mathf.Clamp(Mathf.Abs(attractor.attractionForce) / 100f, 5f, 20f);

                DrawCircle(screenPos, radius, color);

                // Draw force indicator
                string forceText = attractor.attractionForce.ToString("F0");
                Vector2 labelPos = screenPos + Vector2.up * (radius + 15);

                GUI.color = color;
                GUI.Label(new Rect(labelPos.x - 20, labelPos.y - 10, 60, 20), forceText);
                GUI.color = Color.white;
            }
        }

        private void DrawCircle(Vector2 center, float radius, Color color)
        {
            int segments = 16;
            float angleStep = 2 * Mathf.PI / segments;

            Color oldColor = GUI.color;
            GUI.color = color;

            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep;
                float angle2 = (i + 1) * angleStep;

                Vector2 p1 = center + new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1)) * radius;
                Vector2 p2 = center + new Vector2(Mathf.Cos(angle2), Mathf.Sin(angle2)) * radius;

                DrawLine(p1, p2);
            }

            GUI.color = oldColor;
        }

        private void DrawLine(Vector2 start, Vector2 end)
        {
            Vector2 direction = (end - start).normalized;
            float distance = Vector2.Distance(start, end);

            Matrix4x4 matrix = GUI.matrix;

            GUIUtility.RotateAroundPivot(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, start);
            GUI.DrawTexture(new Rect(start.x, start.y - 0.5f, distance, 1f), Texture2D.whiteTexture);

            GUI.matrix = matrix;
        }

        #endregion

        #region Parameter Management

        private void UpdateParametersFromCache()
        {
            paramCache.ApplyToParticleSystem(particleSystem);
        }

        private void GenerateRandomAttractors(int count)
        {
            particleSystem.attractors.Clear();

            for (int i = 0; i < count; i++)
            {
                AttractorData attractor = new AttractorData
                {
                    position = new Vector2(
                        UnityEngine.Random.Range(50f, particleSystem.gridWidth - 50f),
                        UnityEngine.Random.Range(50f, particleSystem.gridHeight - 50f)
                    ),
                    attractionForce = UnityEngine.Random.Range(-8000f, 8000f)
                };

                particleSystem.attractors.Add(attractor);
            }

            selectedAttractorIndex = -1;
        }

        private void ResetSimulation()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void SaveParameterPreset()
        {
            string json = JsonUtility.ToJson(paramCache, true);
            System.IO.File.WriteAllText(Application.persistentDataPath + "/particle_preset.json", json);
            Debug.Log("Preset saved to: " + Application.persistentDataPath);
        }

        private void LoadParameterPreset()
        {
            string path = Application.persistentDataPath + "/particle_preset.json";
            if (System.IO.File.Exists(path))
            {
                string json = System.IO.File.ReadAllText(path);
                ParameterCache loadedCache = JsonUtility.FromJson<ParameterCache>(json);
                paramCache = loadedCache;
                Debug.Log("Preset loaded");
            }
            else
            {
                Debug.Log("No preset file found");
            }
        }

        #endregion
    }

}
