namespace Erlang.Differential;

public static class StandardStreamCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    public static string? FixtureName(string function, int arity) => function == ErlangStandardStreamNames.DisplayString ? arity switch
    {
        1 => "erlang-standard-stream-default-empty",
        2 => "erlang-standard-stream-stderr-empty",
        _ => null
    } : null;

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(
            string name,
            string body,
            string output = "",
            string error = ""
        ) => cases.Add(new(
            "erlang-standard-stream-" + name,
            "-module(standard_stream_contract). -export([run/0]). run()->" + body + ",done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        )
        {
            ExpectedStandardOutputHex = output,
            ExpectedStandardErrorHex = error
        });

        Add("default-empty", "true=erlang:display_string([])");
        Add("stderr-empty", "true=erlang:display_string(stderr,<<>>)");
        Add("stdout-empty", "true=erlang:display_string(stdout,[])");
        Add("default-ascii", "true=erlang:display_string([65,66])", error: "4142");
        Add(
            "default-unicode",
            "true=erlang:display_string([127,128,2047,2048,65535,65536,1114111])",
            error: "7FC280DFBFE0A080EFBFBFF0908080F48FBFBF"
        );
        Add("default-binary", "true=erlang:display_string(<<65,255,195,40>>)", error: "41FFC328");
        Add("stdout-ascii", "true=erlang:display_string(stdout,<<65,66>>)", output: "4142");
        Add("stderr-unicode", "true=erlang:display_string(stderr,[128512])", error: "F09F9880");
        Add("stdout-carriage-return", "true=erlang:display_string(stdout,<<13>>)", output: "0D");
        Add("stdout-line-feed", "true=erlang:display_string(stdout,<<10>>)", output: "0A");
        foreach (string value in new[] { "atom", "0", "1.0", "{}", "#{}", "<<1:1>>", "[-1]", "[55296]", "[57343]", "[1114112]", "[999999999999999999999999]", "[1.0]", "[[65]]", "[65|tail]", "[<<65>>]" })
            Add("bad-value-" + cases.Count, "try erlang:display_string(" + value + ") of _->error(accepted) catch error:badarg->ok end");
        foreach (string device in new[] { "wrong", "[]", "42", "<<\"stdout\">>" })
            Add("bad-device-" + cases.Count, "try erlang:display_string(" + device + ",[]) of _->error(accepted) catch error:badarg->ok end");
        Add(
            "default-error-frame",
            "try erlang:display_string([55296]) of _->error(accepted) catch error:badarg:S->[{erlang,display_string,[[55296]],L}|_]=S,{error_info,#{module:=erl_erts_errors}}=lists:keyfind(error_info,1,L) end"
        );
        Add(
            "device-error-frame",
            "try erlang:display_string(wrong,[]) of _->error(accepted) catch error:badarg:S->[{erlang,display_string,[wrong,[]],L}|_]=S,{error_info,#{module:=erl_erts_errors}}=lists:keyfind(error_info,1,L) end"
        );

        return cases;
    }
}
