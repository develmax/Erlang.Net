-module(beam_epp_restore).
-export([value/0, trigger/0]).
-include("plain.hrl").
-file("virtual.yrl", 100).
-include("parent.hrl").
value() -> {?PLAIN, ?PARENT, ?CHILD, ?LINE}.
trigger() -> erlang:error(mapped).
-file("beam_epp_restore.erl", 300).
-include("same_file.hrl").
