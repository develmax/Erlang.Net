-module(hello_include).
-export([hello/0]).
-include("version.hrl").
hello() -> {?GREETING, ?PAYLOAD, ?BUILD_FLAVOR}.
