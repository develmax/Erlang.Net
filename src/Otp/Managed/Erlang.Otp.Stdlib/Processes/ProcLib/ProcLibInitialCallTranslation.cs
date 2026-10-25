namespace Erlang.Otp;

internal static class ProcLibInitialCallTranslation
{
    public static Term Translate(Atom module, Atom function, Term arguments)
    {
        if (module.Name == ProcLibNames.Gen && function.Name == ProcLibNames.InitIt)
        {
            var prefix = new List<Term>();
            Term tail = arguments;
            while (tail is Cons item && prefix.Count <= ProcLibLifecycleParameters.LongInitializerArity)
            {
                prefix.Add(item.Head);
                tail = item.Tail;
            }
            bool properInitializer = tail is Nil && prefix.Count is ProcLibLifecycleParameters.ShortInitializerArity or ProcLibLifecycleParameters.LongInitializerArity;
            if (properInitializer && prefix[0] is Atom { Name: ProcLibNames.GenServer })
            {
                int behaviourIndex = prefix.Count - ProcLibLifecycleParameters.BehaviourTrailingFields;
                if (prefix[behaviourIndex] is Atom { Name: ProcLibNames.Supervisor } && prefix[behaviourIndex + 1] is TupleTerm { Items: [_, var supervisor, _] })
                    return Term.Tuple(Term.A(ProcLibNames.Supervisor), supervisor, Term.I(1));
                if (prefix[behaviourIndex] is Atom { Name: ProcLibNames.SupervisorBridge } && prefix[behaviourIndex + 1] is Cons bridge)
                    return Term.Tuple(Term.A(ProcLibNames.SupervisorBridge), bridge.Head, Term.I(1));
            }
            if (prefix.Count > 0 && prefix[0] is Atom { Name: ProcLibNames.GenEvent })
                return Term.Tuple(Term.A(ProcLibNames.GenEvent), Term.A(ProcLibNames.InitIt), Term.I(ProcLibLifecycleParameters.ShortInitializerArity));
            if (tail is Nil && prefix.Count == ProcLibLifecycleParameters.ShortInitializerArity && prefix[ProcLibLifecycleParameters.ShortModuleIndex] is Atom shortModule)
                return Term.Tuple(shortModule, Term.A(ProcLibNames.Init), Term.I(1));
            if (prefix.Count > ProcLibLifecycleParameters.LongModuleIndex && prefix[ProcLibLifecycleParameters.LongModuleIndex] is Atom longModule)
                return Term.Tuple(longModule, Term.A(ProcLibNames.Init), Term.I(1));
        }

        return Term.Tuple(module, function, Term.I(Cons.Items(arguments).Count()));
    }
}
