using System.Text;

namespace Erlang.Differential;

public static class TermDisplayCases
{
    private const string Function = "display";
    private const string Prefix = "erlang-term-display-";

    public static string? FixtureName(string function, int arity) => function == Function && arity == 1 ? Prefix + "atom" : null;

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(
            string name,
            string value,
            string output,
            string? hex = null
        ) => cases.Add(new(
            Prefix + name,
            "-module(term_display_contract). -export([run/0]). run()->true=erlang:display(" + value + "),done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        )
        {
            ExpectedStandardOutputHex = hex ?? Convert.ToHexString(Encoding.UTF8.GetBytes(output + "\r\n")),
            ExpectedStandardErrorHex = ""
        });
        Add("atom", "ok", "ok");
        Add("empty-atom", "''", "''");
        Add("quoted-atom", "'Hello world'", "'Hello world'");
        Add("at-atom", "'a@b'", "'a@b'");
        Add("latin-atom", "'éÉ_9'", "éÉ_9");
        Add("unicode-atom", "'λ'", "'λ'");
        Add("atom-escape", "'a\\\'\\\\\\n\\f\\t\\r\\b\\v'", "'a\\'\\\\\\n\\f\\t\\r\\b\\v'");
        Add("atom-control", "list_to_atom([0,31,128,159])", "'\\000\\037\\200\\237'");
        Add("integer", "-42", "-42");
        Add("big-integer", "123456789012345678901234567890", "123456789012345678901234567890");
        Add("float-zero", "0.0", "0.000000e+00");
        Add("float-negative-zero", "-0.0", "-0.000000e+00");
        Add("float-round", "1.23456789", "1.234568e+00");
        Add("float-tiny", "1.0e-100", "1.000000e-100");
        Add("float-large", "1.0e100", "1.000000e+100");
        Add("nil", "[]", "[]");
        Add("string", "[65,66]", "\"AB\"");
        Add(
            "latin-string",
            "[160,255]",
            "",
            hex: "22A0FF220D0A"
        );
        Add(
            "string-controls",
            "[13,9,10]",
            "",
            hex: "220D095C6E220D0A"
        );
        Add("string-quote-slash", "[34,92]", "\"\\\"\\\"");
        Add("list-zero", "[0,127,128,159]", "[0,127,128,159]");
        Add("unicode-list", "[955,128512]", "[955,128512]");
        Add("improper-list", "[65|tail]", "[65|tail]");
        Add("nested-list", "[[65],[0]]", "[\"A\",[0]]");
        Add("tuple", "{ok,42,[]}", "{ok,42,[]}");
        Add("empty-tuple", "{}", "{}");
        Add("empty-map", "#{}", "#{}");
        Add("map", "#{b=>[65],a=>{0}}", "#{a=>{0},b=>\"A\"}");
        cases[^1] = cases[^1] with
        {
            ExpectedStandardOutputAlternatives = Array.AsReadOnly(new[] { Convert.ToHexString(Encoding.UTF8.GetBytes("#{b=>\"A\",a=>{0}}\r\n")) })
        };
        Add("numeric-map", "#{1=>integer,1.0=>floating}", "#{1=>integer,1.000000e+00=>floating}");
        Add("empty-binary", "<<>>", "<<>>");
        Add("binary-string", "<<65,66>>", "<<\"AB\">>");
        Add("binary-quote-slash", "<<34,92>>", "<<\"\\\"\\\">>");
        Add("binary-controls", "<<0,10,255>>", "<<0,10,255>>");
        Add("bits", "<<5:3>>", "<<5:3>>");
        Add("bytes-bits", "<<65,3:2>>", "<<65,3:2>>");
        Add("external-fun", "fun erlang:length/1", "fun erlang:length/1");
        Add("external-quoted-fun", "fun 'a@b':'f x'/2", "fun 'a@b':'f x'/2");

        return cases;
    }
}
