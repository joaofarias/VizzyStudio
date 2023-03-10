using Assets.Scripts.Vizzy.UI.Elements;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using Inputs = UnityEngine.Input;

namespace Assets.Scripts.Patches
{
    [HarmonyPatch(typeof(InstructionElementScript))]
    internal static class InstructionElementScriptPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch("DragBegin")]
        private static bool OnDragBegin(InstructionElementScript __instance, ref List<BlockElementScript> __result)
        {
            if (!Inputs.GetKey(UnityEngine.KeyCode.LeftShift))
            {
                return true;
            }

            MethodInfo updateConnectionsMethod = AccessTools.Method(typeof(InstructionElementScript), "UpdateConnectionPoints");

            __instance.OnChildSizeChanged();

            if (__instance.PrevInstruction != null)
            {
                __instance.PrevInstruction.NextInstruction = __instance.NextInstruction;
            }

            if (__instance.NextInstruction != null)
            {
                __instance.NextInstruction.PrevInstruction = __instance.PrevInstruction;
                updateConnectionsMethod.Invoke(__instance.NextInstruction, null);
            }

            if (__instance.ParentInstruction != null)
            {
                __instance.ParentInstruction.ChildInstruction = __instance.ChildInstruction;
            }

            if (__instance.ChildInstruction != null)
            {
                __instance.ChildInstruction.ParentInstruction = __instance.ParentInstruction;
                updateConnectionsMethod.Invoke(__instance.ChildInstruction, null);
            }

            __instance.PrevInstruction = null;
            __instance.NextInstruction = null;
            __instance.ParentInstruction = null;
            __instance.ChildInstruction = null;

            updateConnectionsMethod.Invoke(__instance, null);

            __result = new List<BlockElementScript>() { __instance };

            return false;
        }
    }
}
