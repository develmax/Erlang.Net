using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTerminalNegatedBooleanCase(Expr body)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || owner.receiveScratchRegisters is not null || recursiveClosure is not null)
            {
                return false;
            }

            Expr.Sequence? sequence = body as Expr.Sequence;
            Pattern.Variable? pending = null;
            if (sequence is not null)
            {
                if (sequence.Items is not
                    [
                        Expr.Match
                    {
                        Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } binding,
                        Value: Expr.Call prefix
                    },
                        Expr.Case
                    ] || HasBinding(binding.Name) || prefix.DynamicRemote
                    || !prefix.Arguments.All(argument => CanRead(argument, added: null)))
                {
                    return false;
                }

                pending = binding;
                body = sequence.Items[^1];
            }

            if (body is not Expr.Case
                {
                    Value: Expr.Binary
                    {
                        Operator: ErlangOperators.AndAlso,
                        Left: Expr.Unary
                        {
                            Operator: ErlangOperators.Not,
                            Operand: Expr.Call { DynamicRemote: false } producer
                        } negation,
                        Right: var right
                    }
                } decision || negation.SourceLine <= 0 || !IsBooleanCase(decision)
                || !IsOrdinaryProducer(producer) || HasProvenBooleanResult(producer)
                || !producer.Arguments.All(argument => CanRead(argument, pending?.Name) || argument is Expr.Tuple
                {
                    Items: [var first, var second]
                } && CanRead(first, pending?.Name) && CanRead(second, pending?.Name)))
            {
                return false;
            }

            var predicates = new List<Expr.Call>();
            if (!ReadDisjunction(right) || predicates.Count < 2)
            {
                return false;
            }

            Expr success = decision.Clauses.Single(clause =>
                clause.Patterns is [Pattern.Literal { Value: Atom { Name: "true" } }]).Body;
            Expr rejected = decision.Clauses.Single(clause =>
                clause.Patterns is [Pattern.Literal { Value: Atom { Name: "false" } }]).Body;
            if (!CanReturn(success) || !CanReturn(rejected))
            {
                return false;
            }

            if (sequence is not null)
            {
                DiscardSequencePrefix(
                    sequence.Items,
                    guardFailure: null,
                    reuseCallSlots: true,
                    rememberRecords: true
                );
            }

            Expression(producer);
            int evaluate = Label();
            int succeed = Label();
            int reject = Label();
            int badArgument = Label();
            owner.EmitMembershipSelect(
                0,
                badArgument,
                [(new Atom("false"), evaluate), (new Atom("true"), reject)]
            );

            // Keep roots used on either return edge, including stack aliases.
            var remaining = ReadSlots([success, rejected]).ToHashSet();
            int[][] retired = new int[predicates.Count][];
            for (int index = predicates.Count - 1; index >= 0; index--)
            {
                retired[index] = ReadSlots(predicates[index].Arguments).Where(remaining.Add).Order().ToArray();
            }

            Mark(evaluate);
            for (int index = 0; index < predicates.Count; index++)
            {
                Expr.Call predicate = predicates[index];
                var argumentLocation = LoadCallArguments(predicate.Arguments, null, orderSimpleOperands: true);
                WriteFrameInitialization(owner.instructions, retired[index]);
                owner.EmitCallLine(predicate, argumentLocation);
                Call(predicate.Module, predicate.Function, predicate.Arguments.Count);
                bool last = index == predicates.Count - 1;
                owner.Op(last ? ExactEqualTest : ExactNotEqualTest, LabelTag, last ? reject : succeed);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(AtomTag, owner.Intern("true"));
            }

            Mark(succeed);
            Expression(success);
            owner.EmitFrameReturn(frameSize);

            Mark(reject);
            Expression(rejected);
            owner.EmitFrameReturn(frameSize);

            // sys_core_fold inverts the Boolean literals of the negated case
            // and preserves the negation's own failure annotation.
            Mark(badArgument);
            owner.EmitConstant(new Atom(ErlangErrorReasons.BadArgument), 0);
            owner.EmitLine(new(producer.PreprocessorFile ?? location.File, negation.SourceLine));
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, "error", 1));
            owner.Operand(BeamWire.UnsignedTag, frameSize);

            return true;

            bool HasBinding(string name) => variables.ContainsKey(name) || deferredPairMembers.ContainsKey(name);

            bool CanRead(Expr expression, string? added) =>
                AttributeLiteral.TryNormalizeConstant(expression, out _)
                || expression is Expr.Variable variable
                && (variable.Name == added || variables.ContainsKey(variable.Name)
                    && !deferredPairMembers.ContainsKey(variable.Name));

            bool IsOrdinaryProducer(Expr.Call call) => call.Module is not null
                ? call.Module != ErlangModuleNames.Module
                : entries.ContainsKey((call.Function, call.Arguments.Count));

            bool ReadDisjunction(Expr expression)
            {
                if (expression is Expr.Binary { Operator: ErlangOperators.OrElse } either)
                {
                    return ReadDisjunction(either.Left) && ReadDisjunction(either.Right);
                }

                if (expression is not Expr.Call call || !HasProvenBooleanResult(call)
                    || !call.Arguments.All(argument => CanRead(argument, pending?.Name)))
                {
                    return false;
                }

                predicates.Add(call);

                return true;
            }

            IEnumerable<int> ReadSlots(IEnumerable<Expr> expressions) => expressions
                .SelectMany(LocalFunctionEntries.Walk)
                .OfType<Expr.Variable>()
                .Select(variable => variables[variable.Name]);

            bool CanReturn(Expr expression) => CanRead(expression, pending?.Name)
                || expression is Expr.List { Items: [var head], Tail: var tail }
                && CanRead(head, pending?.Name) && (tail is null || CanRead(tail, pending?.Name));
        }
    }
}
