using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private readonly Dictionary<int, (int Import, int? Handler)> preparedMapDefaultCalls = [];

    private HashSet<Expr.Call>? originalTerminalMapDefaults;

    private bool IsOriginalTerminalMapDefault(Expr.Call call)
    {
        if (call.DynamicRemote || sourceModule is null)
        {
            return false;
        }

        originalTerminalMapDefaults ??= sourceModule.DeclaredFunctions
            .SelectMany(function => LocalFunctionEntries.ClauseExpressions(function.Clauses))
            .SelectMany(LocalFunctionEntries.Walk).OfType<Expr.Call>()
            .Where(source => !source.DynamicRemote && source.Function == MapModuleNames.Get && source.Arguments.Count == 3)
            .ToHashSet<Expr.Call>(ReferenceEqualityComparer.Instance);

        return originalTerminalMapDefaults.Contains(call);
    }

    private void MarkPreparedMapDefault(int? handler)
    {
        int label = nextLabel++;
        preparedMapDefaultCalls.Add(label, (Import(MapModuleNames.Module, MapModuleNames.Get, 3), handler));
        Op(Label, BeamWire.UnsignedTag, label);
    }

    private sealed partial class GeneralEmitter
    {
        private HashSet<Expr.Call>? originalMapDefaultCalls;
        private int? preparedMapDefaultHandler;

        private void MarkPlannedMapDefault(Expr.Call source, string? module)
        {
            if (module == MapModuleNames.Module && source.Function == MapModuleNames.Get && source.Arguments.Count == 3)
            {
                MarkValueMapDefault(source, source.Module == module ? source : source with { Module = module });
            }
        }

        private void MarkValueMapDefault(Expr.Call source, Expr.Call resolved)
        {
            if (!owner.CoreMapCallsEnabled || parentFunction is null || resolved is not
                { Module: MapModuleNames.Module, Function: MapModuleNames.Get, DynamicRemote: false, Arguments.Count: 3 })
            {
                return;
            }

            // Specializing dynamic remote syntax can later synthesize a static
            // import. Only the originally static source call can be folded.
            originalMapDefaultCalls ??= LocalFunctionEntries.ClauseExpressions(parentFunction.Clauses)
                .SelectMany(LocalFunctionEntries.Walk).OfType<Expr.Call>()
                .Where(call => !call.DynamicRemote && call.Function == MapModuleNames.Get && call.Arguments.Count == 3)
                .ToHashSet<Expr.Call>(ReferenceEqualityComparer.Instance);
            if (originalMapDefaultCalls.Contains(source))
            {
                owner.MarkPreparedMapDefault(preparedMapDefaultHandler);
            }
        }
    }

    private ReadOnlyMemory<byte> CompactPreparedMapDefaultLabels(ReadOnlyMemory<byte> code, ref int labelCount)
    {
        if (preparedMapDefaultCalls.Count == 0
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return code;
        }

        int count = body.Count(instruction => instruction.Definition.Opcode == Label) + 1;
        if (RenumberOutlinedMapCaseLabels(
            code,
            labelCount,
            physicalLabelCount: count
        ) is not { } reordered)
        {
            return code;
        }

        labelCount = count;

        return reordered;
    }

    private ReadOnlyMemory<byte> LowerPreparedMapDefaults(ReadOnlyMemory<byte> code)
    {
        if (preparedMapDefaultCalls.Count == 0
            || DecodeLateAtomicProjectionSlots(code, nextLabel) is not { Body: var body })
        {
            return code;
        }

        using var output = new MemoryStream();
        int copied = 0;
        int entry = -1;
        bool changedCalls = false;
        BeamProjectionFlow? mapFlow = null;
        for (int next = 0; next < body.Count; next++)
        {
            if (body[next].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int end = next;
            if (body[next].Definition.Opcode == FunctionInfo && next > 0)
            {
                int boundary = body[next - 1].Definition.Opcode == Line ? next - 2 : next - 1;
                if (boundary >= 0 && body[boundary].Definition.Opcode == Label)
                {
                    end = boundary;
                }
            }

            if (entry >= 0 && entry < end)
            {
                WriteFunction(entry, end);
            }

            entry = next + 1;
        }

        output.Write(code.Span[copied..]);

        byte[] rewritten = output.ToArray();

        return changedCalls ? RebuildLineTable(rewritten, nextLabel) : rewritten;

        void WriteFunction(int first, int end)
        {
            if (!Enumerable.Range(first, end - first).Any(index => IsMarker(body[index])))
            {
                return;
            }

            bool closed = EndsBeamBlock(body[end - 1].Definition.Opcode);
            var ownership = closed
                ? ReadDisjointFrameOwnership(
                    body,
                    first,
                    end,
                    trackSiblingTrimEpochs: true,
                    allowFrameless: true
                )
                : null;
            var failures = new List<(int Label, byte Opcode, int? Frame)>();
            for (int index = first; index < end; index++)
            {
                BeamInstruction marker = body[index];
                if (!IsMarker(marker))
                {
                    continue;
                }

                int call = index + 1;
                if (call < end && body[call].Definition.Opcode == Line)
                {
                    call++;
                }

                output.Write(code.Span[copied..marker.Offset]);
                copied = body[index + 1].Offset;
                var (imported, handler) = preparedMapDefaultCalls[(int)marker.Operands[0].Value];
                if (call >= end || !closed)
                {
                    continue;
                }

                BeamInstruction invocation = body[call];
                byte opcode = invocation.Definition.Opcode;
                bool terminal = opcode is TailExternalCall or TailExternalCallWithFrame;
                if (!(terminal || opcode == ExternalCall) || invocation.Operands[0].Value != 3
                    || invocation.Operands[1].Value != imported)
                {
                    continue;
                }

                byte failureCall;
                int? failureFrame;
                if (handler is int handlerLabel)
                {
                    if (terminal || !HasPreparedMapDefaultHandler(
                        body,
                        first,
                        end,
                        call,
                        handlerLabel
                    ))
                    {
                        continue;
                    }

                    failureCall = ExternalCall;
                    failureFrame = null;
                }
                else
                {
                    if (ownership is not { } frame)
                    {
                        continue;
                    }

                    int allocation = frame.Owners[call - first];
                    if (allocation == UnframedOwner && opcode == TailExternalCall)
                    {
                        failureCall = TailExternalCall;
                        failureFrame = null;
                    }
                    else if (allocation >= 0 && opcode != TailExternalCall)
                    {
                        int width = frame.Uses[allocation].Length;
                        if (opcode == TailExternalCallWithFrame && invocation.Operands[2].Value != width)
                        {
                            continue;
                        }

                        failureCall = TailExternalCallWithFrame;
                        failureFrame = width;
                    }
                    else
                    {
                        continue;
                    }
                }

                // Later physical blocks can share the call site's source line.
                if (!terminal || call + 1 < end)
                {
                    output.Write(code.Span[copied..invocation.Offset]);
                }

                bool knownMap = SsaTypeInferenceEnabled && (mapFlow ??= new BeamProjectionFlow(body))
                    .HasDominatingMapCheck(call, new BeamOperand(BeamOperandParameters.X, 1), allowStack: true);
                int absent = nextLabel++;
                int? invalid = knownMap ? null : nextLabel++;
                int? joined = terminal ? null : nextLabel++;
                EmitLookup(
                    absent,
                    invalid,
                    joined,
                    failureFrame
                );
                if (invalid is int failure)
                {
                    failures.Add((failure, failureCall, failureFrame));
                }

                changedCalls = true;
                copied = body[call + 1].Offset;
            }

            if (failures.Count == 0)
            {
                return;
            }

            output.Write(code.Span[copied..body[end].Offset]);
            copied = body[end].Offset;
            int error = Import(ErlangModuleNames.Module, ErlangModuleNames.Error, 1);
            int badMap = Intern(ErlangErrorReasons.BadMap);
            foreach (var (label, opcode, frame) in failures)
            {
                WriteTag(Label, BeamWire.UnsignedTag, label);
                WriteTag(TestHeap, BeamWire.UnsignedTag, 3);
                Operand(BeamWire.UnsignedTag, 2);
                WriteTag(PutTuple, XRegisterTag, 0);
                output.WriteByte(ExtendedListOperand);
                Operand(BeamWire.UnsignedTag, 2);
                Operand(AtomTag, badMap);
                Operand(XRegisterTag, 1);
                WriteTag(Line, BeamWire.UnsignedTag, 0);
                WriteTag(opcode, BeamWire.UnsignedTag, 1);
                Operand(BeamWire.UnsignedTag, error);
                if (frame is int width)
                {
                    Operand(BeamWire.UnsignedTag, width);
                }
            }

            highestOpcode = Math.Max(highestOpcode, PutTuple);
        }

        bool IsMarker(BeamInstruction instruction) => instruction.Definition.Opcode == Label
            && preparedMapDefaultCalls.ContainsKey((int)instruction.Operands[0].Value);

        void EmitLookup(
            int absent,
            int? invalid,
            int? joined,
            int? frame
        )
        {
            if (invalid is int failure)
            {
                WriteTag(IsMap, LabelTag, failure);
                Operand(XRegisterTag, 1);
            }

            WriteTag(GetMapElements, LabelTag, absent);
            if (SsaTypeInferenceEnabled)
            {
                output.WriteByte(ExtendedTypedRegisterOperand);
            }

            Operand(XRegisterTag, 1);
            if (SsaTypeInferenceEnabled)
            {
                Operand(BeamWire.UnsignedTag, GetOperandTypeIndex((MapTypeBits, null, null, null)));
            }

            output.WriteByte(ExtendedListOperand);
            Operand(BeamWire.UnsignedTag, 2);
            Operand(XRegisterTag, 0);
            Operand(XRegisterTag, 0);
            if (joined is int join)
            {
                WriteTag(Jump, LabelTag, join);
                WriteTag(Label, BeamWire.UnsignedTag, absent);
                WriteTag(Move, XRegisterTag, 2);
                Operand(XRegisterTag, 0);
                WriteTag(Label, BeamWire.UnsignedTag, join);

                return;
            }

            EmitReturn(frame);
            WriteTag(Label, BeamWire.UnsignedTag, absent);
            WriteTag(Move, XRegisterTag, 2);
            Operand(XRegisterTag, 0);
            EmitReturn(frame);
        }

        void EmitReturn(int? frame)
        {
            if (frame is int width)
            {
                WriteTag(Deallocate, BeamWire.UnsignedTag, width);
            }

            output.WriteByte(Return);
        }

        void WriteTag(byte opcode, int tag, int value)
        {
            output.WriteByte(opcode);
            Operand(tag, value);
        }

        void Operand(int tag, int value) => BeamCompactWriter.Write(output, tag, value);
    }

    private static bool HasPreparedMapDefaultHandler(
        IReadOnlyList<BeamInstruction> body,
        int first,
        int end,
        int call,
        int handlerLabel
    )
    {
        int enter = call - 1;
        for (; enter >= first; enter--)
        {
            byte opcode = body[enter].Definition.Opcode;
            if (opcode == TryEnter)
            {
                break;
            }

            if (opcode is TryCase or TryEnd or TryCaseEnd or CatchEnter or CatchEnd or TrimFrame
                or Deallocate or Allocate or AllocateHeap)
            {
                return false;
            }
        }

        if (enter < first || body[enter].Operands[1].Value != handlerLabel)
        {
            return false;
        }

        int handler = call + 1;
        for (; handler + 1 < end; handler++)
        {
            if (body[handler].Definition.Opcode == Label && body[handler].Operands[0].Value == handlerLabel)
            {
                break;
            }
        }

        if (handler + 1 >= end || body[handler + 1].Definition.Opcode != TryCase
            || body[handler + 1].Operands[0].Value != body[enter].Operands[0].Value)
        {
            return false;
        }

        var interiorLabels = new HashSet<int>();
        for (int index = enter + 1; index < call; index++)
        {
            if (body[index].Definition.Opcode == Label)
            {
                interiorLabels.Add((int)body[index].Operands[0].Value);
            }
        }

        for (int index = first; index < end; index++)
        {
            if (index >= enter && index < call
                || body[index].Definition.Opcode is LocalCall or TailLocalCall or TailLocalCallWithFrame)
            {
                continue;
            }

            if (body[index].Operands.Any(operand => operand.Kind == BeamOperandParameters.Label
                && interiorLabels.Contains((int)operand.Value)))
            {
                return false;
            }
        }

        return true;
    }
}
