using MelonLoader;
using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SlenderToolkit
{
    public static class SpeedDiagnostics
    {
        private static object characterMotor;
        private static FieldInfo movementField;
        private static FieldInfo maxForwardSpeedField;
        private static FieldInfo maxSidewaysSpeedField;
        private static FieldInfo maxBackwardsSpeedField;

        private static float lastLogTime = 0f;

        public static void Initialize()
        {
            MelonLogger.Msg("SpeedDiagnostics: Initialized");

            // Находим CharacterMotor
            MonoBehaviour[] allBehaviours = Object.FindObjectsOfType(typeof(MonoBehaviour)) as MonoBehaviour[];

            foreach (MonoBehaviour mb in allBehaviours)
            {
                if (mb != null && mb.GetType().Name == "CharacterMotor")
                {
                    characterMotor = mb;
                    MelonLogger.Msg($"SpeedDiagnostics: Found CharacterMotor on '{mb.gameObject.name}'");

                    movementField = characterMotor.GetType().GetField("movement", BindingFlags.Public | BindingFlags.Instance);

                    if (movementField != null)
                    {
                        object movement = movementField.GetValue(characterMotor);

                        if (movement != null)
                        {
                            Type movementType = movement.GetType();

                            maxForwardSpeedField = movementType.GetField("maxForwardSpeed", BindingFlags.Public | BindingFlags.Instance);
                            maxSidewaysSpeedField = movementType.GetField("maxSidewaysSpeed", BindingFlags.Public | BindingFlags.Instance);
                            maxBackwardsSpeedField = movementType.GetField("maxBackwardsSpeed", BindingFlags.Public | BindingFlags.Instance);

                            MelonLogger.Msg($"SpeedDiagnostics: Fields found - Fwd: {maxForwardSpeedField != null}, Side: {maxSidewaysSpeedField != null}, Back: {maxBackwardsSpeedField != null}");
                        }
                    }

                    break;
                }
            }
        }

        public static void Update()
        {
            if (characterMotor == null || movementField == null) return;

            float currentTime = Time.realtimeSinceStartup;
            if (currentTime - lastLogTime >= 1f) // Логируем каждую секунду
            {
                lastLogTime = currentTime;

                object movement = movementField.GetValue(characterMotor);

                if (movement != null)
                {
                    float forwardSpeed = maxForwardSpeedField != null ? (float)maxForwardSpeedField.GetValue(movement) : 0f;
                    float sideSpeed = maxSidewaysSpeedField != null ? (float)maxSidewaysSpeedField.GetValue(movement) : 0f;
                    float backSpeed = maxBackwardsSpeedField != null ? (float)maxBackwardsSpeedField.GetValue(movement) : 0f;

                    MelonLogger.Msg($"SpeedDiagnostics: Current speeds - Fwd: {forwardSpeed:F2}, Side: {sideSpeed:F2}, Back: {backSpeed:F2}");
                }
            }
        }
    }
}