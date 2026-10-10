// Modified: CLR adaptation of OTP-29.1.1 erl_eval match1 aliased patterns.
// Preserve the original key/size scope and reuse the transactional match boundary.
namespace Erlang;

public sealed record AliasPattern(Pattern Left, Pattern Right) : Pattern
{
    protected override bool MatchCore(
        Term value,
        Dictionary<string, Term> bindings,
        ProcessContext? context = null,
        Dictionary<string, Term>? keyScope = null
    ) => Left.Match(
        value,
        bindings,
        context,
        keyScope
    ) && Right.Match(
        value,
        bindings,
        context,
        keyScope
    );
}
