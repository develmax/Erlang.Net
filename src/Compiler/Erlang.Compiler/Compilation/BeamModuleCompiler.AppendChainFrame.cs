using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitAppendChainFrame(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null } clause] || !HasUnconditionalParameters(clause))
            {
                return false;
            }

            Expr body = clause.Body;
            Expr.Call? consumer = null;
            Term? tupleMember = null;
            if (body is Expr.Call { DynamicRemote: false, Arguments: [var argument] } call
                && call.Module is not (null or ErlangModuleNames.Module))
            {
                consumer = call;
                body = argument;
            }
            else if (body is Expr.Tuple { Items: [var value, var constant] }
                && AttributeLiteral.TryNormalizeConstant(constant, out Term member))
            {
                tupleMember = member;
                body = value;
            }

            if (body is not Expr.Binary
                {
                    Operator: ErlangOperators.Append, Left: Expr.Variable retained,
                    Right: Expr.Binary
                    {
                        Operator: ErlangOperators.Append, Left: var prefix,
                        Right: Expr.Binary { Operator: ErlangOperators.Append } inner
                    }
                } outer
                || AppendPrefix(prefix) is not { Count: > 0 } items
                || !items.All(item => AttributeLiteral.TryNormalizeConstant(item, out _))
                || AppendPrefix(inner.Left) is not null
                || !IsPreparedOperand(inner.Left) || !IsPreparedOperand(inner.Right))
            {
                return false;
            }

            string[] parameters = clause.Patterns.Cast<Pattern.Variable>().Select(pattern => pattern.Name).ToArray();
            int saved = Array.IndexOf(parameters, retained.Name);
            var append = new Expr.Call(ErlangModuleNames.Module, ErlangOperators.Append, [inner.Left, inner.Right]);
            if (saved < 0 || retained.Name == VariableScopeNames.Wildcard
                || ReadEntryCall(append, parameters) is not { } first)
            {
                return false;
            }

            const int frame = 1;
            EmitFrameAllocation(Math.Max(saved + 1, EntryCallLiveCount(first, parameters)), frame);
            owner.MoveRegister(XRegisterTag, saved, YRegisterTag, 0);
            EmitEntryCallArguments(first);
            var previous = location;
            owner.EmitArithmeticLine(inner.SourceLine, ref previous);
            Call(ErlangModuleNames.Module, ErlangOperators.Append, 2);

            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords * items.Count);
            owner.Operand(BeamWire.UnsignedTag, 1);
            for (int index = items.Count - 1; index >= 0; index--)
            {
                AttributeLiteral.TryNormalizeConstant(items[index], out Term head);
                owner.instructions.WriteByte(PutList);
                owner.LiteralOperand(head);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, index == 0 ? 1 : 0);
            }

            Load(0);
            if (consumer is null && tupleMember is null)
            {
                owner.EmitArithmeticLine(outer.SourceLine, ref previous);
                owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 2);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.Append, 2));
                owner.Operand(BeamWire.UnsignedTag, frame);
                return true;
            }

            owner.Op(TrimFrame, BeamWire.UnsignedTag, frame);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitArithmeticLine(outer.SourceLine, ref previous);
            Call(ErlangModuleNames.Module, ErlangOperators.Append, 2);
            if (consumer is not null)
            {
                owner.EmitCallLine(consumer, previous);
                owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(consumer.Module!, consumer.Function, 1));
                owner.Operand(BeamWire.UnsignedTag, 0);
            }
            else
            {
                EmitTupleWithX0([(null, null), (null, tupleMember)]);
                owner.EmitFrameReturn(0);
            }

            return true;
        }
    }
}
