using Erlang.Differential;
using Erlang.Otp;

namespace Erlang.Tests;

public static class ProcLibReportMfaContracts
{
    public static bool Owns(string module, string function, int arity)
        => module == ProcLibReportCaseNames.Module && (function == ProcLibReportCaseNames.Format && arity is 1 or 2 or 3 ||
            function == ProcLibReportCaseNames.ReportCallback && arity == 2);

    public static async Task Run(string function, int arity)
    {
        string name = (function, arity) switch
        {
            (ProcLibReportCaseNames.Format, 1) => ProcLibReportCaseNames.EmptyDefault,
            (ProcLibReportCaseNames.Format, 2) => ProcLibReportCaseNames.EmptyLatin1,
            (ProcLibReportCaseNames.Format, 3) => ProcLibReportCaseNames.EmptyDepth,
            (ProcLibReportCaseNames.ReportCallback, 2) => ProcLibReportCaseNames.EmptyCallback,
            _ => throw new InvalidOperationException(ProcLibReportCaseNames.Mismatch)
        };
        Term report = Term.List(Nil.Value, Nil.Value);
        Term[] arguments = function == ProcLibReportCaseNames.ReportCallback
            ? [new MapTerm([
                new(Term.A(ProcLibReportCaseNames.Label), Term.Tuple(Term.A(ProcLibReportCaseNames.Module), Term.A(ProcLibReportCaseNames.Crash))),
                new(Term.A(ProcLibReportCaseNames.Report), report)
            ]), new MapTerm([new(Term.A(ProcLibReportCaseNames.CharsLimit), Term.I(ProcLibReportCaseNames.LimitedCharacters))])]
            : arity switch
            {
                1 => [report],
                2 => [report, Term.A(ProcLibReportCaseNames.Latin1)],
                _ => [report, Term.A(ProcLibReportCaseNames.Unicode), Term.I(ProcLibReportCaseNames.DirectDepth)]
            };
        await using var runtime = new ProcessRuntime();
        OtpModules.Register(runtime.Modules);
        Term? actual = null;
        var caller = runtime.Spawn(async context =>
        {
            actual = await runtime.Modules.Call(context, ProcLibReportCaseNames.Module, function, arguments);
            return actual;
        });
        Term reason = await caller.Completion.WaitAsync(ProcLibReportProbeNames.Deadline);
        if (!reason.Equals(Term.A(ProcessExitReasons.Normal)) || actual is null)
            throw new InvalidOperationException(ProcLibReportCaseNames.Mismatch);
        var row = ProcLibReportCaseData.Reports.Single(value => value.Name == name);
        Term expected = ProcLibReportCases.Decode(row.ExpectedEtf);
        Term observed = Term.Tuple(Term.A(ProcLibReportCaseNames.Text), Flatten(actual));
        if (!observed.Equals(expected))
            throw new InvalidOperationException(ProcLibReportCaseNames.Mismatch);
    }

    private static Term Flatten(Term value)
    {
        var items = new List<Term>();
        var pending = new Stack<Term>();
        pending.Push(value);
        while (pending.TryPop(out Term? part))
        {
            if (part is Cons list)
            {
                pending.Push(list.Tail);
                pending.Push(list.Head);
            }
            else if (part is Integer)
                items.Add(part);
            else if (part is not Nil)
                throw new InvalidOperationException(ProcLibReportCaseNames.Mismatch);
        }

        return Cons.From(items);
    }
}
