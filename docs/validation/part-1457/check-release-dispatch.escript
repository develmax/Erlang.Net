#!/usr/bin/env escript
%%! -nostick
-mode(compile).

main([DnsReference, DnsGenerated, DiameterReference, DiameterGenerated]) ->
    Dns = run(inet_dns, DnsReference, fun dns_round_trip/0),
    Dns = run(inet_dns, DnsGenerated, fun dns_round_trip/0),
    Diameter = run(diameter_gen_base_rfc3588, DiameterReference, fun diameter_headers/0),
    Diameter = run(diameter_gen_base_rfc3588, DiameterGenerated, fun diameter_headers/0),
    io:format("Known and raw DNS query types round-trip in both images: ~0p~n", [Dns]),
    io:format("Diameter headers and invalid-key failure match: ~0p~n", [Diameter]).

run(Module, Path, Action) ->
    {module, Module} = code:load_abs(filename:rootname(filename:absname(Path))),
    Result = Action(),
    true = code:delete(Module),
    code:purge(Module),
    Result.

dns_round_trip() ->
    [begin
        Query = <<16#1234:16,16#0100:16,1:16,0:16,0:16,0:16,7,"example",3,"com",0,Type:16,1:16>>,
        {ok, Record} = inet_dns:decode(Query),
        Query = inet_dns:encode(Record),
        Type
     end || Type <- [1,28,65000]].

diameter_headers() ->
    Module = diameter_gen_base_rfc3588,
    [{483,64,undefined},{260,64,undefined},badarg] =
        [Module:avp_header('Accounting-Realtime-Required'),
         Module:avp_header('Vendor-Specific-Application-Id'),
         try Module:avp_header(unknown) catch error:Reason -> Reason end].
