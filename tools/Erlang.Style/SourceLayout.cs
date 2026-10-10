using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;

public static class SourceLayout
{
    private const int ManyArguments = 4;
    private const int LongListWidth = 120;
    private static readonly AdhocWorkspace Workspace = new(MefHostServices.Create(MefHostServices.DefaultAssemblies.Add(typeof(CSharpFormattingOptions).Assembly)));

    public static string Apply(string source)
    {
        for (int pass = 0; pass < 32; pass++)
        {
            string formatted = ApplyPass(source);
            if (formatted == source)
                return formatted;

            source = formatted;
        }

        throw new InvalidOperationException(StyleDiagnostics.UnstableLayout);
    }

    private static string ApplyPass(string source)
    {
        source = source.Replace("\r\n", "\n").TrimStart(
            ' ',
            '\t',
            '\r',
            '\n'
        );
        var root = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview)).GetRoot();
        var insertions = new Dictionary<int, int>();

        void BreakBefore(SyntaxToken token, int requiredNewlines)
        {
            int anchor = token.SpanStart;
            var comment = token.LeadingTrivia.FirstOrDefault(trivia => !trivia.IsKind(SyntaxKind.WhitespaceTrivia) && !trivia.IsKind(SyntaxKind.EndOfLineTrivia));
            if (comment != default)
                anchor = comment.FullSpan.Start;

            int previous = anchor - 1;
            int newlines = 0;
            while (previous >= 0 && char.IsWhiteSpace(source[previous]))
            {
                if (source[previous] == '\n')
                    newlines++;
                previous--;
            }
            if (previous < 0 || newlines >= requiredNewlines)
                return;

            int position = previous + 1;
            insertions[position] = Math.Max(insertions.GetValueOrDefault(position), requiredNewlines - newlines);
        }

        foreach (var block in root.DescendantNodes().OfType<BlockSyntax>())
        {
            if (block.Statements.Count == 0)
                continue;

            BreakBefore(block.OpenBraceToken, 1);
            for (int index = 0; index < block.Statements.Count; index++)
            {
                var statement = block.Statements[index];
                int lines = index > 0 && statement is ReturnStatementSyntax or LocalFunctionStatementSyntax ? 2 : 1;
                BreakBefore(statement.GetFirstToken(), lines);
            }
            BreakBefore(block.CloseBraceToken, 1);
        }

        foreach (var section in root.DescendantNodes().OfType<SwitchSectionSyntax>())
        {
            for (int index = 1; index < section.Statements.Count; index++)
            {
                if (section.Statements[index] is ReturnStatementSyntax)
                    BreakBefore(section.Statements[index].GetFirstToken(), 2);
            }
        }

        foreach (var expression in root.DescendantNodes().OfType<SwitchExpressionSyntax>())
        {
            BreakBefore(expression.OpenBraceToken, 1);
            foreach (var arm in expression.Arms)
                BreakBefore(arm.GetFirstToken(), 1);
            BreakBefore(expression.CloseBraceToken, 1);
        }

        foreach (var type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            for (int index = 1; index < type.Members.Count; index++)
            {
                var member = type.Members[index];
                if (member is BaseMethodDeclarationSyntax || type.Members[index - 1] is BaseMethodDeclarationSyntax)
                    BreakBefore(member.GetFirstToken(), 2);
            }
        }

        foreach (var declaration in root.DescendantNodes().OfType<EnumDeclarationSyntax>())
        {
            foreach (var member in declaration.Members)
                BreakBefore(member.GetFirstToken(), 1);
            BreakBefore(declaration.CloseBraceToken, 1);
        }

        foreach (var list in root.DescendantNodes().OfType<ParameterListSyntax>())
        {
            if (NeedsWrapping(list.Parameters.Count, list))
            {
                foreach (var parameter in list.Parameters)
                    BreakBefore(parameter.GetFirstToken(), 1);
                BreakBefore(list.CloseParenToken, 1);
            }
        }

        foreach (var list in root.DescendantNodes().OfType<ArgumentListSyntax>())
        {
            if (NeedsWrapping(list.Arguments.Count, list))
            {
                foreach (var argument in list.Arguments)
                    BreakBefore(argument.GetFirstToken(), 1);
                BreakBefore(list.CloseParenToken, 1);
            }
        }

        foreach (var insertion in insertions.OrderByDescending(pair => pair.Key))
            source = source.Insert(insertion.Key, new string('\n', insertion.Value));

        source = IndentWrappedLists(source);
        source = Formatter.Format(CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview)).GetRoot(), Workspace).ToFullString().Replace("\r\n", "\n");

        return source.EndsWith('\n') ? source : source + "\n";
    }

    private static bool NeedsWrapping(int count, SyntaxNode list) => count >= ManyArguments || count > 1 && list.DescendantTokens().Sum(token => token.Text.Length) > LongListWidth;

    private static string IndentWrappedLists(string source)
    {
        var root = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview)).GetRoot();
        var edits = new Dictionary<int, (int Length, string Text)>();

        void Indent(SyntaxToken token, int spaces)
        {
            int start = source.LastIndexOf('\n', Math.Max(0, token.SpanStart - 1)) + 1;
            string prefix = source[start..token.SpanStart];
            if (prefix.Any(character => !char.IsWhiteSpace(character)))
                return;

            string desired = new(' ', spaces);
            if (prefix != desired)
                edits[start] = (prefix.Length, desired);
        }

        foreach (var list in root.DescendantNodes().Where(node => node is ParameterListSyntax or ArgumentListSyntax))
        {
            var items = list is ParameterListSyntax parameters
                ? parameters.Parameters.Cast<SyntaxNode>().ToArray()
                : ((ArgumentListSyntax)list).Arguments.Cast<SyntaxNode>().ToArray();
            if (!NeedsWrapping(items.Length, list))
                continue;

            int start = source.LastIndexOf('\n', Math.Max(0, list.SpanStart - 1)) + 1;
            int indentation = source[start..list.SpanStart].TakeWhile(character => character is ' ' or '\t').Sum(character => character == '\t' ? 4 : 1);
            foreach (var item in items)
                Indent(item.GetFirstToken(), indentation + 4);
            Indent(list.GetLastToken(), indentation);
        }

        foreach (var edit in edits.OrderByDescending(pair => pair.Key))
            source = source.Remove(edit.Key, edit.Value.Length).Insert(edit.Key, edit.Value.Text);

        return source;
    }
}
