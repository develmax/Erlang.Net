from pathlib import Path
import argparse,hashlib,json
R=Path(__file__).resolve().parents[2];h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();rd=lambda p:json.loads(p.read_bytes())
a=argparse.ArgumentParser();a.add_argument("--output",required=True);o=Path(a.parse_args().output).resolve();assert o.is_relative_to(R/"artifacts") and not o.exists()
f=R/"artifacts/size-bif354-focus-execution-proposal/source-freeze.json";assert h(f)=="1898567EC6C2E1D9E1DEF98F6EFB0A2383E544B363974E0D01A56BA07B03FD54"
v=rd(f.parent/"execution-inputs.json");c=R/v["Candidate"];d=R/"artifacts/size-bif354-focused-actual1"
for row in v["Rows"]:
 q=c/row["Path"];assert q.stat().st_size==row["Bytes"] and h(q)==row["Sha256"]
assert len(v["Rows"])==3144
for name,code in (("build",0),("parse",1)):
 x=rd(d/(name+".exit.json"));live=rd(d/(name+".live.json"));assert x["Settled"] and not x["TimedOut"] and x["Failures"]==[] and x["ExitCode"]==code and live["Pid"]==x["Pid"]
 assert x["StdoutSha256"]==h(d/(name+".stdout.log")) and x["StderrSha256"]==h(d/(name+".stderr.log"))
x=rd(d/"parse.json");assert x["Passed"] is False and x["Sources"]==[] and "Invalid or unsupported pattern" in x["Failure"]
assert not list(d.glob("target-*")) and rd(d/"completion.json")["Status"]=="PARSE_FAILED_BEFORE_GUEST"
initial=rd(d/"initial-built-dlls.json")["Files"]
for path,digest in initial.items():assert h(Path(path))==digest
assert len(x["LoadedAssemblies"])==7
for row in x["LoadedAssemblies"]:assert initial[str(Path(row["Path"]))]==row["Sha256"]==h(Path(row["Path"]))
first=sorted(row["Path"] for row in v["Rows"] if row["Kind"]=="PrivateParseAllSource")[0];assert first.endswith("backtrace_depth_flag.erl")
assert "[{error_info,#{module=>erl_erts_errors}}]" in (c/first).read_text(encoding="utf-8")
files=[dict(Path=q.relative_to(R).as_posix(),Sha256=h(q)) for q in sorted(d.iterdir()) if q.is_file()]
result={"Status":"PASS_INDEPENDENT354_INITIAL_PARSE_FAILURE_RETENTION_NO_GUEST","BuildExit0":True,"ParseExit1":True,"ManagedExecuted":0,"SemanticFailureCountNotMeasured":True,"FirstAlphabeticalSource":first,"CauseScope":"Source-invalid map association operator in stack pattern, confirmed by source and parser refusal; no runtime generic capture failure observed.","SourceRows3144Unchanged":True,"InitialBuiltAndLoaded7DllsUnchanged":True,"RawFiles":files,"NoSdk":True,"NoMainMutation":True,"NoNative":True}
o.parent.mkdir(parents=True,exist_ok=True);o.write_text(json.dumps(result,indent=2)+"\n",encoding="utf-8");print(h(o))
