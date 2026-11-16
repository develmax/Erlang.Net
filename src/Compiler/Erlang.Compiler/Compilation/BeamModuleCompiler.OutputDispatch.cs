using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private bool IsDeclaredNative((string Name, int Arity) function) =>
        sourceModule?.DeclaredNifs?.Contains(function) == true;

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitOutputDispatch(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null, Patterns: [Pattern.Variable state, Pattern.Variable buffer, Pattern.Variable size,
                    Pattern.Variable pending, Pattern.Variable selection],
                Body: Expr.If { Clauses: [
                    { Guard: Expr.Binary { Operator: ErlangOperators.AndAlso, Left: Expr.Variable hasData,
                        Right: Expr.Binary { Operator: ErlangOperators.Greater,
                            Left: Expr.RecordAccess { Value: Expr.Variable record } watermark, Right: Expr.Variable buffered } },
                        Body: Expr.Call { Module: null, DynamicRemote: false } data },
                    { Guard: Expr.Binary { Operator: ErlangOperators.AndAlso,
                        Left: Expr.Binary { Operator: ErlangOperators.ExactEqual, Left: Expr.Variable selected,
                            Right: Expr.Literal { Value: Atom ready } },
                        Right: Expr.Binary { Operator: ErlangOperators.Greater, Left: Expr.Variable used,
                            Right: Expr.Literal { Value: Integer threshold } } },
                        Body: Expr.Call { Module: null, DynamicRemote: false } send },
                    { Guard: Expr.Literal { Value: Atom { Name: "true" } },
                        Body: Expr.Call { Module: null, DynamicRemote: false } wait }
                ] }
            }] || !DistinctBindings(state, buffer, size, pending, selection)
                || hasData.Name != pending.Name || record.Name != state.Name || buffered.Name != size.Name
                || selected.Name != selection.Name || used.Name != size.Name
                || !Forwards(data, state, buffer, size, selection) || !Forwards(send, state, buffer, size, pending)
                || wait.Arguments.Count != 6 || wait.Arguments[5] is not Expr.Literal { Value: Atom delay }
                || !wait.Arguments.Take(5).SequenceEqual(
                    new[] { state, buffer, size, pending, selection }.Select(value => new Expr.Variable(value.Name)))
                || !entries.TryGetValue((data.Function, 4), out int dataEntry)
                || !entries.TryGetValue((send.Function, 4), out int sendEntry)
                || !entries.TryGetValue((wait.Function, 6), out int waitEntry))
            {
                return false;
            }

            var key = (function.Name, function.Arity);
            bool knownRecord = ProvesOutputArgument(key, 0, watermark, []);
            bool knownNumber = ProvesOutputArgument(key, 2, null, []);
            int trySend = Label();
            int waitForData = Label();
            owner.Op(ExactEqualTest, LabelTag, trySend);
            owner.Operand(XRegisterTag, 3);
            owner.LiteralOperand(Term.A("true"));
            if (!knownRecord)
            {
                EmitDetailRecordTest(0, watermark.Arity, Term.A(watermark.Name), trySend);
            }
            owner.EmitTuplePart(0, watermark.Index, 5);
            owner.Op(IsLess, LabelTag, trySend);
            SizeOperand(2);
            if (knownRecord)
            {
                owner.EmitTypedRegister(5, IntegerTypeBits);
            }
            else
            {
                owner.Operand(XRegisterTag, 5);
            }
            owner.MoveRegister(XRegisterTag, 4, XRegisterTag, 3);
            TailCall(4, dataEntry);
            Mark(trySend);
            owner.Op(ExactEqualTest, LabelTag, waitForData);
            owner.Operand(XRegisterTag, 4);
            owner.LiteralOperand(ready);
            owner.Op(IsLess, LabelTag, waitForData);
            owner.LiteralOperand(threshold);
            SizeOperand(2);
            TailCall(4, sendEntry);
            Mark(waitForData);
            owner.EmitConstant(delay, 5);
            TailCall(6, waitEntry);

            return true;

            void SizeOperand(int register)
            {
                if (knownNumber)
                {
                    owner.EmitTypedRegister(register, NumberTypeBits);
                }
                else
                {
                    owner.Operand(XRegisterTag, register);
                }
            }

            void TailCall(int arity, int entry)
            {
                owner.Op(TailLocalCall, BeamWire.UnsignedTag, arity);
                owner.Operand(LabelTag, entry);
            }
        }

        private static bool Forwards(Expr.Call call, params Pattern.Variable[] arguments) =>
            call.Arguments.SequenceEqual(arguments.Select(argument => new Expr.Variable(argument.Name)));

        private bool ProvesOutputArgument(
            (string Name, int Arity) target, int index, Expr.RecordAccess? record,
            HashSet<((string Name, int Arity) Function, int Index)> active
        )
        {
            if (owner.exportedFunctions.Contains(target) || owner.IsDeclaredNative(target))
            {
                return false;
            }
            if (!active.Add((target, index)))
            {
                return true;
            }

            try
            {
                return localFunctions.Values.All(function => function.Clauses.All(clause =>
                    CheckCalls(clause.Body, function, clause, new(StringComparer.Ordinal))));
            }
            finally
            {
                active.Remove((target, index));
            }

            bool CheckCalls(Expr expression, FunctionDefinition function, Clause clause, Dictionary<string, Expr> bindings)
            {
                switch (expression)
                {
                    case Expr.FunctionReference reference:
                        return (reference.Name, reference.Arity) != target;
                    case Expr.Call { Module: null, DynamicRemote: false } call when (call.Function, call.Arguments.Count) == target:
                        return CheckValue(call.Arguments[index], function, clause, bindings)
                            && call.Arguments.All(item => CheckCalls(item, function, clause, bindings));
                    case Expr.Sequence sequence:
                        var local = new Dictionary<string, Expr>(bindings, StringComparer.Ordinal);
                        foreach (Expr item in sequence.Items)
                        {
                            if (!CheckCalls(item, function, clause, local))
                            {
                                return false;
                            }
                            if (item is Expr.Match { Pattern: Pattern.Variable variable } match)
                            {
                                local.TryAdd(variable.Name, match.Value);
                            }
                        }
                        return true;
                    case Expr.Fun or Expr.ListComprehension or Expr.BinaryComprehension or Expr.MapComprehension:
                        return !LocalFunctionEntries.Walk(expression).Any(item =>
                            item is Expr.FunctionReference reference && (reference.Name, reference.Arity) == target
                            || item is Expr.Call { Module: null, DynamicRemote: false } call
                                && (call.Function, call.Arguments.Count) == target);
                    default:
                        return LocalFunctionEntries.Children(expression).All(child => CheckCalls(child, function, clause, bindings));
                }
            }

            bool CheckValue(Expr value, FunctionDefinition function, Clause clause, Dictionary<string, Expr> bindings)
            {
                if (value is Expr.Variable variable)
                {
                    for (int parameter = 0; parameter < clause.Patterns.Count; parameter++)
                    {
                        Pattern pattern = clause.Patterns[parameter];
                        if (pattern is AliasPattern alias && TryReadPatternAlias(alias, out var bound, out _))
                        {
                            pattern = bound;
                        }
                        if (pattern is Pattern.Variable input && input.Name == variable.Name)
                        {
                            return ProvesOutputArgument((function.Name, function.Arity), parameter, record, active);
                        }
                    }
                    if (!bindings.TryGetValue(variable.Name, out value!))
                    {
                        return false;
                    }
                }

                return record is null
                    ? value is Expr.Literal { Value: Integer or FloatTerm }
                        or Expr.Binary { Operator: ErlangOperators.Plus or ErlangOperators.Minus or ErlangOperators.Multiply }
                        || value is Expr.Call { Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                            Function: AutoImportNames.IolistSize, Arguments.Count: 1 } length && owner.ResolvesErlangBif(length)
                    : value is Expr.RecordConstruction construction && construction.Definition.Name == record.Name
                        && RecordExpressions.Create(construction).Items is { } items && items.Count == record.Arity
                        && items[record.Index] is Expr.Literal { Value: Integer }
                            or Expr.Binary { Operator: ErlangOperators.ShiftRight or ErlangOperators.ShiftLeft };
            }
        }
    }
}
