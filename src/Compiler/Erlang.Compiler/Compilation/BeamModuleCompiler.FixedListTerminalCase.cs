using static Erlang.Compiler.BeamEmissionFormat;
using AstPattern = Erlang.Pattern;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitPlannedFixedListCase(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || recursiveClosure is { SelfName: not null } or { GuardFailure: not null }
                || function.Clauses is not
                [
                {
                    Guard: null,
                    Body: Expr.Case { Value: Expr.Variable selector, Clauses.Count: >= 2 } decision
                } clause
                ]
                || !HasUnconditionalParameters(clause))
            {
                return false;
            }

            string[] inputs = clause.Patterns.Cast<AstPattern.Variable>().Select(pattern => pattern.Name).ToArray();
            int input = Array.IndexOf(inputs, selector.Name);
            if (input < 0)
            {
                return false;
            }

            int tail = inputs.Length + 1;
            var arms = new Dictionary<int, (TerminalCaseBranch Branch, bool RequiresFrame)>();
            foreach (Clause choice in decision.Clauses)
            {
                if (choice is not
                    {
                        Guard: null,
                        Patterns: [AstPattern.List { Items.Count: >= 2, Tail: null } list]
                    }
                    || inputs.Length + list.Items.Count + 1 >= FirstReservedXRegister)
                {
                    return false;
                }

                var names = inputs.ToHashSet(StringComparer.Ordinal);
                var parameters = new string[inputs.Length + list.Items.Count + 1];
                inputs.CopyTo(parameters, 0);
                parameters[tail] = VariableScopeNames.Wildcard;
                for (int index = 0; index < list.Items.Count; index++)
                {
                    if (list.Items[index] is not AstPattern.Variable field
                        || field.Name != VariableScopeNames.Wildcard && !names.Add(field.Name))
                    {
                        return false;
                    }

                    parameters[Head(index)] = field.Name;
                }

                if (LowerTerminalCaseArm(null, choice.Body, parameters) is not { } arm
                    || AllocateTerminalCaseRegisters(arm, parameters) is not { } allocated
                    || !arms.TryAdd(list.Items.Count, (allocated, arm.RequiresFrame)))
                {
                    return false;
                }
            }

            if (!arms.Values.Any(arm => arm.RequiresFrame))
            {
                return false;
            }

            int[] lengths = arms.Keys.OrderDescending().ToArray();
            int longest = lengths[0];
            int rejected = longest + 1;
            var blocks = new List<BeamFrameBlock>
            {
                new(
                    rejected,
                    [],
                    [],
                    RequiresFrame: false
                )
            };
            for (int depth = 0; depth < longest; depth++)
            {
                int exhausted = arms.ContainsKey(depth) ? NilCheck(depth) : rejected;
                blocks.Add(new(
                    depth,
                    [exhausted, depth + 1],
                    [],
                    RequiresFrame: false
                ));
            }

            foreach (int length in lengths)
            {
                blocks.Add(new(
                    NilCheck(length),
                    [rejected, Arm(length)],
                    [],
                    RequiresFrame: false
                ));
                blocks.Add(new(
                    Arm(length),
                    [],
                    [],
                    RequiresFrame: arms[length].RequiresFrame
                ));
            }

            if (BeamFramePlacement.Place(0, blocks) is not { } frames
                || frames.Owners[0] is not null || frames.Owners[rejected] is not null
                || lengths.Any(length => frames.Owners[NilCheck(length)] is not null))
            {
                return false;
            }

            var shorter = lengths.Skip(1).ToDictionary(length => length, _ => Label());
            int failure = Label();
            for (int depth = 0; depth < longest; depth++)
            {
                int carrier = depth == 0 ? input : tail;
                owner.Op(IsNonemptyList, LabelTag, shorter.GetValueOrDefault(depth, failure));
                owner.Operand(XRegisterTag, carrier);
                owner.Op(GetList, XRegisterTag, carrier);
                owner.Operand(XRegisterTag, Head(depth));
                owner.Operand(XRegisterTag, tail);
            }

            foreach (int length in lengths)
            {
                if (shorter.TryGetValue(length, out int shortArm))
                {
                    Mark(shortArm);
                }

                owner.Op(IsNil, LabelTag, failure);
                owner.Operand(XRegisterTag, tail);
                EmitTerminalCaseArm(arms[length].Branch, frames.Owners[Arm(length)] is not null);
            }

            Mark(failure);
            if (decision.SourceLine != 0)
            {
                var caseLocation = new BeamSourceLocation(decision.PreprocessorFile, decision.SourceLine);
                if (caseLocation != location)
                {
                    owner.EmitLine(caseLocation);
                }
            }

            owner.Op(CaseEnd, XRegisterTag, input);

            return true;

            int Head(int index) => index == 0 ? inputs.Length : tail + index;

            int NilCheck(int length) => length == longest ? longest : rejected + length;

            int Arm(int length) => rejected + longest + length;
        }
    }
}
