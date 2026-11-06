using HarmonyLib;
using MelonLoader;
using System;
using System.Reflection;
using UnityEngine;

namespace SlenderToolkit
{
    public static class SpeedHackPatch
    {
        private static HarmonyLib.Harmony harmony;
        private static bool isPatched = false;

        public static void Initialize()
        {
            if (isPatched) return;

            harmony = new HarmonyLib.Harmony("com.w3ntr.slendertoolkit.speedhack");

            // Ищем тип CharacterMotor через поиск по всем загруженным сборкам
            Type characterMotorType = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                characterMotorType = assembly.GetType("CharacterMotor");
                if (characterMotorType != null) break;
            }

            if (characterMotorType == null)
            {
                MelonLogger.Msg("SpeedHackPatch: ERROR - CharacterMotor type not found!");
                return;
            }

            // Патчим MaxSpeedInDirection - метод, который возвращает макс. скорость
            MethodInfo targetMethod = characterMotorType.GetMethod("MaxSpeedInDirection", BindingFlags.Public | BindingFlags.Instance);

            if (targetMethod != null)
            {
                MethodInfo postfixMethod = typeof(SpeedHackPatch).GetMethod(nameof(MaxSpeedInDirectionPostfix), BindingFlags.Static | BindingFlags.NonPublic);
                harmony.Patch(targetMethod, postfix: new HarmonyMethod(postfixMethod));
                MelonLogger.Msg("SpeedHackPatch: MaxSpeedInDirection patched!");
            }
            else
            {
                MelonLogger.Msg("SpeedHackPatch: ERROR - MaxSpeedInDirection method not found!");
            }

            isPatched = true;
        }

        // Postfix умножает возвращаемое значение на speedMultiplier
        private static void MaxSpeedInDirectionPostfix(ref float __result)
        {
            if (PlayerModule.speedHackEnabled)
            {
                __result *= PlayerModule.speedMultiplier;
            }
        }

        public static void Cleanup()
        {
            if (harmony != null)
            {
                harmony.UnpatchSelf();
                isPatched = false;
            }
        }
    }
}