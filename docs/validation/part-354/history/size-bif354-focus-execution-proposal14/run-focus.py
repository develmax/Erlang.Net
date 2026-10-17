"""Root-only bounded initial354 diagnostic. Retains actual RED; no full/readiness."""
from pathlib import Path
import argparse,hashlib,importlib.util,json,os,base64
R=Path(__file__).resolve().parents[2];P=Path(__file__).parent
h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
load=lambda p:json.loads(p.read_bytes())
ap=argparse.ArgumentParser();ap.add_argument("--approved-freeze-sha256",required=True);ap.add_argument("--materialization-receipt",required=True);ap.add_argument("--output",required=True);ap.add_argument("--dotnet",default="dotnet");a=ap.parse_args()
f=P/"source-freeze.json";assert h(f)==a.approved_freeze_sha256.upper()
for row in load(f)["Files"]:
 q=P/row["Path"];assert q.stat().st_size==row["Bytes"] and h(q)==row["Sha256"]
v=load(P/"execution-inputs.json");receipt=load(Path(a.materialization_receipt));assert receipt["ExecutionFreezeSha256"]==h(f) and receipt["ExecutionInputsSha256"]==h(P/"execution-inputs.json")
c=Path(receipt["Candidate"]).resolve();assert c==R/v["Candidate"] and c.is_relative_to(R/"artifacts")
def verify():
 for row in v["Rows"]:
  q=c/row["Path"];assert q.stat().st_size==row["Bytes"] and h(q)==row["Sha256"],row["Path"]
verify();helper=R/v["HelperPath"];assert h(helper)==v["HelperSha256"]
spec=importlib.util.spec_from_file_location("size354_owned",helper);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
o=Path(a.output).resolve();assert o.is_relative_to(R/"artifacts");o.mkdir(parents=True,exist_ok=False)
env=dict(os.environ,DOTNET_PROCESSOR_COUNT="2",MSBUILDDISABLENODEREUSE="1",DOTNET_CLI_TELEMETRY_OPTOUT="1")
m.write(o/"inputs.json",{"SourceFreezeSha256":h(f),"ExecutionInputsSha256":h(P/"execution-inputs.json"),"MaterializationReceiptSha256":h(Path(a.materialization_receipt)),"RunnerSha256":h(Path(__file__)),"Candidate":str(c),"BuildCpuCount":2,"NodeReuse":False,"FullExecuted":False,"ReadinessExecuted":False,"PriorInitialGenericTwoFailuresObserved":True})
try:
 m.owned([a.dotnet,"build","tests/Erlang.Size354.Focus/Focus.csproj","--disable-build-servers","-m:1","-nr:false","--nologo","-v:minimal"],o,"build",240,env,c)
except Exception:
 m.write(o/"completion.json",{"Status":"BUILD_FAILED_BEFORE_PARSE_OR_GUEST","ManagedExecuted":0,"SourceFreezeSha256":h(f)});raise
dll=c/"tests/Erlang.Size354.Focus/bin/Debug/net10.0/Size354Focus.dll"
initial={str(q.resolve()):h(q) for q in sorted(dll.parent.glob("*.dll"))};assert initial
m.write(o/"initial-built-dlls.json",{"Files":initial})
def loaded(actual):
 assert len(actual["LoadedAssemblies"])==7
 assert len({x["Path"] for x in actual["LoadedAssemblies"]})==7
 for row in actual["LoadedAssemblies"]:
  q=Path(row["Path"]).resolve();assert q.is_relative_to(dll.parent) and initial[str(q)]==row["Sha256"]==h(q)
try:
 m.owned([a.dotnet,str(dll),"--parse",str(o/"parse.json")],o,"parse",45,env,c)
 parsed=load(o/"parse.json");assert parsed["Passed"] and parsed["Failure"] is None and len(parsed["Sources"])==36;loaded(parsed)
 expected={Path(x["Path"]).stem:x["Sha256"] for x in v["Rows"] if x["Kind"]=="PrivateParseAllSource"};assert {x["Name"]:x["Sha256"] for x in parsed["Sources"]}==expected
except Exception:
 m.write(o/"completion.json",{"Status":"PARSE_FAILED_BEFORE_GUEST","ManagedExecuted":0,"SourceFreezeSha256":h(f)});raise
results=[];targets=[("--case",name) for name in v["Cases"]]+[("--host","runtime/backtrace-depth-independent-runtimes"),("--common-host","runtime/common-capture-identity-rollback-async")]+[("--handler","handler/"+name) for name in ("RunState","RunFrames","RunNestedFrames","RunSourceLease","RunIsolation")]+[("--fallback-host","runtime/common-capture-null-fallback-prior-host")]+[("--nif-host","runtime/configured-load-nif-common-capture"),("--raw-handler","runtime/raw-selected-handler-observation")]+[("--undefined-lambda","runtime/undefined-lambda-publication-lifecycle"),("--reinstall","runtime/trusted-preloaded-reinstall-identity"),("--captured-lambda","runtime/common-selected-lambda-capture")];assert len(targets)==44
for i,(mode,name) in enumerate(targets):
 d=o/("target-"+str(i).zfill(2));d.mkdir();out=d/"outcome.json";cmd=[a.dotnet,str(dll),mode]+([name] if mode=="--case" else [name.split("/",1)[1]] if mode=="--handler" else [])+[str(out)]
 try:m.owned(cmd,d,"process",90,env,c)
 except RuntimeError:
  process=load(d/"process.exit.json")
  if not process["Settled"] or process["TimedOut"] or process["Failures"] or process["ExitCode"]!=1 or not out.is_file():raise
 actual=load(out);process=load(d/"process.exit.json");assert actual["Name"]==name and actual["Passed"]==(process["ExitCode"]==0);loaded(actual)
 lines=(d/"process.stdout.log").read_text(encoding="utf-8-sig").splitlines();marker=("PASS " if actual["Passed"] else "FAIL ")+name
 if mode=="--raw-handler" and actual["Passed"]:
  assert len(lines)==2 and lines[0].startswith("RAW_SELECTED_HANDLER_ETF ") and lines[1]==marker
  payload=base64.b64decode(lines[0].split(" ",1)[1],validate=True);assert payload[:1]==bytes([131]);(d/"selected-handler.etf").write_bytes(payload)
 else:assert lines==[marker]

 if mode=="--case":
  obs=actual["Observation"]
  # Compile/runtime infrastructure failures are retained as actual failures too, never invented native failures.
  if obs is not None:
   row=next(x for x in v["Rows"] if x["Path"]=="tests/Erlang.Size354.Focus/Fixtures/"+name+".erl");assert obs["SourceSha256"]==row["Sha256"] and len(obs["CSharpSha256"])==64
   if actual["Passed"]:assert obs["ActualEtfHex"]==obs["ExpectedEtfHex"] and obs["StandardOutputHex"]==obs["StandardErrorHex"]==""
 results.append({"Name":name,"Passed":actual["Passed"],"OutcomeSha256":h(out),"ExitSha256":h(d/"process.exit.json")});m.write(o/"progress.json",{"Executed":len(results),"Passed":sum(x["Passed"] for x in results),"Last":name})
verify()
for path,digest in initial.items():assert h(Path(path))==digest
m.write(o/"completion.json",{"Status":"ACTUAL_INITIAL354_DIAGNOSTIC_REQUIRES_INDEPENDENT_AUDIT","ManagedExecuted":44,"Passed":sum(x["Passed"] for x in results),"Failed":sum(not x["Passed"] for x in results),"ParseAll":36,"Targets":results,"SourceRows":3248,"SourceFreezeSha256":h(f),"FullExecuted":False,"ReadinessExecuted":False,"NativeExecuted":False,"MainChanged":False,"AnyActualFailureRetained":True})
print("Actual initial354 diagnostic complete; inspect per-target failures; no full acceptance.")
