namespace Erlang;

internal static class ErlangBasicRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        void Add(string name, int arity, Func<ProcessContext, IReadOnlyList<Term>, Term> body) => registry.Register(
            ErlangModuleNames.Module,
            name,
            arity,
            (context, arguments) => ValueTask.FromResult(body(context, arguments))
        );
        foreach (string operation in new[] { ErlangOperators.Plus, ErlangOperators.Minus, ErlangOperators.Not, ErlangOperators.BitwiseNot })
            Add(operation, 1, (_, arguments) => ErlangOperatorValues.Unary(operation, arguments[0]));
        foreach (string operation in new[] { ErlangOperators.Plus, ErlangOperators.Minus, ErlangOperators.Multiply, ErlangOperators.Divide, ErlangOperators.IntegerDivide, ErlangOperators.Remainder, ErlangOperators.BitwiseAnd, ErlangOperators.BitwiseOr, ErlangOperators.BitwiseXor, ErlangOperators.ShiftLeft, ErlangOperators.ShiftRight, ErlangOperators.And, ErlangOperators.Or, ErlangOperators.Xor, ErlangOperators.NumericEqual, ErlangOperators.NumericNotEqual, ErlangOperators.ExactEqual, ErlangOperators.ExactNotEqual, ErlangOperators.Less, ErlangOperators.Greater, ErlangOperators.LessOrEqual, ErlangOperators.GreaterOrEqual, ErlangOperators.Append, ErlangOperators.SubtractList })
            Add(operation, 2, (context, arguments) => ErlangOperatorValues.Binary(
                operation,
                arguments[0],
                arguments[1],
                context
            ));
        foreach (string operation in new[] { ErlangBasicBifNames.Absolute, ErlangBasicBifNames.Float, ErlangBasicBifNames.Round, ErlangBasicBifNames.Truncate, ErlangBasicBifNames.Floor, ErlangBasicBifNames.Ceiling })
            Add(operation, 1, (_, arguments) => ErlangNumericBifs.Run(operation, arguments[0]));
        Add(ErlangBasicBifNames.Minimum, 2, (_, arguments) => arguments[0].CompareTo(arguments[1]) > 0 ? arguments[1] : arguments[0]);
        Add(ErlangBasicBifNames.Maximum, 2, (_, arguments) => arguments[0].CompareTo(arguments[1]) < 0 ? arguments[1] : arguments[0]);
        Add(
            ErlangBasicBifNames.IsBoolean,
            1,
            (_, arguments) => CoreModules.Boolean(arguments[0] is Atom { Name: ErlangBooleanAtoms.True or ErlangBooleanAtoms.False })
        );
        Add(ErlangBasicBifNames.IsFunction, 1, (_, arguments) => CoreModules.Boolean(arguments[0] is FunctionTerm));
        Add(
            ErlangBasicBifNames.IsFunction,
            2,
            (_, arguments) => arguments[1] is Integer number && number.Value >= 0 ? CoreModules.Boolean(arguments[0] is FunctionTerm function && function.Arity == number.Value) : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        Add(ErlangBasicBifNames.IsPort, 1, (_, arguments) => CoreModules.Boolean(arguments[0] is PortTerm));
        Add(ErlangBasicBifNames.IsReference, 1, (_, arguments) => CoreModules.Boolean(arguments[0] is ReferenceTerm));
        Add(
            ErlangModuleNames.IsInteger,
            3,
            (_, arguments) => arguments[1] is Integer minimum && arguments[2] is Integer maximum ? CoreModules.Boolean(arguments[0] is Integer number && number.Value >= minimum.Value && number.Value <= maximum.Value) : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        Add(
            ErlangBasicBifNames.Size,
            1,
            (_, arguments) => arguments[0] switch
 {
     TupleTerm tuple => Term.I(tuple.Items.Count),
     BitString bits => Term.I(bits.BitLength / 8),
     _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
 }
        );
        foreach (var (operation, arity) in new[] { (ErlangBasicBifNames.SetElement, 3), (ErlangBasicBifNames.AppendElement, 2), (ErlangBasicBifNames.InsertElement, 3), (ErlangBasicBifNames.DeleteElement, 2) })
            Add(operation, arity, (_, arguments) => ErlangTupleBifs.Change(operation, arguments));
        foreach (int arity in new[] { 2, 3 })
            Add(ErlangBasicBifNames.MakeTuple, arity, (_, arguments) => ErlangTupleBifs.Create(arguments));
        Add(
            ErlangBasicBifNames.TupleToList,
            1,
            (_, arguments) => arguments[0] is TupleTerm tuple ? Cons.From(tuple.Items) : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        Add(ErlangBasicBifNames.ListToTuple, 1, (_, arguments) => new TupleTerm(Cons.Items(arguments[0])));
        Add(ErlangBasicBifNames.Append, 2, (_, arguments) => Cons.From(Cons.Items(arguments[0]), arguments[1]));
        Add(ErlangBasicBifNames.Subtract, 2, (_, arguments) => ListBasicOperations.Subtract(arguments[0], arguments[1]));
        foreach (string name in new[] { ErlangBasicBifNames.Send, ErlangOperators.Send })
            registry.Register(
                ErlangModuleNames.Module,
                name,
                2,
                async (context, arguments) =>
            {
                await ErlangOperatorValues.TrySendAsync(context, arguments[0], arguments[1]);

                return arguments[1];
            }
            );
        Add(ErlangModuleNames.Get, 0, (context, _) => Cons.From(context.Dictionary.Select(entry => Term.Tuple(entry.Key, entry.Value))));
        Add(
            ErlangModuleNames.Erase,
            0,
            (context, _) =>
        {
            Term previous = Cons.From(context.Dictionary.Select(entry => Term.Tuple(entry.Key, entry.Value)));
            context.Dictionary.Clear();

            return previous;
        }
        );
        Add(ErlangBasicBifNames.GetKeys, 0, (context, _) => Cons.From(context.Dictionary.Keys));
        Add(
            ErlangBasicBifNames.GetKeys,
            1,
            (context, arguments) => Cons.From(context.Dictionary.Where(entry => entry.Value.Equals(arguments[0])).Select(entry => entry.Key))
        );
        registry.Register(
            ErlangModuleNames.Module,
            ErlangBasicBifNames.Apply,
            ErlangApplyLayout.ModuleApplyArity,
            ErlangApplyDispatch.Invoke
        );
        registry.Register(
            ErlangModuleNames.Module,
            ErlangBasicBifNames.Apply,
            2,
            (context, arguments) =>
        {
            Term[] values = Cons.Items(arguments[1]).ToArray();
            if (arguments[0] is FunctionTerm callback && callback.Arity != values.Length)
                throw context.CaptureSourceFailure(new ErlangException(Term.Tuple(
                    Term.A(ErlangErrorReasons.BadArity),
                    Term.Tuple(callback, Cons.From(values))
                )));

            return ListCallback.Invoke(context, arguments[0], values);
        }
        );
    }
}
