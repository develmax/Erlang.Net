using System.Runtime.InteropServices;

namespace Erlang;

// Private, separately versioned exports in the already hash-admitted bridge.
// No public WinDyn callback slot or ErlDrvEntry layout is changed.
internal sealed class ErlDriverChildServices455
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate nint Create(
        nint creator,
        nuint actor,
        nint name,
        nint data
    );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int InternalPid(nuint token);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate uint Abi();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int Configure(ref Callbacks callbacks);

    [StructLayout(LayoutKind.Sequential)]
    private struct Callbacks
    {
        internal uint Abi;
        internal uint Size;
        internal nint Create;
        internal nint InternalPid;
    }

    private readonly Create create;
    private readonly InternalPid internalPid;

    internal ErlDriverChildServices455(
        ErlDriverThreadBridge420 bridge,
        Func<nint, ulong, byte[], nint, nint> createPort,
        ErlDriverTermTokens420 tokens,
        ErlDriverServiceFault420 fault
    )
    {
        create = (
            creator,
            token,
            name,
            data
        ) =>
        {
            try
            {
                if (!ErlDriverPortTurn420.IsCreatingTurn455(creator) || !tokens.TryInternalActor455(token, out ulong actor) || name == nint.Zero)
                    return new nint(-1);
                int length = 0;
                while (length < ErlDriverChildNames455.MaximumNameBytes && Marshal.ReadByte(name, length) != 0) length++;
                if (length == 0 || length == ErlDriverChildNames455.MaximumNameBytes) return new nint(-1);
                var bytes = new byte[length];
                Marshal.Copy(
                    name,
                    bytes,
                    0,
                    length
                );

                return createPort(
                    creator,
                    actor,
                    bytes,
                    data
                );
            }
            catch (Exception failure)
            {
                // Factory transfers data only as its last nonthrowing action.
                // Unexpected pre-transfer failure faults the actual host.
                fault.Record(failure);

                return new nint(-1);
            }
        };
        internalPid = token => tokens.TryInternalActor455(token, out _) ? 1 : 0;
        nint abi = bridge.OptionalExport455(ErlDriverChildNames455.AbiExport);
        nint configure = bridge.OptionalExport455(ErlDriverChildNames455.Configure);
        if (abi == nint.Zero && configure == nint.Zero) return;
        if (abi == nint.Zero || configure == nint.Zero || Marshal.GetDelegateForFunctionPointer<Abi>(abi)() != ErlDriverChildNames455.Abi)
            throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        var callbacks = new Callbacks
        {
            Abi = ErlDriverChildNames455.Abi,
            Size = checked((uint)Marshal.SizeOf<Callbacks>()),
            Create = Marshal.GetFunctionPointerForDelegate(create),
            InternalPid = Marshal.GetFunctionPointerForDelegate(internalPid)
        };
        int result = Marshal.GetDelegateForFunctionPointer<Configure>(configure)(ref callbacks);
        if (result != 0) throw new System.ComponentModel.Win32Exception(result);
    }
}
