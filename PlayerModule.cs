using MelonLoader;
using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SlenderToolkit
{
    public static class PlayerModule
    {
        public static bool godModeEnabled = false;
        public static bool speedHackEnabled = false;
        public static float speedMultiplier = 2f;

        private static KeyCode toggleGodModeKey = KeyCode.F7;
        private static KeyCode toggleSpeedKey = KeyCode.F8;
        private static KeyCode restartSceneKey = KeyCode.R;

        private static MonoBehaviour playerScript;
        private static FieldInfo sanityField;
        private static FieldInfo caughtField;
        private static FieldInfo lostField;
        private static FieldInfo staminaField;
        private static FieldInfo maxstamField;

        private static float lastGodModeLogTime = 0f;

        public static void Initialize()
        {
            MelonLogger.Msg("PlayerModule: Initialized (F7 - GodMode, F8 - SpeedHack, R - Restart)");
            FindPlayerScript();
        }

        private static void FindPlayerScript()
        {
            MonoBehaviour[] allBehaviours = Object.FindObjectsOfType(typeof(MonoBehaviour)) as MonoBehaviour[];

            foreach (MonoBehaviour mb in allBehaviours)
            {
                if (mb != null && mb.GetType().Name == "PlayerScript")
                {
                    playerScript = mb;
                    MelonLogger.Msg($"PlayerModule: Found PlayerScript on '{mb.gameObject.name}'");

                    Type scriptType = mb.GetType();

                    // Ищем все необходимые поля (они public в коде игры)
                    sanityField = scriptType.GetField("sanity", BindingFlags.Public | BindingFlags.Instance);
                    caughtField = scriptType.GetField("caught", BindingFlags.Public | BindingFlags.Instance);
                    lostField = scriptType.GetField("lost", BindingFlags.Public | BindingFlags.Instance);
                    staminaField = scriptType.GetField("stamina", BindingFlags.Public | BindingFlags.Instance);
                    maxstamField = scriptType.GetField("maxstam", BindingFlags.Public | BindingFlags.Instance);

                    MelonLogger.Msg($"PlayerModule: Fields found - sanity: {sanityField != null}, caught: {caughtField != null}, lost: {lostField != null}, stamina: {staminaField != null}, maxstam: {maxstamField != null}");
                    break;
                }
            }

            if (playerScript == null)
            {
                MelonLogger.Msg("PlayerModule: CRITICAL - PlayerScript not found!");
            }
        }

        public static void Update()
        {
            // Рестарт сцены на R
            if (Input.GetKeyDown(restartSceneKey))
            {
                MelonLogger.Msg("PlayerModule: Restarting scene...");
                // Загружаем сцену 1 (лес), а не текущую (интро)
                Application.LoadLevel(0);
            }

            if (Input.GetKeyDown(toggleGodModeKey))
            {
                godModeEnabled = !godModeEnabled;
                MelonLogger.Msg($"PlayerModule: GodMode {(godModeEnabled ? "ENABLED" : "DISABLED")}");
            }

            if (Input.GetKeyDown(toggleSpeedKey))
            {
                speedHackEnabled = !speedHackEnabled;
                MelonLogger.Msg($"PlayerModule: SpeedHack {(speedHackEnabled ? "ENABLED" : "DISABLED")}");
            }

            if (godModeEnabled && playerScript != null)
            {
                ApplyGodMode();
            }
        }

        private static void ApplyGodMode()
        {
            bool changed = false;

            if (caughtField != null && (bool)caughtField.GetValue(playerScript) == true)
            {
                caughtField.SetValue(playerScript, false);
                changed = true;
            }

            if (lostField != null && (bool)lostField.GetValue(playerScript) == true)
            {
                lostField.SetValue(playerScript, false);
                changed = true;
            }

            if (sanityField != null && (float)sanityField.GetValue(playerScript) < 100f)
            {
                sanityField.SetValue(playerScript, 100f);
                changed = true;
            }

            if (staminaField != null && (float)staminaField.GetValue(playerScript) < 100f)
            {
                staminaField.SetValue(playerScript, 100f);
                changed = true;
            }

            if (maxstamField != null && (float)maxstamField.GetValue(playerScript) < 100f)
            {
                maxstamField.SetValue(playerScript, 100f);
                changed = true;
            }

            // Логируем только если значения были изменены игрой, и не чаще раза в 2 секунды
            if (changed)
            {
                float currentTime = Time.realtimeSinceStartup;
                if (currentTime - lastGodModeLogTime > 2f)
                {
                    MelonLogger.Msg("PlayerModule: GodMode actively blocking damage/stamina drain!");
                    lastGodModeLogTime = currentTime;
                }
            }
        }

        // Этот метод вызывается из PageESPModule при телепорте
        public static void ResetPlayerVelocity()
        {
            // Оставим заглушку, так как CharacterMotor теперь обрабатывается отдельно, 
            // но если нужно, можно вернуть поиск CharacterMotor сюда.
            // Пока телепорт и так работает нормально.
        }

        public static void OnGUI()
        {
            float yPos = 60f;

            if (godModeEnabled)
            {
                GUI.color = Color.green;
                GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
                GUI.Label(new Rect(10, yPos, 150, 20), "GOD MODE", style);
                yPos += 20;
            }

            if (speedHackEnabled)
            {
                GUI.color = Color.yellow;
                GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
                GUI.Label(new Rect(10, yPos, 150, 20), $"SPEED x{speedMultiplier:F1}", style);
                yPos += 20;
            }

            GUI.color = Color.white;
        }
    }
}