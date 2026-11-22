using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTaggedTokenResultTail(Expr body, (int Pair, int Payload)? selectorSlots = null)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || owner.receiveScratchRegisters is not null || deferredPairMembers.Count != 0
                || body is not Expr.Sequence
                {
                    Items:
                    [
                        Expr.Match
                    {
                        Pattern: Pattern.Variable result,
                        Value: Expr.Case
                        {
                            Value: Expr.Call read,
                            Clauses:
                                [
                                {
                                    Guard: null,
                                    Patterns:
                                        [
                                            Pattern.Tuple
                                        {
                                            Items:
                                                [
                                                    Pattern.Literal { Value: Atom tag },
                                                    Pattern.List
                                                {
                                                    Items: [Pattern.Variable first, Pattern.Variable second],
                                                    Tail: null
                                                }
                                                ]
                                        }
                                        ],
                                    Body: Expr.Tuple
                                    {
                                        Items: [Expr.Call { Arguments: [Expr.Variable token] } convert, Expr.Variable rest]
                                    }
                                },
                                {
                                    Guard: null,
                                    Patterns: [global::Erlang.Pattern.Any or Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                                    Body: var otherwise
                                }
                                ]
                        }
                    },
                        Expr.Call
                    {
                        Module: null,
                        DynamicRemote: false,
                        Arguments: [Expr.Variable left, Expr.Variable middle, Expr.Variable returned]
                    } recur
                    ]
                }
                || result.Name != returned.Name || first.Name != token.Name || second.Name != rest.Name
                || !DistinctBindings(result, first, second)
                || new[] { result, first, second }.Any(binding => variables.ContainsKey(binding.Name))
                || !variables.TryGetValue(left.Name, out int leftSlot)
                || !variables.TryGetValue(middle.Name, out int middleSlot)
                || !entries.TryGetValue((recur.Function, recur.Arguments.Count), out int entry)
                || !TryResolveOrdinaryCall(read, out string? producer)
                || read.Arguments.OfType<Expr.Variable>().Any(argument => !variables.ContainsKey(argument.Name))
                || !TryResolveOrdinaryCall(convert, out string? converter)
                || !AttributeLiteral.TryNormalizeConstant(otherwise, out Term fallback))
            {
                return false;
            }

            int[] dead = variables.Values.Where(slot => slot != leftSlot && slot != middleSlot).Distinct().Order().ToArray();
            if (dead.Length == 0)
            {
                return false;
            }

            // Only the two forwarded operands survive the producer. Its arguments
            // have already been copied out before the other slots are cleared.
            // Prefer an older incoming slot over the short-lived selector
            // and sibling field when all three are dead on this branch.
            int savedToken = selectorSlots is { } slots
                ? dead.LastOrDefault(slot => slot != slots.Pair && slot != slots.Payload, dead[^1])
                : dead[^1];
            BeamSourceLocation previous = LoadCallArguments(read.Arguments, null, orderSimpleOperands: true);
            owner.EmitSlotKills(dead);
            owner.EmitCallLine(read, previous);
            Call(producer, read.Function, read.Arguments.Count);

            int reject = Label();
            int tail = Label();
            owner.EmitRecordTest(
                reject,
                XRegisterTag,
                0,
                tag,
                2
            );
            owner.EmitTuplePart(0, 1, 0);
            owner.EmitNonemptyList(0, reject);
            owner.EmitListParts(
                0,
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            owner.EmitNonemptyList(0, reject);
            owner.EmitListParts(
                0,
                YRegisterTag,
                savedToken,
                XRegisterTag,
                0
            );
            owner.EmitNilTest(XRegisterTag, 0, reject);
            owner.MoveRegister(
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            owner.EmitCallLine(convert, CallLocation(read, previous));
            Call(converter, convert.Function, 1);
            EmitTupleWithX0([(null, null), (savedToken, null)], destination: 2);
            JumpTo(tail);

            Mark(reject);
            owner.EmitConstant(fallback, 2);
            Mark(tail);
            Load(middleSlot, 1);
            Load(leftSlot);
            EmitFramedLocalTail(recur.Arguments.Count, entry);

            return true;
        }
    }
}
