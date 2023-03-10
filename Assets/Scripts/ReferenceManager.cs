using Assets.Scripts.Vizzy.UI;
using Assets.Scripts.Vizzy.UI.Elements;
using ModApi.Common.Extensions;
using ModApi.Craft.Program;
using ModApi.Craft.Program.Expressions;
using ModApi.Craft.Program.Instructions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace Assets.Scripts
{
    public class ReferenceManager : MonoBehaviour
    {
        public static ReferenceManager Instance { get; private set; }

        public List<Reference> References { get; private set; }

        private VizzyUIController _vizzyController;

        public void Awake()
        {
            References = new List<Reference>();
            _vizzyController = gameObject.GetComponent<VizzyUIController>();

            Instance = this;
        }

        public void Reset()
        {
            References.Clear();
        }

        public bool IsReferenceLoaded(string fileName)
        {
            return References.Any(r => r.FileName == fileName);
        }

        public void AddReference(Reference reference, FlightProgram program = null)
        {
            if (program == null)
            {
                program = _vizzyController.VizzyUI.FlightProgram;
            }

            References.Add(reference);

            reference.CustomInstructions.ForEach(c => program.AddCustomInstruction(c));
            reference.CustomExpressions.ForEach(c => program.AddCustomExpression(c));

            VizzyStudioUI.Instance.RefreshReferences();
        }

        public void RemoveReference(Reference reference)
        {
            List<BlockElementScript> blocks = _vizzyController.ProgramTransform.GetComponentsInChildren<BlockElementScript>(true).ToList();

            StringBuilder output = new StringBuilder();
            foreach (CustomInstruction customInstruction in reference.CustomInstructions)
            {
                int numCalls = blocks.Where(x => x.Node is CallCustomInstruction).Select(x => x.Node as CallCustomInstruction).Distinct().Count(x => x.Call == customInstruction.Name);
                if (numCalls > 0)
                {
                    output.AppendLine($"Custom Instruction '{customInstruction.Name}' is used in the program and cannot be deleted ({numCalls} references).");
                    break;
                }
            }

            foreach (CustomExpression customExpression in reference.CustomExpressions)
            {
                int numCalls = blocks.Where(x => x.Node is CallCustomExpression).Select(x => x.Node as CallCustomExpression).Distinct().Count(x => x.Call == customExpression.Name);
                if (numCalls > 0)
                {
                    output.AppendLine($"Custom Expression '{customExpression.Name}' is used in the program and cannot be deleted ({numCalls} references).");
                    break;
                }
            }

            if (output.Length > 0)
            {
                Game.Instance.UserInterface.CreateMessageDialog(output.ToString());
                return;
            }

            References.Remove(reference);

            FlightProgram program = _vizzyController.VizzyUI.FlightProgram;
            reference.CustomInstructions.ForEach(c => program.RemoveCustomInstruction(c));
            reference.CustomExpressions.ForEach(c => program.RemoveCustomExpression(c));

            VizzyStudioUI.Instance.RefreshReferences();
        }

        public void SaveReferences(XElement programElement)
        {
            XElement referencesElement = new XElement("References");
            foreach (Reference reference in References)
            {
                referencesElement.Add(reference.Serialize());
            }

            programElement.Add(referencesElement);
        }

        public void LoadReferences(XElement programXml, FlightProgram program)
        {
            XElement referencesElement = programXml.Element("References");
            if (referencesElement == null)
            {
                // nothing to do here
                return;
            }

            foreach (XElement referenceElement in referencesElement.Elements("Reference"))
            {
                string fileName = referenceElement.GetStringAttribute("fileName");
                if (File.Exists(fileName))
                {
                    Reference reference = LoadReferenceFromFile(fileName);
                    AddReference(reference, program);
                }
                else
                {
                    // File no longer exists, use our snapshot
                    Reference reference = new Reference();
                    reference.Deserialize(referenceElement);
                    
                    AddReference(reference, program);
                }
            }
        }

        public Reference LoadReferenceFromFile(string fileName)
        {
            XElement programXml = XDocument.Load(fileName).Root;

            Reference reference = new Reference()
            {
                FileName = fileName
            };

            VariableSet variables = new VariableSet(programXml.Element("Variables"));

            foreach (XElement customInstructionElement in programXml.Descendants("CustomInstruction"))
            {
                CustomInstruction customInstruction = ProgramSerializer.DeserializeInstructionSet(customInstructionElement.Parent) as CustomInstruction;
                reference.CustomInstructions.Add(customInstruction);

                // Find all the global variables used by this instruction and grab them as well
                foreach (XElement variableElement in customInstructionElement.Parent.Descendants("Variable"))
                {
                    string variableName = variableElement.GetStringAttribute("variableName");
                    Variable variable = variables.GetVariable(variableName);
                    if (variable != null && reference.Variables.GetVariable(variableName) == null)
                    {
                        reference.Variables.AddVariable(variable);
                    }
                }
            }

            foreach (XElement customExpressionElement in programXml.Descendants("CustomExpression"))
            {
                CustomExpression customExpression = ProgramSerializer.DeserializeProgramNode(customExpressionElement.Parent) as CustomExpression;
                reference.CustomExpressions.Add(customExpression);

                // Find all the global variables used by this instruction and grab them as well
                foreach (XElement variableElement in customExpressionElement.Parent.Descendants("Variable"))
                {
                    string variableName = variableElement.GetStringAttribute("variableName");
                    Variable variable = variables.GetVariable(variableName);
                    if (variable != null && reference.Variables.GetVariable(variableName) == null)
                    {
                        reference.Variables.AddVariable(variable);
                    }
                }
            }

            return reference;
        }

        // This manager won't exist during flight
        public static void LoadReferencesForFlight(XElement programXml, FlightProgram __result)
        {
            XElement referencesElement = programXml.Element("References");
            if (referencesElement == null)
            {
                // nothing to do here
                return;
            }

            foreach (XElement referenceElement in referencesElement.Elements("Reference"))
            {
                Reference reference = new Reference();
                reference.Deserialize(referenceElement);

                __result.RootInstructions.AddRange(reference.CustomInstructions);
                reference.CustomInstructions.ForEach(c => __result.AddCustomInstruction(c));

                __result.RootExpressions.AddRange(reference.CustomExpressions);
                reference.CustomExpressions.ForEach(c => __result.AddCustomExpression(c));

                foreach (Variable variable in reference.Variables.Variables)
                {
                    __result.GlobalVariables.AddVariable(variable);
                }
            }
        }
    }
}
