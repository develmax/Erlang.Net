using System.Text;

namespace Erlang.Compiler;

internal static class BeamDocumentationMetadata
{
    private const int PrettyPrintLineWidth = 72;

    internal static void Merge(Dictionary<Term, Term> destination, DocumentationSourceForm form)
    {
        if (form.Value is not MapTerm metadata)
        {
            return;
        }

        bool module = form.Name == ModuleAttributeNames.ModuleDoc;
        foreach (var (key, value) in metadata.Entries)
        {
            destination[key] = key switch
            {
                Atom { Name: "authors" } when module => Cons.From(Cons.Items(value).Select(Text)),
                Atom { Name: "equiv" } when !module && value is TupleTerm { Items: [Atom name, Integer arity] }
                    => Binary(FormattableString.Invariant($"{BeamDocumentationWriter.FormatFunctionName(name.Name)}/{arity.Value}")),
                Atom { Name: "since" or "deprecated" } => Text(value),
                Atom { Name: "format" } when module => Text(value),
                Atom { Name: "group" or "equiv" } when !module => Text(value),
                _ => value
            };
        }
        if (!module && form.EquivalentCall is { } call)
        {
            destination[Term.A("equiv")] = Binary(Format(call));
        }
    }

    internal static Term Entry(
        Dictionary<Term, Term> metadata,
        Dictionary<Term, Term> moduleMetadata,
        Term location,
        bool? exported = null
    )
    {
        if (moduleMetadata.TryGetValue(Term.A("since"), out Term? since))
        {
            metadata.TryAdd(Term.A("since"), since);
        }
        metadata[Term.A("source_anno")] = location;
        if (exported is bool isExported)
        {
            metadata[Term.A("exported")] = Term.A(isExported ? "true" : "false");
        }
        else
        {
            metadata.Remove(Term.A("exported"));
        }

        return new MapTerm(metadata);
    }

    private static BitString Text(Term value) => Binary(BeamDocumentationText.ReadText(value));

    private static BitString Binary(string value) => new(Encoding.UTF8.GetBytes(value));

    private static string Format(Expr expression, int indent = 0) => expression switch
    {
        Expr.Call { DynamicRemote: false } call => FormatCall(call, indent),
        Expr.Variable variable => variable.Name,
        Expr.FunctionReference reference => FormattableString.Invariant(
            $"fun {BeamDocumentationWriter.FormatFunctionName(reference.Name)}/{reference.Arity}"),
        Expr.Literal { IsCharacterLiteral: true, Value: Integer character } =>
            character.Value == ' ' ? "$ " : BeamMacroStringification.Character((int)character.Value),
        Expr.Binary { Operator: ErlangOperators.Plus or ErlangOperators.Minus or ErlangOperators.Multiply or ErlangOperators.Divide } binary =>
            FormatArithmetic(binary, indent),
        Expr.Literal { Value: Atom atom } => BeamDocumentationWriter.FormatFunctionName(atom.Name),
        Expr.Literal { Value: Integer integer } => integer.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
        Expr.Literal { Value: Cons text } => BeamMacroStringification.Quote(BeamDocumentationText.ReadText(text), '"'),
        Expr.Literal { Value: Nil } => "[]",
        Expr.Unary { Operator: ErlangOperators.Minus or ErlangOperators.Plus, Operand: Expr.Literal { Value: Integer } } unary
            => unary.Operator + Format(unary.Operand),
        Expr.Tuple tuple => "{" + string.Join(", ", tuple.Items.Select(item => Format(item))) + "}",
        Expr.List list => "[" + string.Join(", ", list.Items.Select(item => Format(item)))
            + (list.Tail is null ? string.Empty : " | " + Format(list.Tail)) + "]",
        Expr.Map { Base: null } map => "#{" + string.Join(", ", map.Fields.Select(field =>
            Format(field.Key) + (field.Exact ? " := " : " => ") + Format(field.Value))) + "}",
        Expr.Fun { SelfName: null, Clauses: [{ Guard: null } clause] } => FormatFun(clause, indent),
        _ => throw new NotSupportedException(BeamEmissionFormat.Unsupported)
    };

    private static string FormatArithmetic(Expr.Binary binary, int indent)
    {
        int precedence = Parser.Precedence(binary.Operator);

        return Operand(binary.Left, precedence) + " " + binary.Operator + " " + Operand(binary.Right, precedence + 1);

        string Operand(Expr expression, int minimum)
        {
            string text = Format(expression, indent);
            return expression is Expr.Binary child && Parser.Precedence(child.Operator) < minimum
                ? "(" + text + ")"
                : text;
        }
    }

    private static string FormatCall(Expr.Call call, int indent)
    {
        bool omitModule = call.Module is null || call.Module == ErlangModuleNames.Module
            && call.Function != AutoImportNames.Float && AutoImportSignatures.Contains(call.Function, call.Arguments.Count);
        string name = omitModule ? string.Empty : BeamDocumentationWriter.FormatFunctionName(call.Module!) + ":";
        name += BeamDocumentationWriter.FormatFunctionName(call.Function);
        var result = new StringBuilder(name).Append('(');
        int argumentIndent = indent + name.Length + 1;
        bool wrapAtomicArguments = call.Arguments.All(argument => argument is Expr.Variable
            or Expr.Literal { Value: Atom or Nil }
            or Expr.List { Items.Count: 0, Tail: null }
            or Expr.Literal { Value: Integer, IsCharacterLiteral: false });
        int continuationColumn = indent + name.EnumerateRunes().Count() + 1;
        int column = continuationColumn;
        for (int index = 0; index < call.Arguments.Count; index++)
        {
            Expr argument = call.Arguments[index];
            string text = Format(argument, argumentIndent);
            int width = wrapAtomicArguments ? text.EnumerateRunes().Count() + 1 : 0;
            if (index != 0)
            {
                result.Append(',');
                if (argument is Expr.Fun || wrapAtomicArguments && column + 1 + width > PrettyPrintLineWidth)
                {
                    result.Append('\n').Append(' ', wrapAtomicArguments ? continuationColumn : argumentIndent);
                    column = continuationColumn;
                }
                else
                {
                    result.Append(' ');
                    column++;
                }
            }

            result.Append(text);
            column += width;
        }

        return result.Append(')').ToString();
    }

    private static string FormatFun(Clause clause, int indent)
    {
        string arguments = string.Join(", ", clause.Patterns.Select(pattern => pattern is Pattern.Variable variable
            ? variable.Name
            : throw new NotSupportedException(BeamEmissionFormat.Unsupported)));
        const int bodyIndent = 7;
        return "fun(" + arguments + ") ->\n" + new string(' ', indent + bodyIndent)
            + Format(clause.Body, indent + bodyIndent) + "\n" + new string(' ', indent) + "end";
    }
}
