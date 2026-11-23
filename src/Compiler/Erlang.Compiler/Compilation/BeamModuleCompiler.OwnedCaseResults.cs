using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private readonly List<(int Position, int? Source, int Destination)> deferredOwnedCaseCopies = [];

    private sealed partial class GeneralEmitter
    {
        private bool TryBindOwnedCaseResult(Expr item, IEnumerable<Expr> continuation)
        {
            if (owner.ModuleOptimizationDisabled || owner.receiveScratchRegisters is not null
                || item is not Expr.Match
                {
                    Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } result,
                    Value: Expr.Case { Clauses.Count: >= 2 } decision
                } match
                || variables.ContainsKey(result.Name) || deferredPairMembers.ContainsKey(result.Name))
            {
                return false;
            }

            Expr[] rest = continuation.ToArray();
            if (rest is not
                [
                    Expr.Call { Arguments.Count: 0 } call,
                    Expr.Variable returned
                ]
                || returned.Name != result.Name || !TryResolveOrdinaryCallTarget(call, out string? module)
                || module == ErlangModuleNames.Module)
            {
                return false;
            }

            // The case value must survive the following call. Route each normal edge to the same slot.
            var copies = new List<(int Position, int? Source)>();
            EmitCase(
                decision,
                null,
                branchContinuation: [match with { Value = new Expr.Literal(Nil.Value) }, .. rest],
                ownedResult: copies
            );

            // Reserve the result after planning the case, when branch-only slots can be reused.
            int destination = Slot();
            foreach (var (position, source) in copies)
            {
                if (source != destination)
                {
                    owner.deferredOwnedCaseCopies.Add((position, source, destination));
                }
            }
            variables.Add(result.Name, destination);

            return true;
        }

        private void EmitOwnedCaseResult(
            Expr body,
            List<(int Position, int? Source)> copies,
            Expr[]? continuation
        )
        {
            if (body is Expr.Block block)
            {
                EmitOwnedCaseResult(block.Body, copies, continuation);

                return;
            }

            if (body is Expr.Sequence sequence)
            {
                for (int index = 0; index < sequence.Items.Count - 1; index++)
                {
                    var remaining = sequence.Items.Skip(index + 1);
                    EmitWithContinuation(
                        sequence.Items[index],
                        continuation is null ? remaining : remaining.Concat(continuation),
                        null,
                        returnValue: false,
                        reuseCallSlots: continuation is not null
                    );
                }

                EmitOwnedCaseResult(sequence.Items[^1], copies, continuation);

                return;
            }

            if (body is Expr.Case nested)
            {
                EmitCase(
                    nested,
                    null,
                    branchContinuation: continuation,
                    ownedResult: copies
                );

                return;
            }

            if (body is Expr.Variable variable && variables.TryGetValue(variable.Name, out int source))
            {
                RecordOwnedCaseResult(copies, source);

                return;
            }

            Expression(body);
            RecordOwnedCaseResult(copies);
        }

        private void RecordOwnedCaseResult(List<(int Position, int? Source)>? copies, int? source = null)
        {
            copies?.Add((checked((int)owner.instructions.Position), source));
        }
    }
}
