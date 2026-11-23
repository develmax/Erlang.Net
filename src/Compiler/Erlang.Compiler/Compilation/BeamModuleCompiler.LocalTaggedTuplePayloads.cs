using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private const int LocalPayloadProofDepth = 16;

    private byte[]? RemoveProvenLocalPayloadTupleChecks(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsTaggedTuple)
            || sourceModule is not { DeclaredFunctions: var functions }
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream();
        Dictionary<int, FunctionDefinition>? targets = null;
        Dictionary<int, string>? atomNames = null;
        int copied = 0;
        for (int index = 0; index + 5 < body.Count; index++)
        {
            if (body[index] is not { Definition.Opcode: LocalCall, Operands: [_, var entry] }
                || body[index + 1] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [var failure, var result, { Value: var outerArity }, { Kind: BeamOperandParameters.Atom } tag]
                }
                || !IsBareResultRegister(result) || outerArity != 2
                || body[index + 2] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var carrier, { Value.IsOne: true }, var payload]
                }
                || !SameProjectionRegister(carrier, result)
                || body[index + 3] is not { Definition.Opcode: IsTuple, Operands: [var tupleFailure, var tested] }
                || tupleFailure != failure || !SameProjectionRegister(payload, tested)
                || body[index + 4] is not { Definition.Opcode: TestArity, Operands: [var arityFailure, var checkedPayload, var arity] }
                || arityFailure != failure || !SameProjectionRegister(payload, checkedPayload)
                || arity.Value.Sign < 0 || arity.Value > int.MaxValue)
            {
                continue;
            }

            atomNames ??= atoms.Where(atom => MapEmittedAtom(atom.Value) != 0)
                .ToDictionary(atom => MapEmittedAtom(atom.Value), atom => atom.Key);
            targets ??= ReadLocalPayloadTargets(body, atomNames, functions);
            if (!targets.TryGetValue((int)entry.Value, out var target)
                || !atomNames.TryGetValue((int)tag.Value, out var name)
                || !HasProvenLocalTaggedTuplePayload(
                    target,
                    name,
                    (int)arity.Value,
                    functions
                ))
            {
                continue;
            }

            // The constructor and all successful local return paths establish
            // the shape of this selected field. The outer tagged check remains.
            output.Write(code.Span[copied..body[index + 3].Offset]);
            copied = body[index + 5].Offset;
            index += 4;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        static bool IsBareResultRegister(BeamOperand register)
            => register is { Kind: BeamOperandParameters.X, Value.IsZero: true };
    }

    private static Dictionary<int, FunctionDefinition> ReadLocalPayloadTargets(
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyDictionary<int, string> atomsByIndex,
        IReadOnlyList<FunctionDefinition> functions
    )
    {
        var definitions = functions.ToDictionary(function => (function.Name, function.Arity));
        var targets = new Dictionary<int, FunctionDefinition>();
        for (int index = 0; index + 1 < body.Count; index++)
        {
            if (body[index] is { Definition.Opcode: FunctionInfo, Operands: [_, var name, var arity] }
                && atomsByIndex.TryGetValue((int)name.Value, out var function)
                && definitions.TryGetValue((function, (int)arity.Value), out var target)
                && body[index + 1] is { Definition.Opcode: Label, Operands: [var entry] })
            {
                targets.Add((int)entry.Value, target);
            }
        }

        return targets;
    }

    private bool HasProvenLocalTaggedTuplePayload(
        FunctionDefinition target,
        string tag,
        int arity,
        IReadOnlyList<FunctionDefinition> functions
    )
    {
        var definitions = functions.ToDictionary(function => (function.Name, function.Arity));
        var active = new HashSet<((string Name, int Arity) Function, bool Tagged)>();
        var closedInputs = new Dictionary<((string Name, int Arity) Function, int Argument), bool>();

        return ProveFunction(target, tagged: true);

        bool ProveFunction(FunctionDefinition function, bool tagged)
        {
            var signature = (function.Name, function.Arity);
            var key = (signature, tagged);
            if (IsDeclaredNative(signature) || active.Count >= LocalPayloadProofDepth)
            {
                return false;
            }
            if (!active.Add(key))
            {
                // A returning recursive path has to reach one of the
                // non-recursive results included in the same proof.
                return true;
            }

            bool valid = function.Clauses.Count != 0 && function.Clauses.All(clause =>
                ProveExpression(
                    clause.Body,
                    function,
                    clause,
                    tagged
                ));
            active.Remove(key);

            return valid;
        }

        bool ProveExpression(
            Expr expression,
            FunctionDefinition function,
            Clause clause,
            bool tagged
        )
        {
            switch (expression)
            {
                case Expr.Block block:
                    return ProveExpression(
                        block.Body,
                        function,
                        clause,
                        tagged
                    );
                case Expr.Sequence { Items.Count: > 0 } sequence:
                    return ProveExpression(
                        sequence.Items[^1],
                        function,
                        clause,
                        tagged
                    );
                case Expr.Case decision:
                    return decision.Clauses.Count != 0
                        && decision.Clauses.All(arm => ProveExpression(
                            arm.Body,
                            function,
                            clause,
                            tagged
                        ));
                case Expr.If decision:
                    return decision.Clauses.Count != 0
                        && decision.Clauses.All(arm => ProveExpression(
                            arm.Body,
                            function,
                            clause,
                            tagged
                        ));
                case Expr.Call { Module: null, DynamicRemote: false } call
                    when definitions.TryGetValue((call.Function, call.Arguments.Count), out var callee):
                    return ProveFunction(callee, tagged);
            }

            if (tagged)
            {
                if (expression is Expr.Tuple { Items: [Expr.Literal { Value: Atom kind }, var payload] })
                {
                    return kind.Name != tag || ProveExpression(
                        payload,
                        function,
                        clause,
                        tagged: false
                    );
                }

                if (expression is Expr.Literal { Value: TupleTerm { Items: [Atom literalTag, var literalPayload] } })
                {
                    return literalTag.Name != tag || literalPayload is TupleTerm nested && nested.Items.Count == arity;
                }

                return expression is Expr.Literal { Value: not TupleTerm }
                    or Expr.Tuple { Items.Count: not 2 };
            }

            if (KnownTuple(expression))
            {
                return true;
            }

            if (expression is not Expr.Variable variable)
            {
                return false;
            }

            for (int index = 0; index < clause.Patterns.Count; index++)
            {
                Pattern pattern = clause.Patterns[index];
                if (pattern is AliasPattern alias && (MatchesTupleAlias(alias.Left, alias.Right, variable.Name)
                    || MatchesTupleAlias(alias.Right, alias.Left, variable.Name)))
                {
                    return true;
                }

                if (pattern is not Pattern.Variable input || input.Name != variable.Name)
                {
                    continue;
                }

                var key = ((function.Name, function.Arity), index);
                if (!closedInputs.TryGetValue(key, out bool proven))
                {
                    proven = HasClosedEntryArgument(
                        function,
                        index,
                        functions,
                        KnownTuple
                    );
                    closedInputs.Add(key, proven);
                }

                return proven;
            }

            return false;
        }

        bool MatchesTupleAlias(Pattern binding, Pattern shape, string name)
            => binding is Pattern.Variable variable && variable.Name == name
                && shape is Pattern.Tuple tuple && tuple.Items.Count == arity;

        bool KnownTuple(Expr input) => input is Expr.Tuple tuple && tuple.Items.Count == arity
            || input is Expr.Literal { Value: TupleTerm literal } && literal.Items.Count == arity;
    }
}
