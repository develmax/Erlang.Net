-module(catch_result_dispatch_probe).
-export([run/2]).
run(Mode, Payload) ->
    prepare(Mode),
    Result = case (catch work(Mode, Payload)) of
        ok -> ok;
        {error, Reason} -> {error, Reason};
        {'EXIT', {noproc, _}} -> {error, closed};
        {'EXIT', Reason} -> {error, Reason};
        Other -> {error, Other}
    end,
    Result.
prepare(_) -> ok.
work(ok, _) -> ok;
work(fail, Payload) -> {error, Payload};
work(exit, Payload) -> exit(Payload);
work(throw, Payload) -> throw(Payload);
work(value, Payload) -> Payload.
