using Erlang.Compiler;

namespace Erlang.BeamBuild;

internal static class BeamBuildSources
{
    internal static byte[] Snapshot(BeamBuildSource source)
    {
        byte[] bytes = File.ReadAllBytes(source.Path);
        List<Token> tokens = Lexer.Scan(BeamSourceText.Read(bytes));
        bool beginning = true;
        int modules = 0;
        for (int index = 0; index < tokens.Count; index++)
        {
            Token token = tokens[index];
            if (token.Kind == BeamSourcePolicyNames.Symbol && token.Text == BeamSourcePolicyNames.Macro)
                throw new InvalidDataException(BeamBuildNames.Invalid);
            if (token.Kind == BeamSourcePolicyNames.FormTerminator)
            {
                beginning = true;
                continue;
            }
            if (beginning && token.Kind == BeamSourcePolicyNames.Symbol && token.Text == BeamSourcePolicyNames.Attribute)
            {
                if (index + 2 >= tokens.Count || !IsAtom(tokens[index + 1])
                    || tokens[index + 1].Text is not (BeamSourcePolicyNames.Module or BeamSourcePolicyNames.Export)
                    || tokens[index + 2].Text != BeamSourcePolicyNames.Open)
                    throw new InvalidDataException(BeamBuildNames.Invalid);
                if (tokens[index + 1].Text == BeamSourcePolicyNames.Module)
                {
                    if (index + 4 >= tokens.Count || !IsAtom(tokens[index + 3])
                        || tokens[index + 3].Text != source.Module || tokens[index + 4].Text != BeamSourcePolicyNames.Close)
                        throw new InvalidDataException(BeamBuildNames.Invalid);
                    modules++;
                }
            }
            beginning = false;
        }
        if (modules != 1)
            throw new InvalidDataException(BeamBuildNames.Invalid);

        return bytes;
    }

    private static bool IsAtom(Token token)
        => token.Kind is BeamSourcePolicyNames.Atom or BeamSourcePolicyNames.QuotedAtom;
}
