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

        void Read507(string name, int arity, Func<ProcessContext, IReadOnlyList<Term>, Term> body) => registry.RegisterCanonicalRead507(
            ErlangModuleNames.Module,
            name,
            arity,
            (context, arguments) => ValueTask.FromResult(body(context, arguments))
        );

        void Value354(string name, int arity, Func<ProcessContext, IReadOnlyList<Term>, Term> body)
            => registry.RegisterCanonicalValueBif354(name, arity, body);

        foreach (string operation in new[] { ErlangOperators.Plus, ErlangOperators.Minus, ErlangOperators.Not, ErlangOperators.BitwiseNot })
            Value354(operation, 1, (_, arguments) => ErlangOperatorValues.Unary(operation, arguments[0]));
        foreach (string operation in new[] { ErlangOperators.Plus, ErlangOperators.Minus, ErlangOperators.Multiply, ErlangOperators.Divide, ErlangOperators.IntegerDivide, ErlangOperators.Remainder, ErlangOperators.BitwiseAnd, ErlangOperators.BitwiseOr, ErlangOperators.BitwiseXor, ErlangOperators.ShiftLeft, ErlangOperators.ShiftRight, ErlangOperators.And, ErlangOperators.Or, ErlangOperators.Xor, ErlangOperators.NumericEqual, ErlangOperators.NumericNotEqual, ErlangOperators.ExactEqual, ErlangOperators.ExactNotEqual, ErlangOperators.Less, ErlangOperators.Greater, ErlangOperators.LessOrEqual, ErlangOperators.GreaterOrEqual, ErlangOperators.SubtractList })
        {
            Func<ProcessContext, IReadOnlyList<Term>, Term> body = (context, arguments) => ErlangOperatorValues.Binary(
                operation,
                arguments[0],
                arguments[1],
                context
            );
            if (operation is ErlangOperators.Plus or ErlangOperators.Minus or ErlangOperators.Multiply
                or ErlangOperators.Divide or ErlangOperators.IntegerDivide or ErlangOperators.Remainder
                or ErlangOperators.BitwiseAnd or ErlangOperators.BitwiseOr or ErlangOperators.BitwiseXor
                or ErlangOperators.ShiftLeft or ErlangOperators.ShiftRight)
                Value354(operation, 2, body);
            else
                Add(operation, 2, body);
        }
        foreach (string operation in new[] { ErlangBasicBifNames.Absolute, ErlangBasicBifNames.Float, ErlangBasicBifNames.Round, ErlangBasicBifNames.Truncate, ErlangBasicBifNames.Floor, ErlangBasicBifNames.Ceiling })
            Value354(operation, 1, (_, arguments) => ErlangNumericBifs.Run(operation, arguments[0]));
        Value354(ErlangBasicBifNames.Minimum, 2, (_, arguments) => ErlangInfallibleBifValues.Minimum(arguments[0], arguments[1]));
        Value354(ErlangBasicBifNames.Maximum, 2, (_, arguments) => ErlangInfallibleBifValues.Maximum(arguments[0], arguments[1]));
        Add(
            ErlangBasicBifNames.IsBoolean,
            1,
            (_, arguments) => CoreModules.Boolean(arguments[0] is Atom { Name: ErlangBooleanAtoms.True or ErlangBooleanAtoms.False })
        );
        Read507(ErlangBasicBifNames.IsFunction, 1, (_, arguments) => CoreModules.Boolean(arguments[0] is FunctionTerm));
        Read507(
            ErlangBasicBifNames.IsFunction,
            2,
            (_, arguments) => arguments[1] is Integer number && number.Value >= 0 ? CoreModules.Boolean(arguments[0] is FunctionTerm function && function.Arity == number.Value) : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        Add(ErlangBasicBifNames.IsPort, 1, (_, arguments) => CoreModules.Boolean(arguments[0] is PortTerm));
        Read507(ErlangBasicBifNames.IsReference, 1, (_, arguments) => CoreModules.Boolean(arguments[0] is ReferenceTerm));
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
        Value354(
            ErlangBasicBifNames.TupleToList,
            1,
            (_, arguments) => arguments[0] is TupleTerm tuple ? Cons.From(tuple.Items) : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        Add(ErlangBasicBifNames.ListToTuple, 1, (_, arguments) => ErlangTupleBifs.FromList(arguments[0]));
        registry.RegisterCanonicalListAppend206(ErlangOperators.Append);
        registry.RegisterCanonicalListAppend206(ErlangBasicBifNames.Append);
        Add(ErlangBasicBifNames.Subtract, 2, (_, arguments) => ListBasicOperations.Subtract(arguments[0], arguments[1]));
        foreach (string name in new[] { ErlangBasicBifNames.Send, ErlangOperators.Send })
            registry.RegisterCanonicalMessage(
                ErlangModuleNames.Module,
                name,
                2,
                async (context, arguments) =>
            {
                await ErlangOperatorValues.TrySendAsync(context, arguments[0], arguments[1]);

                return arguments[1];
            }
            );
        registry.RegisterCanonicalDictionary507(
            ErlangModuleNames.Module,
            ErlangModuleNames.Get,
            0,
            (context, _) => ValueTask.FromResult(OwnedDictionaryBifs507.All(context, false))
        );
        registry.RegisterCanonicalDictionary507(
            ErlangModuleNames.Module,
            ErlangModuleNames.Erase,
            0,
            (context, _) => ValueTask.FromResult(OwnedDictionaryBifs507.All(context, true))
        );
        registry.RegisterCanonicalDictionary507(
            ErlangModuleNames.Module,
            ErlangBasicBifNames.GetKeys,
            0,
            (context, _) => ValueTask.FromResult(OwnedDictionaryKeys605.All(context))
        );
        registry.RegisterCanonicalDictionary507(
            ErlangModuleNames.Module,
            ErlangBasicBifNames.GetKeys,
            1,
            (context, arguments) => ValueTask.FromResult(OwnedDictionaryKeys605.Matching(context, arguments[0]))
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
            ErlangApplySourceLocation.FunctionApplyArity,
            ErlangApplyFunctionDispatch.Invoke
        );
    }
}
