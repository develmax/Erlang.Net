-module(otp_tls481_observer).
-export([worker/1,run/0]).
-file("otp_tls481_observer.erl",1).
worker(Parent)->Parent!{finished,run()}.
run()->[one(Api,Type,V)||Api<-[decode,hello],Type<-[client_hello,server_hello,client,server],V<-vectors()].
one(Api,Type,{Name,Bin})->
 Result=try case Api of
  decode->ssl_handshake:decode_extensions(Bin,{3,4},Type);
  hello->ssl_handshake:decode_hello_extensions(Bin,{3,4},{3,3},Type)
 end of Value->{returned,Value} catch C:R:S->{caught,C,R,S} end,
 {Api,Type,Name,Result}.
vectors()->[
 {empty,<<>>},
 {one_version,<<43:16,3:16,2,3,4>>},
 {two_versions,<<43:16,5:16,4,3,4,3,3>>},
 {selected_version,<<43:16,2:16,3,4>>},
 {odd_versions,<<43:16,4:16,3,3,4,3>>},
 {duplicate_versions,<<43:16,2:16,3,4,43:16,2:16,3,3>>},
 {truncated_header,<<43:16>>},
 {truncated_payload,<<43:16,8:16,2,3,4>>},
 {unknown_extension,<<65500:16,2:16,1,2>>},
 {empty_shares,<<51:16,2:16,0:16>>},
 {one_share,<<51:16,10:16,8:16,29:16,4:16,1,2,3,4>>},
 {truncated_share,<<51:16,5:16,4:16,29:16,0>>}
].
