-module(beam_callback_docs_probe).
-moduledoc "Example".
-doc "Callback z".
-callback z(integer()) -> ok.
-callback a() -> atom().
-optional_callbacks([z/1]).
-export([id/1]).
-doc "Function".
id(X)->X.
