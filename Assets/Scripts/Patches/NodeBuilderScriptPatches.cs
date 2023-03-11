using Assets.Scripts.Blocks;
using Assets.Scripts.Vizzy.UI;
using HarmonyLib;
using ModApi.Craft.Program;
using ModApi.Craft.Program.Instructions;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Assets.Scripts.Patches
{
    [HarmonyPatch(typeof(NodeBuilderScript))]
    internal static class NodeBuilderScriptPatches
    {
        // The variable name in SetLocalVariable block isn't automatically set. Need to fix that up here in the same way the game handles the 'i' variable in the ForInstruction block
        [HarmonyTranspiler]
        [HarmonyPatch("BuildChildrenBlocks")]
        static IEnumerable<CodeInstruction> OnBuildChildrenBlocksTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

            // Find line "if (node is ForInstruction)"
            int index = codes.FindIndex(c => c.Is(OpCodes.Isinst, typeof(ForInstruction)));

            // Find line "variableExpression.IsDefinition = true;"
            int finalJumpIndex = codes.FindIndex(c => c.Is(OpCodes.Callvirt, AccessTools.Method(typeof(VariableExpression), "set_IsDefinition")));

            if (index == -1 || finalJumpIndex == -1)
                return instructions;

            // Go back once to "ldarg.1" which is loading the argument 'node' for comparison in the 'if' statement
            // This is where we insert our instructions and also where we jump to if node isn't of type SetLocalVariable
            --index;

            // if our 'if' statement is false, we want to jump to the original 'if' statement
            Label jumpLabel = generator.DefineLabel();
            codes[index].labels.Add(jumpLabel);

            // if our 'if' statement is true, we're gonna want to jump to the end of the if/else clauses
            // Go back twice which are opCodes to load variables for the "set_IsDefinition" call
            Label finalJumpLabel = generator.DefineLabel();
            codes[finalJumpIndex - 2].labels.Add(finalJumpLabel);

            // We need a new local variable to store the casted node
            LocalBuilder localVar = generator.DeclareLocal(typeof(SetLocalVariableInstruction));

            //Insert our instructions

            // Load argument 'node' into stack
            codes.Insert(index++, new CodeInstruction(OpCodes.Ldarg_1));
            // Check if 'node' is of type SetLocalVariableInstruction
            codes.Insert(index++, new CodeInstruction(OpCodes.Isinst, typeof(SetLocalVariableInstruction)));
            // if it isn't, we jump to the to the starting point "if (node is ForInstruction)"
            codes.Insert(index++, new CodeInstruction(OpCodes.Brfalse_S, jumpLabel));
            // Load argument 'node' into stack again since it has been consumed by the 'if' statement
            codes.Insert(index++, new CodeInstruction(OpCodes.Ldarg_1));
            // Cast the 'node' into SetLocalVariableInstruction and place it in the stack (note: opCode is the same as before but here it's effectively the "as" operator)
            codes.Insert(index++, new CodeInstruction(OpCodes.Isinst, typeof(SetLocalVariableInstruction)));
            // Grab the value from the stack and store it on our new local variable
            codes.Insert(index++, new CodeInstruction(OpCodes.Stloc_S, localVar));
            // Place the local variable 'variableExpression' in the stack
            codes.Insert(index++, new CodeInstruction(OpCodes.Ldloc_S, 14));
            // Place our new local variable in the stack (they need to be in this order for the following operations)
            codes.Insert(index++, new CodeInstruction(OpCodes.Ldloc_S, localVar));
            // Call property getter 'SetLocalVariableInstruction.VariableName'
            codes.Insert(index++, new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(SetLocalVariableInstruction), "get_VariableName")));
            // Call property setter 'VariableExpression.VariableName' with result from previous call
            codes.Insert(index++, new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(VariableExpression), "set_VariableName")));
            // Jump over all the remaining if/else clauses
            codes.Insert(index++, new CodeInstruction(OpCodes.Br_S, finalJumpLabel));

            // Result:
            //if (node is SetLocalVariableInstruction)
            //{
            //    SetLocalVariableInstruction instruction = node as SetLocalVariableInstruction;
            //    variableExpression.VariableName = instruction.VariableName;
            //}
            //else if (node is ForInstruction) <-- entry point from original method

            return codes;
        }
    }
}
