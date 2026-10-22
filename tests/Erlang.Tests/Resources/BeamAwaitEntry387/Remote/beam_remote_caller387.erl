-module(beam_remote_caller387).
-export([worker/2]).

worker(Owner, Case) ->
    Outcome = try beam_remote_peer387:hold(Owner, Case) of
        Value -> {returned, Value}
    catch
        Class:Reason:Stack -> {caught, Class, Reason, Stack}
    end,
    Preserved = receive
        {rejected, Case} -> rejected_prefix_preserved
    after 0 ->
        rejected_prefix_missing
    end,
    Owner ! {Case, {Outcome, Preserved}},
    ok.
