using HarmonyLib;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace SlenderToolkit
{
    public static class SeedManager
    {
        public static bool IsMenuOpen = false;
        public static int currentSeed = -1;
        public static bool isEnabled = false;

        private static string seedInput = "";
        private static KeyCode toggleKey = KeyCode.F6;

        private static System.Random customRng;
        private static bool isRngPatched = false;
        private static bool seedAppliedForCurrentSession = false;
        private static int lastAppliedSeed = -1;

        private static MonoBehaviour playerScript;
        private static MonoBehaviour ml1;
        private static MonoBehaviour ml2;
        private static bool ml1WasEnabled = true;
        private static bool ml2WasEnabled = true;

        private static int finishPointCount = 0;
        private static bool finishPointsCounted = false;
        private static int randomCallCount = 0;

        public static void Initialize()
        {
            MelonLogger.Msg("SeedManager: Initialized (F6 - Toggle Menu)");

            // Находим PlayerScript и MouseLook
            MonoBehaviour[] allBehaviours = UnityEngine.Object.FindObjectsOfType(typeof(MonoBehaviour)) as MonoBehaviour[];
            foreach (MonoBehaviour mb in allBehaviours)
            {
                if (mb != null && mb.GetType().Name == "PlayerScript")
                {
                    playerScript = mb;
                    Type scriptType = mb.GetType();
                    FieldInfo ml1Field = scriptType.GetField("ml1", BindingFlags.Public | BindingFlags.Instance);
                    FieldInfo ml2Field = scriptType.GetField("ml2", BindingFlags.Public | BindingFlags.Instance);

                    if (ml1Field != null) ml1 = ml1Field.GetValue(mb) as MonoBehaviour;
                    if (ml2Field != null) ml2 = ml2Field.GetValue(mb) as MonoBehaviour;
                    break;
                }
            }

            // Патчим SpawnPages.Update для инициализации RNG и сброса флага
            var spawnPagesType = AccessTools.TypeByName("SpawnPages");
            if (spawnPagesType != null)
            {
                var updateMethod = spawnPagesType.GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
                if (updateMethod != null)
                {
                    var harmony = new HarmonyLib.Harmony("com.w3ntr.slendertoolkit.seed.init");
                    harmony.Patch(updateMethod, prefix: new HarmonyLib.HarmonyMethod(typeof(SeedManager), nameof(SpawnPagesUpdatePrefix)));
                    MelonLogger.Msg("SeedManager: Patched SpawnPages.Update for RNG initialization!");
                }
            }

            // Патчим FindGameObjectsWithTag для подсчёта точек
            var findWithTagMethod = typeof(GameObject).GetMethod("FindGameObjectsWithTag", new Type[] { typeof(string) });
            if (findWithTagMethod != null)
            {
                var harmony = new HarmonyLib.Harmony("com.w3ntr.slendertoolkit.seed.count");
                harmony.Patch(findWithTagMethod, postfix: new HarmonyLib.HarmonyMethod(typeof(SeedManager), nameof(FindGameObjectsWithTagPostfix)));
                MelonLogger.Msg("SeedManager: Patched FindGameObjectsWithTag!");
            }

            // Патчим SpawnPages.FindSpawn для подмены рандома
            if (spawnPagesType != null)
            {
                var findSpawnMethod = spawnPagesType.GetMethod("FindSpawn", BindingFlags.NonPublic | BindingFlags.Instance);
                if (findSpawnMethod != null)
                {
                    var harmony = new HarmonyLib.Harmony("com.w3ntr.slendertoolkit.seed");
                    harmony.Patch(findSpawnMethod, transpiler: new HarmonyLib.HarmonyMethod(typeof(SeedManager), nameof(FindSpawnTranspiler)));
                    isRngPatched = true;
                    MelonLogger.Msg("SeedManager: Successfully patched SpawnPages.FindSpawn!");
                }
            }
        }

        // Вызывается ПЕРЕД каждым Update SpawnPages
        private static void SpawnPagesUpdatePrefix(SpawnPages __instance)
        {
            // Сбрасываем флаг при начале сцены (when whichpage == 0)
            if (__instance.whichpage == 0 && seedAppliedForCurrentSession)
            {
                seedAppliedForCurrentSession = false;
                finishPointsCounted = false;
                randomCallCount = 0;
                MelonLogger.Msg("SeedManager: Scene reload detected, resetting RNG flag");
            }

            // Инициализируем RNG при первом кадре после загрузки сцены
            if (isEnabled && currentSeed != -1 && !seedAppliedForCurrentSession)
            {
                ResetRNG();
                MelonLogger.Msg($"SeedManager: RNG initialized at scene load with seed {currentSeed}");
            }
        }

        private static void FindGameObjectsWithTagPostfix(string tag, ref GameObject[] __result)
        {
            if (tag == "Finish" && __result != null && !finishPointsCounted)
            {
                finishPointCount = __result.Length;
                finishPointsCounted = true;
                MelonLogger.Msg($"SeedManager: Found {finishPointCount} Finish points in scene!");

                if (finishPointCount >= 8)
                {
                    long combinations = CalculateCombinations(finishPointCount, 8);
                    MelonLogger.Msg($"SeedManager: Possible page placement combinations: {combinations:N0}");
                }
            }
        }

        private static long CalculateCombinations(int n, int k)
        {
            if (k > n) return 0;
            if (k == 0 || k == n) return 1;
            if (k > n / 2) k = n - k;

            long result = 1;
            for (int i = 0; i < k; i++)
            {
                result = result * (n - i) / (i + 1);
            }
            return result;
        }

        public static IEnumerable<CodeInstruction> FindSpawnTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var getCode = typeof(UnityEngine.Random).GetProperty("value").GetGetMethod();
            var ourMethod = typeof(SeedManager).GetMethod(nameof(GetRandomValue), BindingFlags.Static | BindingFlags.NonPublic);

            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Call && instruction.operand as MethodInfo == getCode)
                {
                    yield return new CodeInstruction(OpCodes.Call, ourMethod);
                }
                else
                {
                    yield return instruction;
                }
            }
        }

        private static float GetRandomValue()
        {
            if (isEnabled && currentSeed != -1 && seedAppliedForCurrentSession)
            {
                if (customRng == null || lastAppliedSeed != currentSeed)
                {
                    customRng = new System.Random(currentSeed);
                    lastAppliedSeed = currentSeed;
                }

                if (randomCallCount < 10)
                {
                    float result = (float)customRng.NextDouble();
                    MelonLogger.Msg($"SeedManager: GetRandomValue() #{randomCallCount} = {result:F4}");
                    randomCallCount++;
                    return result;
                }

                return (float)customRng.NextDouble();
            }
            return UnityEngine.Random.value;
        }

        public static void ResetRNG()
        {
            seedAppliedForCurrentSession = true;
            customRng = new System.Random(currentSeed);
            lastAppliedSeed = currentSeed;
            finishPointsCounted = false;
            randomCallCount = 0;
        }

        public static void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                IsMenuOpen = !IsMenuOpen;

                if (IsMenuOpen)
                {
                    if (ml1 != null) { ml1WasEnabled = ml1.enabled; ml1.enabled = false; }
                    if (ml2 != null) { ml2WasEnabled = ml2.enabled; ml2.enabled = false; }
                    WinAPICursor.UnlockCursor();
                }
                else
                {
                    if (ml1 != null) ml1.enabled = ml1WasEnabled;
                    if (ml2 != null) ml2.enabled = ml2WasEnabled;
                    WinAPICursor.LockCursor();
                }
            }
        }

        public static void OnGUI()
        {
            if (!IsMenuOpen) return;

            Rect panelRect = new Rect(Screen.width - 230, 10, 220, 70);
            GUI.Box(panelRect, "Seed Manager");

            GUI.Label(new Rect(panelRect.x + 10, panelRect.y + 25, 50, 25), "Seed:");
            seedInput = GUI.TextField(new Rect(panelRect.x + 60, panelRect.y + 25, 100, 25), seedInput);

            if (GUI.Button(new Rect(panelRect.x + 165, panelRect.y + 25, 40, 25), "Set"))
            {
                if (int.TryParse(seedInput, out int newSeed))
                {
                    currentSeed = newSeed;
                    isEnabled = true;
                    ResetRNG();
                    MelonLogger.Msg($"SeedManager: Seed set to {newSeed}. Reload scene (R) to apply!");
                }
            }

            GUI.Label(new Rect(panelRect.x + 10, panelRect.y + 50, 190, 20), $"Current: {currentSeed} | F6: Close");
        }
    }
}