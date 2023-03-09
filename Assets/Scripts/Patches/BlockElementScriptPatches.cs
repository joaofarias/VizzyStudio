using Assets.Scripts.Vizzy.UI.Elements;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts.Patches
{
    [HarmonyPatch(typeof(BlockElementScript))]
    internal static class BlockElementScriptPatches
    {
        /// <summary>
        /// When cloning blocks, the dragging callbacks are sent to the original block even though it's the clone that is actually moving.
        /// If we move the clone to a different module, the original gets disabled and dragging is broken.
        /// This function swaps the object being dragged by the event system so that the callbacks go directly to the clone
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(BlockElementScript.StartClone))]
        private static bool OnStartClone(BlockElementScript __instance, PointerEventData eventData, bool cloneChain)
        {
            List<BlockElementScript> clones = __instance.VizzyUI.NodeBuilder.CloneBlock(__instance, cloneChain);
            __instance.VizzyUI.DragBegin(clones, eventData.position);

            // replace the dragged entity
            eventData.pointerDrag = clones[0].gameObject;
            eventData.hovered = new List<GameObject>() { clones[0].gameObject };
            eventData.pointerPress = clones[0].gameObject;

            // Pretend call OnBeginDrag on the clone - can't actually call it because it would just clone itself again
            AccessTools.Field(typeof(BlockElementScript), "_dragBeginTime").SetValue(clones[0], Time.unscaledTime);
            AccessTools.Field(typeof(BlockElementScript), "_dragTotalDelta").SetValue(clones[0], Vector2.zero);
            AccessTools.Property(typeof(BlockElementScript), "IsDragging").SetValue(clones[0], true);

            // we're taking over this function entirely!
            return false;
        }
    }
}
