using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitEmptyOrSingleton(FunctionDefinition function)
        {
            if (function.Clauses is not [
                { Guard: null, Body: Expr.List { Items: [], Tail: null } or Expr.Literal { Value: Nil } } empty,
                {
                    Patterns: [Pattern.Variable input], Guard: null,
                    Body: Expr.List { Items: [Expr.Variable element], Tail: null }
                }
            ] || !HasNilParameter(empty) || input.Name != element.Name)
            {
                return false;
            }

            int singleton = Label();
            owner.Op(IsNil, LabelTag, singleton);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(Return);
            Mark(singleton);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutList, XRegisterTag, 0);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(Return);

            return true;
        }

        private bool TryEmitCheckedListIdentity(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null, Body: Expr.Sequence sequence } clause]
                || sequence.Items is not [
                    Expr.Match
                {
                    Pattern: Pattern.List
                    {
                        Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                        Tail: Pattern.Variable { Name: VariableScopeNames.Wildcard }
                    },
                    Value: Expr.Variable input
                } check,
                    Expr.Variable result
                ] || !HasUnconditionalParameters(clause) || result.Name != input.Name)
            {
                return false;
            }

            int register = ArgumentRegister(clause, input.Name);
            int failure = Label();
            owner.Op(IsNonemptyList, LabelTag, failure);
            owner.Operand(XRegisterTag, register);
            if (register != 0)
            {
                owner.MoveToX0(XRegisterTag, register);
            }
            owner.instructions.WriteByte(Return);
            Mark(failure);
            owner.EmitLine(check.SourceLine == 0 ? location : new(check.PreprocessorFile, check.SourceLine));
            owner.Op(BadMatch, XRegisterTag, register);

            return true;
        }
    }
}
