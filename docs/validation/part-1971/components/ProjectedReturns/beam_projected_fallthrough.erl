-module(beam_projected_fallthrough).
-export([same/1,different/1]).
-record(opened, {path, token, state}).

same(Path) ->
    case beam_return_provider:lookup(Path) of
        {error, Reason} -> {error, Reason};
        _ ->
            case beam_return_provider:open(Path) of
                {ok, Token} -> {ok, #opened{path=Path, token=Token, state=start}};
                {repaired, Token, _, _} -> {ok, #opened{path=Path, token=Token, state=start}};
                {error, Reason} -> {error, Reason}
            end
    end.

different(Path) ->
    case beam_return_provider:lookup(Path) of
        {error, Reason} -> {error, Reason};
        _ ->
            case beam_return_provider:open(Path) of
                {ok, Token} -> {ok, #opened{path=Path, token=Token, state=start}};
                {repaired, Token, _, _} -> {ok, #opened{path=Path, token=Token, state=repaired}};
                {error, Reason} -> {error, Reason}
            end
    end.
