from pathlib import Path
import hashlib
import json
import os
import shutil
import subprocess
import sys

stage=Path(sys.argv[1]).resolve()
inputs=stage.parent
release=inputs.parent/"library376-official-compiler/installed"
assert (release/"bin/erl.exe").is_file(),release
sdk=inputs/"sdk/dotnet.exe"
frozen=stage/"frozen-payload"
root=inputs/("sn-"+hashlib.sha256(stage.name.encode("utf-8")).hexdigest()[:10])
root.mkdir()
logs=root/"logs"
logs.mkdir()
retained_logs=stage/"successor-native-logs"

def data(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def save(path,value):
    path.write_text(json.dumps(value,indent=2)+"\n",encoding="utf-8",newline="\n")

def run(name,args):
    output=logs/(name+".stdout.log")
    errors=logs/(name+".stderr.log")
    env={**os.environ,"DOTNET_ADD_GLOBAL_TOOLS_TO_PATH":"0","DOTNET_PROCESSOR_COUNT":"1",
         "TEMP":str(root),"TMP":str(root),"ERL_CRASH_DUMP":str(logs/(name+".crash.dump"))}
    with output.open("wb") as out,errors.open("wb") as err:
        result=subprocess.run([str(a) for a in args],cwd=root,env=env,stdout=out,stderr=err,timeout=180)
    if result.returncode!=0:
        raise RuntimeError((name,result.returncode,output.as_posix(),errors.as_posix()))

identity=data(stage/"source-freeze.json")
for name in ("Compiler","Runtime"):
    assert sha(frozen/f"Erlang.{name}.dll")==identity[name],name
source_plan=stage/"successor-native-inputs"
for name,digest in data(source_plan/"source-identities.json").items():
    assert sha(source_plan/name)==digest,name
shutil.copytree(source_plan/"state-guard-construction",root/"plan")
shutil.copytree(source_plan/"exclusive-fallback",root/"exclusive-fallback")
plan=root/"plan"
source_cases=plan/"fixtures/cases.json"
rows=data(source_cases)
excluded=[row for row in rows if row["profile"]=="no_ssa_opt"]
assert len(excluded)==1
profiles=[row for row in rows if row not in excluded]
selected=plan/"fixtures/selected-cases.json"
save(selected,profiles)
erl=release/"bin/erl.exe"
compile_eval=(plan/"pinned-compile.eval").read_text(encoding="utf-8-sig")
for row in profiles:
    rel=Path(row["family"])/row["profile"]
    source=plan/"fixtures"/row["input"]
    assert sha(source)==row["source_sha256"]
    pinned=root/"pinned"/rel
    linked=root/"candidate"/rel
    pinned.mkdir(parents=True)
    linked.mkdir(parents=True)
    stem=row["family"]+"-"+row["profile"]
    run(stem+"-compile-pinned",[erl,"+S","1:1","+A","1","-noshell","-eval",compile_eval,"-extra",source,pinned])
    run(stem+"-compile-candidate",[sdk,frozen/"Erlang.Tool.dll","compile-beam",source,linked/(row["module"]+".beam")])
run("state-guard-construction",["pwsh","-NoProfile","-File",plan/"compare.ps1",
    erl,selected,root/"pinned",root/"candidate",root/"out"])
observed=data(root/"out/receipt.json")
assert len(observed)==len(profiles)==6 and all(row["BothLoaded"] and row["NativeTermsEqual"] and row["RetainedOracleMatches"] for row in observed)

projection=root/"exclusive-fallback"
projection_source=projection/"checked_fallback_probe.erl"
pin=projection/"pinned"
pin.mkdir()
new=projection/"candidate"
new.mkdir()
run("projection-compile-pinned",[erl,"+S","1:1","+A","1","-noshell","-eval",compile_eval,"-extra",projection_source,pin])
run("projection-compile-candidate",[sdk,frozen/"Erlang.Tool.dll","compile-beam",projection_source,new/"checked_fallback_probe.beam"])
run("projection-native",["pwsh","-NoProfile","-File",projection/"run-native-comparison.ps1","-Erl",erl,
    "-PinnedBeam",pin/"checked_fallback_probe.beam","-CandidateBeam",new/"checked_fallback_probe.beam",
    "-OutputDirectory",projection/"out"])
projection_receipt=data(projection/"out/native-receipt.json")
assert projection_receipt["EqualObservations"]
retained_logs.mkdir()
for group in (logs,root/"out",projection/"out"):
    for path in sorted(group.rglob("*")):
        if path.is_file() and path.suffix in {".stdout",".stderr",".log",".json",".etf"}:
            dest=retained_logs/("_".join((group.parent.name,group.name,*path.relative_to(group).parts)))
            assert not dest.exists()
            shutil.copyfile(path,dest)
save(stage/"successor-native-receipt.json",{"PassedProfiles":len(observed)+1,"Compiler":identity["Compiler"],
    "Runtime":identity["Runtime"],"StateGuardConstruction":observed,"ExclusiveFallthrough":projection_receipt,
    "RawRoot":root.as_posix(),"LogHashes":{path.name:sha(path) for path in sorted(retained_logs.iterdir())},
    "DisabledSsaProfile":"The permanent managed test covers the explicit no_ssa_opt compile profile. The fixture CLI rejects that source attribute."})
print("Seven supplementary pinned native profiles passed.")
