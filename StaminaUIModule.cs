using MelonLoader;
using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SlenderToolkit
{
    public static class StaminaUIModule
    {
        public static bool isEnabled = false;
        public static Vector2 uiPosition = new Vector2(20, Screen.height - 40); // Снизу слева
        private static Texture2D whiteTexture;

        private static KeyCode toggleKey = KeyCode.F9;
        private static bool isDragging = false;
        private static Vector2 dragOffset = Vector2.zero;

        private static MonoBehaviour playerScript;
        private static FieldInfo staminaField;
        private static FieldInfo maxstamField;

        public static void Initialize()
        {
            MelonLogger.Msg("StaminaUIModule: Initialized (Toggle: F9)");

            whiteTexture = new Texture2D(1, 1);
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply();

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
                    MelonLogger.Msg($"StaminaUIModule: Found PlayerScript on '{mb.gameObject.name}'");

                    Type scriptType = mb.GetType();

                    // Пробуем разные варианты имён полей
                    staminaField = scriptType.GetField("stamina", BindingFlags.Public | BindingFlags.Instance);
                    if (staminaField == null) staminaField = scriptType.GetField("_stamina", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (staminaField == null) staminaField = scriptType.GetField("stamina", BindingFlags.NonPublic | BindingFlags.Instance);

                    maxstamField = scriptType.GetField("maxstam", BindingFlags.Public | BindingFlags.Instance);
                    if (maxstamField == null) maxstamField = scriptType.GetField("_maxstam", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (maxstamField == null) maxstamField = scriptType.GetField("maxstam", BindingFlags.NonPublic | BindingFlags.Instance);

                    MelonLogger.Msg($"StaminaUIModule: Fields found - stamina: {staminaField != null}, maxstam: {maxstamField != null}");

                    if (staminaField != null)
                    {
                        float val = (float)staminaField.GetValue(mb);
                        MelonLogger.Msg($"StaminaUIModule: Current stamina value: {val}");
                    }

                    break;
                }
            }

            if (playerScript == null)
            {
                MelonLogger.Msg("StaminaUIModule: ERROR - PlayerScript not found!");
            }
        }

        public static void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                isEnabled = !isEnabled;
                MelonLogger.Msg($"StaminaUIModule: {(isEnabled ? "ENABLED" : "DISABLED")}");
            }
        }

        public static void OnGUI()
        {
            if (!isEnabled || playerScript == null || staminaField == null || maxstamField == null) return;

            float currentStamina = (float)staminaField.GetValue(playerScript);
            float maxStamina = (float)maxstamField.GetValue(playerScript);

            string staminaText = $"Stamina: {currentStamina:F0} / {maxStamina:F0}";

            // Создаём прямоугольник для UI
            Rect uiRect = new Rect(uiPosition.x, uiPosition.y, 150, 25);

            // Логика перетаскивания
            if (Event.current.type == EventType.MouseDown && uiRect.Contains(Event.current.mousePosition))
            {
                isDragging = true;
                dragOffset = Event.current.mousePosition - uiPosition;
                Event.current.Use();
            }

            if (isDragging && Event.current.type == EventType.MouseDrag)
            {
                uiPosition = Event.current.mousePosition - dragOffset;
                Event.current.Use();
            }

            if (Event.current.type == EventType.MouseUp)
            {
                isDragging = false;
            }

            // Рисуем фон и текст
            GUI.color = new Color(0, 0, 0, 0.5f);
            GUI.DrawTexture(uiRect, whiteTexture);

            GUI.color = currentStamina > 30f ? Color.green : Color.red;
            GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            GUI.Label(uiRect, staminaText, style);

            GUI.color = Color.white;
        }
    }
}