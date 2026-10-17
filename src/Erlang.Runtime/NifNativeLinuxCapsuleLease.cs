namespace Erlang;

internal sealed class NifNativeLinuxCapsuleLease : IDisposable
{
    private readonly object gate = new();
    private readonly NifNativeLinuxNamespace owner;
    private readonly FileStream file;
    private readonly FileStream? experimentalTargetCopy;
    private nint pointer;
    internal nint Pointer => pointer;

    internal NifNativeLinuxCapsuleLease(NifNativeLinuxNamespace owner, FileStream file, nint pointer, FileStream? experimentalTargetCopy = null)
    {
        this.owner = owner;
        this.file = file;
        this.pointer = pointer;
        this.experimentalTargetCopy = experimentalTargetCopy;
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (pointer == 0)
                return;
            try
            {
                NifNativePlatformLoader.Close(pointer);
            }
            catch
            {
                NifNativeLinuxNamespaceRetention.Retain(this);
                throw;
            }
            pointer = 0;
            try
            {
                file.Dispose();
            }
            finally
            {
                try
                {
                    experimentalTargetCopy?.Dispose();
                }
                finally
                {
                    owner.ReleaseCapsule();
                }
            }
        }
    }
}
