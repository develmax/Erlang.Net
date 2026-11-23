using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private const string FirstTailArgument = "\0guarded_tuple_first_argument";
        private const string SecondTailArgument = "\0guarded_tuple_second_argument";

        private sealed record GuardedTupleTail(
            EntryCall Call,
            CallArgumentConstructions? Construction,
            EntryCall? FirstArgument,
            RegisterArithmeticStep? SecondArgument
        );

        private bool TryEmitGuardedTupleContinuation(Clause enclosing, string[] parameters, IEnumerable<string> sourceOrder)
        {
            if (enclosing.Body is not Expr.Case
                {
                    Value: Expr.Call selector,
                    Clauses: [var first, var second, var fallback]
                } decision
                || first is not
                {
                    Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable payload] }],
                    Guard: Expr.Binary { Operator: ErlangOperators.NumericEqual or ErlangOperators.ExactEqual } comparison,
                    Body: Expr.Call guarded
                }
                || second is not
                {
                    Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom repeated }, Pattern.Variable repeatedPayload] }],
                    Guard: null,
                    Body: Expr.Call continued
                }
                || tag.Name != repeated.Name
                || payload.Name == VariableScopeNames.Wildcard || repeatedPayload.Name == VariableScopeNames.Wildcard
                || parameters.Contains(payload.Name, StringComparer.Ordinal)
                || parameters.Contains(repeatedPayload.Name, StringComparer.Ordinal)
                || fallback is not
                {
                    Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                    Guard: null,
                    Body: Expr.Tuple failureTuple
                }
                || (ReadNilGuard(comparison.Left, comparison.Right) ?? ReadNilGuard(comparison.Right, comparison.Left))
                    is not { } checkedName
                || ReadEntryCall(selector, parameters) is not { } producer
                || ReadUsedEntryParameters(enclosing with { Body = decision with { Value = new Expr.Literal(Nil.Value) } })
                    is not { } used)
            {
                return false;
            }

            int[] savedRegisters = sourceOrder.Reverse().Where(used.Contains).Select(name => Array.IndexOf(parameters, name)).ToArray();
            var saved = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int slot = 0; slot < savedRegisters.Length; slot++)
            {
                saved.Add(parameters[savedRegisters[slot]], slot);
            }

            if (!saved.TryGetValue(checkedName, out int guardSlot))
            {
                return false;
            }

            // The producer's different-tag path still uses the incoming bindings. Only the
            // successful tagged path may overwrite a slot dead in both guard outcomes.
            var acceptedReads = LocalFunctionEntries.Walk(guarded).Concat(LocalFunctionEntries.Walk(continued))
                .OfType<Expr.Variable>().Select(variable => variable.Name).ToHashSet(StringComparer.Ordinal);
            int reuse = Array.FindLastIndex(savedRegisters, register =>
                parameters[register] != checkedName && !acceptedReads.Contains(parameters[register]));
            if (reuse < 0)
            {
                return false;
            }

            string replaced = parameters[savedRegisters[reuse]];
            if (ReadGuardedTupleTail(guarded, BindPayload(payload.Name)) is not { } empty
                || ReadGuardedTupleTail(continued, BindPayload(repeatedPayload.Name)) is not { } nonempty
                || ReadCallArgumentConstructions(
                    [failureTuple],
                    -1,
                    saved,
                    inputs: new Dictionary<string, int>()
                )
                    is not { } failure)
            {
                return false;
            }

            int mismatch = Label();
            int differentGuard = Label();
            int frame = savedRegisters.Length;
            EmitFrameAllocation(parameters.Length, frame);
            for (int slot = 0; slot < frame; slot++)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    savedRegisters[slot],
                    YRegisterTag,
                    slot
                );
            }

            EmitEntryCall(producer);
            owner.EmitRecordTest(
                mismatch,
                XRegisterTag,
                0,
                tag,
                2
            );
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(YRegisterTag, reuse);
            owner.Op(IsNil, LabelTag, differentGuard);
            owner.Operand(YRegisterTag, guardSlot);
            var previous = CallLocation(selector, location);
            EmitGuardedTupleTail(empty, previous, frame);

            Mark(differentGuard);
            EmitGuardedTupleTail(nonempty, previous, frame);

            Mark(mismatch);
            EmitCallArgumentConstructions(failure, -1);
            owner.EmitFrameReturn(frame);

            return true;

            string? ReadNilGuard(Expr value, Expr constant) =>
                value is Expr.Variable variable && parameters.Contains(variable.Name, StringComparer.Ordinal)
                    && AttributeLiteral.TryNormalizeConstant(constant, out Term literal) && literal is Nil
                    ? variable.Name
                    : null;

            Dictionary<string, int> BindPayload(string name)
            {
                var branch = new Dictionary<string, int>(saved, StringComparer.Ordinal);
                branch.Remove(replaced);
                branch.Add(name, reuse);

                return branch;
            }
        }

        private GuardedTupleTail? ReadGuardedTupleTail(Expr.Call call, Dictionary<string, int> saved)
        {
            string[] names = Enumerable.Repeat(VariableScopeNames.Wildcard, saved.Values.Max() + 1).ToArray();
            foreach (var (name, slot) in saved)
            {
                names[slot] = name;
            }

            if (ReadEntryCall(call, [], names) is { } ordinary)
            {
                return new GuardedTupleTail(
                    ordinary,
                    null,
                    null,
                    null
                );
            }

            if (!TryResolveOrdinaryCallTarget(call, out string? module))
            {
                return null;
            }

            var target = new EntryCall(
                call,
                module,
                [],
                []
            );
            if (ReadCallArgumentConstructions(
                call.Arguments,
                -1,
                saved,
                inputs: new Dictionary<string, int>()
            )
                is { } construction)
            {
                return new GuardedTupleTail(
                    target,
                    construction,
                    null,
                    null
                );
            }

            if (call.Arguments is not [Expr.Call subcall, Expr.Binary arithmetic, ..]
                || ReadEntryCall(subcall, [], names) is not { } entry
                || !IsRegisterArithmeticOperator(arithmetic.Operator)
                || ReadOperand(arithmetic.Left) is not { } left
                || ReadOperand(arithmetic.Right) is not { } right)
            {
                return null;
            }

            var rewritten = call.Arguments.ToArray();
            rewritten[0] = new Expr.Variable(FirstTailArgument);
            rewritten[1] = new Expr.Variable(SecondTailArgument);
            var computed = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [FirstTailArgument] = 0,
                [SecondTailArgument] = 1
            };
            if (ReadCallArgumentConstructions(
                rewritten,
                -1,
                saved,
                inputs: computed
            ) is not { } arguments)
            {
                return null;
            }

            return new GuardedTupleTail(
                target,
                arguments,
                entry,
                new RegisterArithmeticStep(
                    arithmetic,
                    left,
                    right,
                    Live: 1,
                    Destination: 1
                )
            );

            RegisterArithmeticOperand? ReadOperand(Expr expression)
            {
                if (expression is Expr.Variable variable && saved.TryGetValue(variable.Name, out int slot))
                {
                    return new RegisterArithmeticOperand(slot, RegisterTag: YRegisterTag);
                }

                return AttributeLiteral.TryNormalizeConstant(expression, out Term constant) && constant is Integer or FloatTerm
                    ? new RegisterArithmeticOperand(0, constant)
                    : null;
            }
        }

        private void EmitGuardedTupleTail(GuardedTupleTail tail, BeamSourceLocation previous, int frame)
        {
            if (tail.FirstArgument is { } first && tail.SecondArgument is { } arithmetic)
            {
                owner.EmitOrderedTailMoves(first.Moves, first.Constants, first.SavedInputs);
                owner.EmitCallLine(first.Call, previous);
                Call(first.Module, first.Call.Function, first.Call.Arguments.Count);
                previous = CallLocation(first.Call, previous);
                owner.EmitArithmeticLine(arithmetic.Expression.SourceLine, ref previous);
                owner.Op(GcBifTwo, LabelTag, 0);
                owner.Operand(BeamWire.UnsignedTag, arithmetic.Live);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, arithmetic.Expression.Operator, 2));
                EmitRegisterArithmeticOperand(arithmetic.Left);
                EmitRegisterArithmeticOperand(arithmetic.Right);
                owner.Operand(XRegisterTag, arithmetic.Destination);
            }

            if (tail.Construction is { } construction)
            {
                EmitCallArgumentConstructions(construction, -1);
            }
            else
            {
                owner.EmitOrderedTailMoves(tail.Call.Moves, tail.Call.Constants, tail.Call.SavedInputs);
            }

            EmitPreparedFrameTail(tail.Call, previous, frame);
        }
    }
}
