using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitLeadingAtomTailDispatch(FunctionDefinition function, bool closed = false)
        {
            if (function.Arity < 2 || function.Clauses.Count < 2)
            {
                return false;
            }

            Clause first = function.Clauses[0];
            int selector = Array.FindIndex(first.Patterns.ToArray(), pattern => pattern is Pattern.Literal { Value: Atom });
            if (selector < 0 || closed != (function.Clauses[^1].Patterns[selector] is Pattern.Literal { Value: Atom }))
            {
                return false;
            }

            int armCount = function.Clauses.Count - (closed ? 0 : 1);
            if (armCount < 2)
            {
                return false;
            }

            // Unguarded complete selectors are handled by the other literal dispatch paths.
            int? guardRegister = null;
            var keys = new HashSet<Atom>();
            var branches = new List<(Atom Key, EntryCall Entry)>();
            foreach (Clause clause in function.Clauses.Take(armCount))
            {
                if (clause.Body is not Expr.Call call
                    || clause.Patterns[selector] is not Pattern.Literal { Value: Atom key } || !keys.Add(key))
                {
                    return false;
                }

                var parameters = clause.Patterns.ToArray();
                parameters[selector] = new Pattern.Variable(VariableScopeNames.Wildcard);
                if (!HasUnconditionalParameters(clause with { Patterns = parameters }))
                {
                    return false;
                }

                if (closed)
                {
                    if (clause.Guard is not Expr.Call
                        {
                            Function: AutoImportNames.IsPid, DynamicRemote: false,
                            Module: null or ErlangModuleNames.Module, Arguments: [Expr.Variable argument]
                        } guard
                        || !owner.ResolvesErlangBif(guard))
                    {
                        return false;
                    }

                    int register = Array.FindIndex(parameters, parameter =>
                        parameter is Pattern.Variable { Name: var name } && name == argument.Name);
                    if (register < 0 || register == selector || guardRegister is int previous && previous != register)
                    {
                        return false;
                    }

                    guardRegister = register;
                }
                else if (clause.Guard is not null)
                {
                    return false;
                }

                if (ReadEntryCall(
                        call,
                        parameters.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray(),
                        allowControlTransfer: true
                    ) is not { ConstructedArgument: null, FunctionArgument: null } entry
                    || closed && entry.Module is not null
                    || entry.Module is { } module && IsTrappingTailBif(module, call.Function, call.Arguments.Count))
                {
                    return false;
                }

                branches.Add((key, entry));
            }

            int functionInfo = owner.nextLabel - 2;
            var atomOrder = Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
            // The switch visits its failure edge and ascending atom edges during
            // DFS. Reverse postorder lays out independent arms in descending order.
            bool independent = HasIndependentLeadingTailCalls(branches.Select(branch => branch.Entry));
            if (closed && !independent)
            {
                return false;
            }

            var bodyOrder = independent ? branches.OrderByDescending(branch => branch.Key, atomOrder) : branches.AsEnumerable();
            var labeled = bodyOrder.Select(branch => (branch.Key, branch.Entry, Label: Label())).ToArray();
            int fallback = closed ? functionInfo : Label();
            owner.Op(SelectValue, XRegisterTag, selector);
            owner.Operand(LabelTag, fallback);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, labeled.Length * 2);
            foreach (var branch in labeled.OrderBy(branch => branch.Key, atomOrder))
            {
                owner.LiteralOperand(branch.Key);
                owner.Operand(LabelTag, branch.Label);
            }

            foreach (var branch in labeled)
            {
                Mark(branch.Label);
                // A distinct selector value has already matched. Guard failure
                // can only reach the function's failure label.
                if (guardRegister is int register)
                {
                    owner.Op(IsPid, LabelTag, functionInfo);
                    owner.Operand(XRegisterTag, register);
                }

                EmitEntryCallArguments(branch.Entry);
                Expr.Call call = branch.Entry.Call;
                if (branch.Entry.Module is { } module)
                {
                    owner.EmitCallLine(call, location);
                    owner.Op(TailExternalCall, BeamWire.UnsignedTag, call.Arguments.Count);
                    owner.Operand(BeamWire.UnsignedTag, owner.Import(module, call.Function, call.Arguments.Count));
                }
                else
                {
                    owner.Op(TailLocalCall, BeamWire.UnsignedTag, call.Arguments.Count);
                    owner.Operand(LabelTag, entries[(call.Function, call.Arguments.Count)]);
                }
            }

            if (!closed)
            {
                Mark(fallback);
                EmitGeneralFunction(function with { Clauses = [function.Clauses[^1]] }, functionInfo);
            }

            return true;
        }

        private static bool HasIndependentLeadingTailCalls(IEnumerable<EntryCall> calls)
        {
            var seen = new List<EntryCall>();
            foreach (var call in calls)
            {
                if (seen.Any(previous => previous.Module == call.Module
                    && previous.Call.Function == call.Call.Function
                    && previous.Call.Arguments.Count == call.Call.Arguments.Count
                    && previous.Moves.SequenceEqual(call.Moves)
                    && previous.Constants.SequenceEqual(call.Constants)))
                {
                    return false;
                }

                seen.Add(call);
            }

            return true;
        }
    }
}
