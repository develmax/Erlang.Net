using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTupleOfListHeads(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null, Body: Expr.Tuple tuple } clause]
                || !HasUnconditionalParameters(clause)
                || tuple.Items.Count != function.Arity)
            {
                return false;
            }

            string[] parameters = clause.Patterns.Cast<Pattern.Variable>().Select(pattern => pattern.Name).ToArray();
            var registers = new List<int>(tuple.Items.Count);
            var heads = new List<(int Register, int Line)>();
            var bindings = new HashSet<string>(StringComparer.Ordinal);
            foreach (Expr item in tuple.Items)
            {
                string input;
                int? line = null;
                if (item is Expr.Variable variable)
                {
                    input = variable.Name;
                }
                else if (item is Expr.Sequence
                {
                    Items: [
                        Expr.Match
                        {
                            Pattern: Pattern.List
                            {
                                Items: [Pattern.Variable head],
                                Tail: Pattern.Variable { Name: VariableScopeNames.Wildcard }
                            },
                            Value: Expr.Variable list,
                            PreprocessorFile: null,
                            SourceLine: > 0
                        } match,
                        Expr.Variable result
                    ]
                } && head.Name == result.Name && !parameters.Contains(head.Name) && bindings.Add(head.Name))
                {
                    input = list.Name;
                    line = match.SourceLine;
                }
                else
                {
                    return false;
                }

                int register = Array.IndexOf(parameters, input);
                if (register < 0 || registers.Contains(register))
                {
                    return false;
                }
                registers.Add(register);
                if (line is int matchLine)
                {
                    heads.Add((register, matchLine));
                }
            }

            if (heads.Count == 0 || heads.Any(head => head.Line != heads[0].Line))
            {
                return false;
            }

            int[] failures = heads.Select(_ => Label()).Reverse().ToArray();
            for (int index = 0; index < heads.Count; index++)
            {
                int register = heads[index].Register;
                owner.Op(IsNonemptyList, LabelTag, failures[index]);
                owner.Operand(XRegisterTag, register);
                if (index == heads.Count - 1)
                {
                    owner.Op(TestHeap, BeamWire.UnsignedTag, registers.Count + 1);
                    owner.Operand(BeamWire.UnsignedTag, function.Arity);
                }
                owner.Op(GetHead, XRegisterTag, register);
                owner.Operand(XRegisterTag, register);
            }
            owner.Op(PutTuple, XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, registers.Count);
            foreach (int register in registers)
            {
                owner.Operand(XRegisterTag, register);
            }
            owner.instructions.WriteByte(Return);

            BeamSourceLocation previous = location;
            for (int index = heads.Count - 1; index >= 0; index--)
            {
                Mark(failures[index]);
                owner.EmitArithmeticLine(heads[index].Line, ref previous);
                owner.Op(BadMatch, XRegisterTag, heads[index].Register);
            }

            return true;
        }
    }
}
