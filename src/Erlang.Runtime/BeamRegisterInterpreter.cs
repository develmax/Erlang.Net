namespace Erlang;

internal static class BeamRegisterInterpreter
{
    public static async ValueTask<Term> Execute(
        ProcessContext context,
        BeamLinkedCode code,
        string name,
        IReadOnlyList<Term> arguments,
        long? diagnosticInstructionBudget = null
    )
    {
        BeamLinkedFunction function = code.Exports.SingleOrDefault(value => value.Name == name && value.Arity == arguments.Count)
            ?? throw new InvalidDataException(BeamModuleDiagnostics.MissingExport);
        BeamRegisterValidation.Validate(code, function);
        var registers = new BeamRegisterState();
        for (int index = 0; index < arguments.Count; index++)
        {
            registers.Write(index, arguments[index]);
        }
        var continuations = new Stack<BeamCallContinuation>();
        int pc = function.FirstInstructionIndex;
        long diagnosticSteps = 0;
        while (true)
        {
            await context.ReduceAsync();
            if (diagnosticInstructionBudget is { } budget && diagnosticSteps++ >= budget)
            {
                throw new InvalidOperationException(BeamRegisterNames.DiagnosticBudgetExceeded);
            }
            if (pc < function.FirstInstructionIndex || pc >= function.EndInstructionIndex)
            {
                throw new InvalidDataException(BeamRegisterNames.Unsupported);
            }
            BeamInstruction instruction = code.Instructions[pc++];
            var operands = instruction.Operands;
            switch (instruction.Definition.Name)
            {
                case BeamLinkNames.Label:
                case BeamLinkNames.Line:
                    break;
                case BeamLinkNames.Move:
                    registers.Write((int)operands[1].Value, Read(code, registers, operands[0]));
                    break;
                case BeamLinkNames.Return:
                    Term result = registers.Read(0);
                    if (!continuations.TryPop(out BeamCallContinuation? continuation))
                    {
                        return result;
                    }
                    function = continuation.Function;
                    pc = continuation.InstructionIndex;
                    break;
                case BeamRegisterNames.Call:
                case BeamLinkNames.LocalCall:
                    registers.CheckLive((int)operands[0].Value);
                    if (instruction.Definition.Name == BeamRegisterNames.Call)
                    {
                        continuations.Push(new(function, pc));
                    }
                    function = code.Functions.Single(target => target.EntryLabel == operands[1].Value);
                    pc = function.FirstInstructionIndex;
                    break;
                case BeamLinkNames.IntegerTest:
                    if (Read(code, registers, operands[1]) is not Integer)
                    {
                        pc = Branch(
                            code,
                            function,
                            operands[0],
                            new ErlangException(ErlangErrorReasons.FunctionClause)
                        );
                    }
                    break;
                case BeamLinkNames.EqualityTest:
                    if (!Read(code, registers, operands[1]).Equals(Read(code, registers, operands[2])))
                    {
                        pc = Branch(
                            code,
                            function,
                            operands[0],
                            new ErlangException(ErlangErrorReasons.FunctionClause)
                        );
                    }
                    break;
                case BeamLinkNames.ExternalTailCall:
                    registers.CheckLive((int)operands[0].Value);
                    string externalArithmetic = BeamRegisterValidation.ArithmeticImport(code, (int)operands[1].Value);
                    Term externalResult = await context.Runtime.Modules.Call(
                        context,
                        BeamRegisterNames.Erlang,
                        externalArithmetic,
                        registers.Read(0),
                        registers.Read(1)
                    );
                    registers.Write(0, externalResult);
                    if (!continuations.TryPop(out BeamCallContinuation? externalContinuation))
                    {
                        return externalResult;
                    }
                    function = externalContinuation.Function;
                    pc = externalContinuation.InstructionIndex;
                    break;
                case BeamLinkNames.GuardBif:
                    registers.CheckLive((int)operands[1].Value);
                    string arithmetic = BeamRegisterValidation.ArithmeticImport(code, (int)operands[2].Value);
                    try
                    {
                        Term value = await context.Runtime.Modules.Call(
                            context,
                            BeamRegisterNames.Erlang,
                            arithmetic,
                            Read(code, registers, operands[3]),
                            Read(code, registers, operands[4])
                        );
                        registers.Write((int)operands[5].Value, value);
                    }
                    catch (ErlangException error)
                    {
                        pc = Branch(
                            code,
                            function,
                            operands[0],
                            error
                        );
                    }
                    break;
                case BeamLinkNames.HeapTest:
                    registers.CheckLive((int)operands[1].Value);
                    break;
                case BeamLinkNames.PutList:
                    Term head = Read(code, registers, operands[0]);
                    Term tail = Read(code, registers, operands[1]);
                    registers.Write((int)operands[2].Value, new Cons(head, tail));
                    break;
                case BeamLinkNames.PutTuple:
                    Term[] items = operands.Skip(BeamLinkParameters.TupleListPrefixOperands).Select(operand => Read(code, registers, operand)).ToArray();
                    registers.Write((int)operands[0].Value, new TupleTerm(items));
                    break;
                default:
                    throw new NotSupportedException(BeamRegisterNames.Unsupported);
            }
        }

    }

    private static int Branch(
        BeamLinkedCode code,
        BeamLinkedFunction function,
        BeamOperand target,
        ErlangException error
    )
    {
        if (target.Kind == BeamOperandParameters.Failure)
        {
            throw error;
        }
        if (target.Value == function.InfoLabel)
        {
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        }

        return target.Value == function.EntryLabel ? function.FirstInstructionIndex : code.Labels[(int)target.Value];
    }

    private static Term Read(BeamLinkedCode code, BeamRegisterState registers, BeamOperand operand) => operand.Kind switch
    {
        BeamOperandParameters.X => registers.Read((int)operand.Value),
        BeamOperandParameters.Integer => new Integer(operand.Value),
        BeamOperandParameters.Atom => Term.A(code.Structure.Atoms[(int)operand.Value]!),
        BeamOperandParameters.Nil => Nil.Value,
        BeamOperandParameters.LiteralValue => code.Structure.Literals[(int)operand.Value],
        _ => throw new NotSupportedException(BeamRegisterNames.Unsupported)
    };
}
