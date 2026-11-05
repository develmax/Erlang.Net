// Derived from lib/stdlib/src/ets.erl, OTP-29.1.1, Apache-2.0.
// Copyright Ericsson AB 1996-2026.
using System.Buffers.Binary;

namespace Erlang;

public static partial class EtsModule
{
    private static FunctionTerm Closure(
        ProcessContext context,
        string name,
        int index,
        int arity,
        Term[] environment,
        Func<ProcessContext, IReadOnlyList<Term>, IReadOnlyList<Term>, ValueTask<Term>> body
    )
    {
        var fingerprint = (BitString)context.Runtime.Modules.ModuleInformation(Name, ModuleInformationKeys.Md5);
        var identity = new LocalFunctionIdentity(
            A(Name),
            A(name),
            index,
            (int)(BinaryPrimitives.ReadUInt32BigEndian(fingerprint.ToArray()) >> EtsQueryLayout.OldUniqueBitShift),
            fingerprint
        );

        FunctionTerm Materialize(IReadOnlyList<Term> captured) => new(
            identity,
            arity,
            captured,
            (execution, arguments) => body((ProcessContext)execution, arguments, captured)
        );
        identity = context.Runtime.Functions.Register(
            identity,
            arity,
            environment.Length,
            Materialize
        );

        return Materialize(environment);
    }

    private static async ValueTask<Term> QueryTable(ProcessContext context, Term tableArg, Term options)
    {
        Term table = await SoftWhereis(context, tableArg);
        Term remaining = IsList(options) ? options : Term.List(options);
        Term traverse = A(EtsAtomNames.Select);
        Term limit = Term.I(EtsQueryLayout.DefaultObjectCount);
        var parsedValues = new List<Term>();
        string[] keys = [EtsAtomNames.Traverse, EtsAtomNames.NObjects];
        bool invalid = false;
        using (context.EnterSourceLocation(EtsSource.Location(EtsSourceLines.QueryOptions)))
        {
            for (int index = 0; index < keys.Length; index++)
            {
                string key = keys[index];
                using var helper = EtsSource.Frame(
                    context,
                    EtsFunctionNames.Options,
                    3,
                    EtsSourceLines.QueryOptionLookup
                );
                if (!IsList(remaining))
                {
                    throw EtsSource.Error(
                        context,
                        EtsFunctionNames.Options,
                        [remaining, Cons.From(keys.Skip(index).Select(value => (Term)A(value))), Cons.From(parsedValues.AsEnumerable().Reverse())],
                        A(EtsErrorNames.FunctionClause),
                        EtsSourceLines.QueryOptionsGuard
                    );
                }
                Term selected = await Call(
                    context,
                    EtsDependencyModules.Lists,
                    EtsFunctionNames.Keyfind,
                    A(key),
                    Term.I(1),
                    remaining
                );
                if (Is(selected, EtsAtomNames.False))
                {
                    parsedValues.Add(key == EtsAtomNames.Traverse ? traverse : limit);
                    continue;
                }
                if (selected is TupleTerm { Items.Count: 2 } pair)
                {
                    Term value = pair.Items[1];
                    if (key == EtsAtomNames.NObjects && (Is(value, EtsAtomNames.Default) || value is Integer n && n.Value >= 1))
                    {
                        limit = Is(value, EtsAtomNames.Default) ? Term.I(EtsQueryLayout.DefaultObjectCount) : value;
                    }
                    else if (key == EtsAtomNames.Traverse && (Is(value, EtsAtomNames.Select) || Is(value, EtsAtomNames.FirstNext) || Is(value, EtsAtomNames.LastPrev)
                        || value is TupleTerm { Items.Count: 2 } spec && Is(spec.Items[0], EtsAtomNames.Select)))
                    {
                        traverse = value;
                    }
                    else
                    {
                        invalid = true;
                        break;
                    }
                }
                else
                {
                    throw context.CaptureSourceFailure(new ErlangException(Term.Tuple(A(EtsErrorNames.CaseClause), selected)));
                }
                remaining = await Call(
                    context,
                    EtsDependencyModules.Lists,
                    EtsFunctionNames.Keydelete,
                    A(key),
                    Term.I(1),
                    remaining
                );
                parsedValues.Add(key == EtsAtomNames.Traverse ? traverse : limit);
            }
        }
        if (invalid)
        {
            throw EtsSource.Error(
                context,
                EtsFunctionNames.Table,
                [table, options],
                A(EtsErrorNames.Badarg),
                EtsSourceLines.QueryInvalidOptions
            );
        }
        FunctionTerm traversal;
        if (Is(traverse, EtsAtomNames.FirstNext) || Is(traverse, EtsAtomNames.LastPrev))
        {
            traversal = Closure(
                context,
                EtsQueryFunctions.TraverseKeys,
                EtsQueryFunctionIndices.TraverseKeys,
                0,
                [table, traverse],
                async (c, a, e) =>
            {
                bool reverse = Is(e[1], EtsAtomNames.LastPrev);

                return await QueryKeys(
                    c,
                    e[0],
                    await Call(
                        c,
                        Name,
                        reverse ? EtsFunctionNames.Last : EtsFunctionNames.First,
                        e[0]
                    ),
                    reverse
                );
            }
            );
        }
        else if (traverse is TupleTerm spec)
        {
            traversal = Closure(
                context,
                EtsQueryFunctions.SelectWithMatchSpec,
                EtsQueryFunctionIndices.SelectWithMatchSpec,
                0,
                [table, spec.Items[1], limit],
                async (c, a, e) => await QuerySelect(c, await Call(
                    c,
                    Name,
                    EtsFunctionNames.Select,
                    e[0],
                    e[1],
                    e[2]
                ))
            );
        }
        else
        {
            traversal = Closure(
                context,
                EtsQueryFunctions.SelectWithDefaultMatchSpec,
                EtsQueryFunctionIndices.SelectWithDefaultMatchSpec,
                1,
                [table, limit],
                async (c, a, e) => await QuerySelect(c, await Call(
                    c,
                    Name,
                    EtsFunctionNames.Select,
                    e[0],
                    a[0],
                    e[1]
                ))
            );
        }
        FunctionTerm before = Closure(
            context,
            EtsQueryFunctions.BeforeTraversal,
            EtsQueryFunctionIndices.BeforeTraversal,
            1,
            [table],
            (c, a, e) => Call(
                c,
                Name,
                EtsFunctionNames.SafeFixtable,
                e[0],
                A(EtsAtomNames.True)
            )
        );
        FunctionTerm after = Closure(
            context,
            EtsQueryFunctions.AfterTraversal,
            EtsQueryFunctionIndices.AfterTraversal,
            0,
            [table],
            (c, a, e) => Call(
                c,
                Name,
                EtsFunctionNames.SafeFixtable,
                e[0],
                A(EtsAtomNames.False)
            )
        );
        FunctionTerm info = Closure(
            context,
            EtsQueryFunctions.Information,
            EtsQueryFunctionIndices.Information,
            1,
            [table],
            async (c, a, e) =>
        {
            if (Is(a[0], EtsAtomNames.NumOfObjects) || Is(a[0], EtsFileFields.Keypos))
            {
                return await Call(
                    c,
                    Name,
                    EtsFunctionNames.Info,
                    e[0],
                    Is(a[0], EtsFileFields.Keypos) ? A(EtsFileFields.Keypos) : A(EtsAtomNames.Size)
                );
            }
            if (Is(a[0], EtsAtomNames.IsUniqueObjects) || Is(a[0], EtsAtomNames.IsSortedKey))
            {
                Term type = await Call(
                    c,
                    Name,
                    EtsFunctionNames.Info,
                    e[0],
                    A(EtsAtomNames.Type)
                );

                return A((Is(a[0], EtsAtomNames.IsUniqueObjects) ? !Is(type, EtsAtomNames.DuplicateBag) : Is(type, EtsAtomNames.OrderedSet)) ? EtsAtomNames.True : EtsAtomNames.False);
            }

            return A(EtsAtomNames.Undefined);
        }
        );
        Term type;
        using (context.EnterSourceLocation(EtsSource.Location(EtsSourceLines.QueryEquality)))
        {
            type = await Call(
                context,
                Name,
                EtsFunctionNames.Info,
                table,
                A(EtsAtomNames.Type)
            );
        }
        Term equality = A(Is(type, EtsAtomNames.OrderedSet) ? EtsAtomNames.NumericEquality : EtsAtomNames.ExactEquality);
        Term lookup = traverse is TupleTerm ? A(EtsAtomNames.Undefined) : Closure(
            context,
            EtsQueryFunctions.Lookup,
            EtsQueryFunctionIndices.Lookup,
            2,
            [table],
            async (c, a, e) =>
        {
            var rows = new List<Term>();
            foreach (Term key in Cons.Items(a[1]))
            {
                rows.AddRange(Cons.Items(await Call(
                    c,
                    Name,
                    EtsFunctionNames.Lookup,
                    e[0],
                    key
                )));
            }

            return Cons.From(rows);
        }
        );
        FunctionTerm format = Closure(
            context,
            EtsQueryFunctions.Format,
            EtsQueryFunctionIndices.Format,
            1,
            [table, options],
            QueryFormat
        );
        Term qlcOptions = Cons.From(
            new Term[]
        {
            Term.Tuple(A(EtsAtomNames.PreFun), before), Term.Tuple(A(EtsAtomNames.PostFun), after),
            Term.Tuple(A(EtsAtomNames.InfoFun), info), Term.Tuple(A(EtsAtomNames.FormatFun), format),
            Term.Tuple(A(EtsAtomNames.KeyEquality), equality), Term.Tuple(A(EtsAtomNames.LookupFun), lookup)
        },
            remaining
        );

        return await Call(
            context,
            EtsDependencyModules.Qlc,
            EtsFunctionNames.Table,
            traversal,
            qlcOptions
        );
    }

    private static async ValueTask<Term> QueryKeys(
        ProcessContext context,
        Term table,
        Term key,
        bool reverse
    )
    {
        if (Is(key, EtsAtomNames.EndOfTable))
        {
            return Nil.Value;
        }
        Term objects = await Call(
            context,
            Name,
            EtsFunctionNames.Lookup,
            table,
            key
        );
        FunctionTerm next = Closure(
            context,
            EtsQueryFunctions.NextKey,
            EtsQueryFunctionIndices.NextKey,
            0,
            [table, key, A(reverse ? EtsAtomNames.True : EtsAtomNames.False)],
            async (c, a, e) => await QueryKeys(
                c,
                e[0],
                await Call(
                    c,
                    Name,
                    Is(e[2], EtsAtomNames.True) ? EtsFunctionNames.Prev : EtsFunctionNames.Next,
                    e[0],
                    e[1]
                ),
                Is(e[2], EtsAtomNames.True)
            )
        );

        return Cons.From(Cons.Items(objects), next);
    }

    private static ValueTask<Term> QuerySelect(ProcessContext context, Term result)
    {
        if (Is(result, EtsAtomNames.EndOfTable))
        {
            return ValueTask.FromResult<Term>(Nil.Value);
        }
        var pair = (TupleTerm)result;
        FunctionTerm next = Closure(
            context,
            EtsQueryFunctions.SelectContinuation,
            EtsQueryFunctionIndices.SelectContinuation,
            0,
            [pair.Items[1]],
            async (c, a, e) => await QuerySelect(c, await Call(
                c,
                Name,
                EtsFunctionNames.Select,
                e[0]
            ))
        );

        return ValueTask.FromResult(Cons.From(Cons.Items(pair.Items[0]), next));
    }

    private static async ValueTask<Term> QueryFormat(ProcessContext context, IReadOnlyList<Term> arguments, IReadOnlyList<Term> environment)
    {
        Term table = environment[0];
        Term options = environment[1];
        if (arguments[0] is TupleTerm tuple)
        {
            if (tuple.Items.Count == EtsQueryLayout.AllFormatArity && Is(tuple.Items[0], EtsAtomNames.All))
            {
                return Term.Tuple(A(Name), A(EtsAtomNames.Table), options is Nil ? Term.List(table) : Term.List(table, options));
            }
            if (tuple.Items.Count == EtsQueryLayout.MatchSpecFormatArity && Is(tuple.Items[0], EtsAtomNames.MatchSpec))
            {
                Term opts = IsList(options) ? options : Term.List(options);

                return Term.Tuple(
                    A(Name),
                    A(EtsAtomNames.Table),
                    Term.List(table, new Cons(Term.Tuple(A(EtsAtomNames.Traverse), Term.Tuple(A(EtsAtomNames.Select), tuple.Items[1])), opts))
                );
            }
            if (tuple.Items.Count == EtsQueryLayout.LookupFormatArity && Is(tuple.Items[0], EtsAtomNames.Lookup))
            {
                Term[] values = Cons.Items(tuple.Items[EtsQueryLayout.LookupValuesIndex]).ToArray();
                if (values.Length == 1)
                {
                    return await Call(
                        context,
                        EtsDependencyModules.IoLib,
                        EtsFunctionNames.Format,
                        Term.String(EtsQueryText.LookupExpression),
                        Term.List(A(Name), table, await Apply(context, tuple.Items[EtsQueryLayout.LookupTransformIndex], values[0]))
                    );
                }
                var formatted = new List<Term>();
                foreach (Term value in values)
                {
                    formatted.Add(await Apply(context, tuple.Items[EtsQueryLayout.LookupTransformIndex], value));
                }

                return await Call(
                    context,
                    EtsDependencyModules.IoLib,
                    EtsFunctionNames.Format,
                    Term.String(EtsQueryText.LookupManyExpression),
                    Term.List(A(Name), table, Cons.From(formatted))
                );
            }
        }

        throw new ErlangException(ErlangErrorReasons.FunctionClause);
    }
}
