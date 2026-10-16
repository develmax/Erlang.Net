from pathlib import Path
import json,hashlib,base64,re
base=Path('artifacts/proc-lib337-candidate');out=Path('artifacts/proc-lib337-permanent-tests');src=out/'source';native=Path('artifacts/part336-remote-new-review/format/artifact-12063070500/capture');obs=json.loads((native/'observations.json').read_bytes());assert obs['Executed']==76
j=lambda x:json.dumps(x,ensure_ascii=True)
def write(path,text):
 p=src/path;p.parent.mkdir(parents=True,exist_ok=True);p.write_text(text.replace('\r\n','\n'),encoding='utf-8')
def read(path):return (base/path).read_text(encoding='utf-8-sig')
rows=[];report=[];primitive=[]
for r in obs['Cases']:
 d=native/(r['Track']+'--'+r['Name']);b=(d/'returned.etf').read_bytes();assert hashlib.sha256(b).hexdigest().upper()==r['RawValueEtf']['Sha256'];expected=base64.b64encode(b).decode();rows.append(dict(Name=r['Name'],Track=r['Track'],ExpectedEtfBytes=len(b),ExpectedEtfSha256=hashlib.sha256(b).hexdigest().upper(),OriginalPath=str(d/'returned.etf')))
 if r['Track']=='report':
  source=(d/r['CopiedSource']['Path']).read_bytes();assert hashlib.sha256(source).hexdigest().upper()==r['Source']['Sha256'];report.append((r['Name'],source.decode(),expected))
 else:primitive.append((r['Name'],expected))
assert len(report)==57 and len(primitive)==19
write(Path('tools/Erlang.Differential/ProcLibReportCaseData.cs'),'namespace Erlang.Differential;\n\npublic static class ProcLibReportCaseData\n{\n    public static IReadOnlyList<(string Name, string Source, string ExpectedEtf)> Reports { get; } =\n    [\n'+',\n'.join('        ('+','.join(map(j,r))+')' for r in report)+'\n    ];\n\n    public static IReadOnlyDictionary<string, string> PrimitiveExpected { get; } = new Dictionary<string, string>\n    {\n'+',\n'.join('        ['+j(n)+'] = '+j(e) for n,e in primitive)+'\n    };\n}\n')
write(Path('tools/Erlang.Differential/ProcLibReportCaseNames.cs'),'''namespace Erlang.Differential;

public static class ProcLibReportCaseNames
{
    public const string Prefix = "proc-lib-report-";
    public const string Module = "proc_lib";
    public const string Format = "format";
    public const string ReportCallback = "report_cb";
    public const string EmptyDefault = "empty-default";
    public const string EmptyLatin1 = "empty-explicit-latin1";
    public const string EmptyDepth = "empty-depth-unused";
    public const string EmptyCallback = "empty-limited-report";
    public const string Text = "text";
    public const string Latin1 = "latin1";
    public const string Unicode = "unicode";
    public const string Label = "label";
    public const string Crash = "crash";
    public const string Report = "report";
    public const string CharsLimit = "chars_limit";
    public const int LimitedCharacters = 80;
    public const int DirectDepth = 2;
    public const string Mismatch = "The proc_lib report differs from the independently executed OTP result.";
}
''')
write(Path('tools/Erlang.Differential/ProcLibReportCases.cs'),'''namespace Erlang.Differential;

public static class ProcLibReportCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = ProcLibReportCaseData.Reports.Select(row => new CompiledModuleCase(
        ProcLibReportCaseNames.Prefix + row.Name,
        row.Source,
        Term.Tuple(Term.A(OracleOutcomeTags.Success), Decode(row.ExpectedEtf))
    )).ToArray();

    public static Term Decode(string expectedEtf)
        => ExternalTermFormat.Decode(Convert.FromBase64String(expectedEtf));
}
''')
p=Path('tools/Erlang.Differential/CompiledModuleCases.cs');t=read(p);old='        .. ProcLibStopCases.Modules';assert t.count(old)==1;write(p,t.replace(old,old+',\n        .. ProcLibReportCases.Modules'))
for name in ['ProcLibReportProbeData.cs','ProcLibReportProbeChecks.cs','ProcLibReportProbeNames.cs','ProcLibUnicodeContinuationHostChecks.cs','ProcLibUnicodeContinuationHostNames.cs']:
 p=Path('tests/Erlang.Tests')/name;write(p,read(p))
p=Path('tests/Erlang.Tests/ProcLibReportProbeChecks.cs');t=(src/p).read_text();marker='            Console.WriteLine(JsonSerializer.Serialize(new { probe.Name, Role = ProcLibReportProbeNames.SdkObservation, Actual = actual.ToString(), RawEtfBase64 = Convert.ToBase64String(ExternalTermFormat.Encode(actual)), ComparedWithOriginal = false }));';assert t.count(marker)==1
t=t.replace(marker,'''            Term expected = Term.Tuple(Term.A(OracleOutcomeTags.Success), ProcLibReportCases.Decode(ProcLibReportCaseData.PrimitiveExpected[probe.Name]));
            if (!actual.Equals(expected))
                throw new InvalidOperationException(ProcLibReportCaseNames.Mismatch + probe.Name);
''')
# Existing observational report runner becomes strict too; normal catalog executes each fixture once.
start=t.index('    public static async Task CaptureReports()');end=t.index('    public static async Task PrettySourceContracts()',start)
t=t[:start]+'''    public static async Task CaptureReports()
    {
        foreach (var fixture in ProcLibReportCases.Modules)
            new Erlang.Compiler.Parser(fixture.Source).ParseModule();
        foreach (var fixture in ProcLibReportCases.Modules)
        {
            using var artifact = GeneratedModuleCompiler.Compile(fixture.Source);
            Term actual = await CompiledModuleExecution.Run(artifact);
            if (!actual.Equals(fixture.Expected))
                throw new InvalidOperationException(ProcLibReportCaseNames.Mismatch + fixture.Name);
        }
    }

'''+t[end:];write(p,t)
p=Path('tests/Erlang.Tests/ProcLibReportProbeNames.cs');t=(src/p).read_text();marker='    public const string Success = "ok";';t=t.replace(marker,marker+'''
    public const string PrimitiveTest = "proc_lib report primitives match original OTP";
    public const string PrettyTest = "proc_lib pretty printer source contracts";
    public const string CallbackTest = "proc_lib report awaits its actual extended callback";
    public const string DeepUnicodeTest = "proc_lib unicode continuation handles deep empty prefixes";''');write(p,t)
write(Path('tests/Erlang.Tests/ProcLibReportMfaContracts.cs'),'''using Erlang.Differential;
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
        await caller.Unwound.WaitAsync(ProcLibReportProbeNames.Deadline);
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
''')
p=Path('tests/Erlang.Tests/Program.cs');t=read(p);marker='var tests = new List<(string Name, Func<Task> Body)>();';assert t.count(marker)==1;t=t.replace(marker,marker+'''
Test(Erlang.Tests.ProcLibReportProbeNames.PrimitiveTest, Erlang.Tests.ProcLibReportProbeChecks.CapturePrimitives);
Test(Erlang.Tests.ProcLibReportProbeNames.PrettyTest, Erlang.Tests.ProcLibReportProbeChecks.PrettySourceContracts);
Test(Erlang.Tests.ProcLibReportProbeNames.CallbackTest, Erlang.Tests.ProcLibReportProbeChecks.CallbackAwaitControl);
Test(Erlang.Tests.ProcLibReportProbeNames.DeepUnicodeTest, Erlang.Tests.ProcLibUnicodeContinuationHostChecks.Run);''')
marker='        if (Erlang.Tests.ProcLibMfaContracts.Owns(export.Module))';assert t.count(marker)==1;t=t.replace(marker,'''        if (Erlang.Tests.ProcLibReportMfaContracts.Owns(export.Module, export.Function, export.Arity))
        {
            await Erlang.Tests.ProcLibReportMfaContracts.Run(export.Function, export.Arity);

            return;
        }
'''+marker);write(p,t)
(out/'original-expected-provenance.json').write_text(json.dumps(dict(Status='EXPECTED_VALUES_EXCLUSIVELY_FROM_ORIGINAL_RETURNED_ETF',OriginalObservationsSha256=hashlib.sha256((native/'observations.json').read_bytes()).hexdigest().upper(),Rows=rows,Rows76=len(rows),SdkExpectedValuesUsed=False),indent=2)+'\n')
files=[]
for p in sorted(src.rglob('*')):
 if p.is_file():
  b=p.read_bytes();old=base/p.relative_to(src);files.append(dict(Path=p.relative_to(src).as_posix(),Bytes=len(b),Sha256=hashlib.sha256(b).hexdigest().upper(),ParentSha256=hashlib.sha256(old.read_bytes()).hexdigest().upper() if old.exists() else None))
f=out/'source-freeze.json';f.write_text(json.dumps(dict(Status='UNEXECUTED_PERMANENT_TEST_SOURCE_PROPOSAL',Files=files,CompiledReports57=True,PrimitiveOriginalComparisons19=True,HostGroups4=True,DirectMfas4=True,OriginalExpectedProvenanceSha256=hashlib.sha256((out/'original-expected-provenance.json').read_bytes()).hexdigest().upper(),SdkExecuted=False,MainChanged=False,Candidate337Changed=False),indent=2)+'\n');print(len(files),hashlib.sha256(f.read_bytes()).hexdigest().upper())
