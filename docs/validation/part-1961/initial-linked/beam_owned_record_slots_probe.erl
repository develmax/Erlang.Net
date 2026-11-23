-module(beam_owned_record_slots_probe).
-export([run/3, reordered/4, preserve/3, trap/3, seed/2]).
-record(sock, {id, field}).

run(Rec, Dest, Key) ->
    case fetch(Key) of
        {ok, Sock} ->
            case erlang:put(Dest, Rec#sock{field = Sock}) of
                {ok, Message} ->
                   case erlang:put(Sock, Message) of
                       ok -> ok;
                       {error, _} -> finish(Sock), erlang:erase(Sock)
                   end;
                {error, _} -> finish(Sock), erlang:erase(Sock)
            end;
        {error, _} -> ok
    end.

reordered(Key, Rec, Dest, Extra) ->
    case fetch(Key) of
        {ok, Sock} ->
            case erlang:put(Dest, Rec#sock{field = Sock}) of
                {ok, Message} -> erlang:put(Sock, {Extra, Message});
                {error, _} -> finish(Sock), erlang:erase(Sock)
            end;
        {error, _} -> ok
    end.

preserve(Rec, Dest, Key) ->
    case fetch(Key) of
        {ok, Sock} ->
            case erlang:put(Dest, Rec#sock{field = Sock}) of
                {ok, Message} -> erlang:put(Sock, {Rec, Message});
                {error, _} -> erlang:erase(Sock)
            end;
        {error, _} -> ok
    end.

trap(Rec, Dest, Key) ->
    try
       run(Rec, Dest, Key)
    catch
       error:Reason -> {caught, Reason}
    end.

fetch(Key) -> erlang:get(Key).
finish(Key) -> erlang:get(Key).
seed(Key, Value) -> erlang:put(Key, Value).
