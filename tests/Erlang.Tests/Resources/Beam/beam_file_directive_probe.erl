-module(beam_file_directive_probe).
-export([before/0, mapped/0, same_line/0, after_map/0]).

before() -> {?FILE, ?LINE}.
-file("generated/grammar.yrl", 200).
mapped() -> {?FILE, ?LINE}.
-file("other/grammar.yrl", 400). same_line() -> {?FILE, ?LINE}.

-file("beam_file_directive_probe.erl", 20).
after_map() -> {?FILE, ?LINE}.
