-module(beam_docs_probe).
-moduledoc "\x{200e}\r\nModule docs — λ.\nSecond line 😀.\r\n ".
-export([f/0,g/0]).
-moduledoc #{since => "OTP 29", authors => ["Érlang"], format => <<"text/plain">>}.
-export_type([public/1, sealed/0]).
-doc ~"public(Value)\n\nPublic type — café.".
-doc #{since => <<"OTP 28">>, group => "Types"}.
-type public(_Value) :: {_Value, nested()}.
-type nested() :: atom().
-type private() :: integer().
-opaque sealed() :: binary().
-spec f() -> nested().
-doc "f()\n\nFunction docs.\r\n  Indented text.\n".
-doc #{equiv => g()}.
f() -> ok.
-doc "".
-doc #{equiv => f/0, extra => 7}.
g() -> 42.
