-module(string_audit_numbers).
-export([write/1]).

write(Directory) ->
    ok = filelib:ensure_dir(filename:join(Directory,"placeholder")),
    Corners = [0,1,2,3,16#000FFFFFFFFFFFFE,16#000FFFFFFFFFFFFF,
        16#0010000000000000,16#0010000000000001,16#3FB9999999999999,
        16#3FB999999999999A,16#3FEFFFFFFFFFFFFF,16#3FF0000000000000,
        16#3FF0000000000001,16#433FFFFFFFFFFFFF,16#4340000000000000,
        16#7FEFFFFFFFFFFFFE,16#7FEFFFFFFFFFFFFF],
    {RandomBits,_} = lists:mapfoldl(fun(_,State) ->
        Next = (State*6364136223846793005+1442695040888963407) band ((1 bsl 64)-1),
        {Next band 16#7FEFFFFFFFFFFFFE,Next}
    end,20261103,lists:seq(1,64)),
    Texts = lists:usort([Sign ++ T || B <- Corners ++ RandomBits,
        T <- midpoint_neighbors(B), Sign <- ["","-"]]),
    Numeric = [{F,[T ++ "tail"]} || T <- Texts,F <- [list_to_float,to_float]] ++
        [{to_float,[list_to_binary(T ++ "tail")]} || T <- Texts],
    Seeds = lists:sublist(string_port_cases:cases(),71),
    Cases = lists:usort(Seeds ++ Numeric),
    Rows = [{F,A,string_port_cases:run(F,A)} || {F,A} <- Cases],
    ok = file:write_file(filename:join(Directory,"original.etf"),term_to_binary(Rows)),
    ok = file:write_file(filename:join(Directory,"cases.etf"),term_to_binary(Cases)),
    ok = file:write_file(filename:join(Directory,"exports.etf"),term_to_binary(
        lists:sort(string:module_info(exports) -- [{module_info,0},{module_info,1}]))),
    ok = file:write_file(filename:join(Directory,"backing.etf"),term_to_binary(string_port_cases:backing())),
    io:format("~p exact-rational midpoint cases~n",[length(Rows)]),
    ok.

midpoint_neighbors(Bits) ->
    {M1,P1} = rational(Bits),
    {M2,P2} = rational(Bits+1),
    P = min(P1,P2),
    M = (M1 bsl (P1-P)) + (M2 bsl (P2-P)),
    {Coefficient,Decimals} = case P-1 of
        Power when Power >= 0 -> {M bsl Power,0};
        Power -> {M*pow5(-Power),-Power}
    end,
    [decimal(Coefficient+Delta,Decimals) || Delta <- [-1,0,1]].

rational(Bits) ->
    % At max-finite + 1 this denotes the virtual real successor 2^1024,
    % used only to construct the finite/infinite rounding threshold exactly.
    Exponent = Bits bsr 52,
    Fraction = Bits band ((1 bsl 52)-1),
    case Exponent of
        0 -> {Fraction,-1074};
        _ -> {(1 bsl 52)+Fraction,Exponent-1023-52}
    end.

pow5(0) -> 1;
pow5(N) ->
    Half = pow5(N div 2),
    Half*Half*case N rem 2 of 0 -> 1; 1 -> 5 end.

decimal(Coefficient,0) -> integer_to_list(Coefficient) ++ ".0";
decimal(Coefficient,Places) ->
    S = integer_to_list(Coefficient),
    Padded = lists:duplicate(max(0,Places+1-length(S)),$0) ++ S,
    {Whole,Fraction} = lists:split(length(Padded)-Places,Padded),
    Whole ++ "." ++ Fraction.
