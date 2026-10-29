namespace Erlang;

internal static partial class BeamRegisterInterpreter
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

    internal static ValueTask<Term> ExecuteLocal(
        ProcessContext context,
        BeamLinkedCode code,
        BeamLinkedFunction function,
        IReadOnlyList<Term> arguments,
        IReadOnlyList<Term> environment,
        BeamExecutionIdentity execution,
        long? diagnosticInstructionBudget = null
    )
        => Dispatch432(
            context,
            code,
            function,
            arguments,
            environment,
            execution,
            SourceCodeAdmissionKind.Continuation,
            diagnosticInstructionBudget
        );

    internal static ValueTask<Term> ExecuteFunction(
        ProcessContext context,
        BeamLinkedCode code,
        BeamLinkedFunction function,
        IReadOnlyList<Term> arguments,
        IReadOnlyList<Term> environment,
        BeamExecutionIdentity execution,
        long? diagnosticInstructionBudget = null,
        LocalFunctionInvocation? invocation = null,
        SourceCodeLease? admittedLease647 = null
    )
        => Dispatch432(
            context,
            code,
            function,
            arguments,
            environment,
            execution,
            SourceCodeAdmissionKind.Function,
            diagnosticInstructionBudget,
            invocation,
            admittedLease647
        );

    private static async ValueTask<Term> ExecuteCore(
        ProcessContext context,
        BeamLinkedCode code,
        BeamLinkedFunction function,
        IReadOnlyList<Term> arguments,
        IReadOnlyList<Term> environment,
        BeamExecutionIdentity execution,
        SourceCodeAdmissionKind admission,
        long? diagnosticInstructionBudget,
        LocalFunctionInvocation? invocation = null,
        SourceCodeLease? admittedLease647 = null
    )
    {
        context.Cancellation.ThrowIfCancellationRequested();
        using var debugExecution432 = new DebugBreakpointExecution432();
        using var lease = admittedLease647 ?? await context.Runtime.Modules.AcquireSourceEntry(
            context,
            execution.Image,
            admission,
            invocation
        );
        using var flow = lease.Attach(execution.Owner);
        BeamRegisterValidation.Validate(code, function);
        context.Runtime.RegisterLoaderMapKeys565(code);
        await using var wordExecution504 = new ProcessWordExecution504(context.OwnedWords504);
        var registers = new BeamRegisterState(wordExecution504, context);
        // Admission/import can fail before the ordinary interpreter try/finally.
        // This guard covers imports before the interpreter try/finally.
        using var ownedSetup507 = registers.HasOwnedRoots507 ? registers : null;
        IReadOnlyList<Term> instructionLocations = BeamInstructionLocations.Read(code);
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
        var functionFrames = new Stack<BeamFunctionScope>();
        var handlers = new Stack<BeamHandlerTag>();
        BeamHandlerTag? pendingHandler = null;
        BeamFunctionScope? functionFrame = null;
        bool entering = true;
        int pc = function.FirstInstructionIndex;
        long diagnosticSteps = 0;

        int Branch(
            BeamLinkedCode currentCode,
            BeamLinkedFunction currentFunction,
            BeamOperand target,
            ErlangException error
        )
        {
            if (target.Kind != BeamOperandParameters.Failure && target.Value == currentFunction.InfoLabel
                && currentCode.SourceLocations.Count != 0)
                functionFrame!.SetLocation(instructionLocations[currentCode.Labels[currentFunction.EntryLabel]]);

            return BranchTarget(
                currentCode,
                currentFunction,
                target,
                error
            );
        }

        bool ReturnToCaller()
        {
            registers.ClearFloats();
            functionFrame?.Dispose();
            functionFrame = null;
            if (!continuations.TryPop(out BeamCallContinuation? continuation))
                return false;
            function = continuation.Function;
            pc = continuation.InstructionIndex;
            functionFrame = functionFrames.Pop();
            functionFrame.ClearContinuation();

            return true;
        }

        void TransferModuleApply()
        {
            // The outer async execution context still needs its scope when it unwinds.
            functionFrame?.Dispose();
            if (continuations.Count == 0 && handlers.Count == 0 && callFrames.Count == 0)
            {
                flow.Dispose();
                lease.Dispose();
            }
        }

        try
        {
            while (true)
            {
                try
                {
                    if (entering)
                    {
                        BeamBinaryEntryProtocol.RequireEntry(code, function, registers);
                        if (execution.NativePrimitives.TryGetValue((function.Name, function.Arity), out ErlangFunction? primitive))
                        {
                            Term[] primitiveArguments = Enumerable.Range(0, function.Arity).Select(registers.Read).ToArray();
                            using (context.EnterBeamNative(function.Module, function.Name, function.Arity))
                                registers.Write(
                                    0,
                                    await RuntimeExceptionCapture.InvokeNative(
                                        context,
                                        registers.HasOwnedRoots507 ? OwnedPrimitive507(execution, primitive) : primitive,
                                        function.Module,
                                        function.Name,
                                        primitiveArguments
                                    )
                                );
                            entering = false;
                            if (!ReturnToCaller())
                                return registers.CompleteGuestReturn507(registers.Read(0));
                            continue;
                        }
                        functionFrame = new BeamFunctionScope(context, function, Enumerable.Range(0, function.Arity).Select(registers.CaptureArgument).ToArray());
                        int entryLine = function.FirstInstructionIndex - 3;
                        if (entryLine >= 0 && code.Instructions[entryLine].Definition.Name == BeamLinkNames.Line && code.SourceLocations.Count != 0)
                        {
                            int location = (int)code.Instructions[entryLine].Operands[0].Value;
                            if (location >= code.SourceLocations.Count)
                                throw new InvalidDataException(BeamLineNames.Invalid);
                            functionFrame.SetLocation(code.SourceLocations[location]);
                        }
                        if (context.Runtime.Modules.HasDebugBreakpoint432(execution.Image, function.EntryLabel))
                            await context.Runtime.EnterDebugBreakpoint432(
                                context,
                                execution.Image,
                                function,
                                Enumerable.Range(0, function.Arity).Select(registers.Read).ToArray(),
                                debugExecution432
                            );
                        entering = false;
                        if (NifImageBindingStore.TryGetValue(execution.Image, out var bindings))
                        {
                            using var native = bindings!.Admit(function.Name, function.Arity);
                            if (native is not null)
                            {
                                Term[] nativeArguments = Enumerable.Range(0, function.Arity).Select(registers.Read).ToArray();
                                if (registers.HasOwnedRoots507 && native.GuestNative535)
                                    registers.Write(0, await context.InvokeImageNif535(native, nativeArguments));
                                else
                                {
                                    nativeArguments = registers.ExportArguments507(nativeArguments);
                                    registers.Write(0, await native.InvokeAsync(context, nativeArguments));
                                }
                                if (!ReturnToCaller())
                                    return registers.CompleteGuestReturn507(registers.Read(0));
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
                    if (code.SourceLocations.Count != 0)
                        functionFrame!.SetLocation(instructionLocations[pc]);
                    BeamInstruction instruction = code.Instructions[pc++];
                    var operands = instruction.Operands;
                    if (BeamCollectionOpcodeNames.ClobbersFloats(instruction.Definition.Name))
                        registers.ClearFloats();
                    switch (instruction.Definition.Name)
                    {
                        case BeamRecordConstructionNames.Opcode:
                            registers.Write(operands[BeamRecordConstructionNames.Destination], BeamRecordConstruction.Create(code, registers, operands));
                            break;
                        case BeamCollectionOpcodeNames.UpdateRecord:
                            registers.Write(operands[3], BeamCollectionExecution.UpdateRecord(code, registers, operands));
                            break;
                        case BeamCollectionOpcodeNames.BadRecord:
                            throw new ErlangException(Term.Tuple(Term.A(BeamCollectionOpcodeNames.BadRecord), Read(code, registers, operands[0])));
                        case BeamCollectionOpcodeNames.FloatMove:
                            if (operands[0].Kind == BeamOperandParameters.Float)
                                registers.Write(operands[1], new FloatTerm(registers.ReadFloat(operands[0])));
                            else if (Read(code, registers, operands[0]) is FloatTerm floatSource)
                                registers.WriteFloat(operands[1], floatSource.Value);
                            else
                                throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                            break;
                        case BeamCollectionOpcodeNames.FloatConvert:
                            registers.WriteFloat(operands[1], BeamCollectionExecution.ToFloat(Read(code, registers, operands[0])));
                            break;
                        case BeamCollectionOpcodeNames.FloatNegate:
                            registers.WriteFloat(operands[2], -registers.ReadFloat(operands[1]));
                            break;
                        case BeamCollectionOpcodeNames.FloatMultiply:
                        case BeamCollectionOpcodeNames.FloatAdd:
                        case BeamCollectionOpcodeNames.FloatSubtract:
                        case BeamCollectionOpcodeNames.FloatDivide:
                            double leftFloat = registers.ReadFloat(operands[1]);
                            double rightFloat = registers.ReadFloat(operands[2]);
                            registers.WriteFloat(
                                operands[3],
                                instruction.Definition.Name switch
                                {
                                    BeamCollectionOpcodeNames.FloatAdd => leftFloat + rightFloat,
                                    BeamCollectionOpcodeNames.FloatSubtract => leftFloat - rightFloat,
                                    BeamCollectionOpcodeNames.FloatDivide => leftFloat / rightFloat,
                                    _ => leftFloat * rightFloat
                                }
                            );
                            break;

                        case BeamGeneralOpcodeNames.NilTest:
                        case BeamGeneralOpcodeNames.NonemptyListTest:
                        case BeamGeneralOpcodeNames.ListTest:
                        case BeamGeneralOpcodeNames.TupleTest:
                        case BeamGeneralOpcodeNames.FloatTest:
                        case BeamGeneralOpcodeNames.MapTest:
                        case BeamGeneralOpcodeNames.NumberTest:
                        case BeamGeneralOpcodeNames.BinaryTest:
                        case BeamGeneralOpcodeNames.AtomTest:
                        case BeamGeneralOpcodeNames.BooleanTest:
                        case BeamGeneralOpcodeNames.ReferenceTest:
                        case BeamGeneralOpcodeNames.AnyFunctionTest:
                        case BeamGeneralOpcodeNames.BitStringTest:
                        case BeamGeneralOpcodeNames.PortTest:
                        case BeamGeneralOpcodeNames.NativeRecordTest:
                        case BeamGeneralOpcodeNames.TupleArity:
                        case BeamGeneralOpcodeNames.FunctionTest:
                        case BeamGeneralOpcodeNames.ArithmeticEqual:
                        case BeamGeneralOpcodeNames.ArithmeticNotEqual:
                        case BeamGeneralOpcodeNames.ExactNotEqual:
                        case BeamGeneralOpcodeNames.GreaterOrEqual:
                        case BeamGeneralOpcodeNames.Less:
                            if (!BeamGeneralTests.Match(code, registers, instruction))
                                pc = Branch(
                                    code,
                                    function,
                                    operands[0],
                                    new ErlangException(ErlangErrorReasons.FunctionClause)
                                );
                            break;
                        case BeamGeneralOpcodeNames.GetList:
                        case BeamGeneralOpcodeNames.GetHead:
                        case BeamGeneralOpcodeNames.GetTail:
                            {
                                if (Read(code, registers, operands[0]) is not Cons cell)
                                    throw new InvalidDataException(BeamRegisterNames.Unsupported);
                                registers.WriteListParts550(
                                    instruction.Definition.Name,
                                    operands[0],
                                    operands[1],
                                    instruction.Definition.Name == BeamGeneralOpcodeNames.GetList ? operands[2] : null,
                                    cell
                                );
                                break;
                            }
                        case BeamGeneralOpcodeNames.Swap:
                            registers.SwapRegisters(operands[0], operands[1]);
                            break;
                        case BeamGeneralOpcodeNames.Trim:
                            if (!callFrames.TryPeek(out BeamCallFrame? trimFrame) || trimFrame.FunctionEntryLabel != function.EntryLabel
                                || trimFrame.ContinuationDepth != continuations.Count || trimFrame.Slots != operands[0].Value + operands[1].Value)
                                throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                            registers.Trim((int)operands[0].Value, (int)operands[1].Value);
                            callFrames.Pop();
                            callFrames.Push(trimFrame with { Slots = (int)operands[1].Value });
                            break;
                        case BeamGeneralOpcodeNames.Jump:
                            pc = Branch(
                                code,
                                function,
                                operands[0],
                                new ErlangException(ErlangErrorReasons.FunctionClause)
                            );
                            break;
                        case BeamGeneralOpcodeNames.SelectValue:
                            Term selectedValue = Read(code, registers, operands[0]);
                            BeamOperand selectedTarget = operands[1];
                            for (int choice = 3; choice < operands.Count; choice += 2)
                            {
                                if (!selectedValue.Equals(Read(code, registers, operands[choice])))
                                    continue;
                                selectedTarget = operands[choice + 1];
                                break;
                            }
                            pc = Branch(
                                code,
                                function,
                                selectedTarget,
                                new ErlangException(ErlangErrorReasons.FunctionClause)
                            );
                            break;
                        case BeamGeneralOpcodeNames.SelectTupleArity:
                            pc = Branch(
                                code,
                                function,
                                BeamTupleSelectValidation.Select(Read(code, registers, operands[0]), operands),
                                new ErlangException(ErlangErrorReasons.FunctionClause)
                            );
                            break;
                        case BeamGeneralOpcodeNames.MapFields:
                            if (Read(code, registers, operands[1]) is not MapTerm fieldsMap)
                                throw new InvalidDataException(BeamLinkDiagnostics.OperandRole);
                            if (operands.Skip(3).Any(key => !fieldsMap.TryGet(Read(code, registers, key), out _)))
                                pc = Branch(
                                    code,
                                    function,
                                    operands[0],
                                    new ErlangException(ErlangErrorReasons.FunctionClause)
                                );
                            break;
                        case BeamGeneralOpcodeNames.MapElements:
                            {
                                if (Read(code, registers, operands[1]) is not MapTerm elementMap)
                                    throw new InvalidDataException(BeamLinkDiagnostics.OperandRole);
                                using var extractedRoots507 = registers.HasOwnedRoots507 ? registers.Protect507(elementMap) : null;
                                var extracted = new List<(BeamOperand Destination, Term Value)>();
                                bool missingElement = false;
                                for (int pair = 3; pair < operands.Count; pair += 2)
                                {
                                    if (!elementMap.TryGet(Read(code, registers, operands[pair]), out Term? element))
                                    {
                                        missingElement = true;
                                        break;
                                    }
                                    extracted.Add((operands[pair + 1], element!));
                                }
                                if (missingElement)
                                    pc = Branch(
                                        code,
                                        function,
                                        operands[0],
                                        new ErlangException(ErlangErrorReasons.FunctionClause)
                                    );
                                else
                                    foreach (var element in extracted)
                                        registers.Write(element.Destination, element.Value);
                                break;
                            }
                        case BeamGeneralOpcodeNames.MapExact:
                        case BeamGeneralOpcodeNames.MapAssociate:
                            registers.CheckLive((int)operands[3].Value);
                            if (Read(code, registers, operands[1]) is not MapTerm sourceMap)
                                throw new InvalidDataException(BeamLinkDiagnostics.OperandRole);
                            var entries = sourceMap.Entries.ToList();
                            bool missingExact = false;
                            for (int pair = 5; pair < operands.Count; pair += 2)
                            {
                                Term key = Read(code, registers, operands[pair]);
                                if (instruction.Definition.Name == BeamGeneralOpcodeNames.MapExact && !sourceMap.TryGet(key, out _))
                                {
                                    pc = Branch(
                                        code,
                                        function,
                                        operands[0],
                                        new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadKey), key))
                                    );
                                    missingExact = true;
                                    break;
                                }
                                entries.Add(new(key, Read(code, registers, operands[pair + 1])));
                            }
                            if (!missingExact)
                            {
                                var constructedMap = new MapTerm(entries);
                                if (instruction.Definition.Name == BeamGeneralOpcodeNames.MapAssociate
                                    && code.LoaderMapKeys565.TryGetValue(instruction.Offset, out TupleTerm? literalKeys))
                                {
                                    MapTerm bindings = constructedMap;
                                    constructedMap = new MapTerm(literalKeys.Items.Select(key =>
                                        new KeyValuePair<Term, Term>(key, bindings.Get(key))), literalKeys);
                                }

                                registers.Write(operands[2], constructedMap);
                            }
                            break;
                        case BeamGeneralOpcodeNames.BinaryBif:
                            BeamExternalImport binaryImport = BeamRegisterValidation.BinaryImport(code, (int)operands[1].Value);
                            Term[] binaryArguments = [Read(code, registers, operands[2]), Read(code, registers, operands[3])];
                            try
                            {
                                Term binaryValue = BeamInfallibleBifContract.TryInvoke(
                                    context,
                                    code,
                                    instruction,
                                    binaryArguments,
                                    out Term canonicalBinary
                                )
                                    ? canonicalBinary : await BeamExternalImports.Invoke(context, binaryImport, binaryArguments);
                                registers.Write(operands[4], binaryValue);
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
                        case BeamGeneralOpcodeNames.UnaryBif:
                        case BeamGeneralOpcodeNames.UnaryGcBif:
                            bool unaryGc = instruction.Definition.Name == BeamGeneralOpcodeNames.UnaryGcBif;
                            int unaryIndex = unaryGc ? 2 : 1;
                            if (unaryGc)
                                registers.CheckLive((int)operands[1].Value);
                            BeamExternalImport unaryImport = BeamRegisterValidation.UnaryImport(code, (int)operands[unaryIndex].Value);
                            try
                            {
                                Term[] unaryArguments = [BeamBinaryEntryProtocol.SizeArgument(
                                    code,
                                    registers,
                                    unaryImport,
                                    operands[unaryIndex+1]
                                )];
                                Term unaryValue = BeamInfallibleBifContract.TryInvoke(
                                    context,
                                    code,
                                    instruction,
                                    unaryArguments,
                                    out Term canonicalUnary
                                )
                                    ? canonicalUnary : await BeamExternalImports.Invoke(context, unaryImport, unaryArguments);
                                registers.Write(operands[unaryIndex + 2], unaryValue);
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
                        case BeamGeneralOpcodeNames.CaseEnd:
                            throw new ErlangException(Term.Tuple(Term.A(BeamGeneralOpcodeNames.CaseClause), Read(code, registers, operands[0])));
                        case BeamGeneralOpcodeNames.IfEnd:
                            throw new ErlangException(ErlangErrorReasons.IfClause);
                        case BeamGeneralOpcodeNames.TernaryBif:
                            BeamExternalImport ternaryBifImport = BeamRegisterValidation.TernaryBifImport(code, (int)operands[1].Value);
                            Term[] ternaryBifArguments = operands.Skip(2).Take(3).Select(value => Read(code, registers, value)).ToArray();
                            try
                            {
                                Term ternaryBifValue = await BeamExternalImports.Invoke(context, ternaryBifImport, ternaryBifArguments);
                                registers.Write(operands[5], ternaryBifValue);
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
                        case BeamGeneralOpcodeNames.TernaryGcBif:
                            registers.CheckLive((int)operands[1].Value);
                            BeamExternalImport ternaryImport = BeamRegisterValidation.TernaryImport(code, (int)operands[2].Value);
                            try
                            {
                                Term ternaryValue = await BeamExternalImports.Invoke(context, ternaryImport,
                                    operands.Skip(3).Take(3).Select(value => Read(code, registers, value)).ToArray());
                                registers.Write(operands[6], ternaryValue);
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
                        case BeamCatchNames.Install:
                        case BeamTryNames.Install:
                            BeamHandlerTag installed = registers.InstallHandlerY(
                                (int)operands[0].Value,
                                new(
                                    function,
                                    code.Labels[(int)operands[1].Value],
                                    continuations.Count,
                                    callFrames.Count,
                                    functionFrames.Count,
                                    registers.FrameDepth
                                )
                                { IsCatch = instruction.Definition.Name == BeamCatchNames.Install }
                            );
                            handlers.Push(installed);
                            break;
                        case BeamTryNames.Case:
                        case BeamTryNames.End:
                            BeamHandlerTag active = registers.ReadHandlerY((int)operands[0].Value);
                            if (!handlers.TryPeek(out BeamHandlerTag? nearest) || !ReferenceEquals(active, nearest)
                                || active.Checkpoint.IsCatch
                                || !ReferenceEquals(active.Checkpoint.Function, function))
                                throw new InvalidDataException(BeamRegisterProtocolNames.Handler);
                            if (instruction.Definition.Name == BeamTryNames.Case)
                            {
                                if (!ReferenceEquals(pendingHandler, active))
                                    throw new InvalidDataException(BeamRegisterProtocolNames.Handler);
                                registers.RequireExceptionMarker(BeamTryParameters.ExceptionMarkerRegister, active);
                                BeamSavedTrace caughtTrace = registers.ReadSavedTrace(BeamTryParameters.SavedTraceRegister);
                                registers.Write(BeamTryParameters.CaughtClassRegister, Term.A(caughtTrace.ExceptionClass));
                                registers.ClearXExcept(
                                    BeamTryParameters.CaughtClassRegister,
                                    BeamTryParameters.ReasonRegister,
                                    BeamTryParameters.SavedTraceRegister
                                );
                                pendingHandler = null;
                            }
                            else if (pendingHandler is not null)
                                throw new InvalidDataException(BeamRegisterProtocolNames.Handler);
                            registers.RemoveHandlerY((int)operands[0].Value, active);
                            handlers.Pop();
                            break;
                        case BeamCatchNames.End:
                            BeamHandlerTag catchTag = registers.ReadHandlerY((int)operands[0].Value);
                            if (!handlers.TryPeek(out BeamHandlerTag? catchTop) || !ReferenceEquals(catchTag, catchTop)
                                || !catchTag.Checkpoint.IsCatch || !ReferenceEquals(catchTag.Checkpoint.Function, function))
                                throw new InvalidDataException(BeamRegisterProtocolNames.Handler);
                            if (pendingHandler is not null)
                            {
                                if (!ReferenceEquals(pendingHandler, catchTag))
                                    throw new InvalidDataException(BeamRegisterProtocolNames.Handler);
                                registers.RequireExceptionMarker(BeamTryParameters.ExceptionMarkerRegister, catchTag);
                                BeamSavedTrace catchTrace = registers.ReadSavedTrace(BeamTryParameters.SavedTraceRegister);
                                Term catchReason = registers.Read(BeamTryParameters.ReasonRegister);
                                Term catchResult = catchTrace.ExceptionClass switch
                                {
                                    ErlangExceptionClasses.Throw => catchReason,
                                    ErlangExceptionClasses.Exit => Term.Tuple(Term.A(BeamCatchNames.Exit), catchReason),
                                    _ => Term.Tuple(Term.A(BeamCatchNames.Exit), Term.Tuple(catchReason, catchTrace.StackTraceTerm))
                                };
                                using var catchRoots507 = registers.HasOwnedRoots507 ? registers.Protect507(catchResult) : null;
                                registers.ClearXExcept();
                                registers.Write(0, catchResult);
                                pendingHandler = null;
                            }
                            else
                                _ = registers.Read(0);
                            registers.RemoveHandlerY((int)operands[0].Value, catchTag);
                            handlers.Pop();
                            break;
                        case BeamTryNames.BuildStacktrace:
                            BeamSavedTrace cooked = registers.ReadSavedTrace(BeamTryParameters.CookedTraceRegister);
                            registers.Write(BeamTryParameters.CookedTraceRegister, cooked.StackTraceTerm);
                            registers.ClearXExcept(BeamTryParameters.CookedTraceRegister);
                            break;
                        case BeamTryNames.CaseEnd:
                            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.TryClause), Read(code, registers, operands[0])));
                        case BeamTryNames.Raise:
                            throw BeamTryExecution.Raise(
                                registers.ReadSlot(operands[0]).ReadSavedTrace(),
                                Read(code, registers, operands[1])
                            );
                        case BeamTryNames.RawRaise:
                            Term raisedClass = registers.Read(BeamTryParameters.RawRaiseClassRegister);
                            Term raisedReason = registers.Read(BeamTryParameters.RawRaiseReasonRegister);
                            BeamSavedTrace raisedTrace = registers.ReadSavedTrace(BeamTryParameters.RawRaiseTraceRegister);
                            if (raisedClass is Atom { Name: ErlangExceptionClasses.Error or ErlangExceptionClasses.Exit or ErlangExceptionClasses.Throw } validClass)
                                throw BeamTryExecution.Raise(raisedTrace, raisedReason, validClass.Name);
                            registers.Write(BeamTryParameters.RawRaiseClassRegister, Term.A(ErlangErrorReasons.BadArgument));
                            break;
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
                        case BeamBinaryNames.Create:
                            registers.CheckLive((int)operands[2].Value);
                            try
                            {
                                BitString binary = BeamBinaryExecution.Create(
                                    context,
                                    code,
                                    registers,
                                    operands
                                );
                                registers.Write(operands[4], binary);
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
                        case BeamBinaryNames.Start:
                            BeamBinaryExecution.Start(code, registers, operands);
                            break;
                        case BeamBinaryNames.StartLegacy:
                            if (!BeamBinaryExecution.StartLegacy(code, registers, operands))
                                pc = Branch(
                                    code,
                                    function,
                                    operands[0],
                                    new ErlangException(ErlangErrorReasons.BadArgument)
                                );
                            break;
                        case BeamBinaryNames.ExtractInteger:
                            if (!BeamBinaryFieldExecution.GetInteger(code, registers, operands))
                                pc = Branch(
                                    code,
                                    function,
                                    operands[0],
                                    new ErlangException(ErlangErrorReasons.BadArgument)
                                );
                            break;
                        case BeamBinaryNames.ExtractUtf8:
                        case BeamBinaryNames.SkipUtf8:
                            if (!BeamBinaryFieldExecution.Utf8(registers, operands, instruction.Definition.Name == BeamBinaryNames.SkipUtf8))
                                pc = Branch(
                                    code,
                                    function,
                                    operands[0],
                                    new ErlangException(ErlangErrorReasons.BadArgument)
                                );
                            break;
                        case BeamBinaryNames.ExtractUtf16:
                        case BeamBinaryNames.ExtractUtf32:
                        case BeamBinaryNames.SkipUtf16:
                        case BeamBinaryNames.SkipUtf32:
                            if (!(instruction.Definition.Name is BeamBinaryNames.SkipUtf16 or BeamBinaryNames.SkipUtf32
                                ? BeamUnicodeFields.Skip(registers, operands, instruction.Definition.Name == BeamBinaryNames.SkipUtf32)
                                : BeamUnicodeFields.Get(registers, operands, instruction.Definition.Name == BeamBinaryNames.ExtractUtf32)))
                                pc = Branch(
                                    code,
                                    function,
                                    operands[0],
                                    new ErlangException(ErlangErrorReasons.BadArgument)
                                );
                            break;
                        case BeamFloatStringNames.ExtractFloat:
                            if (!BeamFloatStringExecution.Float(code, registers, operands))
                                pc = Branch(
                                    code,
                                    function,
                                    operands[0],
                                    new ErlangException(ErlangErrorReasons.BadArgument)
                                );
                            break;
                        case BeamFloatStringNames.MatchString:
                            if (!BeamFloatStringExecution.String(code, registers, operands))
                                pc = Branch(
                                    code,
                                    function,
                                    operands[0],
                                    new ErlangException(ErlangErrorReasons.BadArgument)
                                );
                            break;
                        case BeamBinaryNames.SkipBits:
                            if (!BeamBinaryFieldExecution.Skip(code, registers, operands))
                                pc = Branch(
                                    code,
                                    function,
                                    operands[0],
                                    new ErlangException(ErlangErrorReasons.BadArgument)
                                );
                            break;
                        case BeamBinaryNames.ExtractBinary:
                            if (!BeamBinaryExecution.ExtractBinary(code, registers, operands))
                                pc = Branch(
                                    code,
                                    function,
                                    operands[0],
                                    new ErlangException(ErlangErrorReasons.BadArgument)
                                );
                            break;
                        case BeamBinaryNames.ExtractTail:
                            BeamBinaryExecution.ExtractTail(registers, operands);
                            break;
                        case BeamBinaryNames.SavePosition:
                            BeamBinaryExecution.SavePosition(registers, operands);
                            break;
                        case BeamBinaryNames.RestorePosition:
                            BeamBinaryExecution.RestorePosition(registers, operands);
                            break;
                        case BeamBinaryNames.InitializeWritable:
                            BeamBinaryExecution.InitializeWritable(registers);
                            break;
                        case BeamBinaryNames.Match:
                            if (!BeamBinaryExecution.Match(code, registers, operands))
                                pc = Branch(
                                    code,
                                    function,
                                    operands[0],
                                    new ErlangException(ErlangErrorReasons.BadArgument)
                                );
                            break;
                        case BeamReceiveMarkerNames.Reserve:
                            registers.WriteReceiveMarker(operands[0], context.Mailbox.ReserveReceiveMarker());
                            break;
                        case BeamReceiveMarkerNames.Bind:
                            context.Mailbox.BindReceiveMarker(registers.ReadReceiveMarker(operands[0]), Read(code, registers, operands[1]));
                            break;
                        case BeamReceiveMarkerNames.Use:
                            context.Mailbox.UseReceiveMarker(Read(code, registers, operands[0]));
                            break;
                        case BeamReceiveMarkerNames.Clear:
                            context.Mailbox.ClearReceiveMarker(Read(code, registers, operands[0]));
                            break;
                        case BeamApplyNames.Opcode:
                            functionFrame!.SetContinuation(code, pc);
                            Term applied = await BeamApplyExecution.Invoke(
                                context,
                                code,
                                registers,
                                (int)operands[0].Value
                            );
                            functionFrame.ClearContinuation();
                            registers.Write(0, applied);
                            break;
                        case BeamApplyNames.Last:
                            var (appliedTarget, appliedArguments) = BeamApplyExecution.Prepare(
                                context,
                                code,
                                registers,
                                (int)operands[0].Value
                            );
                            if (!callFrames.TryPeek(out BeamCallFrame? applyLastFrame)
                                || applyLastFrame.FunctionEntryLabel != function.EntryLabel
                                || applyLastFrame.ContinuationDepth != continuations.Count
                                || applyLastFrame.Slots != operands[1].Value)
                                throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                            if (appliedTarget.Module == ErlangModuleNames.Module
                                && ((appliedTarget.Function is ErlangModuleNames.Throw or ErlangModuleNames.Exit)
                                        && appliedArguments.Length == 1
                                    || appliedTarget.Function == ErlangModuleNames.Error
                                        && appliedArguments.Length is 1 or 2 or 3)
                                && context.Runtime.Modules.TryInvokeCanonicalNoReturn(
                                    context,
                                    appliedTarget.Module,
                                    appliedTarget.Function,
                                    appliedArguments,
                                    out ValueTask<Term> applyNoReturnInvocation
                                ))
                            {
                                _ = await applyNoReturnInvocation;
                                throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                            }
                            callFrames.Pop();
                            registers.Deallocate(applyLastFrame.Slots);
                            // A successful fixed_apply dispatch deallocates before
                            // entering its target, including a native BIF target.
                            functionFrame?.Dispose();
                            functionFrame = null;
                            Term appliedLast = await BeamExternalImports.Invoke(
                                context,
                                appliedTarget,
                                appliedArguments,
                                captureMissingTarget: true
                            );
                            registers.Write(0, appliedLast);
                            if (!ReturnToCaller())
                                return registers.CompleteGuestReturn507(appliedLast);
                            break;
                        case BeamLinkNames.ExternalCall:
                        case BeamOnLoadNames.ExternalLast:
                            BeamExternalImport import = BeamExternalImports.Resolve(code, (int)operands[1].Value);
                            registers.CheckLive(import.Arity);
                            Term[] importedArguments = Enumerable.Range(0, import.Arity).Select(registers.Read).ToArray();
                            if (instruction.Definition.Name == BeamLinkNames.ExternalCall
                                && BeamRaiseEmptyTraceProof.Prove(code, function, pc - 1)
                                && importedArguments[BeamTryParameters.RawRaiseClassRegister] is Atom
                                { Name: ErlangExceptionClasses.Error or ErlangExceptionClasses.Exit or ErlangExceptionClasses.Throw })
                            {
                                functionFrame!.SetContinuation(code, pc);
                                if (!context.Runtime.Modules.TryInvokeCanonicalNoReturn(
                                    context,
                                    import.Module,
                                    import.Function,
                                    importedArguments,
                                    out ValueTask<Term> emptyTraceRaise
                                ))
                                    throw new NotSupportedException(BeamOnLoadDiagnostics.Frame);
                                _ = await emptyTraceRaise;
                                throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                            }
                            BeamCallFrame? externalLastFrame = null;
                            bool sourceLoadNif = BeamExternalImports.IsSourceLoadNif(import);
                            if (instruction.Definition.Name == BeamOnLoadNames.ExternalLast
                                && BeamNoReturnAnalysis.Import(code, (int)operands[1].Value)
                                && context.Runtime.Modules.TryInvokeCanonicalNoReturn(
                                    context,
                                    import.Module,
                                    import.Function,
                                    importedArguments,
                                    out ValueTask<Term> noReturnInvocation
                                ))
                            {
                                _ = await noReturnInvocation;
                                // Canonical bindings are sealed; a return would invalidate admission assumptions.
                                throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                            }
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
                            if (instruction.Definition.Name == BeamOnLoadNames.ExternalLast && !BeamExternalImports.IsNativePrimitive(import))
                            {
                                functionFrame?.Dispose();
                                functionFrame = null;
                                if (!sourceLoadNif && continuations.Count == 0 && handlers.Count == 0 && callFrames.Count == 0)
                                {
                                    // No BEAM return address remains in this image. Retire
                                    // its executable ownership before entering the callee.
                                    flow.Dispose();
                                    lease.Dispose();
                                }
                            }
                            if (instruction.Definition.Name == BeamLinkNames.ExternalCall)
                                functionFrame!.SetContinuation(code, pc);
                            Term imported;
                            using (instruction.Definition.Name == BeamOnLoadNames.ExternalLast && BeamModuleApplyTailScope.IsModuleApply(import)
                                ? new BeamModuleApplyTailScope(context, TransferModuleApply) : null)
                            using (BeamExternalImports.IsNativePrimitive(import) ? context.EnterBeamNativeCallSite419(code, pc - 1) : null)
                                imported = await BeamExternalImports.Invoke(context, import, importedArguments);
                            if (instruction.Definition.Name == BeamLinkNames.ExternalCall)
                                functionFrame!.ClearContinuation();
                            if (sourceLoadNif && externalLastFrame is not null)
                            {
                                callFrames.Pop();
                                registers.Deallocate(externalLastFrame.Slots);
                            }
                            registers.Write(0, imported);
                            if (instruction.Definition.Name == BeamOnLoadNames.ExternalLast)
                            {
                                if (!ReturnToCaller())
                                    return registers.CompleteGuestReturn507(imported);
                            }
                            break;
                        case BeamLinkNames.Label:
                            break;
                        case BeamLinkNames.Line:
                            break;
                        case BeamLinkNames.Allocate:
                        case BeamLinkNames.AllocateHeap:
                            registers.ApplyLiveHint507((int)operands[instruction.Definition.Name == BeamLinkNames.AllocateHeap ? 2 : 1].Value);
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
                                execution,
                                entries: context.Runtime.Functions
                            );
                            registers.Write(operands[1], closure);
                            break;
                        case BeamGeneralOpcodeNames.DynamicCall:
                        case BeamLambdaNames.Call:
                            {
                                bool dynamicCall = instruction.Definition.Name == BeamGeneralOpcodeNames.DynamicCall;
                                int arity = (int)operands[dynamicCall ? 0 : 1].Value;
                                registers.CheckLive(arity);
                                Term callee = dynamicCall ? registers.Read(arity) : Read(code, registers, operands[2]);
                                if (!dynamicCall && operands[0].Kind == BeamOperandParameters.Unsigned)
                                    BeamClosureFactory.RequireKnownTarget(callee, code, (int)operands[0].Value);
                                if (callee is not FunctionTerm callable)
                                    throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadFunction), callee));
                                Term[] funArguments = Enumerable.Range(0, arity).Select(registers.Read).ToArray();
                                using var funRoots507 = registers.HasOwnedRoots507 ? registers.Protect507(callee, Term.Tuple(funArguments)) : null;
                                bool tailFun = BeamFunTailProtocol.TryDeallocation(
                                    code,
                                    function,
                                    pc,
                                    out int funSlots
                                ) && callable.Arity == arity;
                                if (tailFun)
                                {
                                    if (!callFrames.TryPeek(out BeamCallFrame? funFrame) || funFrame.FunctionEntryLabel != function.EntryLabel
                                        || funFrame.ContinuationDepth != continuations.Count || funFrame.Slots != funSlots)
                                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                                    registers.Deallocate(funFrame.Slots);
                                    callFrames.Pop();
                                    if (funFrame.SavedContinuation is not null)
                                    {
                                        continuations.Pop();
                                        continuations.Push(funFrame.SavedContinuation);
                                    }
                                    functionFrame?.Dispose();
                                    functionFrame = null;
                                }
                                else
                                    functionFrame!.SetContinuation(code, pc);
                                Term funResult;
                                using (tailFun ? null : context.EnterBeamFunAdmission650(callable))
                                    funResult = await callable.Invoke(context, Array.AsReadOnly(funArguments));
                                functionFrame?.ClearContinuation();
                                registers.Write(0, funResult);
                                if (tailFun && !ReturnToCaller())
                                    return registers.CompleteGuestReturn507(funResult);
                                break;
                            }
                        case BeamLinkNames.Bif:
                            BeamRegisterValidation.SelfImport(code, (int)operands[0].Value);
                            registers.Write(operands[1], await BeamExternalImports.Invoke(context, BeamExternalImports.Resolve(code, (int)operands[0].Value), []));
                            break;
                        case BeamLinkNames.Send:
                            {
                                Term recipient = registers.Read(0);
                                Term sentMessage = registers.Read(1);
                                if (registers.HasOwnedRoots507)
                                    await ErlangOperatorValues.TrySendBeam593Async(
                                        context,
                                        registers,
                                        recipient,
                                        sentMessage
                                    );
                                else
                                    await ErlangOperatorValues.TrySendAsync(context, recipient, sentMessage);
                                registers.Write(0, sentMessage);
                                break;
                            }
                        case BeamLinkNames.LoopReceive:
                        case BeamLinkNames.WaitTimeout:
                        case BeamLinkNames.Wait:
                        case BeamLinkNames.Timeout:
                            {
                                BeamReceivePlan plan = instruction.Definition.Name == BeamLinkNames.LoopReceive
                                    ? BeamReceivePlanner.Build(code, function, pc - 1)
                                    : BeamReceivePlanner.BuildDelay(code, function, pc - 1);
                                using BeamRegisterState before = registers.Clone();
                                Func<Term, BeamReceiveSelection?> selectMessage = plan.HasMatcher
                                    ? message => BeamReceiveMatcher.Select(
                                        code,
                                        plan,
                                        before,
                                        message,
                                        context.Self
                                    )
                                    : _ => null;
                                MailboxSelectionAdmission593? admission593 = before.ReceiveAdmission593();
                                BeamReceiveSelection? selected;
                                if (plan.Callback is { } callback)
                                {
                                    Term callbackTimeout = plan.TimeoutOperand is { } timeoutOperand
                                        ? Read(code, before, timeoutOperand) : Term.A(BeamReceiveNames.Infinity);
                                    selected = await context.ReceiveBeamAdmitted593(
                                        message => BeamReceiveCallbackExecution.Select(
                                            context,
                                            code,
                                            function,
                                            functionFrame!,
                                            callback,
                                            before,
                                            message
                                        ),
                                        callbackTimeout,
                                        admission593
                                    );
                                }
                                else if (BeamReceivePureBifs.RequiresAsync(code, plan))
                                    selected = await context.ReceiveBeamGuardAdmitted593(
                                        message => BeamReceiveMatcher.SelectAsync(
                                            code,
                                            plan,
                                            before,
                                            message,
                                            context.Self,
                                            context
                                        ),
                                        plan.TimeoutOperand is { } asyncTimeout ? Read(code, before, asyncTimeout) : Term.A(BeamReceiveNames.Infinity),
                                        admission593
                                    );
                                else
                                    selected = await context.ReceiveBeamPure593(selectMessage,
                                        plan.TimeoutOperand is { } receiveTimeout ? Read(code, before, receiveTimeout) : null,
                                        admission593);
                                if (selected is null)
                                {
                                    if (!plan.HasTimeout)
                                        throw new InvalidDataException(BeamReceiveNames.InvalidPlan);
                                    registers.ClearXExcept();
                                    pc = plan.TimeoutInstructionIndex + 1;
                                }
                                else
                                {
                                    using (selected)
                                    {
                                        foreach (Term markerId in selected.ClearMarkers)
                                            context.Mailbox.ClearReceiveMarker(markerId);
                                        registers.Dispose();
                                        registers = selected.TakeRegisters507();
                                        pc = selected.NextInstructionIndex;
                                    }
                                }
                                break;
                            }
                        case BeamLinkNames.Move:
                            if (operands[0].Kind is BeamOperandParameters.X or BeamOperandParameters.Y)
                                registers.CopyRegister(operands[0], operands[1]);
                            else
                                registers.Write(operands[1], Read(code, registers, operands[0]));
                            break;
                        case BeamLinkNames.Return:
                            Term result = registers.Read(0);
                            if (!ReturnToCaller())
                                return registers.CompleteGuestReturn507(result);
                            break;
                        case BeamRegisterNames.Call:
                        case BeamLinkNames.LocalCall:
                        case BeamGeneralOpcodeNames.LocalLast:
                            registers.CheckLive((int)operands[0].Value);
                            if (instruction.Definition.Name == BeamGeneralOpcodeNames.LocalLast)
                            {
                                if (!callFrames.TryPop(out BeamCallFrame? lastFrame) || lastFrame.FunctionEntryLabel != function.EntryLabel
                                    || lastFrame.ContinuationDepth != continuations.Count || lastFrame.Slots != operands[2].Value)
                                    throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                                registers.Deallocate(lastFrame.Slots);
                                if (lastFrame.SavedContinuation is not null)
                                {
                                    continuations.Pop();
                                    continuations.Push(lastFrame.SavedContinuation);
                                }
                            }
                            if (instruction.Definition.Name == BeamRegisterNames.Call)
                            {
                                functionFrame!.SetContinuation(code, pc);
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
                            if (!BeamExternalImports.IsNativePrimitive(tailImport))
                            {
                                functionFrame?.Dispose();
                                functionFrame = null;
                                if (!BeamExternalImports.IsSourceLoadNif(tailImport)
                                    && continuations.Count == 0 && handlers.Count == 0 && callFrames.Count == 0)
                                {
                                    flow.Dispose();
                                    lease.Dispose();
                                }
                            }
                            Term externalResult;
                            using (BeamModuleApplyTailScope.IsModuleApply(tailImport)
                                ? new BeamModuleApplyTailScope(context, TransferModuleApply) : null)
                            using (BeamExternalImports.IsNativePrimitive(tailImport) ? context.EnterBeamNativeCallSite419(code, pc - 1) : null)
                                externalResult = await BeamExternalImports.Invoke(context, tailImport, tailArguments);
                            registers.Write(0, externalResult);
                            if (!ReturnToCaller())
                                return registers.CompleteGuestReturn507(externalResult);
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
                            registers.ApplyLiveHint507((int)operands[1].Value);
                            break;
                        case BeamLinkNames.PutList:
                            Term head = Read(code, registers, operands[0]);
                            Term tail = Read(code, registers, operands[1]);
                            registers.WriteCons507(operands[2], head, tail);
                            break;
                        case BeamLinkNames.PutTuple:
                            Term[] items = operands.Skip(BeamLinkParameters.TupleListPrefixOperands).Select(operand => Read(code, registers, operand)).ToArray();
                            registers.WriteTuple507(operands[0], items);
                            break;
                        default:
                            throw new NotSupportedException(BeamRegisterNames.Unsupported);
                    }
                    if (registers.HasOwnedRoots507)
                        await BeamOwnedCheckpoint507.Reach(context, instruction.Definition.Name);
                }
                catch (ErlangException error) when (handlers.Count != 0)
                {
                    // Capture before disposing any visible source frame. Handler execution
                    // must never recapture or truncate a previously saved trace.
                    ErlangException capturedError = context.CaptureSourceFailure(
                        error,
                        error.Reason is Atom { Name: ErlangErrorReasons.FunctionClause } ? functionFrame?.Arguments : null
                    );
                    using var exceptionRoots507 = registers.HasOwnedRoots507 ? registers.Protect507(capturedError.Reason, capturedError.StackTraceTerm) : null;
                    var savedTrace = new BeamSavedTrace(capturedError);
                    BeamHandlerTag target = handlers.Peek();
                    BeamHandlerCheckpoint checkpoint = target.Checkpoint;
                    if (continuations.Count < checkpoint.ContinuationDepth
                        || callFrames.Count < checkpoint.CallFrameDepth
                        || functionFrames.Count < checkpoint.FunctionScopeDepth)
                        throw new InvalidDataException(BeamRegisterProtocolNames.Handler);
                    registers.ClearFloats();
                    registers.UnwindToHandler(target);
                    while (functionFrames.Count > checkpoint.FunctionScopeDepth)
                    {
                        functionFrame?.Dispose();
                        functionFrame = functionFrames.Pop();
                    }
                    if (functionFrame is null)
                        throw new InvalidDataException(BeamRegisterProtocolNames.Handler);
                    functionFrame.ClearContinuation();
                    while (continuations.Count > checkpoint.ContinuationDepth)
                        continuations.Pop();
                    while (callFrames.Count > checkpoint.CallFrameDepth)
                        callFrames.Pop();
                    function = checkpoint.Function;
                    pc = checkpoint.HandlerInstructionIndex;
                    entering = false;
                    registers.ClearXExcept();
                    registers.WriteExceptionMarkerX(BeamTryParameters.ExceptionMarkerRegister, target);
                    registers.Write(BeamTryParameters.ReasonRegister, savedTrace.Reason);
                    registers.WriteSavedTrace(BeamTryParameters.SavedTraceRegister, savedTrace);
                    registers.Write(BeamTryParameters.EntryClassRegister, Term.A(savedTrace.ExceptionClass));
                    pendingHandler = target;
                }
            }
        }
        catch (ErlangException error)
        {
            throw registers.ExportException507(context.CaptureSourceFailure(error, error.Reason is Atom { Name: ErlangErrorReasons.FunctionClause } ? functionFrame?.Arguments : null));
        }
        finally
        {
            if (registers.HasOwnedRoots507)
                DisposeOwnedFrames507(registers, functionFrame, functionFrames);
            else
            {
                registers.Dispose();
                functionFrame?.Dispose();
                while (functionFrames.TryPop(out var suspendedFrame))
                    suspendedFrame.Dispose();
            }
        }
    }

    private static int BranchTarget(
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

    internal static Term Read(BeamLinkedCode code, BeamRegisterState registers, BeamOperand operand) => operand.Kind switch
    {
        BeamOperandParameters.X => registers.Read((int)operand.Value),
        BeamOperandParameters.Y => registers.ReadY((int)operand.Value),
        BeamOperandParameters.Integer => code.IntegerConstant(operand.Value),
        BeamOperandParameters.Atom => Term.A(code.Structure.Atoms[(int)operand.Value]!),
        BeamOperandParameters.Nil => Nil.Value,
        BeamOperandParameters.LiteralValue => code.Structure.Literals[(int)operand.Value],
        _ => throw new NotSupportedException(BeamRegisterNames.Unsupported)
    };
}
