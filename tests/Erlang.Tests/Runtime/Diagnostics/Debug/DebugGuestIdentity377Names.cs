using System.Reflection;

namespace Erlang.Tests;

internal static class DebugGuestIdentity377Names
{
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    internal const string GuestModule = "debug_guest_identity377";
    internal const string SourceMethod = "source";
    internal const string ExportMethod = "export";
    internal const string ExportModuleToken = "debug377_export_owner_atom";
    internal const string NestedMethod395 = "export_nested";
    internal const string NestedKey395 = "debug395_nested";
    internal const string CounterMethod = "counter";
    internal const string CopyParameter = "copy_parameter";
    internal const string OverrideMethod = "override_copy";
    internal const string ClearMethod = "clear";
    internal const string SourceKey = "debug377_source";
    internal const string ExportKey = "debug377_export";
    internal const string CounterKey = "debug377_counter";
    internal const string OverrideKey = "debug377_override";
    internal const string Data = "data";
    internal const string Gate = "gate";
    internal const string TupleStorage = "OwnedStorage506";
    internal const string FunctionStorage = "OwnedFunctionStorage";
    internal const string Owner = "Owner";
    internal const string Domain = "LiteralDomain800";
    internal const string Heap = "TermHeap506";
    internal const string Collect = "CollectGeneration508";
    internal const string CurrentInvocation = "CurrentOwned506";
    internal const string Kind = "BindingKind507";
    internal const string Opaque = "Opaque";
    internal const string PortableType = "Erlang.PortableControlTerm826";
    internal const string Capture = "Capture";
    internal const string Failure = "Debug377 genuine guest identity, portable boundary or copied resource lifetime differs.";
    internal const string Source = """
        -module(debug_guest_identity377).
        -export([source/0,export/1,export_nested/2,counter/1,copy_parameter/1,override_copy/0,clear/0]).
        source() ->
            L=beam_debug_sharing425:literal(),
            S=erts_debug:copy_shared(L,false), C=erts_debug:copy_shared(L,true),
            F=beam_debug_sharing425:closure(), FC=erts_debug:copy_shared(F,true),
            O=F(), R=FC(), put(debug377_source,{L,S,C,F,FC,O,R}),
            erlang:garbage_collect(),
            {erts_debug:same(L,S),not erts_debug:same(L,C),C=:=L,
             erts_debug:size_shared(L),not erts_debug:same(F,FC),F=:=FC,
             not erts_debug:same(O,R),erts_debug:same(element(1,R),element(2,R)),R=:=O}.
        export(_M) ->
            L=debug377_export_owner_atom:run(), S=erts_debug:copy_shared(L,false), C=erts_debug:copy_shared(L,true),
            put(debug377_export,{L,S,C}), erlang:garbage_collect(),
            {erts_debug:same(L,S),not erts_debug:same(L,C),L=:=C,
             erts_debug:flat_size(L),erts_debug:size_shared(L),
             erts_debug:same(element(1,L),element(2,L)),
             not erts_debug:same(element(1,C),element(2,C)),erts_debug:size_shared(C)}.
        export_nested(Fresh,Copy) ->
            G=fun erlang:self/0,
            F=case Fresh of true -> erts_debug:copy_shared(G,true); false -> G end,
            L={F,F}, C=erts_debug:copy_shared(L,Copy),
            put(debug395_nested,{L,C}), erlang:garbage_collect(),
            A=element(1,C), B=element(2,C),
            {erts_debug:flat_size(L),erts_debug:size_shared(L),
             erts_debug:same(L,C),erts_debug:same(A,B),erts_debug:size_shared(C),
             erts_debug:flat_size(A),erts_debug:size_shared(A),
             C=:=L,A()=:=self(),B()=:=self()}.
        counter(R) ->
            F=fun()->{R,R} end, FC=erts_debug:copy_shared(F,true), C=FC(),
            put(debug377_counter,{FC,C}), erlang:garbage_collect(),
            {not erts_debug:same(F,FC),erts_debug:same(element(1,C),element(2,C)),C=:={R,R}}.
        copy_parameter(F) -> erts_debug:copy_shared(F,true).
        override_copy() ->
            put(debug377_override,erts_debug:copy_shared({debug_guest_identity377,42},false)),ok.
        clear() -> erase(debug377_source),erase(debug377_export),erase(debug377_counter),erase(debug377_override),ok.
        """;
}
