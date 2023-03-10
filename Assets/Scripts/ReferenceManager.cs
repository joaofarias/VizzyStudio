using Assets.Scripts.Vizzy.UI;
using Assets.Scripts.Vizzy.UI.Elements;
using ModApi.Craft.Program;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using static UnityEngine.Networking.UnityWebRequest;

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

        public void AddReference(Reference reference)
        {
            Debug.Log($"Added reference.");
            References.Add(reference);

            reference.CustomInstructions.ForEach(c => _vizzyController.VizzyUI.FlightProgram.AddCustomInstruction(c));
            reference.CustomExpressions.ForEach(c => _vizzyController.VizzyUI.FlightProgram.AddCustomExpression(c));

            // Refresh UI
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

        public void LoadReferences(XElement programXml)
        {
        }

        // This manager won't exist during flight
        public static void LoadReferencesForFlight(XElement programXml, FlightProgram __result)
        {
            XElement referencesElement = programXml.Element("References");
            if (referencesElement == null )
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
