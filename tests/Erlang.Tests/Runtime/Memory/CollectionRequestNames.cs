namespace Erlang.Tests;

internal static class CollectionRequestNames
{
    internal const string OrdinaryBeamCase = "runtime/statistics/ordinary-beam-target-safe-collection-requests";
    internal const string OwnedBeamCase = "runtime/statistics/owned-beam-target-safe-collection-requests";
    internal const string BeamResource = "Erlang.Tests.Statistics.CollectionRequests.collection_request_protocol.beam";
    internal const string OutcomeResource = "Erlang.Tests.Statistics.CollectionRequests.observed.etf";
    internal const string BeamSha256 = "D1D66513F3BDE2A4705815737DD1DFDB179A621B3F50F5B21EC1CEAE859E45CB";
    internal const string OutcomeSha256 = "7BB2546BA272F53CCFCE32FF8482857929B793BDEF2AE050F92A02A3B4BFF43E";
    internal const string OrdinaryCase = "runtime/statistics/ordinary-source-target-safe-collection-requests";
    internal const string OwnedCase = "runtime/statistics/owned-source-target-safe-collection-requests";
    internal const string ModuleMacro = "?MODULE";
    internal const string GuestModule = "collection_request_protocol";
    internal const string Entry = "run";
    internal const string Returned = "returned";
    internal const string Failure = "Actual target collection protocol or completed VM collection count differs.";
    internal const string Source = """
        -module(collection_request_protocol).
        -export([run/0, waiting/1, busy/1]).
        
        capture(F) -> try {returned,F()} catch C:R:S -> {raised,C,R,S} end.
        
        run() ->
            Parent=self(),
            {Waiting,WM}=spawn_monitor(?MODULE,waiting,[Parent]),
            receive {ready,Waiting} -> ok after 5000 -> error(waiting_start) end,
            {Busy,BM}=spawn_monitor(?MODULE,busy,[Parent]),
            receive {ready,Busy} -> ok after 5000 -> error(busy_start) end,
            try
                SelfId={self_async,make_ref(),[one,two]},
                Self=capture(fun()->_Accepted=garbage_collect(self(),[{async,SelfId}]),
                             receive {garbage_collect,SelfId,Result}->{_Accepted,Result} after 5000->error(self_timeout) end end),
                Wait=capture(fun()->garbage_collect(Waiting,[{type,minor}]) end),
                Id={target_async,make_ref(),#{key=>value}},
                BusyResult=capture(fun()->_Accepted=garbage_collect(Busy,[{async,Id},{type,major}]),
                             receive {garbage_collect,Id,Result}->{_Accepted,Result} after 5000->error(busy_timeout) end end),
                {Self,Wait,BusyResult}
            after
                exit(Waiting,kill), exit(Busy,kill),
                receive {'DOWN',WM,process,Waiting,_}->ok after 5000->error(wait_cleanup) end,
                receive {'DOWN',BM,process,Busy,_}->ok after 5000->error(busy_cleanup) end
            end.
        
        waiting(Parent)->put(kept,{make_ref(),[1,2,3]}),Parent!{ready,self()},receive stop->ok end.
        busy(Parent)->put(kept,{make_ref(),[1,2,3]}),Parent!{ready,self()},loop().
        loop()->receive stop->ok after 0->loop() end.
        """;
}
