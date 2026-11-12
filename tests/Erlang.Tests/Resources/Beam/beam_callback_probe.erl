-module(beam_callback_probe).
-callback z(integer()) -> ok.
-callback a() -> atom().
-optional_callbacks([z/1]).
-compile({no_auto_import,[size/1]}).
-export([size/1]).
size(X) -> X.
