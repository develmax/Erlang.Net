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
wanted={"OTP-PKIX","OTP-PKIX-Relaxed","PKIX1Explicit-2009","ex_gauge","ct_default_gl","inet_tcp","inet6_tcp"}
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
observed={}
for label,recipe_name in [("BerIndefiniteContinuation","native-ber-indefinite-continuation.eval"),
                          ("BerBinarySplit","native-ber-binary-split.eval")]:
    log=otp(label,group(label)/recipe_name,refs,previous,candidate)
    observed[label]=sha(log)

scheme=data(archive/"component-specs.json")
gauge_recipe=Path(scheme["TerminalRecordUpdateRoots"]["Root"])/"gauge-extra.eval"
stub=inputs/"independent-zero-operand-bif-call-1986/stub"
for title, module, recipe, cases in [
    ("Gauge","ex_gauge",gauge_recipe,9),
    ("EntryTupleLiveness","ct_default_gl",group("EntryTupleLiveness")/"native-actual.eval",5),
    ("IPv4","inet_tcp",stage/"tcp-loopback-native.eval",2),
    ("IPv6","inet6_tcp",stage/"tcp-loopback-native.eval",2),
]:
    results={}
    relative=beam_names[module]
    for role,origin in [("pin",refs),("before",previous),("linked",candidate)]:
        output=root/(title+"-"+role+".terms")
        if title=="Gauge":
            args=[origin/relative,stub,output]
        elif title in {"IPv4","IPv6"}:
            args=[module,origin/relative,output]
        else:
            args=[origin/relative,output]
        otp(title+"-"+role,recipe,*args)
        results[role]=sha(output)
    assert len(set(results.values()))==1,(title,results)
    observed[title]={"Output":results["linked"],"Cases":cases}

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
run("probe-build",[sdk,"build",runner/"Probe.csproj","-o",runner/"bin","-m:1"])

for title,source,extra,cases in [
    ("SharedGuardedLocalTails","beam_shared_guarded_local_tails_probe.erl",[],12),
    ("CaseSelectorTupleReuse","beam_selector_alias_probe.erl",[root/"selector-no-core-alias.beam"],10),
]:
    ref=group(title)
    path=ref/source
    generated=root/(path.stem+".beam")
    run(title+"-emit",[sdk,runner/"bin/Probe.dll",path,generated])
    log=otp(title,ref/"native-regression.eval",path,generated,root/(title+"-pinned.beam"),*extra)
    observed[title]={"Log":sha(log),"Cases":cases}

receipt={"Compiler":identity["Compiler"],"Runtime":identity["Runtime"],"ActualCases":66,"SyntheticCases":22,
         "ActualImages":{path:sha(candidate/path) for path in sorted(beam_names.values())},"Observations":observed}
(root/"receipt.json").write_text(json.dumps(receipt,indent=2)+"\n",encoding="utf-8")
print(json.dumps(receipt))