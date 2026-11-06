using Erlang.Otp;

namespace Erlang;

/// <summary>Selectable managed implementation of OTP 29.1.1 erpc.</summary>
public static class ErpcLibrary
{
    public const string Name = ErpcLibraryModuleNames.Module;

    public static IReadOnlyList<(string Name, int Arity)> Exports { get; } = Array.AsReadOnly(new (string, int)[]
    {
        (ErpcNames.Call, 2),
        (ErpcNames.Call, 3),
        (ErpcNames.Call, 4),
        (ErpcNames.Call, 5),
        (ErpcNames.CastPublic, 2),
        (ErpcNames.CastPublic, 4),
        (ErpcNames.SendRequest, 2),
        (ErpcNames.SendRequest, 4),
        (ErpcNames.SendRequest, 6),
        (ErpcNames.ReceiveResponse, 1),
        (ErpcNames.ReceiveResponse, 2),
        (ErpcNames.ReceiveResponse, 3),
        (ErpcNames.WaitResponse, 1),
        (ErpcNames.WaitResponse, 2),
        (ErpcNames.WaitResponse, 3),
        (ErpcNames.CheckResponse, 2),
        (ErpcNames.CheckResponse, 3),
        (ErpcNames.Multicall, 2),
        (ErpcNames.Multicall, 3),
        (ErpcNames.Multicall, 4),
        (ErpcNames.Multicall, 5),
        (ErpcNames.Multicast, 2),
        (ErpcNames.Multicast, 4),
        (ErpcNames.RequestIdsNew, 0),
        (ErpcNames.RequestIdsSize, 1),
        (ErpcNames.RequestIdsAdd, 3),
        (ErpcNames.RequestIdsToList, 1),
        (ErpcNames.Execute, 3),
        (ErpcNames.Execute, 4),
        (ErpcNames.Cast, 3),
        (ErpcNames.IsArgumentError, 4),
        (ErpcNames.TrimStack, 4),
        (ErpcNames.CallResult, 4)
    });

    public static void Register(ModuleRegistry registry)
        => registry.InstallPreloaded(Otp.Erpc.Register);
}
