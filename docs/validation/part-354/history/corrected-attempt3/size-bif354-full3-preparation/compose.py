"""Source-only full3 successor. Plan/apply never runs SDK; execution gated separately."""
from pathlib import Path
import argparse,hashlib,json,re,shutil,subprocess
R=Path(__file__).resolve().parents[2];P=Path(__file__).parent
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();S=lambda b:hashlib.sha256(b).hexdigest().upper();J=lambda p:json.loads(p.read_bytes());B=lambda v:(json.dumps(v,indent=2)+"\n").encode()
BASE="artifacts/size-bif354-full2-preparation/composition-plan.json";BASESHA="2E0A920C615F96CE8FE0F4BA91DFE0EDB6DF4853ABB71801E78FF67E8C602A2B"
PATCH="artifacts/halt-cleanup354-stealing-proposal";PATCHSHA="63754A3D92C96A57CCDB6126D6EB148B589384A6C3C151B957AF1FF6F3B914A0"
CONTROL="artifacts/cleanup-stealing354-permanent-registration";CONTROLSHA="BA294319C7D87D6FD38112475222F88BA595C7AED821CF8790F69E0235803456"
DISPATCHER="src/Erlang.Runtime/RuntimeActorDispatcher.cs";DISPATCHERSHA="F2BBB954CE212ED87AE16FE8F50ACA14268A9A5C9A06812E11799EB7C270AB58"
REG="docs/supported-mfas.json";LEDGER="docs/mfa-compatibility.json"
PLANS=[("tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json",".github/workflows/nif-pending-native.yml","REVIEWED_SOURCE_PLAN_SHA256"),("tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json",".github/workflows/nif-reallocation-native.yml","REVIEWED_REALLOCATION_PLAN_SHA256")]
def safe(s):
 p=Path(s);assert not p.is_absolute() and ".." not in p.parts and not any(x.lower() in (".git","bin","obj") for x in p.parts) and "license.local" not in s.lower();return s
def checked(q,row):assert q.stat().st_size==row["Bytes"] and H(q)==row["Sha256"],str(q)
a=argparse.ArgumentParser();a.add_argument("mode",choices=["plan","apply"]);a.add_argument("--package-plan");a.add_argument("--approved-package-plan-sha256");a.add_argument("--approved-plan-sha256");v=a.parse_args();assert subprocess.check_output(["git","rev-parse","HEAD"],cwd=R,timeout=10).decode().strip()=="a9ec41acf2eb5727f60c613ba8146c470f82fbb0"
pp=P/"composition-plan.json"
if v.mode=="apply":
 assert H(pp)==v.approved_plan_sha256.upper();plan=J(pp);assert plan["ComposerSha256"]==H(Path(__file__));dest=Path(plan["Destination"]);assert dest.is_relative_to(R/"artifacts") and not dest.exists()
 for row in plan["InputPins"]:checked(R/safe(row["Path"]),row)
 for row in plan["Files"]:checked(R/safe(row["SourcePath"]),row)
 dest.mkdir()
 for row in plan["Files"]:
  q=dest/safe(row["Path"]);q.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(R/row["SourcePath"],q)
 for row in plan["Files"]:checked(dest/row["Path"],row)
 (P/"application-receipt.json").write_bytes(B({"Status":"ACTUAL_SOURCE_COMPOSITION_ONLY_NO_SDK","PlanSha256":H(pp),"Rows":len(plan["Files"]),"Destination":str(dest),"PriorFailedCandidatesUnchanged":True,"MainChanged":False}));raise SystemExit
assert not pp.exists() and H(R/BASE)==BASESHA and v.package_plan and v.approved_package_plan_sha256
old=J(R/BASE);mapping={};kinds={};owned=set(old["OwnedPaths"]);pins=list(old["InputPins"]);generated={}
def pin(q):pins.append({"Path":q.relative_to(R).as_posix(),"Bytes":q.stat().st_size,"Sha256":H(q)})
def add(p,q,k):safe(p);mapping[p]=q;kinds[p]=k
def content(p):return generated[p] if p in generated else mapping[p].read_bytes()
for row in old["Files"]:q=R/row["SourcePath"];checked(q,row);add(row["Path"],q,"FrozenFull2Input")
for row in old["StableClosure"]:checked(R/"artifacts/size-bif354-full-candidate2"/row["Path"],row)
pin(R/BASE);assert H(R/PATCH/"source-freeze.json")==PATCHSHA;pin(R/PATCH/"source-freeze.json");patch=J(R/PATCH/"source-freeze.json")
for row in patch["Files"]:checked(R/PATCH/row["Path"],row)
q=R/PATCH/"source"/DISPATCHER;assert H(q)==DISPATCHERSHA and S(content(DISPATCHER))=="0AF57D1F9F65CE153129E6D9718B3F545F7CE83FFE409E6889F51957040C3555";add(DISPATCHER,q,"CleanupQueueStealingClrAdaptation");owned.add(DISPATCHER)
assert H(R/CONTROL/"source-freeze.json")==CONTROLSHA;control=J(R/CONTROL/"source-freeze.json");pin(R/CONTROL/"source-freeze.json");assert control["AddedTestCount"]==1 and control["PriorOrderedPrefix"]==7342 and control["ExpectedTotal"]==7343
assert S(content("tests/Erlang.Tests/Program.cs"))==control["ParentProgramSha256"]
for row in control["Files"]:q=R/CONTROL/"source"/row["Path"];checked(q,row);add(row["Path"],q,"SingleStyledPermanentCleanupControl");owned.add(row["Path"])
# Exactly one current sourceguard hash changes; all587 entries including16 verifiedclaims stay byte-value identical.
ledger=json.loads(content(LEDGER));before=json.loads(content(LEDGER));assert len(ledger["VerifiedSourceFiles"])==115
changed=[]
for row in ledger["VerifiedSourceFiles"]:
 if row["Path"]==DISPATCHER:row["Sha256"]=DISPATCHERSHA;changed.append(row["Path"])
assert changed==[DISPATCHER] and ledger["Entries"]==before["Entries"]
assert [r for r in ledger["VerifiedSourceFiles"] if r["Path"]!=DISPATCHER]==[r for r in before["VerifiedSourceFiles"] if r["Path"]!=DISPATCHER];generated[LEDGER]=B(ledger);owned.add(LEDGER)
packageplan=R/safe(v.package_plan);assert H(packageplan)==v.approved_package_plan_sha256.upper();pin(packageplan);pack=J(packageplan);assert len(pack["Files"])==1
for row in pack["Files"]:
 path=row["TargetPath"];assert path=="docs/OTP-PORTS.md" and S(content(path))==row["ParentCandidateSha256"];q=Path(row["SourcePath"]);assert q.is_relative_to(R/"artifacts");checked(q,row);add(path,q,"ReviewedFrozenFull3PackageNote");owned.add(path)
for row in ledger["VerifiedSourceFiles"]:assert S(content(row["Path"]))==row["Sha256"],row["Path"]
for entry in ledger["Entries"]:
 for row in entry["Evidence"]:assert S(content("docs/"+row["Path"]))==row["Sha256"]
planstats=[]
for path,wf,seal in PLANS:
 manifest=json.loads(content(path));oldmanifest=json.loads(content(path))
 for row in manifest["Files"]:
  data=content(row["Path"]);data=data.replace(b"\r\n",b"\n") if row["HashMode"]=="TextLF" else data;row["Bytes"]=len(data);row["Sha256"]=S(data)
 generated[path]=B(manifest);generated[wf]=re.sub(rb"("+seal.encode()+rb": )([A-F0-9]{64})",lambda m:m[1]+S(generated[path]).encode(),content(wf));owned.update((path,wf));planstats.append({"Path":path,"Rows":len(manifest["Files"]),"NoNewRuntimePaths":True});assert [(x["Path"],x["HashMode"]) for x in manifest["Files"]]==[(x["Path"],x["HashMode"]) for x in oldmanifest["Files"]]
for path,wf,seal in PLANS:
 for row in json.loads(content(path))["Files"]:
  data=content(row["Path"]);data=data.replace(b"\r\n",b"\n") if row["HashMode"]=="TextLF" else data;assert S(data)==row["Sha256"] and len(data)==row["Bytes"]
 assert re.findall(seal.encode()+rb": ([A-F0-9]{64})",content(wf))==[S(content(path)).encode()]
for path,data in generated.items():q=P/"generated"/path;q.parent.mkdir(parents=True,exist_ok=True);q.write_bytes(data);add(path,q,"DispatcherGuardAndNativeSealsOnly")
rows=[{"Path":p,"SourcePath":q.relative_to(R).as_posix(),"Bytes":q.stat().st_size,"Sha256":H(q),"Kind":kinds[p]} for p,q in sorted(mapping.items())];stable=[r for r in rows if r["Path"]!=REG]
prior=R/"artifacts/size-bif354-full-actual2/artifacts/tests.json";assert J(prior)["Passed"]==7341 and J(prior)["Failed"]==1 and len(J(prior)["Tests"])==7342;pin(prior)
new=dict(old);new.update(Status="REVIEWABLE_SOURCE_ONLY_FULL354_ATTEMPT3_PENDING_ROOT_RED_GREEN_ACCEPTANCE",Destination=str(R/"artifacts/size-bif354-full-candidate3"),ComposerSha256=H(Path(__file__)),InputPins=pins,Files=rows,StableClosure=stable,OwnedPaths=sorted(owned),PackageResources=[next(r for r in rows if r["Path"]==p["Path"]) for p in old["PackageResources"]],RegistryBefore=next(r for r in rows if r["Path"]==REG),NativePlans=planstats,ExpectedOldOrderedPrefix=7342,PreviousAllTestNamesSource=prior.relative_to(R).as_posix(),ExpectedNewTests=1,AddedTestNames=[control["TestName"]],ExpectedFullTestsIfPasses=7343,PreviousPassedSuiteRemains=7313,FailedFull2PlanSha256=BASESHA,RuntimePatchFreezeSha256=PATCHSHA,RuntimeSourceSha256=DISPATCHERSHA,PermanentControlFreezeSha256=CONTROLSHA,All587EntriesIncluding16ClaimsUnchanged=True,Other114SourceGuardsUnchanged=True,RequiredExecutionGate="One Root-approved actual deterministic red/green report path and hash; no full/native equivalence inferred",NativeScope="erl_process.c9881/9907/9939 guide idle stealing. CLR canceled-cleanup queue movement is managed adaptation, not native equivalence",SdkExecuted=False)
pp.write_bytes(B(new));print({"PlanSha256":H(pp),"Rows":len(rows),"StableRows":len(stable),"Owned":len(owned),"FullIfPasses":7343})
