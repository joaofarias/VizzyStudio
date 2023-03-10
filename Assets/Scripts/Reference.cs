using ModApi.Craft.Program;
using ModApi.Craft.Program.Expressions;
using ModApi.Craft.Program.Instructions;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Assets.Scripts
{
    public class Reference
    {
        public string FileName { get; set; }
        public List<CustomInstruction> CustomInstructions { get; set; } = new List<CustomInstruction>();
        public List<CustomExpression> CustomExpressions { get; set; } = new List<CustomExpression>();
        public VariableSet Variables { get; set; } = new VariableSet();

        public XElement Serialize()
        {
            XElement referenceElement = new XElement("Reference");
            referenceElement.SetAttributeValue("fileName", FileName);
            referenceElement.Add(Variables.Serialize());

            int _ = 0;

            XElement customInstructionsElement = new XElement("CustomInstructions");
            foreach (CustomInstruction customInstruction in CustomInstructions)
            {
                XElement instructionsElement = new XElement("Instructions");
                ProgramSerializer.SerializeProgramNodes(customInstruction, instructionsElement, ref _, true);
                customInstructionsElement.Add(instructionsElement);
            }
            referenceElement.Add(customInstructionsElement);

            XElement customExpressionsElement = new XElement("CustomExpressions");
            foreach (CustomExpression customExpression in CustomExpressions)
            {
                ProgramSerializer.SerializeProgramNodes(customExpression, customExpressionsElement, ref _, true);
            }
            referenceElement.Add(customExpressionsElement);

            return referenceElement;
        }

        public void Deserialize(XElement referenceElement)
        {
            FileName = referenceElement.Attribute("fileName").Value;
            Variables = new VariableSet(referenceElement.Element("Variables"));

            XElement customInstructionsElement = referenceElement.Element("CustomInstructions");
            foreach (XElement instructionsElement in customInstructionsElement.Elements("Instructions"))
            {
                CustomInstruction customInstruction = ProgramSerializer.DeserializeInstructionSet(instructionsElement) as CustomInstruction;
                CustomInstructions.Add(customInstruction);
            }

            XElement customExpressionsElement = referenceElement.Element("CustomExpressions");
            foreach (XElement customExpressionElement in customExpressionsElement.Elements("CustomExpression"))
            {
                CustomExpression customExpression = ProgramSerializer.DeserializeProgramNode(customExpressionElement) as CustomExpression;
                CustomExpressions.Add(customExpression);
            }
        }
    }
}
