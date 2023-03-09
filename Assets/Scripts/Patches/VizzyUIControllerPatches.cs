using Assets.Scripts.Vizzy.UI;
using Assets.Scripts.Vizzy.UI.Elements;
using HarmonyLib;
using ModApi.Craft.Program;
using ModApi.Ui;
using UI.Xml;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Patches
{
    [HarmonyPatch(typeof(VizzyUIController))]
    static class VizzyUIControllerPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch("UpdateCategory")]
        private static bool OnUpdateCategory(VizzyUIController __instance, string categoryId, ref int ____createType)
        {
            // If any other category is being updated, we hide our menu and let the game run as normal
            if (categoryId != "vizzy-studio")
            {
                __instance.xmlLayout.GetElementById("vizzy-studio").Hide();
                __instance.xmlLayout.GetElementById("create-module-button").Hide();
                return true;
            }

            // if we're updating our category, need to close all other panels and don't allow the game to do anything else

            XmlElement toolboxParent = __instance.xmlLayout.GetElementById("toolbox-node-parent");
            foreach (Transform transform in toolboxParent.transform)
            {
                transform.gameObject.SetActive(false);
                Object.Destroy(transform.gameObject);
            }

            __instance.xmlLayout.GetElementById("toolbox").Hide();
            __instance.xmlLayout.GetElementById("menu").Hide();
            __instance.xmlLayout.GetElementById("vizzy-studio").Show();
            __instance.xmlLayout.GetElementById("create-module-button").Show();

            // Reset create type without going through the property
            ____createType = 0; // 0 => None

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("ClosePanel")]
        private static void OnClosePanel(VizzyUIController __instance)
        {
            __instance.xmlLayout.GetElementById("vizzy-studio").Hide();
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnButtonCreateClicked")]
        private static void OnButtonCreateClicked(VizzyUIController __instance, int ____createType)
        {
            if (____createType != 0)
                return;

            ShowModuleDialog(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnEditVariableClicked")]
        private static bool OnEditButtonClicked(VizzyUIController __instance, XmlElement image)
        {
            if (image.internalId != "edit-module-button")
                return true;

            ShowEditDialog(__instance, image);

            return false;
        }

        private static void ShowEditDialog(VizzyUIController controller, XmlElement image)
        {
            string moduleName = image.parentElement.childElements[1].GetText();

            // If we only have 1 module, go straight to renaming screen to prevent deletion
            if (ModuleManager.Instance.Modules.Count == 1)
            {
                ShowModuleDialog(controller, moduleName);
                return;
            }

            MessageDialogScript messageDialog = Game.Instance.UserInterface.CreateMessageDialog(MessageDialogType.ThreeButtons);
            messageDialog.MessageText = "Would you like to rename this module or delete it from the program?";
            messageDialog.MiddleButtonText = "RENAME";
            messageDialog.OkayButtonText = "DELETE";
            messageDialog.UseDangerButtonStyle = true;
            
            messageDialog.MiddleClicked += editDialog =>
            {
                editDialog.Close();
                ShowModuleDialog(controller, moduleName);
            };

            messageDialog.OkayClicked += editDialog =>
            {
                editDialog.Close();
                ModuleManager.Instance.DeleteModule(moduleName);
            };
        }

        private static void ShowModuleDialog(VizzyUIController controller, string currentName = null)
        {
            InputDialogScript inputDialog = Game.Instance.UserInterface.CreateInputDialog(null);
            inputDialog.CancelButtonText = "CANCEL";
            inputDialog.InputPlaceholderText = "Module Name";
            inputDialog.InvalidCharacters.AddRange("!@#$%^&*()-+={}[]|\\:\";',/<>.?".ToCharArray());

            if (currentName == null)
            {
                inputDialog.MessageText = "CREATE MODULE\nOnly letters and numbers are allowed. No special characters.";
                inputDialog.OkayButtonText = "CREATE";
            }
            else
            {
                inputDialog.MessageText = "RENAME MODULE\nOnly letters and numbers are allowed. No special characters.";
                inputDialog.OkayButtonText = "RENAME";
                inputDialog.InputText = currentName;
            }

            inputDialog.OkayClicked += dialog =>
            {
                string inputText = dialog.InputText;
                if (string.IsNullOrWhiteSpace(inputText))
                {
                    MessageDialogScript messageDialog = Game.Instance.UserInterface.CreateMessageDialog();
                    messageDialog.UseDangerButtonStyle = true;
                    messageDialog.MessageText = "The name can't be empty. Please add a name to the module.";
                    return;
                }

                if (!ModuleManager.Instance.IsModuleNameInUse(inputText))
                {
                    if (currentName == null)
                    {
                        ModuleManager.Instance.CreateNewModule(inputText);
                    }
                    else
                    {
                        ModuleManager.Instance.RenameModule(currentName, inputText);
                    }

                    VizzyStudioUI.Instance.Refresh();

                    dialog.Close();
                }
                else
                {
                    MessageDialogScript messageDialog = Game.Instance.UserInterface.CreateMessageDialog();
                    messageDialog.UseDangerButtonStyle = true;
                    messageDialog.MessageText = "A module with that name already exists. Please use a different name.";
                }
            };
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(VizzyUIController.ShowDragUI))]
        private static void OnShowDragUI(VizzyUIController __instance, bool show)
        {
            VizzyStudioUI.Instance.DisplayModuleDragZones(show);
        }
    }
}
