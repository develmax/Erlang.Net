namespace Erlang;

internal static class BeamRegisterInterpreter
{
    public static ValueTask<Term> Execute(
        ProcessContext context,
        BeamLinkedCode code,
        string name,
        IReadOnlyList<Term> arguments,
        long? diagnosticInstructionBudget = null
    )
        => Execute(
            context,
            code,
            name,
            arguments,
            new(new ModuleCodeImage(code.Structure.Atoms[1]!), code),
            diagnosticInstructionBudget
        );

    internal static ValueTask<Term> Execute(
        ProcessContext context,
        BeamLinkedCode code,
        string name,
        IReadOnlyList<Term> arguments,
        BeamExecutionIdentity execution,
        long? diagnosticInstructionBudget = null
    )
    {
        BeamLinkedFunction function = code.Exports.SingleOrDefault(value => value.Name == name && value.Arity == arguments.Count)
            ?? throw new InvalidDataException(BeamModuleDiagnostics.MissingExport);

        return ExecuteLocal(
            context,
            code,
            function,
            arguments,
            Array.Empty<Term>(),
            execution,
            diagnosticInstructionBudget
        );
    }

    internal static ValueTask<Term> ExecuteLocal(
        ProcessContext context,
        BeamLinkedCode code,
        BeamLinkedFunction function,
        IReadOnlyList<Term> arguments,
        IReadOnlyList<Term> environment,
        long? diagnosticInstructionBudget = null
    )
        => ExecuteLocal(
            context,
            code,
            function,
            arguments,
            environment,
            new(new ModuleCodeImage(code.Structure.Atoms[1]!), code),
            diagnosticInstructionBudget
        );

    internal static async ValueTask<Term> ExecuteLocal(
        ProcessContext context,
        BeamLinkedCode code,
        BeamLinkedFunction function,
        IReadOnlyList<Term> arguments,
        IReadOnlyList<Term> environment,
        BeamExecutionIdentity execution,
        long? diagnosticInstructionBudget = null
    )
    {
        context.Cancellation.ThrowIfCancellationRequested();
        using var lease = await context.Runtime.Modules.AcquireSourceCode(context, execution.Image);
        using var flow = lease.Attach(execution.Owner);
        BeamRegisterValidation.Validate(code, function);
        var registers = new BeamRegisterState();
        for (int index = 0; index < arguments.Count; index++)
        {
            registers.Write(index, arguments[index]);
        }
        for (int index = 0; index < environment.Count; index++)
        {
            registers.Write(arguments.Count + index, environment[index]);
        }
        var continuations = new Stack<BeamCallContinuation>();
        var callFrames = new Stack<BeamCallFrame>();
        var functionFrames = new Stack<IDisposable>();
        IDisposable? functionFrame = null;
        bool entering = true;
        int pc = function.FirstInstructionIndex;
        long diagnosticSteps = 0;

        bool ReturnToCaller()
        {
            functionFrame?.Dispose();
            functionFrame = null;
            if (!continuations.TryPop(out BeamCallContinuation? continuation))
                return false;
            function = continuation.Function;
            pc = continuation.InstructionIndex;
            functionFrame = functionFrames.Pop();

            return true;
        }

        try
        {
            while (true)
            {
                if (entering)
                {
                    functionFrame = context.EnterFunction(function.Module, function.Name, function.Arity);
                    entering = false;
                    if (NifImageBindingStore.TryGetValue(execution.Image, out var bindings))
                    {
                        using var native = bindings!.Admit(function.Name, function.Arity);
                        if (native is not null)
                        {
                            Term[] nativeArguments = Enumerable.Range(0, function.Arity).Select(registers.Read).ToArray();
                            registers.Write(0, await native.InvokeAsync(context, nativeArguments));
                            if (!ReturnToCaller())
                                return registers.Read(0);
                            continue;
                        }
                    }
                }
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
                    case BeamNifNames.Start:
                    case BeamLinkNames.OnLoad:
                        break;
                    case BeamOnLoadNames.InitY:
                        foreach (BeamOperand operand in operands.Skip(BeamFrameParameters.TupleCountPrefix))
                            registers.InitializeY((int)operand.Value);
                        break;
                    case BeamOnLoadNames.BadMatch:
                        throw new ErlangException(Term.Tuple(Term.A(BeamOnLoadNames.BadMatchReason), Read(code, registers, operands[0])));
                    case BeamOnLoadNames.PidTest:
                        if (Read(code, registers, operands[1]) is not Pid)
                            pc = Branch(
                                code,
                                function,
                                operands[0],
                                new ErlangException(ErlangErrorReasons.FunctionClause)
                            );
                        break;
                    case BeamOnLoadNames.TaggedTupleTest:
                        if (Read(code, registers, operands[1]) is not TupleTerm tuple
                            || tuple.Items.Count != operands[2].Value
                            || tuple.Items.Count == 0
                            || !tuple.Items[0].Equals(Read(code, registers, operands[3])))
                            pc = Branch(
                                code,
                                function,
                                operands[0],
                                new ErlangException(ErlangErrorReasons.FunctionClause)
                            );
                        break;
                    case BeamOnLoadNames.TupleElement:
                        registers.Write(operands[2], ((TupleTerm)Read(code, registers, operands[0])).Items[(int)operands[1].Value]);
                        break;
                    case BeamLinkNames.ExternalCall:
                    case BeamOnLoadNames.ExternalLast:
                        BeamExternalImport import = BeamExternalImports.Resolve(code, (int)operands[1].Value);
                        registers.CheckLive(import.Arity);
                        Term[] importedArguments = Enumerable.Range(0, import.Arity).Select(registers.Read).ToArray();
                        BeamCallFrame? externalLastFrame = null;
                        bool sourceLoadNif = BeamExternalImports.IsSourceLoadNif(import);
                        if (instruction.Definition.Name == BeamOnLoadNames.ExternalLast)
                        {
                            if (!callFrames.TryPeek(out externalLastFrame) || externalLastFrame.FunctionEntryLabel != function.EntryLabel
                                || externalLastFrame.ContinuationDepth != continuations.Count || externalLastFrame.Slots != operands[2].Value)
                                throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                            if (!sourceLoadNif)
                            {
                                callFrames.Pop();
                                registers.Deallocate(externalLastFrame.Slots);
                            }
                        }
                        Term imported = await BeamExternalImports.Invoke(context, import, importedArguments);
                        if (sourceLoadNif && externalLastFrame is not null)
                        {
                            callFrames.Pop();
                            registers.Deallocate(externalLastFrame.Slots);
                        }
                        registers.Write(0, imported);
                        if (instruction.Definition.Name == BeamOnLoadNames.ExternalLast)
                        {
                            if (!ReturnToCaller())
                                return imported;
                        }
                        break;
                    case BeamLinkNames.Label:
                    case BeamLinkNames.Line:
                        break;
                    case BeamLinkNames.Allocate:
                    case BeamLinkNames.AllocateHeap:
                        registers.CheckLive((int)operands[instruction.Definition.Name == BeamLinkNames.AllocateHeap ? 2 : 1].Value);
                        continuations.TryPeek(out BeamCallContinuation? savedContinuation);
                        callFrames.Push(new(
                            function.EntryLabel,
                            continuations.Count,
                            savedContinuation,
                            (int)operands[0].Value
                        ));
                        registers.Allocate((int)operands[0].Value);
                        break;
                    case BeamLinkNames.Deallocate:
                        if (!callFrames.TryPop(out BeamCallFrame? frame) || frame.FunctionEntryLabel != function.EntryLabel || frame.ContinuationDepth != continuations.Count || frame.Slots != operands[0].Value)
                            throw new InvalidDataException(BeamReceiveNames.InvalidFrame);
                        registers.Deallocate(frame.Slots);
                        if (frame.SavedContinuation is not null)
                        {
                            _ = continuations.Pop();
                            continuations.Push(frame.SavedContinuation);
                        }
                        break;
                    case BeamLambdaNames.Create:
                        Term[] captured = operands.Skip(BeamLambdaNames.EnvironmentPrefixOperands).Select(operand => Read(code, registers, operand)).ToArray();
                        FunctionTerm closure = BeamClosureFactory.Create(
                            code,
                            (int)operands[0].Value,
                            captured,
                            execution
                        );
                        registers.Write(operands[1], closure);
                        break;
                    case BeamLambdaNames.Call:
                        registers.CheckLive((int)operands[1].Value);
                        Term callee = Read(code, registers, operands[2]);
                        BeamClosureFactory.RequireKnownTarget(callee, code, (int)operands[0].Value);
                        int arity = (int)operands[1].Value;
                        Term[] funArguments = Enumerable.Range(0, arity).Select(registers.Read).ToArray();
                        Term funResult = await ((FunctionTerm)callee).Invoke(context, Array.AsReadOnly(funArguments));
                        registers.Write(0, funResult);
                        break;
                    case BeamLinkNames.Bif:
                        BeamRegisterValidation.SelfImport(code, (int)operands[0].Value);
                        registers.Write(operands[1], await context.Runtime.Modules.Call(context, ErlangModuleNames.Module, ErlangModuleNames.Self));
                        break;
                    case BeamLinkNames.Send:
                        registers.Write(0, ErlangOperatorValues.Send(context, registers.Read(0), registers.Read(1)));
                        break;
                    case BeamLinkNames.LoopReceive:
                        BeamReceivePlan plan = BeamReceivePlanner.Build(code, function, pc - 1);
                        BeamRegisterState before = registers.Clone();
                        BeamReceiveSelection? selected = await context.ReceiveAsync(message => BeamReceiveMatcher.Select(
                            code,
                            plan,
                            before,
                            message
                        ), plan.Timeout);
                        if (selected is null)
                            pc = plan.TimeoutInstructionIndex + 1;
                        else
                        {
                            registers = selected.Registers;
                            pc = selected.NextInstructionIndex;
                        }
                        break;
                    case BeamLinkNames.Move:
                        registers.Write(operands[1], Read(code, registers, operands[0]));
                        break;
                    case BeamLinkNames.Return:
                        Term result = registers.Read(0);
                        if (!ReturnToCaller())
                            return result;
                        break;
                    case BeamRegisterNames.Call:
                    case BeamLinkNames.LocalCall:
                        registers.CheckLive((int)operands[0].Value);
                        if (instruction.Definition.Name == BeamRegisterNames.Call)
                        {
                            continuations.Push(new(function, pc));
                            functionFrames.Push(functionFrame!);
                        }
                        else
                        {
                            functionFrame?.Dispose();
                        }
                        functionFrame = null;
                        entering = true;
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
                        BeamExternalImport tailImport = BeamExternalImports.Resolve(code, (int)operands[1].Value);
                        Term[] tailArguments = Enumerable.Range(0, tailImport.Arity).Select(registers.Read).ToArray();
                        Term externalResult = await BeamExternalImports.Invoke(context, tailImport, tailArguments);
                        registers.Write(0, externalResult);
                        if (!ReturnToCaller())
                            return externalResult;
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
                            registers.Write(operands[5], value);
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
                        registers.Write(operands[2], new Cons(head, tail));
                        break;
                    case BeamLinkNames.PutTuple:
                        Term[] items = operands.Skip(BeamLinkParameters.TupleListPrefixOperands).Select(operand => Read(code, registers, operand)).ToArray();
                        registers.Write(operands[0], new TupleTerm(items));
                        break;
                    default:
                        throw new NotSupportedException(BeamRegisterNames.Unsupported);
                }
            }
        }
        finally
        {
            functionFrame?.Dispose();
            while (functionFrames.TryPop(out var suspendedFrame))
                suspendedFrame.Dispose();
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
        BeamOperandParameters.Y => registers.ReadY((int)operand.Value),
        BeamOperandParameters.Integer => new Integer(operand.Value),
        BeamOperandParameters.Atom => Term.A(code.Structure.Atoms[(int)operand.Value]!),
        BeamOperandParameters.Nil => Nil.Value,
        BeamOperandParameters.LiteralValue => code.Structure.Literals[(int)operand.Value],
        _ => throw new NotSupportedException(BeamRegisterNames.Unsupported)
    };
}
