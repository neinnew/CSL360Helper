using System;
using ICities;
using UnityEngine;
using CitiesHarmony.API;
using ColossalFramework.UI;

namespace CSL360Helper
{
    public class Mod : LoadingExtensionBase, IUserMod
    {
        public string Name => "CSL 360 Helper";

        public string Description => "";

        private UITextField textFieldReferenceSimulationTime;

        private float prevFOV = 45f;

        private SimulationManager simulationManager;
        private uint? loadedCurrentFrameIndex = null;
        private uint frameInterval = 100;
        public static uint referenceFramePoint;

        private static readonly string[] FixTiltLabels =
        {
            "Disable",
            "90°",
            "0°",
            "-90°"
        };

        public void OnEnabled()
        {
            HarmonyHelper.DoOnHarmonyReady(() => Patcher.PatchAll());
        }

        public void OnDisabled()
        {
            if (HarmonyHelper.IsHarmonyInstalled) Patcher.UnpatchAll();
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            UIHelperBase group = helper.AddGroup(Name);
            group.AddCheckbox("VFOV 90", false, (isChecked) => 
            {
                var cameraController = UnityEngine.Object.FindObjectOfType<CameraController>();

                if (cameraController != null)
                {
                    var camera = cameraController.GetComponent<Camera>();

                    if (camera != null)
                    {

                        if (camera.fieldOfView != 89.9999f)  
                            prevFOV = camera.fieldOfView;

                        camera.fieldOfView = isChecked ? 89.9999f : prevFOV;
                        //Debug.LogWarning("prevFOV: " + prevFOV + "FOV:" + camera.fieldOfView);
                    }
                }
            });

            textFieldReferenceSimulationTime = (UITextField)group.AddTextfield("Reference simulation frame: ", frameInterval.ToString(), _ => { }, (value) =>
            {
                frameInterval = uint.Parse(value);
                SetRefrenceFramePoint(loadedCurrentFrameIndex, frameInterval);
            });
            textFieldReferenceSimulationTime.numericalOnly = true;
            textFieldReferenceSimulationTime.size = new Vector2(70f, textFieldReferenceSimulationTime.size.y);
            var parent = textFieldReferenceSimulationTime.parent as UIPanel;
            parent.autoLayoutDirection = LayoutDirection.Horizontal;



            group.AddDropdown("Fix Tilt Value", FixTiltLabels, 0, value =>
            {
                switch (value)
                {
                    case 0:
                        CameraPatch.fixTilt = false;
                        break;
                    case 1:
                        CameraPatch.fixedTiltValue = 90f;
                        CameraPatch.fixTilt = true;
                        break;
                    case 2:
                        CameraPatch.fixedTiltValue = 0f;
                        CameraPatch.fixTilt = true;
                        break;
                    case 3:
                        CameraPatch.fixedTiltValue = -90f;
                        CameraPatch.fixTilt = true;
                        break;
                    default:
                        goto case 0;
                }
            });
        }

        public override void OnLevelLoaded(LoadMode load)
        {
            simulationManager = UnityEngine.Object.FindObjectOfType<SimulationManager>();
            loadedCurrentFrameIndex = simulationManager.m_currentFrameIndex;
            SetRefrenceFramePoint(loadedCurrentFrameIndex, frameInterval);
        }

        void SetRefrenceFramePoint(uint? loadedCurrentFrameIndex, uint frameInterval)
        {
            if (loadedCurrentFrameIndex != null)
            {
                referenceFramePoint = (uint)loadedCurrentFrameIndex + frameInterval;
                ModThreading.resetNeeded = true;
            }
        }
    }

    public class ModThreading : ThreadingExtensionBase
    {
        SimulationManager simulationManager;
        public static bool resetNeeded = false;

        private bool isNeeded = true;
        private bool isDeleted = false;

        private uint removeTime;
        private uint? savedReferenceFramePoint = null;

        private UIPanel panel;
        private UIPanel panel2;

        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            simulationManager ??= UnityEngine.Object.FindObjectOfType<SimulationManager>();
            savedReferenceFramePoint ??= Mod.referenceFramePoint;

            if (!isDeleted && simulationManager.m_currentFrameIndex >= savedReferenceFramePoint)
            {

                if (simulationManager.m_currentFrameIndex >= savedReferenceFramePoint + 100) 
                { 
                    isNeeded = false; 
                }

                if (isNeeded)
                {
                    isNeeded = false;

                    removeTime = simulationManager.m_currentFrameIndex + 100;

                    panel = UIView.GetAView().AddUIComponent(typeof(UIPanel)) as UIPanel;
                    panel.name = "CSL360Helper-FrameCheckerPanel";
                    //panel.isInteractive = false;
                    panel.anchor = UIAnchorStyle.CenterHorizontal | UIAnchorStyle.CenterVertical;
                    panel.size = new Vector2(400f, 400f);
                    panel.backgroundSprite = "GenericPanelWhite";
                    panel.color = new Color32(0, 255, 255, 255);

                    panel2 = panel.AddUIComponent<UIPanel>();
                    panel2.size = new Vector2(panel.size.x, 0f);
                    panel2.relativePosition = new Vector3(0f, 0f);
                    panel2.backgroundSprite = "GenericPanelWhite";
                    panel2.color = new Color32(255, 0, 255, 255);

                    var label = panel.AddUIComponent<UILabel>();
                    label.textScale = 1.5f;
                    label.textColor = new Color32(0, 0, 0, 255);
                    label.anchor = UIAnchorStyle.CenterHorizontal | UIAnchorStyle.CenterVertical;
                }

                panel2.size = new Vector2(panel.size.x, (simulationManager.m_currentFrameIndex - (uint)savedReferenceFramePoint) * 4);
                panel.GetComponentInChildren<UILabel>().text = simulationManager.m_currentFrameIndex.ToString();

                if (!isDeleted && simulationManager.m_currentFrameIndex >= removeTime)
                {
                    panel.RemoveUIComponent(panel.GetComponentInChildren<UIPanel>());
                    UnityEngine.Object.Destroy(panel);

                    isDeleted = true;
                }
            }
            else
            {
                if (resetNeeded)
                {
                    savedReferenceFramePoint = Mod.referenceFramePoint;
                    isNeeded = true;
                    isDeleted = false;
                    resetNeeded = false;
                }
            }

        }
    }
}
