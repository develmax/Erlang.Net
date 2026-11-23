using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private (Expr.Call Call, FunctionDefinition Helper, BeamSourceLocation Location)? PlanMapComprehensionListHelper(
        FunctionDefinition function,
        Clause clause,
        Expr.MapComprehension map,
        BeamSourceLocation location,
        (int Core, int Ssa) ordinaryListOrdinals,
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> localFunctions
    )
    {
        if (map is not { Fields: [{ Key: var key, Value: var value }], Qualifiers: [ComprehensionQualifier.Generator] }
            || map.SourceLine == 0)
        {
            return null;
        }

        // Pinned v3_core:mc_pairs keeps a safe generator-independent value
        // out of the helper. Dependent values use the ordinary key/value list.
        Expr[] valueNodes = LocalFunctionEntries.Walk(value).ToArray();
        if (valueNodes.Any(node => node is not (Expr.Literal or Expr.List or Expr.Tuple or Expr.Variable)))
        {
            return null;
        }

        bool fromKeys = !valueNodes.OfType<Expr.Variable>().Any();
        Expr element = fromKeys ? key : new Expr.Tuple([key, value]);
        var pairs = new Expr.ListComprehension([element], map.Qualifiers)
        {
            PreprocessorLine = map.SourceLine,
            PreprocessorFile = map.PreprocessorFile
        };
        var body = BeamTargetedInlining.Rewrite(
            clause.Body,
            expression => expression,
            rewriteScope: expression => ReferenceEquals(expression, map) ? pairs : null
        );
        string helper = MappingName(
            function.Name,
            function.Arity,
            1 - ordinaryListOrdinals.Core,
            1 - ordinaryListOrdinals.Ssa
        );
        if (PlanRecursiveListComprehension(
            pairs,
            clause with { Body = body },
            location,
            helper,
            localFunctions,
            independent: false,
            discarded: false,
            allowStrict: true,
            appendTail: null
        ) is not { } plan)
        {
            return null;
        }

        var call = fromKeys ? new Expr.Call(MapModuleNames.Module, MapModuleNames.FromKeys, [plan.Call, value])
            : new Expr.Call(MapModuleNames.Module, MapModuleNames.FromList, [plan.Call]);
        call = call with
        {
            SourceLine = map.SourceLine,
            PreprocessorFile = map.PreprocessorFile
        };

        return (call, plan.Helper, plan.Location);
    }

    private sealed partial class GeneralEmitter
    {
        private void EmitMapComprehension(Expr.MapComprehension comprehension)
        {
            Expr[] pairs = comprehension.Fields.Select(field => (Expr)new Expr.Tuple([field.Key, field.Value])).ToArray();
            EmitComprehensionList(pairs, comprehension.Qualifiers);
            Call("maps", "from_list", 1);
        }

        private void EmitMapGenerator(ComprehensionQualifier.MapGenerator generator, int reject, Action<int> emitBody)
        {
            var outer = new Dictionary<string, int>(variables, StringComparer.Ordinal);
            Expression(generator.Source);
            int source = Save();
            Call("erts_internal", "mc_iterator", 1);
            int current = Save();
            int loop = Label();
            int refill = Label();
            int end = Label();
            int invalid = Label();
            int mismatch = generator.Strict ? Label() : loop;

            Mark(loop);
            Load(current);
            owner.Op(IsTuple, LabelTag, refill);
            owner.Operand(XRegisterTag, 0);
            owner.Op(TestArity, LabelTag, invalid);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.EmitTuplePart(0, 2, 1);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                current
            );
            owner.EmitTuplePart(0, 0, 1);
            owner.EmitTuplePart(0, 1, 2);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.Op(PutTuple, XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 2);
            int pair = Save();
            MatchGeneratorPattern(generator.Pattern, pair, mismatch);
            emitBody(loop);
            JumpTo(loop);

            if (generator.Strict)
            {
                Mark(mismatch);
                Load(pair);
                owner.Op(BadMatch, XRegisterTag, 0);
            }

            Mark(refill);
            owner.EmitNonemptyList(0, end);
            Call("erts_internal", "mc_refill", 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                current
            );
            JumpTo(loop);

            Mark(end);
            owner.EmitAtomTest(0, "none", invalid);
            JumpTo(reject);
            Mark(invalid);
            ErrorTuple(ComprehensionErrorReasons.BadGenerator, source);
            variables = outer;
        }
    }
}
