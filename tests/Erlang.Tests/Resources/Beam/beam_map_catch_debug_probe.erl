-module(beam_map_catch_debug_probe).
-export([update/2, handle/1]).
update(Map, Value) -> (Map)#{first => Value}#{first := ok}.
handle(F) ->
    try F()
    catch
        {bad, {Reason, Detail}} -> {Reason, Detail};
        error:{bad, Reason} -> Reason;
        exit:Reason:Stack -> {Reason, Stack}
    end.
