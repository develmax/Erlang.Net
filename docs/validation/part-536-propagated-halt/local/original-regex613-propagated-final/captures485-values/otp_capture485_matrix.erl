-module(otp_capture485_matrix).
-export([worker/1,count/0]).
count()->length(cases()).
cases()->
 B=binary:copy(<<97>>,200),Bits = <<0:3,B/binary,0:5>>,
 <<_:3,U:800/bitstring,_/bitstring>>=Bits,
 Ss=[[],"ab","a","b",<<>>,<<"ab">>,<<"a">>,<<"b">>,B,binary_to_list(B),U,42,#{},{} ,[97|bad],<<1:3>>],
 Ps=["a(.)","(a)?(b)?","(a+)","(b)|(a)","(a)(z)?","()","x","(?<=a)b",<<"(a+)">>,"(",[]],
 [{S,P}||S<-Ss,P<-Ps].
worker({P,I})->
 {S,Re}=lists:nth(I,cases()),
 R=try re:run(S,Re,[{capture,all,binary}]) of V->{returned,V,meta(V)} catch C:E:St->{caught,C,E,St} end,
 P!{finished,I,{S,Re,R}}.
meta({match,Bs})->[{byte_size(B),binary:referenced_byte_size(B)}||B<-Bs];meta(_)->[].
