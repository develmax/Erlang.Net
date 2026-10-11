using Erlang;

namespace Erlang.Differential;

public static class ErlangEtfCases
{
    public static string? FixtureName(string function, int arity) => (function, arity) switch
    {
        ("term_to_binary", 1) => "erlang-etf-vector-default-atom",
        ("term_to_binary", 2) => "erlang-etf-vector-minor-0-atom",
        ("term_to_iovec", 1) => "erlang-etf-iovec-default",
        ("term_to_iovec", 2) => "erlang-etf-iovec-compressed",
        ("binary_to_term", 1) => "erlang-etf-decode-vector-atom",
        ("binary_to_term", 2) => "erlang-etf-used-plain",
        ("external_size", 1) => "erlang-etf-size-default",
        ("external_size", 2) => "erlang-etf-size-compressed",
        _ => null
    };

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body, Term expected) => cases.Add(new(
            "erlang-etf-" + name,
            "-module(etf_contract). -export([run/0,maker/1]). run()->" + body + ". maker(Value)->fun()->Value end.",
            Term.Tuple(Term.A("ok"), expected)
        ));

        void Check(string name, string body) => Add(name, body, Term.A("true"));

        void Bad(string name, string body) => Add(name, "try " + body + " of _->unexpected catch error:Reason->Reason end", Term.A("badarg"));
        var vectors = new (string Name, string Source, byte[] Wire)[]
        {
            ("atom","hello",[131,119,5,104,101,108,108,111]),
            ("nil","[]",[131,106]),
            ("small","255",[131,97,255]),
            ("integer","256",[131,98,0,0,1,0]),
            ("negative","-1",[131,98,255,255,255,255]),
            ("big","4294967296",[131,110,5,0,0,0,0,0,1]),
            ("string","[0,255]",[131,107,0,2,0,255]),
            ("improper","[1|tail]",[131,108,0,0,0,1,97,1,119,4,116,97,105,108]),
            ("tuple","{1,[]}",[131,104,2,97,1,106]),
            ("binary","<<0,255>>",[131,109,0,0,0,2,0,255]),
            ("bits","<<5:3>>",[131,77,0,0,0,1,3,160]),
            ("map","#{1=>2}",[131,116,0,0,0,1,97,1,97,2]),
            ("unicode","'λ😀'",[131,119,6,206,187,240,159,152,128]),
            ("float","1.0",[131,70,63,240,0,0,0,0,0,0]),
            ("signed-zero","-0.0",[131,70,128,0,0,0,0,0,0,0])
        };
        foreach (var vector in vectors)
        {
            string wire = "<<" + string.Join(',', vector.Wire) + ">>";
            Add("vector-default-" + vector.Name, "erlang:term_to_binary(" + vector.Source + ")", new BitString(vector.Wire));
            Check("decode-vector-" + vector.Name, "erlang:binary_to_term(" + wire + ") =:= " + vector.Source);
            Check("vector-size-" + vector.Name, "erlang:external_size(" + vector.Source + ") >= byte_size(" + wire + ")");
        }
        foreach (int minor in new[] { 0, 1 })
        {
            Add(
                "vector-minor-" + minor + "-atom",
                "erlang:term_to_binary(hello,[{minor_version," + minor + "}])",
                new BitString(new byte[] { 131, 100, 0, 5, 104, 101, 108, 108, 111 })
            );
            Check("minor-unicode-" + minor, "B=erlang:term_to_binary('λ😀',[{minor_version," + minor + "}]),erlang:binary_to_term(B) =:= 'λ😀'");
            Check("minor-float-" + minor, "B=erlang:term_to_binary(1.2345,[{minor_version," + minor + "}]),erlang:binary_to_term(B) =:= 1.2345");
        }
        Check(
            "iovec-default",
            "V=erlang:term_to_iovec({<<1,2>>,hello}),is_list(V) andalso erlang:iolist_to_binary(V) =:= erlang:term_to_binary({<<1,2>>,hello})"
        );
        Check(
            "iovec-compressed",
            "V=erlang:term_to_iovec(lists:duplicate(2000,42),[compressed]),is_binary(V) andalso erlang:binary_to_term(V) =:= lists:duplicate(2000,42)"
        );
        Check(
            "iovec-compressed-fallback",
            "V=erlang:term_to_iovec(hello,[compressed]),is_binary(V) andalso V =:= erlang:term_to_binary(hello)"
        );
        Check(
            "iovec-large-binary",
            "B=erlang:list_to_binary(lists:duplicate(1000,42)),V=erlang:term_to_iovec({B,B},[deterministic]),is_list(V) andalso erlang:iolist_to_binary(V) =:= erlang:term_to_binary({B,B},[deterministic])"
        );
        Add("size-default", "erlang:external_size(hello)", Term.I(8));
        Check(
            "size-compressed",
            "V=lists:duplicate(2000,42),erlang:external_size(V,[compressed]) >= byte_size(erlang:term_to_binary(V,[compressed]))"
        );
        Add("used-plain", "erlang:binary_to_term(<<131,97,42,131,106>>,[used])", Term.Tuple(Term.I(42), Term.I(3)));
        Add("trailing-plain", "erlang:binary_to_term(<<131,97,42,255>>)", Term.I(42));
        Check(
            "used-compressed",
            "V=lists:duplicate(2000,42),B=erlang:term_to_binary(V,[compressed]),{V,N}=erlang:binary_to_term(<<B/binary,131,106>>,[used]),N =:= byte_size(B)"
        );
        Check("safe-existing", "erlang:binary_to_term(<<131,119,6,101,114,108,97,110,103>>,[safe,safe]) =:= erlang");
        Bad(
            "safe-new-atom",
            "erlang:binary_to_term(<<131,119,24,101,116,102,95,117,110,107,110,111,119,110,95,97,116,111,109,95,52,53,56,55,51,50,49>>,[safe])"
        );
        foreach (int level in Enumerable.Range(0, 10))
            Check(
                "compression-level-" + level,
                "V=lists:duplicate(1000,17),B=erlang:term_to_binary(V,[{compressed," + level + "}]),erlang:binary_to_term(B) =:= V"
            );
        Check(
            "last-option-wins",
            "erlang:term_to_binary(hello,[{minor_version,0},{minor_version,2},{compressed,9},{compressed,0}]) =:= <<131,119,5,104,101,108,108,111>>"
        );
        foreach (string function in new[] { "term_to_binary", "term_to_iovec", "external_size" })
            foreach (string option in new[] { "bad", "[bad]", "[compressed|bad]", "[{compressed,-1}]", "[{compressed,10}]", "[{compressed,1.0}]", "[{minor_version,-1}]", "[{minor_version,3}]", "[{minor_version,1.0}]", "[local,deterministic]", "[iovec]", "[{fragment,100}]" })
                Bad("bad-options-" + function + "-" + cases.Count, "erlang:" + function + "(42," + option + ")");
        foreach (string option in new[] { "bad", "[bad]", "[safe|bad]", "[compressed]", "[{used,true}]" })
            Bad("bad-decode-options-" + cases.Count, "erlang:binary_to_term(<<131,97,42>>," + option + ")");
        foreach (string value in new[] { "hello", "[]", "<<>>", "<<131>>", "<<132,97,42>>", "<<131,97>>", "<<131,109,0,0,0,2,1>>", "<<131,77,0,0,0,1,0,255>>", "<<131,77,0,0,0,1,9,255>>", "<<131,70,127,240,0,0,0,0,0,0>>", "<<131,119,2,255,255>>", "<<131,255>>", "<<131,116,0,0,0,2,97,1,97,2,97,1,97,3>>", "<<131,110,1,2,42>>", "<<131,97,42,0:1>>" })
        {
            if (value == "<<131,110,1,2,42>>")
                Add(
                    "malformed-" + cases.Count,
                    "try erlang:binary_to_term(" + value + ") of _->unexpected catch error:Reason->Reason end",
                    Term.A("unexpected")
                );
            else
                Bad("malformed-" + cases.Count, "erlang:binary_to_term(" + value + ")");
        }
        foreach (string value in new[] { "{self(),erlang:make_ref()}", "maker({42,hello})", "erlang:make_fun(erlang,length,1)", "#{1=>[2|tail],1.0=><<5:3>>}" })
            foreach (string option in new[] { "[]", "[compressed]", "[local]", "[local,compressed]", "[deterministic]" })
                Check("roundtrip-" + cases.Count, "V=" + value + ",B=erlang:term_to_binary(V," + option + "),C=erlang:binary_to_term(B),C =:= V");
        Check("closure-executes", "F=maker(42),G=erlang:binary_to_term(erlang:term_to_binary(F)),G() =:= 42");
        Check(
            "external-executes",
            "F=erlang:make_fun(erlang,length,1),G=erlang:binary_to_term(erlang:term_to_binary(F),[safe]),G([1,2,3]) =:= 3"
        );
        Check(
            "safe-created-external-stub",
            "F=erlang:make_fun(etf_missing_module,etf_missing_function,0),G=erlang:binary_to_term(erlang:term_to_binary(F),[safe]),G =:= F"
        );
        Check(
            "local-iovec",
            "V={self(),erlang:make_ref(),maker(42)},B=erlang:iolist_to_binary(erlang:term_to_iovec(V,[local])),erlang:binary_to_term(B) =:= V"
        );
        Check(
            "legacy-pid",
            "A=erlang:binary_to_term(<<131,103,119,1,110,0,0,0,1,0,0,0,2,3>>),B=erlang:binary_to_term(<<131,88,119,1,110,0,0,0,1,0,0,0,2,0,0,0,3>>),A =:= B"
        );
        Check(
            "legacy-port",
            "A=erlang:binary_to_term(<<131,102,119,1,110,0,0,0,42,3>>),B=erlang:binary_to_term(<<131,120,119,1,110,0,0,0,0,0,0,0,42,0,0,0,3>>),A =:= B"
        );
        Check(
            "legacy-reference",
            "A=erlang:binary_to_term(<<131,101,119,1,110,0,0,0,42,3>>),B=erlang:binary_to_term(<<131,90,0,3,119,1,110,0,0,0,3,0,0,0,42,0,0,0,0,0,0,0,0>>),A =:= B"
        );
        Check(
            "new-reference",
            "A=erlang:binary_to_term(<<131,114,0,1,119,1,110,3,0,0,0,42>>),B=erlang:binary_to_term(<<131,90,0,1,119,1,110,0,0,0,3,0,0,0,42>>),A =:= B"
        );
        Check(
            "five-word-reference",
            "A=erlang:binary_to_term(<<131,90,0,5,119,1,110,0,0,0,9,255,255,255,255,0,0,0,2,0,0,0,3,0,0,0,4,0,0,0,5>>),erlang:binary_to_term(erlang:term_to_binary(A)) =:= A"
        );
        Bad(
            "safe-uncreated-external",
            "erlang:binary_to_term(<<131,113,119,6,101,114,108,97,110,103,119,6,108,101,110,103,116,104,97,253>>,[safe])"
        );
        foreach (string value in new[] { "<<131,103,119,1,110,0,0,0,1,0,0,0,0,4>>", "<<131,102,119,1,110,0,0,0,1,4>>", "<<131,101,119,1,110,0,4,0,0,0>>", "<<131,90,0,0,119,1,110,0,0,0,0>>", "<<131,90,0,6,119,1,110,0,0,0,0,0:192>>", "<<131,80,0,0,0,1,0>>", "<<131,113,97,1,97,2,97,0>>", "<<131,113,119,1,110,119,1,110,98,255,255,255,255>>" })
            Bad("wire-identity-invalid-" + cases.Count, "erlang:binary_to_term(" + value + ")");
        foreach (var item in new[] { ("1.25", 1.25), ("1,25", 1.25), ("-1.25e+2", -125.0), ("1.25\0ignored", 1.25) })
        {
            byte[] text = new byte[31];
            System.Text.Encoding.ASCII.GetBytes(item.Item1).CopyTo(text, 0);
            Add("legacy-float-" + cases.Count, "erlang:binary_to_term(<<131,99," + string.Join(',', text) + ">>)", new FloatTerm(item.Item2));
        }
        foreach (string text in new[] { "1", "1e2", " 1.0", "1.0junk", "nan", "inf", "1.0e9999" })
        {
            byte[] bytes = new byte[31];
            System.Text.Encoding.ASCII.GetBytes(text).CopyTo(bytes, 0);
            Bad("legacy-float-invalid-" + cases.Count, "erlang:binary_to_term(<<131,99," + string.Join(',', bytes) + ">>)");
        }

        Bad("truncated-new-fun", "erlang:binary_to_term(<<131,112>>)");
        Check(
            "new-fun-size-ignored",
            "F=maker(42),<<131,112,_Size:32,Rest/binary>>=erlang:term_to_binary(F),G=erlang:binary_to_term(<<131,112,0:32,Rest/binary>>),G() =:= 42"
        );
        Check(
            "unloaded-new-fun",
            "<<131,P/binary>>=erlang:term_to_binary(self()),<<131,M/binary>>=erlang:term_to_binary(etf_unloaded),F=erlang:binary_to_term(<<131,112,0:32,0,0:128,17:32,1:32,M/binary,97,17,97,42,P/binary,97,99>>),{name,[]}=erlang:fun_info(F,name),{env,[99]}=erlang:fun_info(F,env),try F() of _->false catch error:undef->true end"
        );

        Check(
            "all-big-sign-bytes",
            "lists:all(fun(S)->V=case S of 0->42; _->-42 end,erlang:binary_to_term(<<131,110,1,S,42>>) =:= V andalso erlang:binary_to_term(<<131,111,1:32,S,42>>) =:= V end,lists:seq(0,255))"
        );
        Check(
            "noncanonical-big-sign-and-zero-padding",
            "erlang:binary_to_term(<<131,110,5,255,0,0,0,0,1>>) =:= -4294967296 andalso erlang:binary_to_term(<<131,110,0,255>>) =:= 0 andalso erlang:binary_to_term(<<131,111,4:32,2,0,0,0,0>>) =:= 0 andalso erlang:binary_to_term(<<131,110,4,2,42,0,0,0>>) =:= -42"
        );

        return cases.AsReadOnly();
    }
}
