-module(beam_leading_projection).
-export([run/1]).

-record(wrapped, {unused, def}).
-record(seq, {context, ignored, extension, fields}).
-record(set, {context, a, b, fields}).

run(Definition) -> dispatch(#wrapped{def=Definition}).

dispatch(#wrapped{def=Def}) ->
    case Def of
        #seq{context=Context,extension=Extension,fields=Fields} ->
            case Extension of
                none -> {Fields,Context};
                N when is_integer(N) -> {note(Fields),Context}
            end;
        #set{context=Context,fields=Fields} ->
            {Fields,Context}
    end.

note(Value) ->
    put(last_note, Value),
    {note, Value}.
