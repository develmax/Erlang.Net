namespace Erlang.Tests;

internal static class ListLengthHostNames
{
    internal const int Cells = 64017;
    internal const int TrailingBits = 3;
    internal const int BitsPerByte = 8;
    internal const int ByteRounding = BitsPerByte - 1;
    internal const string Ordsets = "ordsets";
    internal const string Size = "size";
    internal const string ImproperTail = "length_late_improper_tail";
    internal const string Module = "length_host_guard";
    internal const string Run = "run";
    internal const string ReceiveSelectingMethod = "ReceiveSelectingAsync";
    internal const string Receive = "receive_length";
    internal const string MapKey = "map_key";
    internal const string BitSize = "bit_size";
    internal const string Source = "-module(length_host_guard). -export([run/1,receive_length/1,map_key/2,bit_size/2]). run(L) when length(L) > 0 -> ok; run(_) -> fallback. receive_length(L)->self()!L,receive priority->{priority,receive _->retained after 0->lost end};X when length(X)>0->ok end. map_key(L,M)->#{(length(L)):=V}=M,V. bit_size(L,B)-><<_:(length(L)),_:3>>=B,ok.";
    internal const string Failure = "Cooperative length traversal contract failed";
    internal const string Kill = "kill";
    internal const string Killed = "killed";
    internal const string Ok = "ok";
    internal const string Normal = "normal";
    internal const string Priority = "priority";
    internal const string Retained = "retained";
    internal const string Lost = "lost";
    internal static readonly TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(1);
    internal static readonly TimeSpan SuspensionObservation = TimeSpan.FromSeconds(2);
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
