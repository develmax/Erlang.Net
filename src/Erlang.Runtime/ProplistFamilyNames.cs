namespace Erlang;

internal static class ProplistFamilyNames
{
    internal const string Module = "proplists";
    internal const string Property = "property";
    internal const string Unfold = "unfold";
    internal const string Compact = "compact";
    internal const string Lookup = "lookup";
    internal const string LookupAll = "lookup_all";
    internal const string IsDefined = "is_defined";
    internal const string GetAllValues = "get_all_values";
    internal const string AppendValues = "append_values";
    internal const string GetBool = "get_bool";
    internal const string GetKeys = "get_keys";
    internal const string Delete = "delete";
    internal const string Aliases = "substitute_aliases";
    internal const string Negations = "substitute_negations";
    internal const string Expand = "expand";
    internal const string Normalize = "normalize";
    internal const string Split = "split";
    internal const string FromMap = "from_map";
    internal const string ToMap = "to_map";
    internal const string AliasStage = "aliases";
    internal const string NegationStage = "negations";
    internal const string ExpansionStage = "expand";
    internal const string BadGenerator = "bad_generator";
    internal static readonly Atom True = Term.A("true");
    internal static readonly Atom False = Term.A("false");
    internal static readonly Atom None = Term.A("none");
}
