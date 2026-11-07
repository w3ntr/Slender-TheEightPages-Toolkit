using MelonLoader;
using UnityEngine;
using Harmony;

namespace SlenderToolkit
{
    public static class WindowModule
    {
        private enum Tab { Freecam, FPS, FOV, Triggers, Graphics, Pages }

        public static bool IsMenuOpen => showWindow;
        public static Rect WindowRect => windowRect;
        private static KeyCode toggleKey = KeyCode.Insert;
        private static bool showWindow = false;
        private static Rect windowRect = new Rect(20, 20, 320, 350);
        private static Tab activeTab = Tab.Freecam;

        public static void Initialize()
        {
            MelonLogger.Msg("WindowModule: Initialized (Toggle: Insert)");
        }
        public static void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                showWindow = !showWindow;

                if (showWindow)
                {
                    WinAPICursor.UnlockCursor();
                }
                else
                {
                    WinAPICursor.LockCursor();
                }
            }
        }

        public static void LateUpdate()
        {
            // Ничего не делаем с курсором тут — пусть Harmony + WinAPI работают
        }

        public static void OnGUI()
        {
            if (!showWindow) return;

            // Не трогаем Screen.lockCursor/showCursor здесь — это мешает GUI
            windowRect = GUILayout.Window(1, windowRect, DrawWindow, "Slender Toolkit");
        }

        private static void DrawWindow(int windowID)
        {
            GUILayout.BeginHorizontal();

            GUI.color = activeTab == Tab.Freecam ? Color.cyan : Color.white;
            if (GUILayout.Button("Freecam", GUILayout.ExpandWidth(true))) activeTab = Tab.Freecam;

            GUI.color = activeTab == Tab.Graphics ? Color.cyan : Color.white;
            if (GUILayout.Button("Graphics", GUILayout.ExpandWidth(true))) activeTab = Tab.Graphics;
            
            GUI.color = activeTab == Tab.Pages ? Color.cyan : Color.white;
            if (GUILayout.Button("Pages", GUILayout.ExpandWidth(true))) activeTab = Tab.Pages;

            GUI.color = activeTab == Tab.FPS ? Color.cyan : Color.white;
            if (GUILayout.Button("FPS", GUILayout.ExpandWidth(true))) activeTab = Tab.FPS;

            GUI.color = activeTab == Tab.FOV ? Color.cyan : Color.white;
            if (GUILayout.Button("FOV", GUILayout.ExpandWidth(true))) activeTab = Tab.FOV;

            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            switch (activeTab)
            {
                case Tab.Freecam: DrawFreecamTab(); break;
                case Tab.Graphics: DrawGraphicsTab(); break;
                case Tab.Pages: DrawPagesTab(); break;
                case Tab.FPS: DrawFpsTab(); break;
                case Tab.FOV: DrawFOVTab(); break;
            }

            GUI.DragWindow(new Rect(0, 0, windowRect.width, 25));
        }

        // ... (остальные методы DrawFreecamTab, DrawFpsTab, DrawFOVTab, DrawTriggersTab без изменений)

        private static void DrawFreecamTab()
        {
            GUILayout.Label("=== Freecam Module ===");
            GUILayout.Space(5);
            GUILayout.Label($"Status: {(FreecamModule.IsEnabled ? "ENABLED" : "DISABLED")}");
            GUILayout.Space(5);

            if (GUILayout.Button(FreecamModule.IsEnabled ? "Disable Freecam" : "Enable Freecam"))
            {
                FreecamModule.ToggleFreecam();
            }

            GUILayout.Space(10);
            GUILayout.Label("--- Settings ---");

            GUILayout.Label($"Fly Speed: {FreecamModule.FlySpeed:F1}");
            FreecamModule.FlySpeed = GUILayout.HorizontalSlider(FreecamModule.FlySpeed, 1f, 50f);

            GUILayout.Label($"Fast Multiplier: {FreecamModule.FastFlyMultiplier:F1}");
            FreecamModule.FastFlyMultiplier = GUILayout.HorizontalSlider(FreecamModule.FastFlyMultiplier, 1f, 10f);

            GUILayout.Label($"Mouse Sensitivity: {FreecamModule.MouseSensitivity:F1}");
            FreecamModule.MouseSensitivity = GUILayout.HorizontalSlider(FreecamModule.MouseSensitivity, 0.5f, 10f);

            GUILayout.Space(10);
            GUILayout.Label("--- Controls ---");
            GUILayout.Label("F3 - Toggle Freecam");
            GUILayout.Label("Insert - Toggle GUI");
            GUILayout.Label("WASD - Move | Shift - Fast");
            GUILayout.Label("Space/E - Up | Q/Ctrl - Down");
        }

        private static void DrawGraphicsTab()
        {
            GUILayout.Label("=== Graphics Module ===");
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            bool dayMode = GUILayout.Toggle(GraphicsModule.dayModeEnabled, "Day Mode", GUILayout.Width(100));
            if (dayMode != GraphicsModule.dayModeEnabled)
            {
                GraphicsModule.ToggleDayMode();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("--- Day Mode Settings ---");

            // Ambient Brightness
            GUILayout.Label($"Ambient Brightness: {GraphicsModule.ambientBrightness:F2}");
            float newBrightness = GUILayout.HorizontalSlider(GraphicsModule.ambientBrightness, 0f, 2f);
            if (newBrightness != GraphicsModule.ambientBrightness)
            {
                GraphicsModule.SetAmbientBrightness(newBrightness);
            }

            // Fog Density
            GUILayout.Label($"Fog Density: {GraphicsModule.fogDensity:F3}");
            float newFogDensity = GUILayout.HorizontalSlider(GraphicsModule.fogDensity, 0f, 0.1f);
            if (newFogDensity != GraphicsModule.fogDensity)
            {
                GraphicsModule.SetFogDensity(newFogDensity);
            }

            // Fog Start Distance
            GUILayout.Label($"Fog Start Distance: {GraphicsModule.fogStartDistance:F0}m");
            float newFogStart = GUILayout.HorizontalSlider(GraphicsModule.fogStartDistance, 0f, 100f);
            if (newFogStart != GraphicsModule.fogStartDistance)
            {
                GraphicsModule.SetFogStartDistance(newFogStart);
            }

            // Fog End Distance
            GUILayout.Label($"Fog End Distance: {GraphicsModule.fogEndDistance:F0}m");
            float newFogEnd = GUILayout.HorizontalSlider(GraphicsModule.fogEndDistance, 0f, 500f);
            if (newFogEnd != GraphicsModule.fogEndDistance)
            {
                GraphicsModule.SetFogEndDistance(newFogEnd);
            }

            GUILayout.Space(10);
            GUILayout.Label("--- Quick Presets ---");

            if (GUILayout.Button("Clear Day (Bright)"))
            {
                GraphicsModule.SetAmbientBrightness(1.5f);
                GraphicsModule.SetFogDensity(0.001f);
                GraphicsModule.SetFogStartDistance(0f);
                GraphicsModule.SetFogEndDistance(500f);
            }

            if (GUILayout.Button("Overcast (Gray)"))
            {
                GraphicsModule.SetAmbientBrightness(0.6f);
                GraphicsModule.SetFogDensity(0.02f);
                GraphicsModule.SetFogStartDistance(10f);
                GraphicsModule.SetFogEndDistance(100f);
            }

            if (GUILayout.Button("Reset to Original"))
            {
                GraphicsModule.dayModeEnabled = false;
                RenderSettings.ambientLight = GraphicsModule.originalAmbientLight;
                RenderSettings.fogColor = GraphicsModule.originalFogColor;
                RenderSettings.fogDensity = GraphicsModule.originalFogDensity;
                RenderSettings.fog = GraphicsModule.originalFogEnabled;
                MelonLogger.Msg("GraphicsModule: Restored original settings");
            }

            GUILayout.Space(10);
            GUILayout.Label("--- Info ---");
            GUILayout.Label("Day Mode makes the game brighter and reduces fog.");
            GUILayout.Label("Note: Some games override these settings via post-processing.");
        }

        private static void DrawPagesTab()
        {
            GUILayout.Label("=== Page ESP & Teleport ===");
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            bool enabled = GUILayout.Toggle(PageESPModule.isEnabled, "Enable", GUILayout.Width(100));
            if (enabled != PageESPModule.isEnabled)
            {
                PageESPModule.isEnabled = enabled;
                if (enabled) PageESPModule.FindPages();
            }
            GUILayout.Label("F1 - Toggle | F - Teleport to nearest");
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("--- Options ---");

            GUILayout.BeginHorizontal();
            bool throughWalls = GUILayout.Toggle(PageESPModule.showThroughWalls, "Through Walls", GUILayout.Width(120));
            if (throughWalls != PageESPModule.showThroughWalls) PageESPModule.ToggleThroughWalls();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            bool labels = GUILayout.Toggle(PageESPModule.showLabels, "Show Labels", GUILayout.Width(120));
            if (labels != PageESPModule.showLabels) PageESPModule.ToggleLabels();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            bool wireframe = GUILayout.Toggle(PageESPModule.wireframeMode, "Wireframe", GUILayout.Width(120));
            if (wireframe != PageESPModule.wireframeMode) PageESPModule.ToggleWireframe();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("--- Info ---");
            GUILayout.Label($"Pages found: {PageESPModule.pageCount}/8");
            GUILayout.Label("Press F to teleport to nearest page");
            GUILayout.Label("Pages are highlighted in cyan");
        }

        private static void DrawFpsTab()
        {
            GUILayout.Label("=== Performance ===");
            GUILayout.Space(5);

            GUI.color = Color.green;
            GUILayout.Label(FpsModule.currentFpsText, new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter });
            GUI.color = Color.white;

            GUILayout.Space(10);
            GUILayout.Label("--- Settings ---");

            GUILayout.BeginHorizontal();
            bool vsyncToggle = GUILayout.Toggle(!FpsModule.IsVsyncDisabled, "V-Sync", GUILayout.Width(100));
            if (vsyncToggle != !FpsModule.IsVsyncDisabled)
            {
                FpsModule.ToggleVsync();
            }
            GUILayout.Label(FpsModule.IsVsyncDisabled ? "Disabled (Unlocked)" : "Enabled");
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("--- Display ---");

            GUILayout.BeginHorizontal();
            bool showFps = GUILayout.Toggle(FpsModule.showFpsText, "Show FPS Overlay", GUILayout.Width(150));
            if (showFps != FpsModule.showFpsText)
            {
                FpsModule.showFpsText = showFps;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("--- FPS Cap ---");

            GUILayout.Label($"Current Cap: {(Application.targetFrameRate == -1 ? "Unlimited" : Application.targetFrameRate.ToString())}");

            int currentCap = Application.targetFrameRate == -1 ? 720 : Application.targetFrameRate;
            int newCap = (int)GUILayout.HorizontalSlider(currentCap, 30, 720);

            if (newCap != currentCap)
            {
                FpsModule.SetFpsCap(newCap);
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Set Unlimited FPS (-1)"))
            {
                FpsModule.SetFpsCap(-1);
            }

            GUILayout.Space(10);
            GUILayout.Label("--- Info ---");
            GUILayout.Label("Note: Old Unity games often cap at 60 or 30 FPS by default. Disabling V-Sync lets your GPU render as fast as it can.");
        }

        private static void DrawFOVTab()
        {
            GUILayout.Label("=== Field of View ===");
            GUILayout.Space(5);

            GUILayout.Label($"Current FOV: {FOVModule.currentFOV:F1}°");

            float newFOV = GUILayout.HorizontalSlider(FOVModule.currentFOV, 10f, 179f);
            if (newFOV != FOVModule.currentFOV)
            {
                FOVModule.SetFOV(newFOV);
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Reset to Default"))
            {
                FOVModule.ResetFOV();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Wide FOV (90°)"))
            {
                FOVModule.SetFOV(90f);
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Ultra Wide (120°)"))
            {
                FOVModule.SetFOV(120f);
            }

            GUILayout.Space(10);
            GUILayout.Label("--- Presets ---");
            GUILayout.Label("60° - Default (narrow)");
            GUILayout.Label("75° - Comfortable");
            GUILayout.Label("90° - Wide (recommended)");
            GUILayout.Label("120° - Ultra wide (fish-eye)");
        }
    }
}