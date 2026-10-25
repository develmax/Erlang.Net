namespace Erlang.Differential;

public static class CharacterLiteralDiagnosticExpectations
{
    public const string Code = "ERL001";
    public const string IllegalMessage = "Illegal Unicode character or escape in character literal";
    public const string UnterminatedMessage = "Unterminated character literal";
    public const string IllegalTag = "illegal";
    public const string CharacterTag = "character";
    public const string UnterminatedTag = "unterminated";
    public const string CharTag = "char";

    public static Term Outcome(bool unterminated) => Term.Tuple(
        Term.A(OracleOutcomeTags.Failure),
        Term.List(Term.Tuple(
            Term.A(unterminated ? UnterminatedTag : IllegalTag),
            Term.A(unterminated ? CharTag : CharacterTag)
        ))
    );
}
