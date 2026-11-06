using MelonLoader;
using UnityEngine;

namespace SlenderToolkit
{
    public static class SceneDiagnostics
    {
        public static void Initialize()
        {
            MelonLogger.Msg("=== Scene Diagnostics ===");
            MelonLogger.Msg($"Current scene index: {Application.loadedLevel}");
            MelonLogger.Msg($"Current scene name: {Application.loadedLevelName}");
            MelonLogger.Msg($"Total scenes in build: {Application.levelCount}");

            // В старых Unity нет Application.GetLevelName, просто логируем текущую
            MelonLogger.Msg($"You are in scene: '{Application.loadedLevelName}' (index {Application.loadedLevel})");

            MelonLogger.Msg("========================");
            MelonLogger.Msg("To restart gameplay scene, press R");
            MelonLogger.Msg("This will load scene index 1 (assuming forest is scene 1)");
        }
    }
}