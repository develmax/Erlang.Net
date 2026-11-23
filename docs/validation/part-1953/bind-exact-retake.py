from pathlib import Path
import hashlib,json,shutil,sys

prior,stage,work=map(Path,sys.argv[1:])
def read(p): return json.loads(p.read_text(encoding="utf-8-sig"))
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def save(p,val):p.write_text(json.dumps(val,indent=2)+"\n",encoding="utf-8",newline="\n")

files=("component-specs.json","agent-intake.json","scope-plan.json","scope.json",
       "focused-test-names.txt","fast-test-names.txt","pilot-modules.txt","exact-controls.txt",
       "harness-registrations.txt","synchronous-callbacks.json","adapted-test-receipt.json",
       "strict-preflight.json","replayed.txt","normalized-cache-benchmark.json","all-patches-draft.log",
       "all-patches-draft.stderr.log","all-patches-draft.exit","intake-replay-order.txt","recursive-comprehension-seal.txt",
       "type-smoke.json","type-smoke.log","type-smoke-build.log","type-smoke.exit")
for f in prior.iterdir():
    if f.is_file() and f.suffix in (".py",".ps1",".eval") and f.name not in ("bind-retake.py","prepare-finalize.py"):
        source=f.read_text(encoding="utf-8-sig").replace(prior.as_posix(),stage.as_posix())
        (stage/f.name).write_text(source,encoding="utf-8",newline="\n")
for name in files: shutil.copyfile(prior/name,stage/name)
data=read(prior/"hydration-receipt.json")
data["PriorCompiler"]=read(prior/"focused-identity.json")["Compiler"]
changes={r["Path"]:r["Sha256"] for r in read(prior/"source-identities.json")}
assert changes==data["CandidateChangedPaths"]
fix=read(stage/"retake-proofs/projection-retake.json")
path=fix["Path"]
assert fix["Before"]==changes.get(path,fix["Before"])
assert fix["After"]==sha(work/path)
changes[path]=fix["After"]
assert all(sha(work/path)==digest for path,digest in changes.items())
data["CandidateChangedPaths"]=changes
data["RetakeProofs"].append(dict(Path=path,Reason=fix["Reason"],Digest=sha(stage/"retake-proofs/projection-retake.json")))
save(stage/"hydration-receipt.json",data)
# Preserve both gate failures and the contrastive disassembly under the final seal.
proofs=stage/"retake-proofs"
for source in sorted((prior/"retake-proofs").glob("*")):
    if source.is_file(): shutil.copyfile(source,proofs/source.name)
for rel,tag in [
    ("focused-whole-fixture.json","whole-first.json"),("focused-whole.stdout.log","whole-first.log"),
    ("pilot-receipt.json","whole-first-pilot-receipt.json"),("focused-identity.json","whole-first-compiler.json"),
    ("linked-runtime-witnesses/receipt.json","runtime-recipe-retake.json"),
    ("runtime-first/linked-runtime.stderr.log","runtime-first.stderr.log"),
    ("runtime-first/linked-runtime.exit","runtime-first.exit"),
    ("runtime-first/linked-runtime-witnesses/ex_cursor-pinned.stderr.log","cursor-recipe-first.stderr.log"),
]:
    src=prior/rel
    if src.is_file():shutil.copyfile(src,proofs/tag)
archive=stage/"record-and-archive-1953.py"
script=archive.read_text(encoding="utf-8")
script=script.replace('"bind-retake.py"', '"bind-exact-retake.py","native-ex-cursor-events.eval"')
script=script.replace("Preserve both initial focused failures and the pinned behavior used to correct the guarded fixture.", "Preserve both initial focused failures, the pinned guarded behavior and the original dynamic apply projection.")
archive.write_text(script,encoding="utf-8",newline="\n")
print(len(changes),"verified linked source paths")

