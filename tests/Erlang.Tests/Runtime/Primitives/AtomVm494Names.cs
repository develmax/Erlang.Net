namespace Erlang.Tests;

internal static class AtomVm494Names
{
    internal const string Isolation = "runtime/atom-vm494-isolated-tables-and-async-scopes";
    internal const string Exhaustion = "runtime/atom-vm494-exhaustion-stops-one-vm-and-host-survives";
    internal const string BinaryExhaustion = "runtime/atom-vm494-binary-to-atom1-exhaustion";
    internal const string EncodedExhaustion = "runtime/atom-vm494-binary-to-atom2-exhaustion";
    internal const string Import = "runtime/atom-vm494-host-and-cross-vm-atom-import";
    internal const string BinaryIdentity = "runtime/atom-vm494-name-binary-identity-is-local";
    internal const string SafeDecode = "runtime/atom-vm494-safe-etf-uses-destination-table";
    internal const string Prefix = "atom_vm494_";
    internal const string ExhaustedPrefix = "atom_vm494_exhausted_";
    internal const string Imported = "atom_vm494_imported";
    internal const string Failure = "Logical VM atom-table isolation failed";
    internal const string Erlang = "erlang";
    internal const string Identity = "identity494";
    internal const string AtomToBinary = "atom_to_binary";
    internal const string ListToAtom = "list_to_atom";
    internal const string BinaryToAtom = "binary_to_atom";
    internal const string Utf8 = "utf8";
    internal const string Last = "atom_vm494_last";
    internal const string Warm = "atom_vm494_warm";
    internal const string Module = "atom_vm494";
    internal const string Run = "run";
    internal const string CatchMarker = "atom_vm494_catch";
    internal const string AfterMarker = "atom_vm494_after";
    internal const string SourceName = "atom_vm494.erl";
    internal const string Source = """
        -module(atom_vm494).
        -export([run/2]).
        run(Function, Arguments) ->
            try erlang:apply(erlang, Function, Arguments)
            catch _:_ -> erlang:atom_vm494_catch()
            after erlang:atom_vm494_after()
            end.
        """;
    internal const string Ok = "ok";
    internal const int Capacity = 8192;
    internal const int Processes = 1024;
    internal const int TimeoutSeconds = 8;
}
