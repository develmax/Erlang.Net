using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record ConstantCall(Expr.Call Source, List<(int Destination, Term Value)> Arguments);
    private sealed record ConstantCallSequence(List<ConstantCall> Calls, Term? Result);

    private static ConstantCallSequence? ReadConstantCallSequence(Clause clause)
    {
        if (clause is not { Guard: null, Body: Expr.Sequence { Items.Count: > 1 } sequence }
            || !(HasUnconditionalParameters(clause) || HasNilParameter(clause)))
        {
            return null;
        }

        var parameters = clause.Patterns.OfType<Pattern.Variable>().Select(parameter => parameter.Name)
            .ToHashSet(StringComparer.Ordinal);
        var bindings = new Dictionary<string, Term>(StringComparer.Ordinal);
        var calls = new List<ConstantCall>();
        Term? result = null;
        for (int index = 0; index < sequence.Items.Count; index++)
        {
            Expr expression = sequence.Items[index];
            bool last = index == sequence.Items.Count - 1;
            if (expression is Expr.Call { DynamicRemote: false } call
                && (call.Module != ErlangModuleNames.Module || IsConstantSequenceBif(call)))
            {
                var arguments = new List<(int Destination, Term Value)>();
                foreach (Expr argument in call.Arguments)
                {
                    if (ReadSequenceConstant(argument, bindings) is not { } constant)
                    {
                        return null;
                    }
                    arguments.Add((arguments.Count, constant));
                }
                calls.Add(new(call, arguments));
            }
            else if (!last && expression is Expr.Match { Pattern: Pattern.Variable binding, Value: var value }
                && binding.Name != VariableScopeNames.Wildcard && !parameters.Contains(binding.Name)
                && ReadSequenceConstant(value, bindings) is { } constant && bindings.TryAdd(binding.Name, constant))
            {
                continue;
            }
            else if (!last || (result = ReadSequenceConstant(expression, bindings)) is null)
            {
                return null;
            }
        }

        return calls.Count > 1 || (calls.Count == 1 && result is not null)
            ? new(calls, result)
            : null;
    }

    private static bool IsConstantSequenceBif(Expr.Call call) =>
        (call.Function, call.Arguments.Count) is (AutoImportNames.ProcessFlag, 2 or 3);

    private static Term? ReadSequenceConstant(Expr expression, IReadOnlyDictionary<string, Term> bindings)
    {
        if (AttributeLiteral.TryNormalizeConstant(expression, out Term constant, bindings))
        {
            return constant;
        }
        if (expression is not Expr.Tuple tuple)
        {
            return ReadExternalFunctionLiteral(expression);
        }

        var items = new Term[tuple.Items.Count];
        for (int index = 0; index < items.Length; index++)
        {
            if (ReadSequenceConstant(tuple.Items[index], bindings) is not { } item)
            {
                return null;
            }
            items[index] = item;
        }

        return new TupleTerm(items);
    }

    private bool TryEmitConstantCallSequence(
        Clause clause,
        BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries
    )
    {
        if (ReadConstantCallSequence(clause) is not { } sequence)
        {
            return false;
        }

        var calls = new List<ConstantCall>(sequence.Calls.Count);
        foreach (var (source, arguments) in sequence.Calls)
        {
            Expr.Call call = ResolveImportedCall(source, entries);
            if (call.Module is null && !entries.ContainsKey((call.Function, arguments.Count)))
            {
                call = call with { Module = ResolveExternalModule(call.Function, arguments.Count) };
                if (call.Module is null || call.Module == ErlangModuleNames.Module && !IsConstantSequenceBif(call))
                {
                    return false;
                }
            }
            calls.Add(new(call, arguments));
        }

        Op(Allocate, BeamWire.UnsignedTag, 0);
        Operand(BeamWire.UnsignedTag, 0);
        for (int index = 0; index < calls.Count; index++)
        {
            var (call, arguments) = calls[index];
            bool tail = sequence.Result is null && index == calls.Count - 1;
            EmitIndependentCallMoves(arguments);
            if (!tail || call.Module is not null)
            {
                EmitCallLine(call, location);
                location = CallLocation(call, location);
            }
            if (call.Module is null)
            {
                Op(tail ? TailLocalCallWithFrame : LocalCall, BeamWire.UnsignedTag, arguments.Count);
                Operand(LabelTag, entries[(call.Function, arguments.Count)]);
            }
            else
            {
                Op(tail ? TailExternalCallWithFrame : ExternalCall, BeamWire.UnsignedTag, arguments.Count);
                Operand(BeamWire.UnsignedTag, Import(call.Module, call.Function, arguments.Count));
            }
            if (tail)
            {
                Operand(BeamWire.UnsignedTag, 0);
            }
        }

        if (sequence.Result is { } result)
        {
            EmitConstant(result, 0);
            EmitFrameReturn();
        }

        return true;
    }
}
