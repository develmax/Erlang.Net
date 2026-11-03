"""Passive whole-byte comparison, two fresh original modes vs runtime-bound existing binary390 group."""
import argparse, hashlib, json
from pathlib import Path

PIN="ad05823719d77c8faee87348ea39513d4e2f99c5"
NAMES=["runtime/library390/original-binary-whole-values-and-stacks"]
MODES={"binary390-values":94,"binary390-stacks":470}
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
        require(frame["Passed"] and not frame["Errors"] and frame["Configuration"] in ("Debug", "Release") and set(NAMES).issubset(frame["Names"]),"Managed frame did not pass declared binary group")
        require(frame["RuntimeBeforeSha256"]==frame["RuntimeAfterSha256"]==runtime_sha,"Frame runtime differs from requested current supplied runtime")
        require(sha(bind(frame["RuntimePath"]))==runtime_sha,"Actual runtime changed/stale")
        require(frame["HostBeforeSha256"]==frame["HostAfterSha256"]==sha(bind(frame["HostPath"])),"Actual managed host changed/stale")
        receipt_path=bind(frame["ReceiptPath"]); require(sha(receipt_path)==frame["ReceiptSha256"],"Frame child receipt changed"); receipt=read(receipt_path)
        require(receipt==frame["Receipt"] and receipt["Settled"] and receipt["ExitCode"]==0 and not receipt["TimedOut"] and not receipt["Failures"],"Managed child not settled successfully")
        require(receipt["StdoutSha256"]==sha(bind(receipt_path.with_name(receipt_path.name.removesuffix(".exit.json")+".stdout.log"))) and receipt["StderrSha256"]==sha(bind(receipt_path.with_name(receipt_path.name.removesuffix(".exit.json")+".stderr.log"))),"Managed child streams changed")
        require(str(Path(frame["HostPath"])) in receipt["Command"] and receipt["Command"].count("--case")==len(frame["Names"]) and all(n in receipt["Command"] for n in NAMES),"Managed command did not execute exact admitted groups")
        report_path=bind(frame["ReportPath"]); require(sha(report_path)==frame["ReportSha256"],"Frame report changed"); report=read(report_path)
        require(report["Passed"]==len(frame["Names"]) and report["Failed"]==0 and report["SelectedCount"]==len(frame["Names"]) and {t["Name"] for t in report["Tests"]}==set(frame["Names"]) and all(t["Status"]=="Passed" for t in report["Tests"]),"Managed report differs")
        plan_path=bind(repo/"tools/Runtime/Erlang.OwnedGuest506Observations/binary619-reuse-plan.json"); plan=read(plan_path)
        require(plan["Pin"]==PIN and plan["NoNewCases"] and not plan["FrameProjection"],"Original reuse plan differs")
        for row in plan["Inputs"]: require(sha(bind(repo/row["Path"]))==row["Sha256"],"Original fixture/helper source changed: "+row["Path"])
        completion=read(bind(original/"completion.json")); state["OriginalCompletion"]=completion
        require(completion["Passed"] and not completion["Errors"] and completion["SourcePin"]==PIN and completion["FreshOriginalExecution"] and not completion["RawEtfRewritten"] and not completion["FrameProjection"],"Original completion not admitted")
        require(len(completion["Modes"])==2 and {r["Name"] for r in completion["Modes"]}==set(MODES),"Two exact original modes required")
        before=read(bind(original/"provider-before.json")); after=read(bind(original/"provider-after.json"))
        require(after["ProviderEqual"] and after["Inventory"]==before["Inventory"] and len(before["Inventory"])==6427,"Official provider changed or is incomplete")
        require(sha(bind(before["AdmissionPath"]))==before["AdmissionSha256"],"Provider admission changed")
        state["PreflightPassed"]=True
    except Exception as e: errors.append("preflight: "+repr(e)); state["PreflightPassed"]=False
    selected={}
    if state["PreflightPassed"]:
        try:
            candidates=[]
            for cp in (repo/"artifacts/library390-binary-comparisons").glob("*/comparison.json"):
                info=read(cp)
                if info.get("RuntimeSha256")==runtime_sha and frame["StartedUnixNs"]<=cp.stat().st_mtime_ns<=frame["CompletedUnixNs"]: candidates.append((cp,info))
            require(len(candidates)==1,"One fresh runtime-bound binary390 receipt in owned frame required")
            cp,info=candidates[0]; inside(cp)
            require(info["SourceImageSha256"]=="E398EE7CFEEFFBE73A47AF70BFCB7D7BE1B221F101F7BC52ABF28EE70DB19553" and info["ObserverImageSha256"]=="5DF957E353EBB41BBE58B972F25FF8DBE3986D11667FC5178A97A22082923871" and info["OriginalWholeImageOwned"] and info["NativePrimitives"]==22,"Genuine original binary/observer/native provenance failed")
            require(info["ExpectedCompleteOutcomes"]==564 and len(info["Streams"])==2 and not info["Errors"] and info["CleanupPassed"] and not info["FrameFieldsProjected"],"Binary390 whole streams/cleanup failed")
            cleanup=info["Cleanup"]
            require(all(cleanup[k] for k in ["DisposeCompleted","OwnerClosed","OwnerEmpty","ActiveBuildsZero","NoRetirementFailures","AllObservedPhysicalFreed"]),"Binary390 actual cleanup incomplete")
            require(type(cleanup["ObservedLiveBackingsBeforeDispose"]) is int and cleanup["ObservedLiveBackingsBeforeDispose"]>=0,"Observed backing census invalid")
            state["ActualManagedCleanup"]=cleanup
            for name,entry,actualfile,count,golden in [("binary390-values","run","actual-values.etf",94,"values.etf"),("binary390-stacks","run_stacks","actual-stacks.etf",470,"stacks.etf")]:
                rows=[r for r in info["Streams"] if r["Entry"]==entry]; require(len(rows)==1,"One exact binary stream required"); row=rows[0]
                require(row["CompleteOutcomes"]==count and row["ActualFile"]==actualfile and row["WholeRawEqual"] and row["WholeTypedEqual"],"Whole typed/raw binary stream failed")
                actual=inside(cp.parent/actualfile); require(sha(actual)==row["ActualSha256"]==row["OriginalSha256"]==sha(bind(repo/"tests/Erlang.Tests/Resources/LibraryBinary390"/golden)),"Managed binary stream/immutable golden hash differs")
                selected[name]=actual
        except Exception as e: errors.append("binary390: "+repr(e))
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
    result=dict(Passed=not errors and len(comparisons)==2,IndependentPassiveComparison=True,RuntimeSha256=runtime_sha,Configuration=frame.get("Configuration") if state["PreflightPassed"] else None,ActualManagedCleanup=state.get("ActualManagedCleanup"),NoCumulativeResourceCountClaim=True,CompleteOutcomes=sum(x["CompleteOutcomes"] for x in comparisons if x["Passed"]),ExpectedCompleteOutcomes=564,Modes=comparisons,Errors=errors,BoundFiles=bound,FrameProjection=False,RawNormalization=False,ApiDelta=0,FullContractDelta=0)
    (out/"comparison.json").write_text(json.dumps(result,indent=2)+"\n",encoding="utf-8")
    return 0 if result["Passed"] else 1
if __name__=="__main__": raise SystemExit(main())
