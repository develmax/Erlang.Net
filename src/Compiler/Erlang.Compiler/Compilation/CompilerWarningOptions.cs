namespace Erlang.Compiler;

internal static class CompilerWarningOptions
{
    internal static bool IsSupported(Term option) => option switch
    {
        Atom { Name: "nowarn_deprecated_function" or "nowarn_deprecated_type" or "nowarn_deprecated_catch"
            or "nowarn_deprecated_callback" or "nowarn_hidden_doc" or "nowarn_missing_doc" or "nowarn_unused_vars"
            or "nowarn_unused_function" or "nowarn_unused_type" or "nowarn_unexported_function"
            or "warn_missing_spec" or "warn_unused_vars" or "report_warnings" or "report_errors" or "report" } => true,
        TupleTerm { Items: [Atom { Name: CompilerOptionNames.SuppressUnsafeFunctionWarning or "nowarn_unsafe_function" }, var target] }
            => IsRemoteSignature(target),
        TupleTerm { Items: [Atom { Name: "nowarn_deprecated_function" or "nowarn_deprecated_type" }, var targets] }
            => AreSignatures(targets, IsRemoteSignature),
        TupleTerm { Items: [Atom { Name: "nowarn_redefined_builtin_type" or "nowarn_hidden_doc" or "nowarn_unused_function" }, var targets] }
            => AreSignatures(targets, IsLocalSignature),
        _ => false
    };

    private static bool AreSignatures(Term term, Func<Term, bool> isSignature)
    {
        if (term is Cons or Nil)
        {
            while (term is Cons cell)
            {
                if (!isSignature(cell.Head))
                {
                    return false;
                }
                term = cell.Tail;
            }
            return term is Nil;
        }

        return isSignature(term);
    }

    private static bool IsLocalSignature(Term term) =>
        term is TupleTerm { Items: [Atom, Integer arity] } && IsArity(arity);

    private static bool IsRemoteSignature(Term term) =>
        term is TupleTerm { Items: [Atom, Atom, Integer arity] } && IsArity(arity);

    private static bool IsArity(Integer arity) => arity.Value >= 0 && arity.Value <= BeamWire.MaximumArity;
}
