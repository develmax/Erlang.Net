using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitNestedListRegistration(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null,
                Patterns: [Pattern.List
                {
                    Items: [Pattern.List { Items: [Pattern.Variable first, Pattern.Variable second], Tail: null }],
                    Tail: null
                }],
                Body: Expr.Sequence { Items.Count: >= 3 } sequence
            }]
                || !DistinctBindings(first, second)
                || sequence.Items[0] is not Expr.Match
                {
                    Pattern: Pattern.Variable registeredName,
                    Value: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable left, Expr.Variable right] } lookup
                }
                || left.Name != first.Name || right.Name != second.Name
                || !DistinctBindings(first, registeredName) || !DistinctBindings(second, registeredName)
                || !entries.TryGetValue((lookup.Function, 2), out int entry)
                || sequence.Items[1] is not Expr.Call
                {
                    Function: AutoImportNames.Register, Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                    Arguments: [Expr.Variable name, Expr.Call
                    {
                        Function: AutoImportNames.Self, Module: null or ErlangModuleNames.Module,
                        DynamicRemote: false, Arguments.Count: 0
                    } self]
                } registration
                || name.Name != registeredName.Name
                || registration.Module is null && entries.ContainsKey((registration.Function, 2))
                || self.Module is null && entries.ContainsKey((self.Function, 0)))
            {
                return false;
            }

            var bindings = new Dictionary<string, Term>(StringComparer.Ordinal);
            foreach (Expr item in sequence.Items.Skip(2).SkipLast(1))
            {
                if (item is not Expr.Match { Pattern: Pattern.Variable binding, Value: var value }
                    || binding.Name == first.Name || binding.Name == second.Name || binding.Name == registeredName.Name
                    || binding.Name == VariableScopeNames.Wildcard
                    || !AttributeLiteral.TryNormalizeConstant(value, out Term constant, bindings)
                    || !bindings.TryAdd(binding.Name, constant))
                {
                    return false;
                }
            }

            if (!AttributeLiteral.TryNormalizeConstant(sequence.Items[^1], out Term result, bindings))
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            owner.EmitNonemptyList(0, failure);
            owner.EmitListParts(0, XRegisterTag, 1, XRegisterTag, 2);
            owner.EmitNonemptyList(1, failure);
            owner.EmitListParts(1, XRegisterTag, 3, XRegisterTag, 1);
            owner.EmitNonemptyList(1, failure);
            owner.EmitListParts(1, XRegisterTag, 4, XRegisterTag, 1);
            owner.EmitNilTest(XRegisterTag, 1, failure);
            owner.EmitNilTest(XRegisterTag, 2, failure);
            owner.Op(Allocate, BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 5);
            owner.MoveRegister(XRegisterTag, 4, XRegisterTag, 1);
            owner.MoveRegister(XRegisterTag, 3, XRegisterTag, 0);
            owner.EmitCallLine(lookup, location);
            owner.EmitFieldCall(2, entry);
            owner.Op(BifZero, BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, self.Function, 0));
            owner.Operand(XRegisterTag, 1);
            owner.EmitCallLine(registration, CallLocation(lookup, location));
            owner.Op(ExternalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, registration.Function, 2));
            owner.EmitConstant(result, 0);
            owner.EmitFrameReturn();

            return true;
        }
    }
}
