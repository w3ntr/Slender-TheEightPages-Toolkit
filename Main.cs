using UnityEngine;
using MelonLoader;
using System.Reflection;

[assembly: MelonInfo(typeof(SlenderToolkit.Main), "Slender Toolkit", "1.0.0", "w3ntr")]
[assembly: MelonGame("Parsec Productions", "Slender")]

namespace SlenderToolkit
{
    public class Main : MelonMod
    {
        public override void OnApplicationStart()
        {
            MelonLogger.Msg("=== Slender Toolkit v1.0.0 Loaded ===");

            CursorPatch.Initialize();
            SpeedHackPatch.Initialize();

            FreecamModule.Initialize();
            WindowModule.Initialize();
            FpsModule.Initialize();
            FOVModule.Initialize();
            GraphicsModule.Initialize();
            PageESPModule.Initialize();
            PlayerModule.Initialize();
            SpeedDiagnostics.Initialize();
            StaminaUIModule.Initialize();
            SceneDiagnostics.Initialize();

            MelonLogger.Msg("All modules initialized successfully!");
        }

        public override void OnApplicationQuit()
        {
            CursorPatch.Cleanup();
            SpeedHackPatch.Cleanup();
        }

        public override void OnUpdate()
        {
            FreecamModule.Update();
            WindowModule.Update();
            FpsModule.Update();
            FOVModule.Update();
            GraphicsModule.Update();
            PageESPModule.Update();
            PlayerModule.Update();
            SpeedDiagnostics.Update();
            StaminaUIModule.Update();
        }

        public override void OnLateUpdate()
        {
            WindowModule.LateUpdate();
        }

        public override void OnGUI()
        {
            WindowModule.OnGUI();
            FpsModule.OnGUI();
            PageESPModule.OnGUI();
            PlayerModule.OnGUI();
            StaminaUIModule.OnGUI();
        }
    }
}