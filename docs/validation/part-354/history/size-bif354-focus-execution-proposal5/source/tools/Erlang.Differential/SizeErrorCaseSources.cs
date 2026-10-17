namespace Erlang.Differential;

public static class SizeErrorCaseSources
{
    public const string SizeErrorTupleSize = """
        -module(size_error_tuple_size).
        -export([run/0,qualified/1,auto/1,external/1,named/1,apply_three/1,apply_two/1]).
        run() ->
            lists:foreach(fun(V)->lists:foreach(fun(F)->check(F,V) end,
                [fun qualified/1,fun auto/1,fun external/1,fun named/1,fun apply_three/1,fun apply_two/1]) end,values()), done.
        qualified(V) -> erlang:tuple_size(V).
        auto(V) -> tuple_size(V).
        external(V) -> F=fun erlang:tuple_size/1,F(V).
        named(V) -> F=fun tuple_size/1,F(V).
        apply_three(V) -> M=erlang,F=tuple_size,apply(M,F,[V]).
        apply_two(V) -> F=fun erlang:tuple_size/1,apply(F,[V]).
        check(F,V) ->
            try F(V) of _->error(missing_failure)
            catch error:Reason:Stack ->
                badarg=Reason,
                [{erlang,tuple_size,[V],[{error_info,ErrorInfo}]}|Tail]=Stack,
                true=(ErrorInfo =:= #{module=>erl_erts_errors}),
                1=length([present || {erlang,tuple_size,[_],_} <- Stack]), true=is_list(Tail), ok
            end.
        values() ->
            Native=records:create(test,a,[{x,1}],#{is_exported=>false}),
            ExternalPid=binary_to_term(<<131,88,119,13,"external@host",0,0,0,1,0,0,0,0,0,0,0,0>>),
            ExternalRef=binary_to_term(<<131,90,0,1,119,13,"external@host",0,0,0,0,0,0,0,1>>),
            ExternalPort=binary_to_term(<<131,89,119,13,"external@host",0,0,0,1,0,0,0,0>>),
            [bad,0,1 bsl 200,1.5,[],[a|tail],self(),make_ref(),fun()->ok end,
             fun erlang:self/0,Native,ExternalPid,ExternalRef,ExternalPort,#{a=>1},<<>>,<<1:1>>].

        """;

    public const string SizeImportTupleSize = """
        -module(size_import_tuple_size).
        -import(erlang,[tuple_size/1]).
        -export([run/0,call/1]).
        run() ->
            V=bad,
            try call(V) of _->error(missing_failure)
            catch error:Reason:Stack ->
                badarg=Reason,
                [{erlang,tuple_size,[V],[{error_info,ErrorInfo}]}|_]=Stack,true=(ErrorInfo =:= #{module=>erl_erts_errors}),done
            end.
        call(V) -> tuple_size(V).

        """;

    public const string SizeErrorMapSize = """
        -module(size_error_map_size).
        -export([run/0,qualified/1,auto/1,external/1,named/1,apply_three/1,apply_two/1]).
        run() ->
            lists:foreach(fun(V)->lists:foreach(fun(F)->check(F,V) end,
                [fun qualified/1,fun auto/1,fun external/1,fun named/1,fun apply_three/1,fun apply_two/1]) end,values()), done.
        qualified(V) -> erlang:map_size(V).
        auto(V) -> map_size(V).
        external(V) -> F=fun erlang:map_size/1,F(V).
        named(V) -> F=fun map_size/1,F(V).
        apply_three(V) -> M=erlang,F=map_size,apply(M,F,[V]).
        apply_two(V) -> F=fun erlang:map_size/1,apply(F,[V]).
        check(F,V) ->
            try F(V) of _->error(missing_failure)
            catch error:Reason:Stack ->
                {badmap,V}=Reason,
                [{erlang,map_size,[V],[{error_info,ErrorInfo}]}|Tail]=Stack,
                true=(ErrorInfo =:= #{module=>erl_erts_errors}),
                1=length([present || {erlang,map_size,[_],_} <- Stack]), true=is_list(Tail), ok
            end.
        values() ->
            Native=records:create(test,a,[{x,1}],#{is_exported=>false}),
            ExternalPid=binary_to_term(<<131,88,119,13,"external@host",0,0,0,1,0,0,0,0,0,0,0,0>>),
            ExternalRef=binary_to_term(<<131,90,0,1,119,13,"external@host",0,0,0,0,0,0,0,1>>),
            ExternalPort=binary_to_term(<<131,89,119,13,"external@host",0,0,0,1,0,0,0,0>>),
            [bad,0,1 bsl 200,1.5,[],[a|tail],self(),make_ref(),fun()->ok end,
             fun erlang:self/0,Native,ExternalPid,ExternalRef,ExternalPort,{}, {a,b},<<>>,<<1:1>>].

        """;

    public const string SizeImportMapSize = """
        -module(size_import_map_size).
        -import(erlang,[map_size/1]).
        -export([run/0,call/1]).
        run() ->
            V=bad,
            try call(V) of _->error(missing_failure)
            catch error:Reason:Stack ->
                {badmap,V}=Reason,
                [{erlang,map_size,[V],[{error_info,ErrorInfo}]}|_]=Stack,true=(ErrorInfo =:= #{module=>erl_erts_errors}),done
            end.
        call(V) -> map_size(V).

        """;

    public const string SizeErrorBitSize = """
        -module(size_error_bit_size).
        -export([run/0,qualified/1,auto/1,external/1,named/1,apply_three/1,apply_two/1]).
        run() ->
            lists:foreach(fun(V)->lists:foreach(fun(F)->check(F,V) end,
                [fun qualified/1,fun auto/1,fun external/1,fun named/1,fun apply_three/1,fun apply_two/1]) end,values()), done.
        qualified(V) -> erlang:bit_size(V).
        auto(V) -> bit_size(V).
        external(V) -> F=fun erlang:bit_size/1,F(V).
        named(V) -> F=fun bit_size/1,F(V).
        apply_three(V) -> M=erlang,F=bit_size,apply(M,F,[V]).
        apply_two(V) -> F=fun erlang:bit_size/1,apply(F,[V]).
        check(F,V) ->
            try F(V) of _->error(missing_failure)
            catch error:Reason:Stack ->
                badarg=Reason,
                [{erlang,bit_size,[V],[{error_info,ErrorInfo}]}|Tail]=Stack,
                true=(ErrorInfo =:= #{module=>erl_erts_errors}),
                1=length([present || {erlang,bit_size,[_],_} <- Stack]), true=is_list(Tail), ok
            end.
        values() ->
            Native=records:create(test,a,[{x,1}],#{is_exported=>false}),
            ExternalPid=binary_to_term(<<131,88,119,13,"external@host",0,0,0,1,0,0,0,0,0,0,0,0>>),
            ExternalRef=binary_to_term(<<131,90,0,1,119,13,"external@host",0,0,0,0,0,0,0,1>>),
            ExternalPort=binary_to_term(<<131,89,119,13,"external@host",0,0,0,1,0,0,0,0>>),
            [bad,0,1 bsl 200,1.5,[],[a|tail],self(),make_ref(),fun()->ok end,
             fun erlang:self/0,Native,ExternalPid,ExternalRef,ExternalPort,{}, {a,b},#{a=>1}].

        """;

    public const string SizeImportBitSize = """
        -module(size_import_bit_size).
        -import(erlang,[bit_size/1]).
        -export([run/0,call/1]).
        run() ->
            V=bad,
            try call(V) of _->error(missing_failure)
            catch error:Reason:Stack ->
                badarg=Reason,
                [{erlang,bit_size,[V],[{error_info,ErrorInfo}]}|_]=Stack,true=(ErrorInfo =:= #{module=>erl_erts_errors}),done
            end.
        call(V) -> bit_size(V).

        """;

    public const string SizeErrorByteSize = """
        -module(size_error_byte_size).
        -export([run/0,qualified/1,auto/1,external/1,named/1,apply_three/1,apply_two/1]).
        run() ->
            lists:foreach(fun(V)->lists:foreach(fun(F)->check(F,V) end,
                [fun qualified/1,fun auto/1,fun external/1,fun named/1,fun apply_three/1,fun apply_two/1]) end,values()), done.
        qualified(V) -> erlang:byte_size(V).
        auto(V) -> byte_size(V).
        external(V) -> F=fun erlang:byte_size/1,F(V).
        named(V) -> F=fun byte_size/1,F(V).
        apply_three(V) -> M=erlang,F=byte_size,apply(M,F,[V]).
        apply_two(V) -> F=fun erlang:byte_size/1,apply(F,[V]).
        check(F,V) ->
            try F(V) of _->error(missing_failure)
            catch error:Reason:Stack ->
                badarg=Reason,
                [{erlang,byte_size,[V],[{error_info,ErrorInfo}]}|Tail]=Stack,
                true=(ErrorInfo =:= #{module=>erl_erts_errors}),
                1=length([present || {erlang,byte_size,[_],_} <- Stack]), true=is_list(Tail), ok
            end.
        values() ->
            Native=records:create(test,a,[{x,1}],#{is_exported=>false}),
            ExternalPid=binary_to_term(<<131,88,119,13,"external@host",0,0,0,1,0,0,0,0,0,0,0,0>>),
            ExternalRef=binary_to_term(<<131,90,0,1,119,13,"external@host",0,0,0,0,0,0,0,1>>),
            ExternalPort=binary_to_term(<<131,89,119,13,"external@host",0,0,0,1,0,0,0,0>>),
            [bad,0,1 bsl 200,1.5,[],[a|tail],self(),make_ref(),fun()->ok end,
             fun erlang:self/0,Native,ExternalPid,ExternalRef,ExternalPort,{}, {a,b},#{a=>1}].

        """;

    public const string SizeImportByteSize = """
        -module(size_import_byte_size).
        -import(erlang,[byte_size/1]).
        -export([run/0,call/1]).
        run() ->
            V=bad,
            try call(V) of _->error(missing_failure)
            catch error:Reason:Stack ->
                badarg=Reason,
                [{erlang,byte_size,[V],[{error_info,ErrorInfo}]}|_]=Stack,true=(ErrorInfo =:= #{module=>erl_erts_errors}),done
            end.
        call(V) -> byte_size(V).

        """;

    public const string SizeGuardMatchspec = """
        -module(size_guard_matchspec).
        -export([run/0]).
        run() ->
            rejected=tuple_guard(bad),rejected=map_guard(bad),rejected=bits_guard(bad),rejected=bytes_guard(bad),
            accepted=tuple_guard({}),accepted=map_guard(#{}),accepted=bits_guard(<<1:1>>),accepted=bytes_guard(<<1:1>>),
            lists:foreach(fun(F)->
                {ok,false,[],[]}=erlang:match_spec_test({bad},[{{'$1'},[{'>',{F,'$1'},0}],[true]}],table),
                {ok,'EXIT',[],[]}=erlang:match_spec_test({bad},[{{'$1'},[],[{F,'$1'}]}],table)
            end,[tuple_size,map_size,bit_size,byte_size]),done.
        tuple_guard(V) when tuple_size(V)>=0 -> accepted;tuple_guard(_)->rejected.
        map_guard(V) when map_size(V)>=0 -> accepted;map_guard(_)->rejected.
        bits_guard(V) when bit_size(V)>=0 -> accepted;bits_guard(_)->rejected.
        bytes_guard(V) when byte_size(V)>=0 -> accepted;bytes_guard(_)->rejected.

        """;

    public const string SizeValidValues = """
        -module(size_valid_values).
        -export([run/0]).
        run() ->
            lists:foreach(fun(N)->B= <<0:N>>,N=bit_size(B),Expected=(N+7) div 8,Expected=byte_size(B) end,lists:seq(0,65)),
            <<_:3,Slice:11/bitstring,_/bitstring>> = <<7:3,0:11,3:2>>,11=bit_size(Slice),2=byte_size(Slice),
            0=tuple_size({}),3=tuple_size({a,b,c}),256=tuple_size(list_to_tuple(lists:seq(1,256))),
            0=map_size(#{}),65=map_size(maps:from_list([{N,N} || N<-lists:seq(1,65)])),done.

        """;

    public const string SizeCustomTracePreservation = """
        -module(size_custom_trace_preservation).
        -compile({no_auto_import,[tuple_size/1,map_size/1,bit_size/1,byte_size/1]}).
        -export([run/0,tuple_size/1,map_size/1,bit_size/1,byte_size/1]).
        run() ->
            lists:foreach(fun({Name,Fun})->
                try Fun(bad) of _->error(missing_custom)
                catch error:{custom,Name}:Stack ->
                    [{size_custom_trace_preservation,Name,[bad],Location}|_]=Stack,
                    {error_info,#{module:=size_custom_errors,cause:=Name}}=lists:keyfind(error_info,1,Location),ok
                end
            end,[{tuple_size,fun tuple_size/1},{map_size,fun map_size/1},{bit_size,fun bit_size/1},{byte_size,fun byte_size/1}]),
            try erlang:raise(error,explicit_empty,[]) catch error:explicit_empty:Stack0 -> []=Stack0 end,done.
        tuple_size(V)->erlang:error({custom,tuple_size},[V],[{error_info,#{module=>size_custom_errors,cause=>tuple_size}}]).
        map_size(V)->erlang:error({custom,map_size},[V],[{error_info,#{module=>size_custom_errors,cause=>map_size}}]).
        bit_size(V)->erlang:error({custom,bit_size},[V],[{error_info,#{module=>size_custom_errors,cause=>bit_size}}]).
        byte_size(V)->erlang:error({custom,byte_size},[V],[{error_info,#{module=>size_custom_errors,cause=>byte_size}}]).

        """;

    public const string SizeDepthControls = """
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

        """;

    public const string BacktraceDepthFlag = """
        -module(backtrace_depth_flag).
        -export([run/0]).
        run()->
            Old=erlang:system_flag(backtrace_depth,8),
            try
                8=erlang:system_flag(backtrace_depth,0),
                0=erlang:system_flag(backtrace_depth,1),
                1=erlang:system_flag(backtrace_depth,64),
                64=erlang:system_flag(backtrace_depth,65),
                64=erlang:system_flag(backtrace_depth,65536),
                64=erlang:system_flag(backtrace_depth,(1 bsl 59)-1),
                64=erlang:system_flag(backtrace_depth,8),
                lists:foreach(fun invalid/1,[-1,1 bsl 59,1 bsl 200,1.0,bad,[],#{},{}]),
                Parent=self(),{Child,Monitor}=spawn_monitor(fun()->8=erlang:system_flag(backtrace_depth,2),Parent!changed end),
                receive changed->ok after 2000->error(child_timeout) end,
                receive {'DOWN',Monitor,process,Child,normal}->ok after 2000->error(child_not_joined) end,
                2=erlang:system_flag(backtrace_depth,8),true=is_pid(Child),done
            after erlang:system_flag(backtrace_depth,Old) end.
        invalid(Value)->
            try erlang:system_flag(backtrace_depth,Value) of _->error(missing_badarg)
            catch error:badarg:Stack->
                [{erlang,system_flag,[backtrace_depth,Value],[{error_info,ErrorInfo}]}|_]=Stack,
                true=(ErrorInfo =:= #{module=>erl_erts_errors})
            end,
            8=erlang:system_flag(backtrace_depth,8).

        """;

    public const string SizeDepthImportControls = """
        -module(size_depth_import_controls).
        -import(erlang,[tuple_size/1,map_size/1,bit_size/1,byte_size/1]).
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
                    _->[{size_depth_import_controls,Wrapper,1,Location}|_]=Tail,true=is_list(Location),true=(length(Stack)=<Depth)
                end
            end.
        tuple_direct(V)->R=tuple_size(V),{returned,R}.
        tuple_external(V)->F=fun erlang:tuple_size/1,R=F(V),{returned,R}.
        map_direct(V)->R=map_size(V),{returned,R}.
        map_external(V)->F=fun erlang:map_size/1,R=F(V),{returned,R}.
        bits_direct(V)->R=bit_size(V),{returned,R}.
        bits_external(V)->F=fun erlang:bit_size/1,R=F(V),{returned,R}.
        bytes_direct(V)->R=byte_size(V),{returned,R}.
        bytes_external(V)->F=fun erlang:byte_size/1,R=F(V),{returned,R}.

        """;

    public const string GenericDepthError = """
        -module(generic_depth_error).
        -export([run/0,leaf/1,middle/1]).
        run()->
            Old=erlang:system_flag(backtrace_depth,8),
            try
                lists:foreach(fun(Depth)->erlang:system_flag(backtrace_depth,Depth),check(Depth) end,[0,1,2,8]),done
            after erlang:system_flag(backtrace_depth,Old) end.
        check(Depth)->
            try middle(probe_reason) of _->error(missing_failure)
            catch error:Reason:Stack->
                probe_reason=Reason,
                [{generic_depth_error,leaf,1,Location}|Tail]=Stack,
                true=is_list(Location),
                case Depth of 0->[]=Tail;1->[]=Tail;_-> [{generic_depth_error,middle,1,_}|_]=Tail end,
                true=(length(Stack)=<erlang:max(1,Depth))
            end.
        middle(V)->R=generic_depth_error:leaf(V),{middle_returned,R}.
        leaf(V)->R=erlang:error(V),{leaf_returned,R}.

        """;

    public const string GenericDepthHd = """
        -module(generic_depth_hd).
        -export([run/0,leaf/1,middle/1]).
        run()->
            Old=erlang:system_flag(backtrace_depth,8),
            try
                lists:foreach(fun(Depth)->erlang:system_flag(backtrace_depth,Depth),check(Depth) end,[0,1,2,8]),done
            after erlang:system_flag(backtrace_depth,Old) end.
        check(Depth)->
            try middle(bad) of _->error(missing_failure)
            catch error:Reason:Stack->
                badarg=Reason,
                [{erlang,hd,[bad],[{error_info,ErrorInfo}]}|Tail]=Stack,
                true=(ErrorInfo =:= #{module=>erl_erts_errors}),
                case Depth of 0->[]=Tail;1->[]=Tail;_-> [{generic_depth_hd,leaf,1,_}|_]=Tail end,
                true=(length(Stack)=<erlang:max(1,Depth))
            end.
        middle(V)->R=generic_depth_hd:leaf(V),{middle_returned,R}.
        leaf(V)->R=erlang:hd(V),{leaf_returned,R}.

        """;

    public const string CommonErrorCaptureDepth = """
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

        """;
}
