using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static (Expr.Variable Source, Atom Tag)? ReadTupleProjection(Expr expression) =>
        expression is Expr.ListComprehension
        {
            Items: [Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, Expr.Variable value] }],
            Qualifiers: [ComprehensionQualifier.Generator
            {
                Strict: false, Pattern: Pattern.Variable item, Source: Expr.Variable source
            }]
        } && item.Name == value.Name && item.Name != source.Name ? (source, tag) : null;

    private static Atom? ReadTupleProjectionHelper(FunctionDefinition function) =>
        function.Clauses is [{ Guard: null, Patterns: [Pattern.Variable input], Body: var body }]
        && ReadTupleProjection(body) is { } projection && projection.Source.Name == input.Name ? projection.Tag : null;

    private static (Expr Body, FunctionDefinition Helper, BeamSourceLocation Location)? ExtractTupleProjection(
        Expr body,
        string name,
        BeamSourceLocation location
    )
    {
        FunctionDefinition? helper = null;
        BeamSourceLocation helperLocation = location;
        Expr rewritten = Rewrite(body, root: true);
        return helper is null ? null : (rewritten, helper, helperLocation);

        Expr Rewrite(Expr expression, bool root = false)
        {
            if (helper is not null)
            {
                return expression;
            }
            if (root && expression is Expr.ListComprehension comprehension
                && ReadTupleProjection(comprehension) is { } projection)
            {
                int line = comprehension.PreprocessorLine == 0
                    ? comprehension.Source?.Line ?? location.Line : comprehension.PreprocessorLine;
                helperLocation = new(comprehension.PreprocessorFile ?? location.File, line);
                helper = new(name, 1, [new([new Pattern.Variable(projection.Source.Name)], null, comprehension)]);
                return new Expr.Call(null, name, [projection.Source])
                {
                    SourceLine = line,
                    PreprocessorFile = helperLocation.File
                };
            }

            return expression switch
            {
                Expr.Sequence sequence => sequence with { Items = sequence.Items.Select(item => Rewrite(item)).ToArray() },
                Expr.Case decision => decision with
                {
                    Value = Rewrite(decision.Value),
                    Clauses = decision.Clauses.Select(clause => clause with { Body = Rewrite(clause.Body) }).ToArray()
                },
                Expr.Call { Module: not null, DynamicRemote: false } call => call with
                {
                    Arguments = call.Arguments.Select(argument => argument is Expr.List { Tail: Expr.ListComprehension tail } list
                        ? list with { Tail = Rewrite(tail, root: true) } : argument).ToArray()
                },
                _ => expression
            };
        }
    }

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTupleProjectionHelper(FunctionDefinition function)
        {
            if (ReadTupleProjectionHelper(function) is not { } tag)
            {
                return false;
            }

            int entry = owner.nextLabel - 1;
            int empty = Label();
            int invalid = Label();
            owner.Op(IsNonemptyList, LabelTag, empty);
            owner.Operand(XRegisterTag, 0);
            owner.Op(Allocate, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(GetList, XRegisterTag, 0);
            owner.Operand(YRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFieldCall(1, entry);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3 + ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTagMatchTuple(1, (AtomTag, owner.Intern(tag.Name)), (YRegisterTag, 0));
            owner.Op(PutList, XRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(1);
            Mark(empty);
            owner.Op(IsNil, LabelTag, invalid);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(Return);
            Mark(invalid);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(ErlangErrorReasons.BadGenerator)), (XRegisterTag, 0));
            owner.Op(TailExternalCall, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Error, 1));

            return true;
        }
    }
}
