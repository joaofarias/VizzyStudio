using Assets.Scripts.Blocks;
using Assets.Scripts.Vizzy.UI;
using Assets.Scripts.Vizzy.UI.Elements;
using HarmonyLib;
using ModApi.Craft.Program;
using ModApi.Ui;
using UnityEngine;

namespace Assets.Scripts.Patches
{
    [HarmonyPatch(typeof(VariableElementScript))]
    internal static class VariableElementScriptPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch("OnPointerClick")]
        private static void OnPointerClick(VariableElementScript __instance)
        {
            if (__instance.Parent?.Node is SetLocalVariableInstruction setLocalVarInstruction)
            {
                VariableExpression variableExpression = AccessTools.Field(typeof(VariableElementScript), "_expression").GetValue(__instance) as VariableExpression;
                InputDialogScript variableNameInputDialog = VizzyUIController.CreateVariableNameInputDialog(variableExpression.VariableName);
                variableNameInputDialog.MessageText = "RENAME LOCAL VARIABLE";
                variableNameInputDialog.OkayClicked += dialog =>
                {
                    if (string.IsNullOrWhiteSpace(dialog.InputText))
                        return;

                    string oldName = variableExpression.VariableName;
                    variableExpression.VariableName = dialog.InputText;
                    setLocalVarInstruction.VariableName = dialog.InputText;

                    AccessTools.Method(typeof(VariableElementScript), "RenameLocalVariableInScope").Invoke(__instance, new object[] { setLocalVarInstruction, oldName, setLocalVarInstruction.VariableName });

                    foreach (VariableElementScript varScript in __instance.VizzyUI.ProgramTransform.GetComponentsInChildren<VariableElementScript>())
                    {
                        varScript.Parent?.LayoutElement();
                    }

                    dialog.Close();
                };
            }
        }
    }
}
