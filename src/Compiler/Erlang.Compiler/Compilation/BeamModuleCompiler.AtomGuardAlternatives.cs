using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitAtomGuardAlternatives(Expr.GuardAlternatives alternatives, int failure, string? selector)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || owner.receiveScratchRegisters is not null
                || alternatives.Items.Count < 3 || alternatives.Items[0] is not Expr.Binary first
                || (first.Left as Expr.Variable ?? first.Right as Expr.Variable) is not { } input
                || alternatives.Items.Any(item => item is not Expr.Binary
                { Operator: ErlangOperators.NumericEqual or ErlangOperators.ExactEqual }))
            {
                return false;
            }

            bool registerSelector = IsGuardSelector(input, selector);
            int slot = 0;
            if (!registerSelector && !variables.TryGetValue(input.Name, out slot))
            {
                return false;
            }

            var keys = new List<Atom>();
            if (!ReadAtomGuardKeys(alternatives, input.Name, keys) || keys.Distinct().Count() != keys.Count)
            {
                return false;
            }

            // Each alternative reads the same bound value and can only match its atom.
            // Keep the enclosing guard failure and fall through to the same source body.
            int accepted = Label();
            var order = Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
            var table = keys.Order(order).Select(key => ((Term)key, accepted)).ToArray();
            owner.Op(SelectValue, registerSelector ? XRegisterTag : YRegisterTag, slot);
            owner.EmitMembershipSelectTable(failure, table);
            Mark(accepted);

            return true;
        }
    }
}
