using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private void EmitMaybe(Expr.Maybe conditional)
        {
            var incoming = new Dictionary<string, int>(variables, StringComparer.Ordinal);
            bool hasMatch = conditional.Items.Any(item => item is Expr.MaybeMatch);
            int failure = hasMatch ? Label() : 0;
            int failedValue = hasMatch ? Slot() : 0;
            for (int index = 0; index < conditional.Items.Count; index++)
            {
                if (conditional.Items[index] is Expr.MaybeMatch match)
                {
                    Expression(match.Value);
                    int value = Save();
                    owner.MoveRegister(XRegisterTag, 0, YRegisterTag, failedValue);
                    Pattern(match.Pattern, value, failure);
                    if (index == conditional.Items.Count - 1)
                    {
                        Load(value);
                    }
                }
                else
                {
                    Expression(conditional.Items[index]);
                }
            }

            variables = incoming;
            if (!hasMatch)
            {
                return;
            }

            int done = Label();
            JumpTo(done);
            Mark(failure);
            if (conditional.Clauses.Count == 0)
            {
                Load(failedValue);
            }
            else
            {
                EmitCaseClauses(conditional.Clauses, failedValue, failureReason: ErlangErrorReasons.ElseClause);
            }

            Mark(done);
        }
    }
}
