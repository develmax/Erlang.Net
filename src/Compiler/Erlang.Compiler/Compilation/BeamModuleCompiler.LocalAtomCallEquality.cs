using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? AnnotateLocalAtomCallEquality(
        ReadOnlyMemory<byte> code,
        int labelCount
    )
    {
        if (properListSourceFunctions is not { } definitions || ModuleOptimizationDisabled || !SsaTypeInferenceEnabled
            || (!code.Span.Contains(ExactEqualTest) && !code.Span.Contains(SelectValue)) || !code.Span.Contains(LocalCall)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        GeneralEmitter? emitter = null;
        Dictionary<int, (string Name, int Arity)>? signatures = null;
        var checkedResults = new Dictionary<int, int>();
        int trueAtom = MapEmittedAtom(atoms.GetValueOrDefault(ErlangBooleanAtoms.True));
        int falseAtom = MapEmittedAtom(atoms.GetValueOrDefault(ErlangBooleanAtoms.False));
        using var output = new MemoryStream(code.Length);
        int copied = 0;
        for (int index = 0; index + 1 < body.Count; index++)
        {
            if (body[index] is not { Definition.Opcode: LocalCall, Operands: [_, var target] })
            {
                continue;
            }

            int next = index + 1;
            while (next < body.Count && body[next].Definition.Opcode == Line)
            {
                next++;
            }

            if (next == body.Count)
            {
                continue;
            }

            BeamInstruction test = body[next];
            int operand = test switch
            {
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [_, var input, { Kind: BeamOperandParameters.Atom } literal]
                } when IsBareX0(input) && literal.Value != trueAtom && literal.Value != falseAtom => 1,
                {
                    Definition.Opcode: SelectValue,
                    Operands: [var input, _, _, _, _, _, _, ..]
                } when IsBareX0(input) && IsAtomEntrySelection(test.Operands) => 0,
                _ => -1
            };
            if (operand < 0)
            {
                continue;
            }

            int label = (int)target.Value;
            if (!checkedResults.TryGetValue(label, out int resultType))
            {
                signatures ??= ReadLocalSignatures();
                if (signatures.TryGetValue(label, out var signature) && !IsDeclaredNative(signature)
                    && definitions.ContainsKey(signature))
                {
                    emitter ??= new GeneralEmitter(
                        this,
                        signatures.GroupBy(pair => pair.Value).ToDictionary(group => group.Key, group => group.First().Key),
                        definitions,
                        default,
                        frameSize: 0
                    );
                    resultType = emitter.ReadLocalAtomComparisonResultType(signature);
                }

                checkedResults.Add(label, resultType);
            }

            if (resultType != 0 && (test.Definition.Opcode == ExactEqualTest || resultType == AtomTypeBits))
            {
                copied = AnnotateRegisterInput(
                    code,
                    output,
                    copied,
                    test,
                    operand,
                    resultType
                );
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        Dictionary<int, (string Name, int Arity)> ReadLocalSignatures()
        {
            var result = new Dictionary<int, (string Name, int Arity)>();
            var names = atoms.Where(pair => MapEmittedAtom(pair.Value) > 0)
                .ToDictionary(pair => MapEmittedAtom(pair.Value), pair => pair.Key);
            for (int index = 0; index + 1 < body.Count; index++)
            {
                if (body[index] is { Definition.Opcode: FunctionInfo, Operands: [_, var name, var arity] }
                    && body[index + 1] is { Definition.Opcode: Label, Operands: [var entry] }
                    && names.TryGetValue((int)name.Value, out string? signatureName))
                {
                    result.TryAdd((int)entry.Value, (signatureName, (int)arity.Value));
                }
            }

            return result;
        }
    }
}
