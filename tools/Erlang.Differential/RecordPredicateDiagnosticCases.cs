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
        )
    ];
}
