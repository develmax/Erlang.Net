namespace Erlang;

public sealed partial class ProcessContext
{
    internal Term ReadExecutingModuleLiteral819(Term template, object? templateOwner)
    {
        if (templateOwner is null) return template;
        for (SourceExecution? current = sourceExecution.Value; current is not null; current = current.Parent)
            if (ReferenceEquals(current.Owner, templateOwner))
            {
                if (current.Image.SourceLiteralView819 is not { } view) return template;
                ModuleLiteralLease800 lease = current.Literals800 ?? throw new InvalidOperationException(SourceLiteral819Names.Invalid);

                return view.Read819(template, current.Image, lease);
            }

        return template;
    }

    internal Term ReadExecutingPatternLiteral819(Pattern.Literal pattern)
    {
        for (SourceExecution? current = sourceExecution.Value; current is not null; current = current.Parent)
            if (current.Image.SourceLiteralView819 is { } view && view.ContainsPattern819(pattern))
            {
                ModuleLiteralLease800 lease = current.Literals800 ?? throw new InvalidOperationException(SourceLiteral819Names.Invalid);

                return view.Read819(pattern.Value, current.Image, lease);
            }

        // Native mailbox/runtime patterns remain dynamic data; only exact
        // immutable Source AST nodes participate in the code literal pool.
        return pattern.Value;
    }

    internal Term ReadExecutingSourceLiteral819(Term template)
    {
        for (SourceExecution? current = sourceExecution.Value; current is not null; current = current.Parent)
            if (current.Image.SourceLiteralView819 is { } view)
            {
                ModuleLiteralLease800 lease = current.Literals800 ?? throw new InvalidOperationException(SourceLiteral819Names.Invalid);

                return view.Read819(template, current.Image, lease);
            }

        // Direct expression evaluation and legacy standalone metadata producers
        // have no admitted module area. They gain no native literal authority.
        return template;
    }
}
