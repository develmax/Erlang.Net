from pathlib import Path
import hashlib,json,re,shutil,subprocess,sys
stage,parent,oldbase,templates,live=map(Path,sys.argv[1:])
def read(p): return json.loads(p.read_text(encoding="utf-8-sig"))
def save(p,value): p.write_text(json.dumps(value,indent=2)+"\n",encoding="utf-8")
def txt(p,s): p.write_text(s,encoding="utf-8",newline="\n")
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
freeze=read(parent/"source-freeze.json")
launch=read(parent/"full-launch.json")
assert launch["SourceZipSha256"]==sha(parent/"source.zip")==freeze["Source"]
assert launch["CompilerSha256"]==freeze["Compiler"]
assert (parent/"full-uncached/shard-0/modules.txt").is_file()
base_cases=set((parent/"focused-test-names.txt").read_text(encoding="utf-8-sig").splitlines())
anchor_modules={
 "ct_default_gl","ct_logs","ex_gauge","ex_listCtrl","unix_telnet","megaco_monitor",
 "OTP-PKIX","OTP-PKIX-Relaxed","PKIX1Explicit-2009","mnesia_backup","mnesia_dumper","megaco_digit_map","snmpa_mib_storage_ets","ssh_dbg","asn1ct_gen_ber_bin_v2",
}
baseline_rows=read(oldbase/"full-uncached/otp-corpus.json")["Modules"]
modules={row["Beam"].replace(chr(92),"/") for row in baseline_rows
    if Path(row["Beam"].replace(chr(92),"/")).stem in anchor_modules}
assert len(modules)==len(anchor_modules)
parent_pilot={row["Beam"].replace(chr(92),"/"):row["Status"] for row in read(parent/"pilot.json")["Modules"]}
added_controls=set()
for path,status in parent_pilot.items():
    app=path.split("/")[1]
    if status=="Equal" and path not in modules and app not in added_controls:
        modules.add(path)
        added_controls.add(app)
        if len(added_controls)==8:
            break
assert len(added_controls)==8
reg,sync,manifest,newcases,existing_cases=[],[],[],[],[]
for tag,spec in read(stage/"components.json").items():
    source=Path(spec["Root"])
    files=[source/f for f in spec["Patches"]]
    assert len(files)==len(spec["SHA"])==2 and [sha(p) for p in files]==spec["SHA"],tag
    evidence=[source/f for f in spec["Evidence"]]
    assert all(f.is_file() for f in evidence),tag
    report=source/spec["Report"]
    modules.update(row["Beam"].replace(chr(92),"/") for row in read(report)["Modules"])
    for older_report in spec.get("AdditionalMeasuredScope",[]):
        modules.update(row["Beam"].replace(chr(92),"/") for row in read(source/older_report)["Modules"])
    patch=files[1].read_text(encoding="utf-8-sig")
    added="\n".join(s[1:] for s in patch.splitlines() if s.startswith("+") and not s.startswith("+++"))
    names=re.findall(r'^\s*internal const string \w*TestName = "([^"]+)";',added,re.MULTILINE)
    existing=spec.get("ExistingCase")
    if existing:
        assert not names and existing not in existing_cases,tag
        assert any(existing in path.read_text(encoding="utf-8") for path in (live/"tests/Erlang.Tests").rglob("BeamDirectTupleStoreChecks.cs")),tag
        existing_cases.append(existing)
    else:
        assert names and len(names)==len(set(names)) and not set(names).intersection(base_cases|set(newcases)),(tag,names)
        registrations=re.findall(r'Test\(\s*(Erlang\.Tests\.[\w.]+\.\w*TestName),\s*(Erlang\.Tests\.[\w.]+\.\w+)\s*\);',added)
        assert len(registrations)==len(names),(tag,registrations)
        newcases+=names
        for name, callback in registrations:
            assert name.rsplit(".",1)[0]==callback.rsplit(".",1)[0],(name,callback)
            reg.append(f"Test({name}, {callback});")
            method=callback.rsplit(".",1)[1]
            if f"internal static void {method}(" in added: sync.append(callback)
    manifest.append({"Agent":tag,"Report":report.as_posix(),"PatchDigests":{p.as_posix():sha(p) for p in files},
                     "Evidence":[p.as_posix() for p in evidence]})
known={row["Beam"].replace(chr(92),"/") for row in read(oldbase/"full-uncached/otp-corpus.json")["Modules"]}
assert modules<=known and len(known)==1366
save(stage/"agent-intake.json",manifest)
save(stage/"existing-regression-cases.json",existing_cases)
save(stage/"synchronous-callbacks.json",sync)
save(stage/"scope-plan.json",{"Modules":len(modules),"NewCases":len(newcases),"Source":freeze["Source"],"Groups":len(manifest),
  "Coverage":"Scope union for measured compiler components, linked runtime witnesses and previously exact cross-application controls"})
txt(stage/"pilot-modules.txt","\n".join(sorted(modules,key=str.casefold))+"\n")
txt(stage/"harness-registrations.txt","\n".join(reg)+"\n")
internal=[
 "bind-source-closure.py","cached-normalized-delta.eval","check-disassembler-role.eval","complete-durable.ps1","freeze-source.py",
 "launch-after-split-focus.ps1","merge-rank.eval","normalize-frozen-partitions.py","partition-rank.py","replan-from-parent-times.py",
 "run-durable.ps1","run-fast-splits.py","run-full-diagnostics.ps1","run-linked-scope.ps1","run-planned-pilot.py",
 "run-split-focus-and-full.ps1","summarize-function-rank.eval","verify-scoped-disassembly.ps1","wait-then-collect-full-evidence.ps1",
 "stage-readiness.py","transfer-after-raw-retention.py","run-readiness-durable.ps1","run-census-labels.ps1","prepare-final-label-census.py",
 "label-census-retake.py","final-label-census.eval","collate-loader-roles.eval","finalize-receipts.ps1",
 "run-census-labels-driver.ps1","finalize-receipts-driver.ps1","run-frozen-corpus-bounded.ps1"
]
paths={parent.as_posix():stage.as_posix(),oldbase.as_posix():parent.as_posix()}
pat=re.compile("|".join(re.escape(x) for x in sorted(paths,key=len,reverse=True)))
for file in internal:
    value=pat.sub(lambda m:paths[m.group()],(parent/file).read_text(encoding="utf-8-sig"))
    if file=="stage-readiness.py":
        value=value.replace("Part 1966","Part 1967").replace(
          "Linked native witnesses exercise selected BER failure paths and the shared tagged-entry checks.",
          "Linked native witnesses exercise checked record continuations, grouped failure exits, and deferred Mnesia argument construction.")
    if file=="finalize-receipts.ps1": value=value.replace("record-and-archive-1966.py","record-and-archive-1967.py")
    txt(stage/file,value)
for file in ["strict-preflight.py","merge-harness-hunks.py","prepare-linked-scope.py"]:
    shutil.copyfile(templates/file,stage/file)
shutil.copyfile(parent/"normalized-cache-benchmark.json",stage/"normalized-cache-benchmark.json")
subprocess.run([sys.executable,stage/"merge-harness-hunks.py",stage,live],check=True)
print(read(stage/"scope-plan.json"))

