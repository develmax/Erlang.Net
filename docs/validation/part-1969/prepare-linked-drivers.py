from pathlib import Path
import hashlib,json,re,shutil,sys

stage,parent,previous,inputs,near=map(Path,sys.argv[1:])
def read(where,name):
    return (where/name).read_text(encoding="utf-8-sig")
def save(name,value):
    (stage/name).write_text(value.rstrip()+"\n",encoding="utf-8",newline="\n")
def write_json(name,value):
    save(name,json.dumps(value,indent=2))
def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()
handoff=json.loads(read(stage,"agent-intake.json"))
components=json.loads(read(parent,"components.json"))
sources={
    "LeadingVariableComprehensions":("xmerl-leading-arm-comprehension-1969-intake",["receipt.json","scope.txt"]),
    "OwnedFrameAliases":("owned-frame-regions-and-interior-alias-audit/proposal",
                         ["validation/candidate-scope.json","validation/scope-delta.json","validation/receipt.json",
                          "validation/validation-summary.terms","validation/cohort-outcomes.terms"]),
    "NestedHeadTokenAliases":("epp-input-token-alias-audit-1968",["receipt.md","physical-schedules.terms"]),
    "StructuredConsEquality":("structured-cons-equality-comprehension-1968-intake",["receipt.json","recovered-key-baselines.terms"]),
    "OrdinaryAppendComprehensions":("ordinary-list-append-comprehension-1968-intake",["receipt.json","release-targets.tsv"]),
    "NonterminalCaseCarriers":(near.name,["carrier-probe.terms","nonterminal-case-carriers.terms","unix_telnet.ssa"]),
}
for item in handoff:
    key=item["Agent"]
    directory,files=sources[key]
    directory=inputs/directory
    evidence=[directory/file for file in files]
    assert all(file.is_file() for file in evidence)
    item["Evidence"]=[p.as_posix() for p in evidence]
    components[key]={"Root":directory.as_posix(),"Patches":list(item["PatchDigests"]),
                     "SHA":list(item["PatchDigests"].values()),"Evidence":item["Evidence"]}
write_json("components.json",components)
write_json("agent-intake.json",handoff)

case_candidates=json.loads(read(previous/"full-uncached","otp-corpus.json"))["Modules"]
paths={Path(row["Beam"].replace(chr(92),"/")).stem:row["Beam"].replace(chr(92),"/") for row in case_candidates}
anchor_names={"ct_default_gl","ct_logs","ex_gauge","ex_listCtrl","unix_telnet","megaco_monitor",
 "OTP-PKIX","OTP-PKIX-Relaxed","PKIX1Explicit-2009","mnesia_backup","mnesia_dumper",
 "megaco_digit_map","snmpa_mib_storage_ets","ssh_dbg","asn1ct_gen_ber_bin_v2",
 "application_master","megaco_flex_scanner","observer_html_lib","shell",
 "beam_a","beam_asm","epp_dodger","beam_ssa_destructive_update","edoc_html_to_markdown","observer_wx","ssl_pkix_db"}
owned={"beam_ssa_pre_codegen","eunit_data","cdv_multi_wx","ssh_transport"}
nearby={"test_server_node","unix_telnet","megaco_messenger","mnesia","reltool_server","dets_utils"}
ordered={"beam_a","beam_asm","epp_dodger","beam_ssa_destructive_update"}
scoped={paths[name] for name in anchor_names|owned|nearby|ordered}
for case in json.loads(read(inputs/sources["OwnedFrameAliases"][0]/"validation","candidate-scope.json"))["Modules"]:
    scoped.add(case["Beam"].replace(chr(92),"/"))
for key in ("StructuredConsEquality","OrdinaryAppendComprehensions"):
    scoped.update(read(inputs/sources[key][0],"focused-scope.txt").splitlines())
scoped.update(read(inputs/sources["LeadingVariableComprehensions"][0],"scope.txt").splitlines())
assert scoped<=set(paths.values())
save("pilot-modules.txt","\n".join(sorted(scoped,key=str.casefold)))
write_json("provisional-plan.json",{
    "Parent":parent.as_posix(),"CoveredModules":len(scoped),"NewCases":5,
    "ExistingExtended":"compiler/beam/removes-trailing-duplicate-call-slot",
    "NativeReleaseCases":217,"NativeSyntheticCases":47,"CachedCompilation":False,
    "Scope":"Previously validated witnesses, both alias types, all 20 frame targets, helper owners and controls",
})
write_json("scope-plan.json",{"CandidateGroupCount":6,"Modules":len(scoped)})

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
replacements={parent.as_posix():stage.as_posix(),previous.as_posix():parent.as_posix()}
matcher=re.compile("|".join(re.escape(key) for key in sorted(replacements,key=len,reverse=True)))
for name in drivers:
    value=matcher.sub(lambda match:replacements[match[0]],read(parent,name))
    if name=="bind-final-native.py":
        old='>=192 and witness["SyntheticCases"]==28'
        assert old in value
        value=value.replace(old,'>=217 and witness["SyntheticCases"]==47')
    if name=="finalize-receipts.ps1":
        value=value.replace("record-and-archive-1968.py","record-and-archive-1969.py")
    if name=="stage-readiness.py":
        value=value.replace("Part 1968","Part 1969").replace(
            "checked record continuations, terminal constructor dispatch, and deferred list construction",
            "checked record continuations, source-certified constructor aliases, and function-local frame ownership")
    save(name,value)
print(len(scoped),"scoped release images")
