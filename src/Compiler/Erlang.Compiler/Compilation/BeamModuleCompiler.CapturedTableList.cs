using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record CapturedTableList(
        Expr.Call[] Reports,
        Term[] ReportArguments,
        Expr.Call[] Reads,
        Term HeadTitle,
        Term HeadView,
        Term TailTitle,
        Term TailView,
        Term RowView,
        ComprehensionQualifier.Generator Generator
    );

    private static CapturedTableList? ReadCapturedTableList(FunctionDefinition function,
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> functions)
    {
        if (function.Clauses is not [
            {
                Guard: null, Patterns: [], Body: Expr.Sequence
                {
                    Items: [
                var report1, Expr.Match { Pattern: Pattern.Variable head, Value: Expr.Call { Module: null, Arguments: [] } read1 },
                var report2, Expr.Match
                    {
                        Pattern: Pattern.Tuple { Items: [Pattern.Variable rows, Pattern.Variable warning] },
                        Value: Expr.Call { Module: null, Arguments: [] } read2
                    },
                var report3, Expr.Match { Pattern: Pattern.Variable tail, Value: Expr.Call { Module: null, Arguments: [] } read3 },
                var report4, Expr.Binary
                    {
                        Operator: ErlangOperators.Append,
                        Left: Expr.List
                        {
                            Items: [Expr.Tuple { Items: [Expr.Literal headTitle, Expr.Literal headView, Expr.Variable first] }],
                            Tail: Expr.ListComprehension
                            {
                                Items: [Expr.Tuple
                                {
                                    Items: [
                            Expr.Variable rowTitle, Expr.Literal rowView, Expr.Tuple
                                    {
                                        Items: [
                                Expr.Variable rowColumns, Expr.Variable rowData, Expr.Variable captured]
                                    }]
                                }],
                                Qualifiers: [ComprehensionQualifier.Generator
                                {
                                    Strict: false, Source: Expr.Variable source,
                                    Pattern: Pattern.Tuple { Items: [Pattern.Variable title, Pattern.Variable columns, Pattern.Variable data] }
                                } generator]
                            }
                        },
                        Right: Expr.List
                        {
                            Tail: null, Items: [Expr.Tuple
                            {
                                Items: [
                        Expr.Literal tailTitle, Expr.Literal tailView, Expr.Variable last]
                            }]
                        }
                    }]
                }
            }
        ] || !DistinctBindings(
            head,
            rows,
            warning,
            tail,
            title,
            columns,
            data
        )
            || head.Name != first.Name || tail.Name != last.Name || source.Name != rows.Name
            || rowTitle.Name != title.Name || rowColumns.Name != columns.Name || rowData.Name != data.Name
            || captured.Name != warning.Name || !ReturnsTableList(read2, functions)
            || !functions.ContainsKey((read1.Function, 0)) || !functions.ContainsKey((read3.Function, 0)))
        {
            return null;
        }

        var calls = new List<Expr.Call>(4);
        var arguments = new List<Term>(4);
        foreach (var report in new[] { report1, report2, report3, report4 })
        {
            if (report is not Expr.Call { Module: not null, DynamicRemote: false, Arguments: [var argument] } call
                || !AttributeLiteral.TryNormalizeConstant(argument, out Term literal))
            {
                return null;
            }
            calls.Add(call);
            arguments.Add(literal);
        }

        return new(
            calls.ToArray(),
            arguments.ToArray(),
            [read1, read2, read3],
            headTitle.Value,
            headView.Value,
            tailTitle.Value,
            tailView.Value,
            rowView.Value,
            generator
        );
    }

    private static bool ReturnsTableList(Expr.Call source,
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> functions)
    {
        if (!functions.TryGetValue((source.Function, 0), out var wrapper) || wrapper.Clauses is not [var clause]
            || LastExpression(clause.Body) is not Expr.Tuple { Items: [Expr.Call { Module: null, Arguments: [_] } call, _] }
            || !functions.TryGetValue((call.Function, 1), out var producer))
        {
            return false;
        }

        bool IsRecursiveCall(Expr expression) => expression is Expr.Call { Module: null, Arguments: [_] } recur
            && recur.Function == producer.Name;

        return producer.Clauses.All(branch => LastExpression(branch.Body) switch
        {
            Expr.List { Tail: null } list => list.Items.All(item => item is Expr.Tuple { Items.Count: 3 }),
            Expr.Literal { Value: Nil } => true,
            Expr.List { Items: [Expr.Tuple { Items.Count: 3 }], Tail: { } tail } => IsRecursiveCall(tail),
            var result => IsRecursiveCall(result)
        });
    }

    private static Expr LastExpression(Expr expression) => expression is Expr.Sequence { Items.Count: > 0 } sequence
        ? sequence.Items[^1] : expression;

    private static BeamSourceLocation CapturedTableLocation(CapturedTableList flow, BeamSourceLocation location) =>
        flow.Generator.GeneratorLine == 0 ? location : new(flow.Generator.GeneratorFile, flow.Generator.GeneratorLine);

    private void EmitCapturedTableHelper(
        string name,
        CapturedTableList flow,
        BeamSourceLocation location,
        int entry
    )
    {
        StartFunction(
            name,
            2,
            CapturedTableLocation(flow, location),
            exported: false
        );
        int empty = nextLabel++;
        EmitNonemptyList(0, empty);
        Op(Allocate, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, 2);
        MoveRegister(
            XRegisterTag,
            1,
            YRegisterTag,
            1
        );
        EmitListParts(
            0,
            YRegisterTag,
            0,
            XRegisterTag,
            0
        );
        EmitFieldCall(2, entry);
        Op(TestHeap, BeamWire.UnsignedTag, 10);
        Operand(BeamWire.UnsignedTag, 1);
        for (int field = 0; field < 3; field++)
        {
            Op(GetTupleElement, YRegisterTag, 0);
            Operand(BeamWire.UnsignedTag, field);
            Operand(XRegisterTag, field + 1);
        }
        EmitTagMatchTuple(
            2,
            (XRegisterTag, 2),
            (XRegisterTag, 3),
            (YRegisterTag, 1)
        );
        EmitTupleHeader(XRegisterTag, 3, 1);
        Operand(XRegisterTag, 1);
        LiteralOperand(flow.RowView);
        Operand(XRegisterTag, 2);
        Op(PutList, XRegisterTag, 1);
        Operand(XRegisterTag, 0);
        Operand(XRegisterTag, 0);
        EmitFrameReturn(2);
        Op(Label, BeamWire.UnsignedTag, empty);
        instructions.WriteByte(Return);
    }

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitReadTableWithWarning(FunctionDefinition function)
        {
            if (function.Clauses is not [
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
                    Expr.Tuple
                        {
                            Items: [Expr.Call { Module: null, Arguments: [Expr.Variable input] } transform,
                        Expr.Variable returned]
                        }]
                    }
                }
            ] || !DistinctBindings(rows, warning) || rows.Name != input.Name || warning.Name != returned.Name
                || !entries.TryGetValue((transform.Function, 1), out int target))
            {
                return false;
            }

            int failure = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.InitStackSlot(0);
            owner.EmitCallLine(read, location);
            owner.EmitStaticRemoteCall(read);
            owner.Op(IsTaggedTuple, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.LiteralOperand(tag);
            owner.EmitTuplePart(0, 1, 1);
            GetTupleField(
                0,
                2,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            var readLocation = CallLocation(read, location);
            owner.EmitCallLine(transform, readLocation);
            owner.EmitFieldCall(1, target);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTagMatchTuple(0, (XRegisterTag, 0), (YRegisterTag, 0));
            owner.EmitFrameReturn(1);
            Mark(failure);
            owner.EmitLine(match.SourceLine == 0 ? readLocation : new(match.PreprocessorFile, match.SourceLine));
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;
        }

        private bool TryEmitCapturedTableList(FunctionDefinition function)
        {
            if (ReadCapturedTableList(function, localFunctions) is not { } flow
                || !entries.TryGetValue((MappingName(function.Name, 0), 2), out int helper))
            {
                return false;
            }

            owner.Op(Allocate, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.instructions.WriteByte(InitYRegisters);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 3);
            for (int slot = 0; slot < 3; slot++)
            {
                owner.Operand(YRegisterTag, slot);
            }
            var previous = location;
            for (int index = 0; index < 3; index++)
            {
                var report = flow.Reports[index];
                var read = flow.Reads[index];
                EmitCallListReport(report, flow.ReportArguments[index], previous);
                var reportLocation = CallLocation(report, previous);
                owner.EmitCallLine(read, reportLocation);
                owner.EmitFieldCall(0, entries[(read.Function, 0)]);
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    2 - index
                );
                previous = CallLocation(read, reportLocation);
            }
            EmitCallListReport(flow.Reports[3], flow.ReportArguments[3], previous);
            for (int field = 0; field < 2; field++)
            {
                owner.Op(GetTupleElement, YRegisterTag, 1);
                owner.Operand(BeamWire.UnsignedTag, field);
                owner.Operand(XRegisterTag, field);
            }
            owner.MoveRegister(
                YRegisterTag,
                0,
                YRegisterTag,
                1
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitLine(CapturedTableLocation(flow, location));
            owner.EmitFieldCall(2, helper);
            EmitTableListEnd(flow.TailTitle, flow.TailView, prepend: false);
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            EmitAppend();
            EmitTableListEnd(flow.HeadTitle, flow.HeadView, prepend: true);
            owner.EmitFrameReturn(1);

            return true;
        }

        private void EmitTableListEnd(Term title, Term view, bool prepend)
        {
            owner.Op(TestHeap, BeamWire.UnsignedTag, 4 + ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, 3, 1);
            owner.LiteralOperand(title);
            owner.LiteralOperand(view);
            owner.Operand(YRegisterTag, 0);
            owner.Op(PutList, XRegisterTag, 1);
            if (prepend)
            {
                owner.Operand(XRegisterTag, 0);
            }
            else
            {
                owner.LiteralOperand(Nil.Value);
            }
            owner.Operand(XRegisterTag, prepend ? 0 : 1);
        }
    }
}
