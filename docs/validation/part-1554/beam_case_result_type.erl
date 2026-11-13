-module(beam_case_result_type).
-export([run/1]).
run(Input) ->
    case transform(Input) of
        missing -> Input;
        Result -> Result
    end.
transform([]) -> missing;
transform([last]) -> [];
transform([Head|Tail]) ->
    case transform(Tail) of
        missing -> missing;
        Items -> [Head|Items]
    end.
