from pathlib import Path
import json,hashlib,subprocess,copy,difflib
R=Path(__file__).resolve().parents[2];O=Path(__file__).parent
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
def put(path,text):
 p=O/path;p.parent.mkdir(parents=True,exist_ok=True);p.write_text(text,encoding="utf-8",newline="\n");return p
def raw(path,data):
 p=O/path;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(data);return p
HEAD=subprocess.check_output(["git","rev-parse","HEAD"],cwd=R).decode().strip();assert HEAD=="50e756f73437eb8d2882a19f729bccb09af74f3d"
S=R/"artifacts/size-bif354-proposal2";B=R/"artifacts/backtrace-depth354-proposal2";K=R/"artifacts/source-known354-reporting-proposal"
assert H(S/"source-freeze.json")=="455AA5E75D2C0A6D57075BDBF4F35F46D06930F7F50C8BF9C31D06118F41E4FC"
assert H(B/"source-freeze.json")=="0AE98AB4884CBD3BAACF0761F714382B2836A983DC3D3955C025A4CF8CE0F86A"
assert H(K/"source-freeze.json")=="549B3C721087E20B22A08B5734DACA99E20F32AD00B5EAC4017B753A70B493BD"
for folder in (S,B,K):
 f=json.loads((folder/"source-freeze.json").read_text(encoding="utf-8"))
 for row in f["Files"]:
  p=folder/row["Path"];assert p.stat().st_size==row["Bytes"] and H(p)==row["Sha256"],p
owned={};kinds={}
def own(target,data,kind):
 assert target not in owned,target
 p=raw("source/"+target,data);owned[target]=p;kinds[target]=kind
for folder in (S,B):
 for p in sorted((folder/"source/src").rglob("*.cs")):
  own(p.relative_to(folder/"source").as_posix(),p.read_bytes(),"ReviewedPrivateRuntimeProposal")
fixtures=[]
for row in json.loads((S/"fixture-catalog.json").read_text())["Cases"]:
 fixtures.append((row["Module"],(S/row["Source"]).read_text(encoding="utf-8")))
fixtures.append(("backtrace_depth_flag",(B/"fixtures/backtrace_depth_flag.erl").read_text(encoding="utf-8")))
text=(B/"fixtures/size_depth_controls.erl").read_text(encoding="utf-8").replace("size_depth_controls","size_depth_import_controls")
text=text.replace("-export([", "-import(erlang,[tuple_size/1,map_size/1,bit_size/1,byte_size/1]).\n-export([",1)
for name in ("tuple_size","map_size","bit_size","byte_size"):
 text=text.replace("R=erlang:"+name+"(V)","R="+name+"(V)")
fixtures.append(("size_depth_import_controls",text))
G=R/"artifacts/backtrace354-generic-frame-audit"
assert H(G/"source-freeze.json")=="0C6DCD425AA6653780EA80BB546F008AF6D3E11B0408D55564574E78AE1CB8F6"
for m in ("generic_depth_error","generic_depth_hd"):
 fixtures.append((m,(G/("fixtures/"+m+".erl")).read_text(encoding="utf-8").replace("R=leaf(V)","R="+m+":leaf(V)")))
assert len(fixtures)==16 and len({m for m,s in fixtures})==16
raw("observations/generic_depth_observe.erl",(G/"observations/generic_depth_observe.erl").read_bytes())
# Repair only source-invalid map-pattern association; keep exact one-field metadata equality.
fixed=[]
for module,source in fixtures:
 lines=[]
 for line in source.splitlines():
  if "[{error_info,#{module=>erl_erts_errors}}]" in line:
   line=line.replace("[{error_info,#{module=>erl_erts_errors}}]","[{error_info,ErrorInfo}]")
   if "=Stack,done" in line:
    line=line.replace("=Stack,done","=Stack,true=(ErrorInfo =:= #{module=>erl_erts_errors}),done")
   elif line.rstrip().endswith(","):
    line+="\n        true=(ErrorInfo =:= #{module=>erl_erts_errors}),"
   elif line.rstrip().endswith("=Stack"):
    line+=",\n        true=(ErrorInfo =:= #{module=>erl_erts_errors})"
   else:raise AssertionError((module,line))
  lines.append(line)
 fixed.append((module,"\n".join(lines)+"\n"))
fixtures=fixed
for module,source in fixtures:put("fixtures/"+module+".erl",source)
for p in sorted((S/"observations").glob("*.erl")) if (S/"observations").exists() else []:raw("observations/"+p.name,p.read_bytes())
# Source1 stores raw observers beside assertion fixtures; they are NOT compiled goldens.
for p in sorted((S/"fixtures").glob("size_observe_*.erl")):raw("observations/"+p.name,p.read_bytes())
def identifier(module):return "".join(word.title() for word in module.split("_"))
namecs="namespace Erlang.Differential;\n\npublic static class SizeErrorCaseNames\n{\n    public const string Done = \"done\";\n"+"".join(f"    public const string {identifier(m)} = \"{m}\";\n" for m,s in fixtures)+"}\n"
own("tools/Erlang.Differential/SizeErrorCaseNames.cs",namecs.encode(),"NewPortableFixtureNames")
sourcecs="namespace Erlang.Differential;\n\npublic static class SizeErrorCaseSources\n{\n"
for m,s in fixtures:
 assert s.endswith("\n")
 sourcecs+=f"    public const string {identifier(m)} = \"\"\"\n"+"".join("        "+line+"\n" for line in s.splitlines())+"\n        \"\"\";\n\n"
sourcecs+="}\n"
own("tools/Erlang.Differential/SizeErrorCaseSources.cs",sourcecs.encode(),"ExactPortableSourcesRawLiteralTerminalLf")
casecs="namespace Erlang.Differential;\n\npublic static class SizeErrorCases\n{\n    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = [\n"+",\n".join(f"        new(\n            SizeErrorCaseNames.{identifier(m)},\n            SizeErrorCaseSources.{identifier(m)},\n            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SizeErrorCaseNames.Done))\n        )" for m,s in fixtures)+"\n    ];\n}\n"
own("tools/Erlang.Differential/SizeErrorCases.cs",casecs.encode(),"SixteenSourceDerivedAssertionsIncludesTwoExpectedSourceGapWitnesses")
path="tools/Erlang.Differential/CompiledModuleCases.cs";s=(R/path).read_text(encoding="utf-8");needle="        .. SchedulerAffinityCases.Modules\n";assert s.count(needle)==1;s=s.replace(needle,"        .. SchedulerAffinityCases.Modules,\n        .. SizeErrorCases.Modules\n");own(path,s.encode(),"AppendOnlyCompiledCatalog")
# Simple actual two-runtime effect: invoke the public module, join real actors normally.
host="""namespace Erlang.Tests;

public static class BacktraceDepthHostChecks
{
    public static async Task IndependentRuntimes()
    {
        await using var first = new ProcessRuntime();
        await using var second = new ProcessRuntime();
        var actor = first.Spawn(async context =>
        {
            Require((await Set(context, 0)).Equals(Term.I(8)));
            return Term.A(BacktraceDepthHostNames.Done);
        });
        Require((await actor.Completion.WaitAsync(TimeSpan.FromSeconds(5))).Equals(Term.A(BacktraceDepthHostNames.Normal)));
        var other = second.Spawn(async context =>
        {
            Require((await Set(context, 1)).Equals(Term.I(8)));
            return Term.A(BacktraceDepthHostNames.Done);
        });
        Require((await other.Completion.WaitAsync(TimeSpan.FromSeconds(5))).Equals(Term.A(BacktraceDepthHostNames.Normal)));
        var observed = first.Spawn(async context =>
        {
            Require((await Set(context, 8)).Equals(Term.I(0)));
            return Term.A(BacktraceDepthHostNames.Done);
        });
        Require((await observed.Completion.WaitAsync(TimeSpan.FromSeconds(5))).Equals(Term.A(BacktraceDepthHostNames.Normal)));
    }

    private static ValueTask<Term> Set(ProcessContext context, int depth) => context.Runtime.Modules.Call(
        context,
        BacktraceDepthHostNames.Module,
        BacktraceDepthHostNames.Function,
        Term.A(BacktraceDepthHostNames.Flag),
        Term.I(depth)
    );

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(BacktraceDepthHostNames.Failure);
    }
}
"""
own("tests/Erlang.Tests/BacktraceDepthHostChecks.cs",host.encode(),"ActualPublicFlagIndependentRuntimeEffectNormalJoin")
names="""namespace Erlang.Tests;

internal static class BacktraceDepthHostNames
{
    internal const string Module = "erlang";
    internal const string Function = "system_flag";
    internal const string Flag = "backtrace_depth";
    internal const string Done = "done";
    internal const string Normal = "normal";
    internal const string Failure = "backtrace runtime isolation contract failed";
    internal const string Test = "runtime/backtrace-depth-independent-runtimes";
}
"""
own("tests/Erlang.Tests/BacktraceDepthHostNames.cs",names.encode(),"HostSemanticNames")
path="tests/Erlang.Tests/Program.cs";s=(R/path).read_text(encoding="utf-8");needle="int failed = 0;";assert s.count(needle)==1;s=s.replace(needle,"Test(Erlang.Tests.BacktraceDepthHostNames.Test, Erlang.Tests.BacktraceDepthHostChecks.IndependentRuntimes);\n\n"+needle);own(path,s.encode(),"OneAdditiveHostHookAllExistingTimeoutsUnchanged")
# Independently inspectable source-only preservation request, not a self-admission.
ledger=json.loads((R/"docs/mfa-compatibility.json").read_text(encoding="utf-8"));newledger=json.loads((K/"ledger-candidate.json").read_text(encoding="utf-8"));assert H(R/"docs/mfa-compatibility.json")==json.loads((K/"classification.json").read_text())["ParentLedgerSha256"]
affected=[]
for row in newledger["VerifiedSourceFiles"]:
 if row["Path"] in owned:
  affected.append(dict(Path=row["Path"],OldSha256=row["Sha256"],NewSha256=H(owned[row["Path"]])))
  assert H(R/row["Path"])==row["Sha256"]
  row["Sha256"]=H(owned[row["Path"]])
proof=dict(Status="SOURCE_SCOPE_PRESERVATION_REQUEST_REQUIRES_INDEPENDENT_ROOT_PEER",ParentHead=HEAD,AffectedExistingGuards=affected,GuardCount=len(newledger["VerifiedSourceFiles"]),VerifiedMfas=[dict(Module=e["Module"],Function=e["Function"],Arity=e["Arity"]) for e in ledger["Entries"] if e["Status"]=="Verified compatible"],Rationale=["CoreModules only four invalid size branches; all sixteen verified registrations/predicate values unchanged.","ProcessRuntime BacktraceDepth default8 remains and per-runtime getter/setter signature retained; synchronized backing field and atomic exchange add no scheduling/callbacks. No verified predicate/self/node body changed.","SequenceTraceRegistrations adds one exact backtrace_depth key, preserving all previous key branches and invalid fallback. No verified module dispatch or source resolution changed.","New error factory is used only in four invalid size paths; existing custom/explicit exceptions never rewritten; guard/matchspec catch handling unchanged."],Limit="Caller tail/file/line parity not established; only non-tail routes and narrow depth top/second-MFA assertions proposed. Original corroboration and managed focus remain pending.",ApprovalRequiredBeforeComposition=True,NoExecution=True,GenericDepthGap="Generic error1/hd first-capture depth remains unfixed; both source-derived checks are in proposed asserted catalog and must execute/retain actual failures. No blanket flag-complete claim.")
q=put("scope-preservation-request.json",json.dumps(proof,indent=2)+"\n")
# Candidate evidence is a request until Root independently admits the precise frozen source.
for e in newledger["Entries"]:
 if e["Status"]=="Verified compatible":e["Evidence"].append(dict(Kind="SourceReview",Path="validation/part-354/size-error-source-preservation.json",Sha256=H(q),Scope="Narrow size-error/backtrace-key source-preservation request; valid predicate/self/node bodies unchanged. Requires independent Root review before candidate composition; no new Verified promotion."))
own("docs/mfa-compatibility.json",(json.dumps(newledger,indent=2,ensure_ascii=False)+"\n").encode(),"ConditionalGuardRefreshAndExactlyFiveSourceKnownRows")
own("docs/validation/part-354/size-error-source-preservation.json",q.read_bytes(),"ConditionalPreservationEvidenceSourceRequest")
own("docs/validation/part-354/source-known-options/source-known-review.json",(K/"source-known-review.json").read_bytes(),"ExactlyFiveSourceKnownEvidence")
# Current source proof anchors and observations remain outside the candidate execution closure.
put("fixture-manifest.json",json.dumps(dict(Cases=[dict(Module=m,Fixture="fixtures/"+m+".erl",Bytes=len(s.encode()),Sha256=H(O/("fixtures/"+m+".erl")),Policy="Default",CallerContext="UntrappedFresh",Expected="{ok,done}") for m,s in fixtures],ObservationFiles=[dict(Path=p.relative_to(O).as_posix(),Bytes=p.stat().st_size,Sha256=H(p)) for p in sorted((O/"observations").glob("*.erl"))],SourceAddition="Duplicate depth source replaced by explicit-erlang-import depth module. Generic error/hd middle calls qualified own-module leaf to prevent local no_return analysis discarding middle continuation. Raw fixture history preserved."),indent=2)+"\n")
# Reconstruct C# raw constants according to delimiter newline stripping, assert exact file bytes.
for m,s in fixtures:
 needle=f"    public const string {identifier(m)} = \"\"\"\n";fragment=sourcecs.split(needle,1)[1].split("        \"\"\";",1)[0];lines=fragment.splitlines(True);assert lines[-1]=="\n";decoded="".join(line[8:] if line.startswith("        ") else line for line in lines[:-1]);assert decoded==s,m
# Every source comes from CURRENT Main raw, never obsolete composition SourcePath fields.
base=json.loads((R/"artifacts/scheduler-affinity353-composition-plan2/composition-plan.json").read_text())
mapping={row["Path"]:R/row["Path"] for row in base["Files"]}
tracked=subprocess.check_output(["git","ls-files","-z"],cwd=R).decode().split("\0")
for path in tracked:
 if path and not path.startswith("docs/"):mapping[path]=R/path
# Include current ledger/evidence dependency paths, regardless of older composition membership.
readiness={"docs/mfa-compatibility.json","docs/supported-mfas.json","docs/module-readiness-scope.json","docs/otp-inventory.json","reference-baseline.json"}
for e in newledger["Entries"]:
 for ev in e["Evidence"]:
  path="docs/"+ev["Path"];readiness.add(path)
for path in readiness:
 if path not in owned:assert (R/path).is_file(),path
 mapping[path]=owned.get(path,R/path)
for path,p in owned.items():mapping[path]=p
assert not any(p.startswith("artifacts/") or "/bin/" in p or "/obj/" in p or "LICENSE.local" in p for p in mapping)
rows=[]
for path,p in sorted(mapping.items()):
 assert p.is_file(),path
 rows.append(dict(Path=path,SourcePath=p.relative_to(R).as_posix(),Bytes=p.stat().st_size,Sha256=H(p),Kind=kinds.get(path,"CurrentPublishedMainRaw")))
ownedrows=[dict(Path=path,Bytes=p.stat().st_size,Sha256=H(p),ParentSha256=H(R/path) if (R/path).is_file() else None,Kind=kinds[path]) for path,p in sorted(owned.items())]
put("owned-source.json",json.dumps(dict(ParentHead=HEAD,Files=ownedrows),indent=2)+"\n")
put("proposed-source-closure.json",json.dumps(dict(Status="SOURCE_ONLY_NOT_COMPOSED",Files=rows),indent=2)+"\n")
added=["oracle/compiled-module/"+m for m,s in fixtures]+["runtime/backtrace-depth-independent-runtimes"]
put("focused-execution-plan.json",json.dumps(dict(Status="UNEXECUTED_SOURCE_ONLY",ParseAll=[m for m,s in fixtures]+[p.stem for p in sorted((O/"observations").glob("*.erl"))],AssertedCompiledCases=[m for m,s in fixtures],HostMethod="Erlang.Tests.BacktraceDepthHostChecks.IndependentRuntimes",ExpectedManagedChecks=17,RawObservers="Observe separately with raw returned ETF; never add invented expected terms to permanent catalog.",Build="Actual tests/Erlang.Tests project and existing Differential generation; Root owns fresh build/parse/isolated per-case bounded process runner.",OldRun="Existing owned actor dispatch Run/canonical15 and all previous test bodies unchanged; Root may include unchanged old Run as regression, not a new fixture.",ExpectedCurrentGenericGaps="generic_depth_error and generic_depth_hd are ASSERTED cases, not hidden observations. Record their actual RED if current generic capture misses native contract; no green slice completion based only on four size factories.",Original="New original execution needed for stack error_info and depth routes; no installed Windows erl and no original result invented.",NoFullClaim=True),indent=2)+"\n")
put("added-tests.json",json.dumps(dict(Names=added,Count=len(added),Ordering="Existing compiled-module loop inserts new14 before later old registrations; old names retain order under exact filtering, not a new full old-prefix claim. Host isolation registered at end."),indent=2)+"\n")
pkg=[]
for row in base["PackageResources"]:
 path=row["Path"] if isinstance(row,dict) else row
 p=R/path;pkg.append(dict(Path=path,Bytes=p.stat().st_size,Sha256=H(p)))
put("package-resources.json",json.dumps(dict(Files=pkg),indent=2)+"\n")
put("readiness-inputs.json",json.dumps(dict(Files=[dict(Path=path,Bytes=mapping[path].stat().st_size,Sha256=H(mapping[path])) for path in sorted(readiness)]),indent=2)+"\n")
put("proposal.json",json.dumps(dict(Status="REVIEWABLE_FOCUSED354_INTEGRATION_PLAN_NOT_APPLIED_NOT_SDK_READY_UNTIL_PEER",ParentHead=HEAD,Destination="artifacts/size-bif354-focused-candidate",BaseCompositionPlanSha256=H(R/"artifacts/scheduler-affinity353-composition-plan2/composition-plan.json"),BasePathRows=len(base["Files"]),CurrentMainRaw=True,OwnedPaths=len(ownedrows),RuntimeSourceFiles=9,ClosureRows=len(rows),StableClosureRows=len(rows)-1,PackageRows=len(pkg),ReadinessInputRows=len(readiness),VerifiedSourceGuardCount=len(newledger["VerifiedSourceFiles"]),ChangedGuardPaths=affected,Counts={"Verified compatible":16,"Known differences":29,"Needs verification":542},ExistingVerified16StatusScopeLimitsExact=True,FiveKnownReportingOnly=True,Other582StatusScopeLimitsExact=True,NewPortable16=True,AdditionalHostRuntimeIsolation=True,ExpectedNewManagedChecks=17,Prerequisites=["Independent combined nine-runtime source/16-verified scope admission", "Current parent and all Main raw bytes still exact before fresh composition", "Root-owned bounded build+ParseAll before any fixture execution"],NoMainMutation=True,NoSdk=True,NoNetwork=True,NoNative=True,NoFullFreeze=True,GenericDepthAssertionsIncluded=True,ActualFailuresMustBeRetained=True),indent=2)+"\n")
files=[dict(Path=p.relative_to(O).as_posix(),Bytes=p.stat().st_size,Sha256=H(p)) for p in sorted(O.rglob("*")) if p.is_file() and p.name!="source-freeze.json"]
put("source-freeze.json",json.dumps(dict(Status="IMMUTABLE_PLAN_ONLY_FOCUSED354_COMBINED_SOURCE_REQUIRES_INDEPENDENT_PEER",ParentHead=HEAD,Files=files,UpstreamFreezes=[dict(Path=str((p/"source-freeze.json").relative_to(R)).replace("\\","/"),Sha256=H(p/"source-freeze.json")) for p in (S,B,K)]),indent=2)+"\n");print(H(O/"source-freeze.json"));print(json.dumps(dict(Owned=len(ownedrows),Closure=len(rows),Readiness=len(readiness),Affected=affected)))
