"""Passive whole-byte comparison, five fresh original modes vs runtime-bound three existing managed groups."""
import argparse, hashlib, json
from pathlib import Path

PIN="ad05823719d77c8faee87348ea39513d4e2f99c5"
NAMES=["runtime/library412/real-re-core-original-whole-terms-and-stacks","runtime/library413/original-re8-inspect-internal-run-import-full-outcomes","runtime/native/regex485/binary-captures-whole-176"]
MODES={"core411-values":16,"core411-stacks":80,"three413-values":42,"three413-stacks":210,"captures485-values":176}
def sha(p): return hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
def read(p): return json.loads(Path(p).read_bytes())
def require(v,m):
    if not v: raise ValueError(m)
def main():
    ap=argparse.ArgumentParser(); ap.add_argument("--repository",type=Path,required=True); ap.add_argument("--original",type=Path,required=True); ap.add_argument("--output",type=Path,required=True); ap.add_argument("--runtime-sha256",required=True); ap.add_argument("--managed-frame",type=Path,required=True); args=ap.parse_args()
    repo=args.repository.resolve(); original=args.original.resolve(); out=args.output.resolve(); runtime_sha=args.runtime_sha256.upper()
    require(not out.exists(),"Fresh explicitly selected comparison output required"); out.mkdir(parents=True)
    errors=[]; comparisons=[]; bound={}; state={}
    def bind(p):
        p=Path(p).resolve(); require(p.is_relative_to(repo) or p==args.managed_frame.resolve(),"Evidence outside repository: "+str(p)); bound[str(p)]={"Sha256":sha(p),"Bytes":p.stat().st_size}; return p
    def inside(p):
        p=bind(p); require(frame["StartedUnixNs"]<=p.stat().st_mtime_ns<=frame["CompletedUnixNs"],"Managed artifact is outside exact owned frame: "+str(p)); return p
    try:
        frame_path=bind(args.managed_frame); frame=read(frame_path)
        require(frame["Passed"] and not frame["Errors"] and frame["Configuration"]=="Debug" and set(NAMES).issubset(frame["Names"]),"Managed frame did not pass exact Debug groups")
        require(frame["RuntimeBeforeSha256"]==frame["RuntimeAfterSha256"]==runtime_sha,"Frame runtime differs from requested current Debug runtime")
        require(sha(bind(frame["RuntimePath"]))==runtime_sha,"Actual runtime changed/stale")
        require(frame["HostBeforeSha256"]==frame["HostAfterSha256"]==sha(bind(frame["HostPath"])),"Actual managed host changed/stale")
        receipt_path=bind(frame["ReceiptPath"]); require(sha(receipt_path)==frame["ReceiptSha256"],"Frame child receipt changed"); receipt=read(receipt_path)
        require(receipt==frame["Receipt"] and receipt["Settled"] and receipt["ExitCode"]==0 and not receipt["TimedOut"] and not receipt["Failures"],"Managed child not settled successfully")
        require(receipt["StdoutSha256"]==sha(bind(receipt_path.with_name(receipt_path.name.removesuffix(".exit.json")+".stdout.log"))) and receipt["StderrSha256"]==sha(bind(receipt_path.with_name(receipt_path.name.removesuffix(".exit.json")+".stderr.log"))),"Managed child streams changed")
        require(str(Path(frame["HostPath"])) in receipt["Command"] and receipt["Command"].count("--case")==len(frame["Names"]) and all(n in receipt["Command"] for n in NAMES),"Managed command did not execute exact admitted groups")
        report_path=bind(frame["ReportPath"]); require(sha(report_path)==frame["ReportSha256"],"Frame report changed"); report=read(report_path)
        require(report["Passed"]==len(frame["Names"]) and report["Failed"]==0 and report["SelectedCount"]==len(frame["Names"]) and {t["Name"] for t in report["Tests"]}==set(frame["Names"]) and all(t["Status"]=="Passed" for t in report["Tests"]),"Managed report differs")
        plan_path=bind(repo/"tools/Runtime/Erlang.OwnedGuest506Observations/regex613-reuse-plan.json"); plan=read(plan_path)
        require(plan["Pin"]==PIN and plan["NoNewCases"] and not plan["FrameProjection"],"Original reuse plan differs")
        for row in plan["Inputs"]: require(sha(bind(repo/row["Path"]))==row["Sha256"],"Original fixture/helper source changed: "+row["Path"])
        completion=read(bind(original/"completion.json")); state["OriginalCompletion"]=completion
        require(completion["Passed"] and not completion["Errors"] and completion["SourcePin"]==PIN and completion["FreshOriginalExecution"] and not completion["RawEtfRewritten"] and not completion["FrameProjection"],"Original completion not admitted")
        require(len(completion["Modes"])==5 and {r["Name"] for r in completion["Modes"]}==set(MODES),"Five exact original modes required")
        before=read(bind(original/"provider-before.json")); after=read(bind(original/"provider-after.json"))
        require(after["ProviderEqual"] and after["Inventory"]==before["Inventory"] and len(before["Inventory"])==6427,"Official provider changed or is incomplete")
        require(sha(bind(before["AdmissionPath"]))==before["AdmissionSha256"],"Provider admission changed")
        state["PreflightPassed"]=True
    except Exception as e: errors.append("preflight: "+repr(e)); state["PreflightPassed"]=False
    selected={}
    if state["PreflightPassed"]:
        for label,base,count in [("core411",repo/"artifacts/library412-re-permanent-comparisons/re-core411",96),("three413",repo/"artifacts/library413-re-three-comparisons/re-three413",252)]:
            try:
                candidates=[]
                for cp in base.glob("*/comparison.json"):
                    info=read(cp)
                    if info.get("RuntimeSha256")==runtime_sha and frame["StartedUnixNs"]<=cp.stat().st_mtime_ns<=frame["CompletedUnixNs"]: candidates.append((cp,info))
                require(len(candidates)==1,"One fresh runtime-bound "+label+" receipt required")
                cp,info=candidates[0]; inside(cp)
                require(info["ExaminedCompleteOutcomeRows"]==count and info["WholeTypedEqualRows"]==count and info["WholeValuesRawEqual"] and info["WholeStacksRawEqual"] and info["WholeValuesTermEqual"] and info["WholeStacksTermEqual"] and not info["Differences"] and not info["CaptureErrors"] and not info["FrameFieldsProjected"],label+" full typed/raw comparison failed")
                for kind in ["values","stacks"]:
                    actual=inside(cp.parent/("actual-"+kind+".etf")); key="Actual"+("Values" if kind=="values" else "Stacks")+"Sha256"; require(sha(actual)==info[key],label+" raw artifact differs from receipt"); selected[label+"-"+kind]=actual
            except Exception as e: errors.append(label+": "+repr(e))
        try:
            cp=inside(repo/"artifacts/library485/permanent/comparison.json"); info=read(cp); cleanup=info["Cleanup"]
            require(info["Count"]==176 and info["WholeRawEqual"] and not info["Errors"] and info["CleanupPassed"],"485 whole comparison/cleanup did not pass")
            require(cleanup["Closed"] and cleanup["LibraryFreed"] and cleanup["Active"]==0 and cleanup["RetainedCodes"]==0 and cleanup["CodesCreated"]==cleanup["CodesFreed"] and cleanup["MatchDataCreated"]==cleanup["MatchDataFreed"],"485 actual physical cleanup incomplete")
            selected["captures485-values"]=inside(cp.parent/"managed-values.etf")
        except Exception as e: errors.append("captures485: "+repr(e))
        for name,count in MODES.items():
            row=dict(Mode=name,CompleteOutcomes=count,Passed=False)
            try:
                matching=[r for r in completion["Modes"] if r["Name"]==name]; require(len(matching)==1,"Original mode duplicated"); observed=matching[0]
                require(observed["Passed"] and observed["WholeRawEqual"] and observed["FullErrorsAndStacksRetained"] and not observed["RawEtfRewritten"],"Original whole-byte policy failed")
                d=original/name; proc=read(bind(d/"original.exit.json")); require(proc==observed["Process"] and proc["Settled"] and proc["ExitCode"]==0 and not proc["TimedOut"] and not proc["Failures"],"Original process did not settle")
                require(proc["StdoutSha256"]==sha(bind(d/"original.stdout.log")) and proc["StderrSha256"]==sha(bind(d/"original.stderr.log")),"Original streams changed")
                owner_before=bind(d/"stock-owners-before.etf"); owner_after=bind(d/"stock-owners-after.etf"); require(owner_before.read_bytes()==owner_after.read_bytes(),"Actual original stock owners changed"); bind(d/"observer-owner.etf"); bind(d/"profile.etf")
                actual=bind(d/"observed.etf"); require(sha(actual)==observed["ActualSha256"],"Original result changed")
                declared=[r for r in plan["Modes"] if r["Name"]==name]; require(len(declared)==1,"Original plan mode missing/duplicated"); declared=declared[0]
                fixture=repo/declared["FixtureDirectory"]; golden=bind(fixture/declared["Golden"]); require(sha(golden)==observed["GoldenSha256"]==sha(actual),"Original result no longer matches immutable golden")
                for copied in declared["Copies"]: require(sha(bind(d/copied))==sha(bind(fixture/copied)),"Copied original observer/source differs")
                require(name in selected,"Managed prerequisite failed for "+name); managed=selected[name]; require(actual.read_bytes()==managed.read_bytes(),"Whole unprojected original/managed bytes differ")
                row.update(Passed=True,OriginalPath=str(actual),ManagedPath=str(managed),Sha256=sha(actual),Bytes=actual.stat().st_size,WholeRawEqual=True,FrameProjection=False,RawNormalization=False)
            except Exception as e: row["Error"]=repr(e); errors.append(name+": "+repr(e))
            comparisons.append(row)
    result=dict(Passed=not errors and len(comparisons)==5,IndependentPassiveComparison=True,CurrentDebugRuntimeSha256=runtime_sha,CompleteOutcomes=sum(x["CompleteOutcomes"] for x in comparisons if x["Passed"]),ExpectedCompleteOutcomes=524,Modes=comparisons,Errors=errors,BoundFiles=bound,FrameProjection=False,RawNormalization=False,ApiDelta=0,FullContractDelta=0)
    (out/"comparison.json").write_text(json.dumps(result,indent=2)+"\n",encoding="utf-8")
    return 0 if result["Passed"] else 1
if __name__=="__main__": raise SystemExit(main())
