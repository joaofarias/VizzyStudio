using Assets.Scripts.PlanetStudio.Flyouts.Noise;
using Assets.Scripts.Vizzy.UI;
using Assets.Scripts.Vizzy.UI.Elements;
using ModApi.Craft.Program;
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace Assets.Scripts
{
    public class ModuleManager : MonoBehaviour
    {
        public static ModuleManager Instance { get; private set; }
        public List<Module> Modules { get; private set; }
        public Module ActiveModule { get; private set; }

        private Dictionary<ProgramNode, Module> _nodesToModules;
        private VizzyUIController _vizzyController;

        public void Awake()
        {
            Modules = new List<Module>();
            _nodesToModules = new Dictionary<ProgramNode, Module>();
            _vizzyController = gameObject.GetComponent<VizzyUIController>();
            
            Instance = this;
        }

        public void Reset()
        {
            Modules.Clear();
            ActiveModule = null;
            _nodesToModules.Clear();

            // The game only destroys active gameobjects. We need to destroy the inactive ones ourselves
            foreach (BlockElementScript block in _vizzyController.ProgramTransform.GetComponentsInChildren<BlockElementScript>(true))
            {
                block.Destroy();
            }
        }

        public bool CanRenderNode(ProgramNode node)
        {
            if (_nodesToModules.TryGetValue(node, out Module module))
            {
                return module == ActiveModule;
            }

            return true;
        }

        public bool IsModuleNameInUse(string name)
        {
            return Modules.Any(m => m.Name == name);
        }

        public void CreateNewModule(string name)
        {
            Module newModule = new Module
            {
                Name = name
            };

            Modules.Add(newModule);

            OpenModule(newModule);
        }

        public void RenameModule(string currentName, string newName)
        {
            Module module = Modules.FirstOrDefault(m => m.Name == currentName);
            if (module != null)
            {
                module.Name = newName;
            }
        }

        public void DeleteModule(string name)
        {
            Module module = Modules.FirstOrDefault(m => m.Name == name);
            if (module != null)
            {
                Modules.Remove(module);

                // Delete all the currently active blocks (we only allow deleting a module that is open)
                foreach (Transform blockElementTransform in _vizzyController.ProgramTransform)
                {
                    BlockElementScript blockElement = blockElementTransform.GetComponent<BlockElementScript>();
                    if (blockElement == null)
                        continue;

                    if (blockElement.isActiveAndEnabled)
                    {
                        _nodesToModules.Remove(blockElement.Node);
                        blockElement.gameObject.SetActive(false);
                        blockElement.Destroy();
                    }
                }

                OpenModule(Modules.FirstOrDefault());
            }
        }

        public void SetModule(ProgramNode node, string moduleName)
        {
            if (string.IsNullOrWhiteSpace(moduleName))
            {
                moduleName = ActiveModule.Name;
            }

            Module module = Modules.FirstOrDefault(m => m.Name == moduleName);
            if (module != null)
            {
                _nodesToModules[node] = module;
            }
        }

        public Module GetModule(ProgramNode node)
        {
            if (_nodesToModules.TryGetValue(node, out Module module))
            {
                return module;
            }

            return ActiveModule;
        }

        public void OpenModule(string moduleName)
        {
            Module module = Modules.FirstOrDefault(m => m.Name == moduleName);
            if (module != null)
            {
                OpenModule(module);
            }
        }

        public void OpenModule(Module module)
        {
            if (ActiveModule == module)
                return;

            _vizzyController.VizzyUI.SelectedElement = null;

            ActiveModule.Position = _vizzyController.ProgramTransform.localPosition;
            ActiveModule.Scale = _vizzyController.ProgramTransform.localScale;

            foreach (Transform blockElementTransform in _vizzyController.ProgramTransform)
            {
                BlockElementScript blockElement = blockElementTransform.GetComponent<BlockElementScript>();
                if (blockElement == null)
                    continue;

                if (blockElement.isActiveAndEnabled)
                {
                    _nodesToModules[blockElement.Node] = ActiveModule;
                    blockElement.gameObject.SetActive(false);
                }
                else if (_nodesToModules.TryGetValue(blockElement.Node, out Module nodeModule) && nodeModule == module)
                {
                    blockElement.gameObject.SetActive(true);
                }
            }

            _vizzyController.ProgramTransform.localPosition = module.Position;
            _vizzyController.ProgramTransform.localScale = module.Scale;
            _vizzyController.DragTransform.localScale = module.Scale;

            ActiveModule = module;
            VizzyStudioUI.Instance.Refresh();
        }

        public void LoadModules(XElement programElement)
        {
            // if the element is null, this program does not have modules yet
            XElement modulesElement = programElement.Element("Modules");
            if (modulesElement == null)
            {
                string programName = programElement.Attribute("name").Value;
                Module module = new Module
                {
                    Name = programName
                };

                SanitizeName(module);

                Modules.Add(module);

                if (ActiveModule == null)
                {
                    ActiveModule = module;
                    VizzyStudioUI.Instance.Refresh();
                }
                else
                {
                    OpenModule(module);
                }

                return;
            }

            XAttribute activeModuleAttribute = programElement.Attribute("activeModule");
            string activeModuleName = activeModuleAttribute.Value;

            Module activeModule = null;
            foreach (XElement moduleElement in modulesElement.Elements())
            {
                Module module = new Module();
                module.Deserialize(moduleElement);

                //#TODO: we can't simply change the name here. We'll need to update it in every node we're about to deserialize
                //SanitizeName(module);
                if (Modules.Any(m => m.Name == module.Name))
                {
                    continue;
                }

                Modules.Add(module);

                if (module.Name == activeModuleName)
                {
                    activeModule = module;
                }
            }

            if (activeModule != null)
            {
                if (ActiveModule == null)
                {
                    ActiveModule = activeModule;
                    VizzyStudioUI.Instance.Refresh();
                }
                else
                {
                    OpenModule(activeModule);
                }
            }
        }

        private void SanitizeName(Module module)
        {
            string originalName = module.Name;
            int count = 2;
            while (Modules.Any(m => m.Name == module.Name))
            {
                module.Name = $"{originalName}_{count++}";
            }
        }

        public void SaveModules(XElement programElement)
        {
            // Store the last open module on the program node
            programElement.SetAttributeValue("activeModule", ActiveModule.Name);

            XElement modulesElement = new XElement("Modules");
            foreach (Module module in Modules)
            {
                modulesElement.Add(module.Serialize());
            }

            programElement.Add(modulesElement);
        }
    }
}
