namespace Erlang.Differential;

public static class QueueCoreCaseNames
{
    public const string Empty = "queue/core/empty";
    public const string EmptySource = """
        -module(queue_core_empty).
        -export([run/0]).
        run()->Q=queue:new(), {[],[]}=Q, true=queue:is_queue(Q), true=queue:is_empty(Q), 0=queue:len(Q), []=queue:to_list(Q), {empty,Q}=queue:out(Q), done.
        """;

    public const string Representation = "queue/core/representation";
    public const string RepresentationSource = """
        -module(queue_core_representation).
        -export([run/0]).
        run()->{[a],[]}=queue:from_list([a]), {[b],[a]}=queue:from_list([a,b]), {[c,b],[a]}=queue:from_list([a,b,c]), {[d,c],[a,b]}=queue:from_list([a,b,c,d]), {[e,d,c],[a,b]}=queue:from_list([a,b,c,d,e]), done.
        """;

    public const string Persistence = "queue/core/persistence";
    public const string PersistenceSource = """
        -module(queue_core_persistence).
        -export([run/0]).
        run()->Q0=queue:new(), Q1=queue:in(a,Q0), Q2=queue:in(b,Q1), Q3=queue:in(c,Q2), {[c,b],[a]}=Q3, {{value,a},{[c],[b]}}=queue:out(Q3), [a,b,c]=queue:to_list(Q3), {[a],[]}=Q1, 3=queue:len(Q3), done.
        """;

    public const string RawShapes = "queue/core/raw-shapes";
    public const string RawShapesSource = """
        -module(queue_core_raw_shapes).
        -export([run/0]).
        run()->{{value,1},{[3],[2]}}=queue:out({[3,2,1],[]}), {{value,1},{[5,4],[2,3]}}=queue:out({[5,4,3,2],[1]}), {{value,1},{[],[2,3]}}=queue:out({[],[1,2,3]}), {{value,1},{[],[]}}=queue:out({[],[1]}), done.
        """;

    public const string Improper = "queue/core/improper";
    public const string ImproperSource = """
        -module(queue_core_improper).
        -export([run/0]).
        run()->Q={[a|tail],[]}, true=queue:is_queue(Q), false=queue:is_empty(Q), {[x,a|tail],[]}=queue:in(x,Q), {{value,a},{[],tail}}=queue:out({[],[a|tail]}), {{value,a},{[b|tail],[c|tail]}}=queue:out({[b|tail],[a,c|tail]}), {error,badarg}=capture(fun()->queue:len(Q) end), {error,badarg}=capture(fun()->queue:to_list(Q) end), {error,badarg}=capture(fun()->queue:out(Q) end), {error,badarg}=capture(fun()->queue:from_list([a|tail]) end), done.
        capture(F)->try F() of V->{ok,V} catch C:R->{C,R} end.
        """;

    public const string Ordering = "queue/core/ordering";
    public const string OrderingSource = """
        -module(queue_core_ordering).
        -export([run/0]).
        run()->L=lists:seq(1,1024), Q=build(L,queue:new()), 1024=queue:len(Q), L=queue:to_list(Q), L=drain(Q), L=queue:to_list(Q), done.
        build([],Q)->Q; build([X|Xs],Q)->build(Xs,queue:in(X,Q)).
        drain(Q)->case queue:out(Q) of {empty,_}->[]; {{value,V},Rest}->[V|drain(Rest)] end.
        """;

    public const string Errors = "queue/core/errors";
    public const string ErrorsSource = """
        -module(queue_core_errors).
        -export([run/0]).
        run()->check([bad,[],{},{[]},{[],[],[]},{bad,[]},{[],bad}]), {error,badarg}=capture(fun()->queue:from_list(bad) end), done.
        capture(F)->try F() of V->{ok,V} catch C:R->{C,R} end.
        check([])->ok; check([Q|Qs])->false=queue:is_queue(Q), {error,badarg}=capture(fun()->queue:is_empty(Q) end), {error,badarg}=capture(fun()->queue:len(Q) end), {error,badarg}=capture(fun()->queue:in(x,Q) end), {error,badarg}=capture(fun()->queue:out(Q) end), {error,badarg}=capture(fun()->queue:to_list(Q) end), check(Qs).
        """;

    public const string Heterogeneous = "queue/core/heterogeneous";
    public const string HeterogeneousSource = """
        -module(queue_core_heterogeneous).
        -export([run/0]).
        run()->L=[a,1,1.0,#{x=>[x|tail]},<<1:1>>,{x,[]}], Q=queue:from_list(L), 6=queue:len(Q), L=queue:to_list(Q), true=queue:is_queue(Q), false=queue:is_empty(Q), done.
        """;

}
