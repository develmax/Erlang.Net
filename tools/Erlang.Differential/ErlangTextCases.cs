using System.Globalization;

namespace Erlang.Differential;

public static class ErlangTextCases
{
    private static readonly IReadOnlyList<(string Function, int Arity, string Arguments, string Expected)> exports =
    [
        ("atom_to_list", 1, "[hello]", "\"hello\""),
        ("list_to_atom", 1, "[\"hello\"]", "hello"),
        ("list_to_existing_atom", 1, "[\"true\"]", "true"),
        ("atom_to_binary", 1, "[hello]", "<<\"hello\">>"),
        ("atom_to_binary", 2, "[hello,latin1]", "<<\"hello\">>"),
        ("binary_to_atom", 1, "[<<\"hello\">>]", "hello"),
        ("binary_to_atom", 2, "[<<\"hello\">>,unicode]", "hello"),
        ("binary_to_existing_atom", 1, "[<<\"true\">>]", "true"),
        ("binary_to_existing_atom", 2, "[<<\"true\">>,utf8]", "true"),
        ("float_to_list", 1, "[1.5]", "\"1.50000000000000000000e+00\""),
        ("float_to_list", 2, "[1.5,[{decimals,2}]]", "\"1.50\""),
        ("float_to_binary", 1, "[1.5]", "<<\"1.50000000000000000000e+00\">>"),
        ("float_to_binary", 2, "[1.5,[short]]", "<<\"1.5\">>")
    ];

    public static string? FixtureName(string function, int arity) => exports.Any(export => export.Function == function && export.Arity == arity) ? "erlang-text-export-" + function + "-" + arity : null;

    private static CompiledModuleCase Case(string name, string body) => new(
        "erlang-text-" + name,
        "-module(oracle_erlang_text). -export([run/0]). run()->" + body + ",done.",
        Term.Tuple(Term.A("ok"), Term.A("done"))
    );

    private static string Error(string call, string reason = "badarg") => "try erlang:" + call + " of _->unexpected catch error:" + reason + "->ok end";

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = exports.Select(export => Case("export-" + export.Function + "-" + export.Arity, export.Expected + "=apply(erlang,'" + export.Function + "'," + export.Arguments + ")")).ToList();
        cases.Add(Case(
            "primordial-existing",
            "normal=list_to_existing_atom(\"normal\"),noproc=binary_to_existing_atom(<<\"noproc\">>),''=list_to_existing_atom([]),atom_to_binary=list_to_existing_atom(\"atom_to_binary\")"
        ));
        cases.Add(Case(
            "primordial-case-sensitive",
            "<<\"EXIT\">>=atom_to_binary(binary_to_existing_atom(<<\"EXIT\">>)),<<\"exit\">>=atom_to_binary(binary_to_existing_atom(<<\"exit\">>)),<<\"DOWN\">>=atom_to_binary(binary_to_existing_atom(<<\"DOWN\">>))"
        ));
        cases.Add(Case(
            "created-existing",
            "L=[99,114,101,97,116,101,100,95,49,49,53,95,97],A=list_to_atom(L),L=atom_to_list(A),A=list_to_existing_atom(L),A=binary_to_existing_atom(list_to_binary(L)),A=binary_to_atom(list_to_binary(L))"
        ));
        cases.Add(Case("private-loaded-name", "unused_private_115=list_to_existing_atom(\"unused_private_115\")") with { Source = "-module(oracle_erlang_text). -export([run/0]). run()->unused_private_115=list_to_existing_atom(\"unused_private_115\"),done. unused_private_115()->unused_body_115." });
        cases.Add(Case(
            "literal-loaded-name",
            "literal_not_executed_115=list_to_existing_atom(\"literal_not_executed_115\"),F=fun()->literal_not_executed_115 end,true=is_function(F)"
        ));
        cases.Add(Case(
            "variable-scanner-name",
            "TemporaryVariableName115=ok,'TemporaryVariableName115'=list_to_existing_atom(\"TemporaryVariableName115\"),ok=TemporaryVariableName115"
        ));
        cases.Add(Case(
            "unicode-scalars",
            "L=[0,255,256,65535,65536,1114111],A=list_to_atom(L),L=atom_to_list(A),B=atom_to_binary(A),A=binary_to_atom(B),A=binary_to_existing_atom(B,unicode),A=list_to_existing_atom(L)"
        ));
        cases.Add(Case(
            "latin1",
            "A=list_to_atom([0,127,128,255]),<<0,127,128,255>>=atom_to_binary(A,latin1),A=binary_to_atom(<<0,127,128,255>>,latin1),<<0,127,194,128,195,191>>=atom_to_binary(A,utf8)"
        ));
        cases.Add(Case(
            "nul",
            "A=list_to_atom([97,0,98]),[97,0,98]=atom_to_list(A),<<97,0,98>>=atom_to_binary(A),A=binary_to_atom(<<97,0,98>>),A=list_to_existing_atom([97,0,98])"
        ));
        foreach (string size in new[] { "254", "255" })
            cases.Add(Case(
                "atom-length-" + size,
                "L=lists:duplicate(" + size + ",1114111),A=list_to_atom(L),L=atom_to_list(A),A=binary_to_atom(atom_to_binary(A)),A=list_to_existing_atom(L)"
            ));
        cases.Add(Case(
            "utf8-length-before-next-invalid",
            "B=list_to_binary(lists:duplicate(255,97)++[255])," + Error("binary_to_atom(B)", "system_limit") + "," + Error("binary_to_existing_atom(B)")
        ));
        cases.Add(Case(
            "list-length-before-next-invalid",
            "L=lists:duplicate(255,97)++[bad]," + Error("list_to_atom(L)", "system_limit") + "," + Error("list_to_existing_atom(L)")
        ));
        cases.Add(Case(
            "list-improper-before-length",
            "L=lists:duplicate(255,97)++bad," + Error("list_to_atom(L)") + "," + Error("list_to_existing_atom(L)")
        ));
        foreach (string encoding in new[] { "utf8", "unicode", "latin1" })
        {
            cases.Add(Case(
                "binary-too-long-" + encoding,
                "B=list_to_binary(lists:duplicate(" + (encoding == "latin1" ? "256" : "1021") + ",255))," + Error("binary_to_atom(B," + encoding + ")", "system_limit") + "," + Error("binary_to_existing_atom(B," + encoding + ")")
            ));
        }
        foreach (string input in new[] { "[]", "[256]", "[55296]", "[57343]", "[-1]", "[1114112]", "[1.0]", "[[97]]", "[97|bad]", "bad", "<<97>>" })
        {
            if (input == "[]" || input == "[256]")
                continue;
            cases.Add(Case("list-bad-" + cases.Count, Error("list_to_atom(" + input + ")") + "," + Error("list_to_existing_atom(" + input + ")")));
        }
        foreach (string bytes in new[] { "255", "192,128", "193,191", "194", "224,128,128", "237,160,128", "240,128,128,128", "244,144,128,128", "245,128,128,128", "128", "226,130" })
            cases.Add(Case("invalid-utf8-" + cases.Count, Error("binary_to_atom(<<" + bytes + ">>)") + "," + Error("binary_to_existing_atom(<<" + bytes + ">>)")));
        cases.Add(Case(
            "unknown-existing",
            Error("list_to_existing_atom([122,122,95,117,110,107,110,111,119,110,95,49,49,53,95,110,101,103])") + "," + Error("binary_to_existing_atom(<<122,122,95,117,110,107,110,111,119,110,95,49,49,53,95,110,101,103>>)")
        ));
        foreach (string call in new[] { "atom_to_list(123)", "atom_to_binary(123)", "atom_to_binary(ok,bad)", "atom_to_binary(list_to_atom([256]),latin1)", "binary_to_atom(<<1:1>>)", "binary_to_atom([97])", "binary_to_atom(<<97>>,bad)", "binary_to_existing_atom(<<1:1>>)", "binary_to_existing_atom(<<97>>,123)" })
            cases.Add(Case("atom-errors-" + cases.Count, Error(call)));
        foreach ((string value, string result) in new[] { ("0.0", "0.0"), ("-0.0", "-0.0"), ("1.0", "1.0"), ("-1.5", "-1.5"), ("0.0001", "0.0001"), ("0.00001", "1.0e-5"), ("100.0", "100.0"), ("1000.0", "1.0e3"), ("1.23e10", "1.23e10"), ("1.234e-3", "0.001234"), ("9007199254740992.0", "9.007199254740992e15"), ("5.0e-324", "5.0e-324"), ("1.7976931348623157e308", "1.7976931348623157e308") })
            cases.Add(Case(
                "short-" + cases.Count,
                "\"" + result + "\"=float_to_list(" + value + ",[short]),<<\"" + result + "\">>=float_to_binary(" + value + ",[short])"
            ));
        foreach ((string value, int digits, string result) in new[] { ("2.5", 0, "3"), ("-2.5", 0, "-3"), ("1.25", 1, "1.3"), ("-1.25", 1, "-1.3"), ("1.125", 2, "1.13"), ("-0.0", 2, "-0.00"), ("9.999", 2, "10.00"), ("1.0", 19, "1.0000000000000000000") })
            cases.Add(Case("fixed-round-" + cases.Count, "\"" + result + "\"=float_to_list(" + value + ",[{decimals," + digits + "}])"));
        cases.Add(Case(
            "scientific-half-even",
            "\"2e+00\"=float_to_list(2.5,[{scientific,0}]),\"4e+00\"=float_to_list(3.5,[{scientific,0}]),\"1.2e+00\"=float_to_list(1.25,[{scientific,1}]),\"1.0e+01\"=float_to_list(9.99,[{scientific,1}])"
        ));
        cases.Add(Case(
            "compact",
            "\"1.0\"=float_to_list(1.0,[{decimals,5},compact]),\"1.25\"=float_to_list(1.25,[compact,{decimals,5}]),\"-0.0\"=float_to_list(-0.0,[{decimals,5},compact]),\"1\"=float_to_list(1.0,[{decimals,0},compact])"
        ));
        cases.Add(Case("compact-fallback-zero-decimals", "\"1\"=float_to_list(1.0e20,[{decimals,0},compact])"));
        cases.Add(Case(
            "option-precedence",
            "\"1.25\"=float_to_list(1.25,[{decimals,-1},short]),\"1.25\"=float_to_list(1.25,[{scientific,-1},short]),\"1.3\"=float_to_list(1.25,[short,{decimals,1}]),\"1.2e+00\"=float_to_list(1.25,[{decimals,1},{scientific,1}])"
        ));
        cases.Add(Case(
            "precision-buffer",
            "255=length(float_to_list(0.0,[{decimals,253}])),255=length(float_to_list(0.0,[{scientific,249}]))," + Error("float_to_list(-0.0,[{decimals,253}])") + "," + Error("float_to_list(-0.0,[{scientific,249}])")
        ));
        foreach (string options in new[] { "bad", "[bad]", "[short|bad]", "[{decimals,-1}]", "[{scientific,-1}]", "[{decimals,254}]", "[{scientific,250}]", "[{decimals,1.0}]", "[{decimals,2,3}]", "[{decimals,1152921504606846976},short]", "[{scientific,1152921504606846976},short]", "[123]" })
            cases.Add(Case("float-options-bad-" + cases.Count, Error("float_to_list(1.0," + options + ")") + "," + Error("float_to_binary(1.0," + options + ")")));
        foreach (string input in new[] { "1", "ok", "[]", "<<>>" })
            cases.Add(Case("float-type-bad-" + cases.Count, Error("float_to_list(" + input + ")") + "," + Error("float_to_binary(" + input + ",[short])")));

        foreach ((string value, string result) in new[] { ("1.234e-4", "1.234e-4"), ("12.0", "12.0"), ("120.0", "120.0"), ("1230.0", "1230.0"), ("12300.0", "1.23e4"), ("0.0012", "0.0012"), ("0.00012", "1.2e-4"), ("1.2e10", "1.2e10"), ("1.23e9", "1.23e9") })
            cases.Add(Case("short-length-choice-" + cases.Count, "\"" + result + "\"=float_to_list(" + value + ",[short])"));
        long bits = 123456789;
        for (int index = 0; index < 64; index++)
        {
            bits = unchecked(bits * 6364136223846793005L + 1442695040888963407L);
            double number = BitConverter.Int64BitsToDouble(bits);
            if (!double.IsFinite(number) || number == 0)
                continue;
            string literal = number.ToString("R", CultureInfo.InvariantCulture);
            int exponent = literal.IndexOf('E');
            if (!literal.Contains('.'))
                literal = exponent < 0 ? literal + ".0" : literal.Insert(exponent, ".0");
            cases.Add(Case(
                "short-roundtrip-" + index,
                "F=" + literal + ",F=binary_to_float(float_to_binary(F,[short])),F=list_to_float(float_to_list(F,[short]))"
            ));
        }

        return cases;
    }
}
