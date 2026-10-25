-module(otp_management473_observer).
-export([worker/1,run/0,native_worker/1,native_run/0]).
-file("otp_management473_observer.erl",1).

worker(Parent) -> Parent ! {finished,run()}.
native_worker(Parent) -> Parent ! {finished,native_run()}.
native_run() ->
    [observe({diameter_uri,V,Rfc},fun()->diameter_types:'DiameterURI'(decode,V,#{rfc=>Rfc,string_decode=>true}) end)||Rfc<-[3588,6733],V<-[<<"aaa://example.org">>,<<"aaas://example.org:5868;transport=tcp;protocol=diameter">>,<<"aaa://host:3868;transport=sctp">>,<<"bad">>,<<>>,bad]] ++
    [observe({snmp_counter64_raw,V},fun()->snmp_pdus:dec_value(snmp_pdus:enc_value('Counter64',V)) end)||V<-[-1,0,1,1 bsl 63,(1 bsl 64)-1,1 bsl 64]].
run() ->
    lists:append([diameter_number(T,N)||T<-['Integer32','Integer64','Unsigned32','Unsigned64'],N<-[zero,-1,0,1,127,128,255,256,-128,-129,(1 bsl 31)-1,1 bsl 31,-(1 bsl 31),(1 bsl 32)-1,1 bsl 32,(1 bsl 63)-1,1 bsl 63,-(1 bsl 63),(1 bsl 64)-1,1 bsl 64,bad]]) ++
    lists:append([diameter_number(T,V)||T<-['Float32','Float64'],V<-[zero,infinity,'-infinity',0.0,-0.0,1.5,-3.141592653589793,1.0e300,bad]]) ++
    [observe({diameter_decode,T,B},fun()->apply(diameter_types,T,[decode,B,#{}]) end)||T<-['Integer32','Integer64','Unsigned32','Unsigned64','Float32','Float64','Time','Address'],B<-[<<>>,<<0>>,<<0:32>>,<<255:64>>,<<255,255,255,255>>,<<127,128,0,0>>,<<127,240,0,0,0,0,0,0>>,bad]] ++
    lists:append([diameter_strings(T,V)||T<-['OctetString','UTF8String','DiameterIdentity','IPFilterRule','QoSFilterRule'],V<-[zero,[],"abc",<<"example.org">>,[16#3bb],[16#1f600],<<255>>,bad]]) ++
    lists:append([diameter_number('Address',V)||V<-[zero,{127,0,0,1},{16#2001,16#db8,0,0,0,0,0,1},{3,<<1,2>>},"192.0.2.1","::1",bad]]) ++
    lists:append([diameter_number('Time',V)||V<-[zero,{{1968,1,20},{3,14,8}},{{1970,1,1},{0,0,0}},{{2000,2,29},{23,59,59}},{{2036,2,7},{6,28,15}},{{2036,2,7},{6,28,16}},{{2104,2,26},{9,42,23}},{{2104,2,26},{9,42,24}},bad]]) ++
    [observe({diameter_ip,V},fun()->diameter_lib:ipaddr(V) end)||V<-[{127,0,0,1},{0,0,0,0,0,0,0,1},"127.0.0.1","2001:db8::1","not-an-ip",bad]] ++
    [observe({snmp_bits,N},fun()->{snmp_pdus:bits_to_str(N),snmp_pdus:octet_str_to_bits(snmp_pdus:bits_to_str(N))} end)||N<-[0,1,2,127,128,255,256,65535,1 bsl 128]] ++
    lists:append([snmp_value(T,V)||{T,Vs}<-values(),V<-Vs]) ++
    lists:append([snmp_pdu(P)||P<-pdus()]) ++
    [observe({snmp_decode,B},fun()->snmp_pdus:dec_value(B) end)||B<-[[],[2],[2,1],[2,1,255],[5,0],[128,0],[129,0],[130,0],[4,3,1],[6,0],[255,0],bad]] ++
    [observe({snmp_length,N},fun()->snmp_pdus:get_encoded_length(N) end)||N<-[0,1,127,128,255,256,65535,65536,1 bsl 64,-1,bad]] ++
    [observe({snmp_oid,V},fun()->snmp_misc:is_oid(V) end)||V<-[[],[1,3,6,1,2,1],[0,0],[2,99999],[-1,2],[1|bad],bad]] ++
    [observe({snmp_security,N},fun()->{snmp_misc:is_auth(N),snmp_misc:is_priv(N),snmp_misc:is_reportable(N)} end)||N<-lists:seq(0,15)] ++
    [observe({snmp_flags,N},fun()->{snmp_misc:is_auth(N),snmp_misc:is_priv(N),snmp_misc:is_reportable([N]),snmp_misc:get_sec_level([N])} end)||N<-lists:seq(0,15)] ++
    [observe({snmp_message_flags,T,N},fun()->snmp_misc:mk_msg_flags(T,N) end)||T<-['get-request','get-response','inform-request','snmpv2-trap'],N<-lists:seq(0,3)] ++
    [observe({snmp_scoped,P},fun()->S={scopedPdu,"engine","context",P},snmp_pdus:dec_scoped_pdu(snmp_pdus:enc_scoped_pdu(S)) end)||P<-pdus()] ++
    [observe({snmp_usm,B,T},fun()->S={usmSecurityParameters,"engine",B,T,"user",[],[]},snmp_pdus:dec_usm_security_parameters(snmp_pdus:enc_usm_security_parameters(S)) end)||B<-[0,1,2147483647],T<-[0,1,2147483647]] ++
    [observe({snmp_counter64_tlv,N},fun()->snmp_pdus_basic:dec_Counter64({65542,<<N:72/signed>>},[65542]) end)||N<-[-1,0,1,1 bsl 63,(1 bsl 64)-1,1 bsl 64]].

diameter_number(T,V) ->
    [observe({diameter_encode,T,V},fun()->apply(diameter_types,T,[encode,V,#{}]) end),
     observe({diameter_roundtrip,T,V},fun()->apply(diameter_types,T,[decode,apply(diameter_types,T,[encode,V,#{}]),#{}]) end)].
diameter_strings(T,V) ->
    [observe({diameter_string,T,V,D},fun()->B=apply(diameter_types,T,[encode,V,#{}]),{B,apply(diameter_types,T,[decode,B,#{string_decode=>D}])} end)||D<-[false,true]].
values() ->
    [{'INTEGER',[-(1 bsl 128),-65536,-129,-128,-1,0,1,127,128,255,256,65535,1 bsl 128]},
     {'Counter32',[-1,0,1,(1 bsl 31)-1,1 bsl 31,(1 bsl 32)-1,1 bsl 32]},
     {'Unsigned32',[-1,0,1,1 bsl 31,(1 bsl 32)-1,1 bsl 32]},
     {'Counter64',[-1,0,1,1 bsl 63,(1 bsl 64)-1,1 bsl 64]},
     {'TimeTicks',[-1,0,1,(1 bsl 32)-1,1 bsl 32]},
     {'OCTET STRING',[[],"abc",[0,127,255],lists:duplicate(128,42),bad]},
     {'OBJECT IDENTIFIER',[[0,0],[1,3,6,1,2,1],[2,99999,128,16384],[],bad]},
     {'IpAddress',[{127,0,0,1},[192,0,2,1],[],bad]},
     {'Opaque',[[],[0,255,1]]},{'BITS',[0,1,255,256,1 bsl 63]},
     {'NULL',['NULL',noSuchObject,noSuchInstance,endOfMibView]}].
snmp_value(T,V) ->
    [observe({snmp_encode,T,V},fun()->snmp_pdus:enc_value(T,V) end),
     observe({snmp_varbind,T,V},fun()->snmp_pdus:enc_varbind({varbind,[1,3,6,1,2,1,1,1,0],T,V,1}) end)] ++
    case T of
        'Counter64' -> [];
        _ -> [observe({snmp_roundtrip,T,V},fun()->snmp_pdus:dec_value(snmp_pdus:enc_value(T,V)) end)]
    end.
pdus() ->
    [{pdu,T,N,E,I,[{varbind,[1,3,6,1,2,1,1,1,0],'OCTET STRING',"test",1}]}||T<-['get-request','get-next-request','get-bulk-request','get-response','set-request','inform-request','snmpv2-trap',report],N<-[0,1,2147483647],{E,I}<-[case T of 'get-bulk-request'->{0,3};_->{noError,0} end]] ++ [bad].
snmp_pdu(P) ->
    [observe({snmp_pdu_encode,P},fun()->snmp_pdus:enc_pdu(P) end),
     observe({snmp_pdu_roundtrip,P},fun()->snmp_pdus:dec_pdu(snmp_pdus:enc_pdu(P)) end),
     observe({snmp_message,P},fun()->snmp_pdus:dec_message(snmp_pdus:enc_message({message,'version-2',"public",P})) end)].
observe(Name,F) ->
    try F() of V -> {Name,{returned,V}}
    catch C:R:S -> {Name,{caught,C,R,S}}
    end.
