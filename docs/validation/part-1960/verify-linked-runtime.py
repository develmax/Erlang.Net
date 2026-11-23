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
root.mkdir(exist_ok=False)
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
wanted={"OTP-PKIX","OTP-PKIX-Relaxed","PKIX1Explicit-2009","ex_gauge","ct_default_gl" ,"ex_listCtrl"}
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
ber=group("FramelessFramedCallInput")
ber_log=otp("directive-seed",ber/"native-ber-directive-seed-effect.eval",refs,previous,candidate)
assert "12 cases, baseline 12, candidate 12" in ber_log.read_text(encoding="utf-8")
verified["directive-seed"]={"Cases":12,"Evidence":sha(ber_log)}

recipe=group("DecrementInteger")/"native-ct-retry.eval"
retry=otp("recursive-entry-retry",recipe,refs,previous,candidate,beam_names["ct_default_gl"])
assert retry.read_text(encoding="utf-8").count("[clean_start,clean_stop,old_owner_killed,retry_start,retry_stop]")==3
verified["recursive-entry-retry"]={"Cases":5,"Evidence":sha(retry)}

scheme=data(archive/"component-specs.json")
gauge=Path(scheme["TerminalRecordUpdateRoots"]["Root"])/"gauge-extra.eval"
stubs=inputs/"independent-zero-operand-bif-call-1986/stub"
wx=inputs/"pending-wx-gauge-initializers-2002"
mixed=inputs/"branched-map-update-lifetime-1957-aliases-early"
list_ctrl=group("DyingSlotCallResult")
suites=[
    ("gauge-callback","ex_gauge",gauge,[stubs],9),
    ("gauge-initialization","ex_gauge",wx/"init-native.eval",[wx/"native"],2),
    ("mixed-entry","ct_default_gl",mixed/"native-actual.eval",[],5),
    ("list-initialization","ex_listCtrl",list_ctrl/"list-native.eval",[list_ctrl/"list-wx"],1),
]
for name,module,recipe,args,cases in suites:
    values={}
    for role,origin in [("pinned",refs),("before",previous),("linked",candidate)]:
        out=root/(name+"-"+role+".terms")
        otp(name+"-"+role,recipe,origin/beam_names[module],*args,out)
        values[role]=sha(out)
    assert len(set(values.values()))==1,(name,values)
    verified[name]={"Cases":cases,"Evidence":values["linked"]}
verified["list-initialization"]["OrderedInsertions"]=51

receipt={"Compiler":identity["Compiler"],"Runtime":identity["Runtime"],
         "ActualCases":sum(row["Cases"] for row in verified.values()),"SyntheticCases":0,
         "ActualImages":{path:sha(candidate/path) for path in sorted(beam_names.values())},"Observations":verified}
(root/"receipt.json").write_text(json.dumps(receipt,indent=2)+"\n",encoding="utf-8")
print(json.dumps(receipt))

