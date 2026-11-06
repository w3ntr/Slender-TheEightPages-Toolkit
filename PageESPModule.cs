using MelonLoader;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SlenderToolkit
{
    public static class PageESPModule
    {
        private static KeyCode toggleKey = KeyCode.F1;
        public static bool isEnabled = false;
        public static bool showThroughWalls = true;
        public static bool showLabels = true;
        public static bool wireframeMode = false;

        private static List<PageInfo> pages = new List<PageInfo>();
        public static int pageCount = 0;
        private static List<GameObject> overlayObjects = new List<GameObject>();
        private static Material overlayMaterial;
        private static Mesh wireframeMesh;

        private class PageInfo
        {
            public GameObject gameObject;
            public Collider collider;
            public Vector3 center;
            public Vector3 size;
            public string name;
        }

        public static void Initialize()
        {
            MelonLogger.Msg("PageESPModule: Initialized (F1 - Toggle)");
            CreateMaterial();
            CreateWireframeMesh();
        }

        private static void CreateMaterial()
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null) shader = Shader.Find("GUI/Text Shader");
            overlayMaterial = new Material(shader);
            UpdateMaterialProperties();
        }

        private static void UpdateMaterialProperties()
        {
            if (overlayMaterial == null) return;
            overlayMaterial.SetFloat("_SrcBlend", 5f);
            overlayMaterial.SetFloat("_DstBlend", 10f);
            overlayMaterial.SetFloat("_Cull", 0f);
            overlayMaterial.SetFloat("_ZWrite", 0f);
            overlayMaterial.SetFloat("_ZTest", showThroughWalls ? 8f : 4f);
            overlayMaterial.color = new Color(0f, 1f, 1f, 0.3f); // Голубой для страниц
        }

        private static void CreateWireframeMesh()
        {
            if (wireframeMesh != null) return;
            wireframeMesh = new Mesh();
            Vector3[] verts = new Vector3[8] {
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3( 0.5f, -0.5f, -0.5f),
                new Vector3( 0.5f,  0.5f, -0.5f), new Vector3(-0.5f,  0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f,  0.5f), new Vector3( 0.5f, -0.5f,  0.5f),
                new Vector3( 0.5f,  0.5f,  0.5f), new Vector3(-0.5f,  0.5f,  0.5f)
            };
            int[] lines = new int[24] {
                0,1, 1,2, 2,3, 3,0, 4,5, 5,6, 6,7, 7,4, 0,4, 1,5, 2,6, 3,7
            };
            wireframeMesh.vertices = verts;
            wireframeMesh.triangles = lines;
        }

        public static void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                isEnabled = !isEnabled;
                MelonLogger.Msg($"PageESPModule: {(isEnabled ? "ENABLED" : "DISABLED")}");
                if (isEnabled) FindPages();
                else ClearOverlays();
            }

            // Телепорт к ближайшей странице по клавише F
            if (Input.GetKeyDown(KeyCode.F) && isEnabled && pages.Count > 0)
            {
                TeleportToNearestPage();
            }
        }

        public static void OnGUI()
        {
            if (!isEnabled || Camera.main == null) return;

            if (showLabels)
            {
                foreach (PageInfo page in pages)
                {
                    if (page.gameObject == null) continue;
                    Vector3 screenPos = Camera.main.WorldToScreenPoint(page.center);
                    if (screenPos.z > 0)
                    {
                        GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                        float distance = Vector3.Distance(Camera.main.transform.position, page.center);
                        GUI.color = Color.cyan;
                        GUI.Label(new Rect(screenPos.x - 60, Screen.height - screenPos.y + 30, 120, 30), $"[PAGE: {distance:F1}m]", labelStyle);
                        GUI.color = Color.white;
                    }
                }
            }

            GUI.Label(new Rect(10, 25, 200, 30), $"Pages: {pageCount}/8", new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, normal = new GUIStyleState { textColor = Color.cyan } });
        }

        private static void CreateOverlays()
        {
            ClearOverlays();
            foreach (PageInfo page in pages)
            {
                if (page.gameObject == null) continue;

                Vector3 size = page.collider != null ? page.collider.bounds.size : new Vector3(1f, 1f, 0.1f);
                GameObject overlayObj = wireframeMode ? new GameObject("[PageWire]") : GameObject.CreatePrimitive(PrimitiveType.Cube);

                if (wireframeMode)
                {
                    overlayObj.AddComponent<MeshFilter>().sharedMesh = wireframeMesh;
                    overlayObj.AddComponent<MeshRenderer>().material = overlayMaterial;
                }
                else
                {
                    if (overlayObj.GetComponent<Collider>() != null) Object.Destroy(overlayObj.GetComponent<Collider>());
                    overlayObj.GetComponent<Renderer>().material = overlayMaterial;
                }

                overlayObj.transform.position = page.center;
                overlayObj.transform.localScale = size + new Vector3(0.01f, 0.01f, 0.01f);
                overlayObjects.Add(overlayObj);
            }
        }

        private static void ClearOverlays()
        {
            foreach (GameObject obj in overlayObjects) if (obj != null) Object.Destroy(obj);
            overlayObjects.Clear();
        }

        public static void FindPages()
        {
            pages.Clear();
            MelonLogger.Msg("PageESPModule: Searching for pages...");

            // Способ 1: Ищем по компоненту PickupPage (если класс доступен)
            MonoBehaviour[] allBehaviours = Object.FindObjectsOfType(typeof(MonoBehaviour)) as MonoBehaviour[];

            foreach (MonoBehaviour mb in allBehaviours)
            {
                if (mb == null) continue;

                string typeName = mb.GetType().Name;

                // Ищем PickupPage или похожие классы
                if (typeName.Contains("PickupPage") || typeName.Contains("Page") || typeName.Contains("Collectible"))
                {
                    GameObject go = mb.gameObject;
                    if (go == null) continue;

                    // ИСКЛЮЧАЕМ PageController и SpawnPages
                    if (go.name.Contains("PageController") || go.name.Contains("SpawnPages") || typeName == "SpawnPages")
                    {
                        MelonLogger.Msg($"PageESPModule: Skipping '{go.name}' (spawner, not a page)");
                        continue;
                    }

                    Collider col = go.GetComponent<Collider>();
                    Vector3 center = col != null ? col.bounds.center : go.transform.position;

                    pages.Add(new PageInfo
                    {
                        gameObject = go,
                        collider = col,
                        center = center,
                        size = col != null ? col.bounds.size : new Vector3(1f, 1f, 0.1f),
                        name = go.name
                    });

                    MelonLogger.Msg($"PageESPModule: Found page '{go.name}' via component {typeName}");
                }
            }

            // Способ 2: Ищем по имени объекта (фоллбэк)
            if (pages.Count == 0)
            {
                MelonLogger.Msg("PageESPModule: No pages found via components, searching by name...");
                GameObject[] allObjects = Object.FindObjectsOfType(typeof(GameObject)) as GameObject[];

                foreach (GameObject go in allObjects)
                {
                    if (go == null) continue;
                    string name = go.name.ToLower();

                    if (name.Contains("page") || name.Contains("paper") || name.Contains("note"))
                    {
                        // Проверяем, не дубликат ли
                        bool alreadyFound = false;
                        foreach (PageInfo p in pages)
                        {
                            if (p.gameObject == go) { alreadyFound = true; break; }
                        }
                        if (alreadyFound) continue;

                        Collider col = go.GetComponent<Collider>();
                        Vector3 center = col != null ? col.bounds.center : go.transform.position;

                        pages.Add(new PageInfo
                        {
                            gameObject = go,
                            collider = col,
                            center = center,
                            size = col != null ? col.bounds.size : new Vector3(1f, 1f, 0.1f),
                            name = go.name
                        });

                        MelonLogger.Msg($"PageESPModule: Found page '{go.name}' by name");
                    }
                }
            }

            pageCount = pages.Count;
            MelonLogger.Msg($"PageESPModule: Found {pageCount} pages total");
            CreateOverlays();
        }

        public static void TeleportToNearestPage()
        {
            if (pages.Count == 0)
            {
                MelonLogger.Msg("PageESPModule: No pages to teleport to!");
                return;
            }

            // Ищем CharacterMotor через рефлексию (по имени типа)
            MonoBehaviour[] allBehaviours = Object.FindObjectsOfType(typeof(MonoBehaviour)) as MonoBehaviour[];
            MonoBehaviour characterMotorMb = null;

            foreach (MonoBehaviour mb in allBehaviours)
            {
                if (mb != null && mb.GetType().Name == "CharacterMotor")
                {
                    characterMotorMb = mb;
                    break;
                }
            }

            if (characterMotorMb == null)
            {
                MelonLogger.Msg("PageESPModule: ERROR - CharacterMotor not found!");
                return;
            }

            GameObject playerObject = characterMotorMb.gameObject;
            MelonLogger.Msg($"PageESPModule: Found player object: '{playerObject.name}'");

            // Находим ближайшую страницу
            Vector3 playerPos = playerObject.transform.position;
            PageInfo nearest = null;
            float nearestDist = float.MaxValue;

            foreach (PageInfo page in pages)
            {
                if (page.gameObject == null) continue;
                float dist = Vector3.Distance(playerPos, page.center);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = page;
                }
            }

            if (nearest == null)
            {
                MelonLogger.Msg("PageESPModule: ERROR - No valid page found!");
                return;
            }

            // Телепортируем игрока прямо на позицию страницы
            playerObject.transform.position = nearest.center;

            MelonLogger.Msg($"PageESPModule: Teleported player to page '{nearest.name}' ({nearestDist:F1}m away)");

            // Сбрасываем velocity через PlayerModule
            PlayerModule.ResetPlayerVelocity();
        }

        public static void ToggleThroughWalls() { showThroughWalls = !showThroughWalls; UpdateMaterialProperties(); }
        public static void ToggleLabels() { showLabels = !showLabels; }
        public static void ToggleWireframe() { wireframeMode = !wireframeMode; if (isEnabled) CreateOverlays(); }
    }
}