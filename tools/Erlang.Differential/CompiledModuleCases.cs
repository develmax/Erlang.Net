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
        ),
        new(
            "bit-value-size-order",
            "-module(oracle_bit_order). -export([run/0]). run()->put(trace,[]),B = <<(mark(v1,1)):(mark(s1,8)),(mark(v2,2)):(mark(s2,8))>>,{B,get(trace)}. mark(K,V)->put(trace,[K|get(trace)]),V.",
            Term.Tuple(
                Term.A(OracleOutcomeTags.Success),
                Term.Tuple(new BitString([1,2]),Term.List(
                    Term.A("s2"),
                    Term.A("v2"),
                    Term.A("s1"),
                    Term.A("v1")
                ))
            )
        ),
        new(
            "bit-value-before-size-exception",
            "-module(oracle_bit_value_error). -export([run/0]). run()-><<(throw(value_first)):(throw(size_second))>>.",
            Term.Tuple(Term.A(OracleOutcomeTags.Failure),Term.A("throw"),Term.A("value_first"))
        ),
        new(
            "bit-size-before-next-value-exception",
            "-module(oracle_bit_size_error). -export([run/0]). run()-><<1:(throw(size_first)),(throw(next_value)):8>>.",
            Term.Tuple(Term.A(OracleOutcomeTags.Failure),Term.A("throw"),Term.A("size_first"))
        ),
        new(
            "bit-invalid-value-later-exception",
            "-module(oracle_bit_invalid_value). -export([run/0]). run()-><<(id(bad)):8,(throw(later)):8>>. id(X)->X.",
            Term.Tuple(Term.A(OracleOutcomeTags.Failure),Term.A("throw"),Term.A("later"))
        ),
        new(
            "bit-invalid-size-later-exception",
            "-module(oracle_bit_invalid_size). -export([run/0]). run()-><<1:(id(bad)),(throw(later)):8>>. id(X)->X.",
            Term.Tuple(Term.A(OracleOutcomeTags.Failure),Term.A("throw"),Term.A("later"))
        ),
        new(
            "bit-size-binding-exported-after-binary",
            "-module(oracle_bit_size_binding). -export([run/0]). run()->B = <<1:(S=4),2:4>>,{B,S}.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.Tuple(new BitString([18]),Term.I(4)))
        ),
        new(
            "closure-bit-captured-size",
            "-module(oracle_closure_bit_size). -export([run/0]). run()->N=4,F=fun(<<X:N,Y:N>>)->{X,Y}; (_)->miss end,{F(<<18>>),N}.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.Tuple(Term.Tuple(Term.I(1),Term.I(2)),Term.I(4)))
        ),
        new(
            "closure-bit-clause-binding-rollback",
            "-module(oracle_closure_bit_retry). -export([run/0]). run()->F=fun(<<N:4,X:N,0:1>>)->{first,N,X}; (<<N:4,X:N,_/bitstring>>)->{second,N,X} end,F(<<3:4,5:3,1:1>>).",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.Tuple(Term.A("second"),Term.I(3),Term.I(5)))
        ),
        new(
            "closure-guard-fallback-keeps-outer",
            "-module(oracle_closure_guard_retry). -export([run/0]). run()->X=42,F=fun({X}) when X>10->X; (_)->X end,{F({7}),F({12}),X}.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.Tuple(Term.I(42),Term.I(12),Term.I(42)))
        ),
        new(
            "closure-map-key-capture-value-shadow",
            "-module(oracle_closure_map_key). -export([run/0]). run()->K=a,V=42,F=fun(#{K:=V})->V; (_)->V end,{F(#{a=>7}),F(#{b=>8}),V}.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.Tuple(Term.I(7),Term.I(42),Term.I(42)))
        ),
        new(
            "closure-nested-body-local-capture",
            "-module(oracle_closure_nested). -export([run/0]). run()->X=42,F=fun()->Y=7,fun()->{X,Y} end end,G=F(),{G(),X}.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.Tuple(Term.Tuple(Term.I(42),Term.I(7)),Term.I(42)))
        ),
        new(
            "bit-invalid-utf-later-exception",
            "-module(oracle_bit_invalid_utf). -export([run/0]). run()-><<(id(55296))/utf8,(throw(later)):8>>. id(X)->X.",
            Term.Tuple(Term.A(OracleOutcomeTags.Failure),Term.A("throw"),Term.A("later"))
        ),
        new(
            "bit-independent-binding-exports",
            "-module(oracle_bit_exports). -export([run/0]). run()->B = <<(X=1),(Y=2)>>,{B,X,Y}.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.Tuple(new BitString([1,2]),Term.I(1),Term.I(2)))
        ),
        new(
            "bit-prebound-size-across-fields",
            "-module(oracle_bit_prebound). -export([run/0]). run()->S=4,B = <<1:S,2:S>>,{B,S}.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.Tuple(new BitString([18]),Term.I(4)))
        )
    ];
}
