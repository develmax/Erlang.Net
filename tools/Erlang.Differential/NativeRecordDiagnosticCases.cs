namespace Erlang.Differential;

public static class NativeRecordDiagnosticCases
{
    public static IReadOnlyList<NativeRecordDiagnosticContract> Contracts { get; } = Build();
    public static IReadOnlyList<CompiledDiagnosticCase> All { get; } = Contracts.Select(contract => contract.Case).ToArray();

    private static IReadOnlyList<NativeRecordDiagnosticContract> Build()
    {
        var cases = new List<NativeRecordDiagnosticContract>();

        void Add(
            string name,
            string source,
            string message,
            Term reason,
            string code = NativeRecordDiagnosticNames.RecordCode
        ) => cases.Add(new(
            new CompiledDiagnosticCase(
                "native-record-" + name,
                "-module(native_record_diagnostic). -export([run/0]). " + source,
                "r",
                Kind: DiagnosticCaseKind.NativeRecord
            ),
            code,
            message,
            reason
        ));
        Add(
            "required-field",
            "-record #r{x}. run()->#r{}.",
            "field x is not initialized in native record r",
            Term.Tuple(Term.A(NativeRecordDiagnosticNames.NoValue), Term.A("r"), Term.A("x"))
        );
        Add(
            "default-call",
            "-record #r{x=erlang:self()}. run()->ok.",
            "illegal default value for field x in native record r",
            Term.Tuple(Term.A(NativeRecordDiagnosticNames.IllegalDefault), Term.A("r"), Term.A("x"))
        );
        Add(
            "field-index",
            "-record #r{x=1}. run()->#r.x.",
            "syntax #r.x is only supported for tuple records",
            Term.Tuple(Term.A(NativeRecordDiagnosticNames.IllegalIndex), Term.A("r"), Term.A("x"))
        );
        Add(
            "wildcard-init",
            "-record #r{x=1}. run()->#r{_=42}.",
            "multi-field initialization (assigning to _) is only supported for tuple records",
            Term.A(NativeRecordDiagnosticNames.IllegalMultiField)
        );
        Add(
            "record-info",
            "-record #r{x=1}. run()->record_info(fields,r).",
            "record_info/2 is only supported for tuple records",
            Term.A(NativeRecordDiagnosticNames.IllegalInfo)
        );
        Add(
            "guard-create",
            "-record #r{x=1}. run()->check(ok). check(_) when is_record(#r{})->yes;check(_)->no.",
            "creating a record in a guard is only supported for tuple records",
            Term.A(NativeRecordDiagnosticNames.InGuard),
            NativeRecordDiagnosticNames.GuardCode
        );
        Add(
            "tuple-export",
            "-record(r,{x=1}). -export_record([r]). run()->#r{}.",
            "tuple records cannot be exported; only native records can",
            Term.A(NativeRecordDiagnosticNames.TupleExport)
        );
        Add(
            "undefined-export",
            "-export_record([r]). run()->ok.",
            "native record r undefined",
            Term.Tuple(Term.A(NativeRecordDiagnosticNames.Undefined), Term.A("r"))
        );

        Add(
            "default-list-operator",
            "-record #r{x=[1]++[2]}. run()->ok.",
            "illegal default value for field x in native record r",
            Term.Tuple(Term.A(NativeRecordDiagnosticNames.IllegalDefault), Term.A("r"), Term.A("x"))
        );
        Add(
            "default-map-comparison",
            "-record #r{x=#{}=:=#{}}. run()->ok.",
            "illegal default value for field x in native record r",
            Term.Tuple(Term.A(NativeRecordDiagnosticNames.IllegalDefault), Term.A("r"), Term.A("x"))
        );
        Add(
            "default-short-circuit",
            "-record #r{x=true andalso false}. run()->ok.",
            "illegal default value for field x in native record r",
            Term.Tuple(Term.A(NativeRecordDiagnosticNames.IllegalDefault), Term.A("r"), Term.A("x"))
        );
        Add(
            "default-integer-binary",
            "-record #r{x= <<1>>}. run()->ok.",
            "illegal default value for field x in native record r",
            Term.Tuple(Term.A(NativeRecordDiagnosticNames.IllegalDefault), Term.A("r"), Term.A("x"))
        );
        Add(
            "default-string-comparison",
            "-record #r{x=\"a\"=:=\"a\"}. run()->ok.",
            "illegal default value for field x in native record r",
            Term.Tuple(Term.A(NativeRecordDiagnosticNames.IllegalDefault), Term.A("r"), Term.A("x"))
        );
        Add(
            "type-arity",
            "-record #r{x::integer(atom())}. run()->ok.",
            "type integer(_) undefined",
            Term.Tuple(Term.A(NativeRecordDiagnosticNames.UndefinedType), Term.Tuple(Term.A("integer"), Term.I(1)))
        );
        Add(
            "unknown-construction-field",
            "-record #r{x=1}. run()->#r{z=2}.",
            "Field 'z' is undefined in record 'r'",
            Term.Tuple(Term.A(NativeRecordDiagnosticNames.UndefinedField), Term.A("r"), Term.A("z"))
        );
        Add(
            "import-after-local",
            "-record #r{x=1}. -import_record(ext,[r]). run()->ok.",
            "record r is already defined locally",
            Term.Tuple(Term.A(NativeRecordDiagnosticNames.RedefineLocal), Term.A("r"))
        );
        Add(
            "local-after-import",
            "-import_record(ext,[r]). -record #r{x=1}. run()->ok.",
            "record r already imported from ext",
            Term.Tuple(Term.A(NativeRecordDiagnosticNames.RedefineImported), Term.Tuple(Term.A("ext"), Term.A("r")))
        );
        Add(
            "duplicate-import",
            "-import_record(ext,[r]). -import_record(ext,[r]). run()->ok.",
            "record r already imported from ext",
            Term.Tuple(Term.A(NativeRecordDiagnosticNames.RedefineImported), Term.Tuple(Term.A("ext"), Term.A("r")))
        );

        return cases;
    }
}
