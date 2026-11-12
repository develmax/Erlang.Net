using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitRecordGuardedListCase(FunctionDefinition function, int functionInfo)
        {
            if (function.Arity + 1 >= FirstReservedXRegister || function.Clauses is not
                [
                {
                    Guard: { } guard,
                    Body: Expr.Case
                    {
                        Value: Expr.RecordAccess { Value: Expr.Variable source } field,
                        Clauses:
                            [
                            { Guard: null, Patterns: [Pattern.Literal key] } selected,
                            {
                                Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                                Body: Expr.Call other
                            }
                            ]
                    }
                } first,
                { Guard: null, Body: Expr.Call sibling } second,
                { Guard: null } empty
                ]
                || owner.ReadRecordTest(guard) is not { Value: Expr.Variable tested } record
                || source.Name != tested.Name || field.Name != record.Name.Name || field.Arity != record.Arity)
            {
                return false;
            }

            int input = Array.FindIndex(empty.Patterns.ToArray(), pattern => ListEntryTest(pattern) == IsNil);
            int headRegister = function.Arity;
            if (input < 0
                || !ReadRegisters(
                    first,
                    isEmpty: false,
                    out var guarded,
                    out string? head
                ) || head != source.Name
                || !ReadRegisters(
                    second,
                    isEmpty: false,
                    out var fallback,
                    out _
                )
                || !ReadRegisters(
                    empty,
                    isEmpty: true,
                    out var emptyRegisters,
                    out _
                )
                || ReadBranchCalls(selected.Body, guarded) is not { } branch
                || ReadBranchCalls(empty.Body, emptyRegisters) is not { } emptyBranch
                || ReadConstructedTail(other, guarded) is not { } otherTail
                || ReadConstructedTail(sibling, fallback) is not { } siblingTail
                || otherTail.Call.Module != siblingTail.Call.Module || otherTail.Call.Function != siblingTail.Call.Function
                || otherTail.Call.Arguments.Count != siblingTail.Call.Arguments.Count
                || !SameListConstructor(otherTail.Arguments, siblingTail.Arguments))
            {
                return false;
            }

            int tail = Label();
            int nil = Label();
            owner.EmitNonemptyList(input, nil);
            owner.EmitListParts(
                input,
                XRegisterTag,
                headRegister,
                XRegisterTag,
                input
            );
            EmitDetailRecordTest(
                headRegister,
                record.Arity,
                record.Name,
                tail
            );
            owner.EmitTuplePart(headRegister, field.Index, headRegister + 1);
            owner.Op(ExactEqualTest, LabelTag, tail);
            owner.Operand(XRegisterTag, headRegister + 1);
            owner.LiteralOperand(key.Value);
            EmitSavedCallSequence(branch);

            Mark(tail);
            EmitCallArgumentConstructions(siblingTail.Arguments, -1);
            EmitRegisterTailTarget(siblingTail.Call, siblingTail.Call.Arguments.Count);

            Mark(nil);
            owner.EmitNilTest(XRegisterTag, input, functionInfo);
            EmitSavedCallSequence(emptyBranch);

            return true;

            bool ReadRegisters(
                Clause clause,
                bool isEmpty,
                out Dictionary<string, int> registers,
                out string? head
            )
            {
                registers = new(StringComparer.Ordinal);
                head = null;
                for (int register = 0; register < function.Arity; register++)
                {
                    Pattern pattern = clause.Patterns[register];
                    if (register == input)
                    {
                        if (isEmpty)
                        {
                            continue;
                        }

                        if (pattern is not Pattern.List { Items: [Pattern.Variable firstItem], Tail: Pattern.Variable rest })
                        {
                            return false;
                        }

                        head = firstItem.Name;
                        if (head != VariableScopeNames.Wildcard && !registers.TryAdd(head, headRegister))
                        {
                            return false;
                        }

                        pattern = rest;
                    }

                    if (pattern is not Pattern.Variable variable
                        || variable.Name != VariableScopeNames.Wildcard && !registers.TryAdd(variable.Name, register))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private SavedCallSequence? ReadBranchCalls(Expr body, Dictionary<string, int> registers)
        {
            IReadOnlyList<Expr> statements = body is Expr.Sequence sequence ? sequence.Items : [body];
            if (statements.LastOrDefault() is not Expr.Call)
            {
                return null;
            }

            var parameters = Enumerable.Repeat(VariableScopeNames.Wildcard, registers.Values.DefaultIfEmpty(-1).Max() + 1).ToArray();
            foreach (var (name, register) in registers)
            {
                parameters[register] = name;
            }

            return ReadStraightCalls(FlattenCallArguments(statements), parameters) is { Length: >= 2 } calls
                ? ReadSavedCallSequence(
                    calls,
                    parameters,
                    extraction: null,
                    allowLinear: true
                ) : null;
        }

        private (Expr.Call Call, CallArgumentConstructions Arguments)? ReadConstructedTail(
            Expr.Call call,
            Dictionary<string, int> registers
        )
        {
            var signature = call with { Arguments = call.Arguments.Select(_ => (Expr)new Expr.Literal(Nil.Value)).ToArray() };
            if (!TryResolveOrdinaryCall(signature, out string? module)
                || ReadCallArgumentConstructions(
                    call.Arguments,
                    -1,
                    new Dictionary<string, int>(),
                    inputs: registers
                ) is not { } arguments)
            {
                return null;
            }

            return (call with { Module = module }, arguments);
        }
    }
}
