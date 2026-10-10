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
        ),
        new(
            "if-common-binding-first",
            "-module(oracle_if_first). -export([run/0]). run()->if true -> X=1; true -> X=2 end,X.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.I(1))
        ),
        new(
            "if-common-binding-second",
            "-module(oracle_if_second). -export([run/0]). run()->if false -> X=1; true -> X=2 end,X.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.I(2))
        ),
        new(
            "if-guard-alternatives",
            "-module(oracle_if_guards). -export([run/0]). run()->if hd(atom)=:=1; false -> no; is_integer(42),42>0 -> yes end.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.A("yes"))
        ),
        new(
            "if-no-match",
            "-module(oracle_if_error). -export([run/0]). run()->if false -> no end.",
            Term.Tuple(Term.A(OracleOutcomeTags.Failure),Term.A("error"),Term.A("if_clause"))
        ),
        new(
            "if-body-error",
            "-module(oracle_if_body). -export([run/0]). run()->if true -> throw(selected); true -> no end.",
            Term.Tuple(Term.A(OracleOutcomeTags.Failure),Term.A("throw"),Term.A("selected"))
        ),
        new(
            "if-side-effects",
            "-module(oracle_if_effects). -export([run/0]). run()->put(counter,0),if false -> put(counter,1); true -> put(counter,2) end,get(counter).",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.I(2))
        ),
        new(
            "if-tail-calls",
            "-module(oracle_if_tail). -export([run/0]). run()->loop(50000,0). loop(N,A)->if N=:=0 -> A; N>0 -> loop(N-1,A+1) end.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.I(50000))
        ),
        new(
            "if-closure-branch-export",
            "-module(oracle_if_closure). -export([run/0]). run()->F=fun(N)->if N>0 -> X=42; true -> X=7 end,fun()->X end end,G=F(1),G().",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.I(42))
        ),
        new(
            "begin-export",
            "-module(oracle_begin_export). -export([run/0]). run()->begin X=40,begin Y=2,X+Y end end,{X,Y}.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.Tuple(Term.I(40),Term.I(2)))
        ),
        new(
            "operators-vector",
            "-module(oracle_ops_vector). -export([run/0]). run()->{bnot 0,13 band 6,8 bor 1,7 bxor 3,5 bsl 3,-5 bsr 1,true and false,true or false,true xor true}.",
            Term.Tuple(
                Term.A(OracleOutcomeTags.Success),
                Term.Tuple(
                    Term.I(-1),
                    Term.I(4),
                    Term.I(9),
                    Term.I(4),
                    Term.I(40),
                    Term.I(-3),
                    Term.A("false"),
                    Term.A("true"),
                    Term.A("false")
                )
            )
        ),
        new(
            "begin-tail-calls",
            "-module(oracle_begin_tail). -export([run/0]). run()->loop(50000,0). loop(0,A)->A; loop(N,A)->begin B=A+1,loop(N-1,B) end.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.I(50000))
        ),
        new(
            "operators-precedence",
            "-module(oracle_ops_prec). -export([run/0]). run()->{1 bor 2 band 4,1 bsl 2 + 1,true or false and false,true andalso true andalso payload}.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.Tuple(
                Term.I(1),
                Term.I(5),
                Term.A("true"),
                Term.A("payload")
            ))
        ),
        new(
            "operators-strict-bindings",
            "-module(oracle_ops_bind). -export([run/0]). run()->(X=1)+(Y=2),{X,Y}.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.Tuple(Term.I(1),Term.I(2)))
        ),
        new(
            "operators-strict-error-order",
            "-module(oracle_ops_order). -export([run/0]). run()->atom and error(rhs).",
            Term.Tuple(Term.A(OracleOutcomeTags.Failure),Term.A("error"),Term.A("rhs"))
        ),
        new(
            "operators-big-shifts",
            "-module(oracle_ops_shifts). -export([run/0]). run()->{1 bsl 64,-5 bsl -1,5 bsr -3,-42 bsr 18446744073709551616}.",
            Term.Tuple(
                Term.A(OracleOutcomeTags.Success),
                Term.Tuple(
                    new Integer(System.Numerics.BigInteger.Parse("18446744073709551616")),
                    Term.I(-3),
                    Term.I(40),
                    Term.I(-1)
                )
            )
        ),
        new(
            "catch-throw-exit",
            "-module(oracle_catch_values). -export([run/0]). run()->{catch throw(payload),catch exit(reason)}.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.Tuple(Term.A("payload"),Term.Tuple(Term.A("EXIT"),Term.A("reason"))))
        ),
        new(
            "catch-error-reason-stack",
            "-module(oracle_catch_error). -export([run/0]). run()->{'EXIT',{Reason,Stack}}=catch error(boom),{Reason,is_list(Stack),length(Stack)>0}.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.Tuple(Term.A("boom"),Term.A("true"),Term.A("true")))
        ),
        new(
            "catch-body-binding-rollback",
            "-module(oracle_catch_scope). -export([run/0]). run()->X=42,V=catch begin X=42,throw(done) end,{V,X}.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.Tuple(Term.A("done"),Term.I(42)))
        ),
        new(
            "operators-send-match",
            "-module(oracle_ops_send). -export([run/0]). run()->self() ! X=42,receive X -> X after 0 -> no end.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.I(42))
        ),
        new(
            "begin-closure",
            "-module(oracle_begin_closure). -export([run/0]). run()->begin X=40,F=fun(Y)->begin Z=X+Y,Z end end,F(2) end.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.I(42))
        ),
        new(
            "expr-list-tuple-export",
            "-module(oracle_expr_tuple). -export([run/0]). run()->{X=1,Y=2},{X,Y}.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.Tuple(Term.I(1),Term.I(2)))
        ),
        new(
            "expr-list-call-export",
            "-module(oracle_expr_call). -export([run/0]). run()->T=element(X=1,Y={2}),{T,X,Y}.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.Tuple(Term.I(2),Term.I(1),Term.Tuple(Term.I(2))))
        ),
        new(
            "expr-list-apply-export",
            "-module(oracle_expr_apply). -export([run/0]). run()->F=fun(A,B)->A+B end,T=F(X=1,Y=2),{T,X,Y}.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.Tuple(Term.I(3),Term.I(1),Term.I(2)))
        ),
        new(
            "expr-list-effect-order",
            "-module(oracle_expr_effect). -export([run/0]). run()->put(mark,0),T={put(mark,1),put(mark,2)},{T,get(mark)}.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.Tuple(Term.Tuple(Term.I(0),Term.I(1)),Term.I(2)))
        ),
        new(
            "expr-list-conflict",
            "-module(oracle_expr_conflict). -export([run/0]). run()->{X=1,X=2}.",
            Term.Tuple(Term.A(OracleOutcomeTags.Failure),Term.A("error"),Term.Tuple(Term.A("badmatch"),Term.I(2)))
        ),
        new(
            "expr-list-cons-export",
            "-module(oracle_expr_cons). -export([run/0]). run()->[X=1|Y=tail],{X,Y}.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.Tuple(Term.I(1),Term.A("tail")))
        ),
        new(
            "compiled-binding-nonconstant",
            "-module(oracle_compiled_dynamic). -export([run/0]). run()->{X=id(1),X=id(2)}. id(V)->V.",
            Term.Tuple(Term.A(OracleOutcomeTags.Failure),Term.A("error"),Term.Tuple(Term.A("badmatch"),Term.I(2)))
        ),
        new(
            "compiled-binding-cons-order",
            "-module(oracle_compiled_cons). -export([run/0]). run()->put(mark,none),R=catch [X=1,X=2,put(mark,late)],{'EXIT',{Reason,_}}=R,{Reason,get(mark)}.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.Tuple(Term.Tuple(Term.A("badmatch"),Term.I(2)),Term.A("none")))
        ),
        new(
            "compiled-binding-call-conflict",
            "-module(oracle_compiled_call). -export([run/0]). run()->element(X=1,X=2).",
            Term.Tuple(Term.A(OracleOutcomeTags.Failure),Term.A("error"),Term.Tuple(Term.A("badmatch"),Term.I(2)))
        ),
        new(
            "compiled-binding-fun-local",
            "-module(oracle_compiled_fun). -export([run/0]). run()->A=1,T={X=2,(fun()->X=99,A=1 end)()},{T,X}.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success),Term.Tuple(Term.Tuple(Term.I(2),Term.I(1)),Term.I(2)))
        )
    ];
}
