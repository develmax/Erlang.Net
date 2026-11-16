-module(beam_dynamic_apply_tail_probe).
-export([invoke/3, explicit/3, syntax/3, dynamic_function/3, variadic/3, foreign/3]).

invoke(M, A, B) ->
    Mod = identity(M),
    apply(Mod, append, [A, B]).

explicit(A, M, B) ->
    Mod = identity(M),
    erlang:apply(Mod, append, [B, A]).

syntax(M, A, B) ->
    Mod = identity(M),
    Mod:append(A, B).

dynamic_function(M, F, A) ->
    Function = identity(F),
    M:Function(A).

variadic(M, F, Args) ->
    Mod = identity(M),
    apply(Mod, F, Args).

foreign(M, A, B) ->
    Mod = identity(M),
    lists:apply(Mod, append, [A, B]).

identity(Term) ->
    Term.
