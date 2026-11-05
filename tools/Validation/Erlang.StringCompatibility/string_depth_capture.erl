-module(string_depth_capture).
-export([write/1]).

write(Directory) ->
    Cases = [
        {trim,[lists:foldr(fun(C,T)->[C|T] end,invalid,lists:duplicate(20,32)++"a a a a a"),trailing," "]},
        {take,[lists:foldr(fun(C,T)->[C|T] end,invalid,"a a a a a a a a")," ",false,trailing]},
        {take,[[<<" ">>|invalid]," ",false,trailing]},
        {take,[[<<"a">>|invalid]," ",true,trailing]},
        {take,["a",[],invalid]},
        {take,["a",[],true,invalid]},
        {take,["a",[],false,{}]},
        {substr,[[97,98|invalid],1,50]},
        {to_upper,[[97,98|invalid]]},
        {lexemes,[[97|invalid]," "]},
        {split,[invalid,"a",all]},
        {equal,[invalid,"x",true,nfc]},
        {equal,["x",invalid,true,nfc]},
        {jaro_similarity,[invalid,"x"]},
        {jaro_similarity,["x",invalid]},
        {slice,[<<97,192,192>>,0,4]},
        {join,[["a"|invalid],","]},
        {take,[[<<"a">>,"!",invalid],"a",false,leading]},
        {take,[[binary:copy(<<"a">>,20),"!",invalid],"a",false,leading]},
        {take,[<<"a">>,invalid,true,leading]},
        {prefix,["abc",<<255>>]},
        {find,["abc",[[]]]},
        {split,[[<<97,255>>,"b"],"a",all]}
    ],
    OriginalDepth = erlang:system_flag(backtrace_depth,8),
    try
        lists:foreach(fun(Depth) ->
            erlang:system_flag(backtrace_depth,Depth),
            Rows = [{F,A,string_port_cases:run(F,A)} || {F,A} <- Cases],
            Target = filename:join(Directory,"depth-"++integer_to_list(Depth)),
            ok = file:make_dir(Target),
            [ {ok,_} = file:copy(filename:join(Directory,N),filename:join(Target,N)) ||
                N <- ["exports.etf","backing.etf","string_port_cases.beam"] ],
            ok = file:write_file(filename:join(Target,"original.etf"),term_to_binary(Rows))
        end,[0,1,2,3,8,32])
    after erlang:system_flag(backtrace_depth,OriginalDepth)
    end.
