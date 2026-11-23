from pathlib import Path
import hashlib
import json
import os
import shutil
import subprocess
import sys
import time

stage,parent,refs,inputs,archive,capture=map(Path,sys.argv[1:])
root=stage/"linked-runtime-witnesses"
root.mkdir()
def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()
def data(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))
def wait(path,seconds):
    deadline=time.monotonic()+seconds
    while not path.is_file() or not path.stat().st_size:
        if time.monotonic()>deadline:
            raise TimeoutError(path)
        time.sleep(4)
def run(name,args,**env):
    stdout=root/(name+".stdout.log")
    with stdout.open("wb") as out, (root/(name+".stderr.log")).open("wb") as err:
        subprocess.run([str(arg) for arg in args],cwd=root,stdout=out,stderr=err,check=True,timeout=400,
                       env={**os.environ,"DOTNET_PROCESSOR_COUNT":"1","ERL_CRASH_DUMP":str(root/(name+"-crash.dump")),**env})
    return stdout
def otp(name,recipe,*args):
    return run(name,[refs/"bin/erl.exe","+S","1:1","+A","1","-noshell","-eval",recipe.read_text(encoding="utf-8-sig"),"-extra",*args])
wait(stage/"pilot-receipt.json",2300)
wait(parent/"full-uncached/timing.json",2300)
wanted={"OTP-PKIX","OTP-PKIX-Relaxed","PKIX1Explicit-2009","ex_gauge","ct_default_gl" ,"ex_listCtrl","megaco_monitor"}
found={Path(row["Beam"].replace(chr(92),"/")).stem:row for row in data(stage/"pilot.json")["Modules"]}
assert wanted<=found.keys(),wanted-found.keys()
beam_names={name:found[name]["Beam"].replace(chr(92),"/") for name in sorted(wanted)}
baseline={row["Beam"].replace(chr(92),"/"):row["Status"] for row in data(parent/"full-uncached/otp-corpus.json")["Modules"]}
identity=data(stage/"focused-identity.json")
payload=stage/"corpus-bin"
assert all(sha(payload/f"Erlang.{name}.dll")==identity[name] for name in ("Runtime","Compiler"))
candidate=root/"candidate"
previous=root/"previous"
exact=[beam_names[name] for name in wanted if found[name]["Status"]=="Equal"]
sdk=inputs/"sdk/dotnet.exe"
if exact:
    tool=root/"capture"
    shutil.copytree(capture,tool)
    for name in ("Compiler","Runtime"):
        shutil.copyfile(payload/f"Erlang.{name}.dll",tool/f"Erlang.{name}.dll")
    manifest=tool/"modules.txt"
    manifest.write_text("\n".join(sorted(exact))+"\n",encoding="utf-8")
    temp=tool/"temp"
    temp.mkdir()
    report=tool/"compiled.json"
    run("capture-exact",[sdk,tool/"Erlang.BeamCorpus.dll",refs,report,inputs/"public-key-generated",
        inputs/"pinned-inputs-full","--modules-file",manifest,"--jobs","1"],
        TEMP=str(temp),TMP=str(temp),ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES="1")
    assert data(report)["Counts"]=={"Equal":len(exact)}
for rel in beam_names.values():
    output=candidate/rel
    output.parent.mkdir(parents=True,exist_ok=True)
    source=(tool/"compiled.json.generated"/rel) if rel in exact else stage/"pilot.json.generated"/rel
    if rel in exact:
        assert sha(source)==sha(refs/rel)
    os.link(source,output)
    dest=previous/rel
    dest.parent.mkdir(parents=True,exist_ok=True)
    old=[refs/rel] if baseline[rel]=="Equal" else list((parent/"full-uncached").glob("shard-*/otp-corpus.json.generated/"+rel))
    assert len(old)==1,(rel,old)
    os.link(old[0],dest)

components=data(stage/"components.json")
group=lambda name:Path(components[name]["Root"])
verified={}
def three(name,scope,recipe,count):
    rel=beam_names[scope]
    log=otp(name,recipe,refs/rel,previous/rel,candidate/rel)
    verified[name]={"Cases":count,"Evidence":sha(log)}
def roles(name,module,recipe,extra,count):
    values={}
    for role,origin in [("pinned",refs),("before",previous),("linked",candidate)]:
        out=root/(name+"-"+role+".terms")
        otp(name+"-"+role,recipe,origin/beam_names[module],*extra,out)
        values[role]=sha(out)
    assert len(set(values.values()))==1,(name,values)
    verified[name]={"Cases":count,"Evidence":values["linked"]}

ber=inputs/"asn-named-bits-replay-1931/ber-directive-seed-effect-1959"
log=otp("directive-seed",ber/"native-ber-directive-seed-effect.eval",refs,previous,candidate)
assert "12 cases, baseline 12, candidate 12" in log.read_text(encoding="utf-8")
verified["directive-seed"]={"Cases":12,"Evidence":sha(log)}
integer=inputs/"asn-named-bits-replay-1931/closed-recursive-integer-1958-recovered"
retry=otp("recursive-entry-retry",integer/"native-ct-retry.eval",refs,previous,candidate,beam_names["ct_default_gl"])
assert retry.read_text(encoding="utf-8").count("[clean_start,clean_stop,old_owner_killed,retry_start,retry_stop]")==3
verified["recursive-entry-retry"]={"Cases":5,"Evidence":sha(retry)}

mixed=inputs/"branched-map-update-lifetime-1957-aliases-early"
roles("mixed-entry","ct_default_gl",mixed/"native-actual.eval",[],5)
gauge=inputs/"mixed-selected-cold-layout-2011"
for kind,count in [("event",11),("toggle",3),("undetermined",3)]:
    roles("gauge-"+kind,"ex_gauge",gauge/f"native-{kind}.eval",[gauge/"native-stubs"],count)
wx=inputs/"pending-wx-gauge-initializers-2002"
roles("gauge-initialization","ex_gauge",wx/"init-native.eval",[wx/"native"],2)
list_ctrl=inputs/"dying-slot-call-result-copy-2004"
roles("list-initialization","ex_listCtrl",list_ctrl/"list-native.eval",[list_ctrl/"list-wx"],1)
verified["list-initialization"]["OrderedInsertions"]=51
three("protected-tuples","megaco_monitor",group("ProtectedTupleBoundary")/"native-monitor.eval",9)
three("relocated-error-lines","megaco_monitor",group("PhysicalLineFirstUse")/"native-megaco.eval",2)

receipt={"Compiler":identity["Compiler"],"Runtime":identity["Runtime"],
         "ActualCases":sum(row["Cases"] for row in verified.values()),"SyntheticCases":0,
         "ActualImages":{path:sha(candidate/path) for path in sorted(beam_names.values())},"Observations":verified}
(root/"receipt.json").write_text(json.dumps(receipt,indent=2)+"\n",encoding="utf-8")
print(json.dumps(receipt))

