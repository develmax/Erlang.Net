namespace Erlang.Differential;

public static class PacketBoundaryCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-packet-boundary-" + name,
            "-module(packet_boundary_contract). -export([run/0]). run()->" + body + ",done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));

        foreach (var (type, packet, expected, header) in new[]
        {
            ("1", "<<2,97,98>>", "<<97,98>>", 1),
            ("2", "<<0,2,97,98>>", "<<97,98>>", 2),
            ("4", "<<0,0,0,2,97,98>>", "<<97,98>>", 4),
            ("asn1", "<<4,2,97,98>>", "<<4,2,97,98>>", 2),
            ("sunrm", "<<128,0,0,2,97,98>>", "<<128,0,0,2,97,98>>", 4),
            ("cdr", "<<\"GIOP\",17,34,0,99,0,0,0,2,97,98>>", "<<\"GIOP\",17,34,0,99,0,0,0,2,97,98>>", 12),
            ("fcgi", "<<1,99,17,34,0,2,2,77,97,98,0,0>>", "<<1,99,17,34,0,2,2,77,97,98>>", 8),
            ("tpkt", "<<3,77,0,6,97,98>>", "<<3,77,0,6,97,98>>", 4),
            ("ssl_tls", "<<25,34,17,0,2,97,98>>", "{ssl_tls,[],25,{34,17},<<97,98>>}", 5)
        })
        {
            Add(
                "bit-offset-" + type,
                "Packet= " + packet + ",Rest= <<123,34,0,250>>,lists:foreach(fun(N)->Original= <<0:N,Packet/binary,Rest/binary>>,<<_:N,B/binary>>=Original,{ok," + expected + ",Rest}=erlang:decode_packet(" + type + ",B,[]) end,lists:seq(0,32))"
            );
            Add(
                "every-incomplete-prefix-" + type,
                "Packet= " + packet + ",Size=byte_size(Packet),lists:foreach(fun(N)-><<B:N/binary,_/binary>>=Packet,Wanted=case N<" + header + " of true->undefined;false->Size end,{more,Wanted}=erlang:decode_packet(" + type + ",B,[]) end,lists:seq(0,Size-1))"
            );
        }
        foreach (var (type, packet, expected) in new[]
        {
            ("raw", "<<97,98>>", "<<97,98,123,34,0,250>>"),
            ("line", "<<97,10>>", "<<97,10>>"),
            ("http", "<<\"GET / HTTP/1.1\\r\\n\">>", "{http_request,'GET',{abs_path,\"/\"},{1,1}}"),
            ("http_bin", "<<\"GET / HTTP/1.1\\r\\n\">>", "{http_request,'GET',{abs_path,<<\"/\">>},{1,1}}"),
            ("httph", "<<\"Host: a\\r\\n\">>", "{http_header,14,'Host',\"Host\",\"a\"}"),
            ("httph_bin", "<<\"Host: a\\r\\n\">>", "{http_header,14,'Host',<<\"Host\">>,<<\"a\">>}"),
            ("ssl_tls", "<<128,5,1,3,0,97,98>>", "{ssl_tls,[],22,{3,0},<<1,0,0,4,3,0,97,98>>}")
        })
            Add(
                "extra-bit-offset-" + type,
                "Packet= " + packet + ",Rest= <<123,34,0,250>>,lists:foreach(fun(N)->Original= <<0:N,Packet/binary,Rest/binary>>,<<_:N,B/binary>>=Original,{ok," + expected + "," + (type == "raw" ? "<<>>" : "Rest") + "}=erlang:decode_packet(" + type + ",B,[]) end,lists:seq(0,32))"
            );
        Add(
            "unaligned-remainder-rejected",
            "lists:foreach(fun(T)->ok=try erlang:decode_packet(T,<<2,97,98,1:1>>,[]) of _->unexpected catch error:badarg->ok end end,[0,1,2,4,line,http,httph,ssl_tls])"
        );
        Add(
            "full-uint-option-boundary",
            "{ok,<<1>>,<<>>}=erlang:decode_packet(raw,<<1>>,[{packet_size,4294967295},{line_length,4294967295}]),{more,undefined}=erlang:decode_packet(line,<<1>>,[{line_length,4294967295}])"
        );
        Add(
            "negative-lengths",
            "lists:foreach(fun(N)->{error,invalid}=erlang:decode_packet(4,<<N:32,1,2,3>>,[]) end,lists:seq(-10,-1))"
        );
        Add(
            "negative-option-locations",
            "lists:foreach(fun(O)->F=fun(Opts)->ok=try erlang:decode_packet(raw,<<1>>,Opts) of _->unexpected catch error:badarg->ok end end,F(O),F([O]),F([O,{packet_size,1000}]),F([{packet_size,1000},O]) end,[2,false,self(),<<1>>,\"Options\",fun()->dummy end,packet_size,{packet_size},{packet_size,0,false},{packet_size,-1},{packet_size,100.0},{packet_size,false},{line_length,-1},{line_length,100.0},{line_length,false}])"
        );
        Add(
            "stream-http-boundaries",
            "B= <<\"GET / HTTP/1.1\\r\\nHost: a\\r\\n\\r\\nbody\">>,{ok,{http_request,'GET',{abs_path,\"/\"},{1,1}},R1}=erlang:decode_packet(http,B,[]),{ok,{http_header,14,'Host',\"Host\",\"a\"},R2}=erlang:decode_packet(httph,R1,[]),{ok,http_eoh,<<\"body\">>}=erlang:decode_packet(httph,R2,[])"
        );
        Add(
            "stream-line-boundaries",
            "B= <<1,2,3,10,4,5,10,6>>,{ok,<<1,2>>,R1}=erlang:decode_packet(line,B,[{line_length,2}]),{ok,<<3,10>>,R2}=erlang:decode_packet(line,R1,[{line_length,2}]),{ok,<<4,5>>,R3}=erlang:decode_packet(line,R2,[{line_length,2}]),{ok,<<10>>,<<6>>}=erlang:decode_packet(line,R3,[{line_length,2}])"
        );

        return cases;
    }
}
