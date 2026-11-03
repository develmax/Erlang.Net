-module(math_acceptance).
-export([write/1]).

write(Path) ->
    {ok, VersionBytes} = file:read_file(filename:join([code:root_dir(),"releases",erlang:system_info(otp_release),"OTP_VERSION"])),
    "29.1.1" = string:trim(binary_to_list(VersionBytes)),
    Unary = [acos,acosh,asin,asinh,atan,atanh,ceil,cos,cosh,erf,erfc,exp,floor,log,log2,log10,sin,sinh,sqrt,tan,tanh],
    Binary = [atan2,fmod,pow],
    Edges = [0.0,-0.0,1.0,-1.0,2.0,-2.0,0.5,-0.5,1.0e-300,-1.0e-300,1.0e300,-1.0e300] ++
        [float_bits(B) || B <- [1,16#8000000000000001,16#000fffffffffffff,16#0010000000000000,
          16#7fefffffffffffff,16#ffefffffffffffff,16#3fefffffffffffff,16#3ff0000000000001,
          16#bfefffffffffffff,16#bff0000000000001]],
    Integers = [0,1,-1,42,-42] ++ lists:append([
        [I,-I] || K <- lists:seq(53,1025), D <- [-1,0,1], I <- [(1 bsl K)+D]]) ++
        lists:append([[I,-I] || K <- lists:seq(54,1024), D <- [-1,0,1], I <- [(1 bsl K)+(1 bsl (K-53))+D]]),
    Invalid = [bad_math_input,[],[1|2],{1},#{1=>2},<<0>>,<<1:1>>,fun erlang:abs/1,
        fun() -> ok end,self(),make_ref(),opaque_port()],
    Bits = random(4096,16#4d4154484f545032,[]),
    Floats = [finite_bits(B) || B <- Bits],
    Domain = [float_bits((B band 16#800fffffffffffff) bor 16#3fe0000000000000) || B <- Bits],
    Subnormal = [float_bits(B band 16#800fffffffffffff) || B <- lists:sublist(Bits,1024)],
    Inputs = Edges ++ Integers ++ Invalid ++ Floats ++ Domain ++ Subnormal,
    Pairs = [[A,B] || A <- Edges ++ Invalid ++ [1 bsl 1024,-(1 bsl 1024)], B <- Edges ++ Invalid ++ [1 bsl 1024,-(1 bsl 1024)]] ++
        [[A,B] || {A,B} <- lists:zip(Floats,lists:reverse(Floats))] ++
        [[A,B] || {A,B} <- lists:zip(Domain,lists:reverse(Domain))] ++
        [[A,B] || {A,B} <- lists:zip(Integers,lists:reverse(Integers))],
    Rows = [observe(F,[A]) || F <- Unary, A <- Inputs] ++
        [observe(F,Args) || F <- Binary, Args <- Pairs] ++
        [observe(pi,[]),observe(tau,[])],
    ok = file:write_file(Path,term_to_binary(Rows)),
    Directory = filename:dirname(Path),
    ok = file:write_file(filename:join(Directory,"exports.etf"),term_to_binary([
        {F,A} || {F,A} <- math:module_info(exports), F =/= module_info])),
    ok = file:write_file(filename:join(Directory,"metadata.etf"),term_to_binary({
        VersionBytes,erlang:system_info(system_version),erlang:system_info(system_architecture)})),
    {ok,_} = file:copy(code:which(math),filename:join(Directory,"math.beam")),
    {ok,beam_math_observer392} = compile:file("tests/Erlang.Tests/Resources/LibraryDependencies392/beam_math_observer392.erl",[{outdir,Directory}]),
    {module,beam_math_observer392} = code:load_abs(filename:join(Directory,"beam_math_observer392")),
    ok = file:write_file(filename:join(Directory,"values.etf"),term_to_binary(beam_math_observer392:run())),
    ok = file:write_file(filename:join(Directory,"stacks.etf"),term_to_binary(beam_math_observer392:run_stacks())),
    io:format("~p rows~n",[length(Rows)]),
    ok.

observe(F,Args) ->
    Result = try apply(math,F,Args) of V -> {ok,V} catch error:R -> {error,R} end,
    {F,Args,Result}.

float_bits(B) -> <<F:64/float>> = <<B:64>>, F.
opaque_port() -> binary_to_term(<<131,102,119,13,"nonode@nohost",0:32,0:8>>).
finite_bits(B) ->
    case B band 16#7ff0000000000000 of
        16#7ff0000000000000 -> float_bits(B bxor 16#0010000000000000);
        _ -> float_bits(B)
    end.

random(0,_,Acc) -> Acc;
random(N,X,Acc) ->
    Next = (X * 6364136223846793005 + 1442695040888963407) band 16#ffffffffffffffff,
    random(N-1,Next,[Next|Acc]).
