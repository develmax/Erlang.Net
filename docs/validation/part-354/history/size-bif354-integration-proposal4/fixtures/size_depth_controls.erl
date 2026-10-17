-module(size_depth_controls).
-export([run/0,tuple_direct/1,tuple_external/1,map_direct/1,map_external/1,bits_direct/1,bits_external/1,bytes_direct/1,bytes_external/1]).
run() ->
    Old=erlang:system_flag(backtrace_depth,8),
    try
        lists:foreach(fun(Depth)->
            erlang:system_flag(backtrace_depth,Depth),
            lists:foreach(fun({Name,Wrapper,Fun})->check(Depth,Name,Wrapper,Fun) end,routes())
        end,[0,1,2,8]),done
    after erlang:system_flag(backtrace_depth,Old) end.
routes()->[
    {tuple_size,tuple_direct,fun tuple_direct/1},{tuple_size,tuple_external,fun tuple_external/1},
    {map_size,map_direct,fun map_direct/1},{map_size,map_external,fun map_external/1},
    {bit_size,bits_direct,fun bits_direct/1},{bit_size,bits_external,fun bits_external/1},
    {byte_size,bytes_direct,fun bytes_direct/1},{byte_size,bytes_external,fun bytes_external/1}].
check(Depth,Name,Wrapper,Fun)->
    try Fun(bad) of _->error(missing_failure)
    catch error:Reason:Stack->
        case Name of map_size->{badmap,bad}=Reason;_->badarg=Reason end,
        [{erlang,Name,[bad],[{error_info,ErrorInfo}]}|Tail]=Stack,
        true=(ErrorInfo =:= #{module=>erl_erts_errors}),
        case Depth of
            0->[]=Tail;
            1->[]=Tail;
            _->[{size_depth_controls,Wrapper,1,Location}|_]=Tail,true=is_list(Location),true=(length(Stack)=<Depth)
        end
    end.
tuple_direct(V)->R=erlang:tuple_size(V),{returned,R}.
tuple_external(V)->F=fun erlang:tuple_size/1,R=F(V),{returned,R}.
map_direct(V)->R=erlang:map_size(V),{returned,R}.
map_external(V)->F=fun erlang:map_size/1,R=F(V),{returned,R}.
bits_direct(V)->R=erlang:bit_size(V),{returned,R}.
bits_external(V)->F=fun erlang:bit_size/1,R=F(V),{returned,R}.
bytes_direct(V)->R=erlang:byte_size(V),{returned,R}.
bytes_external(V)->F=fun erlang:byte_size/1,R=F(V),{returned,R}.
