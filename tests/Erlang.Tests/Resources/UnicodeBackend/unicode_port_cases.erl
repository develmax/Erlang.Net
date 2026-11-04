-module(unicode_port_cases).
-export([write/1, cases/0, run/2, outcome/2, worker/3, backing/0, write_extra/1]).

write(Directory) ->
    Cases = lists:usort(cases()),
    Results = [{F,A,run(F,A)} || {F,A} <- Cases],
    ok = file:write_file(filename:join(Directory,"original.etf"),term_to_binary(Results)),
    ok = file:write_file(filename:join(Directory,"exports.etf"),term_to_binary(
        lists:sort(unicode:module_info(exports) -- [{module_info,0},{module_info,1}]))).

run(F,A) ->
    {Pid,Ref} = spawn_monitor(?MODULE,worker,[F,A,self()]),
    receive {Pid,Result} ->
        receive {'DOWN',Ref,process,Pid,normal} -> Result
        after 10000 -> error(worker_not_settled) end
    after 10000 -> exit(Pid,kill),error(worker_timeout) end.

worker(F,A,Parent) -> Parent ! {self(),outcome(F,A)}.

outcome(F,A) ->
    try apply(unicode,F,A) of V -> {ok,V}
    catch C:R:S -> {exception,C,R,S}
    end.

encodings() -> [latin1,unicode,utf8,utf16,{utf16,big},{utf16,little},utf32,{utf32,big},{utf32,little}].

cases() ->
    Es = encodings(),
    Bad = [bad,42,{}, {utf8,big}, {utf16,bad}, {utf32}, [utf8]],
    Scalars = [-1,0,1,9,32,65,127,128,255,256,16#7ff,16#800,16#d7ff,16#d800,
        16#dbff,16#dc00,16#dfff,16#e000,16#fffe,16#ffff,16#10000,16#10ffff,16#110000,1 bsl 80],
    Data = [[],<<>>,<<1:1>>,bad,1,{},[bad],[65|66],[65|bad],[bad,66],
        [65,bad,66],[[65|66]], [1 bsl 80,bad], [256,bad],
        [65], [65,[66,[],<<67>>]], [<<65>>|<<66>>], [<<195>>,[],[<<165>>]],
        [<<195>>,65], [<<195>>,bad], [<<195>>|<<165>>]] ++
        [[S] || S <- Scalars] ++ [<<B>> || B <- lists:seq(0,255)] ++
        [<<A,B>> || A <- [0,16#c0,16#c2,16#df,16#e0,16#ed,16#ef,16#f0,16#f4,16#f5,16#ff], B <- [0,127,128,159,160,191,192,255]],
    Conversion = [{characters_to_binary,[D,I,O]} || D <- Data, I <- Es, O <- Es] ++
        [{F,[D,E]} || F <- [characters_to_binary,characters_to_list,characters_to_binary_int,characters_to_list_int],D <- Data,E <- Es++Bad] ++
        [{F,[D]} || F <- [characters_to_binary,characters_to_list,bin_is_7bit,bom_to_encoding], D <- Data] ++
        [{characters_to_binary,[D,I,O]} || D <- [[],<<>>,bad], I <- Es++Bad, O <- Bad] ++
        [{encoding_to_bom,[E]} || E <- Es++Bad] ++
        [{bom_to_encoding,[B]} || B <- [<<239,187,191,65>>,<<255,254,0,0,65>>,<<0,0,254,255>>,<<254,255>>,<<255,254>>,<<239,187>>,<<255>>,<<0,0,254>>]],
    Split = lists:append([begin
        B = unicode:characters_to_binary([0,65,255,256,16#20ac,16#1f600],unicode,E),
        [{characters_to_binary,[[binary:part(B,0,N),[],[binary:part(B,N,byte_size(B)-N)]],E,O]} ||
            N <- lists:seq(0,byte_size(B)),O <- Es]
        end || E <- Es, E =/= latin1]),
    NFs = [characters_to_nfd_list,characters_to_nfd_binary,characters_to_nfkd_list,characters_to_nfkd_binary,
        characters_to_nfc_list,characters_to_nfc_binary,characters_to_nfkc_list,characters_to_nfkc_binary],
    NormData = Data ++ [[65,778,16#301], [16#fb03,16#212b,16#ac01], [16#1100,16#1161,16#11a8],
        lists:duplicate(405,16#e5), [65,<<255>>,66], [<<195>>]],
    Properties = [is_whitespace,is_id_start,is_id_continue,category],
    CPs = lists:usort(Scalars ++ lists:seq(0,512) ++ lists:seq(4990,5010) ++ lists:seq(8190,8300) ++
        [16#b7,16#387,16#1885,16#1886,16#2118,16#212e,16#309b,16#309c,16#200c,16#200d,16#10fffd]),
    Conversion ++ Split ++ [{F,[D]} || F <- NFs,D <- NormData] ++
        [{F,[C]} || F <- Properties,C <- CPs++[bad,[],1.0,<<65>>]].

write_extra(Directory) ->
    {ok,Bytes} = file:read_file(filename:join(Directory,"original.etf")),
    Rows = binary_to_term(Bytes),
    Signatures = lists:usort([{F,length(A)} || {F,A,_} <- Rows]),
    Small = lists:append([begin
        Group = [R || R={F,A,_} <- Rows,F=:=Name,length(A)=:=Arity],
        lists:sublist(Group,4) ++ lists:sublist(lists:reverse(Group),4)
        end || {Name,Arity} <- Signatures]),
    Edges = [R || R={_,[D|_],_} <- Rows,D=:=[-1] orelse D=:=[<<195>>,65]],
    ok = file:write_file(filename:join(Directory,"contracts.etf"),term_to_binary(lists:usort(Small++Edges))),
    Extra = [{F,A,run(F,A)} || {F,A} <- extra_cases()],
    ok = file:write_file(filename:join(Directory,"extra.etf"),term_to_binary(Extra)),
    ok = file:write_file(filename:join(Directory,"backing.etf"),term_to_binary(backing())).

backing() ->
    Parent = binary:copy(<<"a",195,165>>,100),
    AsciiParent = binary:copy(<<"abc">>,100),
    Sub = binary:part(Parent,3,150),
    Ascii = binary:part(AsciiParent,3,150),
    [inspect(unicode:characters_to_binary(D,E)) ||
        D <- [Sub,[Sub],Ascii,[Ascii]], E <- [utf8,unicode,latin1]] ++
    [inspect(unicode:characters_to_binary(D,I,O)) ||
        D <- [Sub,Ascii],{I,O} <- [{utf8,utf8},{utf8,latin1},{latin1,utf8},{latin1,latin1}]] ++
    [inspect(unicode:characters_to_binary(lists:duplicate(N,65),utf16,latin1)) ||
        N <- [1,64,65,128,129,150,256,257,300]].

inspect(B) when is_binary(B) -> {B,binary:referenced_byte_size(B)};
inspect([H|T]) -> [inspect(H)|inspect(T)];
inspect(T) when is_tuple(T) -> list_to_tuple([inspect(E) || E <- tuple_to_list(T)]);
inspect(T) -> T.

extra_cases() ->
    Es = encodings(),
    lists:append([begin
        B = unicode:characters_to_binary(lists:duplicate(5000,229),unicode,E),
        D = [B,<<B/binary,255>>,[B,-1],[B,16#110000],
             [binary:part(B,0,8191),[binary:part(B,8191,byte_size(B)-8191)]]],
        [{characters_to_binary,[Input,E,O]} || Input <- D,O <- Es] ++
        [{characters_to_list,[Input,E]} || Input <- D]
        end || E <- Es,E =/= latin1]) ++
    [{F,[lists:foldl(fun(_,Acc)->[Acc] end,[65,778],lists:seq(1,100))]} ||
        F <- [characters_to_nfc_list,characters_to_nfc_binary,characters_to_nfd_list,characters_to_nfd_binary,
              characters_to_nfkc_list,characters_to_nfkc_binary,characters_to_nfkd_list,characters_to_nfkd_binary]].
