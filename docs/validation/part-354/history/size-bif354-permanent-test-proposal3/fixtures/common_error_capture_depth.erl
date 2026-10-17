-module(common_error_capture_depth).
-export([run/0, w1/1,w2/1,w3/1,w4/1,w5/1,w6/1,w7/1,w8/1,w9/1,leaf/1]).
run() ->
    Old = erlang:system_flag(backtrace_depth, 8),
    try
        [begin
             erlang:system_flag(backtrace_depth, Depth),
             [begin
                  {w9,{w8,{w7,{w6,{w5,{w4,{w3,{w2,{w1,done}}}}}}}}} = route(Route,{Mode,[],Depth})
              end || Route <- [ordinary,apply,named_fun], Mode <- [error,hd]]
         end || Depth <- [0,1,2,8]],
        done
    after erlang:system_flag(backtrace_depth, Old)
    end.
route(ordinary, Value) -> common_error_capture_depth:w9(Value);
route(apply, Value) -> erlang:apply(common_error_capture_depth,w9,[Value]);
route(named_fun, Value) -> F = fun common_error_capture_depth:w9/1, F(Value).
w9(X) -> {w9,common_error_capture_depth:w8(X)}.
w8(X) -> {w8,common_error_capture_depth:w7(X)}.
w7(X) -> {w7,common_error_capture_depth:w6(X)}.
w6(X) -> {w6,common_error_capture_depth:w5(X)}.
w5(X) -> {w5,common_error_capture_depth:w4(X)}.
w4(X) -> {w4,common_error_capture_depth:w3(X)}.
w3(X) -> {w3,common_error_capture_depth:w2(X)}.
w2(X) -> {w2,common_error_capture_depth:w1(X)}.
w1(X) -> {w1,common_error_capture_depth:leaf(X)}.
leaf({Mode,Value,Depth}) ->
    try
        case Mode of
            error -> erlang:error(capture_fault);
            hd -> erlang:hd(Value)
        end
    catch error:Reason:Stack ->
        Prefix = case Mode of
            error -> capture_fault = Reason, [{common_error_capture_depth,leaf,1}];
            hd -> badarg = Reason,
                  [{erlang,hd,[Value],Location}|_] = Stack,
                  {error_info,#{module:=erl_erts_errors}} = lists:keyfind(error_info,1,Location),
                  [{erlang,hd,[Value]},{common_error_capture_depth,leaf,1}]
        end,
        Expected = Prefix ++ [{common_error_capture_depth,w1,1},{common_error_capture_depth,w2,1},{common_error_capture_depth,w3,1},
                              {common_error_capture_depth,w4,1},{common_error_capture_depth,w5,1},{common_error_capture_depth,w6,1},
                              {common_error_capture_depth,w7,1},{common_error_capture_depth,w8,1},{common_error_capture_depth,w9,1}],
        Actual = [{M,F,A} || {M,F,A,_} <- Stack],
        ExpectedPrefix = lists:sublist(Expected, erlang:max(1,Depth)),
        ExpectedPrefix = Actual,
        false = lists:any(fun({M,F,_}) -> M =:= erlang andalso F =:= error end, Actual),
        done
    end.
