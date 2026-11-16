using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private bool TryEmitComputedListField(Clause clause, BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries)
    {
        if (clause is not { Guard: null, Patterns: [Pattern.Variable input], Body: Expr.List { Tail: null } list }
            || !DistinctBindings(input))
        {
            return false;
        }

        Expr.Call? compute = null;
        var prefix = new List<Term>();
        var suffix = new List<Term>();
        Term? field = null;
        foreach (Expr item in list.Items)
        {
            if (compute is null && item is Expr.Tuple
                {
                    Items: [
                Expr.Call { Module: null, Arguments: [Expr.Variable argument] } call, var value]
                }
                && argument.Name == input.Name && AttributeLiteral.TryNormalizeConstant(value, out field))
            {
                compute = call;
            }
            else if (AttributeLiteral.TryNormalizeConstant(item, out Term literal))
            {
                (compute is null ? prefix : suffix).Add(literal);
            }
            else
            {
                return false;
            }
        }
        if (compute is null || !entries.TryGetValue((compute.Function, 1), out int entry))
        {
            return false;
        }

        Op(Allocate, BeamWire.UnsignedTag, 0);
        Operand(BeamWire.UnsignedTag, 1);
        EmitCallLine(compute, location);
        EmitFieldCall(1, entry);
        Op(TestHeap, BeamWire.UnsignedTag, 3 + (prefix.Count + 1) * ConsHeapWords);
        Operand(BeamWire.UnsignedTag, 1);
        EmitTupleHeader(XRegisterTag, 2);
        Operand(XRegisterTag, 0);
        LiteralOperand(field!);
        Op(PutList, XRegisterTag, 0);
        LiteralOperand(Cons.From(suffix));
        Operand(XRegisterTag, 0);
        for (int index = prefix.Count - 1; index >= 0; index--)
        {
            instructions.WriteByte(PutList);
            LiteralOperand(prefix[index]);
            Operand(XRegisterTag, 0);
            Operand(XRegisterTag, 0);
        }
        EmitFrameReturn();

        return true;
    }

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitAtomListTitle(FunctionDefinition function)
        {
            if (function.Clauses is not [
                { Guard: null, Patterns: [Pattern.Literal { Value: Atom key }] } fixedTitle,
                {
                    Guard: null, Patterns: [Pattern.Variable input], Body: Expr.Binary
                    {
                        Operator: ErlangOperators.Append, Left: Expr.Literal prefix, Right: Expr.Binary
                        {
                            Operator: ErlangOperators.Append, Left: Expr.Call
                            {
                                Module: null or ErlangModuleNames.Module, Function: AutoImportNames.AtomToList, DynamicRemote: false,
                                Arguments: [Expr.Variable argument]
                            } convert, Right: Expr.Literal suffix
                        }
                    }
                }
            ] || input.Name != argument.Name || !DistinctBindings(input)
                || !AttributeLiteral.TryNormalizeConstant(fixedTitle.Body, out Term fixedValue)
                || !owner.ResolvesErlangBif(convert))
            {
                return false;
            }

            int fallback = Label();
            owner.EmitAtomTest(0, key.Name, fallback);
            owner.EmitConstant(fixedValue, 0);
            owner.instructions.WriteByte(Return);
            Mark(fallback);
            owner.Op(Allocate, BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitCallLine(convert, location);
            owner.EmitStaticRemoteCall(convert with { Module = ErlangModuleNames.Module });
            owner.EmitConstant(suffix.Value, 1);
            EmitAppend();
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.EmitConstant(prefix.Value, 0);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.Append, 2));
            owner.Operand(BeamWire.UnsignedTag, 0);

            return true;
        }
    }
}
