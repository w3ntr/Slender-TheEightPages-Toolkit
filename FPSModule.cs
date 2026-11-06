using MelonLoader;
using UnityEngine;

namespace SlenderToolkit
{
    public static class FpsModule
    {
        public static bool IsVsyncDisabled = false;
        public static string currentFpsText = "FPS: 0";
        public static bool showFpsText = true;

        // Позиция FPS текста (перетаскиваемая)
        public static Vector2 fpsTextPosition = new Vector2(10, 40);
        private static bool isDraggingFps = false;
        private static Vector2 dragOffset = Vector2.zero;

        private static float accum = 0.0f;
        private static int frames = 0;
        private static float timeLeft = 0.5f;
        private static float lastRealTimeFps = 0f;

        public static void Initialize()
        {
            MelonLogger.Msg("FpsModule: Initialized");
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 1;
            IsVsyncDisabled = false;
            lastRealTimeFps = Time.realtimeSinceStartup;
        }

        public static void Update()
        {
            float currentRealTime = Time.realtimeSinceStartup;
            float realDeltaTime = currentRealTime - lastRealTimeFps;
            lastRealTimeFps = currentRealTime;

            if (realDeltaTime > 0.1f) realDeltaTime = 0.1f;
            if (realDeltaTime < 0f) realDeltaTime = 0f;

            timeLeft -= realDeltaTime;
            accum += realDeltaTime;
            frames++;

            if (timeLeft <= 0.0f)
            {
                float fps = accum > 0f ? frames / accum : 0f;
                currentFpsText = $"FPS: {Mathf.RoundToInt(fps)}";
                timeLeft = 0.5f;
                accum = 0.0f;
                frames = 0;
            }
        }

        public static void OnGUI()
        {
            if (!showFpsText) return;

            // Рисуем FPS текст
            GUI.color = new Color(0, 1, 0, 0.8f);
            GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };

            // Создаём область для перетаскивания
            Rect fpsRect = new Rect(fpsTextPosition.x, fpsTextPosition.y, 100, 20);

            // Проверяем нажатие мыши на тексте
            if (Event.current.type == EventType.MouseDown && fpsRect.Contains(Event.current.mousePosition))
            {
                isDraggingFps = true;
                dragOffset = Event.current.mousePosition - fpsTextPosition;
                Event.current.Use();
            }

            // Перетаскивание
            if (isDraggingFps && Event.current.type == EventType.MouseDrag)
            {
                fpsTextPosition = Event.current.mousePosition - dragOffset;
                Event.current.Use();
            }

            // Отпускание мыши
            if (Event.current.type == EventType.MouseUp)
            {
                isDraggingFps = false;
            }

            GUI.Label(fpsRect, currentFpsText, style);
            GUI.color = Color.white;
        }

        public static void ToggleVsync()
        {
            IsVsyncDisabled = !IsVsyncDisabled;
            QualitySettings.vSyncCount = IsVsyncDisabled ? 0 : 1;
        }

        public static void SetFpsCap(int cap)
        {
            Application.targetFrameRate = cap;
        }
    }
}