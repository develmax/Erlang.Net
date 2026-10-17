"""Passive initial354 acquisition/semantic split. No SDK or downloaded-code execution."""
from pathlib import Path
import argparse,hashlib,json,re
R=Path(__file__).resolve().parents[2];P=Path(__file__).parent
h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
rd=lambda p:json.loads(p.read_bytes())
ap=argparse.ArgumentParser();ap.add_argument("--output",required=True);a=ap.parse_args();out=Path(a.output).resolve();assert out.is_relative_to(R/"artifacts") and not out.exists()
F=R/"artifacts/size-bif354-focus-execution-proposal14";freeze=F/"source-freeze.json";assert h(freeze)=="F804F2FF04F8ACCA2465270DA0155448B7DA6FD1A3ED6C6C9708C7FA2AF71B1A"
for row in rd(freeze)["Files"]:
 q=F/row["Path"];assert q.stat().st_size==row["Bytes"] and h(q)==row["Sha256"]
v=rd(F/"execution-inputs.json");c=R/v["Candidate"];d=R/"artifacts/size-bif354-focused-actual8"
materialized=R/"artifacts/root-size354-materialized14.json";assert h(materialized)=="0446F7C9B95E4965C9B867AFF6051918EDDF11FB6D7449A13C0E9EB30D9C26F0"
assert rd(materialized)["ExecutionFreezeSha256"]==h(freeze)
for row in v["Rows"]:
 q=c/row["Path"];assert q.stat().st_size==row["Bytes"] and h(q)==row["Sha256"],row["Path"]
assert len(v["Rows"])==3248
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
 if completion["Status"]=="PARSE_FAILED_BEFORE_GUEST":
  receipt(d,"parse",{1});parsed=rd(d/"parse.json");assert parsed["Passed"] is False and parsed["Failure"] is not None;loaded(parsed)
  assert completion["ManagedExecuted"]==0 and not list(d.glob("target-*"))
  assert parsed["CurrentSource"] in {Path(x["Path"]).stem for x in v["Rows"] if x["Kind"]=="PrivateParseAllSource"}
  result={"Status":"PASS_INDEPENDENT354_PARSE_FAILURE_ACQUISITION_NO_GUEST_EXECUTION","ManagedExecuted":0,"SemanticPassed":0,"SemanticFailed":0,"FullAccepted":False,"CurrentSource":parsed["CurrentSource"],"ParseFailure":parsed["Failure"]}
 else:
  receipt(d,"parse",{0});parsed=rd(d/"parse.json");assert parsed["Passed"] is True and parsed["Failure"] is None and parsed["Name"]=="--parse";loaded(parsed)
  expected={Path(x["Path"]).stem:x["Sha256"] for x in v["Rows"] if x["Kind"]=="PrivateParseAllSource"};assert len(expected)==36 and {x["Name"]:x["Sha256"] for x in parsed["Sources"]}==expected
  # Decode only the source-derived expected values; raw observations are retained unchanged.
  def decode_expected(value):
   data=bytes.fromhex(value);assert data[0]==131;at=1
   def take(n):
    nonlocal at
    assert at+n<=len(data);b=data[at:at+n];at+=n;return b
   def term():
    tag=take(1)[0]
    if tag==97:return take(1)[0]
    if tag==98:return int.from_bytes(take(4),'big',signed=True)
    if tag in (100,115,118,119):
     n=int.from_bytes(take(1 if tag in (115,119) else 2),'big');return take(n).decode('latin1' if tag in (100,115) else 'utf-8')
    if tag==106:return []
    if tag==107:return list(take(int.from_bytes(take(2),'big')))
    if tag in (104,105):return tuple(term() for _ in range(int.from_bytes(take(1 if tag==104 else 4),'big')))
    if tag==108:
     values=[term() for _ in range(int.from_bytes(take(4),'big'))];tail=term();return values if tail==[] else {'Improper':(values,tail)}
    raise AssertionError(('Unexpected expected ETF tag',tag))
   result=term();assert at==len(data);return result
  def expected_value(name):
   if name=='rpc-errors':return ('ok','ok')
   prefix='erlang-apply-forwarded-frame-'
   if not name.startswith(prefix):return ('ok','done')
   suffix=name[len(prefix):]
   def failure(reason,module,function,args):return ('error',reason,(module,function,args))
   bad=failure('badarg','erlang','hd',['not_a_list'])
   undefined=failure('undef','missing_apply_frame_module','absent',[17])
   if suffix=='badarg-direct-and-dynamic':return ('ok',(bad,bad))
   if suffix=='undef-direct-and-dynamic':return ('ok',(undefined,undefined))
   if suffix=='invalid-module-keeps-apply-frame':return ('ok',failure('badarg','erlang','apply',[0,'hd',[]]))
   if suffix=='invalid-function-keeps-apply-frame':return ('ok',failure('badarg','erlang','apply',['erlang',0,[]]))
   if suffix=='improper-arguments-keeps-apply-frame':return ('ok',failure('badarg','erlang','apply',['erlang','hd',{'Improper':([1],2)}]))
   assert suffix=='explicit-raise-preserves-all-classes-and-empty-stack'
   trace=[('synthetic_module','synthetic_function',[17],[])]
   return ('ok',tuple((kind,'reason',stack) for stack in (trace,[]) for kind in ('error','throw','exit')))
  targets=v["Cases"]+["runtime/backtrace-depth-independent-runtimes","runtime/common-capture-identity-rollback-async"]+["handler/"+name for name in ("RunState","RunFrames","RunNestedFrames","RunSourceLease","RunIsolation")]+["runtime/common-capture-null-fallback-prior-host","runtime/configured-load-nif-common-capture","runtime/raw-selected-handler-observation","runtime/undefined-lambda-publication-lifecycle","runtime/trusted-preloaded-reinstall-identity","runtime/common-selected-lambda-capture"];assert len(targets)==len(set(targets))==44
  actual=[]
  for i,name in enumerate(targets):
   folder=d/("target-"+str(i).zfill(2));process=receipt(folder,"process",{0,1});p=folder/"outcome.json";x=rd(p);assert x["Name"]==name and isinstance(x["Passed"],bool) and x["Passed"]==(process["ExitCode"]==0);loaded(x)
   assert x["NativeExecuted"] is False and x["FullValidationExecuted"] is False and x["ReadinessExecuted"] is False
   marker=("PASS " if x["Passed"] else "FAIL ")+name;stdout=(folder/"process.stdout.log").read_text(encoding="utf-8-sig").splitlines()
   if name=="runtime/raw-selected-handler-observation" and x["Passed"]:
    import base64
    assert len(stdout)==2 and stdout[0].startswith("RAW_SELECTED_HANDLER_ETF ") and stdout[1]==marker
    raw=base64.b64decode(stdout[0].split(" ",1)[1],validate=True);assert raw[:1]==bytes([131]) and (folder/"selected-handler.etf").read_bytes()==raw
   else:assert stdout==[marker]
   assert (x["Failure"] is None)==x["Passed"]
   obs=x["Observation"]
   if i<len(v["Cases"]) and x["Passed"]:assert obs is not None
   if i<len(v["Cases"]) and obs is not None:
    assert obs["Name"]==name and obs["SourceSha256"]==expected[name] and re.fullmatch("[0-9A-F]{64}",obs["CSharpSha256"])
    assert obs["CallerContext"]==0 and obs["CompilerPolicy"]==0
    assert decode_expected(obs["ExpectedEtfHex"])==expected_value(name)
    if x["Passed"]:assert obs["ActualEtfHex"]==obs["ExpectedEtfHex"] and obs["StandardOutputHex"]==obs["StandardErrorHex"]==""
   if i>=len(v["Cases"]):assert obs is None
   actual.append({"Name":name,"Passed":x["Passed"],"Failure":x["Failure"],"CompiledObservationPresent":obs is not None,"OutcomeSha256":h(p),"ExitSha256":h(folder/"process.exit.json")})
  assert len(list(d.glob("target-*")))==44
  assert completion["Status"]=="ACTUAL_INITIAL354_DIAGNOSTIC_REQUIRES_INDEPENDENT_AUDIT" and completion["ManagedExecuted"]==44 and completion["ParseAll"]==36 and completion["SourceRows"]==3248 and completion["SourceFreezeSha256"]==h(freeze)
  assert completion["Targets"]==[{k:x[k] for k in ("Name","Passed","OutcomeSha256","ExitSha256")} for x in actual]
  passed=sum(x["Passed"] for x in actual);failed=44-passed;assert completion["Passed"]==passed and completion["Failed"]==failed
  for key in ("FullExecuted","ReadinessExecuted","NativeExecuted","MainChanged"):assert completion[key] is False
  for path,digest in initial.items():assert h(Path(path))==digest
  result={"Status":"PASS_INDEPENDENT354_DIAGNOSTIC_ACQUISITION_WITH_SEPARATE_SEMANTIC_OUTCOMES","ManagedExecuted":44,"SemanticPassed":passed,"SemanticFailed":failed,"ParseAll":36,"Outcomes":actual,"ActualAllTargetsPassed":failed==0,"PriorGenericRedCurrentOutcomes":[x for x in actual if x["Name"] in ("generic_depth_error","generic_depth_hd")],"FullAccepted":False,"InitialBuiltDllSnapshotVerified":True,"LoadedAssembliesPerProcess":7}
if result["ManagedExecuted"]==44:
 before=R/"artifacts/size-bif354-selected-handler-actual1/selected-handler.etf";assert h(before)=="703BD5337A6545A091C3961149D407548C9A89AC807AFD7E7F78147EDB35B33A"
 rawpath=d/"target-40/selected-handler.etf";assert result["Outcomes"][40]["Name"]=="runtime/raw-selected-handler-observation"
 result["RawHandlerObservation"]={"BeforeSha256":h(before),"AfterSha256":h(rawpath),"RawBytesEqual":before.read_bytes()==rawpath.read_bytes(),"NoNormalization":True,"SemanticExpectedTraceInjected":False,"Scope":"Raw provenance/frame observation only; original/native expected values not invented"}
result.update({"SourceFreezeSha256":h(freeze),"SourceRows":3248,"MaterializationReceiptSha256":h(materialized),"ProducerCompletionSha256":h(d/"completion.json"),"OwnedReceipts":evidence,"ReceiptCount":len(evidence),"SourceAuthorDisclosure":"Reviewer authored initial factories/flag/harness and source-only witnesses. Root executed independently; this passive audit validates acquisition/observed outcomes, not independent original parity.","NoSdk":True,"NoNativeExecution":True,"NoMainMutation":True,"NoStatusPromotion":True})
out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(result,indent=2,ensure_ascii=False)+"\n",encoding="utf-8");print(h(out))
