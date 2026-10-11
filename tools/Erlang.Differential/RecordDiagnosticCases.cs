namespace Erlang.Differential;

public static class RecordDiagnosticCases
{
    public static IReadOnlyList<CompiledDiagnosticCase> All { get; } =
    [
        new(
            "record-undefined",
            "-module(record_undefined). -export([run/0]). run()->#state{}.",
            "state",
            Kind: DiagnosticCaseKind.UndefinedRecord
        ),
        new(
            "record-field-undefined",
            "-module(record_field_undefined). -export([run/0]). -record(state,{a}). run()->#state{zzzzzz=value}.",
            "state",
            "zzzzzz",
            DiagnosticCaseKind.UndefinedRecordField
        ),
        new(
            "record-field-duplicate",
            "-module(record_field_duplicate). -export([run/0]). -record(state,{a,a}). run()->ok.",
            "state",
            "a",
            DiagnosticCaseKind.DuplicateRecordField
        ),
        new(
            "record-duplicate",
            "-module(record_duplicate). -export([run/0]). -record(state,{a}). -record(state,{b}). run()->ok.",
            "state",
            Kind: DiagnosticCaseKind.DuplicateRecord
        ),
        new(
            "record-wildcard-update",
            "-module(record_wildcard). -export([run/0]). -record(state,{a}). run()->R=#state{},R#state{_=value}.",
            "state",
            Kind: DiagnosticCaseKind.RecordWildcardUpdate
        )
    ];
}
