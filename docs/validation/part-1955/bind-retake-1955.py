from pathlib import Path
import hashlib,json,shutil,sys
prior,stage,build_parent,accepted,source,repo=map(Path,sys.argv[1:])
def read(p):return json.loads(p.read_text(encoding="utf-8-sig"))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,s):p.write_text(s,encoding="utf-8",newline="\n")
def save(p,d):write(p,json.dumps(d,indent=2)+"\n")
files=("component-specs.json","sealed-components.json","agent-intake.json","scope-plan.json","scope.json",
       "focused-test-names.txt","fast-test-names.txt","pilot-modules.txt","exact-controls.txt","harness-registrations.txt",
       "synchronous-callbacks.json","adapted-test-receipt.json","strict-preflight.json","replayed.txt",
       "normalized-cache-benchmark.json","all-patches-draft.log","all-patches-draft.stderr.log",
       "all-patches-draft.exit","intake-replay-order.txt","intake-verified.log","intake-verified.stderr.log",
       "additional-native-cases.json")
for f in prior.iterdir():
    if f.is_file() and f.suffix in (".py",".ps1",".eval") and f.name!="bind-retake-1955.py":
        write(stage/f.name,f.read_text(encoding="utf-8-sig").replace(prior.as_posix(),stage.as_posix()))
for name in files: shutil.copyfile(prior/name,stage/name)
shutil.copytree(prior/"adapted-tests",stage/"adapted-tests")
proofs=stage/"retake-proofs"
for rel in ("pilot.json","pilot-delta.json","pilot-receipt.json","focused-identity.json","focused-fast.json",
            "focused-fast.log","source-identities.json","split-orchestration.stderr.log","durable.exit"):
    shutil.copyfile(prior/rel,proofs/("first-"+rel))
copy=prior/"linked-runtime-witnesses/receipt.json"
assert copy.is_file()
shutil.copyfile(copy,proofs/"first-linked-runtime.json")
data=read(prior/"hydration-receipt.json")
verified={entry["Path"]:entry["Sha256"].lower() for entry in read(prior/"source-identities.json")}
assert set(verified)==data["CandidateChangedPaths"].keys()
parent=read(build_parent/"source-hashes.json")
retakes={}
for fix in ("closure","forwarding"):
    record=proofs/(fix+"-source.json")
    for p,n in read(record).items():
        assert p not in retakes and n["Before"]==verified.get(p,parent.get(p)),p
        assert sha(source/p)==n["After"],p
        retakes[p]=n["After"]
assert len(retakes)==5
verified.update(retakes)
assert all(sha(source/p)==digest for p,digest in verified.items())
assert all(sha(source/p)==digest for p,digest in parent.items() if p not in verified)
data["CandidateChangedPaths"]=verified
data["RetakeSourcePaths"]={p:retakes[p] for p in sorted(retakes)}
data["RejectedCompiler"]=read(prior/"focused-identity.json")["Compiler"]
data["AcceptedComparisonBase"]=read(accepted/"source-freeze.json")["Source"]
save(stage/"hydration-receipt.json",data)
test=(proofs/"closure-test-name.txt").read_text().strip()
for f in ("fast-test-names.txt","focused-test-names.txt"):
    path=stage/f
    collection=path.read_text().splitlines()
    assert test not in collection
    write(path,"\n".join([*collection,test])+"\n")
scope=read(stage/"scope.json")
for key in ("Cases","FastCases","TotalCatalog"):scope[key]+=1
modules=set((stage/"pilot-modules.txt").read_text().splitlines())
modules.add("lib/common_test-1.31.2/ebin/ct_logs.beam")
modules.add("lib/ssl-11.7.7/ebin/ssl_crl_hash_dir.beam")
write(stage/"pilot-modules.txt","\n".join(sorted(modules,key=str.casefold))+"\n")
scope["Modules"]=len(modules)
save(stage/"scope.json",scope)
model=read(stage/"scope-plan.json")
vol={r["Beam"].replace(chr(92),"/"):r["Status"] for r in read(build_parent/"full-uncached/otp-corpus.json")["Modules"]}
model.update(Modules=len(modules),RetainedBaselineCounts={s:sum(vol[k]==s for k in modules) for s in ("Equal","Different")},
             Retakes=["Closure capture ownership","Closed entry forwarding"],PassedSourceBaseline=accepted.as_posix())
save(stage/"scope-plan.json",model)

def replace(name,old,new):
    file=stage/name
    text=file.read_text()
    assert old in text,(name,old)
    write(file,text.replace(old,new))
replace("launch-after-split-focus.ps1",'$ready="$p/finalize-receipts.exit"',
        f'$accepted="{accepted.as_posix()}"\n$ready="$accepted/finalize-receipts.exit"')
replace("launch-after-split-focus.ps1",'    Partitions=$plan.Modules;',
        '    EffectiveAcceptanceParent=$accepted;BuildInputParent=$p;Partitions=$plan.Modules;')
replace("run-full-diagnostics.ps1",f"$prior='{build_parent.as_posix()}/full-uncached'",
        f"$prior='{accepted.as_posix()}/full-uncached'")
id_line="if($LASTEXITCODE){exit $LASTEXITCODE}"
f=stage/"run-full-diagnostics.ps1"
s=f.read_text()
point=s.index("\n"+id_line,s.index("compare-frozen-corpora.py"))+1+len(id_line)
guard=f"""
& 'C:/Users/Maksim/AppData/Local/Programs/Python/Python312/python.exe' "$stage/verify-interim-raw.py" '{build_parent.as_posix()}/full-uncached/otp-corpus.json' "$full/otp-corpus.json" "$full/interim-raw-retention.json"
if($LASTEXITCODE){{exit $LASTEXITCODE}}
"""
s=s[:point]+guard+s[point:]
point=s.index("\n"+id_line,s.index("normalized-summary.stderr.log"))+1+len(id_line)
guard=f"""
& $beam +S '1:1' +A 1 -noshell -eval (Get-Content "$stage/verify-interim-normalized.eval" -Raw) -extra '{build_parent.as_posix()}/full-uncached/normalized-summary.terms' "$full/normalized-summary.terms" "$full/interim-normalized-retention.terms" > "$full/interim-normalized.stdout.log" 2> "$full/interim-normalized.stderr.log"
if($LASTEXITCODE){{exit $LASTEXITCODE}}
"""
write(f,s[:point]+guard+s[point:])
f=stage/"finalize-receipts.ps1"
s=f.read_text()
s=s.replace(f"{repo.as_posix()}/docs/validation/part-1954/sha256.json",f"{repo.as_posix()}/docs/validation/part-1953/sha256.json")
s=s.replace("Part 1954 proved release archive","Part 1953 accepted release archive").replace("Part 1954 seal","Part 1953 acceptance seal")
s='\n'.join(line.replace(build_parent.as_posix(),accepted.as_posix()) if (
    "stage-readiness.py" in line or "transfer-after-raw-retention.py" in line) else line for line in s.split('\n'))
write(f,s)
replace("record-and-archive-1955.py",f'Path("{build_parent.as_posix()}")',f'Path("{accepted.as_posix()}")')
replace("record-and-archive-1955.py",'native[\'LinkedExecution\'][\'AdditionalSynthetic\']==35','native[\'LinkedExecution\'][\'AdditionalSynthetic\']==36')
replace("record-and-archive-1955.py","A further 35 synthetic cases","A further 36 synthetic cases")
replace("record-and-archive-1955.py",'for folder in ("adapted-tests",):','for folder in ("adapted-tests","retake-proofs"):')
replace("record-and-archive-1955.py",'".patch",".txt",".cs"}','".patch",".txt",".cs",".md",".result"}')
replace("record-and-archive-1955.py",'"additional-native-cases.json",','"additional-native-cases.json","bind-retake-1955.py","verify-interim-raw.py","verify-interim-normalized.eval",')
replace("record-and-archive-1955.py","Preserve known constructor input patterns","Restore captured closure order and forwarded ASN.1 entry arguments found in the initial gate.\n- Preserve known constructor input patterns")
replace("verify-linked-runtime.py","constant=Path(spec['ClosedEffectArgumentRematerialization']['Root'])",
        "constant=root_inputs/'binary-match-audit-1931/entry-forwarding-boundary-1966'")
replace("verify-linked-runtime.py","agree on six","agree on seven")
replace("verify-linked-runtime.py","'ClosedArguments':{'Count':6","'ClosedArguments':{'Count':7")
print(scope)

