namespace Erlang;

internal static class ErlangAtomRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        void Add(string name, int arity, Func<IReadOnlyList<Term>, Term> body) => registry.RegisterCanonicalValueBif354(
            name,
            arity,
            (context, arguments) =>
            {
                try
                {
                    Term result = body(arguments);

                    return name == ErlangAtomBifNames.AtomToBinary
                        ? context.Runtime.OwnAtomBinaryConstant357(context, arguments, result) : result;
                }
                catch (ErlangException error) when (arity == 1
                    && name is ErlangAtomBifNames.AtomToBinary or ErlangAtomBifNames.BinaryToAtom or ErlangAtomBifNames.BinaryToExistingAtom
                    && error.ExceptionClass == ErlangExceptionClasses.Error)
                {
                    throw new ErlangException(error.Reason, error.ExceptionClass)
                    {
                        ExplicitStackTrace = true,
                        StackTraceTerm = context.CaptureErrorStack(
                            Cons.From(arguments),
                            context.Runtime.ErrorHelperInfo357(context),
                            name,
                            PortErrorSourceLocations.ErrorWithInfo
                        )
                    };
                }
            }
        );
        Add(
            ErlangAtomBifNames.AtomToList,
            1,
            arguments => arguments[0] is Atom atom ? Term.String(atom.Name) : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        Add(ErlangAtomBifNames.ListToAtom, 1, arguments => ErlangAtomConversions.FromList(arguments[0], false));
        Add(ErlangAtomBifNames.ListToExistingAtom, 1, arguments => ErlangAtomConversions.FromList(arguments[0], true));
        foreach (int arity in new[] { 1, 2 })
        {
            Add(ErlangAtomBifNames.AtomToBinary, arity, ErlangAtomConversions.ToBinary);
            Add(ErlangAtomBifNames.BinaryToAtom, arity, arguments => ErlangAtomConversions.FromBinary(arguments, false));
            Add(ErlangAtomBifNames.BinaryToExistingAtom, arity, arguments => ErlangAtomConversions.FromBinary(arguments, true));
        }
    }
}
