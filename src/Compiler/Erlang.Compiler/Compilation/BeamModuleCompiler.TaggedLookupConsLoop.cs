using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTaggedLookupConsLoop(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || recursiveClosure is not null
                || function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [
                            Pattern.List
                    {
                        Items: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable given] }],
                        Tail: Pattern.Variable directRest
                    },
                            Pattern.Variable directNames,
                            Pattern.Variable directItems
                        ],
                    Body: Expr.Call givenTail
                },
                {
                    Guard: Expr.Call
                    {
                        Function: AutoImportNames.IsAtom, DynamicRemote: false,
                        Arguments: [Expr.Variable tested]
                    } guard,
                    Patterns: [
                            Pattern.List { Items: [Pattern.Variable key], Tail: Pattern.Variable rest },
                            Pattern.Variable names,
                            Pattern.Variable items
                        ],
                    Body: Expr.Case
                    {
                        Value: Expr.Call
                        {
                            Module: ListModuleNames.Module,
                            Function: ListModuleNames.KeyFind,
                            DynamicRemote: false,
                            Arguments: [Expr.Variable sought, Expr.Literal { Value: Integer position }, Expr.Variable table]
                        } lookup,
                        Clauses: [
                        {
                            Guard: null,
                            Patterns: [Pattern.Tuple { Items: [Pattern.Variable itemKey, Pattern.Variable found] }],
                            Body: Expr.Call foundTail
                        },
                        {
                            Guard: null,
                            Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                            Body: var failed
                        }
                            ]
                    }
                },
                {
                    Guard: null,
                    Patterns: [
                            Pattern.List { Items.Count: 0, Tail: null } or Pattern.Literal { Value: Nil },
                            Pattern.Variable ignored,
                            Pattern.Variable accumulated
                        ],
                    Body: Expr.Call { DynamicRemote: false, Arguments: [Expr.Variable input] } finish
                }
                ]
                || !DistinctBindings(
                    given,
                    directRest,
                    directNames,
                    directItems
                )
                || !DistinctBindings(
                    key,
                    rest,
                    names,
                    items,
                    found
                )
                || itemKey.Name != VariableScopeNames.Wildcard
                    && new[] { key.Name, rest.Name, names.Name, items.Name, found.Name }.Contains(itemKey.Name)
                || ignored.Name == accumulated.Name || !DistinctBindings(accumulated)
                || tested.Name != key.Name || sought.Name != key.Name || table.Name != names.Name
                || input.Name != accumulated.Name || !owner.ResolvesErlangBif(guard)
                || !IsRecursion(
                    givenTail,
                    directRest,
                    directNames,
                    directItems,
                    given
                )
                || !IsRecursion(
                    foundTail,
                    rest,
                    names,
                    items,
                    found
                )
                || ReadTaggedExit(failed, key.Name) is not { } error
                || !TryResolveOrdinaryCallTarget(finish, out string? finishModule) || finishModule is null
                || !entries.TryGetValue((function.Name, 3), out int entry))
            {
                return false;
            }

            int functionFailure = owner.nextLabel - 2;
            int atom = Label();
            int missing = Label();
            int empty = Label();
            owner.Op(IsNonemptyList, LabelTag, empty);
            owner.Operand(XRegisterTag, 0);
            owner.Op(GetList, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 3);
            owner.Operand(XRegisterTag, 4);
            owner.Op(IsTaggedTuple, LabelTag, atom);
            owner.Operand(XRegisterTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.LiteralOperand(tag);
            EmitPrepend(
                tuple: 3,
                live: 5,
                XRegisterTag,
                2
            );
            owner.MoveToX0(XRegisterTag, 4);
            EmitTailLocal(entry, 3, frame: null);

            Mark(atom);
            owner.Op(IsAtom, LabelTag, functionFailure);
            owner.Operand(XRegisterTag, 3);
            EmitFrameAllocation(5, 4);
            owner.MoveRegister(
                XRegisterTag,
                3,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                4,
                YRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                2,
                YRegisterTag,
                2
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                3
            );

            owner.MoveRegister(
                XRegisterTag,
                1,
                XRegisterTag,
                2
            );
            owner.EmitConstant(position, 1);
            owner.MoveToX0(XRegisterTag, 3);
            owner.EmitCallLine(lookup, location);
            Call(lookup.Module, lookup.Function, 3);
            owner.Op(IsTuple, LabelTag, missing);
            owner.EmitTypedRegister(0, AtomTypeBits | TupleTypeBits);
            owner.Op(TestArity, LabelTag, missing);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            EmitPrepend(
                tuple: 0,
                live: 1,
                YRegisterTag,
                2
            );
            owner.MoveRegister(
                YRegisterTag,
                3,
                XRegisterTag,
                1
            );
            owner.MoveToX0(YRegisterTag, 1);
            EmitTailLocal(entry, 3, frame: 4);

            Mark(missing);
            EmitTaggedExit(
                error.Call,
                error.Tags,
                CallLocation(lookup, location),
                framed: true,
                live: 0,
                source: 0,
                frameSlots: 4,
                sourceTag: YRegisterTag
            );

            Mark(empty);
            owner.EmitNilTest(XRegisterTag, 0, functionFailure);
            owner.MoveToX0(XRegisterTag, 2);
            owner.EmitCallLine(finish, location);
            owner.Op(TailExternalCall, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(finishModule, finish.Function, 1));

            return true;

            bool IsRecursion(
                Expr.Call call,
                Pattern.Variable tail,
                Pattern.Variable keys,
                Pattern.Variable current,
                Pattern.Variable value
            ) => call is
            {
                Module: null,
                DynamicRemote: false,
                Arguments: [
                    Expr.Variable next,
                    Expr.Variable nextNames,
                    Expr.List { Items: [Expr.Variable prepend], Tail: Expr.Variable prior }
                ]
            }
            && call.Function == function.Name && next.Name == tail.Name && nextNames.Name == keys.Name
            && prepend.Name == value.Name && prior.Name == current.Name;

            void EmitPrepend(
                int tuple,
                int live,
                int tailKind,
                int tail
            )
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
                owner.Operand(BeamWire.UnsignedTag, live);
                owner.EmitTuplePart(tuple, 1, 0);
                owner.Op(PutList, XRegisterTag, 0);
                owner.Operand(tailKind, tail);
                owner.Operand(XRegisterTag, 2);
            }
        }
    }
}
