using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTerminalCaseTuple(FunctionDefinition function, int functionInfo)
        {
            if (function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: var patterns,
                    Body: Expr.Sequence
                    {
                        Items: [Expr.Match
                        {
                            Pattern: Pattern.Variable result,
                            Value: Expr.Case { Value: Expr.Variable selector } decision
                        }, Expr.Tuple tuple]
                    }
                }]
                || decision.Clauses.Count < 2
                || result.Name == VariableScopeNames.Wildcard)
            {
                return false;
            }

            // beam_ssa_opt:tail_phi_cost permits up to three operations per
            // predecessor plus two. A dynamic put_tuple costs its arity plus one.
            int branchCount = decision.Clauses.Count;
            if ((tuple.Items.Count + 1L) * branchCount > 3L * branchCount + 2)
            {
                return false;
            }

            if (ReadTerminalCaseParameters(patterns, out int tupleInput) is not { } parameters
                || Array.IndexOf(parameters, selector.Name) is not (>= 0 and var selectorRegister)
                || Array.IndexOf(parameters, result.Name) >= 0)
            {
                return false;
            }

            var operands = new List<(int? Register, Term? Constant)>();
            int resultUses = 0;
            foreach (var item in tuple.Items)
            {
                switch (item)
                {
                    case Expr.Variable variable when variable.Name == result.Name:
                        resultUses++;
                        operands.Add((null, null));
                        break;
                    case Expr.Variable variable when Array.IndexOf(parameters, variable.Name) is >= 0 and var register:
                        operands.Add((register, null));
                        break;
                    default:
                        if (!AttributeLiteral.TryNormalizeConstant(item, out Term constant))
                        {
                            return false;
                        }

                        operands.Add((null, constant));
                        break;
                }
            }

            if (resultUses != 1)
            {
                return false;
            }

            var branches = new List<(Atom Key, EntryCall Call, bool DoesNotReturn)>();
            var keys = new HashSet<Atom>();
            foreach (var clause in decision.Clauses)
            {
                if (clause is not { Guard: null, Patterns: [Pattern.Literal { Value: Atom key }], Body: Expr.Call call }
                    || !keys.Add(key)
                    || ReadEntryCall(call, parameters) is not { } entry)
                {
                    return false;
                }

                branches.Add((key, entry, IsTerminalCaseNonreturningLocalCall(entry)));
            }

            branches.Sort((left, right) => TermOrder.Compare(right.Key, left.Key, exact: true));
            var targets = branches.Select(branch => (branch.Key, branch.Call, branch.DoesNotReturn, Label: Label())).ToArray();
            int failure = Label();
            if (tupleInput >= 0)
            {
                EmitTupleShapeTest(
                    XRegisterTag,
                    tupleInput,
                    2,
                    functionInfo
                );
                owner.EmitTuplePart(tupleInput, 0, function.Arity);
                owner.EmitTuplePart(tupleInput, 1, tupleInput);
            }

            owner.EmitMembershipSelect(
                selectorRegister,
                failure,
                targets.Reverse().Select(target => ((Term)target.Key, target.Label))
            );
            int[] saved = operands.Select(operand => operand.Register)
                .OfType<int>().Where(register => register != selectorRegister).Distinct().Order().ToArray();
            int savedLive = saved.Length == 0 ? 0 : saved[^1] + 1;
            foreach (var (key, call, doesNotReturn, target) in targets)
            {
                Mark(target);
                if (doesNotReturn)
                {
                    EmitEntryCallArguments(call);
                    EmitRegisterTailTarget(call.Call, call.Call.Arguments.Count);
                    continue;
                }

                EmitFrameAllocation(Math.Max(savedLive, EntryCallLiveCount(call, parameters)), saved.Length);
                for (int index = 0; index < saved.Length; index++)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        saved[index],
                        YRegisterTag,
                        index
                    );
                }

                EmitEntryCall(call);
                owner.Op(TestHeap, BeamWire.UnsignedTag, tuple.Items.Count + 1);
                owner.Operand(BeamWire.UnsignedTag, 1);
                owner.EmitTupleHeader(XRegisterTag, tuple.Items.Count, 0);
                foreach (var (register, constant) in operands)
                {
                    if (register == selectorRegister)
                    {
                        owner.LiteralOperand(key);
                    }
                    else if (register is int existing)
                    {
                        owner.Operand(YRegisterTag, Array.IndexOf(saved, existing));
                    }
                    else if (constant is not null)
                    {
                        owner.LiteralOperand(constant);
                    }
                    else
                    {
                        owner.Operand(XRegisterTag, 0);
                    }
                }

                owner.EmitFrameReturn(saved.Length);
            }

            Mark(failure);
            owner.EmitLine(new(location.File, decision.SourceLine == 0 ? location.Line : decision.SourceLine));
            owner.Op(CaseEnd, XRegisterTag, selectorRegister);

            return true;
        }

        private static string[]? ReadTerminalCaseParameters(IReadOnlyList<Pattern> patterns, out int tupleInput)
        {
            tupleInput = -1;
            var names = new List<string>();
            string? firstField = null;
            foreach (var pattern in patterns)
            {
                switch (pattern)
                {
                    case Pattern.Variable variable:
                        names.Add(variable.Name);
                        break;
                    case Pattern.Tuple { Items: [Pattern.Variable first, Pattern.Variable second] } when tupleInput < 0:
                        tupleInput = names.Count;
                        firstField = first.Name;
                        names.Add(second.Name);
                        break;
                    default:
                        return null;
                }
            }

            if (firstField is not null)
            {
                names.Add(firstField);
            }

            return names.Count <= FirstReservedXRegister && !names.Contains(VariableScopeNames.Wildcard)
                && names.Distinct(StringComparer.Ordinal).Count() == names.Count ? names.ToArray() : null;
        }

        // Match a statically resolved, unconditional raising body. The caller
        // has no reachable constructor on that edge, so it needs no frame.
        private bool IsTerminalCaseNonreturningLocalCall(EntryCall entry) =>
            entry.Module is null
            && localFunctions.TryGetValue((entry.Call.Function, entry.Call.Arguments.Count), out var target)
            && target.Clauses is [
                {
                    Guard: null, Body: Expr.Call
                    {
                        Module: null or ErlangModuleNames.Module, DynamicRemote: false, Arguments.Count: 1,
                        Function: AutoImportNames.Error
                    } transfer
                }]
            && owner.ResolvesErlangBif(transfer);
    }
}
