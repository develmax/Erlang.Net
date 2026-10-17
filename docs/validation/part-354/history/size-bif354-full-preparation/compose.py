"""Artifact-only plan/apply; no SDK, Git mutation or network. Root reviews before apply."""
from pathlib import Path
import argparse,hashlib,json,re,shutil,subprocess
R=Path(__file__).resolve().parents[2];P=Path(__file__).parent
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
S=lambda b:hashlib.sha256(b).hexdigest().upper()
J=lambda p:json.loads(p.read_bytes())
B=lambda v:(json.dumps(v,indent=2)+"\n").encode()
PARENT="a9ec41acf2eb5727f60c613ba8146c470f82fbb0"
REG="docs/supported-mfas.json"
BASE="artifacts/scheduler-affinity353-composition-plan2/composition-plan.json"
OVERLAYS=[("artifacts/part354-metadata-proposal2/copy-plan.json","EBD1E596C3791C681322E992506E02D596250CFBE7DD4696C74849DDFFC107DC"),("artifacts/common-error354-documentation-proposal3/copy-plan.json","AC34168B93FA871F68C7BC3DBA90ABD0449D07C1168900F3C325C0BA895097E4")]
PINNED=[("artifacts/root-size354-styled3-fixtures.json","6834C00D6DE1C14238FFD9D50B3C7F837B236EBD1470CC1E1150347A9A2CD2A8"),("artifacts/root-size354-focus11-independent.json","2DABAA680DF8CC8F7483B4437EA8385FC66D415B094B977BCC3EF1A68338D35D"),("artifacts/common-error354-capture-independent/final-styled3-report.json","6F8F70465578BBDAB8C544916D35B941DC5477F28B8DD072666CF48E4D0942AD")]
PLANS=[("tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json",".github/workflows/nif-pending-native.yml","REVIEWED_SOURCE_PLAN_SHA256"),("tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json",".github/workflows/nif-reallocation-native.yml","REVIEWED_REALLOCATION_PLAN_SHA256")]
PACKAGE=["README.md","docs/DOTNET-LICENSE.txt","docs/DOTNET-ThirdPartyNotices.txt","docs/OTP-LICENSE.txt","docs/ROSLYN-LICENSE.txt","docs/OTP-PORTS.md"]
def safe(s):
 p=Path(s);assert not p.is_absolute() and ".." not in p.parts and not any(x.lower() in (".git","bin","obj") for x in p.parts) and "license.local" not in s.lower();return s
def check(row,key="Path"):
 q=R/safe(row[key]);assert q.stat().st_size==row["Bytes"] and H(q)==row["Sha256"],row[key];return q
a=argparse.ArgumentParser();a.add_argument("mode",choices=["plan","apply"]);a.add_argument("--approved-plan-sha256");v=a.parse_args();assert subprocess.check_output(["git","rev-parse","HEAD"],cwd=R,timeout=10).decode().strip()==PARENT
planpath=P/"composition-plan.json"
if v.mode=="apply":
 assert H(planpath)==v.approved_plan_sha256.upper();plan=J(planpath);assert plan["ComposerSha256"]==H(Path(__file__))
 dest=Path(plan["Destination"]);assert dest.is_relative_to(R/"artifacts") and not dest.exists()
 for row in plan["InputPins"]:check(row)
 for row in plan["Files"]:check(row,"SourcePath")
 dest.mkdir()
 for row in plan["Files"]:
  q=dest/safe(row["Path"]);q.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(R/row["SourcePath"],q)
 for row in plan["Files"]:assert H(dest/row["Path"])==row["Sha256"]
 receipt={"Status":"ACTUAL_SOURCE_COMPOSITION_ONLY_NO_SDK","PlanSha256":H(planpath),"Destination":str(dest),"Rows":len(plan["Files"]),"SdkExecuted":False,"MainChanged":False};(P/"application-receipt.json").write_bytes(B(receipt));print(receipt);raise SystemExit
assert not planpath.exists();mapping={};kinds={};owned=set();pins=[];generated={}
def pin(q):pins.append({"Path":q.relative_to(R).as_posix(),"Bytes":q.stat().st_size,"Sha256":H(q)})
def add(path,q,kind):
 safe(path);assert q.is_file(),str(q);mapping[path]=q;kinds[path]=kind
def content(path):return generated[path] if path in generated else mapping[path].read_bytes()
base=J(R/BASE);assert len(base["Files"])==3107;pin(R/BASE)
# Every base path uses CURRENT published Main raw bytes, never old candidate source paths.
for row in base["Files"]:add(row["Path"],R/row["Path"],"CurrentPublishedA9Raw")
for path,digest in PINNED:assert H(R/path)==digest;pin(R/path)
focus=J(R/PINNED[1][0]);assert focus["SemanticPassed"]==36 and focus["SemanticFailed"]==0 and focus["ManagedExecuted"]==36
styled=J(R/PINNED[0][0]);assert len(styled["Files"])==50
for row in styled["Files"]:
 q=R/"artifacts/size-bif354-owned-style-view3"/row["Path"];assert H(q)==row["Sha256"] and q.stat().st_size==row["Bytes"];add(row["Path"],q,"FinalStyledPermanent3Owned");owned.add(row["Path"])
for path,digest in OVERLAYS:
 assert H(R/path)==digest;pin(R/path)
 for row in J(R/path)["Files"]:
  target=safe(row["TargetPath"]);parent=R/target;assert (H(parent) if parent.exists() else None)==row["ParentSha256"],target
  q=Path(row["SourcePath"]);assert q.is_relative_to(R/"artifacts") and H(q)==row["Sha256"] and q.stat().st_size==row["Bytes"];add(target,q,"ReviewedMetadataPackageOverlay");owned.add(target)
assert content(".gitattributes").splitlines().count(b"docs/validation/part-354/** binary")==1
scopepath="docs/module-readiness-scope.json";scope=json.loads(content(scopepath));scope["ComparisonTestReport"]="validation/part-353/full/raw/artifacts/tests.json";generated[scopepath]=B(scope);owned.add(scopepath)
priorpath="docs/validation/part-353/full/raw/artifacts/tests.json";add(priorpath,R/priorpath,"Published353ActualFullBaseline");prior=json.loads(content(priorpath));assert prior["Passed"]==7313 and prior["Failed"]==0 and len(prior["Tests"])==7313
ledger=json.loads(content("docs/mfa-compatibility.json"));assert len(ledger["Entries"])==587 and len(ledger["VerifiedSourceFiles"])==114
counts={s:sum(e["Status"]==s for e in ledger["Entries"]) for s in ("Verified compatible","Known differences","Needs verification")};assert counts=={"Verified compatible":16,"Known differences":30,"Needs verification":541}
required={priorpath,"reference-baseline.json","docs/otp-inventory.json",REG,"docs/module-readiness-scope.json","docs/mfa-compatibility.json"}
for entry in ledger["Entries"]:
 for evidence in entry["Evidence"]:
  target="docs/"+safe(evidence["Path"])
  if target not in mapping:add(target,R/target,"TransitiveLedgerEvidence")
  assert S(content(target))==evidence["Sha256"],target;required.add(target)
for row in ledger["VerifiedSourceFiles"]:assert S(content(row["Path"]))==row["Sha256"],row["Path"]
registry=json.loads(content(REG));assert len(registry)==587 and content(REG)==(R/REG).read_bytes()
# Each source plan preserves all non-Files metadata and old row policies; only current bytes and added runtime paths change.
planstats=[]
for path,workflow,seal in PLANS:
 old=json.loads(content(path));plan=json.loads(content(path));oldrows={x["Path"]:dict(x) for x in plan["Files"]};newpaths=sorted(x["Path"] for x in styled["Files"] if x["Path"].startswith("src/") and x["Path"] not in oldrows)
 for new in newpaths:plan["Files"].append({"Path":new,"Bytes":0,"Sha256":"","HashMode":"TextLF"})
 for row in plan["Files"]:
  assert row["HashMode"] in ("TextLF","BinaryExact");data=content(row["Path"]);data=data.replace(b"\r\n",b"\n") if row["HashMode"]=="TextLF" else data;row["Bytes"]=len(data);row["Sha256"]=S(data)
 generated[path]=B(plan);owned.add(path)
 data=content(workflow);pattern=rb"("+seal.encode()+rb": )([A-F0-9]{64})";assert len(re.findall(pattern,data))==1;generated[workflow]=re.sub(pattern,lambda m:m[1]+S(generated[path]).encode(),data);owned.add(workflow)
 planstats.append({"Path":path,"OldRows":len(oldrows),"Rows":len(plan["Files"]),"NewRuntimePaths":newpaths,"NonFilesMetadataPreserved":{k:v for k,v in old.items() if k!="Files"}=={k:v for k,v in plan.items() if k!="Files"}})
# Reallocation plan depends on the already-resealed pending plan/workflow; verify every final policy byte.
for path,workflow,seal in PLANS:
 assert b"\r" not in generated[path] and not generated[path].startswith(b"\xef\xbb\xbf")
 for row in json.loads(content(path))["Files"]:
  data=content(row["Path"]);data=data.replace(b"\r\n",b"\n") if row["HashMode"]=="TextLF" else data;assert row["Bytes"]==len(data) and row["Sha256"]==S(data),row["Path"]
 assert re.search(seal.encode()+rb": ([A-F0-9]{64})",content(workflow))[1].decode()==S(content(path))
for path,data in generated.items():
 q=P/"generated"/path;q.parent.mkdir(parents=True,exist_ok=True);q.write_bytes(data);add(path,q,"GeneratedComparisonAndNativePlanSeals")
rows=[{"Path":path,"SourcePath":q.relative_to(R).as_posix(),"Bytes":q.stat().st_size,"Sha256":H(q),"Kind":kinds[path]} for path,q in sorted(mapping.items())];stable=[x for x in rows if x["Path"]!=REG]
fixtures=J(R/"artifacts/size-bif354-permanent-test-proposal3/fixtures.json")["Cases"];assert len(fixtures)==23
newnames=["oracle/compiled-module/"+x["Module"] for x in fixtures]+["runtime/backtrace-depth-independent-runtimes","runtime/common-capture-identity-rollback-async","runtime/common-capture-null-fallback-prior-host","runtime/configured-load-nif-common-capture","runtime/common-preloaded-reinstall-identity","runtime/common-undefined-lambda-handler-provenance"];assert len(set(newnames))==29
plan={"Status":"REVIEWABLE_SOURCE_ONLY_FULL354_PLAN_NOT_APPLIED","ParentHead":PARENT,"Destination":str(R/"artifacts/size-bif354-full-candidate"),"ComposerSha256":H(Path(__file__)),"InputPins":pins,"Files":rows,"StableClosure":stable,"OwnedPaths":sorted(owned),"PackageResources":[next(r for r in rows if r["Path"]==p) for p in PACKAGE],"ReadinessInputs":sorted(required),"VerifiedSourceGuardCount":114,"CompatibilityCounts":counts,"ExpectedOldOrderedPrefix":7313,"ExpectedNewTests":29,"AddedTestNames":newnames,"ExpectedFullTestsIfPasses":7342,"PreviousTestReport":priorpath,"ExpectedIntegrationOutcomes":11,"ExpectedPackageOutputs":2,"RegistryBefore":next(r for r in rows if r["Path"]==REG),"RegistryExcludedFromStableClosure":True,"NativePlans":planstats,"SdkExecuted":False,"NativeExecuted":False,"MainChanged":False,"AcceptedFocusGuardSha256":H(R/PINNED[1][0])}
planpath.write_bytes(B(plan));print({"PlanSha256":H(planpath),"Rows":len(rows),"StableRows":len(stable),"Owned":len(owned),"NativePlans":planstats})
