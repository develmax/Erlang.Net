using System.Reflection;

namespace Erlang.Tests;

internal static class NifSourceEntryHostNames
{
    internal const string Test = "runtime/nif-source-named-entry-routing-and-owned-drain";
    internal const string Module = "nif_source_entry_host";
    internal const string Target = "target";
    internal const string Plain = "plain";
    internal const string Table = "NifBindings";
    internal const string Begin = "Begin";
    internal const string Publish = "Publish";
    internal const string Retire = "RetireAsync";
    internal const string Store = "Erlang.NifImageBindingStore";
    internal const string IsCreated = "IsCreated";
    internal const string Broken = "Named source NIF entry contract failed";
    internal const string Kill = "kill";
    internal const string Source = "-module(nif_source_entry_host). -export([target/1,plain/0,tail/1,local/1,named/1,external/1,anonymous/1,remote/1]). target(no)->erlang:error(stub). plain()->17. tail(X)->target(X). local(X)->{target(X)}. named(X)->F=fun target/1,F(X). external(X)->F=fun nif_source_entry_host:target/1,F(X). anonymous(X)->F=fun(Y)->target(Y) end,F(X). remote(X)->nif_source_entry_host:target(X).";
    internal static readonly string[] Routes = [Target, "tail", "named", "external", "anonymous", "remote"];
    internal const string Local = "local";
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic;
    internal const BindingFlags StaticMembers = BindingFlags.Static | BindingFlags.NonPublic;
    internal const int Argument = 73;
    internal const int Replacement = 91;
    internal const int PlainValue = 17;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
}
