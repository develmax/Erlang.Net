using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record PageComprehension(
        Expr.Call Single,
        Expr.Call Create,
        Term Id,
        Term Options,
        Expr.Call Map,
        ComprehensionQualifier.Generator Generator
    )
    {
        internal BeamSourceLocation Location(BeamSourceLocation fallback) =>
            Generator.GeneratorLine == 0 ? fallback : new(Generator.GeneratorFile, Generator.GeneratorLine);
    }

    private static PageComprehension? ReadPageComprehension(FunctionDefinition function,
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> functions)
    {
        if (function.Clauses is not [
            {
                Guard: null, Patterns: [Pattern.Variable panel,
                Pattern.List { Items: [Pattern.Tuple { Items: [Pattern.Variable ignored, Pattern.Variable callback] }], Tail: null },
                Pattern.Variable args], Body: Expr.Sequence
                {
                    Items: [
                    Expr.Match
                    {
                        Pattern: Pattern.Variable page, Value: Expr.Call
                        {
                            Module: null, DynamicRemote: false,
                            Arguments: [Expr.Variable singlePanel, Expr.Variable singleCallback, Expr.Variable singleArgs]
                        } single
                    },
                    Expr.Tuple { Items: [Expr.Variable pageResult, Expr.List { Items: [Expr.Variable onlyPage], Tail: null }] }
                ]
                }
            },
            {
                Guard: null, Patterns: [Pattern.Variable parent, Pattern.Variable specs, Pattern.Variable captures],
                Body: Expr.Sequence
                {
                    Items: [
                    Expr.Match
                    {
                        Pattern: Pattern.Variable notebook, Value: Expr.Call
                        {
                            Module: not null, DynamicRemote: false,
                            Arguments: [Expr.Variable createParent, var id, var options]
                        } create
                    },
                    Expr.Match
                    {
                        Pattern: Pattern.Variable pages, Value: Expr.ListComprehension
                        {
                            Items: [
                        Expr.Call
                            {
                                Module: null, DynamicRemote: false, Arguments: [
                            Expr.Variable mapNotebook, Expr.Variable mapTitle, Expr.Variable mapCallback, Expr.Variable mapArgs]
                            } map
                    ], Qualifiers: [ComprehensionQualifier.Generator
                    {
                        Strict: false, Source: Expr.Variable source,
                        Pattern: Pattern.Tuple { Items: [Pattern.Variable title, Pattern.Variable item] }
                    } generator]
                        }
                    },
                    Expr.Tuple { Items: [Expr.Variable notebookResult, Expr.Variable pagesResult] }
                ]
                }
            }
        ] || !DistinctBindings(
            panel,
            callback,
            args,
            page
        ) || ignored.Name != VariableScopeNames.Wildcard
                && !DistinctBindings(
                    panel,
                    ignored,
                    callback,
                    args,
                    page
                )
            || !DistinctBindings(
                parent,
                specs,
                captures,
                notebook,
                pages,
                title,
                item
            )
            || singlePanel.Name != panel.Name || singleCallback.Name != callback.Name || singleArgs.Name != args.Name
            || pageResult.Name != page.Name || onlyPage.Name != page.Name
            || createParent.Name != parent.Name || source.Name != specs.Name || mapNotebook.Name != notebook.Name
            || mapTitle.Name != title.Name || mapCallback.Name != item.Name || mapArgs.Name != captures.Name
            || notebookResult.Name != notebook.Name || pagesResult.Name != pages.Name
            || !functions.ContainsKey((single.Function, 3)) || !functions.ContainsKey((map.Function, 4))
            || !AttributeLiteral.TryNormalizeConstant(id, out Term idValue)
            || !AttributeLiteral.TryNormalizeConstant(options, out Term optionValue))
        {
            return null;
        }

        return new(
            single,
            create,
            idValue,
            optionValue,
            map,
            generator
        );
    }

    private void EmitPageComprehensionHelper(
        string helper,
        PageComprehension flow,
        BeamSourceLocation function,
        IReadOnlyDictionary<(string Name, int Arity), int> entries
    )
    {
        var location = flow.Location(function);
        StartFunction(
            helper,
            3,
            location,
            exported: false
        );
        int entry = entries[(helper, 3)];
        int skip = nextLabel++;
        int end = nextLabel++;
        int bad = nextLabel++;
        EmitNonemptyList(0, end);
        EmitListParts(
            0,
            XRegisterTag,
            3,
            XRegisterTag,
            0
        );
        EmitTupleCheck(3, 2, skip);
        Op(Allocate, BeamWire.UnsignedTag, 3);
        Operand(BeamWire.UnsignedTag, 4);
        MoveRegister(
            XRegisterTag,
            2,
            YRegisterTag,
            0
        );
        MoveRegister(
            XRegisterTag,
            0,
            YRegisterTag,
            1
        );
        MoveRegister(
            XRegisterTag,
            1,
            YRegisterTag,
            2
        );
        EmitTuplePart(3, 0, 1);
        EmitTuplePart(3, 1, 2);
        MoveRegister(
            YRegisterTag,
            2,
            XRegisterTag,
            3
        );
        MoveRegister(
            YRegisterTag,
            0,
            XRegisterTag,
            0
        );
        EmitCallLine(flow.Map, location);
        EmitFieldCall(4, entries[(flow.Map.Function, 4)]);
        MoveRegister(
            YRegisterTag,
            1,
            XRegisterTag,
            3
        );
        MoveRegister(
            YRegisterTag,
            2,
            XRegisterTag,
            1
        );
        MoveRegister(
            XRegisterTag,
            0,
            YRegisterTag,
            2
        );
        MoveRegister(
            YRegisterTag,
            0,
            XRegisterTag,
            2
        );
        Op(TrimFrame, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, 1);
        MoveRegister(
            XRegisterTag,
            3,
            XRegisterTag,
            0
        );
        EmitLine(location);
        EmitFieldCall(3, entry);
        Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
        Operand(BeamWire.UnsignedTag, 1);
        Op(PutList, YRegisterTag, 0);
        Operand(XRegisterTag, 0);
        Operand(XRegisterTag, 0);
        EmitFrameReturn(1);

        Op(Label, BeamWire.UnsignedTag, skip);
        Op(TailLocalCall, BeamWire.UnsignedTag, 3);
        Operand(LabelTag, entry);
        Op(Label, BeamWire.UnsignedTag, end);
        EmitNilTest(XRegisterTag, 0, bad);
        instructions.WriteByte(Return);
        Op(Label, BeamWire.UnsignedTag, bad);
        EmitTaggedArgumentError(
            new Expr.Call(ErlangModuleNames.Module, "error", []),
            Term.A("bad_generator"),
            [0],
            location
        );
    }

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitPageComprehension(FunctionDefinition function)
        {
            if (ReadPageComprehension(function, localFunctions) is not { } flow
                || !entries.TryGetValue((MappingName(function.Name, function.Arity), 3), out int helper))
            {
                return false;
            }

            int multiple = Label();
            owner.EmitNonemptyList(1, multiple);
            owner.EmitListParts(
                1,
                XRegisterTag,
                3,
                XRegisterTag,
                4
            );
            owner.EmitTupleCheck(3, 2, multiple);
            owner.EmitNilTest(XRegisterTag, 4, multiple);
            owner.Op(Allocate, BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.EmitTuplePart(3, 1, 1);
            var previous = location;
            EmitWindowLocal(flow.Single, entries[(flow.Single.Function, 3)], ref previous);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords + 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutList, XRegisterTag, 0);
            owner.Operand(AtomTag, NilAtomIndex);
            owner.Operand(XRegisterTag, 1);
            owner.EmitTagMatchTuple(0, (XRegisterTag, 0), (XRegisterTag, 1));
            owner.EmitFrameReturn(0);

            Mark(multiple);
            owner.Op(Allocate, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.MoveRegister(
                XRegisterTag,
                2,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                1
            );
            owner.EmitConstant(flow.Options, 2);
            owner.EmitConstant(flow.Id, 1);
            EmitWindowCall(flow.Create, ref previous);
            Load(1, 2);
            SaveWindowResult(1);
            Load(0, 1);
            owner.Op(Swap, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 2);
            TrimDetailFrame(1, 1);
            owner.EmitLine(flow.Location(location));
            owner.EmitFieldCall(3, helper);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTagMatchTuple(0, (YRegisterTag, 0), (XRegisterTag, 0));
            owner.EmitFrameReturn(1);

            return true;
        }
    }
}
