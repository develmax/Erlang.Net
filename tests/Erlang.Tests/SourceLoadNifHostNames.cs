using System.Reflection;

namespace Erlang.Tests;

internal static class SourceLoadNifHostNames
{
    internal const string Test = "runtime/direct-source-load-nif-exact-caller-capability";
    internal const string Broken = "Direct source load_nif caller capability contract failed";
    internal const string Install = "InstallSourceLoadNifHandler";
    internal const string Consume = "Consume";
    internal const string Function = "load_nif";
    internal const string Erlang = "erlang";
    internal const string Module = "source_load_nif_host";
    internal const string Other = "source_load_nif_other";
    internal const string Direct = "direct";
    internal const string Last = "last";
    internal const string NonTail = "non_tail";
    internal const string Cross = "cross";
    internal const string Dynamic = "dynamic";
    internal const string Reference = "reference";
    internal const string Local = "local";
    internal const string Arguments = "arguments";
    internal const string ArgumentDone = "argument_done";
    internal const string OldCode = "OldCode";
    internal const string NoExecutingCaller = "NoExecutingCaller";
    internal const string PendingResource = ".BeamOnLoad.fresh_success.new.beam_on_load_fresh_success.beam";
    internal const string PendingImage = "CodeImage";
    internal const string Kill = "kill";
    internal const string Error = "error";
    internal const string BadLibrary = "bad_lib";
    internal const string ApplyRejectedMessage = "load_nif/2 must be explicitly called from the NIF module. It cannot be called through apply/3.";
    internal const string Source = "-module(source_load_nif_host). -export([direct/1,last/1,non_tail/1,cross/1,dynamic/1,reference/1,local/1,arguments/0]). direct(X)->erlang:load_nif(X,42). last(X)->Y=X,erlang:load_nif(Y,42). non_tail(X)->{erlang:load_nif(X,42)}. cross(X)->source_load_nif_other:direct(X). dynamic(X)->erlang:apply(erlang,load_nif,[X,42]). reference(X)->F=fun erlang:load_nif/2,F(X,42). local(X)->load_nif(X,42). load_nif(X,_)->X. arguments()->erlang:load_nif(argument(),42). argument()->erlang:put(argument_done,true),17.";
    internal const string OtherSource = "-module(source_load_nif_other). -export([direct/1]). direct(X)->erlang:load_nif(X,42).";
    internal const int Arity = 2;
    internal const int Marker = 42;
    internal const int Value = 17;
    internal const int OwnedDirectReferences = 2;
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
}
