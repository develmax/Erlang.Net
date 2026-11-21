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

            if (closed && !owner.ModuleOptimizationDisabled
                && (TryEmitClosedRemoteTailDispatch(function) || TryEmitClosedLocalExitDispatch(function)))
            {
                return true;
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

        private bool TryEmitClosedLocalExitDispatch(FunctionDefinition function)
        {
            if (function.Arity != 3 || function.Clauses.Count != 3)
            {
                return false;
            }

            var keys = new HashSet<Atom>();
            var branches = new List<(Atom Key, EntryCall Tail, EntryCall? Preparation, string[] Parameters)>();
            int exits = 0;
            foreach (var clause in function.Clauses)
            {
                if (clause is not
                    {
                        Guard: null,
                        Patterns: [Pattern.Literal { Value: Atom key }, Pattern.Variable value, Pattern.Variable context]
                    }
                    || !keys.Add(key)
                    || value.Name != VariableScopeNames.Wildcard && value.Name == context.Name)
                {
                    return false;
                }

                string[] parameters = [VariableScopeNames.Wildcard, value.Name, context.Name];
                if (clause.Body is Expr.Call { Function: AutoImportNames.Exit, Arguments: [var reason] } exit
                    && owner.ResolvesErlangBif(exit) && AttributeLiteral.TryNormalizeConstant(reason, out _)
                    && ReadEntryCall(exit, [], allowControlTransfer: true) is { } failure)
                {
                    branches.Add((key, failure, null, parameters));
                    exits++;
                    continue;
                }

                Expr action = clause.Body;
                string tailInput = value.Name;
                string[] tailParameters = parameters;
                EntryCall? preparation = null;
                if (action is Expr.Sequence
                    {
                        Items: [Expr.Match { Pattern: Pattern.Variable bound, Value: Expr.Call prepare }, Expr.Call next]
                    })
                {
                    if (bound.Name == VariableScopeNames.Wildcard || parameters.Contains(bound.Name, StringComparer.Ordinal)
                        || prepare is not { Module: null, DynamicRemote: false, Arguments: [Expr.Variable input] }
                        || input.Name != value.Name || ReadEntryCall(prepare, parameters) is not { Module: null } call)
                    {
                        return false;
                    }

                    preparation = call;
                    tailInput = bound.Name;
                    tailParameters = [tailInput];
                    action = next;
                }

                if (value.Name == VariableScopeNames.Wildcard
                    || action is not Expr.Call
                    {
                        Module: null, DynamicRemote: false, Arguments: [Expr.Variable usingInput, var options]
                    } local || usingInput.Name != tailInput || !AttributeLiteral.TryNormalizeConstant(options, out _)
                    || ReadEntryCall(local, tailParameters) is not { Module: null } tail)
                {
                    return false;
                }

                branches.Add((key, tail, preparation, parameters));
            }

            var localBranches = branches.Where(branch => branch.Tail.Module is null).ToArray();
            if (exits != 1 || localBranches.Length != 2
                || (localBranches[0].Preparation is null) != (localBranches[1].Preparation is null)
                || !HasIndependentLeadingTailCalls(localBranches.Select(branch => branch.Tail)))
            {
                return false;
            }

            // The closed switch visits the ascending atom edges, then emits their reverse postorder.
            var atomOrder = Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
            int functionInfo = owner.nextLabel - 2;
            var labeled = branches.OrderByDescending(branch => branch.Key, atomOrder)
                .Select(branch => (Arm: branch, Label: Label()))
                .ToArray();
            owner.Op(SelectValue, XRegisterTag, 0);
            owner.Operand(LabelTag, functionInfo);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, labeled.Length * 2);
            foreach (var branch in labeled.Reverse())
            {
                owner.LiteralOperand(branch.Arm.Key);
                owner.Operand(LabelTag, branch.Label);
            }

            foreach (var branch in labeled)
            {
                Mark(branch.Label);
                if (branch.Arm.Preparation is { } producer)
                {
                    EmitEntryCallChain(producer, branch.Arm.Parameters, [(branch.Arm.Tail, null)]);
                }
                else
                {
                    EmitRegisterTailCall(branch.Arm.Tail);
                }
            }

            return true;
        }

        private bool TryEmitClosedRemoteTailDispatch(FunctionDefinition function)
        {
            if (function.Clauses is not
                [
                { Guard: null, Patterns: [Pattern.Literal { Value: Atom first }, ..], Body: var action } run,
                {
                    Guard: null,
                    Patterns: [Pattern.Literal { Value: Atom second }, ..],
                    Body: Expr.Call { Function: AutoImportNames.Exit, Arguments: [var reason] } exit
                } reject
                ]
                || first.Equals(second) || !owner.ResolvesErlangBif(exit)
                || !AttributeLiteral.TryNormalizeConstant(reason, out _))
            {
                return false;
            }

            var runParameters = run.Patterns.ToArray();
            var rejectedParameters = reject.Patterns.ToArray();
            runParameters[0] = new Pattern.Variable(VariableScopeNames.Wildcard);
            rejectedParameters[0] = runParameters[0];
            if (!HasUnconditionalParameters(run with { Patterns = runParameters })
                || !HasUnconditionalParameters(reject with { Patterns = rejectedParameters })
                || ReadEntryCall(exit, [], allowControlTransfer: true) is not { } failure)
            {
                return false;
            }

            var parameters = runParameters.Cast<Pattern.Variable>().Select(item => item.Name).ToArray();
            EntryCall? preparation = null;
            string[] tailInputs = parameters;
            if (action is Expr.Sequence
                {
                    Items: [Expr.Match { Pattern: Pattern.Variable bound, Value: Expr.Call prepare }, Expr.Call tail]
                })
            {
                if (bound.Name == VariableScopeNames.Wildcard || parameters.Contains(bound.Name, StringComparer.Ordinal)
                    || prepare.Arguments is not [Expr.Variable]
                    || ReadEntryCall(prepare, parameters) is not { Module: null, ConstructedArgument: null } producer)
                {
                    return false;
                }

                preparation = producer;
                tailInputs = [bound.Name];
                action = tail;
            }

            if (action is not Expr.Call { Arguments: [Expr.Variable, var options] } target
                || !AttributeLiteral.TryNormalizeConstant(options, out _)
                || ReadEntryCall(target, tailInputs) is not { Module: not (null or ErlangModuleNames.Module) } continuation)
            {
                return false;
            }

            int functionInfo = owner.nextLabel - 2;
            bool ascending = TermOrder.Compare(first, second, exact: true) < 0;
            int higher = Label();
            int lower = Label();
            owner.Op(SelectValue, XRegisterTag, 0);
            owner.Operand(LabelTag, functionInfo);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.LiteralOperand(ascending ? first : second);
            owner.Operand(LabelTag, lower);
            owner.LiteralOperand(ascending ? second : first);
            owner.Operand(LabelTag, higher);

            Mark(higher);
            EmitArm(rejected: ascending);
            Mark(lower);
            EmitArm(rejected: !ascending);

            return true;

            void EmitArm(bool rejected)
            {
                if (rejected)
                {
                    EmitRegisterTailCall(failure);
                }
                else if (preparation is null)
                {
                    EmitRegisterTailCall(continuation);
                }
                else
                {
                    EmitEntryCallChain(
                        preparation,
                        parameters,
                        [(continuation, null)]
                    );
                }
            }
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
