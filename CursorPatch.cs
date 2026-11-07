using HarmonyLib;
using MelonLoader;
using System.Reflection;
using UnityEngine;

namespace SlenderToolkit
{
    public static class CursorPatch
    {
        private static HarmonyLib.Harmony harmony;
        private static bool isPatched = false;
        private static bool isInsidePatch = false;
        private static int blockCount = 0;

        public static void Initialize()
        {
            if (isPatched) return;

            harmony = new HarmonyLib.Harmony("com.w3ntr.slendertoolkit.cursorpatch");
            MethodInfo targetMethod = AccessTools.Method(typeof(Screen), "set_lockCursor");

            if (targetMethod != null)
            {
                MethodInfo prefixMethod = typeof(CursorPatch).GetMethod(nameof(LockCursorPrefix), BindingFlags.Static | BindingFlags.NonPublic);
                harmony.Patch(targetMethod, prefix: new HarmonyMethod(prefixMethod));
                MelonLogger.Msg("CursorPatch: Successfully patched Screen.set_lockCursor!");
            }
            else
            {
                MelonLogger.Msg("CursorPatch: ERROR - Could not find Screen.set_lockCursor!");
            }

            isPatched = true;
        }

        private static bool LockCursorPrefix(ref bool value)
        {
            if (isInsidePatch) return true;

            // Проверяем ОБА флага: WindowModule И SeedManager
            bool anyMenuOpen = WindowModule.IsMenuOpen || SeedManager.IsMenuOpen;

            // Если ЛЮБОЕ меню открыто и игра пытается заблокировать курсор
            if (anyMenuOpen && value == true)
            {
                isInsidePatch = true;
                value = false; // Принудительно меняем значение на false
                isInsidePatch = false;

                if (blockCount < 5)
                {
                    MelonLogger.Msg($"CursorPatch: BLOCKED lockCursor=true (attempt #{blockCount + 1})");
                    blockCount++;
                }

                return false; // Отменяем оригинальный вызов
            }

            // Сбрасываем счётчик, когда ВСЕ меню закрыты
            if (!anyMenuOpen)
            {
                blockCount = 0;
            }

            return true; // Разрешаем оригинальный вызов
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