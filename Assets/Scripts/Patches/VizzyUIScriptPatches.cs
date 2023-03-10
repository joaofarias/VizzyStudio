using Assets.Scripts.Vizzy.UI;
using Assets.Scripts.Vizzy.UI.Elements;
using HarmonyLib;
using ModApi.Craft.Program;
using Rewired.Integration.UnityUI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Assets.Scripts.Patches
{
    [HarmonyPatch(typeof(VizzyUIScript))]
    static class VizzyUIScriptPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch("CreateController")]
        private static void CreateController(VizzyUIScript __instance, VizzyUIController ____controller)
        {
            ____controller.gameObject.AddComponent<VizzyStudioUI>();
            ____controller.gameObject.AddComponent<ModuleManager>();
            ____controller.gameObject.AddComponent<ReferenceManager>();
        }

        [HarmonyPostfix]
        [HarmonyPatch("CreateElementForNode")]
        private static void OnCreateElementForNode(ProgramNode programNode, BlockElementScript __result)
        {
            if (!ModuleManager.Instance.CanRenderNode(programNode))
            {
                __result.gameObject.SetActive(false);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(VizzyUIScript.LoadNewFlightProgram))]
        private static void OnNewFlightProgram()
        {
            ModuleManager.Instance.Reset();
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(VizzyUIScript.LoadFlightProgram), typeof(XElement))]
        private static void OnLoadFlightProgram()
        {
            ModuleManager.Instance.Reset();
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(VizzyUIScript.DragUpdate))]
        private static bool OnDragUpdate(VizzyUIScript __instance, Vector2 position, VizzyUIController ____controller)
        {
            if (VizzyStudioUI.Instance.OnDragUpdate(position, __instance.DragSelection))
            {
                //Setting private property to null: __instance.DragSelection.BestConnection = null;
                PropertyInfo bestConnectionProperty = AccessTools.Property(typeof(DragSelection), "BestConnection");
                bestConnectionProperty.SetValue(__instance.DragSelection, null);

                __instance.DragSelection.Transform.position = position;

                ____controller.TrashCanDropZone.Selected = false;

                return false;
            }

            return true;
        }
    }
}
