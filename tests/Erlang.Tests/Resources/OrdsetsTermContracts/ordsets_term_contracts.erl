-module(ordsets_term_contracts).
-export([run/0,pool/0,ordsets_probes/0]).
-record #r{z,a}.
-export_record([r]).
-record #q{a}.

run() ->
    Pool=pool(),
    {Pool,[{I,J,sign(X,Y),X==Y,X=:=Y} || {I,X} <- enumerate(Pool,0),
                                         {J,Y} <- enumerate(Pool,0)]}.

ordsets_probes() ->
    A=fake_fun(0,1,[]), B=fake_fun(255,2,[]),
    C=fake_fun(127,2,[]), D=fake_fun(128,1,[]),
    Pairs=[{A,D},{fake_fun(0,1,[1]),fake_fun(0,1,[1.0])},
           {[1],[1.0]},{-0.0,0.0},{#{1=>a},#{1.0=>a}},
           {#{A=>0,B=>1},#{C=>1,D=>0}},
           {#{A=>0,D=>1},#{D=>0,A=>1}},
           {grow(33,#{A=>0,D=>1}),grow(33,#{D=>0,A=>1})},
           {#{#{A=>0,B=>1}=>0},#{#{C=>1,D=>0}=>0}},
           {record([z,a],[1,2]),record([a,z],[2,1])}],
    [{I,sign(X,Y),X==Y,X=:=Y,ordsets:is_equal([X],[Y]),
      ordsets:is_element(X,[Y]),ordsets:is_set([X,Y])}
        || {I,{X,Y}} <- enumerate(Pairs,0)].

pool() ->
    A = fake_fun(0,1,[]), B = fake_fun(255,2,[]),
    C = fake_fun(127,2,[]), D = fake_fun(128,1,[]),
    Funs = [A,B,C,D,fake_fun(0,1,[1]),fake_fun(0,1,[1.0]),
            fun erlang:abs/1,fun erlang:round/1,fun erlang:abs/2],
    Numbers = [-18446744073709551617,-9007199254740993,-9007199254740992.0,
               -1,-1.0,-0.0,0,0.0,1,1.0,9007199254740992.0,9007199254740993,
               18446744073709551617,1.0e-300,-1.0e-300,1.0e300,-1.0e300],
    Base = Numbers ++ [a,z,'\x{E000}','\x{10000}',{}, {1}, {1.0},
                       {a,0},{a,0.0},[],[1],[1.0],[1|tail],[1.0|tail],
                       <<>>,<<0:1>>,<<1:1>>,<<1:7>>,<<1:8>>] ++ Funs,
    Maps = [#{},#{1=>a},#{1.0=>a},#{-0.0=>a},#{0.0=>a},
            #{a=>1},#{a=>1.0},#{A=>0,B=>1},#{C=>1,D=>0},
            #{C=>0,D=>1},#{A=>0,D=>1},#{A=>1,D=>0},#{D=>0,A=>1}],
    BigMaps = [grow(N,M)
               || N <- [30,31,32,33,64], M <- Maps],
    Nested = [#{M=>V} || M <- Maps, V <- [0,1,1.0]] ++
             [#{X=>V,Y=>W} || {X,Y} <- [{#{z=>0,a=>0},#{z=>0,b=>0}},
                 {#{A=>0,B=>1},#{C=>1,D=>0}}], {V,W} <- [{0,1},{1,0}]],
    Ids = [pid(N,I,S,Creation) || {N,I,S,Creation} <- [{a,0,0,0},{a,1,0,0},{a,0,1,0},
             {a,0,0,1},{z,0,0,0},{a,16#ffffffff,16#ffffffff,16#ffffffff}]] ++
          [port(N,I,Creation) || {N,I,Creation} <- [{a,0,0},{a,1,0},{z,0,0},{a,0,1},
             {a,16#ffffffffffffffff,16#ffffffff}]] ++
          [ref(N,Creation,Words) || {N,Creation,Words} <- [{a,0,[1]},{a,0,[1,0]},
             {a,0,[0,1]},{a,1,[1]},{z,0,[1]},{a,16#ffffffff,[0,0,16#ffffffff]}]],
    Records = [#r{z=1,a=2},#r{z=1.0,a=2.0},#q{a=1},
               record([z,a],[1,2]),record([a,z],[2,1])],
    Pool = Base ++ Maps ++ BigMaps ++ Nested ++ Ids ++ Records ++
           [lists:duplicate(1000,1),lists:duplicate(1000,1.0)],
    Pool.

%% Bulk sorting equal-order, exact-distinct fun keys is address-seeded in OTP.
%% Insert those keys into an already large map to get a reproducible physical
%% collision history, while the other maps exercise the 32/33 bulk boundary.
grow(N,M) ->
    Keys=maps:keys(M),
    Tied=case Keys of [X,Y] when is_function(X),is_function(Y) -> sign(X,Y)==0; _ -> false end,
    Size=case Tied of true -> max(N,33); false -> N end,
    Seed=maps:from_list([{I,I} || I <- lists:seq(1,Size)]),
    case Tied of
        true -> maps:fold(fun(K,V,Acc) -> maps:put(K,V,Acc) end,Seed,M);
        false -> maps:merge(Seed,M)
    end.

enumerate([],_) -> [];
enumerate([H|T],N) -> [{N,H}|enumerate(T,N+1)].
sign(X,Y) when X < Y -> -1;
sign(X,Y) when X > Y -> 1;
sign(_,_) -> 0.

%% Valid, unloaded local fun terms. Never execute them: ordsets must compare
%% opaque function values even when their code has been unloaded.
fake_fun(U,OldUnique,Env) ->
    Unique = binary:copy(<<U>>,16),
    Payload = iolist_to_binary([<<0,Unique/binary,0:32,(length(Env)):32>>,
                               encoded(ordsets_term_contracts_unloaded),encoded(0),
                               encoded(OldUnique),encoded(list_to_pid("<0.0.0>")),
                               [encoded(E) || E <- Env]]),
    binary_to_term(<<131,112,(byte_size(Payload)+4):32,Payload/binary>>).
encoded(T) -> <<131,B/binary>> = term_to_binary(T), B.
pid(N,I,S,C) -> Node=encoded(N), binary_to_term(<<131,88,Node/binary,I:32,S:32,C:32>>).
port(N,I,C) -> Node=encoded(N), binary_to_term(<<131,120,Node/binary,I:64,C:32>>).
ref(N,C,Words) ->
    Node=encoded(N), Payload = << <<W:32>> || W <- Words >>,
    binary_to_term(<<131,90,(length(Words)):16,Node/binary,C:32,Payload/binary>>).
record(Keys,Values) ->
    Payload=iolist_to_binary([encoded(?MODULE),encoded(r),
                              [encoded(K) || K <- Keys],[encoded(V) || V <- Values]]),
    binary_to_term(<<131,67,(length(Keys)):32,1,Payload/binary>>).
