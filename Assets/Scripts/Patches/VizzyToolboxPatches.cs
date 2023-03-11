using Assets.Scripts.Vizzy.UI;
using HarmonyLib;
using ModApi.Common.Extensions;
using System;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace Assets.Scripts.Patches
{
    [HarmonyPatch(typeof(VizzyToolbox))]
    internal static class VizzyToolboxPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(MethodType.Constructor)]
        [HarmonyPatch(new Type[] { typeof(XElement), typeof(bool) })]
        private static void OnConstructor(XElement xml)
        {
            XElement blocksXml = XElement.Parse(Game.Instance.UserInterface.ResourceDatabase.GetResource<TextAsset>("Vizzy Studio/Blocks").text);
            XElement toolboxStyles = xml.Element("Styles");
            foreach (XElement style in blocksXml.Element("Styles").Elements())
            {
                toolboxStyles.Add(style);
            }

            XElement toolboxCategories = xml.Element("Categories");
            foreach (XElement category in blocksXml.Element("Categories").Elements())
            {
                XElement toolboxCategory = toolboxCategories.Elements().FirstOrDefault(e => e.GetStringAttribute("name") == category.GetStringAttribute("name"));
                if (toolboxCategory != null)
                {
                    foreach (XElement block in category.Elements())
                    {
                        //#TODO: Setting as first is specific to "SetLocalVariable". Add an attribute to specify position
                        toolboxCategory.AddFirst(block);
                    }
                }
            }
        }
    }
}
