using System.Numerics;
namespace Erlang;

internal static class BeamFrameValidation
{
    internal static void Validate(BeamLinkedCode code, BeamLinkedFunction function, IReadOnlyDictionary<int, BeamReceivePlan> receives)
    {
        if (code.Instructions.Skip(function.FirstInstructionIndex)
            .Take(function.EndInstructionIndex - function.FirstInstructionIndex)
            .Any(instruction => instruction.Definition.Name is BeamCatchNames.Install or BeamCatchNames.End or BeamTryNames.Install
                or BeamTryNames.Case or BeamTryNames.End or BeamTryNames.BuildStacktrace
                or BeamTryNames.Raise or BeamTryNames.RawRaise or BeamTryNames.CaseEnd or BeamApplyNames.Opcode or BeamApplyNames.Last
                or BeamReceiveMarkerNames.Reserve or BeamReceiveMarkerNames.Bind
                or BeamReceiveMarkerNames.Clear or BeamReceiveMarkerNames.Use
                or BeamBinaryNames.Start or BeamBinaryNames.Match or BeamBinaryNames.Create
                or BeamBinaryNames.StartLegacy or BeamBinaryNames.ExtractBinary or BeamBinaryNames.InitializeWritable
                or BeamBinaryNames.ExtractTail or BeamBinaryNames.SavePosition or BeamBinaryNames.RestorePosition
                or BeamFloatStringNames.ExtractFloat or BeamFloatStringNames.MatchString or BeamBinaryNames.ExtractInteger or BeamBinaryNames.ExtractUtf16 or BeamBinaryNames.ExtractUtf32 or BeamBinaryNames.ExtractUtf8 or BeamBinaryNames.SkipUtf16 or BeamBinaryNames.SkipUtf32 or BeamBinaryNames.SkipUtf8 or BeamBinaryNames.SkipBits
                or BeamCollectionOpcodeNames.FloatMove or BeamCollectionOpcodeNames.FloatConvert or BeamCollectionOpcodeNames.FloatNegate or BeamCollectionOpcodeNames.FloatMultiply or BeamCollectionOpcodeNames.FloatAdd or BeamCollectionOpcodeNames.FloatSubtract or BeamCollectionOpcodeNames.FloatDivide)
            || code.Instructions.Skip(function.FirstInstructionIndex)
                .Take(function.EndInstructionIndex - function.FirstInstructionIndex)
                .Any(instruction => instruction.Definition.Name == BeamGeneralOpcodeNames.Swap)
                && BeamNoReturnAnalysis.Local(code, function.EntryLabel)
            || BeamBinaryEntryProtocol.HasLocalContextCall(code, function)
            || receives.Values.Any(plan => !plan.HasMatcher || BeamReceivePlanner.WritesFrame(code, plan)
                || plan.MatcherInstructions.Any(index => code.Instructions[index].Definition.Name
                    is BeamGeneralOpcodeNames.BinaryTest or BeamGeneralOpcodeNames.FunctionTest)
                || BeamReceivePureBifs.RequiresAsync(code, plan)))
        {
            ValidateExceptional(code, function, receives);

            return;
        }

        var work = new Stack<(int Pc, int Slots, BigInteger Initialized)>();
        var seen = new Dictionary<(int Pc, int Slots), (int Slots, BigInteger Initialized)>();
        var sizes = new Dictionary<int, int>();
        work.Push((function.FirstInstructionIndex, BeamFrameParameters.NoFrame, BigInteger.Zero));

        void Next(int pc, int slots, BigInteger initialized)
        {
            if (pc < function.FirstInstructionIndex || pc >= function.EndInstructionIndex)
                throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
            work.Push((pc, slots, initialized));
        }
        while (work.TryPop(out var current))
        {
            int slots = current.Slots;
            BigInteger initialized = current.Initialized;
            if (sizes.TryGetValue(current.Pc, out int previousSlots) && slots != previousSlots
                && !IsExceptionSuffix(code, function, current.Pc))
                throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
            sizes.TryAdd(current.Pc, slots);
            if (seen.TryGetValue((current.Pc, slots), out var previous))
            {
                initialized &= previous.Initialized;
                if (initialized == previous.Initialized)
                    continue;
            }
            seen[(current.Pc, slots)] = (slots, initialized);
            BeamInstruction instruction = code.Instructions[current.Pc];
            var operands = instruction.Operands;

            void Y(BeamOperand operand, bool source)
            {
                if (operand.Kind != BeamOperandParameters.Y)
                    return;
                int index = (int)operand.Value;
                if (index < 0 || index >= slots || source && (initialized & (BigInteger.One << index)) == 0)
                    throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
            }

            void Write(BeamOperand operand)
            {
                Y(operand, false);
                if (operand.Kind == BeamOperandParameters.Y)
                    initialized |= BigInteger.One << (int)operand.Value;
            }

            void Roots()
            {
                if (slots != BeamFrameParameters.NoFrame && initialized != Mask(slots))
                    throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
            }

            void Fail(BeamOperand target)
            {
                if (target.Kind == BeamOperandParameters.Label && target.Value != function.InfoLabel)
                    Next(target.Value == function.EntryLabel ? function.FirstInstructionIndex : code.Labels[(int)target.Value], slots, initialized);
            }
            switch (instruction.Definition.Name)
            {
                case BeamRecordConstructionNames.Opcode:
                    if (slots != BeamFrameParameters.NoFrame && initialized != (BigInteger.One << slots) - BigInteger.One)
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    for (int field = BeamRecordConstructionNames.FirstField; field < operands.Count; field += BeamRecordConstructionNames.FieldWidth)
                        Y(operands[field + 1], true);
                    Write(operands[BeamRecordConstructionNames.Destination]);
                    break;
                case BeamRecordConstructionNames.GetField:
                    Y(operands[BeamRecordConstructionNames.FieldSource], true);
                    Fail(operands[BeamRecordConstructionNames.FieldFailure]);
                    Write(operands[BeamRecordConstructionNames.FieldDestination]);
                    break;
                case BeamCollectionOpcodeNames.UpdateRecord:
                    if (slots != BeamFrameParameters.NoFrame && initialized != (BigInteger.One << slots) - BigInteger.One)
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    Y(operands[2], true);
                    for (int field = 6; field < operands.Count; field += 2)
                        Y(operands[field], true);
                    Write(operands[3]);
                    break;
                case BeamCollectionOpcodeNames.BadRecord:
                    Y(operands[0], true);
                    Roots();
                    continue;
                case BeamGeneralOpcodeNames.Trim:
                    if (slots == BeamFrameParameters.NoFrame || operands[0].Value + operands[1].Value != slots)
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    slots = (int)operands[1].Value;
                    initialized >>= (int)operands[0].Value;
                    break;
                case BeamGeneralOpcodeNames.Swap:
                    Y(operands[0], true);
                    Y(operands[1], true);
                    Write(operands[0]);
                    Write(operands[1]);
                    break;
                case BeamGeneralOpcodeNames.GetList:
                    Y(operands[0], true);
                    Write(operands[1]);
                    Write(operands[2]);
                    break;
                case BeamGeneralOpcodeNames.GetHead:
                case BeamGeneralOpcodeNames.GetTail:
                    Y(operands[0], true);
                    Write(operands[1]);
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
                    Y(operands[1], true);
                    Fail(operands[0]);
                    break;
                case BeamGeneralOpcodeNames.FunctionTest:
                case BeamGeneralOpcodeNames.ArithmeticEqual:
                case BeamGeneralOpcodeNames.ArithmeticNotEqual:
                case BeamGeneralOpcodeNames.ExactNotEqual:
                case BeamGeneralOpcodeNames.GreaterOrEqual:
                case BeamGeneralOpcodeNames.Less:
                    Y(operands[1], true);
                    Y(operands[2], true);
                    Fail(operands[0]);
                    break;
                case BeamGeneralOpcodeNames.Jump:
                    Fail(operands[0]);
                    continue;
                case BeamGeneralOpcodeNames.SelectValue:
                case BeamGeneralOpcodeNames.SelectTupleArity:
                    Y(operands[0], true);
                    Fail(operands[1]);
                    for (int target = 4; target < operands.Count; target += 2)
                        Fail(operands[target]);
                    continue;
                case BeamGeneralOpcodeNames.BinaryBif:
                    Y(operands[2], true);
                    Y(operands[3], true);
                    Fail(operands[0]);
                    Write(operands[4]);
                    break;
                case BeamGeneralOpcodeNames.MapFields:
                    Y(operands[1], true);
                    foreach (BeamOperand key in operands.Skip(3))
                        Y(key, true);
                    Fail(operands[0]);
                    break;
                case BeamGeneralOpcodeNames.MapElements:
                    Y(operands[1], true);
                    for (int pair = 3; pair < operands.Count; pair += 2)
                        Y(operands[pair], true);
                    Fail(operands[0]);
                    for (int pair = 3; pair < operands.Count; pair += 2)
                        Write(operands[pair + 1]);
                    break;
                case BeamGeneralOpcodeNames.MapExact:
                case BeamGeneralOpcodeNames.MapAssociate:
                    if (slots != BeamFrameParameters.NoFrame && initialized != (BigInteger.One << slots) - BigInteger.One)
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    Y(operands[1], true);
                    foreach (BeamOperand value in operands.Skip(5))
                        Y(value, true);
                    Fail(operands[0]);
                    Write(operands[2]);
                    break;
                case BeamGeneralOpcodeNames.UnaryBif:
                    Y(operands[2], true);
                    Fail(operands[0]);
                    Write(operands[3]);
                    break;
                case BeamGeneralOpcodeNames.UnaryGcBif:
                    Y(operands[3], true);
                    Fail(operands[0]);
                    Write(operands[4]);
                    break;
                case BeamGeneralOpcodeNames.TernaryBif:
                    foreach (BeamOperand source in operands.Skip(2).Take(3))
                        Y(source, true);
                    Fail(operands[0]);
                    Write(operands[5]);
                    break;
                case BeamGeneralOpcodeNames.TernaryGcBif:
                    foreach (BeamOperand source in operands.Skip(3).Take(3))
                        Y(source, true);
                    Fail(operands[0]);
                    Write(operands[6]);
                    break;
                case BeamGeneralOpcodeNames.IfEnd:
                    Roots();
                    continue;
                case BeamGeneralOpcodeNames.LocalLast:
                    Roots();
                    if (slots != operands[2].Value)
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    continue;
                case BeamGeneralOpcodeNames.CaseEnd:
                    Y(operands[0], true);
                    Roots();
                    continue;
                case BeamLinkNames.Allocate:
                case BeamLinkNames.AllocateHeap:
                    if (slots != BeamFrameParameters.NoFrame)
                        throw new NotSupportedException(BeamOnLoadDiagnostics.Frame);
                    slots = (int)operands[0].Value;
                    initialized = BigInteger.Zero;
                    break;
                case BeamLinkNames.Deallocate:
                    if (slots != operands[0].Value)
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    slots = BeamFrameParameters.NoFrame;
                    initialized = BigInteger.Zero;
                    break;
                case BeamOnLoadNames.InitY:
                    foreach (BeamOperand operand in operands.Skip(BeamFrameParameters.TupleCountPrefix))
                    {
                        Y(operand, false);
                        initialized |= BigInteger.One << (int)operand.Value;
                    }
                    break;
                case BeamLinkNames.Move:
                    Y(operands[0], true);
                    Write(operands[1]);
                    break;
                case BeamLambdaNames.Create:
                    foreach (BeamOperand value in operands.Skip(BeamLambdaNames.EnvironmentPrefixOperands))
                        Y(value, true);
                    Write(operands[1]);
                    break;
                case BeamLinkNames.Bif:
                    Write(operands[1]);
                    break;
                case BeamLinkNames.PutTuple:
                    foreach (BeamOperand operand in operands.Skip(BeamLinkParameters.TupleListPrefixOperands))
                        Y(operand, true);
                    Write(operands[0]);
                    break;
                case BeamLinkNames.PutList:
                    Y(operands[0], true);
                    Y(operands[1], true);
                    Write(operands[2]);
                    break;
                case BeamOnLoadNames.TupleElement:
                    Y(operands[0], true);
                    Write(operands[2]);
                    break;
                case BeamLinkNames.LoopReceive:
                case BeamLinkNames.WaitTimeout:
                case BeamLinkNames.Wait:
                case BeamLinkNames.Timeout:
                    BeamReceivePlan plan = receives[current.Pc];
                    if (plan.TimeoutOperand is { } timeoutOperand)
                        Y(timeoutOperand, true);
                    foreach (int node in plan.MatcherInstructions)
                    {
                        BeamInstruction matcher = code.Instructions[node];
                        foreach (BeamOperand operand in matcher.Operands)
                            Y(operand, true);
                        if (matcher.Definition.Name == BeamLinkNames.RemoveMessage)
                            Next(node + 1, slots, initialized);
                    }
                    if (plan.HasTimeout)
                        Next(plan.TimeoutInstructionIndex + 1, slots, initialized);
                    continue;
                case BeamRegisterNames.Call:
                case BeamGeneralOpcodeNames.DynamicCall:
                case BeamApplyNames.Opcode:
                case BeamLambdaNames.Call:
                case BeamLinkNames.ExternalCall:
                    if (slots == BeamFrameParameters.NoFrame)
                        throw new NotSupportedException(BeamOnLoadDiagnostics.Frame);
                    if (instruction.Definition.Name == BeamLambdaNames.Call)
                        Y(operands[2], true);
                    break;
                case BeamLinkNames.GuardBif:
                    Y(operands[3], true);
                    Y(operands[4], true);
                    Fail(operands[0]);
                    Write(operands[5]);
                    break;
                case BeamLinkNames.IntegerTest:
                case BeamOnLoadNames.PidTest:
                case BeamOnLoadNames.TaggedTupleTest:
                    Y(operands[1], true);
                    Fail(operands[0]);
                    break;
                case BeamLinkNames.EqualityTest:
                    Y(operands[1], true);
                    Y(operands[2], true);
                    Fail(operands[0]);
                    break;
                case BeamOnLoadNames.BadMatch:
                    Y(operands[0], true);
                    Roots();
                    continue;
                case BeamOnLoadNames.ExternalLast:
                    Roots();
                    if (slots != operands[2].Value && !BeamNoReturnAnalysis.Import(code, (int)operands[1].Value))
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    continue;
                case BeamLinkNames.Return:
                case BeamLinkNames.LocalCall:
                case BeamLinkNames.ExternalTailCall:
                    if (slots != BeamFrameParameters.NoFrame)
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    continue;
            }
            Next(current.Pc + 1, slots, initialized);
        }
    }

    private static bool IsExceptionSuffix(BeamLinkedCode code, BeamLinkedFunction function, int pc)
    {
        // Different frame sizes may join only on a bounded direct exception suffix.
        // Each size retains its own definite-init facts; no Y kinds are unioned.
        while (pc < function.EndInstructionIndex)
        {
            BeamInstruction instruction = code.Instructions[pc++];
            switch (instruction.Definition.Name)
            {
                case BeamLinkNames.Label:
                case BeamLinkNames.Line:
                    continue;
                case BeamOnLoadNames.BadMatch:
                case BeamGeneralOpcodeNames.CaseEnd:
                case BeamGeneralOpcodeNames.IfEnd:
                case BeamCollectionOpcodeNames.BadRecord:
                    return true;
                case BeamOnLoadNames.ExternalLast:
                    return BeamNoReturnAnalysis.Import(code, (int)instruction.Operands[1].Value);
                default:
                    return false;
            }
        }

        return false;
    }

    private sealed record Handler(
        int Installation,
        int Slot,
        int Target,
        Handler? Parent
    );

    private sealed record ExceptionalState(
        int Pc,
        int Slots,
        BigInteger TermY,
        BigInteger RawY,
        BigInteger TermX,
        BigInteger RawX,
        BigInteger MarkerY,
        BigInteger MarkerX,
        BigInteger ContextY,
        BigInteger ContextX,
        BigInteger ClassY,
        BigInteger ClassX,
        Handler? Handler,
        int Pending
    )
    {
        // Conservative current-function history, not a physical stack/root map.
        internal bool EverOpaqueInFunction { get; init; }
        internal BigInteger Floating { get; init; }
        internal BigInteger PositionX { get; init; }
        internal BigInteger PositionY { get; init; }
        internal BigInteger InitializedY { get; init; }
        internal BigInteger InitializedX { get; init; }
    }

    private static bool SameOpaqueKinds(ExceptionalState left, ExceptionalState right)
        => left.RawX == right.RawX && left.RawY == right.RawY
            && left.MarkerX == right.MarkerX && left.MarkerY == right.MarkerY
            && left.ContextX == right.ContextX && left.ContextY == right.ContextY
            && left.PositionX == right.PositionX && left.PositionY == right.PositionY;

    private static void ValidateExceptional(
        BeamLinkedCode code,
        BeamLinkedFunction function,
        IReadOnlyDictionary<int, BeamReceivePlan> receives
    )
    {
        BigInteger contexts = BeamBinaryEntryProtocol.ContextMask(code, function);
        bool required = BeamBinaryEntryProtocol.RequiresContext(code, function);
        ValidateExceptionalEntry(
            code,
            function,
            receives,
            required ? contexts : BigInteger.Zero
        );
        if (!required && contexts != BigInteger.Zero
            && code.Instructions.Any(instruction =>
                instruction.Definition.Name is BeamRegisterNames.Call or BeamLinkNames.LocalCall or BeamGeneralOpcodeNames.LocalLast
                && instruction.Operands[1].Value == function.EntryLabel))
            ValidateExceptionalEntry(
                code,
                function,
                receives,
                contexts
            );
    }

    private static void ValidateExceptionalEntry(
        BeamLinkedCode code,
        BeamLinkedFunction function,
        IReadOnlyDictionary<int, BeamReceivePlan> receives,
        BigInteger entryContexts
    )
    {
        // Binary candidate contexts must be fresh and fully consumed inside
        // reviewed adjacent start/match pairs. Other transport stays refused.
        bool hasBinary = code.Instructions.Skip(function.FirstInstructionIndex)
            .Take(function.EndInstructionIndex - function.FirstInstructionIndex)
            .Any(instruction => BeamReceiveBinaryPairs.IsBinary(instruction.Definition.Name));
        bool delayOnlyBinary = hasBinary && receives.Count != 0 && receives.Values.All(plan => !plan.HasMatcher);
        bool ordinaryBinaryScratch = hasBinary && receives.Count != 0
            && BeamReceiveBinaryPairs.HasOrdinaryScratch(code, function, receives);
        if (receives.Count != 0 && hasBinary)
            BeamReceiveBinaryPairs.ValidateFunction(code, function, receives);

        const int NoPendingHandler = -1;
        var handlerTargets = new HashSet<int>();
        for (int index = function.FirstInstructionIndex; index < function.EndInstructionIndex; index++)
        {
            BeamInstruction candidate = code.Instructions[index];
            if (candidate.Definition.Name is BeamTryNames.Install or BeamCatchNames.Install)
                handlerTargets.Add(code.Labels[(int)candidate.Operands[1].Value]);
        }
        if (BeamBinaryEntryProtocol.RequiresContext(code, function) && code.Exports.Contains(function))
            throw new NotSupportedException(BeamBinaryNames.Unsupported);
        var work = new Stack<ExceptionalState>();
        var seen = new Dictionary<(int Pc, int Slots, int Pending), List<ExceptionalState>>();
        var sizes = new Dictionary<int, (int Slots, bool Heterogeneous, bool Protected)>();
        work.Push(new(
            function.FirstInstructionIndex,
            BeamFrameParameters.NoFrame,
            BigInteger.Zero,
            BigInteger.Zero,
            Mask(function.Arity) & ~entryContexts,
            BigInteger.Zero,
            BigInteger.Zero,
            BigInteger.Zero,
            BigInteger.Zero,
            BigInteger.Zero,
            BigInteger.Zero,
            BigInteger.Zero,
            null,
            NoPendingHandler
        )
        { ContextX = entryContexts, EverOpaqueInFunction = entryContexts != BigInteger.Zero });
        while (work.TryPop(out ExceptionalState? state))
        {
            bool protectedState = state.Handler is not null || state.Pending != NoPendingHandler;
            var summary = sizes.TryGetValue(state.Pc, out var previousSize)
                ? (Slots: previousSize.Slots,
                    Heterogeneous: previousSize.Heterogeneous || state.Slots != previousSize.Slots,
                    Protected: previousSize.Protected || protectedState)
                : (Slots: state.Slots, Heterogeneous: false, Protected: protectedState);
            if (summary.Heterogeneous && (summary.Protected || !IsExceptionSuffix(code, function, state.Pc)))
                throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
            sizes[state.Pc] = summary;
            var frameStateKey = (state.Pc, state.Slots, state.Pending);
            if (!seen.TryGetValue(frameStateKey, out var variants))
            {
                variants = new List<ExceptionalState>();
                seen.Add(frameStateKey, variants);
            }
            // A term and a context may be transported along different valid
            // paths. Keep their opaque kinds distinct rather than erasing
            // both kinds at the join or treating unknown storage as a term.
            if (variants.Any(prior => prior.Handler != state.Handler))
                throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
            int variant = variants.FindIndex(prior => SameOpaqueKinds(prior, state));
            if (variant >= 0)
            {
                ExceptionalState prior = variants[variant];
                if (state.Slots != prior.Slots || state.Handler != prior.Handler || state.Pending != prior.Pending)
                    throw new InvalidDataException(BeamOnLoadDiagnostics.Frame)
                    {
                        Data =
                        {
                            [nameof(BeamLinkedFunction)] = function,
                            [nameof(ExceptionalState)] = state,
                            [nameof(prior)] = prior
                        }
                    };
                state = state with
                {
                    InitializedY = (state.InitializedY | state.TermY | state.RawY | state.MarkerY | state.ContextY | state.PositionY)
                        & (prior.InitializedY | prior.TermY | prior.RawY | prior.MarkerY | prior.ContextY | prior.PositionY),
                    InitializedX = (state.InitializedX | state.TermX | state.RawX | state.MarkerX | state.ContextX | state.PositionX)
                        & (prior.InitializedX | prior.TermX | prior.RawX | prior.MarkerX | prior.ContextX | prior.PositionX),
                    TermY = state.TermY & prior.TermY,
                    RawY = state.RawY & prior.RawY,
                    TermX = state.TermX & prior.TermX,
                    RawX = state.RawX & prior.RawX,
                    MarkerY = state.MarkerY & prior.MarkerY,
                    ContextY = state.ContextY & prior.ContextY,
                    MarkerX = state.MarkerX & prior.MarkerX,
                    ContextX = state.ContextX & prior.ContextX,
                    PositionX = state.PositionX & prior.PositionX,
                    PositionY = state.PositionY & prior.PositionY,
                    ClassY = state.ClassY & prior.ClassY,
                    ClassX = state.ClassX & prior.ClassX,
                    EverOpaqueInFunction = state.EverOpaqueInFunction || prior.EverOpaqueInFunction,
                    Floating = state.Floating & prior.Floating
                };
                if (state == prior)
                    continue;
            }
            if (variant >= 0)
                variants[variant] = state;
            else
            {
                if (variants.Count >= BeamFrameParameters.MaximumOpaqueVariants)
                    throw new NotSupportedException(BeamOnLoadDiagnostics.Frame);
                variants.Add(state);
            }
            BeamInstruction instruction = code.Instructions[state.Pc];
            var operands = instruction.Operands;
            string name = instruction.Definition.Name;
            if (name != BeamBinaryNames.Create && BeamReceiveBinaryPairs.IsBinary(name))
                state = state with { EverOpaqueInFunction = true };
            if (state.Pending != NoPendingHandler
                && name is not (BeamLinkNames.Label or BeamLinkNames.Line or BeamTryNames.Case or BeamCatchNames.End))
                throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);

            void Enqueue(int pc, ExceptionalState next, bool exceptional = false)
            {
                if (pc < function.FirstInstructionIndex || pc >= function.EndInstructionIndex
                    || !exceptional && handlerTargets.Contains(pc)
                        && !(next.Handler is { } catchHandler && catchHandler.Target == pc
                            && code.Instructions[catchHandler.Installation].Definition.Name == BeamCatchNames.Install))
                    throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                work.Push(next with { Pc = pc });
            }

            int Index(BeamOperand operand)
            {
                int index = (int)operand.Value;
                if (index < 0 || operand.Kind == BeamOperandParameters.Y && index >= state.Slots
                    || operand.Kind == BeamOperandParameters.X && index >= BeamOperandParameters.MaximumRegister)
                    throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);

                return index;
            }

            bool Raw(BeamOperand operand)
            {
                int index = Index(operand);

                return operand.Kind == BeamOperandParameters.Y
                    ? (state.RawY & Bit(index)) != 0
                    : operand.Kind == BeamOperandParameters.X && (state.RawX & Bit(index)) != 0;
            }

            bool Marker(BeamOperand operand)
            {
                if (operand.Kind is not (BeamOperandParameters.X or BeamOperandParameters.Y))
                    return false;
                int index = Index(operand);

                return ((operand.Kind == BeamOperandParameters.X ? state.MarkerX : state.MarkerY) & Bit(index)) != 0;
            }

            bool Context(BeamOperand operand)
            {
                if (operand.Kind is not (BeamOperandParameters.X or BeamOperandParameters.Y))
                    return false;
                int index = Index(operand);

                return ((operand.Kind == BeamOperandParameters.X ? state.ContextX : state.ContextY) & Bit(index)) != 0;
            }

            bool Position(BeamOperand operand)
            {
                if (operand.Kind is not (BeamOperandParameters.X or BeamOperandParameters.Y))
                    return false;
                int index = Index(operand);

                return ((operand.Kind == BeamOperandParameters.X ? state.PositionX : state.PositionY) & Bit(index)) != 0;
            }

            void Read(BeamOperand operand, bool transport = false)
            {
                if (operand.Kind is not (BeamOperandParameters.X or BeamOperandParameters.Y))
                    return;
                int index = Index(operand);
                BigInteger terms = operand.Kind == BeamOperandParameters.X ? state.TermX : state.TermY;
                if ((terms & Bit(index)) == 0 && !(transport && (Raw(operand) || Marker(operand) || Context(operand) || Position(operand))))
                    throw new InvalidDataException(BeamOnLoadDiagnostics.Frame)
                    {
                        Data =
                        {
                            [nameof(ExceptionalState)] = state,
                            [nameof(BeamOperand)] = operand,
                            [nameof(BeamLinkedFunction)] = function
                        }
                    };
            }

            bool Class(BeamOperand operand)
            {
                if (operand.Kind == BeamOperandParameters.Atom)
                    return code.Structure.Atoms[(int)operand.Value] is ErlangExceptionClasses.Error
                        or ErlangExceptionClasses.Exit or ErlangExceptionClasses.Throw;
                if (operand.Kind is not (BeamOperandParameters.X or BeamOperandParameters.Y))
                    return false;
                int index = Index(operand);

                return ((operand.Kind == BeamOperandParameters.X ? state.ClassX : state.ClassY) & Bit(index)) != 0;
            }

            void Write(
                BeamOperand operand,
                bool raw = false,
                bool validClass = false,
                bool marker = false,
                bool context = false,
                bool position = false
            )
            {
                int index = Index(operand);
                if (operand.Kind == BeamOperandParameters.Y)
                {
                    for (Handler? tag = state.Handler; tag is not null; tag = tag.Parent)
                        if (tag.Slot == index)
                            throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    state = state with
                    {
                        TermY = raw || marker || context || position ? state.TermY & ~Bit(index) : state.TermY | Bit(index),
                        RawY = raw ? state.RawY | Bit(index) : state.RawY & ~Bit(index),
                        MarkerY = marker ? state.MarkerY | Bit(index) : state.MarkerY & ~Bit(index),
                        ContextY = context ? state.ContextY | Bit(index) : state.ContextY & ~Bit(index),
                        PositionY = position ? state.PositionY | Bit(index) : state.PositionY & ~Bit(index),
                        ClassY = validClass ? state.ClassY | Bit(index) : state.ClassY & ~Bit(index)
                    };
                }
                else if (operand.Kind == BeamOperandParameters.X)
                    state = state with
                    {
                        TermX = raw || marker || context || position ? state.TermX & ~Bit(index) : state.TermX | Bit(index),
                        RawX = raw ? state.RawX | Bit(index) : state.RawX & ~Bit(index),
                        MarkerX = marker ? state.MarkerX | Bit(index) : state.MarkerX & ~Bit(index),
                        ContextX = context ? state.ContextX | Bit(index) : state.ContextX & ~Bit(index),
                        PositionX = position ? state.PositionX | Bit(index) : state.PositionX & ~Bit(index),
                        ClassX = validClass ? state.ClassX | Bit(index) : state.ClassX & ~Bit(index)
                    };
                else
                    throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
            }

            void Roots()
            {
                BigInteger initialized = state.InitializedY | state.TermY | state.RawY | state.MarkerY | state.ContextY | state.PositionY;
                for (Handler? tag = state.Handler; tag is not null; tag = tag.Parent)
                    initialized |= Bit(tag.Slot);
                if (state.Slots != BeamFrameParameters.NoFrame && initialized != Mask(state.Slots))
                    throw new InvalidDataException(BeamOnLoadDiagnostics.Frame)
                    {
                        Data = { [nameof(ExceptionalState)] = state }
                    };
            }

            void Live(int count, bool arguments = false)
            {
                if (count < 0 || count > BeamOperandParameters.MaximumRegister)
                    throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                BigInteger valid = arguments ? state.TermX : state.InitializedX | state.TermX | state.RawX | state.MarkerX | state.ContextX | state.PositionX;
                if ((valid & Mask(count)) != Mask(count))
                    throw new InvalidDataException(BeamOnLoadDiagnostics.Frame)
                    {
                        Data = { [nameof(ExceptionalState)] = state, [nameof(count)] = count }
                    };
            }

            void Exception()
            {
                Roots();
                if (state.Handler is { } handler)
                    Enqueue(
                        handler.Target,
                        state with
                        {
                            InitializedX = BigInteger.Zero,
                            TermX = BigInteger.Zero,
                            RawX = BigInteger.Zero,
                            MarkerX = BigInteger.Zero,
                            ContextX = BigInteger.Zero,
                            PositionX = BigInteger.Zero,
                            ClassX = BigInteger.Zero,
                            Floating = BigInteger.Zero,
                            Pending = handler.Installation
                        },
                        exceptional: true
                    );
            }

            void Branch(BeamOperand target)
            {
                if (target.Kind == BeamOperandParameters.Failure || target.Value == function.InfoLabel)
                {
                    Exception();

                    return;
                }
                int pc = target.Value == function.EntryLabel
                    ? function.FirstInstructionIndex : code.Labels[(int)target.Value];
                Enqueue(pc, state);
            }

            void Bif(BeamOperand target, BeamOperand destination)
            {
                Branch(target);
                Write(destination);
            }

            void Prune(int count)
            {
                Live(count);
                state = state with
                {
                    TermX = state.TermX & Mask(count),
                    InitializedX = state.InitializedX & Mask(count),
                    RawX = state.RawX & Mask(count),
                    MarkerX = state.MarkerX & Mask(count),
                    ContextX = state.ContextX & Mask(count),
                    PositionX = state.PositionX & Mask(count),
                    ClassX = state.ClassX & Mask(count)
                };
            }

            void Arguments(int count)
            {
                Live(count, arguments: true);
                Exception();
            }

            void LocalArguments(BeamOperand arity, BeamOperand target)
            {
                BeamLinkedFunction callee = code.Functions.Single(value => value.EntryLabel == target.Value);
                int count = (int)arity.Value;
                BigInteger allowed = BeamBinaryEntryProtocol.ContextMask(code, callee);
                BigInteger actual = state.ContextX & Mask(count);
                if ((actual & ~allowed) != 0 || ((state.TermX | actual) & Mask(count)) != Mask(count)
                    || BeamBinaryEntryProtocol.RequiresContext(code, callee) && (actual & allowed) != allowed)
                    throw new InvalidDataException(BeamOnLoadDiagnostics.Frame)
                    {
                        Data = { [nameof(ExceptionalState)] = state, [nameof(BeamLinkedFunction)] = callee }
                    };
                Exception();
            }

            void NoTags()
            {
                if (state.Handler is not null)
                    throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
            }

            switch (name)
            {
                case BeamRecordConstructionNames.Opcode:
                    Roots();
                    Live((int)operands[BeamRecordConstructionNames.Live].Value);
                    for (int field = BeamRecordConstructionNames.FirstField; field < operands.Count; field += BeamRecordConstructionNames.FieldWidth)
                        Read(operands[field + 1]);
                    Exception();
                    Prune((int)operands[BeamRecordConstructionNames.Live].Value);
                    Write(operands[BeamRecordConstructionNames.Destination]);
                    break;
                case BeamRecordConstructionNames.GetField:
                    Read(operands[BeamRecordConstructionNames.FieldSource]);
                    Branch(operands[BeamRecordConstructionNames.FieldFailure]);
                    Write(operands[BeamRecordConstructionNames.FieldDestination]);
                    break;
                case BeamCollectionOpcodeNames.UpdateRecord:
                    Roots();
                    Read(operands[2]);
                    for (int field = 6; field < operands.Count; field += 2)
                        Read(operands[field]);
                    Write(operands[3]);
                    break;
                case BeamCollectionOpcodeNames.BadRecord:
                    Read(operands[0]);
                    Exception();
                    continue;
                case BeamCollectionOpcodeNames.FloatMove:
                    if (operands[0].Kind == BeamOperandParameters.Float)
                    {
                        if ((state.Floating & Bit((int)operands[0].Value)) == 0)
                            throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                        // OTP fstore uses already reserved heap space and cannot GC.
                        // Its destination may be the next uninitialized frame slot.
                        Write(operands[1]);
                    }
                    else
                    {
                        Read(operands[0]);
                        state = state with { Floating = state.Floating | Bit((int)operands[1].Value) };
                    }
                    break;
                case BeamCollectionOpcodeNames.FloatConvert:
                    Read(operands[0]);
                    Exception();
                    state = state with { Floating = state.Floating | Bit((int)operands[1].Value) };
                    break;
                case BeamCollectionOpcodeNames.FloatNegate:
                    if ((state.Floating & Bit((int)operands[1].Value)) == 0)
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    state = state with { Floating = state.Floating | Bit((int)operands[2].Value) };
                    break;
                case BeamCollectionOpcodeNames.FloatMultiply:
                case BeamCollectionOpcodeNames.FloatAdd:
                case BeamCollectionOpcodeNames.FloatSubtract:
                case BeamCollectionOpcodeNames.FloatDivide:
                    if ((state.Floating & Bit((int)operands[1].Value)) == 0
                        || (state.Floating & Bit((int)operands[2].Value)) == 0)
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    Exception();
                    state = state with { Floating = state.Floating | Bit((int)operands[3].Value) };
                    break;

                case BeamBinaryNames.Create:
                    Roots();
                    foreach (BeamOperand value in operands.Skip(BeamBinaryNames.CreatePrefix))
                        Read(value);
                    Prune((int)operands[2].Value);
                    Branch(operands[0]);
                    Write(operands[4]);
                    break;
                case BeamBinaryNames.Start:
                case BeamBinaryNames.StartLegacy:
                    bool legacyStart = name == BeamBinaryNames.StartLegacy;
                    BeamOperand startSource = operands[legacyStart ? 1 : 2];
                    bool candidateStart = receives.Values.Any(receive => receive.MatcherInstructions.Contains(state.Pc));
                    Read(startSource, transport: !candidateStart);
                    if (startSource.Kind is BeamOperandParameters.X or BeamOperandParameters.Y
                        && (Raw(startSource) || Marker(startSource) || Position(startSource)))
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    if (!legacyStart && code.Structure.Atoms[(int)operands[0].Value] == BeamBinaryNames.Resume && !Context(startSource))
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    Roots();
                    if (legacyStart && !Context(startSource))
                        Branch(operands[0]);
                    bool retainedContext = Context(startSource);
                    Prune((int)operands[legacyStart ? 2 : 1].Value);
                    if (startSource.Kind == BeamOperandParameters.X)
                        Write(startSource, context: retainedContext);
                    Write(operands[3], context: true);
                    break;
                case BeamBinaryNames.ExtractInteger:
                case BeamFloatStringNames.ExtractFloat:
                case BeamBinaryNames.ExtractBinary:
                    if (!Context(operands[1]))
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    Read(operands[3]);
                    Roots();
                    Branch(operands[0]);
                    Prune((int)operands[2].Value);
                    Write(operands[6]);
                    break;
                case BeamBinaryNames.ExtractUtf8:
                case BeamBinaryNames.ExtractUtf16:
                case BeamBinaryNames.ExtractUtf32:
                case BeamBinaryNames.SkipUtf16:
                case BeamBinaryNames.SkipUtf32:
                case BeamBinaryNames.SkipUtf8:
                    if (!Context(operands[1]))
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    Roots();
                    Live((int)operands[2].Value);
                    Branch(operands[0]);
                    Prune((int)operands[2].Value);
                    if (name is not (BeamBinaryNames.SkipUtf8 or BeamBinaryNames.SkipUtf16 or BeamBinaryNames.SkipUtf32))
                        Write(operands[4]);
                    break;
                case BeamFloatStringNames.MatchString:
                    if (!Context(operands[1]))
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    Branch(operands[0]);
                    break;
                case BeamBinaryNames.SkipBits:
                    if (!Context(operands[1]))
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    Read(operands[2]);
                    Branch(operands[0]);
                    break;
                case BeamBinaryNames.ExtractTail:
                case BeamBinaryNames.SavePosition:
                    if (!Context(operands[0]))
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    Roots();
                    Prune((int)operands[2].Value);
                    Write(operands[1], position: name == BeamBinaryNames.SavePosition);
                    break;
                case BeamBinaryNames.RestorePosition:
                    if (!Context(operands[0]) || !Position(operands[1]))
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    break;
                case BeamBinaryNames.InitializeWritable:
                    Roots();
                    Live(1, arguments: true);
                    state = state with
                    {
                        InitializedX = BigInteger.Zero,
                        TermX = Bit(0),
                        RawX = BigInteger.Zero,
                        MarkerX = BigInteger.Zero,
                        ContextX = BigInteger.Zero,
                        PositionX = BigInteger.Zero,
                        ClassX = BigInteger.Zero,
                        Floating = BigInteger.Zero
                    };
                    break;
                case BeamBinaryNames.Match:
                    if (!Context(operands[1]))
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    Roots();
                    var failureProof = new BeamBinaryMatchFailureProof();
                    foreach (BeamBinaryCommand command in BeamBinaryCommands.Read(code.Structure, operands))
                    {
                        // Preserve partial writes on actual failure edges. Prior
                        // length/alignment guards can prove later commands infallible.
                        if (failureProof.MayFailAndAdvance(command))
                            Branch(operands[0]);
                        if (command.Live >= 0)
                        {
                            if (operands[1].Kind == BeamOperandParameters.X && operands[1].Value >= command.Live)
                                throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                            Prune(command.Live);
                        }
                        if (command.Destination is { } destination)
                            Write(destination);
                    }
                    break;
                case BeamReceiveMarkerNames.Reserve:
                    Write(operands[0], marker: true);
                    break;
                case BeamReceiveMarkerNames.Bind:
                    if (!Marker(operands[0])
                        || operands[1].Kind is not (BeamOperandParameters.X or BeamOperandParameters.Y))
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    Read(operands[1]);
                    break;
                case BeamReceiveMarkerNames.Clear:
                case BeamReceiveMarkerNames.Use:
                    if (operands[0].Kind is not (BeamOperandParameters.X or BeamOperandParameters.Y))
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    Read(operands[0]);
                    break;
                case BeamLinkNames.LoopReceive:
                case BeamLinkNames.WaitTimeout:
                case BeamLinkNames.Wait:
                case BeamLinkNames.Timeout:
                    Roots();
                    BeamReceivePlan receive = receives[state.Pc];
                    if (delayOnlyBinary && state.EverOpaqueInFunction)
                        throw new NotSupportedException(BeamOnLoadDiagnostics.Frame);
                    // A normal binary block may precede a receive only after
                    // every mutable context and saved position has been killed.
                    // In particular, candidate X overwrite is not a liveness proof.
                    if (ordinaryBinaryScratch && (state.ContextX | state.ContextY | state.PositionX | state.PositionY) != BigInteger.Zero)
                        throw new NotSupportedException(BeamOnLoadDiagnostics.Frame);
                    if (receive.TimeoutOperand is { } receiveTimeout)
                    {
                        Read(receiveTimeout);
                        // Lazy timeout parsing may fail only after a scan miss;
                        // its handler sees the original Y state, never scratch.
                        Exception();
                    }
                    state = state with { Floating = BigInteger.Zero };
                    ExceptionalState beforeReceive = state;
                    if (receive.HasMatcher)
                    {
                        Write(new BeamOperand(BeamOperandParameters.X, receive.MessageRegister));
                        Enqueue(state.Pc + 1, state);
                    }
                    // The actual interpreter selects against clones of beforeReceive.
                    // Rejected matcher scratch never feeds another scan. Timeout kills X.
                    if (receive.HasTimeout)
                        Enqueue(
                            receive.TimeoutInstructionIndex + 1,
                            beforeReceive with
                            {
                                InitializedX = BigInteger.Zero,
                                TermX = BigInteger.Zero,
                                RawX = BigInteger.Zero,
                                MarkerX = BigInteger.Zero,
                                ContextX = BigInteger.Zero,
                                PositionX = BigInteger.Zero,
                                ClassX = BigInteger.Zero
                            }
                        );
                    continue;
                case BeamLinkNames.LoopReceiveEnd:
                    Roots();
                    // Planner already proves the rejection targets its own scan.
                    // Failed matching does not mutate the source register snapshot.
                    continue;
                case BeamLinkNames.RemoveMessage:
                    break;
                case BeamCatchNames.Install:
                case BeamTryNames.Install:
                    int slot = Index(operands[0]);
                    if (state.Handler is { } enclosing && slot >= enclosing.Slot)
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    state = state with
                    {
                        Handler = new Handler(
                            state.Pc,
                            slot,
                            code.Labels[(int)operands[1].Value],
                            state.Handler
                        ),
                        TermY = state.TermY & ~Bit(slot),
                        RawY = state.RawY & ~Bit(slot),
                        MarkerY = state.MarkerY & ~Bit(slot),
                        ContextY = state.ContextY & ~Bit(slot),
                        PositionY = state.PositionY & ~Bit(slot),
                        ClassY = state.ClassY & ~Bit(slot)
                    };
                    break;
                case BeamTryNames.Case:
                case BeamTryNames.End:
                    Handler tag = state.Handler ?? throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    if (code.Instructions[tag.Installation].Definition.Name != BeamTryNames.Install
                        || tag.Slot != Index(operands[0])
                        || name == BeamTryNames.Case && state.Pending != tag.Installation
                        || name == BeamTryNames.End && state.Pending != NoPendingHandler)
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    state = state with
                    {
                        Handler = tag.Parent,
                        TermY = state.TermY | Bit(tag.Slot),
                        RawY = state.RawY & ~Bit(tag.Slot),
                        MarkerY = state.MarkerY & ~Bit(tag.Slot),
                        ContextY = state.ContextY & ~Bit(tag.Slot),
                        PositionY = state.PositionY & ~Bit(tag.Slot),
                        Pending = NoPendingHandler,
                        ClassY = state.ClassY & ~Bit(tag.Slot)
                    };
                    if (name == BeamTryNames.Case)
                        state = state with { InitializedX = BigInteger.Zero, TermX = Mask(2), RawX = Bit(2), MarkerX = BigInteger.Zero, ContextX = BigInteger.Zero, PositionX = BigInteger.Zero, ClassX = Bit(0) };
                    break;
                case BeamCatchNames.End:
                    Handler caught = state.Handler ?? throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    if (caught.Slot != Index(operands[0])
                        || code.Instructions[caught.Installation].Definition.Name != BeamCatchNames.Install
                        || state.Pending != NoPendingHandler && state.Pending != caught.Installation)
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    if (state.Pending == NoPendingHandler)
                        Read(new BeamOperand(BeamOperandParameters.X, 0));
                    else
                        state = state with
                        {
                            InitializedX = BigInteger.Zero,
                            TermX = Bit(0),
                            RawX = BigInteger.Zero,
                            MarkerX = BigInteger.Zero,
                            ContextX = BigInteger.Zero,
                            PositionX = BigInteger.Zero,
                            ClassX = BigInteger.Zero
                        };
                    state = state with
                    {
                        Handler = caught.Parent,
                        Pending = NoPendingHandler,
                        TermY = state.TermY | Bit(caught.Slot),
                        RawY = state.RawY & ~Bit(caught.Slot),
                        MarkerY = state.MarkerY & ~Bit(caught.Slot),
                        ContextY = state.ContextY & ~Bit(caught.Slot),
                        PositionY = state.PositionY & ~Bit(caught.Slot),
                        ClassY = state.ClassY & ~Bit(caught.Slot)
                    };
                    break;
                case BeamTryNames.BuildStacktrace:
                    Roots();
                    if ((state.RawX & Bit(0)) == 0)
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    state = state with { InitializedX = BigInteger.Zero, TermX = Bit(0), RawX = BigInteger.Zero, MarkerX = BigInteger.Zero, ContextX = BigInteger.Zero, PositionX = BigInteger.Zero, ClassX = BigInteger.Zero };
                    break;
                case BeamTryNames.Raise:
                    Read(operands[0], transport: true);
                    if (!Raw(operands[0]))
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    Read(operands[1]);
                    Exception();
                    continue;
                case BeamTryNames.CaseEnd:
                    Read(operands[0]);
                    Exception();
                    continue;
                case BeamTryNames.RawRaise:
                    var classRegister = new BeamOperand(BeamOperandParameters.X, BigInteger.Zero);
                    var reasonRegister = new BeamOperand(BeamOperandParameters.X, BigInteger.One);
                    var traceRegister = new BeamOperand(BeamOperandParameters.X, new BigInteger(2));
                    Read(classRegister);
                    Read(reasonRegister);
                    Read(traceRegister, transport: true);
                    if (!Raw(traceRegister))
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    bool definitelyValidClass = Class(classRegister);
                    Exception();
                    if (definitelyValidClass)
                        continue;
                    // Native raw_raise returns badarg only for an invalid class.
                    // Other registers, including the saved trace, remain intact.
                    Write(classRegister);
                    break;
                case BeamLinkNames.Label:
                case BeamLinkNames.Line:
                case BeamLinkNames.OnLoad:
                case BeamNifNames.Start:
                    break;
                case BeamLinkNames.Move:
                    Read(operands[0], transport: true);
                    bool movedRaw = (operands[0].Kind is BeamOperandParameters.X or BeamOperandParameters.Y) && Raw(operands[0]);
                    bool movedClass = Class(operands[0]);
                    Write(
                        operands[1],
                        movedRaw,
                        movedClass,
                        Marker(operands[0]),
                        Context(operands[0]),
                        Position(operands[0])
                    );
                    break;
                case BeamGeneralOpcodeNames.Swap:
                    Read(operands[0], transport: true);
                    Read(operands[1], transport: true);
                    bool leftRaw = Raw(operands[0]);
                    bool rightRaw = Raw(operands[1]);
                    bool leftClass = Class(operands[0]);
                    bool rightClass = Class(operands[1]);
                    bool leftMarker = Marker(operands[0]);
                    bool rightMarker = Marker(operands[1]);
                    bool leftContext = Context(operands[0]);
                    bool rightContext = Context(operands[1]);
                    bool leftPosition = Position(operands[0]);
                    bool rightPosition = Position(operands[1]);
                    Write(
                        operands[0],
                        rightRaw,
                        rightClass,
                        rightMarker,
                        rightContext,
                        rightPosition
                    );
                    Write(
                        operands[1],
                        leftRaw,
                        leftClass,
                        leftMarker,
                        leftContext,
                        leftPosition
                    );
                    break;
                case BeamLinkNames.Allocate:
                case BeamLinkNames.AllocateHeap:
                    if (state.Slots != BeamFrameParameters.NoFrame)
                        throw new NotSupportedException(BeamOnLoadDiagnostics.Frame);
                    Prune((int)operands[name == BeamLinkNames.Allocate ? 1 : 2].Value);
                    state = state with { Slots = (int)operands[0].Value, InitializedY = BigInteger.Zero, TermY = BigInteger.Zero, RawY = BigInteger.Zero, MarkerY = BigInteger.Zero, ContextY = BigInteger.Zero, PositionY = BigInteger.Zero, ClassY = BigInteger.Zero };
                    break;
                case BeamLinkNames.HeapTest:
                    Roots();
                    Prune((int)operands[1].Value);
                    break;
                case BeamOnLoadNames.InitY:
                    foreach (BeamOperand operand in operands.Skip(BeamFrameParameters.TupleCountPrefix))
                        Write(operand);
                    break;
                case BeamGeneralOpcodeNames.Trim:
                    int removed = (int)operands[0].Value;
                    int remaining = (int)operands[1].Value;
                    if (state.Slots == BeamFrameParameters.NoFrame || removed + remaining != state.Slots)
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    state = state with
                    {
                        Slots = remaining,
                        InitializedY = state.InitializedY >> removed,
                        TermY = state.TermY >> removed,
                        RawY = state.RawY >> removed,
                        MarkerY = state.MarkerY >> removed,
                        ContextY = state.ContextY >> removed,
                        PositionY = state.PositionY >> removed,
                        ClassY = state.ClassY >> removed,
                        Handler = Shift(state.Handler, removed)
                    };
                    break;
                case BeamLinkNames.Deallocate:
                    NoTags();
                    if (state.Slots != operands[0].Value)
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    state = state with { Slots = BeamFrameParameters.NoFrame, InitializedY = BigInteger.Zero, TermY = BigInteger.Zero, RawY = BigInteger.Zero, MarkerY = BigInteger.Zero, ContextY = BigInteger.Zero, PositionY = BigInteger.Zero, ClassY = BigInteger.Zero };
                    break;
                case BeamGeneralOpcodeNames.Jump:
                    Branch(operands[0]);
                    continue;
                case BeamGeneralOpcodeNames.SelectValue:
                case BeamGeneralOpcodeNames.SelectTupleArity:
                    Read(operands[0]);
                    Branch(operands[1]);
                    for (int target = 4; target < operands.Count; target += 2)
                        Branch(operands[target]);
                    continue;
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
                case BeamLinkNames.IntegerTest:
                case BeamOnLoadNames.PidTest:
                case BeamOnLoadNames.TaggedTupleTest:
                    Read(operands[1]);
                    Branch(operands[0]);
                    break;
                case BeamGeneralOpcodeNames.FunctionTest:
                case BeamGeneralOpcodeNames.ArithmeticEqual:
                case BeamGeneralOpcodeNames.ArithmeticNotEqual:
                case BeamGeneralOpcodeNames.ExactNotEqual:
                case BeamGeneralOpcodeNames.GreaterOrEqual:
                case BeamGeneralOpcodeNames.Less:
                case BeamLinkNames.EqualityTest:
                    Read(operands[1]);
                    Read(operands[2]);
                    Branch(operands[0]);
                    break;
                case BeamGeneralOpcodeNames.GetList:
                    Read(operands[0]);
                    Write(operands[1]);
                    Write(operands[2]);
                    break;
                case BeamGeneralOpcodeNames.GetHead:
                case BeamGeneralOpcodeNames.GetTail:
                    Read(operands[0]);
                    Write(operands[1]);
                    break;
                case BeamOnLoadNames.TupleElement:
                    Read(operands[0]);
                    Write(operands[2]);
                    break;
                case BeamLinkNames.PutList:
                    Read(operands[0]);
                    Read(operands[1]);
                    Write(operands[2]);
                    break;
                case BeamLinkNames.PutTuple:
                    foreach (BeamOperand operand in operands.Skip(BeamLinkParameters.TupleListPrefixOperands))
                        Read(operand);
                    Write(operands[0]);
                    break;
                case BeamLinkNames.Bif:
                    Write(operands[1]);
                    break;
                case BeamGeneralOpcodeNames.UnaryBif:
                    if (Context(operands[2]) && BeamBinaryEntryProtocol.IsContextSizeImport(code, (int)operands[1].Value))
                        Read(operands[2], transport: true);
                    else
                        Read(operands[2]);
                    if (!BeamInfallibleBifContract.IsInfallible(code, instruction))
                        Branch(operands[0]);
                    Write(operands[3]);
                    break;
                case BeamGeneralOpcodeNames.BinaryBif:
                    Read(operands[2]);
                    Read(operands[3]);
                    if (!BeamInfallibleBifContract.IsInfallible(code, instruction))
                        Branch(operands[0]);
                    Write(operands[4]);
                    break;
                case BeamGeneralOpcodeNames.UnaryGcBif:
                    Roots();
                    Live((int)operands[1].Value);
                    if (Context(operands[3]) && BeamBinaryEntryProtocol.IsContextSizeImport(code, (int)operands[2].Value))
                        Read(operands[3], transport: true);
                    else
                        Read(operands[3]);
                    Prune((int)operands[1].Value);
                    Bif(operands[0], operands[4]);
                    break;
                case BeamLinkNames.GuardBif:
                    Roots();
                    Live((int)operands[1].Value);
                    Read(operands[3]);
                    Read(operands[4]);
                    Prune((int)operands[1].Value);
                    Bif(operands[0], operands[5]);
                    break;
                case BeamGeneralOpcodeNames.TernaryBif:
                    foreach (BeamOperand source in operands.Skip(2).Take(3))
                        Read(source);
                    Bif(operands[0], operands[5]);
                    break;
                case BeamGeneralOpcodeNames.TernaryGcBif:
                    Roots();
                    Live((int)operands[1].Value);
                    foreach (BeamOperand source in operands.Skip(3).Take(3))
                        Read(source);
                    Prune((int)operands[1].Value);
                    Bif(operands[0], operands[6]);
                    break;
                case BeamGeneralOpcodeNames.IfEnd:
                    Exception();
                    continue;
                case BeamGeneralOpcodeNames.MapFields:
                    Read(operands[1]);
                    foreach (BeamOperand key in operands.Skip(3))
                        Read(key);
                    Branch(operands[0]);
                    break;
                case BeamGeneralOpcodeNames.MapElements:
                    Read(operands[1]);
                    for (int pair = 3; pair < operands.Count; pair += 2)
                        Read(operands[pair]);
                    Branch(operands[0]);
                    for (int pair = 3; pair < operands.Count; pair += 2)
                        Write(operands[pair + 1]);
                    break;
                case BeamGeneralOpcodeNames.MapExact:
                case BeamGeneralOpcodeNames.MapAssociate:
                    Roots();
                    Live((int)operands[3].Value);
                    Read(operands[1]);
                    foreach (BeamOperand value in operands.Skip(5))
                        Read(value);
                    Branch(operands[0]);
                    Prune((int)operands[3].Value);
                    Write(operands[2]);
                    break;
                case BeamLambdaNames.Create:
                    foreach (BeamOperand value in operands.Skip(BeamLambdaNames.EnvironmentPrefixOperands))
                        Read(value);
                    Write(operands[1]);
                    break;
                case BeamRegisterNames.Call:
                case BeamLinkNames.ExternalCall:
                case BeamGeneralOpcodeNames.DynamicCall:
                case BeamApplyNames.Opcode:
                case BeamLambdaNames.Call:
                    if (state.Slots == BeamFrameParameters.NoFrame)
                        throw new NotSupportedException(BeamOnLoadDiagnostics.Frame);
                    if (name == BeamLambdaNames.Call)
                    {
                        Read(operands[2]);
                        Arguments((int)operands[1].Value);
                    }
                    else if (name is BeamGeneralOpcodeNames.DynamicCall or BeamApplyNames.Opcode)
                    {
                        Live((int)operands[0].Value + (name == BeamApplyNames.Opcode ? BeamApplyNames.ExtraRegisters : 1), arguments: true);
                        Exception();
                    }
                    else if (name == BeamRegisterNames.Call)
                        LocalArguments(operands[0], operands[1]);
                    else
                        Arguments((int)operands[0].Value);
                    if (name == BeamLinkNames.ExternalCall && (BeamNoReturnAnalysis.Import(code, (int)operands[1].Value)
                            || Class(new(BeamOperandParameters.X, BeamTryParameters.RawRaiseClassRegister))
                                && BeamRaiseEmptyTraceProof.Prove(code, function, state.Pc))
                        || name == BeamRegisterNames.Call && (BeamNoReturnAnalysis.Local(code, (int)operands[1].Value)
                            || BeamLiteralCallFailureProof.Prove(code, function, state.Pc)))
                        continue;
                    state = state with { InitializedX = BigInteger.Zero, TermX = Bit(0), RawX = BigInteger.Zero, MarkerX = BigInteger.Zero, ContextX = BigInteger.Zero, PositionX = BigInteger.Zero, ClassX = BigInteger.Zero };
                    break;
                case BeamLinkNames.Send:
                    Arguments(2);
                    state = state with { InitializedX = BigInteger.Zero, TermX = Bit(0), RawX = BigInteger.Zero, MarkerX = BigInteger.Zero, ContextX = BigInteger.Zero, PositionX = BigInteger.Zero, ClassX = BigInteger.Zero };
                    break;
                case BeamGeneralOpcodeNames.LocalLast:
                case BeamOnLoadNames.ExternalLast:
                    bool terminalFailure = name == BeamOnLoadNames.ExternalLast
                        && BeamNoReturnAnalysis.Import(code, (int)operands[1].Value);
                    if (terminalFailure)
                    {
                        Arguments((int)operands[0].Value);
                        continue;
                    }
                    NoTags();
                    if (state.Slots != operands[2].Value)
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    if (name == BeamGeneralOpcodeNames.LocalLast)
                        LocalArguments(operands[0], operands[1]);
                    else
                        Arguments((int)operands[0].Value);
                    continue;
                case BeamApplyNames.Last:
                    NoTags();
                    if (state.Slots != operands[1].Value)
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    Arguments((int)operands[0].Value + BeamApplyNames.ExtraRegisters);
                    continue;
                case BeamLinkNames.LocalCall:
                case BeamLinkNames.ExternalTailCall:
                    NoTags();
                    if (state.Slots != BeamFrameParameters.NoFrame)
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    if (name == BeamLinkNames.LocalCall)
                        LocalArguments(operands[0], operands[1]);
                    else
                        Arguments((int)operands[0].Value);
                    continue;
                case BeamLinkNames.Return:
                    NoTags();
                    if (state.Slots != BeamFrameParameters.NoFrame)
                        throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);
                    Live(1, arguments: true);
                    continue;
                case BeamOnLoadNames.BadMatch:
                case BeamGeneralOpcodeNames.CaseEnd:
                    Read(operands[0]);
                    Exception();
                    continue;
                default:
                    throw new NotSupportedException(BeamOnLoadDiagnostics.Frame);
            }
            if (BeamCollectionOpcodeNames.ClobbersFloats(name))
                state = state with { Floating = BigInteger.Zero };
            Enqueue(state.Pc + 1, state, exceptional: state.Pending != NoPendingHandler);
        }
    }

    private static BigInteger Bit(int index) => BigInteger.One << index;

    private static BigInteger Mask(int count) => (BigInteger.One << count) - BigInteger.One;

    private static Handler? Shift(Handler? tag, int removed)
    {
        if (tag is null)
            return null;
        if (tag.Slot < removed)
            throw new InvalidDataException(BeamOnLoadDiagnostics.Frame);

        return tag with { Slot = tag.Slot - removed, Parent = Shift(tag.Parent, removed) };
    }
}
