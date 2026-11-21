using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitRegisterListRemoteTail(Clause clause)
        {
            if (clause is not
                {
                    Guard: null,
                    Patterns: [Erlang.Pattern.Variable first, Erlang.Pattern.Variable second],
                    Body: Expr.Sequence body
                }
                || !DistinctBindings(first, second)
                || body.Items.Where(item => item is not Expr.Literal).ToArray() is not
                [
                    Expr.Match { Pattern: Erlang.Pattern.Variable alias, Value: var value },
                    Expr.Call
                {
                    Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                    Arguments: [Expr.Variable firstOperand, var operation, Expr.List { Items: [Expr.Variable head, Expr.Variable last], Tail: null }]
                } call
                ]
                || !DistinctBindings(first, second, alias) || firstOperand.Name != alias.Name
                || head.Name != first.Name || last.Name != second.Name
                || !AttributeLiteral.TryNormalizeConstant(value, out Term receiver)
                || !AttributeLiteral.TryNormalizeConstant(operation, out Term secondOperand))
            {
                return false;
            }

            owner.Op(TestHeap, BeamWire.UnsignedTag, 2 * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 2);
            PutStartList(1, 1, Nil.Value);
            PutStartList(0, 2, tailRegister: 1);
            owner.EmitConstant(secondOperand, 1);
            owner.EmitConstant(receiver, 0);
            owner.EmitCallLine(call, location);
            owner.Op(TailExternalCall, BeamWire.UnsignedTag, call.Arguments.Count);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(call.Module!, call.Function, call.Arguments.Count));

            return true;
        }

        private bool TryEmitChildSpecConstructor(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Patterns.Count is not (3 or 4))
            {
                return false;
            }

            Expr expression = clause.Body is Expr.Sequence sequence
                && sequence.Items.Take(sequence.Items.Count - 1).All(item => item is Expr.Literal)
                ? sequence.Items[^1] : clause.Body;
            if (expression is not Expr.Tuple
                {
                    Items: [var childName, Expr.Tuple { Items: [var module, Expr.Literal { Value: Atom operation }, Expr.Variable arguments] },
                            Expr.Variable restart, ..]
                } spec || spec.Items.Count < 4)
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Erlang.Pattern.Variable>().ToArray();
            if (arguments.Name != parameters[1].Name || restart.Name != parameters[2].Name
                || !Equals(childName, module))
            {
                return false;
            }

            Term? nameLiteral = null;
            Term? suffixList;
            bool literalName = parameters.Length == 4;
            if (literalName)
            {
                if (!AttributeLiteral.TryNormalizeConstant(childName, out nameLiteral)
                    || spec.Items[^1] is not Expr.Binary
                    {
                        Operator: ErlangOperators.Append,
                        Left: Expr.List { Items: [var name], Tail: null },
                        Right: Expr.Variable suffix
                    }
                    || !Equals(name, childName) || suffix.Name != parameters[3].Name)
                {
                    return false;
                }

                suffixList = null;
            }
            else
            {
                if (childName is not Expr.Variable name || name.Name != parameters[0].Name
                    || spec.Items[^1] is not Expr.List { Items.Count: >= 2 } list
                    || !Equals(list.Items[0], name)
                    || !AttributeLiteral.TryNormalizeConstant(list with { Items = list.Items.Skip(1).ToArray() }, out suffixList))
                {
                    return false;
                }
            }

            var remaining = new List<Term>();
            foreach (Expr item in spec.Items.Skip(3).SkipLast(1))
            {
                if (!AttributeLiteral.TryNormalizeConstant(item, out Term member))
                {
                    return false;
                }

                remaining.Add(member);
            }

            owner.Op(TestHeap, BeamWire.UnsignedTag, 4 + ConsHeapWords + spec.Items.Count + 1);
            owner.Operand(BeamWire.UnsignedTag, parameters.Length);
            int listRegister = literalName ? 0 : 3;
            if (literalName)
            {
                EmitDependencies();
            }

            TupleHeader(1, 3);
            EmitName();
            owner.LiteralOperand(operation);
            owner.Operand(XRegisterTag, 1);
            if (!literalName)
            {
                EmitDependencies();
            }

            TupleHeader(0, spec.Items.Count);
            EmitName();
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 2);
            foreach (Term member in remaining)
            {
                owner.LiteralOperand(member);
            }

            owner.Operand(XRegisterTag, listRegister);
            owner.instructions.WriteByte(Return);

            return true;

            void EmitName()
            {
                if (nameLiteral is null)
                {
                    owner.Operand(XRegisterTag, 0);
                }
                else
                {
                    owner.LiteralOperand(nameLiteral);
                }
            }

            void EmitDependencies()
            {
                owner.instructions.WriteByte(PutList);
                EmitName();
                if (suffixList is null)
                {
                    owner.Operand(XRegisterTag, 3);
                }
                else
                {
                    owner.LiteralOperand(suffixList);
                }

                owner.Operand(XRegisterTag, listRegister);
            }
        }

        private void PutStartList(
            int source,
            int result,
            Term? tail = null,
            int tailRegister = 0,
            int sourceTag = XRegisterTag
        )
        {
            owner.Op(PutList, sourceTag, source);
            if (tail is null)
            {
                owner.Operand(XRegisterTag, tailRegister);
            }
            else
            {
                owner.LiteralOperand(tail);
            }

            owner.Operand(XRegisterTag, result);
        }

        private void TupleHeader(int register, int count)
        {
            owner.Op(PutTuple, XRegisterTag, register);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, count);
        }
    }
}
