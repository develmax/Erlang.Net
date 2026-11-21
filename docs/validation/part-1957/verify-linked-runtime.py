from pathlib import Path
import hashlib
import json
import os
import shutil
import subprocess
import sys
import time

stage,parent,refs,inputs,boolean,binary,capture=map(Path,sys.argv[1:])
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
wanted={"OTP-PKIX","OTP-PKIX-Relaxed","PKIX1Explicit-2009","ex_gauge","erl_scan","asn1ct_constructed_per"}
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

log=otp("ber-consumer",binary/"native-ber-binary-length-consumer.eval",refs,previous,candidate)
assert "24 cases, baseline 24, candidate 24" in log.read_text(encoding="utf-8")
scheme=data(parent/"component-specs.json")
recipe=Path(scheme["TerminalRecordUpdateRoots"]["Root"])/"gauge-extra.eval"
stub=inputs/"independent-zero-operand-bif-call-1986/stub"
gauge=[]
for role,origin in [("pinned",refs),("linked",candidate)]:
    out=root/f"gauge-{role}.terms"
    otp(f"gauge-{role}",recipe,origin/beam_names["ex_gauge"],stub,out)
    gauge.append(sha(out))
assert gauge[0]==gauge[1]

fixture=boolean/"baseline-fixture/beam_exact_boolean_returns_probe.erl"
runner=root/"probe"
runner.mkdir()
(runner/"Probe.csproj").write_text("""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup>
  <ItemGroup><Reference Include="%s;*.dll" /></ItemGroup>
</Project>""" % (payload/"Erlang.Compiler.dll").as_posix(),encoding="utf-8")
for name in ("Erlang.Runtime.dll","Erlang.Terms.dll","Erlang.Serialization.dll","Erlang.Otp.Behaviours.dll"):
    shutil.copyfile(payload/name,runner/name)
(runner/"Program.cs").write_text("""using Erlang.Compiler;
File.WriteAllBytes(args[1], CodeGeneration.CompileBeamModule(File.ReadAllText(args[0]), Path.GetFileName(args[0])));
""",encoding="utf-8")
probe=root/"beam_exact_boolean_returns_probe.beam"
run("boolean-probe-build",[sdk,"build",runner/"Probe.csproj","-o",runner/"bin","-m:1"])
run("boolean-probe-emit",[sdk,runner/"bin/Probe.dll",fixture,probe])
proof=otp("boolean-predicate",boolean/"native-regression.eval",fixture,probe,root/"boolean-reference.beam")
assert "ten exact predicate" in proof.read_text(encoding="utf-8")
corrected={}
for name,recipe,scenario_count in [("erl_scan","erl-scan-native.eval",6),("asn1ct_constructed_per","asn1-generator-native.eval",5)]:
    rel=beam_names[name]
    outcomes={}
    for role,origin in [("pinned",refs),("baseline",previous),("linked",candidate)]:
        output=root/(name+"-"+role)
        output.mkdir()
        if name=="erl_scan":
            observed=output/"scan.terms"
            args=[origin/rel,observed]
        else:
            observed=output/"native.terms"
            args=[origin/rel,stage/"RetakeProof.asn1",output]
        otp(name+"-"+role,stage/recipe,*args)
        outcomes[role]=sha(observed)
    assert len(set(outcomes.values()))==1,(name,outcomes)
    corrected[name]={"Cases":scenario_count,"Digest":outcomes["linked"]}
receipt={"Compiler":identity["Compiler"],"Runtime":identity["Runtime"],"ActualCases":44,"SyntheticCases":10,
         "ActualImages":{path:sha(candidate/path) for path in sorted(beam_names.values())},
         "Ber":sha(log),"Gauge":gauge[0],"Boolean":sha(proof),"Retaken":corrected}
(root/"receipt.json").write_text(json.dumps(receipt,indent=2)+"\n",encoding="utf-8")
print(json.dumps(receipt))