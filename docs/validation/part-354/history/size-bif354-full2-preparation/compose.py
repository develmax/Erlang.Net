"""Fresh source-only corrected full2 plan/apply, never mutates failedfull1."""
from pathlib import Path
import argparse,hashlib,json,re,shutil,subprocess
R=Path(__file__).resolve().parents[2];P=Path(__file__).parent
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();S=lambda b:hashlib.sha256(b).hexdigest().upper();J=lambda p:json.loads(p.read_bytes());B=lambda v:(json.dumps(v,indent=2)+"\n").encode()
PARENT="a9ec41acf2eb5727f60c613ba8146c470f82fbb0"
BASE="artifacts/size-bif354-full-preparation/composition-plan.json";BASESHA="7129614325BD320F9205DBFDEBC74E3C7F09065C318431360B64FA6A078890C6"
REG="docs/supported-mfas.json"
PATCHES=[("artifacts/apply-forwarding354-correction/source-freeze.json","29518E0283613D2BD0852C2F6E568ECC14CD6AB01839869F85A0C303D4C5E7F7"),("artifacts/common-error354-erpc-execute-scope-proposal/source-freeze2.json","1666053A974EAC3898EB38D2FED0EF0A171B1DF36B76A7FC1E87F0F070E5196F")]
OVERLAYS=[("artifacts/size-bif354-corrected-source-guards/copy-plan.json","D0D61BDDBE6DC495118F949A316B750D1E36D27ACB83A6C3BB8F9CBEC15371E2"),("artifacts/common-error354-otp-ports-full2-proposal/copy-plan.json","BF82CAC9C7D3BD1CD6E870BCDFF15EB87EFE120BA1717024C7FCA40CEAE64A61")]
FOCUS="artifacts/root-size354-focus14-independent.json";FOCUSS="AAB2EF6AC9EFA11D0A8F7E179E539B1548E91E3F4ADACB20681F574AF0A2FD33"
PLANS=[("tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json",".github/workflows/nif-pending-native.yml","REVIEWED_SOURCE_PLAN_SHA256"),("tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json",".github/workflows/nif-reallocation-native.yml","REVIEWED_REALLOCATION_PLAN_SHA256")]
def safe(s):
 p=Path(s);assert not p.is_absolute() and ".." not in p.parts and not any(x.lower() in (".git","bin","obj") for x in p.parts) and "license.local" not in s.lower();return s
def checked(q,row):assert q.stat().st_size==row["Bytes"] and H(q)==row["Sha256"],str(q)
a=argparse.ArgumentParser();a.add_argument("mode",choices=["plan","apply"]);a.add_argument("--approved-plan-sha256");v=a.parse_args();assert subprocess.check_output(["git","rev-parse","HEAD"],cwd=R,timeout=10).decode().strip()==PARENT
pp=P/"composition-plan.json"
if v.mode=="apply":
 assert H(pp)==v.approved_plan_sha256.upper();plan=J(pp);assert plan["ComposerSha256"]==H(Path(__file__));dest=Path(plan["Destination"]);assert dest.is_relative_to(R/"artifacts") and not dest.exists()
 for row in plan["InputPins"]:checked(R/safe(row["Path"]),row)
 for row in plan["Files"]:checked(R/safe(row["SourcePath"]),row)
 dest.mkdir()
 for row in plan["Files"]:
  q=dest/safe(row["Path"]);q.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(R/row["SourcePath"],q)
 for row in plan["Files"]:checked(dest/row["Path"],row)
 (P/"application-receipt.json").write_bytes(B({"Status":"ACTUAL_CORRECTED_SOURCE_COMPOSITION_ONLY_NO_SDK","PlanSha256":H(pp),"Rows":len(plan["Files"]),"Destination":str(dest),"FailedFull1Unchanged":True,"MainChanged":False}));raise SystemExit
assert not pp.exists() and H(R/BASE)==BASESHA;old=J(R/BASE);mapping={};kinds={};owned=set(old["OwnedPaths"]);pins=list(old["InputPins"]);generated={}
def pin(q):pins.append({"Path":q.relative_to(R).as_posix(),"Bytes":q.stat().st_size,"Sha256":H(q)})
def add(path,q,kind):safe(path);mapping[path]=q;kinds[path]=kind
def content(path):return generated[path] if path in generated else mapping[path].read_bytes()
# Full1 inputs and raw failed outputs remain immutable; base sources refer to original sealed inputs, not generated registry.
for row in old["Files"]:q=R/row["SourcePath"];checked(q,row);add(row["Path"],q,"UnchangedFrozenFull1Input")
for row in old["StableClosure"]:checked(R/"artifacts/size-bif354-full-candidate"/row["Path"],row)
for p in (R/BASE,R/"artifacts/size-bif354-full-actual1/full.exit.json",R/"artifacts/size-bif354-full-actual1/artifacts/tests.json",R/"artifacts/size-bif354-failed-full-independent/actual-report.json"):pin(p)
assert J(R/"artifacts/size-bif354-failed-full-independent/actual-report.json")["FullAccepted"] is False
assert H(R/FOCUS)==FOCUSS;fg=J(R/FOCUS);assert fg["ManagedExecuted"]==44 and fg["SemanticPassed"]==44 and fg["SemanticFailed"]==0 and fg["ParseAll"]==36;pin(R/FOCUS)
for fp,digest in PATCHES:
 f=R/fp;assert H(f)==digest;pin(f);m=J(f)
 for row in m["Files"]:
  path=row["Path"];assert S(content(path))==row.get("ParentSourceSha256",m.get("ParentSourceSha256")),path;q=f.parent/"source"/path;checked(q,row);add(path,q,"PeerReviewedApplyErpcCorrection");owned.add(path)
for fp,digest in OVERLAYS:
 f=R/fp;assert H(f)==digest;pin(f);m=J(f)
 if "ParentLedgerSha256" in m:assert S(content("docs/mfa-compatibility.json"))==m["ParentLedgerSha256"]
 for row in m["Files"]:
  path=safe(row["TargetPath"])
  if "ParentCandidateSha256" in row:assert S(content(path))==row["ParentCandidateSha256"]
  q=Path(row["SourcePath"]);assert q.is_relative_to(R/"artifacts");checked(q,row);add(path,q,"PeerReviewedCorrected115GraphAndPackage");owned.add(path)
ledger=json.loads(content("docs/mfa-compatibility.json"));assert len(ledger["VerifiedSourceFiles"])==115 and len(ledger["Entries"])==587
assert {s:sum(e["Status"]==s for e in ledger["Entries"]) for s in old["CompatibilityCounts"]}==old["CompatibilityCounts"]
required=set(old["ReadinessInputs"])
for row in ledger["VerifiedSourceFiles"]:assert S(content(row["Path"]))==row["Sha256"],row["Path"]
for entry in ledger["Entries"]:
 for row in entry["Evidence"]:
  path="docs/"+safe(row["Path"])
  if path not in mapping:add(path,R/path,"InheritedTransitiveEvidence")
  assert S(content(path))==row["Sha256"];required.add(path)
planstats=[]
for path,wf,seal in PLANS:
 manifest=json.loads(content(path));oldmanifest=json.loads(content(path))
 for row in manifest["Files"]:
  data=content(row["Path"]);data=data.replace(b"\r\n",b"\n") if row["HashMode"]=="TextLF" else data;row["Bytes"]=len(data);row["Sha256"]=S(data)
 generated[path]=B(manifest);generated[wf]=re.sub(rb"("+seal.encode()+rb": )([A-F0-9]{64})",lambda m:m[1]+S(generated[path]).encode(),content(wf));owned.update((path,wf));planstats.append({"Path":path,"Rows":len(manifest["Files"]),"AllOldPoliciesAndNonFilesPreserved":True});assert [(x["Path"],x["HashMode"]) for x in manifest["Files"]]==[(x["Path"],x["HashMode"]) for x in oldmanifest["Files"]]
for path,wf,seal in PLANS:
 for row in json.loads(content(path))["Files"]:
  data=content(row["Path"]);data=data.replace(b"\r\n",b"\n") if row["HashMode"]=="TextLF" else data;assert S(data)==row["Sha256"] and len(data)==row["Bytes"],row["Path"]
 assert re.findall(seal.encode()+rb": ([A-F0-9]{64})",content(wf))==[S(content(path)).encode()]
for path,data in generated.items():q=P/"generated"/path;q.parent.mkdir(parents=True,exist_ok=True);q.write_bytes(data);add(path,q,"CorrectedNativeSourceRowsAndSeals")
rows=[{"Path":p,"SourcePath":q.relative_to(R).as_posix(),"Bytes":q.stat().st_size,"Sha256":H(q),"Kind":kinds[p]} for p,q in sorted(mapping.items())];stable=[x for x in rows if x["Path"]!=REG]
focusinputs=R/"artifacts/size-bif354-focus-execution-proposal14/execution-inputs.json";pin(focusinputs);fr={r["Path"]:r for r in J(focusinputs)["Rows"]};code=[r for r in rows if r["Path"].startswith(("src/","tests/Erlang.Tests/","tools/Erlang.Differential/"))]
for row in code:assert (row["Bytes"],row["Sha256"])==(fr[row["Path"]]["Bytes"],fr[row["Path"]]["Sha256"]),row["Path"]
new=dict(old);new.update(Status="REVIEWABLE_CORRECTED_SOURCE_ONLY_FULL354_ATTEMPT2_NOT_APPLIED",Destination=str(R/"artifacts/size-bif354-full-candidate2"),ComposerSha256=H(Path(__file__)),InputPins=pins,Files=rows,StableClosure=stable,OwnedPaths=sorted(owned),PackageResources=[next(r for r in rows if r["Path"]==p["Path"]) for p in old["PackageResources"]],RegistryBefore=next(r for r in rows if r["Path"]==REG),ReadinessInputs=sorted(required),VerifiedSourceGuardCount=115,NativePlans=planstats,AcceptedFocusGuardSha256=FOCUSS,FailedFull1PlanSha256=BASESHA,FocusedProductionAndTestInputRowsIdentical=len(code),ExpectedFullTestsIfPasses=7342,PrivateRpcDepthNotPermanent=True,FailedFull1Immutable=True)
pp.write_bytes(B(new));print({"PlanSha256":H(pp),"Rows":len(rows),"StableRows":len(stable),"Owned":len(owned),"CodeRows":len(code),"Plans":planstats})
