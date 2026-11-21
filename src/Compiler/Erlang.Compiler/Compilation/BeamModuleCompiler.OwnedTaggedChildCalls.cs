using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitOwnedForwardingList(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Patterns.Count is < 3 or >= FirstReservedXRegister
                || clause.Body is not Expr.Sequence body
                || body.Items.Where(item => item is not Expr.Literal).ToArray() is not
                [
                    Expr.Match { Pattern: Erlang.Pattern.Variable alias, Value: var aliasValue },
                    Expr.Call { Module: null, DynamicRemote: false, Arguments.Count: 3 or 4 } call
                ]
                || call.Arguments[0] is not Expr.Variable receiver || receiver.Name != alias.Name
                || call.Arguments[^1] is not Expr.List { Items.Count: >= 2, Tail: null } list
                || !AttributeLiteral.TryNormalizeConstant(aliasValue, out Term literal)
                || !entries.TryGetValue((call.Function, call.Arguments.Count), out int entry))
            {
                return false;
            }

            string[] parameters = clause.Patterns.Cast<Erlang.Pattern.Variable>().Select(variable => variable.Name).ToArray();
            if (alias.Name == VariableScopeNames.Wildcard || parameters.Contains(alias.Name, StringComparer.Ordinal))
            {
                return false;
            }

            int finalRegister = call.Arguments.Count - 1;
            var survivingRegisters = new HashSet<int>();
            var moves = new List<(int Source, int Destination)>();
            for (int destination = 1; destination < finalRegister; destination++)
            {
                if (call.Arguments[destination] is not Expr.Variable forwarded)
                {
                    return false;
                }

                int source = Array.IndexOf(parameters, forwarded.Name);
                if (source < 0 || source == finalRegister || !survivingRegisters.Add(source)
                    || moves.Any(move => move.Destination == source))
                {
                    return false;
                }

                if (source != destination)
                {
                    moves.Add((source, destination));
                }
            }

            var unique = new HashSet<int>();
            var operands = new (int Register, Term? Literal)[list.Items.Count];
            for (int index = 0; index < operands.Length; index++)
            {
                if (list.Items[index] is Expr.Variable variable
                    && Array.IndexOf(parameters, variable.Name) is int register && register >= 0
                    && !survivingRegisters.Contains(register) && unique.Add(register))
                {
                    operands[index] = (register, null);
                }
                else if (index != operands.Length - 1 && AttributeLiteral.TryNormalizeConstant(list.Items[index], out Term term))
                {
                    operands[index] = (-1, term);
                }
                else
                {
                    return false;
                }
            }

            owner.Op(TestHeap, BeamWire.UnsignedTag, list.Items.Count * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, parameters.Length);
            int tailRegister = operands[^1].Register;
            owner.Op(PutList, XRegisterTag, tailRegister);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, tailRegister);
            for (int index = operands.Length - 2; index >= 0; index--)
            {
                var operand = operands[index];
                owner.instructions.WriteByte(PutList);
                if (operand.Literal is { } value)
                {
                    owner.LiteralOperand(value);
                }
                else
                {
                    owner.Operand(XRegisterTag, operand.Register);
                }

                owner.Operand(XRegisterTag, tailRegister);
                tailRegister = index == 0 ? finalRegister : operand.Literal is null ? operand.Register : tailRegister;
                owner.Operand(XRegisterTag, tailRegister);
            }

            foreach (var (source, destination) in moves)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    source,
                    XRegisterTag,
                    destination
                );
            }

            owner.EmitConstant(literal, 0);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, call.Arguments.Count);
            owner.Operand(LabelTag, entry);

            return true;
        }

        private bool TryEmitTaggedKeyCleanup(Clause clause)
        {
            if (clause is not
                {
                    Guard: null, Patterns: [Erlang.Pattern.Variable key], Body: Expr.Sequence body
                }
                || body.Items.Where(item => item is not Expr.Literal).ToArray() is not
                [
                    Expr.Match { Pattern: Erlang.Pattern.Variable receiver, Value: var value },
                    Expr.Case
                {
                    Value: Expr.Call
                    {
                        Function: AutoImportNames.Whereis, DynamicRemote: false, Arguments: [Expr.Variable lookupReceiver]
                    } lookup,
                    Clauses:
                        [
                        { Guard: null, Patterns: [Erlang.Pattern.Literal { Value: Atom absent }], Body: Expr.Literal missing },
                        {
                            Guard: null,
                            Patterns: [Erlang.Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                            Body: Expr.Sequence
                            {
                                Items:
                                    [
                                        Expr.Call
                                    {
                                        Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                                        Arguments: [Expr.Variable firstReceiver, Expr.Tuple { Items: [var firstTag, Expr.Variable firstKey] }]
                                    } first,
                                        Expr.Call
                                    {
                                        Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                                        Arguments: [Expr.Variable secondReceiver, Expr.Tuple { Items: [var secondTag, Expr.Variable secondKey] }]
                                    } second
                                    ]
                            }
                        }
                        ]
                }
                ]
                || !DistinctBindings(key, receiver) || lookupReceiver.Name != receiver.Name
                || firstReceiver.Name != receiver.Name || secondReceiver.Name != receiver.Name
                || firstKey.Name != key.Name || secondKey.Name != key.Name
                || !owner.ResolvesErlangBif(lookup)
                || !AttributeLiteral.TryNormalizeConstant(value, out Term address)
                || !AttributeLiteral.TryNormalizeConstant(firstTag, out Term tagA)
                || !AttributeLiteral.TryNormalizeConstant(secondTag, out Term tagB))
            {
                return false;
            }

            const int frame = 1;
            int active = Label();
            EmitFrameAllocation(1, frame);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            owner.EmitConstant(address, 0);
            owner.EmitCallLine(lookup, location);
            Call(ErlangModuleNames.Module, lookup.Function, 1);
            owner.Op(ExactEqualTest, LabelTag, active);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(absent);
            owner.EmitConstant(missing.Value, 0);
            owner.EmitFrameReturn(frame);

            Mark(active);
            EmitKey(tagA);
            owner.EmitCallLine(first, CallLocation(lookup, location));
            owner.EmitStaticRemoteCall(first);
            EmitKey(tagB);
            owner.EmitCallLine(second, CallLocation(first, location));
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(second.Module!, second.Function, 2));
            owner.Operand(BeamWire.UnsignedTag, frame);

            return true;

            void EmitKey(Term tag)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
                owner.Operand(BeamWire.UnsignedTag, 0);
                TaggedChildTupleHeader(1, 2);
                owner.LiteralOperand(tag);
                owner.Operand(YRegisterTag, 0);
                owner.EmitConstant(address, 0);
            }
        }

        private void TaggedChildTupleHeader(int register, int count)
        {
            owner.Op(PutTuple, XRegisterTag, register);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, count);
        }
    }
}
