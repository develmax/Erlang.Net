namespace Erlang;

public sealed partial class ProcessContext
{
    internal PreparedBeamImage? CaptureBeamLoaderSource()
    {
        for (SourceExecution? executing = sourceExecution.Value; executing is not null; executing = executing.Parent)
            if (executing.Image.ExecutionOwner is PreparedBeamImage image
                && ReferenceEquals(image.Owner, Runtime.Modules)
                && IsExecutingCode(executing.Image))
                return image;

        return null;
    }
}
