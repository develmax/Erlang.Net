from pathlib import Path
import json,re,sys

stage,parent,held,accepted=map(Path,sys.argv[1:])
def write(name,value):
    (stage/name).write_text(value.rstrip()+"\n",encoding="utf-8",newline="\n")
def text(base,name):
    return (base/name).read_text(encoding="utf-8-sig")
files=[
    "bind-final-native.py","bind-source-closure.py","cached-normalized-delta.eval",
    "check-disassembler-role.eval","collate-loader-roles.eval","complete-durable.ps1",
    "final-label-census.eval","finalize-receipts-driver.ps1","finalize-receipts.ps1",
    "freeze-source.py","label-census-retake.py","launch-after-split-focus.ps1",
    "merge-rank.eval","normalize-frozen-partitions.py","partition-rank.py",
    "prepare-final-label-census.py","replan-from-parent-times.py","run-census-labels-driver.ps1",
    "run-census-labels.ps1","run-durable.ps1","run-fast-splits.py",
    "run-frozen-corpus-bounded.ps1","run-full-diagnostics.ps1","run-linked-runtime.ps1",
    "run-linked-scope.ps1","run-planned-pilot.py","run-readiness-durable.ps1",
    "run-split-focus-and-full.ps1","stage-readiness.py","summarize-function-rank.eval",
    "transfer-after-raw-retention.py","verify-linked-runtime.py","verify-scoped-disassembly.ps1",
    "wait-then-collect-full-evidence.ps1"
]
roles={
    parent.as_posix():stage.as_posix(),
    held.as_posix():parent.as_posix(),
    accepted.as_posix():parent.as_posix(),
}
managed = re.compile("|".join(re.escape(p) for p in sorted(roles,key=len,reverse=True)))
preserve_old={"verify-linked-runtime.py"}
for name in files:
    body=text(parent,name)
    if name in preserve_old:
        body=body.replace("**data(parent/\"components.json\"),","**data(parent/\"components.json\"),")
    else:
        body=managed.sub(lambda match:roles[match[0]],body)
    if name=="launch-after-split-focus.ps1":
        body,n=re.subn(r'& pwsh -NoProfile -File "\$s/verify-pilot-correction.ps1"\r?\nif\(\$LASTEXITCODE\)\{[^\n]+\}',"",body)
        assert n==1
    if name=="bind-final-native.py":
        body=body.replace('>=172 and witness["SyntheticCases"]==13','>=192 and witness["SyntheticCases"]==28')
    if name=="stage-readiness.py":
        body=body.replace("Part 1967","Part 1968")
        body=body.replace("Linked native witnesses exercise checked record continuations, grouped failure exits, and deferred Mnesia argument construction.","Linked native witnesses exercise checked record continuations, terminal constructor dispatch, and deferred list construction.")
    if name=="finalize-receipts.ps1":
        body=body.replace("record-and-archive-1967.py","record-and-archive-1968.py")
    write(name,body)

historic=json.loads(text(held,"components.json"))
historic.update(json.loads(text(parent,"components.json")))
p8=json.loads(text(stage,"agent-intake.json"))
for item in p8:
    prod=Path(next(iter(item["PatchDigests"])))
    spec=json.loads(text(stage.parent,"part-1968-qualified-component-selection.json"))[item["Agent"]]
    base=stage.parent/spec[0]
    historic[item["Agent"]]={"Root":base.as_posix(),"Patches":list(item["PatchDigests"]),
        "SHA":list(item["PatchDigests"].values()),"Report":item["Report"],"Evidence":item["Evidence"]}
write("components.json",json.dumps(historic,indent=2))
write("normalized-cache-benchmark.json",text(parent,"normalized-cache-benchmark.json"))
def names(body):
    return body.strip().splitlines()
rows=json.loads(text(accepted/"full-uncached","otp-corpus.json"))["Modules"]
anchor_names={"ct_default_gl","ct_logs","ex_gauge","ex_listCtrl","unix_telnet","megaco_monitor",
 "OTP-PKIX","OTP-PKIX-Relaxed","PKIX1Explicit-2009","mnesia_backup","mnesia_dumper",
 "megaco_digit_map","snmpa_mib_storage_ets","ssh_dbg","asn1ct_gen_ber_bin_v2",
 "application_master","megaco_flex_scanner","observer_html_lib","shell",
 "beam_a","beam_asm","epp_dodger","beam_ssa_destructive_update",
 "edoc_html_to_markdown","observer_wx","ssl_pkix_db"}
modules={row["Beam"].replace(chr(92),"/") for row in rows if Path(row["Beam"]).stem in anchor_names}
assert len(modules)==len(anchor_names),{Path(item).stem for item in modules}
union=modules|set(names(text(stage,"pilot-modules.txt")))
write("pilot-modules.txt","\n".join(sorted(union,key=str.casefold)))
write("provisional-plan.json",json.dumps({
    "Parent":parent.as_posix(),"Modules":len(union),"NativeWitnessImages":len(anchor_names),
    "PlannedNewCases":len(json.loads(text(stage,"new-cases.json"))),
    "RequiresParentCompletion":True,"WholeCorpusShards":5,"CompilationCache":False
},indent=2))
print(f"{len(union)} complete scoped images, {len(anchor_names)} execution witness images")
