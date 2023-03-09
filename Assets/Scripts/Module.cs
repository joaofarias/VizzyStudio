using System.Xml.Linq;
using UnityEngine;

namespace Assets.Scripts
{
    public class Module
    {
        public string Name { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Scale { get; set; } = Vector3.one;

        public XElement Serialize()
        {
            XElement xElement = new XElement("Module");
            xElement.SetAttributeValue("name", Name);
            xElement.SetAttributeValue("position", $"{Position.x},{Position.y},{Position.z}");
            xElement.SetAttributeValue("scale", $"{Scale.x},{Scale.y},{Scale.z}");
            return xElement;
        }

        public void Deserialize(XElement moduleElement)
        {
            Name = moduleElement.Attribute("name").Value;

            string position = moduleElement.Attribute("position")?.Value;
            if (position != null)
            {
                string[] tokens = position.Split(',');
                Position = new Vector3(float.Parse(tokens[0]), float.Parse(tokens[1]), float.Parse(tokens[2]));
            }

            string scale = moduleElement.Attribute("scale")?.Value;
            if (scale != null)
            {
                string[] tokens = moduleElement.Attribute("scale").Value.Split(',');
                Scale = new Vector3(float.Parse(tokens[0]), float.Parse(tokens[1]), float.Parse(tokens[2]));
            }
        }
    }
}
