"""Own one fresh replay of the EXISTING binary390 group. No build."""
import argparse, hashlib, importlib.util, json, os, time
from pathlib import Path

NAMES = ["runtime/library390/original-binary-whole-values-and-stacks"]
def sha(p): return hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
def save(p,v): p.write_text(json.dumps(v,indent=2)+"\n",encoding="utf-8")
def require(v,m):
    if not v: raise ValueError(m)
def main():
    ap=argparse.ArgumentParser(); ap.add_argument("--repository",type=Path,required=True); ap.add_argument("--output",type=Path,required=True); args=ap.parse_args()
    repo=args.repository.resolve(); out=args.output.resolve()
    require(os.name=="nt" and out.is_relative_to(repo/"artifacts") and not out.exists(),"Fresh confined Windows output required")
    host=repo/"tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Tests.dll"; runtime=host.with_name("Erlang.Runtime.dll")
    require(host.is_file() and runtime.is_file(),"Actual Debug binaries absent")
    out.mkdir(parents=True); frame=dict(Names=NAMES,Configuration="Debug",HostPath=str(host),RuntimePath=str(runtime),HostBeforeSha256=sha(host),RuntimeBeforeSha256=sha(runtime),StartedUnixNs=time.time_ns(),Passed=False,Errors=[])
    cmd=["dotnet","exec","--fx-version","10.0.11",str(host)]
    for name in NAMES: cmd += ["--case",name]
    cmd += ["--report",str(out/"report.json")]
    try:
        spec=importlib.util.spec_from_file_location("binary536_owned",repo/"tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/owned.py")
        owned=importlib.util.module_from_spec(spec); spec.loader.exec_module(owned)
        env=dict(os.environ,DOTNET_PROCESSOR_COUNT="2",PYTHONDONTWRITEBYTECODE="1")
        frame["Receipt"]=owned.owned(cmd,out,"managed",180,env,str(repo))
    except Exception as e: frame["Errors"].append(repr(e))
    finally:
        frame["CompletedUnixNs"]=time.time_ns()
        try:
            frame.update(HostAfterSha256=sha(host),RuntimeAfterSha256=sha(runtime))
            require(frame["HostBeforeSha256"]==frame["HostAfterSha256"] and frame["RuntimeBeforeSha256"]==frame["RuntimeAfterSha256"],"Managed binaries changed")
            report=json.loads((out/"report.json").read_bytes()); frame["ReportPath"]=str(out/"report.json"); frame["ReportSha256"]=sha(out/"report.json")
            require(report["Passed"]==1 and report["Failed"]==0 and report["SelectedCount"]==1 and {t["Name"] for t in report["Tests"]}==set(NAMES) and all(t["Status"]=="Passed" for t in report["Tests"]),"The existing binary390 group did not pass")
            # Owned receipt remains authoritative even when helper raised.
            receipt=json.loads((out/"managed.exit.json").read_bytes()); frame["Receipt"]=receipt
            require(receipt["Settled"] and receipt["ExitCode"]==0 and not receipt["TimedOut"] and not receipt["Failures"],"Managed child failed physical completion")
            frame["ReceiptPath"]=str(out/"managed.exit.json"); frame["ReceiptSha256"]=sha(out/"managed.exit.json")
        except Exception as e: frame["Errors"].append("postflight: "+repr(e))
        frame["Passed"]=not frame["Errors"]; save(out/"frame.json",frame)
    return 0 if frame["Passed"] else 1
if __name__=="__main__": raise SystemExit(main())
