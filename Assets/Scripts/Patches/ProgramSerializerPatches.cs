using Assets.Scripts.Blocks;
using HarmonyLib;
using ModApi.Craft.Program;
using ModApi.Craft.Program.Instructions;
using System;
using System.Reflection;
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
            if (Game.InFlightScene)
            {
                // Deserializing for flight
            }
            else if (Game.InDesignerScene)
            {
                //Deserialize for vizzy
                _canDeserializeNodes = true;

                ModuleManager.Instance.LoadModules(programXml);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(ProgramSerializer.DeserializeFlightProgram))]
        private static void OnDeserializeFlightProgramPostfix(XElement programXml, FlightProgram __result)
        {
            if (Game.InFlightScene)
            {
                ReferenceManager.LoadReferencesForFlight(programXml, __result);
            }
            else if (Game.InDesignerScene)
            {
                //Deserialize for vizzy
                _canDeserializeNodes = false;

                ReferenceManager.Instance.LoadReferences(programXml, __result);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(ProgramSerializer.SerializeFlightProgram))]
        private static void OnSerializedFlightProgram(XElement __result)
        {
            ModuleManager.Instance.SaveModules(__result);
            ReferenceManager.Instance.SaveReferences(__result);
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

        private static bool _initializedBlocks = false;
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ProgramSerializer.CreateProgramNode))]
        private static void OnCreateProgramNode()
        {
            if (_initializedBlocks)
                return;

            _initializedBlocks = true;

            ConstructorInfo programNodeCreatorConstructor = AccessTools.FirstConstructor(AccessTools.TypeByName("ProgramSerializer.ProgramNodeCreator"), c => true);
            ConstructorInfo programNodeCreatorConstructor2 = AccessTools.FirstConstructor(AccessTools.TypeByName("ProgramNodeCreator"), c => true);

            object typeNameLookup = AccessTools.Field(typeof(ProgramSerializer), "_typeNameLookup").GetValue(null);
            object xmlNameLookup = AccessTools.Field(typeof(ProgramSerializer), "_xmlNameLookup").GetValue(null);

            MethodInfo typeNameLookupAddMethod = AccessTools.Method(typeNameLookup.GetType(), "Add");
            MethodInfo xmlNameLookupAddMethod = AccessTools.Method(xmlNameLookup.GetType(), "Add");

            object setLocalVariableCreator = programNodeCreatorConstructor2.Invoke(new object[] { "SetLocalVariable", typeof(SetLocalVariableInstruction), (Func<ProgramNode>)(() => new SetLocalVariableInstruction()) });

            typeNameLookupAddMethod.Invoke(typeNameLookup, new object[] { typeof(SetLocalVariableInstruction).Name, setLocalVariableCreator });
            xmlNameLookupAddMethod.Invoke(xmlNameLookup, new object[] { "SetLocalVariable", setLocalVariableCreator });
        }
    }
}
