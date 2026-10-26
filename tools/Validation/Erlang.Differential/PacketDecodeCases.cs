namespace Erlang.Differential;

public static class PacketDecodeCases
{
    public static string? FixtureName(string function, int arity) => function == PacketDecodeContractNames.Function && arity == 3 ? PacketDecodeContractNames.RawFixture : null;

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-packet-decode-" + name,
            "-module(packet_decode_contract). -export([run/0]). run()->" + body + ",done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));

        Add(
            "raw",
            "{ok,<<1,2,3>>,<<>>}=erlang:decode_packet(0,<<1,2,3>>,[]),{ok,<<1,2,3>>,<<>>}=erlang:decode_packet(raw,<<1,2,3>>,[{packet_size,1},{line_length,1}])"
        );
        Add("raw-empty", "{more,undefined}=erlang:decode_packet(raw,<<>>,[])");
        foreach (int width in new[] { 1, 2, 4 })
        {
            string head = width == 1 ? "3" : width == 2 ? "0,3" : "0,0,0,3";
            string zero = string.Join(",", Enumerable.Repeat("0", width));
            Add("framed-" + width, "{ok,<<1,2,3>>,<<9>>}=erlang:decode_packet(" + width + ",<<" + head + ",1,2,3,9>>,[])");
            Add("framed-more-" + width, "{more," + (width + 3) + "}=erlang:decode_packet(" + width + ",<<" + head + ",1>>,[])");
            Add("framed-zero-" + width, "{ok,<<>>,<<9>>}=erlang:decode_packet(" + width + ",<<" + zero + ",9>>,[])");
            Add("framed-limit-" + width, "{error,invalid}=erlang:decode_packet(" + width + ",<<" + head + ">>,[{packet_size,2}])");
            Add("framed-exact-limit-" + width, "{more," + (width + 3) + "}=erlang:decode_packet(" + width + ",<<" + head + ">>,[{packet_size,3}])");
            Add(
                "framed-duplicate-" + width,
                "{more," + (width + 3) + "}=erlang:decode_packet(" + width + ",<<" + head + ">>,[{packet_size,2},{packet_size,3}])"
            );
        }
        Add(
            "framed-short-header",
            "{more,undefined}=erlang:decode_packet(1,<<>>,[]),{more,undefined}=erlang:decode_packet(2,<<0>>,[]),{more,undefined}=erlang:decode_packet(4,<<0,0,0>>,[])"
        );
        Add(
            "framed-default-signed-bound",
            "{error,invalid}=erlang:decode_packet(4,<<128,0,0,0>>,[]),{error,invalid}=erlang:decode_packet(4,<<255,255,255,255>>,[])"
        );
        Add(
            "line",
            "{ok,<<97,10>>,<<98>>}=erlang:decode_packet(line,<<97,10,98>>,[]),{more,undefined}=erlang:decode_packet(line,<<97>>,[])"
        );
        Add(
            "line-delimiter",
            "{ok,<<97,0>>,<<98>>}=erlang:decode_packet(line,<<97,0,98>>,[{line_delimiter,0}]),{ok,<<255>>,<<>>}=erlang:decode_packet(line,<<255>>,[{line_delimiter,255}])"
        );
        Add("line-truncate-no-delimiter", "{ok,<<1,2>>,<<3>>}=erlang:decode_packet(line,<<1,2,3>>,[{line_length,2}])");
        Add("line-truncate-delimiter", "{ok,<<1,2>>,<<3,10,9>>}=erlang:decode_packet(line,<<1,2,3,10,9>>,[{line_length,2}])");
        Add(
            "line-limit-precedes-truncation",
            "{error,invalid}=erlang:decode_packet(line,<<1,2,3>>,[{packet_size,2},{line_length,1}]),{error,invalid}=erlang:decode_packet(line,<<1,2,10>>,[{packet_size,2},{line_length,1}])"
        );
        Add(
            "line-equal-limit-still-more",
            "{more,undefined}=erlang:decode_packet(line,<<1,2>>,[{packet_size,2}]),{ok,<<1,10>>,<<9>>}=erlang:decode_packet(line,<<1,10,9>>,[{packet_size,2}])"
        );
        Add(
            "line-duplicate-delimiter",
            "{ok,<<97,0>>,<<10>>}=erlang:decode_packet(line,<<97,0,10>>,[{line_delimiter,10},{line_delimiter,0}])"
        );
        Add("asn1-short", "{ok,<<4,2,97,98>>,<<9>>}=erlang:decode_packet(asn1,<<4,2,97,98,9>>,[])");
        Add("asn1-indefinite-header-only", "{ok,<<4,128>>,<<97,98,0,0>>}=erlang:decode_packet(asn1,<<4,128,97,98,0,0>>,[])");
        Add(
            "asn1-long-tag",
            "{ok,<<31,128,1,1,97>>,<<9>>}=erlang:decode_packet(asn1,<<31,128,1,1,97,9>>,[]),{more,undefined}=erlang:decode_packet(asn1,<<31,128,1>>,[])"
        );
        foreach (int width in new[] { 1, 2, 3, 4 })
        {
            string length = string.Join(",", Enumerable.Repeat("0", width - 1).Append("2"));
            Add(
                "asn1-long-length-" + width,
                "{ok,<<4," + (128 + width) + "," + length + ",97,98>>,<<9>>}=erlang:decode_packet(asn1,<<4," + (128 + width) + "," + length + ",97,98,9>>,[])"
            );
        }
        Add(
            "asn1-length-five",
            "{more,undefined}=erlang:decode_packet(asn1,<<4,133,0,0,0,0>>,[]),{error,invalid}=erlang:decode_packet(asn1,<<4,133,0,0,0,0,0>>,[])"
        );
        Add(
            "asn1-more",
            "{more,4}=erlang:decode_packet(asn1,<<4,2>>,[]),{more,undefined}=erlang:decode_packet(asn1,<<4,130,0>>,[])"
        );
        Add(
            "sunrm",
            "{ok,<<128,0,0,2,97,98>>,<<9>>}=erlang:decode_packet(sunrm,<<128,0,0,2,97,98,9>>,[]),{more,6}=erlang:decode_packet(sunrm,<<0,0,0,2>>,[])"
        );
        foreach (var (name, flags, length) in new[] { ("big", "0", "0,0,0,2"), ("little", "1", "2,0,0,0") })
            Add(
                "cdr-" + name,
                "B = <<\"GIOP\",1,2," + flags + ",0," + length + ",97,98>>, {ok,B,<<9>>}=erlang:decode_packet(cdr,<<B/binary,9>>,[])"
            );
        Add(
            "cdr-invalid",
            "{error,invalid}=erlang:decode_packet(cdr,<<\"FAIL\",1,2,0,0,0,0,0,2>>,[]),{more,undefined}=erlang:decode_packet(cdr,<<\"FAIL\",1>>,[])"
        );
        Add("fcgi-padding", "{ok,<<1,1,0,1,0,2,2,0,97,98>>,<<9>>}=erlang:decode_packet(fcgi,<<1,1,0,1,0,2,2,0,97,98,0,0,9>>,[])");
        Add(
            "fcgi-padding-counts-limit",
            "{error,invalid}=erlang:decode_packet(fcgi,<<1,1,0,1,0,2,2,0>>,[{packet_size,3}]),{more,12}=erlang:decode_packet(fcgi,<<1,1,0,1,0,2,2,0>>,[{packet_size,4}])"
        );
        Add("fcgi-invalid-version", "{error,invalid}=erlang:decode_packet(fcgi,<<2,1,0,1,0,0,0,0>>,[])");
        Add(
            "tpkt",
            "{ok,<<3,0,0,6,97,98>>,<<9>>}=erlang:decode_packet(tpkt,<<3,0,0,6,97,98,9>>,[]),{more,6}=erlang:decode_packet(tpkt,<<3,0,0,6>>,[])"
        );
        Add(
            "tpkt-invalid",
            "{error,invalid}=erlang:decode_packet(tpkt,<<2,0,0,6>>,[]),{error,invalid}=erlang:decode_packet(tpkt,<<3,0,0,3>>,[])"
        );
        Add("tls", "{ok,{ssl_tls,[],23,{3,3},<<97,98>>},<<9>>}=erlang:decode_packet(ssl_tls,<<23,3,3,0,2,97,98,9>>,[])");
        Add("tls-empty", "{ok,{ssl_tls,[],23,{3,3},<<>>},<<9>>}=erlang:decode_packet(ssl_tls,<<23,3,3,0,0,9>>,[])");
        Add(
            "tls-v2-hello",
            "{ok,{ssl_tls,[],22,{3,0},<<1,0,0,4,3,0,97,98>>},<<9>>}=erlang:decode_packet(ssl_tls,<<128,5,1,3,0,97,98,9>>,[])"
        );
        Add("tls-v2-empty", "{ok,{ssl_tls,[],22,{3,0},<<1,0,0,2,3,0>>},<<9>>}=erlang:decode_packet(ssl_tls,<<128,3,1,3,0,9>>,[])");
        Add(
            "tls-more",
            "{more,7}=erlang:decode_packet(ssl_tls,<<23,3,3,0,2>>,[]),{more,undefined}=erlang:decode_packet(ssl_tls,<<23,3,3,0>>,[])"
        );
        int index = 0;
        foreach (string selector in new[] { "3", "-1", "1.0", "unknown", "[]", "{}" })
            Add(
                "bad-selector-" + index++,
                "ok=try erlang:decode_packet(" + selector + ",<<>>,[]) of _->unexpected catch error:badarg->ok end"
            );
        index = 0;
        foreach (string input in new[] { "[]", "wrong", "1", "<<1:1>>" })
            Add("bad-input-" + index++, "ok=try erlang:decode_packet(0," + input + ",[]) of _->unexpected catch error:badarg->ok end");
        index = 0;
        foreach (string options in new[] { "wrong", "[wrong]", "[{unknown,1}]", "[{packet_size,-1}]", "[{packet_size,4294967296}]", "[{packet_size,1.0}]", "[{packet_size,1,2}]", "[{packet_size,1}|wrong]", "[{line_delimiter,10}]" })
            Add("bad-option-" + index++, "ok=try erlang:decode_packet(0,<<1>>," + options + ") of _->unexpected catch error:badarg->ok end");
        index = 0;
        foreach (string delimiter in new[] { "256", "-1", "1.0", "wrong" })
            Add(
                "bad-line-delimiter-" + index++,
                "ok=try erlang:decode_packet(line,<<1>>,[{line_delimiter," + delimiter + "}]) of _->unexpected catch error:badarg->ok end"
            );

        return cases;
    }
}
