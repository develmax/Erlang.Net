using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private const string LookupListModule = "lists";
    private const string KeyFindFunction = "keyfind";
    private const int KeyFindArity = 3;
    private const int KeyFindPositionRegister = 1;
    private const int KeyFindListRegister = 2;
    private readonly List<(int Failure, int Arity, int Tag, int Line)> constrainedKeyFindTupleTests = [];

    private void RecordConstrainedKeyFindTupleTest(int failure, int arity, Atom tag)
    {
        if (!atoms.TryGetValue(tag.Name, out int name))
        {
            return;
        }

        var location = lastEmittedLine.File == implicitLineFile
            ? lastEmittedLine with { File = null }
            : lastEmittedLine;
        constrainedKeyFindTupleTests.Add((failure, arity, name, lines.GetValueOrDefault(location)));
    }

    private byte[]? AnnotateKeyFindTupleTests(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        string[] atomNames,
        (int Module, int Function, int Arity)[] importsByIndex
    )
    {
        if (!SsaTypeInferenceEnabled)
        {
            return null;
        }

        int lookup = Array.FindIndex(
            importsByIndex,
            entry => entry.Arity == KeyFindArity
            && atomNames[entry.Module - 1] == LookupListModule && atomNames[entry.Function - 1] == KeyFindFunction
        );
        if (lookup < 0)
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 2 < body.Count; index++)
        {
            if (body[index] is not { Definition.Opcode: ExternalCall, Operands: [var arity, var import] }
                || arity.Value != KeyFindArity || import.Value != lookup)
            {
                continue;
            }

            int consumer = index + 1;
            while (consumer < body.Count - 1 && body[consumer].Definition.Opcode == Line)
            {
                consumer++;
            }

            if (consumer + 1 >= body.Count || body[consumer] is not
                {
                    Definition.Opcode: IsTuple,
                    Operands: [var failure, { Kind: BeamOperandParameters.X, Value.IsZero: true, TypeIndex: null }]
                }
                || !HasKnownKeyFindArguments(body, index)
                || IsConstrainedKeyFindTupleTest(
                    body,
                    index,
                    consumer + 1,
                    (int)failure.Value
                ))
            {
                continue;
            }

            // With no intervening label, the tuple test reads only the successful returned value.
            output.Write(code.Span[copied..body[consumer].Offset]);
            output.WriteByte(IsTuple);
            BeamCompactWriter.Write(output, LabelTag, failure.Value);
            output.WriteByte(ExtendedTypedRegisterOperand);
            BeamCompactWriter.Write(output, XRegisterTag, 0);
            int type = GetOperandTypeIndex((AtomTypeBits | TupleTypeBits, null, null, null));
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, type);
            copied = body[consumer + 1].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private bool IsConstrainedKeyFindTupleTest(
        IReadOnlyList<BeamInstruction> body,
        int call,
        int arityCheck,
        int failure
    )
    {
        if (constrainedKeyFindTupleTests.Count == 0 || body[arityCheck] is not
            {
                Definition.Opcode: TestArity,
                Operands: [var failed, { Kind: BeamOperandParameters.X, Value.IsZero: true, TypeIndex: null }, var arity]
            } || failed.Value != failure
            || !constrainedKeyFindTupleTests.Any(test => MapEmittedLabel(test.Failure) == failure && test.Arity == arity.Value))
        {
            return false;
        }

        int? line = null;
        int? tag = null;
        bool? firstPosition = null;
        for (int index = call - 1; index >= 0; index--)
        {
            BeamInstruction instruction = body[index];
            if (instruction.Definition.Opcode == Line)
            {
                line ??= (int)instruction.Operands[0].Value;
                continue;
            }

            if (instruction.Definition.Opcode == InitYRegisters)
            {
                continue;
            }

            if (instruction is not { Definition.Opcode: Move, Operands: [var value, var target] })
            {
                break;
            }

            if (target.Kind != BeamOperandParameters.X)
            {
                continue;
            }

            if (target.Value.IsZero && tag is null)
            {
                if (value.Kind != BeamOperandParameters.Atom)
                {
                    return false;
                }

                tag = (int)value.Value;
            }
            else if (target.Value == KeyFindPositionRegister)
            {
                firstPosition ??= value.Kind == BeamOperandParameters.Integer && value.Value.IsOne;
            }
        }

        // An SSA tagged-tuple test narrowed to tuple/arity uses bare register operands.
        return firstPosition == true && tag.HasValue && constrainedKeyFindTupleTests.Any(test =>
            MapEmittedLabel(test.Failure) == failure && test.Arity == arity.Value
            && test.Tag == tag.Value && test.Line == line.GetValueOrDefault());
    }

    private static bool HasKnownKeyFindArguments(IReadOnlyList<BeamInstruction> body, int call)
    {
        bool validPosition = false;
        bool sawList = false;
        for (int index = call - 1; index >= 0; index--)
        {
            BeamInstruction instruction = body[index];
            if (instruction.Definition.Opcode is Line or InitYRegisters)
            {
                continue;
            }

            if (instruction is not { Definition.Opcode: Move, Operands: [var value, var destination] })
            {
                break;
            }

            if (destination.Kind != BeamOperandParameters.X)
            {
                continue;
            }

            if (!validPosition && destination.Value == KeyFindPositionRegister)
            {
                if (value.Kind != BeamOperandParameters.Integer || value.Value.Sign <= 0
                    || value.Value >= SourceBifReturnParameters.MaximumTupleArity)
                {
                    return false;
                }

                validPosition = true;
            }

            if (!sawList && destination.Value == KeyFindListRegister)
            {
                if (value.Kind is BeamOperandParameters.Atom or BeamOperandParameters.Integer or BeamOperandParameters.Float)
                {
                    return false;
                }

                sawList = true;
            }

            if (validPosition && sawList)
            {
                break;
            }
        }

        return validPosition;
    }
}
