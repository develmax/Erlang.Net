using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSingletonLookupConstruction(Clause clause)
        {
            if (clause is not
                {
                    Guard: null,
                    Patterns: [Erlang.Pattern.Variable first, Erlang.Pattern.Variable second, Erlang.Pattern.Variable third],
                    Body: Expr.Sequence body
                }
                || body.Items.Where(expression => expression is not Expr.Literal).ToArray() is not
                [
                    Expr.Match
                {
                    Pattern: Erlang.Pattern.Variable input,
                    Value: Expr.Call { Module: not (null or ErlangModuleNames.Module), DynamicRemote: false, Arguments: [var queryKey] } query
                },
                    Expr.Match
                {
                    Pattern: Erlang.Pattern.Variable id,
                    Value: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable queryResult, var seed] } fold
                },
                    Expr.Match
                {
                    Pattern: Erlang.Pattern.List { Items: [Erlang.Pattern.Tuple pattern], Tail: null },
                    Value: Expr.Call { Module: not (null or ErlangModuleNames.Module), DynamicRemote: false, Arguments: [var lookupKey, var lookupField] } lookup
                } match,
                    Expr.Match
                {
                    Pattern: Erlang.Pattern.Variable built,
                    Value: Expr.Call { Module: null or ErlangModuleNames.Module, Function: AutoImportNames.MakeRef, DynamicRemote: false, Arguments: [] } build
                },
                    Expr.Match { Pattern: Erlang.Pattern.Variable options, Value: Expr.List optionList },
                    Expr.Match { Pattern: Erlang.Pattern.Variable spec, Value: Expr.Tuple specTuple },
                    Expr.Case
                {
                    Value: Expr.Call { Module: not (null or ErlangModuleNames.Module), DynamicRemote: false, Arguments: [var invokeKey, Expr.Variable invokeSpec] } invoke,
                    Clauses: [var success, var failure]
                }
                ]
                || !DistinctBindings(
                    first,
                    second,
                    third,
                    input,
                    id,
                    built,
                    options,
                    spec
                )
                || input.Name != queryResult.Name || spec.Name != invokeSpec.Name
                || !owner.ResolvesErlangBif(build)
                || !entries.TryGetValue((fold.Function, 2), out int foldEntry)
                || ReadLiteral(queryKey) is not { } queryArgument || ReadLiteral(seed) is not { } seedValue
                || ReadLiteral(lookupKey) is not { } lookupArgument || ReadLiteral(lookupField) is not { } fieldArgument
                || ReadLiteral(invokeKey) is not { } invokeArgument
                || pattern.Items.Count < 2 || !pattern.Items.All(item => item is Erlang.Pattern.Variable))
            {
                return false;
            }

            var fields = pattern.Items.Cast<Erlang.Pattern.Variable>().Select((field, index) => (field, index))
                .Where(member => member.field.Name != VariableScopeNames.Wildcard).ToArray();
            if (fields is not [{ field: var projected, index: int projectedField }]
                || specTuple.Items is not [Expr.Tuple { Items: [var idKey, Expr.Variable idValue] }, var address, ..]
                || idValue.Name != id.Name || ReadLiteral(idKey) is not { } idTag
                || address is not Expr.Tuple
                {
                    Items: [var module, var operation, Expr.List { Items: [Expr.Variable a, Expr.Variable b, Expr.Variable c, Expr.Variable d], Tail: null }]
                }
                || ReadLiteral(module) is not Atom addressModule || ReadLiteral(operation) is not Atom addressFunction
                || a.Name != projected.Name || b.Name != first.Name || c.Name != built.Name || d.Name != options.Name
                || !DistinctBindings(
                    first,
                    second,
                    third,
                    input,
                    id,
                    built,
                    options,
                    spec,
                    projected
                )
                || optionList.Items is not
                [
                    Expr.Tuple { Items: [var primaryOption, Expr.Variable primaryValue] },
                    Expr.Tuple { Items: [var secondaryOption, Expr.Variable secondaryValue] },
                    ..
                ]
                || primaryValue.Name != projected.Name || secondaryValue.Name != third.Name
                || ReadLiteral(primaryOption) is not { } primaryKey || ReadLiteral(secondaryOption) is not { } secondaryKey)
            {
                return false;
            }

            Term? optionTail = optionList.Tail is null ? Nil.Value : ReadLiteral(optionList.Tail);
            var specTail = specTuple.Items.Skip(2).Select(ReadLiteral).ToArray();
            if (optionTail is null || specTail.Any(member => member is null))
            {
                return false;
            }

            for (int index = optionList.Items.Count - 1; index >= 2; index--)
            {
                if (ReadLiteral(optionList.Items[index]) is not { } literal)
                {
                    return false;
                }

                optionTail = new Cons(literal, optionTail);
            }

            if (success is not
                {
                    Guard: null,
                    Patterns: [Erlang.Pattern.Tuple { Items: [Erlang.Pattern.Literal { Value: Atom tag }, Erlang.Pattern.Variable payload] }],
                    Body: Expr.Sequence successBody
                }
                || successBody.Items.Where(expression => expression is not Expr.Literal).ToArray() is not
                [
                    Expr.Call
                {
                    Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                    Arguments: [Expr.Variable notifyFirst, Expr.Variable notifySecond, Expr.Variable notifyThird]
                } notify,
                    Expr.Tuple { Items: [Expr.Literal { Value: Atom resultTag }, Expr.Variable result] }
                ]
                || !tag.Equals(resultTag) || notifyFirst.Name != first.Name || notifySecond.Name != second.Name
                || payload.Name != notifyThird.Name || payload.Name != result.Name
                || failure is not
                {
                    Guard: null, Patterns: [Erlang.Pattern.Variable rejected], Body: var rejectedBody
                }
                || (rejectedBody is Expr.Sequence rejectSequence
                    && rejectSequence.Items.Take(rejectSequence.Items.Count - 1).All(expression => expression is Expr.Literal)
                        ? rejectSequence.Items[^1] : rejectedBody) is not Expr.Variable returned
                || rejected.Name != returned.Name
                || !DistinctBindings(
                    first,
                    second,
                    third,
                    input,
                    id,
                    built,
                    options,
                    spec,
                    projected,
                    payload,
                    rejected
                ))
            {
                return false;
            }

            const int projectionSlot = 0;
            const int idSlot = 1;
            const int auxiliarySlot = 2;
            const int secondSlot = 3;
            const int firstSlot = 4;
            const int frame = 5;
            int noMatch = Label();
            int invalidLookup = Label();
            EmitFrameAllocation(arity: 3, size: frame);
            EmitFrameInitialization(projectionSlot, auxiliarySlot);
            Save(2, auxiliarySlot);
            Save(1, secondSlot);
            Save(0, firstSlot);

            var previous = location;
            owner.EmitConstant(queryArgument, 0);
            EmitCall(query);
            owner.EmitConstant(seedValue, 1);
            owner.EmitCallLine(fold, previous);
            previous = CallLocation(fold, previous);
            owner.EmitFieldCall(2, foldEntry);
            Save(0, idSlot);
            owner.EmitConstant(fieldArgument, 1);
            owner.EmitConstant(lookupArgument, 0);
            EmitCall(lookup);

            owner.EmitNonemptyList(0, invalidLookup);
            owner.EmitListParts(
                0,
                YRegisterTag,
                projectionSlot,
                XRegisterTag,
                1
            );
            EmitTupleShapeTest(
                YRegisterTag,
                projectionSlot,
                pattern.Items.Count,
                invalidLookup
            );
            owner.EmitNilTest(XRegisterTag, 1, invalidLookup);
            Call(ErlangModuleNames.Module, build.Function, 0);

            int words = 3 + 2 + 3 + 2 + 3 + 4 * 2 + 4 + specTuple.Items.Count + 1;
            owner.Op(TestHeap, BeamWire.UnsignedTag, words);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTuplePart(
                projectionSlot,
                projectedField,
                1,
                YRegisterTag
            );
            EmitPair(
                2,
                secondaryKey,
                YRegisterTag,
                auxiliarySlot
            );
            EmitCons(
                XRegisterTag,
                2,
                2,
                tailLiteral: optionTail
            );
            EmitPair(
                3,
                primaryKey,
                XRegisterTag,
                1
            );
            EmitCons(
                XRegisterTag,
                3,
                2,
                tailRegister: 2
            );
            EmitPair(
                3,
                idTag,
                YRegisterTag,
                idSlot
            );
            EmitCons(
                XRegisterTag,
                2,
                2,
                tailLiteral: Nil.Value
            );
            EmitCons(
                XRegisterTag,
                0,
                0,
                tailRegister: 2
            );
            EmitCons(YRegisterTag, firstSlot, 0);
            EmitCons(XRegisterTag, 1, 0);
            TupleHeader(0, 3);
            owner.LiteralOperand(addressModule);
            owner.LiteralOperand(addressFunction);
            owner.Operand(XRegisterTag, 0);
            TupleHeader(1, specTuple.Items.Count);
            owner.Operand(XRegisterTag, 3);
            owner.Operand(XRegisterTag, 0);
            foreach (Term? operand in specTail)
            {
                owner.LiteralOperand(operand!);
            }

            EmitFrameInitialization(projectionSlot, secondSlot);
            owner.EmitConstant(invokeArgument, 0);
            EmitCall(invoke);
            Save(0, auxiliarySlot);
            EmitDetailRecordTest(
                0,
                2,
                tag,
                noMatch
            );
            owner.EmitTuplePart(0, 1, 2);
            owner.MoveRegister(
                YRegisterTag,
                secondSlot,
                XRegisterTag,
                1
            );
            owner.MoveRegister(
                YRegisterTag,
                firstSlot,
                XRegisterTag,
                0
            );
            owner.MoveRegister(
                YRegisterTag,
                auxiliarySlot,
                YRegisterTag,
                firstSlot
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, firstSlot);
            owner.Operand(BeamWire.UnsignedTag, 1);
            EmitCall(notify);
            owner.MoveToX0(YRegisterTag, 0);
            owner.EmitFrameReturn(1);

            Mark(noMatch);
            owner.EmitFrameReturn(frame);
            Mark(invalidLookup);
            owner.EmitLine(new(match.PreprocessorFile, match.SourceLine));
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;

            static Term? ReadLiteral(Expr expression) => AttributeLiteral.TryNormalizeConstant(expression, out Term value) ? value : null;

            void Save(int register, int slot) => owner.MoveRegister(
                XRegisterTag,
                register,
                YRegisterTag,
                slot
            );

            void EmitCall(Expr.Call call)
            {
                owner.EmitCallLine(call, previous);
                previous = CallLocation(call, previous);
                owner.EmitStaticRemoteCall(call);
            }

            void TupleHeader(int register, int count)
            {
                owner.Op(PutTuple, XRegisterTag, register);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, count);
            }

            void EmitPair(
                int register,
                Term key,
                int valueTag,
                int value
            )
            {
                TupleHeader(register, 2);
                owner.LiteralOperand(key);
                owner.Operand(valueTag, value);
            }

            void EmitCons(
                int headTag,
                int head,
                int destination,
                int tailRegister = 0,
                Term? tailLiteral = null
            )
            {
                owner.Op(PutList, headTag, head);
                if (tailLiteral is not null)
                {
                    owner.LiteralOperand(tailLiteral);
                }
                else
                {
                    owner.Operand(XRegisterTag, tailRegister);
                }

                owner.Operand(XRegisterTag, destination);
            }
        }
    }
}
