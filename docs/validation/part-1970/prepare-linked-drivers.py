from pathlib import Path
import json,re,shutil,sys

stage,parent,base,inputs,mixed_audit,mixed_draft=map(Path,sys.argv[1:])
def text(folder,name):
    return (folder/name).read_text(encoding="utf-8-sig")
def save(name,value):
    (stage/name).write_text(value.rstrip()+"\n",encoding="utf-8",newline="\n")
def read(folder,name):
    return json.loads(text(folder,name))
artifacts={
    "EntryRecordHeads":(inputs/"entry-first-frame-audit-1967",
        ["REPORT.md","list-head-record-placement.terms","exact-control-hashes.tsv","fixture/native.eval"]),
    "BoundOrderedCons":(inputs/"returned-constructor-lifetime-census-1967/fresh-bound-rhs-draft",
        ["README.md","draft-receipt.json","reference-results.terms"]),
    "SavedCasePayloadScratch":(inputs/"saved-case-payload-scratch-1970-intake",
        ["receipt.md","oracle.stdout.log","case_payload_scratch_probe.erl"]),
    "MixedAtomTuplePair":(mixed_draft,
        ["native-pinned.stdout.log","frozen-native.stdout.log","frozen-structural.stdout.log","strict-preflight-draft.json"]),
}
parts=read(stage,"agent-intake.json")
prior_components=read(parent,"components.json")
for item in parts:
    family=item["Agent"]
    folder,files=artifacts[family]
    evidence=[folder/name for name in files]
    assert all(p.is_file() for p in evidence)
    item["Evidence"]=[p.as_posix() for p in evidence]
    if family=="MixedAtomTuplePair":
        item["Evidence"] += [(mixed_audit/name).as_posix() for name in (
            "small-mixed-summary.terms","small-mixed-layouts.terms","physical-normalized-labels.diff","normalized.terms")]
    prior_components[family]={"Root":folder.as_posix(),"Patches":list(item["PatchDigests"]),"Evidence":item["Evidence"]}
save("components.json",json.dumps(prior_components,indent=2))
save("agent-intake.json",json.dumps(parts,indent=2))
scoped=set(read(parent/"linked-runtime-witnesses","receipt.json")["ActualImages"].keys())
all_rows=read(base/"full-uncached","otp-corpus.json")["Modules"]
module_by_name={Path(row["Beam"]).stem:row["Beam"].replace(chr(92),"/") for row in all_rows}
layout=text(mixed_audit,"small-mixed-layouts.terms").splitlines()
layout_paths={re.match(r'^\{"([^"]+)"',line)[1] for line in layout
    if '[tuple_first],[atom_first],"Different"' in line}
assert len(layout_paths)==64
confirmed_controls={re.match(r'^\{"([^"]+)"',line)[1] for line in layout if line.endswith('"Equal"}.')}
scoped.update(layout_paths|confirmed_controls)
record=artifacts["EntryRecordHeads"][0]
matches=set(re.findall(r"^\{([a-z]\w*),\w+,\d+,\s*same_tag_before_vs_after",
    text(record,"list-head-record-placement.terms"),re.MULTILINE))
assert len(matches)==10,matches
scoped.update(module_by_name[name] for name in matches)
scoped.update(line.split("\t")[0] for line in text(record,"exact-control-hashes.tsv").splitlines())
scoped.update(module_by_name[name] for name in (
    "erl_syntax_lib","diameter_service","sofs","mnesia_bup","mnesia_backup","unix_telnet","megaco_trans_sup"))
assert scoped<=set(module_by_name.values())
save("pilot-modules.txt","\n".join(sorted(scoped,key=str.casefold)))
save("scope-plan.json",json.dumps({
    "SourceParent":read(parent,"source-freeze.json")["Source"],"ModuleCount":len(scoped),
    "CasePartitionOwners":103,"CasePartitionModules":64,"PreviousExactCaseControls":len(confirmed_controls),
    "RecordHeadSiteModules":len(matches),"NewTests":2,"Extended":2,
    "ReleaseNativeCases":219,"SyntheticCases":84,"CompileCache":False
},indent=2))
drivers=[
    "bind-final-native.py","bind-source-closure.py","cached-normalized-delta.eval","check-disassembler-role.eval",
    "collate-loader-roles.eval","complete-durable.ps1","final-label-census.eval","finalize-receipts-driver.ps1",
    "finalize-receipts.ps1","freeze-source.py","label-census-retake.py","launch-after-split-focus.ps1",
    "merge-rank.eval","normalize-frozen-partitions.py","partition-rank.py","prepare-final-label-census.py",
    "prepare-linked-scope.py","replan-from-parent-times.py","run-census-labels-driver.ps1","run-census-labels.ps1",
    "run-durable.ps1","run-fast-splits.py","run-frozen-corpus-bounded.ps1","run-full-diagnostics.ps1",
    "run-linked-runtime.ps1","run-linked-scope.ps1","run-planned-pilot.py","run-readiness-durable.ps1",
    "run-split-focus-and-full.ps1","ssh-file-roles.eval","stage-readiness.py","summarize-function-rank.eval",
    "transfer-after-raw-retention.py","verify-linked-runtime.py","verify-scoped-disassembly.ps1",
    "wait-then-collect-full-evidence.ps1","normalized-cache-benchmark.json"
]
replace={parent.as_posix():stage.as_posix(),base.as_posix():parent.as_posix()}
pattern=re.compile("|".join(re.escape(x) for x in sorted(replace,key=len,reverse=True)))
for fn in drivers:
    body=pattern.sub(lambda match:replace[match[0]],text(parent,fn))
    if fn=="bind-final-native.py":
        body=body.replace('>=217 and witness["SyntheticCases"]==51','>=219 and witness["SyntheticCases"]==84')
    if fn=="finalize-receipts.ps1":
        body=body.replace("record-and-archive-1969.py","record-and-archive-1970.py")
    if fn=="stage-readiness.py":
        body=body.replace("Part 1969","Part 1970").replace(
            "checked record continuations, source-certified constructor aliases, and function-local frame ownership",
            "typed case exits, early list-head checks, and fresh-bound constructor evaluation")
    save(fn,body)
print(len(scoped),"scope modules")
