using MelonLoader;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace SlenderToolkit
{
    public static class WinAPICursor
    {
        [DllImport("user32.dll")]
        private static extern int ShowCursor(bool bShow);

        private static bool isUnlocked = false;
        private static int callCount = 0; // Счётчик вызовов ShowCursor

        public static void UnlockCursor()
        {
            if (isUnlocked) return;

            // Вызываем ShowCursor(true) несколько раз, чтобы перебить счётчик игры
            // Игра могла вызвать ShowCursor(false) много раз
            for (int i = 0; i < 10; i++)
            {
                int result = ShowCursor(true);
                MelonLogger.Msg($"WinAPI Unlock call #{i}: result={result}");
            }
            callCount = 10;

            Screen.lockCursor = false;
            Screen.showCursor = true;

            isUnlocked = true;
            MelonLogger.Msg($"WinAPI: Cursor unlocked (callCount={callCount})");
        }

        public static void LockCursor()
        {
            if (!isUnlocked) return;

            // Компенсируем — вызываем ShowCursor(false) столько же раз
            for (int i = 0; i < callCount; i++)
            {
                ShowCursor(false);
            }

            Screen.lockCursor = true;
            Screen.showCursor = false;

            callCount = 0;
            isUnlocked = false;
            MelonLogger.Msg($"WinAPI: Cursor locked (compensated {callCount} calls)");
        }

        // Убрали ClipCursor — он ломает GUI!
    }
}