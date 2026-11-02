using System.Reflection;

namespace Erlang.Tests;

internal static class VmConstantPool357Names
{
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    internal const string VmPool380 = "vmConstants357";
    internal const string CurrentOwned380 = "CurrentOwned506";
    internal const string BindingKind380 = "BindingKind507";
    internal const string CanBorrow380 = "CanBorrow507";
    internal const string Opaque380 = "Opaque";
    internal const string AuthorityDiagnostic380 = "VM_CONSTANT380 private={0} kind={1} parentCanBorrow={2} effectiveBorrow=false";
    internal const string OverrideSource380 = "-module(vm_constant_pool357). -export([override/1]). override(B)->atom_to_binary(B).";
    internal const string TestModule = "vm_constant_pool357";
    internal const string Keep = "keep";
    internal const string Clear = "clear";
    internal const string Override = "override";
    internal const string OverrideKey = "vm357_override";
    internal const string AtomToBinary = "atom_to_binary";
    internal const string Erlang = "erlang";
    internal const string Abc = "abc";
    internal const string Binary = "vm357_binary";
    internal const string Empty = "vm357_empty";
    internal const string Info = "vm357_info";
    internal const string CrossInfo = "vm357_cross_info";
    internal const string Again = "vm357_again";
    internal const string Data = "data";
    internal const string Gate = "gate";
    internal const string OwnedBinary = "OwnedHeapBits";
    internal const string OwnedMap = "OwnedMapPayload517";
    internal const string RootPayload = "RootPayload";
    internal const string Owner = "Owner";
    internal const string MetadataModule = "module";
    internal const string Formatter = "erl_erts_errors";
    internal const string Domain = "LiteralDomain800";
    internal const string Retirement = "Retirement800";
    internal const string Failure = "VM constant357 actual native identity, isolation or retirement failed.";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
    internal const string Source = """
        -module(vm_constant_pool357).
        -export([keep/0,clear/0,override/0]).
        keep() ->
            B=atom_to_binary(abc), put(vm357_binary,B),
            put(vm357_empty,atom_to_binary('')),
            put(vm357_info,capture(atom_to_binary)),
            erlang:garbage_collect(),
            put(vm357_again,atom_to_binary(abc)),
            put(vm357_cross_info,capture(binary_to_atom)),
            ok.
        capture(F) ->
            try apply(erlang,F,[123])
            catch error:_:S ->
                find(element(4,hd(S)))
            end.
        find([{error_info,I}|_]) -> I;
        find([_|T]) -> find(T).
        override() -> put(vm357_override,atom_to_binary(abc)),ok.
        clear() ->
            erase(vm357_binary),erase(vm357_empty),erase(vm357_info),
            erase(vm357_cross_info),erase(vm357_again),erase(vm357_override),
            erlang:garbage_collect(),ok.
        """;
}
