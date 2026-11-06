using MelonLoader;
using UnityEngine;

namespace SlenderToolkit
{
    public static class GraphicsModule
    {
        // ПУБЛИЧНЫЕ поля — чтобы WindowModule мог к ним обращаться
        public static bool dayModeEnabled = false;
        public static float ambientBrightness = 0.8f;
        public static float fogDensity = 0.01f;
        public static float fogStartDistance = 0f;
        public static float fogEndDistance = 100f;

        // Оригинальные значения (тоже public для доступа из WindowModule)
        public static Color originalAmbientLight;
        public static Color originalFogColor;
        public static float originalFogDensity;
        public static bool originalFogEnabled;

        public static void Initialize()
        {
            MelonLogger.Msg("GraphicsModule: Initialized");

            originalAmbientLight = RenderSettings.ambientLight;
            originalFogColor = RenderSettings.fogColor;
            originalFogDensity = RenderSettings.fogDensity;
            originalFogEnabled = RenderSettings.fog;

            MelonLogger.Msg($"GraphicsModule: Original ambient: {originalAmbientLight}");
            MelonLogger.Msg($"GraphicsModule: Original fog: {originalFogEnabled}, density: {originalFogDensity}");
        }

        public static void Update()
        {
            if (dayModeEnabled)
            {
                RenderSettings.ambientLight = new Color(ambientBrightness, ambientBrightness, ambientBrightness, 1f);
                RenderSettings.fogColor = new Color(0.9f, 0.9f, 0.9f, 1f);
                RenderSettings.fogDensity = fogDensity;
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogStartDistance = fogStartDistance;
                RenderSettings.fogEndDistance = fogEndDistance;
            }
            else
            {
                RenderSettings.ambientLight = originalAmbientLight;
                RenderSettings.fogColor = originalFogColor;
                RenderSettings.fogDensity = originalFogDensity;
                RenderSettings.fog = originalFogEnabled;
            }
        }

        public static void ToggleDayMode()
        {
            dayModeEnabled = !dayModeEnabled;
            MelonLogger.Msg($"GraphicsModule: Day Mode {(dayModeEnabled ? "ENABLED" : "DISABLED")}");
        }

        public static void SetAmbientBrightness(float brightness)
        {
            ambientBrightness = brightness;
        }

        public static void SetFogDensity(float density)
        {
            fogDensity = density;
        }

        public static void SetFogStartDistance(float distance)
        {
            fogStartDistance = distance;
        }

        public static void SetFogEndDistance(float distance)
        {
            fogEndDistance = distance;
        }
    }
}