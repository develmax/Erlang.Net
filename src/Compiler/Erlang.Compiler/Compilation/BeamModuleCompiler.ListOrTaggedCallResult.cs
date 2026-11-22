using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? SimplifyListOrTaggedCallResult(
        ReadOnlyMemory<byte> code,
        int labelCount,
        IReadOnlyList<FunctionDefinition> functions,
        IReadOnlyDictionary<(string Name, int Arity), int> entries
    )
    {
        if (!SsaTypeInferenceEnabled || ModuleOptimizationDisabled || OnLoadFunction is not null
            || !code.Span.Contains(IsTaggedTuple))
        {
            return null;
        }

        IReadOnlyList<BeamInstruction> body;
        try
        {
            body = BeamInstructionReader.Read(
                new BeamCodeHeader(
                    BeamWire.FormatVersion,
                    highestOpcode,
                    labelCount,
                    exports.Count + locals.Count,
                    code
                ),
                atoms.Count + 1,
                literalBytes.Count
            );
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return null;
        }

        var names = atoms.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        var result = new BeamOperand(BeamOperandParameters.X, 0);
        var localFunctions = new Dictionary<(string Name, int Arity), FunctionDefinition>();
        var functionsByEntry = new Dictionary<int, FunctionDefinition>();
        foreach (var function in functions)
        {
            var signature = (function.Name, function.Arity);
            if (function.IsInlineFailure || IsDeclaredNative(signature) || !entries.TryGetValue(signature, out int label)
                || (label <= 0 && !deferredEntries.TryGetValue(label, out label)))
            {
                continue;
            }

            localFunctions.TryAdd(signature, function);
            functionsByEntry.TryAdd(MapEmittedLabel(label), function);
        }

        using var output = new MemoryStream();
        int copied = 0;
        var facts = new Dictionary<(int Entry, int Arity, string Tag), int>();
        for (int index = 0; index + 2 < body.Count; index++)
        {
            if (body[index] is not { Definition.Opcode: LocalCall, Operands: [_, var callee] }
                || !functionsByEntry.TryGetValue((int)callee.Value, out var function))
            {
                continue;
            }

            int test = index + 1;
            while (test < body.Count && body[test].Definition.Opcode == Line)
            {
                test++;
            }

            if (test + 1 >= body.Count || body[test] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [var failed, var tested, var arity, { Kind: BeamOperandParameters.Atom } tag]
                } || tested.Kind != BeamOperandParameters.X || tested.Value != 0)
            {
                continue;
            }

            var key = (Entry: (int)callee.Value, Arity: (int)arity.Value, Tag: names[(int)tag.Value - 1]);
            if (!facts.TryGetValue(key, out int shape))
            {
                shape = ReadClosedTaggedCallResult(
                    function,
                    key.Arity,
                    key.Tag,
                    localFunctions
                );
                if ((shape & TupleTypeBits) == 0 || (shape & ProperListTypeBits) == 0)
                {
                    shape = ReadClosedTaggedCallResult(
                        function,
                        key.Arity,
                        key.Tag,
                        localFunctions,
                        allowAtom: true
                    );
                }

                facts.Add(key, shape);
            }

            if (shape != LocalAtomOrTupleType
                && (tested != result || (shape & TupleTypeBits) == 0 || (shape & ProperListTypeBits) == 0))
            {
                continue;
            }

            output.Write(code.Span[copied..body[test].Offset]);
            output.WriteByte(IsTuple);
            BeamCompactWriter.Write(output, LabelTag, failed.Value);
            output.WriteByte(ExtendedTypedRegisterOperand);
            WriteRegister(output, result);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, GetOperandTypeIndex((shape, null, null, null)));
            copied = body[test + 1].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);
        highestOpcode = Math.Max(highestOpcode, IsTuple);

        return output.ToArray();
    }

    private int ReadClosedTaggedCallResult(
        FunctionDefinition function,
        int arity,
        string tag,
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> definitions,
        bool allowAtom = false
    )
    {
        if (arity <= 0 || IsDeclaredNative((function.Name, function.Arity)))
        {
            return 0;
        }

        var seen = new HashSet<(string Name, int Arity)> { (function.Name, function.Arity) };
        var remaining = new Stack<Expr>(function.Clauses.Select(clause => clause.Body));
        int shape = 0;
        while (remaining.TryPop(out Expr? expression))
        {
            switch (expression)
            {
                case Expr.Sequence { Items.Count: > 0 } sequence:
                    remaining.Push(sequence.Items[^1]);
                    break;
                case Expr.Block block:
                    remaining.Push(block.Body);
                    break;
                case Expr.Match match:
                    remaining.Push(match.Value);
                    break;
                case Expr.Case decision:
                    PushBranches(decision.Clauses, matchSelector: allowAtom);
                    break;
                case Expr.If decision:
                    PushBranches(decision.Clauses);
                    break;
                case Expr.Try { Clauses.Count: > 0, After: null } guarded when allowAtom:
                    PushBranches(guarded.Clauses, matchSelector: true);
                    PushBranches(guarded.Catches);
                    break;
                case Expr.RecordConstruction { Wildcard: null } record when allowAtom:
                    remaining.Push(RecordExpressions.Create(record));
                    break;
                case Expr.Literal { Value: Atom } when allowAtom:
                    shape |= AtomTypeBits;
                    break;
                case Expr.Literal { Value: TupleTerm tuple } when tuple.Items.Count == arity
                    && tuple.Items[0] is Atom literal && literal.Name == tag:
                case Expr.Tuple value when value.Items.Count == arity
                    && (value.Items[0] is Expr.Literal { Value: Atom explicitTag } && explicitTag.Name == tag
                        || allowAtom && AttributeLiteral.TryNormalizeConstant(value.Items[0], out Term constantTag)
                        && constantTag is Atom normalizedTag && normalizedTag.Name == tag):
                    shape |= TupleTypeBits;
                    break;
                case Expr.Literal { Value: Cons } when !allowAtom:
                case Expr.List { Items.Count: > 0 } when !allowAtom:
                    shape |= ConsTypeBits;
                    break;
                case Expr.Literal { Value: Nil } when !allowAtom:
                case Expr.List { Items.Count: 0, Tail: null } when !allowAtom:
                    shape |= ProperListTypeBits & ~ConsTypeBits;
                    break;
                case Expr.Call { Module: null, DynamicRemote: false } call
                    when definitions.TryGetValue((call.Function, call.Arguments.Count), out var local):
                    var signature = (call.Function, call.Arguments.Count);
                    if (IsDeclaredNative(signature))
                    {
                        return 0;
                    }

                    if (seen.Add(signature))
                    {
                        PushBranches(local.Clauses);
                    }

                    break;
                case Expr.Call { DynamicRemote: false } call
                    when !allowAtom && (call.Module, call.Function, call.Arguments.Count) is ("lists", "reverse", 1):
                    shape |= call.Arguments[0] switch
                    {
                        Expr.List { Items.Count: > 0 } or Expr.Literal { Value: Cons } => ConsTypeBits,
                        Expr.List { Items.Count: 0, Tail: null } or Expr.Literal { Value: Nil } => ProperListTypeBits & ~ConsTypeBits,
                        _ => ProperListTypeBits
                    };
                    break;
                case Expr.Call call when IsKnownNonreturningLocalResult(call):
                    break;
                default:
                    // Unknown return paths or another tuple tag invalidate the proof.
                    return 0;
            }
        }

        return shape;

        void PushBranches(IReadOnlyList<Clause> clauses, bool matchSelector = false)
        {
            foreach (var clause in clauses)
            {
                if (matchSelector && clause is
                    {
                        Patterns: [AliasPattern alias],
                        Body: Expr.Variable { Name: not VariableScopeNames.Wildcard } returned
                    }
                    && (MatchesTupleAlias(alias.Left, alias.Right, returned.Name)
                        || MatchesTupleAlias(alias.Right, alias.Left, returned.Name)))
                {
                    shape |= TupleTypeBits;
                }
                else
                {
                    remaining.Push(clause.Body);
                }
            }
        }

        bool MatchesTupleAlias(Pattern alias, Pattern source, string name) =>
            alias is Pattern.Variable variable && variable.Name == name
            && source is Pattern.Tuple tuple && tuple.Items.Count == arity
            && tuple.Items[0] is Pattern.Literal { Value: Atom matched } && matched.Name == tag;
    }

    private byte[]? AnnotateLocalTuplePairResults(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsTaggedTuple)
            || !code.Span.Contains(LocalCall)
            || ReadPrivateEntryFacts() is not { } facts
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        // This scan uses final physical entries. No later optimizer needs to
        // interpret a typed operand as a plain register in a structural match.
        var names = atoms.Where(pair => MapEmittedAtom(pair.Value) > 0)
            .ToDictionary(pair => MapEmittedAtom(pair.Value), pair => pair.Key);
        var pairEntries = new HashSet<int>();
        for (int index = 0; index + 1 < body.Count; index++)
        {
            if (body[index] is { Definition.Opcode: FunctionInfo, Operands: [_, var atom, var arity] }
                && body[index + 1] is { Definition.Opcode: Label, Operands: [var entry] }
                && names.TryGetValue((int)atom.Value, out string? name))
            {
                var signature = (name, (int)arity.Value);
                if (!IsDeclaredNative(signature) && facts.ProvesTuplePairResult(signature))
                {
                    pairEntries.Add((int)entry.Value);
                }
            }
        }

        using var output = new MemoryStream(code.Length);
        int copied = 0;
        for (int index = 0; index + 1 < body.Count; index++)
        {
            if (body[index] is not { Definition.Opcode: LocalCall, Operands: [_, var callee] }
                || !pairEntries.Contains((int)callee.Value))
            {
                continue;
            }

            int test = index + 1;
            while (test < body.Count && body[test].Definition.Opcode == Line)
            {
                test++;
            }

            if (test < body.Count && body[test] is
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [_, var result, { Value: var arity }, _]
                }
                && arity == 2 && IsBareX0(result))
            {
                copied = AnnotateRegisterInput(
                    code,
                    output,
                    copied,
                    body[test],
                    1,
                    TupleTypeBits
                );
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
