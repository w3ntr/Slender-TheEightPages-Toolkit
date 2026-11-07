using UnityEngine;
using MelonLoader;
using System.Reflection;

[assembly: MelonInfo(typeof(SlenderToolkit.Main), "Slender Toolkit", "1.1.0", "w3ntr")]
[assembly: MelonGame("Parsec Productions", "Slender")]

namespace SlenderToolkit
{
    public class Main : MelonMod
    {
        public override void OnApplicationStart()
        {
            MelonLogger.Msg("=== Slender Toolkit v1.1.0 Loaded ===");

            CursorPatch.Initialize();
            SpeedHackPatch.Initialize();

            FreecamModule.Initialize();
            WindowModule.Initialize();
            FpsModule.Initialize();
            FOVModule.Initialize();
            GraphicsModule.Initialize();
            PageESPModule.Initialize();
            PlayerModule.Initialize();
            StaminaUIModule.Initialize();
            SeedManager.Initialize();

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
            StaminaUIModule.Update();
            SeedManager.Update();
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
            SeedManager.OnGUI();
        }
    }
}