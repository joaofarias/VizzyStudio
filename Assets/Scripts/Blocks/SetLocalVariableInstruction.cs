using HarmonyLib;
using ModApi.Craft.Program;
using ModApi.Craft.Program.Instructions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Blocks
{
    [Serializable]
    public class SetLocalVariableInstruction : ProgramInstruction
    {
        [ProgramNodeProperty]
        private string _variable = "var";

        public string VariableName
        {
            get => _variable;
            set => _variable = value;
        }

        public override ProgramInstruction Execute(IThreadContext context)
        {
            ExpressionResult variableValue = GetExpression(0).Evaluate(context);
            context.CreateLocalVariable(VariableName).Value.Set(variableValue);

            return base.Execute(context);
        }
    }
}
