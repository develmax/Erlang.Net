namespace Erlang.Differential;

public static class CompiledModuleCases
{
    public static IReadOnlyList<CompiledModuleCase> All { get; } = [
        new(
            "private-clauses",
            "-module(oracle_clauses). -export([run/0]). run()->pick(2). pick(0)->zero; pick(N) when is_integer(N)->N+1.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.I(3))
        ),
        new(
            "guard-fallback",
            "-module(oracle_guards). -export([run/0]). run()->pick(atom). pick(X) when hd(X)==1; is_atom(X)->yes; pick(_)->no.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.A("yes"))
        ),
        new(
            "closure-shadow",
            "-module(oracle_closure). -export([run/0]). run()->X=42,F=fun({X})->X; (_)->X end,{F({7}),F(atom)}.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.Tuple(Term.I(7),Term.I(42)))
        ),
        new(
            "tail-calls",
            "-module(oracle_tail). -export([run/0]). run()->loop(2000,0). loop(0,A)->A; loop(N,A)->loop(N-1,A+1).",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.I(2000))
        ),
        new(
            "map-exact-keys",
            "-module(oracle_map). -export([run/0]). run()->M=#{1=>integer,1.0=>float},#{1:=A,1.0:=B}=M,{A,B}.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.Tuple(Term.A("integer"),Term.A("float")))
        ),
        new(
            "bit-pattern",
            "-module(oracle_bits). -export([run/0]). run()-><<N,X:N,T/bitstring>> = <<3,5:3,2:2>>,{X,T}.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.Tuple(Term.I(5),new BitString([128],2)))
        ),
        new(
            "utf-pattern",
            "-module(oracle_utf). -export([run/0]). run()-><<X/utf16-little>> = <<128512/utf16-little>>,X.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.I(128512))
        ),
        new(
            "string-size-binding",
            "-module(oracle_string). -export([run/0]). run()->B = <<\"ab\":(S=16)/little>>,{B,S}.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.Tuple(new BitString([97,0,98,0]),Term.I(16)))
        ),
        new(
            "unicode-atom",
            "-module(oracle_unicode). -export([run/0]). run()->'𐀀'.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.A("𐀀"))
        ),
        new(
            "key-find",
            "-module(oracle_keys). -export([run/0]). run()->lists:keyfind(a,1,[{a,found}|tail]).",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.Tuple(Term.A("a"),Term.A("found")))
        ),
        new(
            "function-clause",
            "-module(oracle_function_error). -export([run/0]). run()->pick(atom). pick(0)->ok.",
            Term.Tuple(Term.A(OracleOutcomeTags.Failure),Term.A("error"),Term.A("function_clause"))
        ),
        new(
            "badmatch",
            "-module(oracle_match_error). -export([run/0]). run()->{a,X}={b,1},X.",
            Term.Tuple(Term.A(OracleOutcomeTags.Failure),Term.A("error"),Term.Tuple(Term.A("badmatch"),Term.Tuple(Term.A("b"),Term.I(1))))
        ),
        new(
            "throw",
            "-module(oracle_throw). -export([run/0]). run()->throw(reason).",
            Term.Tuple(Term.A(OracleOutcomeTags.Failure),Term.A("throw"),Term.A("reason"))
        ),
        new(
            "exit",
            "-module(oracle_exit). -export([run/0]). run()->exit(reason).",
            Term.Tuple(Term.A(OracleOutcomeTags.Failure),Term.A("exit"),Term.A("reason"))
        )
    ];
}
