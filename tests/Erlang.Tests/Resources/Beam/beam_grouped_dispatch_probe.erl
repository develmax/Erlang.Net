-module(beam_grouped_dispatch_probe).
-export([handles/3]).

handles(vm, exit, 1) -> true;
handles(vm, throw, 1) -> true;
handles(vm, error, 1) -> true;
handles(vm, error, 2) -> true;
handles(_, _, _) -> false.
