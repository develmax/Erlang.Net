namespace Erlang;

internal static class ProcessHibernateRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        registry.Register(
            ErlangModuleNames.Module,
            ProcessHibernateNames.Function,
            0,
            async (context, _) =>
        {
            await context.Mailbox.WaitForMessage(context.Cancellation);

            return Term.A(ProcessHibernateNames.Completed);
        }
        );
        registry.Register(
            ErlangModuleNames.Module,
            ProcessHibernateNames.Function,
            3,
            (context, arguments) =>
        {
            if (arguments[0] is not Atom module || arguments[1] is not Atom function)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            var values = new List<Term>();
            Term remaining = arguments[2];
            while (remaining is Cons list && values.Count <= ProcessHibernateParameters.MaximumCourtesyArity)
            {
                values.Add(list.Head);
                remaining = list.Tail;
            }
            if (remaining is not Nil)
                throw new ErlangException(values.Count > ProcessHibernateParameters.MaximumCourtesyArity ? ErlangErrorReasons.SystemLimit : ErlangErrorReasons.BadArgument);

            throw new ProcessHibernateTransfer(module.Name, function.Name, values);
        }
        );
    }
}
