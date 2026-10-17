-module(common_wrapper_capture_depth).
-export([run/0, w1/1,w2/1,w3/1,w4/1,w5/1,w6/1,w7/1,w8/1,w9/1,leaf/1]).
run() ->
    Old = erlang:system_flag(backtrace_depth, 8),
    try
        [begin
             erlang:system_flag(backtrace_depth, Depth),
             [begin
                  {w9,{w8,{w7,{w6,{w5,{w4,{w3,{w2,{w1,done}}}}}}}}} = common_wrapper_capture_depth:w9({Mode,Depth})
              end || Mode <- [initial_call,start_link]]
         end || Depth <- [0,1,2,8]],
        done
    after erlang:system_flag(backtrace_depth, Old)
    end.
w9(X) -> {w9,common_wrapper_capture_depth:w8(X)}.
w8(X) -> {w8,common_wrapper_capture_depth:w7(X)}.
w7(X) -> {w7,common_wrapper_capture_depth:w6(X)}.
w6(X) -> {w6,common_wrapper_capture_depth:w5(X)}.
w5(X) -> {w5,common_wrapper_capture_depth:w4(X)}.
w4(X) -> {w4,common_wrapper_capture_depth:w3(X)}.
w3(X) -> {w3,common_wrapper_capture_depth:w2(X)}.
w2(X) -> {w2,common_wrapper_capture_depth:w1(X)}.
w1(X) -> {w1,common_wrapper_capture_depth:leaf(X)}.
leaf({Mode,Depth}) ->
    Arguments = [common_wrapper_capture_depth,unused,[],10,[monitor]],
    try
        case Mode of
            initial_call -> proc_lib:initial_call([{dictionary,[{'$initial_call',{m,f,invalid_arity}}]}]);
            start_link -> proc_lib:start_link(common_wrapper_capture_depth,unused,[],10,[monitor])
        end
    catch error:badarg:Stack ->
        Prefix = case Mode of
            initial_call -> [{erlang,integer_to_list,[invalid_arity]},{proc_lib,make_dummy_args,2},{proc_lib,initial_call,1}];
            start_link -> [{proc_lib,start_link,Arguments}]
        end,
        Expected = Prefix ++ [{common_wrapper_capture_depth,leaf,1},{common_wrapper_capture_depth,w1,1},
                              {common_wrapper_capture_depth,w2,1},{common_wrapper_capture_depth,w3,1},
                              {common_wrapper_capture_depth,w4,1},{common_wrapper_capture_depth,w5,1},
                              {common_wrapper_capture_depth,w6,1},{common_wrapper_capture_depth,w7,1}],
        Actual = [{M,F,A} || {M,F,A,_} <- Stack],
        ExpectedPrefix = lists:sublist(Expected, erlang:max(1,Depth)),
        ExpectedPrefix = Actual,
        done
    end.
