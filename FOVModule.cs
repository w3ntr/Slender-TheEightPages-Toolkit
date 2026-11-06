using MelonLoader;
using UnityEngine;

namespace SlenderToolkit
{
    public static class FOVModule
    {
        private static Camera mainCam;
        public static float currentFOV = 60f;
        public static float originalFOV = 60f;

        public static void Initialize()
        {
            MelonLogger.Msg("FOVModule: Initialized");
        }

        public static void Update()
        {
            if (mainCam == null)
            {
                mainCam = Camera.main;
                if (mainCam != null)
                {
                    originalFOV = mainCam.fieldOfView;
                    currentFOV = originalFOV;
                }
            }
            else
            {
                mainCam.fieldOfView = currentFOV;
            }
        }

        public static void SetFOV(float fov)
        {
            currentFOV = Mathf.Clamp(fov, 10f, 179f);
        }

        public static void ResetFOV()
        {
            currentFOV = originalFOV;
        }
    }
}