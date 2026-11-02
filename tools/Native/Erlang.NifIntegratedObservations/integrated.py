"""Exact-source integrated NIF CI helper; no source/runtime/native synthesis."""
from pathlib import Path
import argparse, hashlib, importlib.util, json, os, re, shutil, subprocess, sys, urllib.request, urllib.error, urllib.parse, zipfile
sys.dont_write_bytecode = True
PIN = "ad05823719d77c8faee87348ea39513d4e2f99c5"
TOOL = "tools/Native/Erlang.NifIntegratedObservations"

def need(value, message):
    if not value: raise RuntimeError(message)
def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()
def row(path): return dict(Path=str(Path(path).resolve()), Sha256=sha(path))
def save(path, value):
    with Path(path).open("x", encoding="utf-8") as stream: json.dump(value, stream, indent=2)
def load(name, path):
    spec=importlib.util.spec_from_file_location(name,path);module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module

def guard(a):
    repo=a.repository.resolve(); planpath=a.plan.resolve()
    need(sys.platform.startswith("linux") and os.uname().machine=="x86_64", "Actual Linux x64 required")
    need(re.fullmatch("[0-9a-f]{40}",a.expected_head) is not None,"Exact head required")
    need(subprocess.check_output(["git","--no-optional-locks","-C",str(repo),"rev-parse","HEAD"],text=True,timeout=20).strip()==a.expected_head,"Actual checkout differs")
    need(not os.environ.get("GITHUB_SHA") or os.environ["GITHUB_SHA"]==a.expected_head,"Workflow head differs")
    need(sha(planpath)==a.plan_sha256.upper(),"Final integrated plan changed")
    plan=json.loads(planpath.read_bytes());need(plan["ReadyForRootReviewedExecution"] is True and plan["Pin"]==PIN,"Unsealed template cannot execute")
    need(plan["Inputs"] and len(plan["Inputs"])==len({i["Path"] for i in plan["Inputs"]}),"Explicit duplicate-free final inputs required")
    need(len(plan["DriverSources"])==6,"Exact reviewed620 driver source set")
    for item in plan["DriverSources"]:
        need(sha(repo/item["Path"])==item["Sha256"],"Driver source differs")
    for item in plan["Inputs"]:
        path=(repo/item["Path"]).resolve();need(path.is_relative_to(repo) and path.is_file() and sha(path)==item["Sha256"],"Changed source input "+item["Path"])
    for key,item in plan["PlanPins"].items():
        need(re.fullmatch("[0-9A-F]{64}",item["Sha256"]) is not None and sha(repo/item["Path"])==item["Sha256"],"Unsealed dependency "+key)
        need(os.environ.get(key,item["Sha256"])==item["Sha256"],"Workflow/dependency pin differs "+key)
    return repo,plan

def fresh(repo,path):
    path=path.resolve();need(path.is_relative_to(repo/"artifacts/nif-upgrade693") and not path.exists(),"Fresh confined output required");path.mkdir(parents=True);return path

def checked(item):
    path=Path(item["Path"]);need(path.is_file() and sha(path)==item["Sha256"],"Changed admitted artifact");return path

def assets(a,repo,plan):
    out=fresh(repo,a.output); original=plan["Original582"]
    # Only API requests carry the GitHub token. A signed redirect never receives it.
    def request(url,auth=False):
        headers={"Accept":"application/vnd.github+json","User-Agent":"Erlang-Net-source-bound-init582"}
        if auth: headers["Authorization"]="Bearer "+os.environ["GITHUB_TOKEN"]
        return urllib.request.Request(url,headers=headers)
    repository=os.environ["GITHUB_REPOSITORY"];need(re.fullmatch(r"[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+",repository),"Repository identity")
    artifactid=str(a.artifact_id);need(artifactid.isdigit(),"Exact original artifact ID required")
    base="https://api.github.com/repos/"+repository+"/actions/artifacts/"+artifactid
    with urllib.request.urlopen(request(base,True),timeout=30) as response: metadata=json.load(response)
    need(not metadata["expired"] and metadata["workflow_run"]["id"]==original["RunId"] and metadata["workflow_run"]["head_sha"]==original["Head"],"Wrong original run/head")
    class NoRedirect(urllib.request.HTTPRedirectHandler):
        def redirect_request(self,*unused): return None
    try:
        response=urllib.request.build_opener(NoRedirect).open(request(base+"/zip",True),timeout=30)
    except urllib.error.HTTPError as error:
        need(error.code in (301,302,303,307,308),"Artifact API did not redirect")
        location=error.headers["Location"];error.close();url=urllib.parse.urlsplit(location)
        need(url.scheme=="https" and (bool(url.hostname) and (url.hostname.endswith(".blob.core.windows.net") or url.hostname.endswith(".actions.githubusercontent.com"))),"Unexpected artifact storage origin")
        try: response=urllib.request.urlopen(urllib.request.Request(location),timeout=60)
        except Exception: raise RuntimeError("Signed artifact storage transfer failed") from None
    with response:
        raw=response.read(original["MaximumZipBytes"]+1)
    need(len(raw)<=original["MaximumZipBytes"] and hashlib.sha256(raw).hexdigest().upper()==original["ZipSha256"],"Actual original ZIP changed")
    archive=out/"original-d935.zip";archive.write_bytes(raw)
    selected=[]
    with zipfile.ZipFile(archive) as z:
        need(len(z.infolist())==len(set(z.namelist())),"Duplicate archive members")
        for item in original["Members"]:
            info=z.getinfo(item["Member"]);need(info.file_size==item["Bytes"] and info.file_size<100000,"Unexpected selected asset size")
            path=(out/item["Member"]).resolve();need(path.is_relative_to(out),"Archive path escape")
            data=z.read(info);need(hashlib.sha256(data).hexdigest().upper()==item["Sha256"],"Original member changed")
            path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(data);selected.append(row(path))
    complete=json.loads((out/"capture/completion.json").read_bytes())
    need(complete["Passed"] and complete["OriginalOnly"] and not complete["ManagedExecuted"] and all(complete["Checks"].values()),"Original did not pass")
    save(out/"assets.json",dict(Passed=True,OriginalRun=original["RunId"],OriginalHead=original["Head"],Zip=row(archive),Files=selected,FreshCurrentHeadOriginalClaimed=False,NativeFixturesRecompiled=False))

def runtime(a):
    path=a.runtime_manifest.resolve();m=json.loads(path.read_bytes())
    need(m["Status"]=="ROOT_REVIEWED_CURRENT_RUNTIME_N2" and m["Head"]==a.expected_head and m["BuildExitCode"]==0,"Actual current runtime descriptor")
    for item in m["Files"]+[m["SourceFreeze"],m["BuildEvidence"]]: checked(item)
    freeze=json.loads(Path(m["SourceFreeze"]["Path"]).read_bytes());need(freeze["SubjectHead"]==a.expected_head and freeze["Rows"],"Actual source freeze")
    for item in freeze["Rows"]:
        path=(a.repository.resolve()/item["Path"]).resolve();need(path.is_relative_to(a.repository.resolve()),"Source escape")
        checked(dict(Path=str(path),Sha256=item["Sha256"]))
    need(a.dotnet is not None and a.dotnet.resolve()==Path(m["RuntimeHost"]["Path"]).resolve() and sha(a.dotnet)==m["RuntimeHost"]["Sha256"],"Authenticated dotnet host")
    return m

def stage_tools(repo):
    identity=load("integrated_identity",repo/"tools/Runtime/Erlang.SchedulerAffinityOriginalObservations/source_inputs.py")
    owned=load("integrated_owned",repo/"tools/Distribution/Erlang.GuestConnectedSendObservations/owned_process.py")
    entry=load("integrated_stage",repo/"tools/Native/Erlang.NifEntryOriginalObservations/common.py")
    env=identity.sanitized_environment(os.environ)
    for key in ("GITHUB_TOKEN","GH_TOKEN","LD_PRELOAD","LD_AUDIT","LD_LIBRARY_PATH","DOTNET_STARTUP_HOOKS","DOTNET_ADDITIONAL_DEPS"):env.pop(key,None)
    def stage(argv,d,name,timeout,extra=None):
        return entry.stage(owned,identity,[str(v) for v in argv],d,dict(env,**(extra or {})),d/name,timeout)
    return stage

def build(a,repo,plan):
    out=fresh(repo,a.output);m=runtime(a);stage=stage_tools(repo)
    project=repo/plan["Managed582Project"]
    result=dict(Passed=False,Head=a.expected_head,Errors=[],RuntimeManifest=row(a.runtime_manifest))
    try:
        receipt=stage([a.dotnet,"build",project,"-c","Release","--no-incremental","-m:1","-p:RuntimeDirectory="+m["RuntimeDirectory"],"-o",out/"bin"],out,"build",180)
        result.update(Passed=True,BuildEvidence=receipt,Files=[row(p) for p in sorted((out/"bin").iterdir()) if p.is_file()],SourceFiles=[row(repo/r["Path"]) for r in plan["DriverSources"]])
        need((out/"bin/ManagedInitProgress617.dll").is_file(),"Actual620 driver missing")
    except Exception as error:result["Passed"]=False;result["Errors"].append(repr(error))
    finally:
        try:guard(a);runtime(a)
        except Exception as error:result["Passed"]=False;result["Errors"].append(repr(error))
        save(out/"build-receipt.json",result)
    need(result["Passed"],"Managed582 driver build failed")

def replay(a,repo,plan):
    out=fresh(repo,a.output);m=runtime(a);stage=stage_tools(repo)
    buildpath=a.driver_build.resolve();d=json.loads(buildpath.read_bytes());need(d["Passed"] and d["Head"]==a.expected_head and d["RuntimeManifest"]["Sha256"]==sha(a.runtime_manifest),"Actual driver build association")
    for item in d["Files"]+d["SourceFiles"]:checked(item)
    assetsdir=a.assets.resolve();assets=json.loads((assetsdir/"assets.json").read_bytes());need(assets["Passed"] and assets["OriginalHead"]==plan["Original582"]["Head"],"Actual original assets")
    for item in assets["Files"]:checked(item)
    checked(assets["Zip"])
    bridgepath=a.bridge_receipt.resolve();bridge=json.loads(bridgepath.read_bytes());need(bridge["SubjectHead"]==a.expected_head and bridge["Pin"]==PIN,"Actual current bridge")
    library=Path(bridge["BridgePath"]);need(sha(library)==bridge["BridgeSha256"],"Bridge changed")
    results=[];errors=[]
    for route in ("ordinary","reviewed"):
        directory=out/route;directory.mkdir();record=dict(Route=route,Passed=False)
        try:
            private=directory/"host";private.mkdir()
            for item in d["Files"]:shutil.copyfile(checked(item),private/Path(item["Path"]).name)
            destination=private/library.name
            need(not destination.exists() or sha(destination)==sha(library),"Conflicting adjacent bridge")
            if not destination.exists():shutil.copyfile(library,destination)
            artifacts={key:row(assetsdir/member) for key,member in plan["Original582"]["ArtifactKeys"].items()}
            artifacts.update(RuntimeManifest=row(a.runtime_manifest),Bridge=row(destination),BridgeReceipt=row(bridgepath))
            images=[dict(Id=name,**row(assetsdir/member),BuildReceipt=str(assetsdir/"capture/completion.json"),BuildReceiptSha256=sha(assetsdir/"capture/completion.json")) for name,member in plan["Original582"]["NativeKeys"].items()]
            inputs={i["Path"]:i for i in [*artifacts.values(),*images,*m["Files"],*d["Files"],*d["SourceFiles"],row(buildpath),row(a.runtime_manifest),row(destination)]}
            admission=dict(Status="APPROVED_MANAGED_INIT582_POSITIVE",Pin=PIN,SubjectHead=a.expected_head,Repository=str(repo),Route=route,Inputs=list(inputs.values()),Artifacts=artifacts,Images=images)
            ap=directory/"admission.json";save(ap,admission);case=directory/"capture"
            env={"ERLANG_INIT582_LOG":str(case/"events.log"),"ERLANG_INIT582_ACK":str(case/"ack"),"ERLANG_INIT582_READY":str(case/"ready")}
            result=stage([a.dotnet,"exec","--fx-version",m["RuntimeHost"]["FrameworkVersion"],private/"ManagedInitProgress617.dll",ap,sha(ap),case],directory,"managed",90,env)
            cp=case/"completion.json";capture=json.loads(cp.read_bytes());need(capture["Passed"] and capture["SubjectHead"]==a.expected_head and all(capture["Checks"].values()),"Whole managed582 positive failed")
            record.update(Passed=True,Process=result,Completion=row(cp))
        except Exception as error:record["Error"]=repr(error);errors.append(dict(Route=route,Error=repr(error)))
        results.append(record);save(directory/"route.json",record)
    try:
        guard(a);runtime(a)
        for item in assets["Files"]+d["Files"]+d["SourceFiles"]:checked(item)
    except Exception as error:errors.append(dict(Phase="final-integrity",Error=repr(error)))
    result=dict(Passed=len(results)==2 and all(r["Passed"] for r in results) and not errors,SubjectHead=a.expected_head,Rows=results,Errors=errors,WholeNineProjection=False,OriginalRun=plan["Original582"]["RunId"],ApiDelta=0,FullContractDelta=0,AllTlsFunctionsExecuted=False)
    save(out/"completion.json",result);need(result["Passed"],"One or more independent managed582 routes failed")

def aggregate(a,repo,plan):
    out=a.output.resolve();need(out.is_relative_to(repo/"artifacts/nif-upgrade693") and not out.exists(),"Fresh aggregateoutput");out.mkdir(parents=True)
    errors=[];reports={}
    required={"N1":"artifacts/nif-version651/ci/completion.json","N2Original":"artifacts/nif-upgrade693/original-linux/completion.json","N2Managed":"artifacts/nif-upgrade693/managed-linux/completion.json","N2Strict":"artifacts/nif-upgrade693/ci/completion.json","EnvOriginal":"artifacts/nif-upgrade693/env-original/completion.json","EnvManaged":"artifacts/nif-upgrade693/env-managed/completion.json","EnvStrict":"artifacts/nif-upgrade693/env-ci/completion.json","Managed582":"artifacts/nif-upgrade693/init582-managed/completion.json"}
    for key,name in required.items():
        try:
            path=repo/name;j=json.loads(path.read_bytes());need(j["Passed"] and j["SubjectHead"]==a.expected_head,key+"failed/wronghead");reports[key]=dict(Receipt=row(path),Value=j)
        except Exception as error:errors.append(dict(Group=key,Error=repr(error),MissingOrFailedPrerequisite=True))
    adjacent={}
    def adjacent_gate(name,relative,validate):
        try:
            path=repo/relative;value=json.loads(path.read_bytes())
            validate(value)
            adjacent[name]=dict(Passed=True,Receipt=row(path))
        except Exception as error:
            adjacent[name]=dict(Passed=False,Error=repr(error))
            errors.append(dict(Group=name,Error=repr(error)))
    def legacy_gate(value):
        need(value["Equal"] and len(value["Rows"])==68 and all(r["Equal"] for r in value["Rows"]),"Actual legacy68")
    def ordinary_gate(value):
        need(value["AttemptedCases"]==3 and len(value["Cases"])==3 and not value["Failures"],"Actual ordinary3")
    def provider_gate(value):
        need(len(value["Rows"])==4 and all(next(c["Calls"] for c in r["Calls"] if c["Name"]=="pthread_create")>0 and next(c["Calls"] for c in r["Calls"] if c["Name"]=="pthread_join")>0 for r in value["Rows"]),"Actual create/join")
    def binary_gate(value):
        need(value["Passed"] and value["ExpectedRows"]==38 and len(value["Profiles"])==2 and all(r["Completion"]["RuntimeClosed"] for r in value["Profiles"]),"Actual binary38 bothprofiles")
    def mutex_gate(value):
        need(value["Passed"] and len(value["Completed"])==4 and not value["Errors"],"Actual mutex4")
    adjacent_gate("Legacy68","artifacts/nif-version651/acceptance/legacy/comparison.json",legacy_gate)
    adjacent_gate("Ordinary3","artifacts/nif-version651/acceptance/ordinary3/completion.json",ordinary_gate)
    adjacent_gate("PthreadCreateJoin","artifacts/nif-version651/acceptance/legacy/provider-acceptance.json",provider_gate)
    adjacent_gate("Binary38","artifacts/gc-binary818/ci-linux01/summary.json",binary_gate)
    adjacent_gate("Mutex4","artifacts/nif-version651/mutex795-linux/completion.json",mutex_gate)
    checks={}
    def test(name,fn):
        try:checks[name]=bool(fn())
        except Exception as error:checks[name]=False;errors.append(dict(Check=name,Error=repr(error)))
    test("N1",lambda:reports["N1"]["Value"]["ExpectedComparisons"]==84 and reports["N1"]["Value"]["ExpectedModuleTextComparisons"]==8 and not reports["N1"]["Value"]["PendingGates"])
    test("N2",lambda:reports["N2Original"]["Value"]["Count"]==12 and reports["N2Managed"]["Value"]["Comparisons"]==24 and reports["N2Managed"]["Value"]["MatchedComparisons"]==24)
    test("ENV",lambda:len(reports["EnvOriginal"]["Value"]["Rows"])==2 and len(reports["EnvManaged"]["Value"]["Rows"])==4)
    test("N2Strict",lambda:reports["N2Strict"]["Value"]["OriginalCases"]==12 and reports["N2Strict"]["Value"]["Comparisons"]==24 and not reports["N2Strict"]["Value"]["Errors"] and all(v=="success" for v in reports["N2Strict"]["Value"]["Stages"].values()))
    test("EnvStrict",lambda:reports["EnvStrict"]["Value"]["OriginalCases"]==2 and reports["EnvStrict"]["Value"]["Comparisons"]==4 and not reports["EnvStrict"]["Value"]["Errors"] and all(v=="success" for v in reports["EnvStrict"]["Value"]["Stages"].values()))
    test("Managed582",lambda:len(reports["Managed582"]["Value"]["Rows"])==2)
    stages={k:v for k,v in os.environ.items() if k.startswith("COHORT_") and k.endswith("_OUTCOME")}
    if not stages or any(v!="success" for v in stages.values()):errors.append(dict(StageOutcomes=stages,Error="Skipped/failed stage remains failure; independent outputs retained"))
    passed=not errors and all(checks.values());save(out/"completion.json",dict(Passed=passed,SubjectHead=a.expected_head,Checks=checks,Errors=errors,Reports=reports,AdjacentGates=adjacent,Stages=stages,Scope="Focused N1+N2+ENV+managed582; no wholeAPI promotion",ApiDelta=0,FullContractDelta=0,AllTlsFunctionsExecuted=False,CppTlsVerified=False));need(passed,"Integrated cohort failed")

def main():
    p=argparse.ArgumentParser();p.add_argument("mode",choices=["verify","assets","build","replay","aggregate"])
    for key in ("repository","plan","output","runtime-manifest","dotnet","driver-build","assets","bridge-receipt"):p.add_argument("--"+key,type=Path,required=key in ("repository","plan","output"))
    for key in ("expected-head","plan-sha256","artifact-id"):p.add_argument("--"+key,required=key!="artifact-id")
    a=p.parse_args()
    try: repo,plan=guard(a)
    except Exception as error:
        if a.mode=="aggregate":
            repo=a.repository.resolve();out=a.output.resolve();need(out.is_relative_to(repo/"artifacts/nif-upgrade693") and not out.exists(),"Fresh failureoutput")
            out.mkdir(parents=True);save(out/"completion.json",dict(Passed=False,SourceGuardFailed=True,Error=repr(error),OriginalExecuted=False,ManagedExecuted=False))
        raise
    if a.mode=="verify":
        out=fresh(repo,a.output);save(out/"completion.json",dict(Passed=True,SourceOnly=True,SubjectHead=a.expected_head,Inputs=len(plan["Inputs"])))
    elif a.mode=="assets":assets(a,repo,plan)
    elif a.mode=="build":build(a,repo,plan)
    elif a.mode=="replay":replay(a,repo,plan)
    else:aggregate(a,repo,plan)
if __name__=="__main__":main()
