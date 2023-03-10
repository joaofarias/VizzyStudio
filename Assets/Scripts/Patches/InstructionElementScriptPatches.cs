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

            DisconnectBlock(__instance);

            __result = new List<BlockElementScript>() { __instance };

            return false;
        }

        public static void DisconnectBlock(InstructionElementScript block)
        {
            MethodInfo updateConnectionsMethod = AccessTools.Method(typeof(InstructionElementScript), "UpdateConnectionPoints");
            //MethodInfo repositionNextInstructionMethod = AccessTools.Method(typeof(InstructionElementScript), "RepsitionNextInstruction");

            block.OnChildSizeChanged();

            if (block.ParentInstruction != null && block.NextInstruction != null)
            {
                block.ParentInstruction.ChildInstruction = block.NextInstruction;
                block.NextInstruction.PrevInstruction = null;
                block.NextInstruction.ParentInstruction = block.ParentInstruction;
                // no need to call layout as OnChildSizeChanged will do it
            }
            else if (block.ParentInstruction != null)
            {
                block.ParentInstruction.ChildInstruction = null;
                // no need to call layout as OnChildSizeChanged will do it
            }
            else if (block.PrevInstruction != null && block.NextInstruction != null)
            {
                block.PrevInstruction.NextInstruction = block.NextInstruction;
                block.NextInstruction.PrevInstruction = block.PrevInstruction;
                block.PrevInstruction.LayoutElement();
            }
            else if (block.PrevInstruction != null)
            {
                block.PrevInstruction.NextInstruction = null;
                block.PrevInstruction.LayoutElement();
            }
            else if (block.NextInstruction != null)
            {
                block.NextInstruction.PrevInstruction = null;
                block.NextInstruction.LayoutElement();
            }

            block.PrevInstruction = null;
            block.NextInstruction = null;
            block.ParentInstruction = null;

            updateConnectionsMethod.Invoke(block, null);
        }
    }
}
