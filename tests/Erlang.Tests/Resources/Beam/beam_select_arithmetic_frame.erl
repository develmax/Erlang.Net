-module(beam_select_arithmetic_frame).
-export([route/2]).
-record(entry, {limit, width, method}).

route(#entry{method = first, limit = Limit, width = Width}, Value) ->
    Result = erlang:phash(Value, scale(Width + 1)),
    if
        Result > Limit -> Result - scale(Width);
        true -> Result
    end;
route(#entry{method = second, limit = Limit, width = Width}, Value) ->
    Result = erlang:phash2(Value, scale(Width + 1)) + 1,
    if
        Result > Limit -> Result - scale(Width);
        true -> Result
    end;
route(Old, Value) ->
    Converted = convert(Old),
    route(Converted, Value).

convert({entry, Limit, Width}) ->
    #entry{limit = Limit, width = Width, method = first}.

scale(Width) ->
    1 bsl Width.
