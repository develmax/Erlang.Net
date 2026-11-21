from pathlib import Path
import hashlib,json,shutil,sys
stage,root=map(Path,sys.argv[1:])
def read(path): return path.read_text(encoding="utf-8-sig")
def data(path): return json.loads(read(path))
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest().upper()
def save(path,value): path.write_text(json.dumps(value,indent=2)+"\n",encoding="utf-8",newline="\n")
source=data(stage/"source-freeze.json")
proof=data(stage/"linked-runtime-witnesses/receipt.json")
assert proof["Compiler"].lower()==source["Compiler"] and proof["Runtime"].lower()==source["Runtime"]
assert proof["ActualCases"]==284 and proof["SyntheticCases"]==323
assert all(read(stage/name).strip()=="1" for name in ("linked-runtime.exit","durable.exit","finalize-receipts.exit"))
assert not (stage/"full-launch.json").exists()
assert all(term in read(stage/"successor-native.stderr.log") for term in ("FileNotFoundError", "exclusive-fallback", "receipt.json"))
assert root.name=="sn-"+hashlib.sha256(stage.name.encode("utf-8")).hexdigest()[:10]
ordinary=data(root/"out/receipt.json")
manifest=data(root/"plan/fixtures/selected-cases.json")
assert len(ordinary)==len(manifest)==6
for reported,expected in zip(ordinary,manifest):
    family,profile=expected["family"],expected["profile"]
    assert reported["Family"]==family and reported["Profile"]==profile
    assert reported["BothLoaded"] and reported["NativeTermsEqual"] and reported["RetainedOracleMatches"]
    assert sha(root/"plan/fixtures"/expected["input"]).lower()==expected["source_sha256"]
    for result in reported["Results"]:
        assert result["ExitCode"]==0
        image=Path(result["Beam"])
        assert image.is_relative_to(root.resolve()) and sha(image)==result["BeamSha256"]
        artifact=root/"out"/family/profile/(result["Side"]+".etf")
        assert sha(artifact)==result["NativeTermSha256"]
    hashes={result["NativeTermSha256"] for result in reported["Results"]}
    assert len(hashes)==1
projection=root/"exclusive-fallback"
projected=data(projection/"out/native-receipt.json")
assert projected["EqualObservations"]
for side,result in projected["Arms"].items():
    img=Path(result["Image"])
    assert result["ExitCode"]==0 and img.is_relative_to(root.resolve()) and sha(img)==result["ImageSha256"]
    assert sha(projection/"out"/(side+".stdout.log"))==result["StdoutSha256"]
    assert sha(projection/"out"/(side+".stderr.log"))==result["StderrSha256"]
assert len({result["StdoutSha256"] for result in projected["Arms"].values()})==1
retained=stage/"successor-native-logs"
retained.mkdir()
for group in (root/"logs",root/"out",projection/"out"):
    for path in sorted(group.rglob("*")):
        if path.is_file() and path.suffix in {".stdout",".stderr",".log",".json",".etf"}:
            dest=retained/("_".join((group.parent.name,group.name,*path.relative_to(group).parts)))
            assert not dest.exists()
            shutil.copyfile(path,dest)
receipt=stage/"successor-native-receipt.json"
assert not receipt.exists()
save(receipt,{"PassedProfiles":7,"Compiler":source["Compiler"],"Runtime":source["Runtime"],
             "StateGuardConstruction":ordinary,"ExclusiveFallthrough":projected,"RawRoot":root.as_posix(),
             "LogHashes":{p.name:sha(p) for p in sorted(retained.iterdir())},
             "DisabledSsaProfile":"Covered in the permanent managed test through its explicit fixture options.",
             "Recovery":"All seven executed successfully. A receipt filename mismatch prevented automatic aggregation."})
failure=stage/"native-receipt-name-recovery"
failure.mkdir()
old=stage/"run-successor-native.py"
shutil.copyfile(old,failure/"original-run-successor-native.py")
writing=read(old)
needle='projection_receipt=data(projection/"out/receipt.json")'
assert writing.count(needle)==1
old.write_text(writing.replace(needle,'projection_receipt=data(projection/"out/native-receipt.json")'),
               encoding="utf-8",newline="\n")
for name in ("linked-runtime.exit","durable.exit","finalize-receipts.exit","full-orchestration.stderr.log",
             "full-orchestration.stdout.log","successor-native.stderr.log","successor-native.stdout.log","durable.stderr.log","durable.stdout.log"):
    original=stage/name
    assert original.is_file() and original.resolve().parent==stage.resolve()
    shutil.move(original,failure/name)
for name in ("successor-native.stderr.log","successor-native.stdout.log"):
    shutil.copyfile(failure/name,stage/name)
save(failure/"recovery-receipt.json",{"UnchangedFrozenCompiler":source["Compiler"],"NativeProfileDigest":sha(receipt),
     "ChangedScriptDigest":sha(old),"OriginalFiles":{p.name:sha(p) for p in sorted(failure.iterdir()) if p.is_file()},
     "NativeExecutionsReused":7,"CompilerCorpusReused":False})
archive=stage/"record-and-archive-1976.py"
text=read(archive)
anchor='    "scope-plan.json",'
assert text.count(anchor)==1
paths=[p.relative_to(stage).as_posix() for p in sorted(failure.iterdir()) if p.is_file()]
text=text.replace(anchor,"    "+",\n    ".join(map(json.dumps,paths))+",\n"+anchor)
text=text.replace('".exit",".diff",".erl"}','".exit",".diff",".erl",".etf"}')
compile(text,str(archive),"exec")
archive.write_text(text,encoding="utf-8",newline="\n")
(stage/"linked-runtime.exit").write_text("0\n",encoding="utf-8")
print("Seven native profiles verified directly from raw receipts. Main gate ready; existing compiler payload unchanged.")
