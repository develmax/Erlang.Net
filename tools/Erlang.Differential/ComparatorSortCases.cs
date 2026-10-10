namespace Erlang.Differential;

public static class ComparatorSortCases
{
    private static Term Ok(Term value) => Term.Tuple(Term.A(OracleOutcomeTags.Success), value);

    private static Term Error(string reason) => Term.Tuple(Term.A(OracleOutcomeTags.Failure), Term.A("error"), Term.A(reason));

    private static Term Integers(params int[] values) => Cons.From(values.Select(value => Term.I(value)));

    public static IReadOnlyList<OperatorExpressionCase> All { get; } = [
        new("sort-empty", "lists:sort(fun(A,B)->A=<B end,[])", Ok(Nil.Value)),
        new("sort-single-no-call", "lists:sort(fun(_,_)->error(called) end,[42])", Ok(Integers(42))),
        new("sort-ascending", "lists:sort(fun(A,B)->A=<B end,[3,1,4,1,5,9,2,6])", Ok(Integers(
            1,
            1,
            2,
            3,
            4,
            5,
            6,
            9
        ))),
        new("sort-descending", "lists:sort(fun(A,B)->A>=B end,[3,1,4,1,5,9,2,6])", Ok(Integers(
            9,
            6,
            5,
            4,
            3,
            2,
            1,
            1
        ))),
        new("sort-split-runs", "lists:sort(fun(A,B)->A=<B end,[1,5,3,0,7,2,9,8,6,4])", Ok(Integers(
            0,
            1,
            2,
            3,
            4,
            5,
            6,
            7,
            8,
            9
        ))),
        new("sort-reverse-runs", "lists:sort(fun(A,B)->A=<B end,[9,5,7,10,3,8,1,2,4,6])", Ok(Integers(
            1,
            2,
            3,
            4,
            5,
            6,
            7,
            8,
            9,
            10
        ))),
        new(
            "sort-stable-ties",
            "lists:sort(fun({A,_},{B,_})->A=<B end,[{2,a},{1,b},{2,c},{1,d},{2,e}])",
            Ok(Term.List(
                Term.Tuple(Term.I(1),Term.A("b")),
                Term.Tuple(Term.I(1),Term.A("d")),
                Term.Tuple(Term.I(2),Term.A("a")),
                Term.Tuple(Term.I(2),Term.A("c")),
                Term.Tuple(Term.I(2),Term.A("e"))
            ))
        ),
        new("sort-numeric-ties", "lists:sort(fun(A,B)->A=<B end,[1.0,1,0])", Ok(Term.List(Term.I(0),new FloatTerm(1),Term.I(1)))),
        new("sort-always-true", "lists:sort(fun(_,_)->true end,[3,1,2])", Ok(Integers(3,1,2))),
        new("sort-always-false", "lists:sort(fun(_,_)->false end,[3,1,2])", Ok(Integers(2,1,3))),
        new(
            "sort-nonboolean",
            "lists:sort(fun(_,_)->'maybe' end,[1,2])",
            Term.Tuple(Term.A("error"),Term.A("error"),Term.Tuple(Term.A("case_clause"),Term.A("maybe")))
        ),
        new("sort-callback-error", "lists:sort(fun(_,_)->error(compare_failed) end,[1,2])", Error("compare_failed")),
        new(
            "sort-callback-throw",
            "lists:sort(fun(_,_)->throw(compare_failed) end,[1,2])",
            Term.Tuple(Term.A("error"),Term.A("throw"),Term.A("compare_failed"))
        ),
        new("sort-badfun-empty", "lists:sort(no_fun,[])", Error("function_clause")),
        new(
            "sort-badfun-two",
            "lists:sort(no_fun,[1,2])",
            Term.Tuple(Term.A("error"),Term.A("error"),Term.Tuple(Term.A("badfun"),Term.A("no_fun")))
        ),
        new("sort-improper-one", "lists:sort(fun(A,B)->A=<B end,[1|tail])", Error("function_clause")),
        new("sort-improper-two", "lists:sort(fun(A,B)->A=<B end,[1,2|tail])", Error("function_clause")),
        new("sort-error-before-tail", "lists:sort(fun(_,_)->error(compare_failed) end,[1,2|tail])", Error("compare_failed")),
        new(
            "sort-trace-ascending",
            "begin put(log,[]),lists:sort(fun(A,B)->put(log,[{A,B}|get(log)]),A=<B end,[1,2,3,4]),get(log) end",
            Ok(Term.List(Term.Tuple(Term.I(3),Term.I(4)),Term.Tuple(Term.I(2),Term.I(3)),Term.Tuple(Term.I(1),Term.I(2))))
        ),
        new(
            "iterator-custom-reversed",
            "[K || K := _ <- maps:iterator(#{a=>1,b=>2,c=>3},fun(A,B)->A>B end)]",
            Ok(Term.List(Term.A("c"),Term.A("b"),Term.A("a")))
        ),
        new("iterator-custom-empty", "maps:next(maps:iterator(#{},fun(_,_)->error(called) end))", Ok(Term.A("none"))),
        new("iterator-custom-single", "[V || _ := V <- maps:iterator(#{a=>42},fun(_,_)->error(called) end)]", Ok(Integers(42))),
        new("iterator-custom-badarity", "maps:iterator(#{},fun(X)->X end)", Error("badarg")),
        new("iterator-custom-nonmap", "maps:iterator([],fun(A,B)->A=<B end)", Error("badarg")),
        new("iterator-custom-error", "maps:iterator(#{a=>1,b=>2},fun(_,_)->error(compare_failed) end)", Error("compare_failed")),
        new(
            "iterator-custom-nonboolean",
            "maps:iterator(#{a=>1,b=>2},fun(_,_)->'maybe' end)",
            Term.Tuple(Term.A("error"),Term.A("error"),Term.Tuple(Term.A("case_clause"),Term.A("maybe")))
        ),
        new(
            "iterator-custom-map-output",
            "#{K=>V*2 || K := V <- maps:iterator(#{a=>1,b=>2},fun(A,B)->A>B end)}",
            Ok(new MapTerm([new(Term.A("a"),Term.I(2)),new(Term.A("b"),Term.I(4))]))
        )
    ];

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = All.Select((fixture, index) => new CompiledModuleCase(
        "compiled-" + fixture.Name,
        "-module(oracle_sort_" + index + "). -export([run/0]). run()->" + fixture.Source + ".",
        fixture.Expected
    )).ToArray();
}
