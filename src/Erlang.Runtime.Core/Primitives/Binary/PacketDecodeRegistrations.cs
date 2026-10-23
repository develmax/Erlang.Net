namespace Erlang;

internal static class PacketDecodeRegistrations
{
    public static void Register(ModuleRegistry registry) => registry.Register(
        ErlangModuleNames.Module,
        PacketDecodeNames.Function,
        3,
        (_, arguments) => ValueTask.FromResult(PacketDecoder.Decode(arguments[0], arguments[1], arguments[2]))
    );
}
