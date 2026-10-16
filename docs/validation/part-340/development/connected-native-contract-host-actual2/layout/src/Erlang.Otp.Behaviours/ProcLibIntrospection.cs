using System.Globalization;

namespace Erlang.Otp;

internal static class ProcLibIntrospection
{
    public static void Register(ModuleRegistry registry)
    {
        registry.Register(
            ProcLibNames.Module,
            ProcLibIntrospectionNames.InitialCall,
            1,
            InitialCall
        );
        registry.Register(
            ProcLibNames.Module,
            ProcLibIntrospectionNames.TranslateInitialCall,
            1,
            TranslateInitialCall
        );
        registry.Register(
            ProcLibNames.Module,
            ProcLibIntrospectionNames.SetLabel,
            1,
            SetLabel
        );
        registry.Register(
            ProcLibNames.Module,
            ProcLibIntrospectionNames.GetLabel,
            1,
            GetLabel
        );
    }

    private static async ValueTask<Term> InitialCall(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        Term raw = await RawInitialCall(context, arguments[0]);
        if (raw is not TupleTerm { Items: [var module, var function, var arity] })
            return ProcLibIntrospectionAtoms.False;
        Term dummyArguments = await MakeDummyArguments(context, arity);

        return Term.Tuple(module, function, dummyArguments);
    }

    private static async ValueTask<Term> TranslateInitialCall(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        Term raw = await RawInitialCall(context, arguments[0]);

        return raw is TupleTerm ? raw : ProcLibIntrospectionAtoms.DefaultInitialCall;
    }

    private static async ValueTask<Term> RawInitialCall(ProcessContext context, Term input)
    {
        if (input is TupleTerm { Items: [Integer channel, Integer number, Integer serial] })
        {
            string text = string.Format(
                CultureInfo.InvariantCulture,
                ProcLibIntrospectionNames.PidFormat,
                channel.Value,
                number.Value,
                serial.Value
            );
            input = await context.Runtime.Modules.Call(
                context,
                ProcLibNames.Erlang,
                ProcessIdentifierTextNames.ListToPid,
                Term.String(text)
            );
        }
        if (input is Pid)
        {
            Term value = await DictionaryValue(context, input, ProcLibIntrospectionAtoms.InitialCallQuery);

            return value is TupleTerm { Items.Count: 3 } ? value : ProcLibIntrospectionAtoms.False;
        }
        if (input is not Cons and not Nil)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        Term specific = await KeyFind(context, ProcLibIntrospectionAtoms.InitialCallQuery, input);
        if (specific is TupleTerm { Items: [_, var stored] })
            return stored is TupleTerm { Items.Count: 3 } ? stored : ProcLibIntrospectionAtoms.False;
        if (!specific.Equals(ProcLibIntrospectionAtoms.False))
            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), specific));
        Term dictionary = await KeyFind(context, ProcLibIntrospectionAtoms.Dictionary, input);
        if (dictionary is not TupleTerm { Items: [_, var entries] })
            return ProcLibIntrospectionAtoms.False;
        Term initial = await KeyFind(context, ProcLibAtoms.InitialCall, entries);

        return initial is TupleTerm { Items: [_, TupleTerm { Items.Count: 3 } mfa] } ? mfa : ProcLibIntrospectionAtoms.False;
    }

    private static async ValueTask<Term> KeyFind(ProcessContext context, Term key, Term list)
        => await context.Runtime.Modules.Call(
            context,
            ListModuleNames.Module,
            ListModuleNames.KeyFind,
            key,
            Term.I(1),
            list
        );

    private static async ValueTask<Term> DictionaryValue(ProcessContext context, Term target, TupleTerm query)
    {
        try
        {
            Term information = await ProcLibProcessInfoQuery.Read(context, target, query);

            return information is TupleTerm { Items: [var key, var value] } && key.Equals(query) ? value : ProcessInfoAtoms.Undefined;
        }
        catch (ErlangException)
        {
            return ProcessInfoAtoms.Undefined;
        }
    }

    internal static async ValueTask<Term> MakeDummyArguments(ProcessContext context, Term arity)
    {
        Term result = Nil.Value;
        while (arity is not Integer integer || !integer.Value.IsZero)
        {
            await context.ReduceAsync();
            Term digits = await context.Runtime.Modules.Call(
                context,
                ProcLibNames.Erlang,
                ErlangDataBifNames.IntegerToList,
                arity
            );
            Term argument = await context.Runtime.Modules.Call(
                context,
                ProcLibNames.Erlang,
                ErlangAtomBifNames.ListToAtom,
                Term.String(ProcLibIntrospectionNames.DummyArgumentPrefix + Term.Text(digits))
            );
            result = new Cons(argument, result);
            arity = new Integer(((Integer)arity).Value - 1);
        }

        return result;
    }

    private static async ValueTask<Term> SetLabel(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        await context.Runtime.Modules.Call(
            context,
            ProcLibNames.Erlang,
            ProcLibIntrospectionNames.Put,
            ProcLibIntrospectionAtoms.ProcessLabel,
            arguments[0]
        );

        return ProcLibAtoms.Ok;
    }

    private static async ValueTask<Term> GetLabel(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (arguments[0].Equals(context.Self))
            return await context.Runtime.Modules.Call(
                context,
                ProcLibNames.Erlang,
                ProcLibIntrospectionNames.Get,
                ProcLibIntrospectionAtoms.ProcessLabel
            );

        return await DictionaryValue(context, arguments[0], ProcLibIntrospectionAtoms.ProcessLabelQuery);
    }
}
