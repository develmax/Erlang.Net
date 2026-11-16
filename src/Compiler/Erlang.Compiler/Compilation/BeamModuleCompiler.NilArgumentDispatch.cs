using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitNilArgumentDispatch(FunctionDefinition function)
        {
            if (function.Clauses is not
                [
                    { Guard: null, Body: Expr.Call firstSource } empty,
                    { Guard: null, Body: Expr.Call secondSource } fallback
                ]
                || !HasUnconditionalParameters(fallback))
            {
                return false;
            }

            Expr.Call first = owner.ResolveImportedCall(firstSource, entries);
            Expr.Call second = owner.ResolveImportedCall(secondSource, entries);
            int nilRegister = -1;
            var variables = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < function.Arity; index++)
            {
                switch (empty.Patterns[index])
                {
                    case Pattern.List { Items: [], Tail: null } or Pattern.Literal { Value: Nil } when nilRegister < 0:
                        nilRegister = index;
                        break;
                    case Pattern.Variable parameter when parameter.Name == VariableScopeNames.Wildcard
                        || variables.Add(parameter.Name):
                        break;
                    default:
                        return false;
                }
            }

            if (nilRegister < 0 || ReadMoves(first, empty) is not { } emptyMoves
                || ReadMoves(second, fallback) is not { } fallbackMoves)
            {
                return false;
            }

            int alternative = Label();
            owner.EmitNilTest(XRegisterTag, nilRegister, alternative);
            EmitTail(first, emptyMoves);
            Mark(alternative);
            EmitTail(second, fallbackMoves);

            return true;

            List<(int Destination, int Source)>? ReadMoves(Expr.Call call, Clause clause)
            {
                if (call is not { Module: not (null or ErlangModuleNames.Module), DynamicRemote: false })
                {
                    return null;
                }

                var moves = new List<(int Destination, int Source)>();
                var patterns = clause.Patterns.ToArray();
                for (int destination = 0; destination < call.Arguments.Count; destination++)
                {
                    if (call.Arguments[destination] is not Expr.Variable variable)
                    {
                        return null;
                    }

                    int source = Array.FindIndex(patterns, pattern => pattern is Pattern.Variable parameter
                        && parameter.Name == variable.Name && parameter.Name != VariableScopeNames.Wildcard);
                    if (source < 0)
                    {
                        return null;
                    }

                    if (source != destination)
                    {
                        moves.Add((destination, source));
                    }
                }

                return moves;
            }

            void EmitTail(Expr.Call call, List<(int Destination, int Source)> moves)
            {
                owner.EmitArgumentMoves(moves, []);
                owner.EmitCallLine(call, location);
                owner.Op(TailExternalCall, BeamWire.UnsignedTag, call.Arguments.Count);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(call.Module!, call.Function, call.Arguments.Count));
            }
        }

        private bool TryEmitRecordListPrefix(FunctionDefinition function)
        {
            if (function.Clauses is not
                [
                    {
                        Guard: null, Patterns:
                        [
                            Pattern.Variable data, Pattern.List { Items: [Pattern.Tuple record], Tail: null },
                            Pattern.List { Items: [], Tail: null } or Pattern.Literal { Value: Nil },
                            Pattern.Variable ignoredFirst
                        ],
                        Body: Expr.List { Items: [Expr.Variable field, Expr.Variable firstData], Tail: null }
                    },
                    {
                        Guard: null, Patterns:
                        [
                            Pattern.Variable fallbackData, Pattern.Variable ignored,
                            Pattern.List { Items: [], Tail: null } or Pattern.Literal { Value: Nil },
                            Pattern.Variable ignoredSecond
                        ],
                        Body: Expr.List { Items: [var literal, Expr.Variable secondData], Tail: null }
                    }
                ] || data.Name != firstData.Name || fallbackData.Name != secondData.Name
                || data.Name == field.Name || data.Name == VariableScopeNames.Wildcard
                || ignoredFirst.Name == data.Name || ignoredFirst.Name == field.Name
                || fallbackData.Name == ignored.Name || fallbackData.Name == VariableScopeNames.Wildcard
                || ignoredSecond.Name == fallbackData.Name
                || ignoredSecond.Name == ignored.Name && ignored.Name != VariableScopeNames.Wildcard
                || field.Name == VariableScopeNames.Wildcard
                || record.Items is not [Pattern.Literal { Value: Atom tag }, ..]
                || !AttributeLiteral.TryNormalizeConstant(literal, out Term prefix))
            {
                return false;
            }

            int testField = -1;
            int resultField = -1;
            Term? expected = null;
            for (int index = 1; index < record.Items.Count; index++)
            {
                switch (record.Items[index])
                {
                    case Pattern.Variable { Name: VariableScopeNames.Wildcard }:
                        break;
                    case Pattern.Variable variable when variable.Name == field.Name && resultField < 0:
                        resultField = index;
                        break;
                    case Pattern.Literal value when testField < 0:
                        testField = index;
                        expected = value.Value;
                        break;
                    default:
                        return false;
                }
            }

            if (resultField < 0 || expected is null)
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            int fallback = Label();
            int head = function.Arity;
            int tail = head + 1;
            owner.EmitNonemptyList(1, fallback);
            owner.EmitListParts(1, XRegisterTag, head, XRegisterTag, tail);
            owner.EmitRecordTest(fallback, XRegisterTag, head, tag, record.Items.Count);
            owner.EmitTuplePart(head, testField, tail + 1);
            owner.Op(ExactEqualTest, LabelTag, fallback);
            owner.Operand(XRegisterTag, tail + 1);
            owner.LiteralOperand(expected);
            owner.EmitNilTest(XRegisterTag, tail, fallback);
            owner.EmitNilTest(XRegisterTag, 2, failure);
            EmitHeap(head + 1);
            owner.EmitTuplePart(head, resultField, 1);
            EmitDataTail();
            owner.Op(PutList, XRegisterTag, 1);
            EmitPrefixTail();

            Mark(fallback);
            owner.EmitNilTest(XRegisterTag, 2, failure);
            EmitHeap(1);
            EmitDataTail();
            owner.instructions.WriteByte(PutList);
            owner.LiteralOperand(prefix);
            EmitPrefixTail();

            return true;

            void EmitHeap(int live)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, 2 * ConsHeapWords);
                owner.Operand(BeamWire.UnsignedTag, live);
            }

            void EmitDataTail()
            {
                owner.Op(PutList, XRegisterTag, 0);
                owner.LiteralOperand(Nil.Value);
                owner.Operand(XRegisterTag, 0);
            }

            void EmitPrefixTail()
            {
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
                owner.instructions.WriteByte(Return);
            }
        }
    }
}
