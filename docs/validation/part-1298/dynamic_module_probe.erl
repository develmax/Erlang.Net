-module(dynamic_module_probe).
-export([run/1, retained/1, target/0, work/1]).
run(Value) when is_atom(Value) ->
    Target = ?MODULE:target(),
    Target:work(Value),
    ok.
retained(Value) ->
    Target = ?MODULE:target(),
    Target:work(Value),
    Target.
target() -> ?MODULE.
work(Value) -> self() ! {ran, Value}, ok.

