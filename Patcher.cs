using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CSL360Helper
{
    public static class Patcher
    {
        private const string HarmonyId = "neinnew.CSL360Helper";

        private static bool patched = false;

        public static void PatchAll()
        {
            if (patched) return;
            if (!CitiesHarmony.API.HarmonyHelper.IsHarmonyInstalled)
            {
                UnityEngine.Debug.LogError("CSL360Helper: Harmony is not installed.");
                return;
            }

            UnityEngine.Debug.Log("CSL360Helper: Patching...");

            patched = true;

            var harmony = new Harmony(HarmonyId);


            //https://github.com/algernon-A/ACME/blob/master/Code/Patches/Patcher.cs

            MethodBase targetMethod = typeof(CameraController).GetMethod("FpsBoosterLateUpdate", BindingFlags.Public | BindingFlags.Instance);
            if (targetMethod == null)
            {
                targetMethod = typeof(CameraController).GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            if (targetMethod == null)
            {
                UnityEngine.Debug.LogWarning("unable to find FPS patch target method");
                return;
            }

            MethodInfo patchMethod = typeof(CameraPatch).GetMethod(nameof(CameraPatch.Postfix));

            harmony.Patch(
                original: targetMethod,
                postfix: new HarmonyMethod(patchMethod)
                );
        }

        public static void UnpatchAll()
        {
            if (!patched) return;

            var harmony = new Harmony(HarmonyId);
            harmony.UnpatchAll(HarmonyId);

            patched = false;

            UnityEngine.Debug.Log("CSL360Helper: Reverted...");
        }


        
        }
    [HarmonyPatch]
    public static class CameraPatch
    {
        private static CameraController controller;
        public static bool fixTilt = false;
        public static float fixedTiltValue;
        public static void Postfix()
        {

            controller ??= GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<CameraController>();

            if (controller == null)
            {
                UnityEngine.Debug.LogWarning("null");
                return;
            }
            if (fixTilt)
            {
                controller.m_currentAngle.y = fixedTiltValue;
                controller.m_targetAngle.y = fixedTiltValue;
            }
        }
    }
}

    


