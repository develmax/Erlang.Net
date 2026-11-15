-module(beam_linear_frame_regions).
-export([init/1, iterate/1, run/2, choose/2]).

init([]) ->
    Set = gb_sets:empty(),
    Tree = gb_trees:empty(),
    {ok,{state,Set,Tree}}.
iterate(Count) when is_integer(Count) ->
    Start = put(stage1, Count),
    Middle = put(stage2, Start),
    Last = put(stage3, Middle),
    {ok,Last,Count}.
run({one,Item}, Keep) ->
    Start = put(stage1, Item),
    Middle = put(stage2, Start),
    Last = put(stage3, Middle),
    {ok,Last,Keep};
run({two,Item}, Keep) ->
    A = put(stage4, Item),
    B = put(stage5, A),
    {ok,B,Keep};
run(_, Keep) ->
    {default,Keep}.
choose(first, Value) ->
    Start = put(stage1, Value),
    Middle = put(stage2, Start),
    Last = put(stage3, Middle),
    {ok,Last,Value};
choose(second, Value) ->
    A = put(stage4, Value),
    B = put(stage5, A),
    {ok,B,Value};
choose(_, Value) ->
    {default,Value}.
