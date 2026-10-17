"""Passive initial354 acquisition/semantic split. No SDK or downloaded-code execution."""
from pathlib import Path
import argparse,hashlib,json,re
R=Path(__file__).resolve().parents[2];P=Path(__file__).parent
h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
rd=lambda p:json.loads(p.read_bytes())
ap=argparse.ArgumentParser();ap.add_argument("--output",required=True);a=ap.parse_args();out=Path(a.output).resolve();assert out.is_relative_to(R/"artifacts") and not out.exists()
F=R/"artifacts/size-bif354-focus-execution-proposal";freeze=F/"source-freeze.json";assert h(freeze)=="1898567EC6C2E1D9E1DEF98F6EFB0A2383E544B363974E0D01A56BA07B03FD54"
for row in rd(freeze)["Files"]:
 q=F/row["Path"];assert q.stat().st_size==row["Bytes"] and h(q)==row["Sha256"]
v=rd(F/"execution-inputs.json");c=R/v["Candidate"];d=R/"artifacts/size-bif354-focused-actual1"
materialized=R/"artifacts/root-size354-materialized3.json";assert h(materialized)=="0E48CF0ACFD21F77DCAA6EFF89DAA90CD5AF78E09CF2987A6740EF9FE2445EBC"
assert rd(materialized)["ExecutionFreezeSha256"]==h(freeze)
for row in v["Rows"]:
 q=c/row["Path"];assert q.stat().st_size==row["Bytes"] and h(q)==row["Sha256"],row["Path"]
assert len(v["Rows"])==3144
inputs=rd(d/"inputs.json");assert inputs["SourceFreezeSha256"]==h(freeze) and inputs["ExecutionInputsSha256"]==h(F/"execution-inputs.json") and inputs["RunnerSha256"]==h(F/"run-focus.py") and inputs["MaterializationReceiptSha256"]==h(materialized)
evidence=[]
def receipt(folder,name,allowed):
 p=folder/(name+".exit.json");x=rd(p);live=rd(folder/(name+".live.json"));assert isinstance(x["Pid"],int) and x["Pid"]>0 and live["Pid"]==x["Pid"]
 assert x["Settled"] is True and x["TimedOut"] is False and x["Failures"]==[] and x["ExitCode"] in allowed
 assert x["StdoutSha256"]==h(folder/(name+".stdout.log")) and x["StderrSha256"]==h(folder/(name+".stderr.log"))
 assert x["ElapsedSeconds"]>=0
 evidence.append({"Path":p.relative_to(R).as_posix(),"Sha256":h(p),"Pid":x["Pid"],"ExitCode":x["ExitCode"]});return x
completion=rd(d/"completion.json")
if completion["Status"]=="BUILD_FAILED_BEFORE_PARSE_OR_GUEST":
 x=receipt(d,"build",set(range(1,256)));assert completion["ManagedExecuted"]==0 and not (d/"parse.exit.json").exists() and not list(d.glob("target-*"))
 result={"Status":"PASS_INDEPENDENT354_BUILD_FAILURE_ACQUISITION_NO_GUEST_EXECUTION","ManagedExecuted":0,"SemanticPassed":0,"SemanticFailed":0,"FullAccepted":False,"BuildExitCode":x["ExitCode"]}
else:
 receipt(d,"build",{0})
 text=(d/"build.stdout.log").read_text(encoding="utf-8-sig")
 for english,russian in ((r"^\s*([0-9]+) Warning\(s\)\s*$",r"^\s*Предупреждений:\s*([0-9]+)\s*$"),(r"^\s*([0-9]+) Error\(s\)\s*$",r"^\s*Ошибок:\s*([0-9]+)\s*$")):
  hits=re.findall(english,text,re.M)+re.findall(russian,text,re.M);assert hits and all(int(x)==0 for x in hits),hits
 initial=rd(d/"initial-built-dlls.json")["Files"];initial={str(Path(k).resolve()).casefold():val for k,val in initial.items()};assert initial
 dllroot=c/"tests/Erlang.Size354.Focus/bin/Debug/net10.0"
 def loaded(actual):
  rows=actual["LoadedAssemblies"];assert len(rows)==len({x["Name"] for x in rows})==len({x["Path"].casefold() for x in rows})==7
  assert {x["Name"] for x in rows}=={"Erlang.Terms","Erlang.Runtime","Erlang.Compiler","Erlang.Serialization","Erlang.Differential","Erlang.Tests","Size354Focus"}
  for row in rows:
   q=Path(row["Path"]).resolve();assert q.is_relative_to(dllroot) and initial[str(q).casefold()]==row["Sha256"]==h(q)
 receipt(d,"parse",{0});parsed=rd(d/"parse.json");assert parsed["Passed"] is True and parsed["Failure"] is None and parsed["Name"]=="--parse";loaded(parsed)
 expected={Path(x["Path"]).stem:x["Sha256"] for x in v["Rows"] if x["Kind"]=="PrivateParseAllSource"};assert len(expected)==21 and {x["Name"]:x["Sha256"] for x in parsed["Sources"]}==expected
 # Passive decode only the tiny source-derived expected ETF tuple; accepts canonical Latin1/UTF8 atom tags.
 def expected_done(value):
  data=bytes.fromhex(value);assert data[:3]==bytes([131,104,2]);at=3;atoms=[]
  for _ in range(2):
   tag=data[at];at+=1;assert tag in (100,118,119)
   n=data[at] if tag==119 else int.from_bytes(data[at:at+2],"big");at+=1 if tag==119 else 2
   atoms.append(data[at:at+n].decode("latin1" if tag==100 else "utf-8"));at+=n
  assert at==len(data) and atoms==["ok","done"]
 targets=v["Cases"]+["runtime/backtrace-depth-independent-runtimes"];assert len(targets)==len(set(targets))==17
 actual=[]
 for i,name in enumerate(targets):
  folder=d/("target-"+str(i).zfill(2));process=receipt(folder,"process",{0,1});p=folder/"outcome.json";x=rd(p);assert x["Name"]==name and isinstance(x["Passed"],bool) and x["Passed"]==(process["ExitCode"]==0);loaded(x)
  assert x["NativeExecuted"] is False and x["FullValidationExecuted"] is False and x["ReadinessExecuted"] is False
  assert (folder/"process.stdout.log").read_text(encoding="utf-8-sig").strip()==("PASS " if x["Passed"] else "FAIL ")+name
  assert (x["Failure"] is None)==x["Passed"]
  obs=x["Observation"]
  if i<16 and obs is not None:
   assert obs["Name"]==name and obs["SourceSha256"]==expected[name] and re.fullmatch("[0-9A-F]{64}",obs["CSharpSha256"])
   assert obs["CallerContext"]==0 and obs["CompilerPolicy"]==0
   expected_done(obs["ExpectedEtfHex"])
   if x["Passed"]:assert obs["ActualEtfHex"]==obs["ExpectedEtfHex"] and obs["StandardOutputHex"]==obs["StandardErrorHex"]==""
  if i==16:assert obs is None
  actual.append({"Name":name,"Passed":x["Passed"],"Failure":x["Failure"],"CompiledObservationPresent":obs is not None,"OutcomeSha256":h(p),"ExitSha256":h(folder/"process.exit.json")})
 assert len(list(d.glob("target-*")))==17
 assert completion["Status"]=="ACTUAL_INITIAL354_DIAGNOSTIC_REQUIRES_INDEPENDENT_AUDIT" and completion["ManagedExecuted"]==17 and completion["ParseAll"]==21 and completion["SourceRows"]==3144 and completion["SourceFreezeSha256"]==h(freeze)
 assert completion["Targets"]==[{k:x[k] for k in ("Name","Passed","OutcomeSha256","ExitSha256")} for x in actual]
 passed=sum(x["Passed"] for x in actual);failed=17-passed;assert completion["Passed"]==passed and completion["Failed"]==failed
 for key in ("FullExecuted","ReadinessExecuted","NativeExecuted","MainChanged"):assert completion[key] is False
 for path,digest in initial.items():assert h(Path(path))==digest
 result={"Status":"PASS_INDEPENDENT354_DIAGNOSTIC_ACQUISITION_WITH_SEPARATE_SEMANTIC_OUTCOMES","ManagedExecuted":17,"SemanticPassed":passed,"SemanticFailed":failed,"ParseAll":21,"Outcomes":actual,"ActualAllTargetsPassed":failed==0,"SourceExpectedGenericGaps":[x for x in actual if x["Name"] in ("generic_depth_error","generic_depth_hd")],"FullAccepted":False,"InitialBuiltDllSnapshotVerified":True,"LoadedAssembliesPerProcess":7}
result.update({"SourceFreezeSha256":h(freeze),"SourceRows":3144,"MaterializationReceiptSha256":h(materialized),"ProducerCompletionSha256":h(d/"completion.json"),"OwnedReceipts":evidence,"ReceiptCount":len(evidence),"SourceAuthorDisclosure":"Reviewer authored initial factories/flag/harness and source-only witnesses. Root executed independently; this passive audit validates acquisition/observed outcomes, not independent original parity.","NoSdk":True,"NoNativeExecution":True,"NoMainMutation":True,"NoStatusPromotion":True})
out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(result,indent=2,ensure_ascii=False)+"\n",encoding="utf-8");print(h(out))
