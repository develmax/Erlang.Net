using System.Text;
using Erlang.Compiler;

namespace Erlang.BeamBuild;

internal static class BeamDeclaredRawPolicy
{
    internal static BeamDeclaredIncludeEdge[] Inspect(
        string logical,
        byte[] bytes,
        IReadOnlySet<string> declared,
        string[] includeDirectories
    )
    {
        List<Token> tokens = Lexer.Scan(BeamSourceText.Read(bytes));
        var edges = new List<BeamDeclaredIncludeEdge>();
        bool beginning = true;
        for (int index = 0; index < tokens.Count; index++)
        {
            Token token = tokens[index];
            if (token.Kind == BeamSourcePolicyNames.FormTerminator)
            {
                beginning = true;
                continue;
            }
            if (token.Kind == BeamDeclaredInputNames.EndToken)
                break;
            if (!beginning)
                continue;
            beginning = false;
            if (token.Kind != BeamSourcePolicyNames.Symbol)
                continue;
            if (token.Text == BeamSourcePolicyNames.Macro)
                throw new InvalidDataException(BeamBuildNames.Invalid);
            if (token.Text != BeamSourcePolicyNames.Attribute)
                continue;
            if (++index >= tokens.Count)
                throw new InvalidDataException(BeamBuildNames.Invalid);
            Token attribute = tokens[index];
            // if/else can be keyword tokens. Exact literal spelling is allowed;
            // symbol/variable macro-generated attribute names are never allowed.
            if (attribute.Kind is not (BeamSourcePolicyNames.Atom or BeamSourcePolicyNames.QuotedAtom or BeamDeclaredInputNames.KeywordToken)
                || !BeamDeclaredInputNames.Attributes.Contains(attribute.Text))
                throw new InvalidDataException(BeamBuildNames.Invalid);
            if (attribute.Text != BeamDeclaredInputNames.Include)
                continue;
            if (++index >= tokens.Count || tokens[index].Text != BeamSourcePolicyNames.Open)
                throw new InvalidDataException(BeamBuildNames.Invalid);
            var literal = new StringBuilder();
            int count = 0;
            while (++index < tokens.Count && tokens[index].Kind == BeamDeclaredInputNames.StringToken)
            {
                literal.Append(tokens[index].Text);
                count++;
            }
            if (count == 0 || index + 1 >= tokens.Count || tokens[index].Text != BeamSourcePolicyNames.Close
                || tokens[index + 1].Kind != BeamSourcePolicyNames.FormTerminator)
                throw new InvalidDataException(BeamBuildNames.Invalid);
            string include = literal.ToString();
            var candidates = new List<string>();
            string? selected = null;
            foreach (string directory in new[] { BeamDeclaredPaths.DirectoryName(logical) }.Concat(includeDirectories))
            {
                string candidate = BeamDeclaredPaths.Resolve(directory, include);
                candidates.Add(candidate);
                if (declared.Contains(candidate))
                {
                    selected = candidate;
                    break;
                }
            }
            if (selected is null)
                throw new InvalidDataException(BeamBuildNames.Invalid);
            edges.Add(new(
                logical,
                token.Start,
                include,
                candidates.ToArray(),
                selected
            ));
        }

        return edges.ToArray();
    }
}
