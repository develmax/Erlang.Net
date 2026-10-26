namespace Erlang.Differential;

public static class ErlangMd5Cases
{
    private static CompiledModuleCase Case(string name, string body) => new(
        "erlang-md5-" + name,
        "-module(oracle_erlang_md5). -export([run/0]). run()->" + body + ",done.",
        Term.Tuple(Term.A("ok"), Term.A("done"))
    );

    private static string Error(string call) => "try erlang:" + call + " of _->unexpected catch error:badarg->ok end";

    public static string? FixtureName(string function, int arity) => (function, arity) switch
    {
        ("md5_init", 0) => "erlang-md5-export-md5_init-0",
        ("md5_update", 2) => "erlang-md5-export-md5_update-2",
        ("md5_final", 1) => "erlang-md5-export-md5_final-1",
        _ => null
    };

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();
        cases.Add(Case(
            "context-transfer",
            "P=self(),C=erlang:md5_update(erlang:md5_init(),\"abc\"),spawn(fun()->P!{digest,erlang:md5_final(C)} end),receive {digest,H}->H=erlang:md5(\"abc\") after 1000->error(missing) end,H0=erlang:md5_final(C),H0=erlang:md5(\"abc\")"
        ));
        cases.Add(Case("native64-initial-context", "<<1,35,69,103,137,171,205,239,254,220,186,152,118,84,50,16,0:640>>=erlang:md5_init()"));
        cases.Add(Case(
            "native64-short-context",
            "C=erlang:md5_update(erlang:md5_init(),<<97,98,99>>),<<1,35,69,103,137,171,205,239,254,220,186,152,118,84,50,16,0:64/little,3:64/little,97,98,99,0:488>>=C"
        ));
        cases.Add(Case("export-md5_init-0", "C=erlang:md5_init(),true=is_binary(C),H=erlang:md5_final(C),H=erlang:md5([])"));
        cases.Add(Case(
            "export-md5_update-2",
            "C=erlang:md5_init(),D=erlang:md5_update(C,\"abc\"),H=erlang:md5_final(D),H=erlang:md5(\"abc\"),E=erlang:md5_final(C),E=erlang:md5([])"
        ));
        cases.Add(Case(
            "export-md5_final-1",
            "C=erlang:md5_update(erlang:md5_init(),<<97,98,99>>),<<144,1,80,152,60,210,79,176,214,150,63,125,40,225,127,114>>=erlang:md5_final(C),<<144,1,80,152,60,210,79,176,214,150,63,125,40,225,127,114>>=erlang:md5_final(C)"
        ));
        foreach (string size in new[] { "0", "1", "55", "56", "57", "63", "64", "65", "127", "128", "129", "511", "512", "513", "4096", "100000" })
            cases.Add(Case(
                "block-boundary-" + size,
                "L=lists:duplicate(" + size + ",97),C=erlang:md5_update(erlang:md5_init(),L),H=erlang:md5_final(C),H=erlang:md5(L),B=erlang:list_to_binary(L),D=erlang:md5_update(erlang:md5_init(),B),H=erlang:md5_final(D)"
            ));
        foreach (string split in new[] { "1", "55", "56", "63", "64", "65", "127", "128" })
            cases.Add(Case(
                "split-boundary-" + split,
                "L=lists:duplicate(256,98),{A,B}=lists:split(" + split + ",L),C=erlang:md5_update(erlang:md5_update(erlang:md5_init(),A),B),H=erlang:md5_final(C),H=erlang:md5(L)"
            ));
        cases.Add(Case(
            "branch-context",
            "C=erlang:md5_update(erlang:md5_init(),\"prefix\"),A=erlang:md5_update(C,\"a\"),B=erlang:md5_update(C,\"b\"),HA=erlang:md5_final(A),HA=erlang:md5(\"prefixa\"),HB=erlang:md5_final(B),HB=erlang:md5(\"prefixb\"),HC=erlang:md5_final(C),HC=erlang:md5(\"prefix\"),true=HA=/=HB"
        ));
        cases.Add(Case(
            "nested-iodata",
            "L=[[],1,[2,<<3,4>>],<<5>>,[]|<<6,7>>],C=erlang:md5_update(erlang:md5_init(),L),H=erlang:md5_final(C),H=erlang:md5(<<1,2,3,4,5,6,7>>)"
        ));
        cases.Add(Case(
            "unaligned-binary",
            "<<_:1,B:3/binary,_:7>> = <<0:1,97,98,99,0:7>>,C=erlang:md5_update(erlang:md5_init(),B),H=erlang:md5_final(C),H=erlang:md5(\"abc\")"
        ));
        cases.Add(Case(
            "empty-update-preserves",
            "C=erlang:md5_update(erlang:md5_init(),<<1,2,3>>),C=erlang:md5_update(C,[]),C=erlang:md5_update(C,<<>>),C=erlang:md5_update(C,[[],<<>>])"
        ));
        cases.Add(Case(
            "sixty-four-pending",
            "A=erlang:md5_update(erlang:md5_init(),<<97>>),B=erlang:md5_update(A,lists:duplicate(63,97)),H=erlang:md5_final(B),H=erlang:md5(lists:duplicate(64,97)),C=erlang:md5_update(B,[]),H=erlang:md5_final(C),D=erlang:md5_update(C,<<98>>),HD=erlang:md5_final(D),HD=erlang:md5([lists:duplicate(64,97),98])"
        ));
        foreach (string input in new[] { "bad", "[]", "1", "1.0", "<<>>", "<<1:1>>", "<<0:8>>", "<<0:760>>", "<<0:776>>" })
            cases.Add(Case("context-bad-" + cases.Count, Error("md5_final(" + input + ")") + "," + Error("md5_update(" + input + ",[])")));
        foreach (string input in new[] { "bad", "1", "[256]", "[-1]", "[1|2]", "[1|bad]", "[<<1:1>>]", "[1.0]", "[{a}]", "<<1:1>>" })
            cases.Add(Case("iodata-bad-" + cases.Count, Error("md5_update(erlang:md5_init()," + input + ")")));

        return cases;
    }
}
