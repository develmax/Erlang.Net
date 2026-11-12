using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitRecursiveTermExpansion(FunctionDefinition function)
        {
            if (function.Clauses is not [var marker, var binary, var list, var tuple,
                { Guard: null, Patterns: [Pattern.Variable original, Pattern.Variable ignored], Body: Expr.Variable unchanged }]
                || !DistinctBindings(original) || original.Name != unchanged.Name || original.Name == ignored.Name
                || ReadExpansionMarker(marker) is not { } tagged || ReadExpansionBinary(binary) is not { } raw
                || ReadExpansionList(list, function.Name) is not { } sequence
                || ReadExpansionTuple(tuple, function.Name) is not { } product
                || !entries.TryGetValue((function.Name, 2), out int recur))
            {
                return false;
            }

            int binaryLabel = Label();
            int rawLabel = Label();
            int listLabel = Label();
            int improper = Label();
            int tupleLabel = Label();
            int fallback = Label();
            int badMatch = Label();
            owner.EmitNonemptyList(0, binaryLabel);
            owner.EmitListParts(
                0,
                XRegisterTag,
                2,
                XRegisterTag,
                3
            );
            owner.EmitAtomTest(2, tagged.Marker.Name, binaryLabel);
            foreach (int register in new[] { 2, 4, 5 })
            {
                owner.EmitNonemptyList(3, binaryLabel);
                owner.EmitListParts(
                    3,
                    XRegisterTag,
                    register,
                    XRegisterTag,
                    3
                );
            }
            owner.EmitNilTest(XRegisterTag, 3, binaryLabel);
            owner.EmitAtomTest(1, "true", binaryLabel);
            owner.Op(AllocateHeap, BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, 6);
            owner.EmitTagMatchTuple(
                0,
                (XRegisterTag, 2),
                (XRegisterTag, 4),
                (XRegisterTag, 5)
            );
            owner.EmitCallLine(tagged.Read, location);
            owner.EmitStaticRemoteCall(tagged.Read);
            owner.Op(IsTaggedTuple, LabelTag, badMatch);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.LiteralOperand(tagged.Success);
            owner.EmitTuplePart(0, 1, 0);
            owner.EmitFrameReturn();

            Mark(binaryLabel);
            owner.Op(IsBinary, LabelTag, listLabel);
            owner.Operand(XRegisterTag, 0);
            owner.Op(IsBoolean, LabelTag, rawLabel);
            owner.Operand(XRegisterTag, 1);
            JumpTo(listLabel);
            Mark(rawLabel);
            owner.EmitCallLine(raw, location);
            owner.Op(TailExternalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(raw.Module!, raw.Function, 2));

            Mark(listLabel);
            owner.EmitNonemptyList(0, tupleLabel);
            owner.Op(Allocate, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.InitStackSlot(0);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                2
            );
            owner.EmitListParts(
                0,
                YRegisterTag,
                1,
                XRegisterTag,
                0
            );
            owner.EmitCallLine(sequence.Tail, location);
            owner.EmitFieldCall(2, recur);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            owner.Op(IsList, LabelTag, improper);
            owner.Operand(XRegisterTag, 0);
            var tailLocation = CallLocation(sequence.Tail, location);
            EmitExpandedHead(
                sequence.Proper,
                tailLocation,
                recur,
                proper: true
            );
            Mark(improper);
            EmitExpandedHead(
                sequence.Improper,
                tailLocation,
                recur,
                proper: false
            );

            Mark(tupleLabel);
            owner.Op(IsTuple, LabelTag, fallback);
            owner.Operand(XRegisterTag, 0);
            owner.Op(Allocate, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            owner.EmitCallLine(product.Convert, location);
            owner.EmitStaticRemoteCall(product.Convert with { Module = ErlangModuleNames.Module });
            Load(0, 1);
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 0);
            var convertLocation = CallLocation(product.Convert, location);
            owner.EmitCallLine(product.Recur, convertLocation);
            owner.EmitFieldCall(2, recur);
            owner.EmitTailRemote(ErlangModuleNames.Module, product.Finish.Function, framed: true);
            Mark(fallback);
            owner.instructions.WriteByte(Return);
            EmitBinaryDetailBadMatch(badMatch, tagged.Match, tagged.Read);

            return true;
        }

        private static (Atom Marker, Atom Success, Expr.Match Match, Expr.Call Read)? ReadExpansionMarker(Clause clause)
        {
            if (clause is not
                {
                    Guard: null, Patterns: [Pattern.List
                    {
                        Tail: null, Items: [
                Pattern.Literal { Value: Atom marker }, Pattern.Variable first, Pattern.Variable second, Pattern.Variable third]
                    },
                Pattern.Literal { Value: Atom { Name: "true" } }], Body: Expr.Sequence
                {
                    Items: [
                    Expr.Match
                    {
                        Pattern: Pattern.Tuple { Items: [Pattern.Literal { Value: Atom success }, Pattern.Variable value] },
                        Value: Expr.Call
                        {
                            Module: not null, DynamicRemote: false, Arguments: [
                            Expr.Tuple { Items: [Expr.Variable a, Expr.Variable b, Expr.Variable c] }]
                        } read
                    } match,
                    Expr.Variable returned]
                }
                } || !DistinctBindings(
                    first,
                    second,
                    third,
                    value
                )
                || a.Name != first.Name || b.Name != second.Name || c.Name != third.Name || returned.Name != value.Name)
            {
                return null;
            }

            return (marker, success, match, read);
        }

        private Expr.Call? ReadExpansionBinary(Clause clause)
        {
            return clause is
            {
                Patterns: [Pattern.Variable binary, Pattern.Variable table],
                Guard: Expr.Binary
                {
                    Operator: ErlangOperators.AndAlso, Left: Expr.Call binaryTest,
                    Right: Expr.Unary { Operator: ErlangOperators.Not, Operand: Expr.Call booleanTest }
                },
                Body: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable data, Expr.Variable target] } call
            }
                && DistinctBindings(binary, table) && data.Name == binary.Name && target.Name == table.Name
                && IsExpansionTest(binaryTest, AutoImportNames.IsBinary, binary.Name)
                && IsExpansionTest(booleanTest, AutoImportNames.IsBoolean, table.Name) ? call : null;
        }

        private (Expr.Call Tail, Expr.Call Proper, Expr.Call Improper)? ReadExpansionList(Clause clause, string function)
        {
            if (clause is not
                {
                    Guard: null, Patterns: [Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable rest },
                Pattern.Variable mode], Body: Expr.Case
                {
                    Value: Expr.Call
                    {
                        Module: null, Arguments: [
                    Expr.Variable tail, Expr.Variable tailMode]
                    } recurse,
                    Clauses: [
                    {
                        Patterns: [Pattern.Variable proper], Guard: Expr.Call listTest, Body: Expr.List
                        {
                            Items: [
                            Expr.Call { Module: null, Arguments: [Expr.Variable properHead, Expr.Variable properMode] } first],
                            Tail: Expr.Variable appended
                        }
                    },
                    {
                        Patterns: [Pattern.Variable improper], Guard: null, Body: Expr.List
                        {
                            Tail: null, Items: [
                            Expr.Call { Module: null, Arguments: [Expr.Variable improperHead, Expr.Variable improperMode] } second,
                            Expr.Variable last]
                        }
                    }]
                }
                }
                || !DistinctBindings(
                    head,
                    rest,
                    mode,
                    proper
                ) || !DistinctBindings(
                    head,
                    rest,
                    mode,
                    improper
                )
                || tail.Name != rest.Name || tailMode.Name != mode.Name || appended.Name != proper.Name || last.Name != improper.Name
                || properHead.Name != head.Name || improperHead.Name != head.Name
                || properMode.Name != mode.Name || improperMode.Name != mode.Name
                || recurse.Function != function || first.Function != function || second.Function != function
                || !IsExpansionTest(listTest, "is_list", proper.Name))
            {
                return null;
            }

            return (recurse, first, second);
        }

        private (Expr.Call Convert, Expr.Call Recur, Expr.Call Finish)? ReadExpansionTuple(Clause clause, string function)
        {
            if (clause is not
                {
                    Patterns: [Pattern.Variable tuple, Pattern.Variable mode], Guard: Expr.Call tupleTest,
                    Body: Expr.Call
                    {
                        Function: AutoImportNames.ListToTuple, Arguments: [
                    Expr.Call
                        {
                            Module: null, Arguments: [
                        Expr.Call { Function: AutoImportNames.TupleToList, Arguments: [Expr.Variable input] } convert,
                        Expr.Variable state]
                        } recur]
                    } finish
                }
                || !DistinctBindings(tuple, mode) || input.Name != tuple.Name || state.Name != mode.Name || recur.Function != function
                || !IsExpansionTest(tupleTest, AutoImportNames.IsTuple, tuple.Name)
                || !owner.ResolvesErlangBif(convert) || !owner.ResolvesErlangBif(finish)
                || convert.DynamicRemote || finish.DynamicRemote)
            {
                return null;
            }

            return (convert, recur, finish);
        }

        private bool IsExpansionTest(Expr.Call call, string function, string variable) =>
            call is { DynamicRemote: false, Arguments: [Expr.Variable input] }
            && call.Function == function && input.Name == variable && owner.ResolvesErlangBif(call);

        private void EmitExpandedHead(
            Expr.Call call,
            BeamSourceLocation previous,
            int entry,
            bool proper
        )
        {
            Load(2, 1);
            Load(1);
            owner.MoveRegister(
                YRegisterTag,
                0,
                YRegisterTag,
                2
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitCallLine(call, previous);
            owner.EmitFieldCall(2, entry);
            owner.Op(TestHeap, BeamWire.UnsignedTag, proper ? ConsHeapWords : 2 * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            if (!proper)
            {
                owner.Op(PutList, YRegisterTag, 0);
                owner.LiteralOperand(Nil.Value);
                owner.Operand(XRegisterTag, 1);
            }
            owner.Op(PutList, XRegisterTag, 0);
            owner.Operand(proper ? YRegisterTag : XRegisterTag, proper ? 0 : 1);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(1);
        }
    }
}
