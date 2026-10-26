namespace Erlang;

internal static class BeamLiteralCallFailureProof
{
    private const int MaximumSteps = 1024;
    private const int MaximumDepth = 16;

    internal static bool Prove(BeamLinkedCode code, BeamLinkedFunction caller, int callPc)
    {
        BeamInstruction call = code.Instructions[callPc];
        int arity = (int)call.Operands[0].Value;
        var arguments = new Term?[arity];
        for (int pc = callPc - 1; pc >= caller.FirstInstructionIndex && arguments.Any(value => value is null); pc--)
        {
            BeamInstruction instruction = code.Instructions[pc];
            if (instruction.Definition.Name == BeamLinkNames.Line)
                continue;
            if (instruction.Definition.Name != BeamLinkNames.Move)
                return false;
            BeamOperand destination = instruction.Operands[1];
            if (destination.Kind != BeamOperandParameters.X || destination.Value >= arity)
                return false;
            int index = (int)destination.Value;
            if (arguments[index] is null)
            {
                arguments[index] = Literal(code, instruction.Operands[0]);
                if (arguments[index] is null)
                    return false;
            }
        }
        if (arguments.Any(value => value is null))
            return false;
        int remaining = MaximumSteps;

        return Fails(
            code,
            (int)call.Operands[1].Value,
            arguments,
            0,
            ref remaining
        );
    }

    private static Term? Literal(BeamLinkedCode code, BeamOperand operand) => operand.Kind switch
    {
        BeamOperandParameters.Integer => code.IntegerConstant(operand.Value),
        BeamOperandParameters.Atom => Term.A(code.Structure.Atoms[(int)operand.Value]!),
        BeamOperandParameters.Nil => Nil.Value,
        BeamOperandParameters.LiteralValue => code.Structure.Literals[(int)operand.Value],
        _ => null
    };

    private static bool Fails(
        BeamLinkedCode code,
        int label,
        Term?[] arguments,
        int depth,
        ref int remaining
    )
    {
        if (depth >= MaximumDepth)
            return false;
        BeamLinkedFunction function = code.Functions.Single(value => value.EntryLabel == label);
        if (function.Arity != arguments.Length)
            return false;
        var registers = new Dictionary<BeamOperand, Term?>();
        for (int index = 0; index < arguments.Length; index++)
            registers[new(BeamOperandParameters.X, index)] = arguments[index];

        Term? Read(BeamOperand operand) => registers.GetValueOrDefault(operand) ?? Literal(code, operand);

        // A bounded proof over concrete immutable literals only. Unknown instructions,
        // predicates, values, returns and exhausted budgets never establish failure.
        // In particular, no guest code or external function runs during admission.
        int pc = function.FirstInstructionIndex;
        while (pc >= function.FirstInstructionIndex && pc < function.EndInstructionIndex && remaining-- > 0)
        {
            BeamInstruction instruction = code.Instructions[pc++];
            var operands = instruction.Operands;
            switch (instruction.Definition.Name)
            {
                case BeamLinkNames.Label:
                case BeamLinkNames.Line:
                case BeamLinkNames.HeapTest:
                    break;
                case BeamLinkNames.Allocate:
                    foreach (BeamOperand register in registers.Keys.Where(value => value.Kind == BeamOperandParameters.Y).ToArray())
                        registers.Remove(register);
                    break;
                case BeamLinkNames.Move:
                    registers[operands[1]] = Read(operands[0]);
                    break;
                case BeamGeneralOpcodeNames.GetList:
                    if (Read(operands[0]) is not Cons list)
                        return false;
                    registers[operands[1]] = list.Head;
                    registers[operands[2]] = list.Tail;
                    break;
                case BeamLinkNames.PutList:
                    if (Read(operands[0]) is not Term head || Read(operands[1]) is not Term tail)
                        return false;
                    registers[operands[2]] = new Cons(head, tail);
                    break;
                case BeamGeneralOpcodeNames.NonemptyListTest:
                case BeamGeneralOpcodeNames.NilTest:
                case BeamLinkNames.EqualityTest:
                    Term? value = Read(operands[1]);
                    if (value is null)
                        return false;
                    bool succeeds;
                    if (instruction.Definition.Name == BeamLinkNames.EqualityTest)
                    {
                        Term? other = Read(operands[2]);
                        if (other is null)
                            return false;
                        succeeds = value.Equals(other);
                    }
                    else
                        succeeds = instruction.Definition.Name == BeamGeneralOpcodeNames.NilTest ? value is Nil : value is Cons;
                    if (!succeeds)
                    {
                        if (operands[0].Value == function.InfoLabel)
                            return true;
                        pc = code.Labels[(int)operands[0].Value];
                    }
                    break;
                case BeamRegisterNames.Call:
                case BeamLinkNames.LocalCall:
                    int arity = (int)operands[0].Value;
                    Term?[] next = Enumerable.Range(0, arity).Select(index => Read(new(BeamOperandParameters.X, index))).ToArray();

                    return Fails(
                        code,
                        (int)operands[1].Value,
                        next,
                        depth + 1,
                        ref remaining
                    );
                default:
                    return false;
            }
        }

        return false;
    }
}
