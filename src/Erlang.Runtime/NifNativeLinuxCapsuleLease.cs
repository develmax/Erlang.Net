namespace Erlang;

internal sealed class NifNativeLinuxCapsuleLease : IDisposable
{
    private readonly object gate = new();
    private readonly NifNativeLinuxNamespace owner;
    private readonly FileStream file;
    private nint pointer;
    internal nint Pointer => pointer;

    internal NifNativeLinuxCapsuleLease(NifNativeLinuxNamespace owner, FileStream file, nint pointer)
    {
        this.owner = owner;
        this.file = file;
        this.pointer = pointer;
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
                owner.ReleaseCapsule();
            }
        }
    }
}
