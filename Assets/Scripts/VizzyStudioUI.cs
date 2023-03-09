using Assets.Scripts.Ui;
using Assets.Scripts.Vizzy.UI;
using HarmonyLib;
using ModApi.Ui;
using Rewired.Integration.UnityUI;
using System.Linq;
using System.Net.Configuration;
using System.Reflection;
using System.Xml.Linq;
using UI.Xml;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class VizzyStudioUI : MonoBehaviour
    {
        public static VizzyStudioUI Instance { get; private set; }

        private VizzyUIController _vizzyController;
        private bool _isDraggingBlocks;

        public static void Initialize()
        {
            Game.Instance.UserInterface.AddBuildUserInterfaceXmlAction(UserInterfaceIds.Vizzy, OnBuildVizzyUI);
        }

        private static void OnBuildVizzyUI(BuildUserInterfaceXmlRequest request)
        {
            XElement vizzyStudioXml = XElement.Parse(Game.Instance.UserInterface.ResourceDatabase.GetResource<TextAsset>("Vizzy Studio/VizzyFileExplorer").text);
            XElement vizzyStudioButton = vizzyStudioXml.Descendants().FirstOrDefault(e => (string)e.Attribute("id") == "vizzy-studio-button");
            XElement vizzyStudioPanel = vizzyStudioXml.Descendants().FirstOrDefault(e => (string)e.Attribute("id") == "vizzy-studio");
            XElement dropZoneArea = vizzyStudioXml.Descendants().FirstOrDefault(e => (string)e.Attribute("id") == "modules-drop-zone-area");

            XElement menuButton = request.XmlDocument.Descendants().FirstOrDefault(e => (string)e.Attribute("id") == "menu-button");
            menuButton.AddAfterSelf(vizzyStudioButton);

            XElement menu = request.XmlDocument.Descendants().FirstOrDefault(e => (string)e.Attribute("id") == "menu");
            menu.AddAfterSelf(vizzyStudioPanel);

            XElement trashDropZones = request.XmlDocument.Descendants().FirstOrDefault(e => (string)e.Attribute("id") == "drop-zones");
            trashDropZones.AddBeforeSelf(dropZoneArea);

            // swapt trash icon with text
            foreach (XElement element in trashDropZones.Descendants())
            {
                if (element.Name.ToString().Contains("Image"))
                {
                    element.SetAttributeValue("offsetXY", "0 -20");
                }
                else if (element.Name.ToString().Contains("Text"))
                {
                    element.Attribute("offsetXY").Value = "0 -10";
                    element.SetAttributeValue("alignment", "top");
                }
            }
        }

        private void Awake()
        {
            _vizzyController = gameObject.GetComponent<VizzyUIController>();

            Instance = this;
        }

        private void Update()
        {
            if (_isDraggingBlocks)
            {
                Vector2 mousePosition = UnityEngine.Input.mousePosition;

                float speed = 0.15f;
                float scrollChange = 0f;
                XmlElement arrowUpElement = _vizzyController.xmlLayout.GetElementById("modules-drop-zone-arrow-up");
                if (RectTransformUtility.RectangleContainsScreenPoint(arrowUpElement.rectTransform, mousePosition))
                {
                    scrollChange = 1f;
                }
                else
                {
                    XmlElement arrowDownElement = _vizzyController.xmlLayout.GetElementById("modules-drop-zone-arrow-down");
                    if (RectTransformUtility.RectangleContainsScreenPoint(arrowDownElement.rectTransform, mousePosition))
                    {
                        scrollChange = -1f;
                    }
                }

                if (scrollChange != 0f)
                {
                    XmlElement scrollViewElemnt = _vizzyController.xmlLayout.GetElementById("modules-drop-zone-scroll-view");
                    ScrollRect scrollRect = scrollViewElemnt.gameObject.GetComponent<ScrollRect>();
                    scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition + speed * Time.deltaTime * scrollChange);
                }
            }
        }

        public void Refresh()
        {
            XmlElement modulesPanel = _vizzyController.xmlLayout.GetElementById("modules-node-parent");

            // remove existing modules
            foreach (Transform transform in modulesPanel.transform)
            {
                transform.gameObject.SetActive(false);
                Object.Destroy(transform.gameObject);
            }

            // create modules again
            XmlElement template = _vizzyController.xmlLayout.GetElementById("template-module");
            foreach (Module module in ModuleManager.Instance.Modules.OrderBy(m => m.Name))
            {
                XmlElement panel = UiUtilities.CloneTemplate(template, modulesPanel, true);
                panel.GetElementByInternalId("button-text").SetText(module.Name);
                panel.AddOnClickEvent(() =>
                {
                    ModuleManager.Instance.OpenModule(module);
                });
                panel.Show();

                if (ModuleManager.Instance.ActiveModule == module)
                {
                    panel.AddClass("toggle-button-toggled");
                    panel.GetElementByInternalId("edit-module-button").Show();
                }
                else
                {
                    panel.RemoveClass("toggle-button-toggled");
                    panel.GetElementByInternalId("edit-module-button").Hide();
                }
            }
        }

        public void DisplayModuleDragZones(bool show)
        {
            if (show)
            {
                _vizzyController.xmlLayout.GetElementById("modules-drop-zone-area").Show();
                _isDraggingBlocks = true;

                XmlElement modulesDropZone = _vizzyController.xmlLayout.GetElementById("modules-drop-zone");

                // remove existing modules
                foreach (Transform transform in modulesDropZone.transform)
                {
                    transform.gameObject.SetActive(false);
                    Object.Destroy(transform.gameObject);
                }

                // create modules again
                XmlElement template = _vizzyController.xmlLayout.GetElementById("module-dropzone-template");
                foreach (Module module in ModuleManager.Instance.Modules.OrderBy(m => m.Name))
                {
                    XmlElement moduleDropZone = UiUtilities.CloneTemplate(template, modulesDropZone, true);
                    moduleDropZone.GetElementByInternalId("module-text").SetText(module.Name);

                    if (module == ModuleManager.Instance.ActiveModule)
                    {
                        moduleDropZone.GetElementByInternalId("module-background").SetAndApplyAttribute("color", "Primary");
                    }

                    moduleDropZone.Show();
                }
            }
            else
            {
                _vizzyController.xmlLayout.GetElementById("modules-drop-zone-area").Hide();
                _isDraggingBlocks = false;
            }
        }

        public bool OnDragUpdate(Vector2 position, DragSelection dragSelection)
        {
            XmlElement currentActiveModuleDropZone = null;
            XmlElement hoveredModuleDropZone = null;

            XmlElement modulesDropZone = _vizzyController.xmlLayout.GetElementById("modules-drop-zone");
            foreach (Transform transform in modulesDropZone.transform)
            {
                XmlElement moduleElement = transform.GetComponent<XmlElement>();

                string moduleName = moduleElement.GetElementByInternalId("module-text").GetText();
                if (moduleName == ModuleManager.Instance.ActiveModule.Name)
                {
                    currentActiveModuleDropZone = moduleElement;

                    // if we already have a hovered module, can stop now
                    if (hoveredModuleDropZone != null)
                        break;

                    continue;
                }

                // already have a hovered module so no point checking this module
                if (hoveredModuleDropZone != null)
                    continue;

                // rectangleSize overrides the rectTransform so that only the actual image counts as the rectangle. The transform has 20 extra pixels all around that would trigger this
                Vector4 rectangleSize = new Vector4(100, 100, 100, 100);
                bool isOverModule = RectTransformUtility.RectangleContainsScreenPoint(transform as RectTransform, position, null, rectangleSize);
                if (isOverModule)
                {
                    hoveredModuleDropZone = moduleElement;

                    // if we already have the current module, can stop now
                    if (currentActiveModuleDropZone != null)
                        break;
                }
            }

            if (hoveredModuleDropZone != null)
            {
                // swap backgrounds
                currentActiveModuleDropZone.GetElementByInternalId("module-background").SetAndApplyAttribute("color", "White");
                hoveredModuleDropZone.GetElementByInternalId("module-background").SetAndApplyAttribute("color", "Primary");

                // open hovered module
                string moduleName = hoveredModuleDropZone.GetElementByInternalId("module-text").GetText();
                ModuleManager.Instance.OpenModule(moduleName);

                // Reset potential connections for selection
                dragSelection.TargetConnectionPoints.Clear();
                AccessTools.Method(typeof(DragSelection), "IdentifyTargetConnectionPoints").Invoke(dragSelection, null);

                return true;
            }

            return false;
        }
    }
}
