using System.Text;

namespace Erlang.Compiler;

internal sealed class BeamDocumentationDeprecations
{
    private static readonly Term Wildcard = Term.A("_");
    private static readonly Term Deprecated = Term.A("deprecated");
    private readonly Dictionary<(string Kind, Term Name, Term Arity), Term> reasons = [];

    internal BeamDocumentationDeprecations(IEnumerable<ModuleAttribute> attributes)
    {
        foreach (var attribute in attributes)
        {
            string? kind = attribute.Name switch
            {
                "deprecated" => "function",
                "deprecated_type" => "type",
                "deprecated_callback" => "callback",
                _ => null
            };
            if (kind is not null)
            {
                Read(kind, attribute.Value);
            }
        }
    }

    private void Read(string kind, Term value)
    {
        if (value is Cons list)
        {
            foreach (Term entry in Cons.Items(list))
            {
                Read(kind, entry);
            }
        }
        else if (value is TupleTerm { Items: [Atom name, var arity] })
        {
            reasons[(kind, name, arity)] = Term.A("undefined");
        }
        else if (value is TupleTerm { Items: [Atom specificName, var specificArity, var reason] })
        {
            reasons[(kind, specificName, specificArity)] = reason;
        }
    }

    internal void Apply(Dictionary<Term, Term> metadata, string module, string kind, (string Name, int Arity) key)
    {
        if (metadata.ContainsKey(Deprecated))
        {
            return;
        }

        Term name = Term.A(key.Name);
        Term arity = Term.I(key.Arity);
        if (!reasons.TryGetValue((kind, name, arity), out Term? reason)
            && !reasons.TryGetValue((kind, name, Wildcard), out reason)
            && !reasons.TryGetValue((kind, Wildcard, arity), out reason)
            && !reasons.TryGetValue((kind, Wildcard, Wildcard), out reason))
        {
            return;
        }

        string detail = reason switch
        {
            Atom { Name: "undefined" } => "see the documentation for details",
            Atom { Name: "next_version" } => "will be removed in the next version. See the documentation for details",
            Atom { Name: "next_major_release" } => "will be removed in the next major release. See the documentation for details",
            Atom { Name: "eventually" } => "will be removed in a future release. See the documentation for details",
            Cons or Nil => BeamDocumentationText.ReadText(reason),
            _ => throw new NotSupportedException(BeamEmissionFormat.Unsupported)
        };
        string typeArguments = string.Join(",", Enumerable.Repeat("_", key.Arity));
        string subject = kind == "function"
            ? FormattableString.Invariant($"{module}:{key.Name}/{key.Arity}")
            : FormattableString.Invariant($"the {kind} {BeamDocumentationWriter.FormatFunctionName(module)}:{BeamDocumentationWriter.FormatFunctionName(key.Name)}({typeArguments})");
        metadata[Deprecated] = new BitString(Encoding.UTF8.GetBytes($"{subject} is deprecated; {detail}"));
    }
}
