using HarmonyLib;
using ModApi.Craft.Program;
using ModApi.Craft.Program.Instructions;
using System.Xml.Linq;
using UnityEngine;

namespace Assets.Scripts.Patches
{
    [HarmonyPatch(typeof(ProgramSerializer))]
    static class ProgramSerializerPatches
    {
        private static bool _canDeserializeNodes = false;

        [HarmonyPostfix]
        [HarmonyPatch(nameof(ProgramSerializer.DeserializeInstructionSet))]
        private static void OnDeserializeInstruction(XElement containerElement, ProgramInstruction __result)
        {
            OnDeserializeNode(containerElement, __result);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(ProgramSerializer.DeserializeProgramNode))]
        private static void OnDeserializeNode(XElement nodeElement, ProgramNode __result)
        {
            if (!Game.InDesignerScene)
                return;

            if (!_canDeserializeNodes)
                return;

            if (nodeElement.Parent == null)
            {
                return;
            }

            if (nodeElement.Parent.Name == "Instructions" || nodeElement.Parent.Name == "Expressions")
            {
                XAttribute moduleAttribute = nodeElement.Attribute("module");
                ModuleManager.Instance.SetModule(__result, moduleAttribute?.Value);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(ProgramSerializer.DeserializeFlightProgram))]
        private static void OnDeserializeFlightProgramPrefix(XElement programXml)
        {
            if (!Game.InDesignerScene)
                return;

            _canDeserializeNodes = true;

            ModuleManager.Instance.LoadModules(programXml);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(ProgramSerializer.DeserializeFlightProgram))]
        private static void OnDeserializeFlightProgramPostfix(XElement programXml, FlightProgram __result)
        {
            if (!Game.InDesignerScene)
                return;

            _canDeserializeNodes = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(ProgramSerializer.SerializeFlightProgram))]
        private static void OnSerializedFlightProgram(XElement __result)
        {
            ModuleManager.Instance.SaveModules(__result);
        }

        [HarmonyPostfix]
        [HarmonyPatch("SerializeProgramNode")]
        private static void OnSerializedFlightProgram(ProgramNode node, XElement parentElement, XElement __result)
        {
            if (parentElement.Name == "Instructions" || parentElement.Name == "Expressions")
            {
                Module module = ModuleManager.Instance.GetModule(node);
                __result.SetAttributeValue("module", module.Name);
            }
        }
    }
}
