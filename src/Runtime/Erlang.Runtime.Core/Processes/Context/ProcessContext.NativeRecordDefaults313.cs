namespace Erlang;

public sealed partial class ProcessContext
{
    internal Term ReadExecutingNativeRecordDefault313(Term template, object? templateOwner)
    {
        if (templateOwner is null || CurrentOwned506 is null) return template;
        for (SourceExecution? current = sourceExecution.Value; current is not null; current = current.Parent)
            if (ReferenceEquals(current.Owner, templateOwner))
                return current.Image.ReadNativeRecordDefault313(template, current.Literals800);

        return template;
    }
}
