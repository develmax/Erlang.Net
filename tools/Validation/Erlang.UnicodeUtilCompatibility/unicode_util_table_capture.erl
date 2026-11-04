-module(unicode_util_table_capture).
-export([write/1,write_detail/1]).

write(Directory) ->
    crypto:start(),
    Rows=[begin
        Last=min(First+4095,16#10ffff),
        Digest=crypto:hash(sha256,[table_bytes(CP)||CP<-lists:seq(First,Last)]),
        {First,Last,Digest}
    end || First<-lists:seq(0,16#10ffff,4096)],
    file:write_file(filename:join(Directory,"tables.etf"),term_to_binary(Rows)).

table_bytes(CP) ->
    #{ccc:=CCC,canon:=Can,compat:=Compat,category:={Major,Minor}}=unicode_util:lookup(CP),
    {Major,Minor}=unicode_util:category(CP),
    #{upper:=U,lower:=L,title:=T,fold:=F}=unicode_util:get_case(CP),
    [<<CP:32/little,CCC:8>>,word(Major),word(Minor),tagged(Can),compat(Compat),
        mapping(U),mapping(L),mapping(T),mapping(F),
        [bool(apply(unicode_util,Fun,[CP])) || Fun<-[is_wide,is_whitespace,is_other_id_start,
            is_other_id_continue,is_letter_not_pattern_syntax]],
        [shape(unicode_util:gc(Probe)) || Probe <- probes(CP)]].
probes(CP) -> [[CP,769,65],[65,CP,66],[CP,65,66],[CP,8205,128512,65],
    [CP,2381,2325],[2325,CP,2325],[CP,4352,4449,4520,65],[4352,CP,4449,4520,65]].
shape(I) when is_integer(I)-><<0,I:32/little>>;
shape([])-><<1>>;
shape([H|T])->[<<2>>,shape(H),shape(T)].
word(A)->B=atom_to_binary(A,utf8),[<<(byte_size(B)):32/little>>,B].
tagged(L)->[<<(length(L)):32/little>>,[<<C:8,P:32/little>>||{C,P}<-L]].
compat([])-><<0>>;
compat({Tag,L})->[<<1>>,word(Tag),tagged(L)].
mapping(P) when is_integer(P)-><<0,P:32/little>>;
mapping(L)->[<<1,(length(L)):32/little>>,[<<P:32/little>>||P<-L]].
bool(true)->1;
bool(false)->0.

write_detail(Directory)->
    Rows=[{CP,[unicode_util:gc(P)||P<-probes(CP)]}||CP<-lists:seq(4096,8191)],
    file:write_file(filename:join(Directory,"transition-details.etf"),term_to_binary(Rows)).
