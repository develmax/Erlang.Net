namespace Erlang.Differential;

public static class CharacterLiteralCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "character-literal-" + name,
            "-module(character_literal_contract). -export([run/0]). run()->" + body + ",done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));

        foreach (var (name, expression, expected) in new (string, string, int)[]
        {
            ("digit", "$0", 48),
            ("letter", "$x", 120),
            ("dollar", "$$", 36),
            ("space", "$ ", 32),
            ("comment-marker", "$%", 37),
            ("double-quote", "$\"", 34),
            ("single-quote", "$'", 39),
            ("raw-newline", "$\n", 10),
            ("bmp", "$Ж", 1046),
            ("supplementary", "$😀", 128512),
            ("newline", """$\n""", 10),
            ("carriage-return", """$\r""", 13),
            ("tab", """$\t""", 9),
            ("vertical-tab", """$\v""", 11),
            ("backspace", """$\b""", 8),
            ("form-feed", """$\f""", 12),
            ("escape", """$\e""", 27),
            ("escaped-space", """$\s""", 32),
            ("delete", """$\d""", 127),
            ("backslash", """$\\""", 92),
            ("identity-escape", """$\Ж""", 1046),
            ("non-octal-eight", """$\8""", 56),
            ("octal-one", """$\7""", 7),
            ("octal-two", """$\77""", 63),
            ("octal-three", """$\777""", 511),
            ("hex-fixed", """$\xAf""", 175),
            ("hex-braced", """$\x{1f600}""", 128512),
            ("hex-leading-zeroes", """$\x{0000000000000000000041}""", 65),
            ("maximum-unicode", """$\x{10ffff}""", 1114111),
            ("plane-noncharacter", """$\x{1fffe}""", 131070),
            ("control-null", """$\^@""", 0),
            ("control-uppercase", """$\^A""", 1),
            ("control-lowercase", """$\^z""", 26),
            ("control-underscore", """$\^_""", 31),
            ("control-delete", """$\^?""", 127)
        })
            Add(name, expected + "=" + expression);

        Add("integer-identity", "true=($a=:=97),true=is_integer($a),[97,98]=[$a,$b]");
        Add("pattern", "F=fun($a)->yes;(_)->no end,yes=F(97),no=F(98)");
        Add("guard", "F=fun(X) when X=:=$a->yes;(_)->no end,yes=F(97),no=F(98)");
        Add("binary", "<<97,98>> = <<$a,$b>>");
        Add("comprehension", "[98,99]=[X+1||X<-[$a,$b],X>=$a]");
        Add("adjacent-operators", "98=$a+1,96=$a-1");

        return cases;
    }
}
