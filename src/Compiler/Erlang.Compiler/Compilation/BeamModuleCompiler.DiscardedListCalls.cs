using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record DiscardedListCalls(
        Expr.ListComprehension List,
        Expr.Call Item,
        Term ListResult,
        Expr.Call Erase,
        Atom Missing,
        Term MissingResult,
        Expr.Call Present
    );

    private static DiscardedListCalls? ReadDiscardedListCalls(
        FunctionDefinition function,
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> functions
    )
    {
        if (function.Clauses is not [
            {
                Patterns: [Pattern.Variable list],
                Guard: Expr.Call
                {
                    Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                    Function: AutoImportNames.IsList, Arguments: [Expr.Variable listTest]
                } listGuard,
                Body: Expr.Sequence
                {
                    Items: [
                    Expr.ListComprehension
                    {
                        Items: [Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable item] } each],
                        Qualifiers: [ComprehensionQualifier.Generator
                        {
                            Pattern: Pattern.Variable member, Source: Expr.Variable source, Strict: false
                        }]
                    } comprehension,
                    Expr.Literal { Value: Atom listResult }
                ]
                }
            },
            {
                Patterns: [Pattern.Variable key],
                Guard: Expr.Call
                {
                    Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                    Function: AutoImportNames.IsAtom, Arguments: [Expr.Variable keyTest]
                } keyGuard,
                Body: Expr.Case
                {
                    Value: Expr.Call
                    {
                        Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                        Function: AutoImportNames.Erase, Arguments: [Expr.Variable input]
                    } erase,
                    Clauses: [
                    {
                        Guard: null, Patterns: [Pattern.Literal { Value: Atom missing }],
                        Body: Expr.Literal { Value: Atom missingResult }
                    },
                    {
                        Guard: null, Patterns: [Pattern.Variable bound],
                        Body: Expr.Call
                        {
                            Module: not null, DynamicRemote: false,
                            Arguments: [Expr.Variable value]
                        } present
                    }
                    ]
                }
            }
        ] || !DistinctBindings(list) || !DistinctBindings(member) || !DistinctBindings(key, bound)
            || list.Name != listTest.Name || list.Name != source.Name || member.Name != item.Name
            || key.Name != keyTest.Name || key.Name != input.Name || bound.Name != value.Name
            || !functions.ContainsKey((each.Function, 1))
            || listGuard.Module is null && functions.ContainsKey((AutoImportNames.IsList, 1))
            || keyGuard.Module is null && functions.ContainsKey((AutoImportNames.IsAtom, 1))
            || erase.Module is null && functions.ContainsKey((AutoImportNames.Erase, 1)))
        {
            return null;
        }

        return new(
            comprehension,
            each,
            listResult,
            erase,
            missing,
            missingResult,
            present
        );
    }

    private void EmitDiscardedListDispatch(DiscardedListCalls flow, int helper, BeamSourceLocation location)
    {
        int functionInfo = nextLabel - 2;
        int atom = nextLabel++;
        int present = nextLabel++;
        Op(IsList, LabelTag, atom);
        Operand(XRegisterTag, 0);
        Op(Allocate, BeamWire.UnsignedTag, 0);
        Operand(BeamWire.UnsignedTag, 1);
        EmitLine(ComprehensionLocation(flow, location));
        EmitFieldCall(1, helper);
        EmitConstant(flow.ListResult, 0);
        EmitFrameReturn();

        Op(Label, BeamWire.UnsignedTag, atom);
        Op(IsAtom, LabelTag, functionInfo);
        Operand(XRegisterTag, 0);
        Op(Allocate, BeamWire.UnsignedTag, 0);
        Operand(BeamWire.UnsignedTag, 1);
        EmitCallLine(flow.Erase, location);
        Op(ExternalCall, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, AutoImportNames.Erase, 1));
        EmitAtomTest(0, flow.Missing.Name, present);
        EmitConstant(flow.MissingResult, 0);
        EmitFrameReturn();

        Op(Label, BeamWire.UnsignedTag, present);
        EmitCallLine(flow.Present, CallLocation(flow.Erase, location));
        EmitTailRemote(flow.Present.Module!, flow.Present.Function, framed: true);
    }

    private void EmitDiscardedListHelper(
        string helper,
        DiscardedListCalls flow,
        BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries
    )
    {
        StartFunction(
            helper,
            1,
            ComprehensionLocation(flow, location),
            exported: false
        );
        int empty = nextLabel++;
        int invalid = nextLabel++;
        EmitNonemptyList(0, empty);
        Op(Allocate, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, 1);
        EmitListParts(
            0,
            XRegisterTag,
            0,
            YRegisterTag,
            0
        );
        EmitFieldCall(1, entries[(flow.Item.Function, 1)]);
        MoveRegister(
            YRegisterTag,
            0,
            XRegisterTag,
            0
        );
        Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 1);
        Operand(LabelTag, entries[(helper, 1)]);
        Operand(BeamWire.UnsignedTag, 1);
        EmitMappingEnd(empty, invalid);
    }

    private static BeamSourceLocation ComprehensionLocation(DiscardedListCalls flow, BeamSourceLocation location) =>
        flow.List.PreprocessorLine == 0 ? CallLocation(flow.Item, location) : new(flow.List.PreprocessorFile, flow.List.PreprocessorLine);
}
