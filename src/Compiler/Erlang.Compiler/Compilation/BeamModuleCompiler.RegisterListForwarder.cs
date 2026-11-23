using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitRegisterListForwarder(Clause clause)
        {
            if (clause is not
                {
                    Guard: null,
                    Patterns: [Erlang.Pattern.Variable first, Erlang.Pattern.Variable last],
                    Body: Expr.Sequence body
                }
                || !DistinctBindings(first, last)
                || body.Items[^1] is not Expr.Call { Module: null, DynamicRemote: false, Arguments: [var receiver, var key, var operand, var list] } call
                || !entries.TryGetValue((call.Function, 4), out int entry))
            {
                return false;
            }

            var constants = new Dictionary<string, Term>(StringComparer.Ordinal);
            Expr.List? constructed = null;
            string? result = null;
            foreach (Expr expression in body.Items.Take(body.Items.Count - 1))
            {
                if (expression is Expr.Literal)
                {
                    continue;
                }

                if (expression is not Expr.Match { Pattern: Erlang.Pattern.Variable variable, Value: var value }
                    || variable.Name == VariableScopeNames.Wildcard
                    || variable.Name == first.Name || variable.Name == last.Name
                    || variable.Name == result || constants.ContainsKey(variable.Name))
                {
                    return false;
                }

                if (AttributeLiteral.TryNormalizeConstant(value, out Term constant, constants))
                {
                    constants.Add(variable.Name, constant);
                }
                else if (constructed is null && value is Expr.List listValue)
                {
                    constructed = listValue;
                    result = variable.Name;
                }
                else
                {
                    return false;
                }
            }

            if (constructed is not { Items.Count: >= 2, Tail: null }
                || list is not Expr.Variable returned || returned.Name != result
                || constructed.Items[^1] is not Expr.Variable listTail || listTail.Name != last.Name
                || !AttributeLiteral.TryNormalizeConstant(receiver, out Term receiverValue, constants)
                || !AttributeLiteral.TryNormalizeConstant(key, out Term keyValue, constants))
            {
                return false;
            }

            bool forwardsFirst = operand is Expr.Variable source && source.Name == first.Name;
            if (!AttributeLiteral.TryNormalizeConstant(operand, out Term operandValue, constants) && !forwardsFirst)
            {
                return false;
            }

            Expr firstHead = constructed.Items[0];
            bool readsSelf = firstHead is Expr.Call { Function: AutoImportNames.Self, Arguments: [], DynamicRemote: false } current
                && owner.ResolvesErlangBif(current);
            if (!readsSelf && (firstHead is not Expr.Variable initial || initial.Name != first.Name))
            {
                return false;
            }

            var items = new Term[constructed.Items.Count - 2];
            for (int index = 0; index < items.Length; index++)
            {
                if (!AttributeLiteral.TryNormalizeConstant(constructed.Items[index + 1], out items[index], constants))
                {
                    return false;
                }
            }

            const int tail = 1;
            const int selfRegister = 2;
            const int listRegister = 3;
            owner.Op(TestHeap, BeamWire.UnsignedTag, constructed.Items.Count * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 2);
            if (readsSelf)
            {
                owner.Op(BifZero, BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Self, 0));
                owner.Operand(XRegisterTag, selfRegister);
            }

            owner.Op(PutList, XRegisterTag, tail);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, tail);
            for (int index = items.Length - 1; index >= 0; index--)
            {
                owner.instructions.WriteByte(PutList);
                owner.LiteralOperand(items[index]);
                owner.Operand(XRegisterTag, tail);
                owner.Operand(XRegisterTag, tail);
            }

            owner.Op(PutList, XRegisterTag, readsSelf ? selfRegister : 0);
            owner.Operand(XRegisterTag, tail);
            owner.Operand(XRegisterTag, listRegister);

            owner.EmitConstant(keyValue, 1);
            if (forwardsFirst)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    XRegisterTag,
                    2
                );
            }
            else
            {
                owner.EmitConstant(operandValue, 2);
            }

            owner.EmitConstant(receiverValue, 0);
            owner.EmitCallLine(call, location);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, call.Arguments.Count);
            owner.Operand(LabelTag, entry);

            return true;
        }
    }
}
