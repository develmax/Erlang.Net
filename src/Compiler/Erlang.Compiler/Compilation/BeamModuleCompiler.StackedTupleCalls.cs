using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private bool TryEmitStackedTupleCalls(Clause clause, BeamSourceLocation location)
    {
        if (!HasNilParameter(clause) || clause.Guard is not null
            || clause.Body is not Expr.Sequence
            {
                Items: [
                Expr.Match
                {
                    Pattern: Pattern.Variable saved, Value: Expr.Tuple
                    {
                        Items: [.., Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr firstArg] } first]
                    } savedTuple
                },
                ..,
                Expr.Match
                {
                    Pattern: Pattern.Variable current, Value: Expr.Call
                    {
                        Module: not null, DynamicRemote: false, Arguments: [Expr.Call
                        {
                            Module: not null, DynamicRemote: false, Arguments: [Expr secondArg]
                        } second]
                    } third
                },
                Expr.Match
                {
                    Pattern: Pattern.Variable list, Value: Expr.List
                    {
                        Items: [Expr.Tuple item], Tail: null
                    }
                },
                Expr.Tuple { Items: [Expr tag, Expr.Tuple { Items: [Expr.Variable left, Expr.Variable right] }] }
            ]
            } sequence
            || !DistinctBindings(saved, current, list) || left.Name != saved.Name || right.Name != list.Name)
        {
            return false;
        }

        var constants = new Dictionary<string, Term>(StringComparer.Ordinal);
        foreach (Expr expression in sequence.Items.Skip(1).SkipLast(3))
        {
            if (expression is not Expr.Match { Pattern: Pattern.Variable name, Value: var value }
                || !DistinctBindings(
                    name,
                    saved,
                    current,
                    list
                )
                || !AttributeLiteral.TryNormalizeConstant(value, out Term constant, constants)
                || !constants.TryAdd(name.Name, constant))
            {
                return false;
            }
        }

        if (!AttributeLiteral.TryNormalizeConstant(firstArg, out Term firstValue)
            || !AttributeLiteral.TryNormalizeConstant(secondArg, out Term secondValue, constants)
            || !AttributeLiteral.TryNormalizeConstant(tag, out Term resultTag, constants)
            || ReadTupleWithCallResult(savedTuple, savedTuple.Items[^1], null) is not { } savedItems
            || item.Items.FirstOrDefault(value => value is Expr.Variable variable && variable.Name == current.Name) is not { } result
            || ReadTupleWithCallResult(item, result, constants) is not { } itemFields)
        {
            return false;
        }

        Op(Allocate, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, 0);
        instructions.WriteByte(InitYRegisters);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, 1);
        Operand(YRegisterTag, 0);
        EmitConstant(firstValue, 0);
        EmitCallLine(first, location);
        EmitUnaryRemoteCall(first);
        Op(TestHeap, BeamWire.UnsignedTag, savedItems.Length + 1);
        Operand(BeamWire.UnsignedTag, 1);
        EmitTupleWithCallResult(savedItems, YRegisterTag);

        EmitConstant(secondValue, 0);
        EmitCallLine(second, CallLocation(first, location));
        EmitUnaryRemoteCall(second);
        EmitCallLine(third, CallLocation(second, location));
        EmitUnaryRemoteCall(third);

        const int pairHeapWords = 3;
        Op(TestHeap, BeamWire.UnsignedTag, itemFields.Length + 1 + ConsHeapWords + 2 * pairHeapWords);
        Operand(BeamWire.UnsignedTag, 1);
        EmitTupleWithCallResult(itemFields, XRegisterTag);
        Op(PutList, XRegisterTag, 0);
        LiteralOperand(Nil.Value);
        Operand(XRegisterTag, 0);
        EmitTupleHeader(XRegisterTag, 2);
        Operand(YRegisterTag, 0);
        Operand(XRegisterTag, 0);
        EmitTupleHeader(XRegisterTag, 2);
        LiteralOperand(resultTag);
        Operand(XRegisterTag, 0);
        EmitFrameReturn(1);

        return true;
    }

    private static Term?[]? ReadTupleWithCallResult(
        Expr.Tuple tuple,
        Expr result,
        IReadOnlyDictionary<string, Term>? constants
    )
    {
        var items = new Term?[tuple.Items.Count];
        for (int index = 0; index < items.Length; index++)
        {
            Expr field = tuple.Items[index];
            if (!ReferenceEquals(field, result) && !AttributeLiteral.TryNormalizeConstant(field, out items[index], constants))
            {
                return null;
            }
        }

        return items;
    }

    private void EmitUnaryRemoteCall(Expr.Call call)
    {
        Op(ExternalCall, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, Import(call.Module!, call.Function, 1));
    }

    private void EmitTupleWithCallResult(Term?[] items, int destination)
    {
        EmitTupleHeader(destination, items.Length);
        foreach (Term? item in items)
        {
            if (item is null)
            {
                Operand(XRegisterTag, 0);
            }
            else
            {
                LiteralOperand(item);
            }
        }
    }

    private void EmitTupleHeader(int destination, int arity, int register = 0)
    {
        Op(PutTuple, destination, register);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, arity);
    }
}
