using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
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
            owner.MoveRegister(XRegisterTag, 1, YRegisterTag, current);
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
            owner.MoveRegister(XRegisterTag, 0, YRegisterTag, current);
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
