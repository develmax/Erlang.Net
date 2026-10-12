namespace Erlang.Differential;

public static class RecordPredicateDiagnosticCases
{
    public static IReadOnlyList<CompiledDiagnosticCase> All { get; } =
    [
        new(
            "record-predicate-undefined-local",
            "-module(record_predicate_bad_local). -export([run/0]). run()->is_record({missing},missing).",
            "missing",
            Kind: DiagnosticCaseKind.UndefinedRecord
        ),
        new(
            "record-predicate-undefined-remote",
            "-module(record_predicate_bad_remote). -export([run/0]). run()->erlang:is_record({missing},missing).",
            "missing",
            Kind: DiagnosticCaseKind.UndefinedRecord
        ),
        new(
            "record-predicate-unbound-before-zero-fold",
            "-module(record_predicate_unbound). -export([run/0]). run()->erlang:is_record(Unbound,r,0).",
            "Unbound"
        ),
        new(
            "record-predicate-dynamic-two-illegal-guard",
            "-module(record_predicate_guard). -export([run/0]). run()->check({r},r). check(X,N) when is_record(X,N)->true;check(_,_)->false.",
            "is_record",
            Kind: DiagnosticCaseKind.RecordPredicateGuard
        ),
        new(
            "record-predicate-three-published-dynamic-guard",
            "-module(record_predicate_contract). -export([run/0]).  run()->yes=check({r,one},r,2),no=check({r},r,2),no=check({r},42,1),no=check({r},r,1.0),done. id(X)->X. check(X,N,S) when is_record(X,N,S)->yes;check(_,_,_)->no.",
            "is_record",
            Kind: DiagnosticCaseKind.RecordPredicateThreeGuard
        ),
        new(
            "record-predicate-three-dynamic-name",
            "-module(record_guard_name). -export([run/0]). run()->check({r},r). check(X,N) when is_record(X,N,1)->true;check(_,_)->false.",
            "is_record",
            Kind: DiagnosticCaseKind.RecordPredicateThreeGuard
        ),
        new(
            "record-predicate-three-dynamic-size",
            "-module(record_guard_size). -export([run/0]). run()->check({r},1). check(X,S) when is_record(X,r,S)->true;check(_,_)->false.",
            "is_record",
            Kind: DiagnosticCaseKind.RecordPredicateThreeGuard
        ),
        new(
            "record-predicate-three-remote-dynamic-size",
            "-module(record_guard_remote). -export([run/0]). run()->check({r},1). check(X,S) when erlang:is_record(X,r,S)->true;check(_,_)->false.",
            "is_record",
            Kind: DiagnosticCaseKind.RecordPredicateThreeGuard
        ),
        new(
            "record-predicate-three-float-size",
            "-module(record_guard_float). -export([run/0]). run()->check({r}). check(X) when is_record(X,r,1.0)->true;check(_)->false.",
            "is_record",
            Kind: DiagnosticCaseKind.RecordPredicateThreeGuard
        ),
        new(
            "record-predicate-three-integer-name",
            "-module(record_guard_tag). -export([run/0]). run()->check({r}). check(X) when is_record(X,42,1)->true;check(_)->false.",
            "is_record",
            Kind: DiagnosticCaseKind.RecordPredicateThreeGuard
        )
    ];
}
