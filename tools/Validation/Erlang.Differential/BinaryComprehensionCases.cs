namespace Erlang.Differential;

public static class BinaryComprehensionCases
{
    private static Term Success(Term value) => Term.Tuple(Term.A(OracleOutcomeTags.Success), value);

    private static Term Failure(string reason, Term value) => Term.Tuple(Term.A(OracleOutcomeTags.Failure), Term.A("error"), Term.Tuple(Term.A(reason), value));

    private static BitString Bytes(params byte[] values) => new(values);

    private static Term Numbers(params long[] values) => Cons.From(values.Select(Term.I));

    public static IReadOnlyList<OperatorExpressionCase> All { get; } = [
        new("bc-list-source", "<< <<(X*2)>> || X <- [1,2,3] >>", Success(Bytes(2,4,6))),
        new("bc-filter", "<< <<X>> || X <- [1,2,3,4],X rem 2=:=0 >>", Success(Bytes(2,4))),
        new("bc-empty", "<< <<X>> || X <- [] >>", Success(Bytes())),
        new("bc-no-generator", "<< <<7>> || true >>", Success(Bytes(7))),
        new("bc-variable-template", "begin B= <<7,8>>,<< B || X <- [1,2] >> end", Success(Bytes(
            7,
            8,
            7,
            8
        ))),
        new("bc-nonbit-template", "<< X || X <- [1] >>", Term.Tuple(Term.A("error"),Term.A("error"),Term.A("badarg"))),
        new("bc-unaligned-output", "<< <<X:3>> || X <- [1,2,3] >>", Success(new BitString(new byte[]{41,128},9))),
        new("bc-empty-template", "<< <<>> || X <- [1,2] >>", Success(Bytes())),
        new("bc-template-segments", "<< <<X,X:4>> || X <- [1,2] >>", Success(Bytes(1,16,34))),
        new("bc-nested", "<< << <<Y>> || Y <- [X,X+1] >> || X <- [1,2] >>", Success(Bytes(
            1,
            2,
            2,
            3
        ))),
        new("bc-ordinary-filter", "<< <<X>> || X <- [1,2],(fun(N)->N>1 end)(X) >>", Success(Bytes(2))),
        new("bc-guard-errors", "<< <<X>> || X <- [1,atom,2],X+1>1 >>", Success(Bytes(1,2))),
        new(
            "bc-template-before-next-error",
            "begin put(mark,none),R=catch << <<(begin put(mark,X),X end)>> || X <- [1|tail] >>,{'EXIT',{Reason,_}}=R,{Reason,get(mark)} end",
            Success(Term.Tuple(Term.Tuple(Term.A("bad_generator"),Term.A("tail")),Term.I(1)))
        ),
        new("bg-byte-list", "[X || <<X>> <= <<1,2,3>>]", Success(Numbers(1,2,3))),
        new("bg-byte-binary", "<< <<(X+1)>> || <<X>> <= <<1,2,3>> >>", Success(Bytes(2,3,4))),
        new("bg-empty", "[X || <<X>> <= <<>>]", Success(Numbers())),
        new("bg-strict-empty", "[X || <<X>> <:= <<>>]", Success(Numbers())),
        new("bg-relaxed-literal", "[X || <<1,X>> <= <<0,9,1,7,1,8>>]", Success(Numbers(7,8))),
        new("bg-strict-literal", "[X || <<1,X>> <:= <<1,7,0,9,1,8>>]", Failure("badmatch",Bytes(
            0,
            9,
            1,
            8
        ))),
        new("bg-relaxed-tail", "[X || <<X:16>> <= <<0,1,2>>]", Success(Numbers(1))),
        new("bg-strict-tail", "[X || <<X:16>> <:= <<0,1,2>>]", Failure("badmatch",Bytes(2))),
        new("bg-unaligned", "[X || <<X:3>> <= <<1:3,2:3,3:3,1:1>>]", Success(Numbers(1,2,3))),
        new("bg-strict-unaligned-tail", "[X || <<X:3>> <:= <<1:3,2:3,1:1>>]", Failure("badmatch",new BitString(new byte[]{128},1))),
        new("bg-bad-source", "[X || <<X>> <= atom]", Failure("bad_generator",Term.A("atom"))),
        new("bg-repeated", "[X || <<X,X>> <= <<1,1,1,2,3,3>>]", Success(Numbers(1,3))),
        new("bg-incoming-shadow", "begin X=9,R=[X || <<X>> <= <<1,2>>],{X,R} end", Success(Term.Tuple(Term.I(9),Numbers(1,2)))),
        new("bg-incoming-size", "begin S=4,[X || <<X:S>> <= <<1:4,2:4,3:4>>] end", Success(Numbers(1,2,3))),
        new("bg-sequential-size", "[X || <<S:8,X:S>> <= <<4,3:4,4,5:4>>]", Success(Numbers(3,5))),
        new(
            "bg-dependent-source",
            "[{X,Y} || X <- [1,2],<<Y>> <= <<X,(X+10)>>]",
            Success(Term.List(
                Term.Tuple(Term.I(1),Term.I(1)),
                Term.Tuple(Term.I(1),Term.I(11)),
                Term.Tuple(Term.I(2),Term.I(2)),
                Term.Tuple(Term.I(2),Term.I(12))
            ))
        ),
        new("bg-source-private", "[X || <<X>> <= begin B= <<1,2>>,B end]", Success(Numbers(1,2))),
        new("bg-utf8", "[X || <<X/utf8>> <= <<65,8364/utf8,128512/utf8>>]", Success(Numbers(65,8364,128512))),
        new("bg-utf-invalid-tail", "[X || <<X/utf8>> <= <<65,255,66>>]", Success(Numbers(65))),
        new("bg-strict-utf-invalid-tail", "[X || <<X/utf8>> <:= <<65,255,66>>]", Failure("badmatch",Bytes(255,66))),
        new("bg-float-invalid-skips", "[X || <<X:32/float>> <= <<2143289344:32,1.0:32/float>>]", Success(Term.List(new FloatTerm(1)))),
        new("bg-binary-segments", "[X || <<X:1/binary>> <= <<1,2>>]", Success(Term.List(Bytes(1),Bytes(2)))),
        new("bg-little-signed", "[X || <<X:16/little-signed>> <= <<255,255,0,1>>]", Success(Numbers(-1,256)))
    ];

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = All.Select((fixture, index) => new CompiledModuleCase(
        "compiled-" + fixture.Name,
        "-module(oracle_bc_" + index + "). -export([run/0]). run()->" + fixture.Source + ".",
        fixture.Expected
    )).ToArray();
}
