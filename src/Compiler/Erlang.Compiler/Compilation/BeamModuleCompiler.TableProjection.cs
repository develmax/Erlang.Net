using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record TableProjection(
        Atom Tag,
        Expr.Match Match,
        Expr.Call Read,
        Expr.Call Columns,
        ListMapping Mapping,
        bool ColumnsAfter = false
    );

    private static TableProjection? ReadTableProjection(IReadOnlyList<Clause> clauses)
    {
        if (ReadDeferredTableProjection(clauses) is { } deferred)
        {
            return deferred;
        }
        if (clauses is not [
            {
                Guard: null, Patterns: [], Body: Expr.Sequence
                {
                    Items: [
                Expr.Match
                    {
                        Pattern: Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable rows, Pattern.Variable warning] },
                        Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [] } read
                    } match,
                Expr.Match { Pattern: Pattern.Variable columns, Value: Expr.Call { Module: null, Arguments: [] } describe },
                Expr.Match
                    {
                        Pattern: Pattern.Variable projected, Value: Expr.ListComprehension
                        {
                            Items: [var row],
                            Qualifiers: [ComprehensionQualifier.Generator { Strict: false, Source: Expr.Variable source } generator]
                        }
                    },
                Expr.Tuple { Items: [Expr.Variable header, Expr.Variable values, Expr.Variable returned] }]
                }
            }
        ] || !DistinctBindings(
            rows,
            warning,
            columns,
            projected
        ) || rows.Name != source.Name
            || warning.Name != returned.Name || columns.Name != header.Name || projected.Name != values.Name)
        {
            return null;
        }

        Expr.Call location = new(null, string.Empty, [])
        {
            SourceLine = generator.GeneratorLine,
            PreprocessorFile = generator.GeneratorFile
        };
        if (generator.Pattern is Pattern.Variable item && DistinctBindings(
            rows,
            warning,
            columns,
            projected,
            item
        )
            && row is Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable input] } convert
            && item.Name == input.Name)
        {
            return new(
                tag,
                match,
                read,
                describe,
                new(convert, location, null)
            );
        }
        if (generator.Pattern is Pattern.Tuple { Items: [Pattern.Variable description, Pattern.Variable record] }
            && row is Expr.Block
            {
                Body: Expr.Sequence
                {
                    Items: [
                Expr.Match
                    {
                        Pattern: Pattern.List
                        {
                            Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard },
                    Pattern.Variable { Name: VariableScopeNames.Wildcard }], Tail: Pattern.Variable tail
                        },
                        Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable inputRecord] } project
                    } discard,
                Expr.List { Items: [Expr.Variable label], Tail: Expr.Variable data }]
                }
            }
            && DistinctBindings(
                rows,
                warning,
                columns,
                projected,
                description,
                record,
                tail
            )
            && description.Name == label.Name && record.Name == inputRecord.Name && tail.Name == data.Name)
        {
            return new(
                tag,
                match,
                read,
                describe,
                new(
                    project,
                    location,
                    null,
                    discard
                )
            );
        }

        return null;
    }

    private static TableProjection? ReadDeferredTableProjection(IReadOnlyList<Clause> clauses)
    {
        if (clauses is not [
            {
                Guard: null, Patterns: [], Body: Expr.Sequence
                {
                    Items: [
                Expr.Match
                    {
                        Pattern: Pattern.Tuple
                        {
                            Items: [Pattern.Literal { Value: Atom tag },
                    Pattern.Variable rows, Pattern.Variable warning]
                        },
                        Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [] } read
                    } match,
                Expr.Match
                    {
                        Pattern: Pattern.Variable projected, Value: Expr.ListComprehension
                        {
                            Items: [
                    Expr.Call
                            {
                                Module: null or ErlangModuleNames.Module, Function: AutoImportNames.TupleToList, DynamicRemote: false,
                                Arguments: [Expr.Variable input]
                            } convert],
                            Qualifiers: [ComprehensionQualifier.Generator
                            {
                                Strict: false, Pattern: Pattern.Variable item,
                                Source: Expr.Variable source
                            } generator]
                        }
                    },
                Expr.Tuple
                    {
                        Items: [Expr.Call { Module: null, Arguments: [] } columns,
                    Expr.Variable values, Expr.Variable returned]
                    }]
                }
            }
        ] || !DistinctBindings(
            rows,
            warning,
            projected,
            item
        ) || input.Name != item.Name || source.Name != rows.Name
            || values.Name != projected.Name || returned.Name != warning.Name)
        {
            return null;
        }

        Expr.Call location = new(null, string.Empty, [])
        {
            SourceLine = generator.GeneratorLine,
            PreprocessorFile = generator.GeneratorFile
        };

        return new(
            tag,
            match,
            read,
            columns,
            new(convert, location, null),
            ColumnsAfter: true
        );
    }

    private void EmitTableProjection(TableProjection flow, BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries)
    {
        if (!entries.TryGetValue((flow.Columns.Function, 0), out int columns)
            || !entries.TryGetValue((MappingName(activeFunction, 0), 1), out int mapping)
            || flow.ColumnsAfter && !ResolvesErlangBif(flow.Mapping.Convert))
        {
            throw new NotSupportedException(Unsupported);
        }

        int badMatch = nextLabel++;
        Op(Allocate, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, 0);
        instructions.WriteByte(InitYRegisters);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, 2);
        Operand(YRegisterTag, 0);
        Operand(YRegisterTag, 1);
        EmitCallLine(flow.Read, location);
        EmitStaticRemoteCall(flow.Read);
        var readLocation = CallLocation(flow.Read, location);
        if (flow.ColumnsAfter)
        {
            EmitDeferredTableProjection(
                flow,
                readLocation,
                badMatch,
                columns,
                mapping
            );

            return;
        }
        MoveRegister(
            XRegisterTag,
            0,
            YRegisterTag,
            1
        );
        Op(IsTaggedTuple, LabelTag, badMatch);
        Operand(XRegisterTag, 0);
        Operand(BeamWire.UnsignedTag, 3);
        LiteralOperand(flow.Tag);
        Op(GetTupleElement, XRegisterTag, 0);
        Operand(BeamWire.UnsignedTag, 2);
        Operand(YRegisterTag, 0);
        EmitCallLine(flow.Columns, readLocation);
        EmitFieldCall(0, columns);
        Op(GetTupleElement, YRegisterTag, 1);
        Operand(BeamWire.UnsignedTag, 1);
        Operand(XRegisterTag, 1);
        MoveRegister(
            XRegisterTag,
            0,
            YRegisterTag,
            1
        );
        MoveRegister(
            XRegisterTag,
            1,
            XRegisterTag,
            0
        );
        EmitCallLine(flow.Mapping.Collect, CallLocation(flow.Columns, readLocation));
        EmitFieldCall(1, mapping);
        Op(TestHeap, BeamWire.UnsignedTag, 4);
        Operand(BeamWire.UnsignedTag, 1);
        EmitTagMatchTuple(
            0,
            (YRegisterTag, 1),
            (XRegisterTag, 0),
            (YRegisterTag, 0)
        );
        EmitFrameReturn(2);
        Op(Label, BeamWire.UnsignedTag, badMatch);
        EmitLine(flow.Match.SourceLine == 0 ? readLocation : new(flow.Match.PreprocessorFile, flow.Match.SourceLine));
        Op(BadMatch, XRegisterTag, 0);
    }

    private void EmitDeferredTableProjection(
        TableProjection flow,
        BeamSourceLocation readLocation,
        int badMatch,
        int columns,
        int mapping
    )
    {
        Op(IsTaggedTuple, LabelTag, badMatch);
        Operand(XRegisterTag, 0);
        Operand(BeamWire.UnsignedTag, 3);
        LiteralOperand(flow.Tag);
        EmitTuplePart(0, 1, 1);
        Op(GetTupleElement, XRegisterTag, 0);
        Operand(BeamWire.UnsignedTag, 2);
        Operand(YRegisterTag, 1);
        MoveRegister(
            XRegisterTag,
            1,
            XRegisterTag,
            0
        );
        EmitCallLine(flow.Mapping.Collect, readLocation);
        EmitFieldCall(1, mapping);
        MoveRegister(
            XRegisterTag,
            0,
            YRegisterTag,
            0
        );
        EmitCallLine(flow.Columns, CallLocation(flow.Mapping.Collect, readLocation));
        EmitFieldCall(0, columns);
        Op(TestHeap, BeamWire.UnsignedTag, 4);
        Operand(BeamWire.UnsignedTag, 1);
        EmitTagMatchTuple(
            0,
            (XRegisterTag, 0),
            (YRegisterTag, 0),
            (YRegisterTag, 1)
        );
        EmitFrameReturn(2);
        Op(Label, BeamWire.UnsignedTag, badMatch);
        EmitLine(flow.Match.SourceLine == 0 ? readLocation : new(flow.Match.PreprocessorFile, flow.Match.SourceLine));
        Op(BadMatch, XRegisterTag, 0);
    }

    private void EmitTupleProjectionHelper(
        string name,
        ListMapping map,
        BeamSourceLocation location,
        int entry
    )
    {
        var generator = CallLocation(map.Collect, location);
        StartFunction(
            name,
            1,
            generator,
            exported: false
        );
        int skip = nextLabel++;
        int empty = nextLabel++;
        int invalid = nextLabel++;
        int badMatch = nextLabel++;
        EmitNonemptyList(0, empty);
        EmitListParts(
            0,
            XRegisterTag,
            1,
            XRegisterTag,
            0
        );
        EmitTupleCheck(1, 2, skip);
        Op(Allocate, BeamWire.UnsignedTag, 3);
        Operand(BeamWire.UnsignedTag, 2);
        InitStackSlot(0);
        MoveRegister(
            XRegisterTag,
            0,
            YRegisterTag,
            2
        );
        Op(GetTupleElement, XRegisterTag, 1);
        Operand(BeamWire.UnsignedTag, 0);
        Operand(YRegisterTag, 1);
        EmitTuplePart(1, 1, 0);
        EmitCallLine(map.Convert, generator);
        EmitStaticRemoteCall(map.Convert);
        EmitNonemptyList(0, badMatch);
        Op(GetTail, XRegisterTag, 0);
        Operand(XRegisterTag, 1);
        EmitNonemptyList(1, badMatch);
        Op(GetTail, XRegisterTag, 1);
        Operand(YRegisterTag, 0);
        MoveRegister(
            YRegisterTag,
            2,
            XRegisterTag,
            0
        );
        MoveRegister(
            YRegisterTag,
            0,
            YRegisterTag,
            2
        );
        Op(TrimFrame, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, 2);
        EmitLine(generator);
        EmitFieldCall(1, entry);
        Op(TestHeap, BeamWire.UnsignedTag, 4);
        Operand(BeamWire.UnsignedTag, 1);
        Op(PutList, YRegisterTag, 0);
        Operand(YRegisterTag, 1);
        Operand(XRegisterTag, 1);
        Op(PutList, XRegisterTag, 1);
        Operand(XRegisterTag, 0);
        Operand(XRegisterTag, 0);
        EmitFrameReturn(2);
        Op(Label, BeamWire.UnsignedTag, skip);
        Op(TailLocalCall, BeamWire.UnsignedTag, 1);
        Operand(LabelTag, entry);
        EmitMappingEnd(empty, invalid);
        Op(Label, BeamWire.UnsignedTag, badMatch);
        var match = map.Discard!;
        EmitLine(match.SourceLine == 0 ? CallLocation(map.Convert, generator) : new(match.PreprocessorFile, match.SourceLine));
        Op(BadMatch, XRegisterTag, 0);
    }
}
