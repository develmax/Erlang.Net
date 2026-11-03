namespace Erlang.Compiler;

internal sealed class SourceConstantPool820
{
    internal IReadOnlyList<Term> Values { get; }
    internal IReadOnlyList<Pattern.Literal> Patterns { get; }
    internal IReadOnlyList<(object Node, Term Value)> Slots { get; }

    internal SourceConstantPool820(ModuleDefinition module)
    {
        LocalCallValidation.Validate(module);
        var slots = new Dictionary<object, Term>(ReferenceEqualityComparer.Instance);
        var constants = new List<Term>();
        var reader = new SourceClosedConstant820();
        var templates = LocalCallValidation.LiteralTemplates819(
            module,
            (expression, guard) =>
        {
            // RecordGuards may construct new structural nodes. Keep their old
            // leaf pool and path; body constants use exact original AST identity.
            if (guard || expression is not (Expr.Tuple or Expr.List or Expr.Bits)) return false;
            if (slots.ContainsKey(expression)) return true;
            if (!reader.TryRead(expression, out Term value)) return false;
            slots.Add(expression, value);
            constants.Add(value);

            return true;
        }
        );
        Values = templates.Values.Concat(constants)
            .Concat(module.NativeRecords.SelectMany(definition => definition.Fields).Select(field => field.Value).OfType<Term>())
            .Distinct<Term>(ReferenceEqualityComparer.Instance).ToArray();
        Patterns = templates.Patterns;
        Slots = slots.Select(pair => (pair.Key, pair.Value)).ToArray();
    }
}
