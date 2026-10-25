-module(otp_network472_observer).
-export([worker/1,run/0]).
-file("otp_network472_observer.erl",1).

worker(Parent) -> Parent ! {finished,run()}.
run() ->
    [observe({http_status,N},fun()->httpd_util:reason_phrase(N) end)||N<-lists:seq(99,600)] ++
    [observe({ftp_status,N},fun()->ftp_response:interpret(integer_to_list(N)++" reply\r\n") end)||N<-lists:seq(100,599)] ++
    [observe({ftp_error,E},fun()->{ftp_response:error_string(E),ftp_response:error_string({error,E})} end)||E<-[echunk,eclosed,econn,eexists,ehost,elogin,enotbinary,epath,etype,euser,etnospc,enofile,epnospc,efnamena]] ++
    [observe({http_month,N},fun()->http_util:convert_month(N) end)||N<-lists:seq(0,13)] ++
    [observe({http_month,S},fun()->http_util:convert_month(S) end)||S<-["Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec","Bad"]] ++
    [observe({http_day,N},fun()->http_util:convert_day(N) end)||N<-lists:seq(0,8)] ++
    [observe({http_day,S},fun()->http_util:convert_day(S) end)||S<-["Mon","Tue","Wed","Thu","Fri","Sat","Sun","Bad"]] ++
    [observe({http_case,S},fun()->{http_util:to_lower(S),http_util:to_upper(S)} end)||S<-[[],"AbC-123","HEADER",[16#df],[16#3a3,16#3c2],<<"AbC">>]] ++
    [observe({http_html,S},fun()->http_util:html_encode(S) end)||S<-[[],"plain","<a href='x'>&\"/",[16#3bb]]] ++
    [observe({http_hex,N},fun()->{http_util:integer_to_hexlist(N),http_util:hexlist_to_integer(http_util:integer_to_hexlist(N))} end)||N<-[0,1,15,16,255,256,65535,1 bsl 256,-1]] ++
    [observe({http_tokens,S},fun()->http_util:connection_tokens(S) end)||S<-[undefined,[],"Keep-Alive, Upgrade"," CLOSE ,\tKEEP-ALIVE ",",,,",bad]] ++
    [observe({http_date,D},fun()->date_format(D) end)||D<-[{{2026,10,25},{0,0,0}},{{2000,2,29},{23,59,59}},{{1970,1,1},{0,0,1}},bad]] ++
    [observe({ftp_stream,B,N},fun()->ftp_split(B,N) end)||B<-[<<"220 ready\r\n">>,<<"123-first\r\n234 middle\r\n123 done\r\nNEXT">>,<<"250 ok\r\nrest">>],N<-lists:seq(0,byte_size(B))] ++
    lists:append([packet(M)||M<-messages()]) ++
    [observe({tftp_decode,B},fun()->tftp_lib:decode_msg(B) end)||B<-[<<>>,<<0>>,<<0,7>>,<<0,1>>,<<0,1,"x",0>>,<<0,1,"x",0,"OCTET",0,"blksize",0>>,<<0,3,0>>,<<0,4,0,1,0>>,<<0,5,0,1,"bad">>,<<0,6,"BLKSIZE",0,"512",0>>,bad]] ++
    [observe({tftp_replace,K,V,L},fun()->tftp_lib:replace_val(K,V,L) end)||{K,V,L}<-[{a,1,[]},{a,1,[{a,1},{b,2}]},{a,3,[{a,1},{a,2}]},{a,3,[{b,2}]}]] ++
    [observe({ldap_scope,F},fun()->apply(eldap,F,[]) end)||F<-[baseObject,singleLevel,wholeSubtree,neverDerefAliases,derefInSearching,derefFindingBaseObj,derefAlways]] ++
    [observe({ldap_filter,F,D,V},fun()->apply(eldap,F,[D,V]) end)||F<-[equalityMatch,greaterOrEqual,lessOrEqual,approxMatch],{D,V}<-[{"cn","Alice"},{[],[]},{bad,42},{<<"cn">>,<<0,255>>}]] ++
    [observe({ldap_present,V},fun()->eldap:present(V) end)||V<-[[],"cn",bad]] ++
    [observe({ldap_modify,F,D,V},fun()->apply(eldap,F,[D,V]) end)||F<-[mod_add,mod_delete,mod_replace],{D,V}<-[{"cn",["a","b"]},{[],[]},{bad,42}]] ++
    [observe({ldap_boolean,F,V},fun()->apply(eldap,F,[V]) end)||F<-['and','or','not'],V<-[[],[{present,"cn"}],{present,"cn"},bad]] ++
    [observe({ldap_substrings,V},fun()->eldap:substrings("cn",V) end)||V<-[[],[{initial,"A"},{any,"b"},{final,"c"}],[{bad,"x"}],bad]] ++
    [observe({ldap_extensible,V},fun()->eldap:extensibleMatch("value",V) end)||V<-[[],[{type,"cn"},{matchingRule,"caseExactMatch"},{dnAttributes,true}],[{dnAttributes,false}],[{bad,42}]]] ++
    [observe({ldap_dn,V},fun()->eldap:parse_dn(V) end)||V<-[[],"cn=Alice,dc=example,dc=org","cn=A+uid=7,ou=People","cn=A\\, B,dc=example","cn=\\41lice","cn=#04024142","bad","=bad",",cn=a",bad]] ++
    [observe({ldap_url,V},fun()->eldap:parse_ldap_url(V) end)||V<-["ldap://localhost","ldap://example.org/dc=example,dc=org","ldap://example.org:1389/cn=Alice?cn,sn?sub?(uid=alice)","ldaps://localhost","ldap://","bad",bad]] ++
    [observe({ftp_bad,V},fun()->ftp_response:interpret(V) end)||V<-[[],"0","600 bad","999 bad",bad]].

date_format(bad) -> httpd_util:rfc1123_date(bad);
date_format(Utc) -> httpd_util:rfc1123_date(calendar:universal_time_to_local_time(Utc)).
ftp_split(B,N) ->
    <<First:N/binary,Second/binary>>=B,
    R=ftp_response:parse_lines(First,[],start),
    case R of
        {continue,{Pending,Lines,Status}} -> {R,ftp_response:parse_lines(<<Pending/binary,Second/binary>>,Lines,Status)};
        _ -> {R,Second}
    end.
messages() ->
    [{tftp_msg_req,A,"File.Name","OCTET",O,undefined}||A<-[read,write],O<-[[],[{"blksize","1024"},{"timeout","3"}]]] ++
    [{tftp_msg_data,N,<<0,1,255>>}||N<-[0,1,65535,-1,65536]] ++
    [{tftp_msg_ack,N}||N<-[0,1,65535,-1,65536]] ++
    [{tftp_msg_error,C,"error text",undefined}||C<-[undef,enoent,eacces,enospc,badop,badblk,eexist,baduser,badopt,9,65535,-1,65536,bad]] ++
    [{tftp_msg_oack,[]},{tftp_msg_oack,[{"blksize","512"}]},bad].
packet(M) ->
    [observe({tftp_encode,M},fun()->tftp_lib:encode_msg(M) end),
     observe({tftp_wire,M},fun()->iolist_to_binary(tftp_lib:encode_msg(M)) end),
     observe({tftp_roundtrip,M},fun()->tftp_lib:decode_msg(iolist_to_binary(tftp_lib:encode_msg(M))) end)].
observe(Name,F) ->
    try F() of V -> {Name,{returned,V}}
    catch C:R:S -> {Name,{caught,C,R,S}}
    end.
