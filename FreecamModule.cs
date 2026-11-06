using MelonLoader;
using UnityEngine;

namespace SlenderToolkit
{
    public static class FreecamModule
    {
        public static bool IsEnabled = false;
        public static float FlySpeed = 12f;
        public static float FastFlyMultiplier = 3f;
        public static float MouseSensitivity = 2f;

        private static KeyCode toggleKey = KeyCode.F3;
        private static GameObject freecamObj;
        private static Camera freecamComp;
        private static Camera originalMainCam;
        private static float savedTimeScale = 1f;
        private static float rotX = 0f;
        private static float rotY = 0f;
        private static float lastRealTime = 0f;

        // Фонарик игрока
        private static Light playerFlashlight;
        private static Vector3 originalFlashlightPosition;
        private static Quaternion originalFlashlightRotation;
        private static Transform originalFlashlightParent;

        // Скрипт игрока
        private static MonoBehaviour playerScript;

        public static void Initialize()
        {
            MelonLogger.Msg("FreecamModule: Initialized (Toggle: F3)");
        }

        public static void Update()
        {
            if (Input.GetKeyDown(toggleKey)) ToggleFreecam();
            if (!IsEnabled) return;

            if (freecamObj == null || originalMainCam == null) { DisableFreecam(); return; }

            float currentRealTime = Time.realtimeSinceStartup;
            float realDeltaTime = currentRealTime - lastRealTime;
            lastRealTime = currentRealTime;

            if (realDeltaTime > 0.1f) realDeltaTime = 0.1f;
            if (realDeltaTime < 0f) realDeltaTime = 0f;

            rotX += Input.GetAxis("Mouse X") * MouseSensitivity * 2f;
            rotY -= Input.GetAxis("Mouse Y") * MouseSensitivity * 2f;
            rotY = Mathf.Clamp(rotY, -89f, 89f);

            freecamObj.transform.rotation = Quaternion.Euler(rotY, rotX, 0f);

            float speed = FlySpeed * (Input.GetKey(KeyCode.LeftShift) ? FastFlyMultiplier : 1f) * realDeltaTime;
            Vector3 move = Vector3.zero;

            if (Input.GetKey(KeyCode.W)) move += freecamObj.transform.forward;
            if (Input.GetKey(KeyCode.S)) move -= freecamObj.transform.forward;
            if (Input.GetKey(KeyCode.D)) move += freecamObj.transform.right;
            if (Input.GetKey(KeyCode.A)) move -= freecamObj.transform.right;
            if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Space)) move += Vector3.up;
            if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftControl)) move -= Vector3.up;

            freecamObj.transform.position += move * speed;

            // Перемещаем фонарик вместе с FreeCam
            MoveFlashlight();
        }

        public static void ToggleFreecam()
        {
            if (!IsEnabled) EnableFreecam();
            else DisableFreecam();
        }

        private static void EnableFreecam()
        {
            originalMainCam = Camera.main;
            if (originalMainCam == null) { MelonLogger.Msg("FreecamModule: ERROR - No main camera!"); return; }

            IsEnabled = true;
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            lastRealTime = Time.realtimeSinceStartup;

            freecamObj = new GameObject("SlenderFreecam");
            freecamObj.tag = "MainCamera";
            freecamComp = freecamObj.AddComponent<Camera>();
            freecamComp.CopyFrom(originalMainCam);

            freecamObj.transform.position = originalMainCam.transform.position;
            freecamObj.transform.rotation = originalMainCam.transform.rotation;
            rotX = freecamObj.transform.eulerAngles.y;
            rotY = freecamObj.transform.eulerAngles.x;

            originalMainCam.enabled = false;
            Screen.lockCursor = true;
            Screen.showCursor = false;

            // Находим фонарик игрока
            FindFlashlight();

            // Отключаем PlayerScript чтобы он не управлял фонариком
            DisablePlayerScript();

            MelonLogger.Msg("FreecamModule: Enabled");
        }

        private static void FindFlashlight()
        {
            Light[] allLights = Object.FindObjectsOfType(typeof(Light)) as Light[];

            MelonLogger.Msg($"FreecamModule: Found {allLights.Length} lights in scene");

            // Ищем Spot-свет (фонарик игрока)
            foreach (Light light in allLights)
            {
                if (light == null) continue;

                MelonLogger.Msg($"  Light: '{light.gameObject.name}', Type: {light.type}, Intensity: {light.intensity}, Parent: {light.transform.parent?.name ?? "null"}");

                if (light.type == LightType.Spot && light.intensity > 0f)
                {
                    playerFlashlight = light;
                    originalFlashlightPosition = light.transform.position;
                    originalFlashlightRotation = light.transform.rotation;
                    originalFlashlightParent = light.transform.parent;

                    // Отсоединяем фонарик от родителя (камеры игрока)
                    if (light.transform.parent != null)
                    {
                        light.transform.parent = null;
                        string parentName = originalFlashlightParent != null ? originalFlashlightParent.name : "null";
                        MelonLogger.Msg($"FreecamModule: Detached flashlight from parent '{parentName}'");
                    }

                    MelonLogger.Msg($"FreecamModule: Found flashlight '{light.gameObject.name}' (Spot type)");
                    break;
                }
            }

            if (playerFlashlight == null)
            {
                MelonLogger.Msg("FreecamModule: WARNING - Could not find player flashlight!");
            }
        }

        private static void DisablePlayerScript()
        {
            // Ищем PlayerScript на объекте игрока
            playerScript = originalMainCam.GetComponent<MonoBehaviour>();

            if (playerScript != null)
            {
                playerScript.enabled = false;
                MelonLogger.Msg($"FreecamModule: Disabled PlayerScript '{playerScript.GetType().Name}'");
            }
            else
            {
                // Если не нашли на камере, ищем на родительском объекте
                if (originalMainCam.transform.parent != null)
                {
                    playerScript = originalMainCam.transform.parent.GetComponent<MonoBehaviour>();
                    if (playerScript != null)
                    {
                        playerScript.enabled = false;
                        MelonLogger.Msg($"FreecamModule: Disabled PlayerScript on parent '{playerScript.GetType().Name}'");
                    }
                }
            }

            if (playerScript == null)
            {
                MelonLogger.Msg("FreecamModule: WARNING - Could not find PlayerScript!");
            }
        }

        private static void MoveFlashlight()
        {
            if (playerFlashlight != null)
            {
                // Перемещаем фонарик вместе с FreeCam
                playerFlashlight.transform.position = freecamObj.transform.position;
                playerFlashlight.transform.rotation = freecamObj.transform.rotation;
            }
        }

        private static void DisableFreecam()
        {
            IsEnabled = false;
            Time.timeScale = savedTimeScale > 0f ? savedTimeScale : 1f;
            if (originalMainCam != null) originalMainCam.enabled = true;
            if (freecamObj != null) Object.Destroy(freecamObj);

            // Возвращаем фонарик на место
            RestoreFlashlight();

            // Включаем PlayerScript обратно
            EnablePlayerScript();

            Screen.lockCursor = false;
            Screen.showCursor = true;
            MelonLogger.Msg("FreecamModule: Disabled");
        }

        private static void RestoreFlashlight()
        {
            if (playerFlashlight != null)
            {
                playerFlashlight.transform.position = originalFlashlightPosition;
                playerFlashlight.transform.rotation = originalFlashlightRotation;

                // Возвращаем фонарик к родителю
                if (originalFlashlightParent != null)
                {
                    playerFlashlight.transform.parent = originalFlashlightParent;
                    MelonLogger.Msg($"FreecamModule: Reattached flashlight to parent '{originalFlashlightParent.name}'");
                }

                MelonLogger.Msg("FreecamModule: Restored flashlight to original position");
            }

            playerFlashlight = null;
            originalFlashlightParent = null;
        }

        private static void EnablePlayerScript()
        {
            if (playerScript != null)
            {
                playerScript.enabled = true;
                MelonLogger.Msg($"FreecamModule: Re-enabled PlayerScript '{playerScript.GetType().Name}'");
                playerScript = null;
            }
        }
    }
}