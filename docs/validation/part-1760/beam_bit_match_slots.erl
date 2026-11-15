-module(beam_bit_match_slots).
-export([fixed/2, split/3, signed/3]).

fixed(Bin, Keep) ->
    Snapshot = put(first, Bin),
    case Snapshot of
        <<A:8, 42, B:8>> -> {A, B, Keep, put(next, A)};
        _ -> {missing, Keep, put(next, Bin)}
    end.

split(Bin, Count, Keep) ->
    Snapshot = put(first, Bin),
    case Snapshot of
        <<A:Count/binary, B/binary>> -> {A, B, Keep, put(next, A)};
        _ -> {missing, Keep, put(next, Bin)}
    end.

signed(Bin, Count, Keep) ->
    Snapshot = put(first, Bin),
    case Snapshot of
        <<A:Count/signed, B:8>> -> {A, B, Keep, put(next, A)};
        _ -> {missing, Keep, put(next, Bin)}
    end.
