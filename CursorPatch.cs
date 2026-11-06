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
        private static int blockCount = 0; // Для ограничения спама в логах

        public static void Initialize()
        {
            if (isPatched) return;

            harmony = new HarmonyLib.Harmony("com.w3ntr.slendertoolkit.cursorpatch");

            // Используем AccessTools.Method - это самый надёжный способ найти setter в Harmony
            MethodInfo targetMethod = AccessTools.Method(typeof(Screen), "set_lockCursor");

            if (targetMethod != null)
            {
                MethodInfo prefixMethod = typeof(CursorPatch).GetMethod(nameof(LockCursorPrefix), BindingFlags.Static | BindingFlags.NonPublic);
                harmony.Patch(targetMethod, prefix: new HarmonyMethod(prefixMethod));
                MelonLogger.Msg("CursorPatch: Successfully patched Screen.set_lockCursor via AccessTools!");
            }
            else
            {
                MelonLogger.Msg("CursorPatch: ERROR - Could not find Screen.set_lockCursor method!");
            }

            isPatched = true;
        }

        private static bool LockCursorPrefix(ref bool value)
        {
            if (isInsidePatch) return true;

            // Если меню открыто и игра пытается заблокировать курсор
            if (WindowModule.IsMenuOpen && value == true)
            {
                isInsidePatch = true;
                value = false; // Принудительно меняем значение на false
                isInsidePatch = false;

                // Логируем только первые 5 блокировок, чтобы не спамить
                if (blockCount < 5)
                {
                    MelonLogger.Msg($"CursorPatch: BLOCKED lockCursor=true (attempt #{blockCount + 1})");
                    blockCount++;
                }

                return false; // Отменяем оригинальный вызов
            }

            // Сбрасываем счётчик, когда меню закрыто
            if (!WindowModule.IsMenuOpen)
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