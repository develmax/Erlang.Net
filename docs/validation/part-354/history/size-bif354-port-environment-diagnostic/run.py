"""Root-only build/capture of immutable failed-full DLLs. No automatic privilege change."""
from pathlib import Path
import argparse,hashlib,json,os,importlib.util
R=Path(__file__).resolve().parents[2];P=Path(__file__).parent
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();J=lambda p:json.loads(p.read_bytes())
a=argparse.ArgumentParser();a.add_argument("mode",choices=["build","capture"]);a.add_argument("--approved-freeze-sha256",required=True);a.add_argument("--output",required=True);a.add_argument("--harness-dll");a.add_argument("--profile",choices=["default-environment","root-authorized-outside-sandbox"]);a.add_argument("--dotnet",default="dotnet");v=a.parse_args();freeze=P/"source-freeze.json";assert H(freeze)==v.approved_freeze_sha256.upper()
for row in J(freeze)["Files"]:
 q=P/row["Path"];assert q.stat().st_size==row["Bytes"] and H(q)==row["Sha256"]
t=J(P/"targets.json");C=R/"artifacts/size-bif354-full-candidate";plan=J(R/"artifacts/size-bif354-full-preparation/composition-plan.json");assert H(R/"artifacts/size-bif354-full-preparation/composition-plan.json")==t["FullPlanSha256"]
for row in plan["StableClosure"]:assert H(C/row["Path"])==row["Sha256"]
for row in t["ActualFullTestDlls"]:assert H(C/row["Path"])==row["Sha256"]
helper=R/"artifacts/spawn-priority351-preparation/execute-root.py";assert H(helper)=="C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A";spec=importlib.util.spec_from_file_location("owned_ports354",helper);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
o=Path(v.output).resolve();assert o.is_relative_to(R/"artifacts") and not o.exists();o.mkdir();env=dict(os.environ,DOTNET_PROCESSOR_COUNT="2",MSBUILDDISABLENODEREUSE="1",DOTNET_CLI_TELEMETRY_OPTOUT="1")
if v.mode=="build":
 assert not (P/"source/bin").exists();m.owned([v.dotnet,"build",str(P/"source/Diagnostic.csproj"),"--disable-build-servers","-m:1","-nr:false","--nologo","-v:minimal"],o,"build",120,env,R);m.write(o/"built.json",{"HarnessDll":str(P/"source/bin/Debug/net10.0/Port354Diagnostic.dll"),"Sha256":H(P/"source/bin/Debug/net10.0/Port354Diagnostic.dll"),"SourceFreezeSha256":H(freeze)});raise SystemExit
assert v.harness_dll and v.profile;dll=Path(v.harness_dll).resolve();assert dll.is_relative_to(P/"source/bin") and dll.is_file();binary=H(dll);m.write(o/"inputs.json",{"SourceFreezeSha256":H(freeze),"HarnessDllSha256":binary,"ActualFullDlls":t["ActualFullTestDlls"],"ProfileLabelOnly":v.profile,"NoAutomaticEscalationOrGlobalSettingsChange":True});results=[]
for i,name in enumerate(t["Names"]):
 d=o/("target-"+str(i).zfill(2));d.mkdir();out=d/"outcome.json"
 try:m.owned([v.dotnet,str(dll),str(C/"tests/Erlang.Tests/bin/Debug/net10.0"),name,str(out)],d,"process",90,env,C)
 except RuntimeError:
  receipt=J(d/"process.exit.json");assert receipt["Settled"] and not receipt["TimedOut"] and receipt["ExitCode"]==1 and not receipt["Failures"] and out.is_file()
 x=J(out);assert x["Name"]==name and x["Passed"]==(J(d/"process.exit.json")["ExitCode"]==0) and x["InputAssembliesUnchanged"]
 for row in x["LoadedAssemblies"]:assert any(str((C/r["Path"]).resolve()).casefold()==row["Path"].casefold() and r["Sha256"]==row["Sha256"] for r in t["ActualFullTestDlls"])
 results.append({"Name":name,"Passed":x["Passed"],"OutcomeSha256":H(out)})
assert H(dll)==binary
for row in t["ActualFullTestDlls"]:assert H(C/row["Path"])==row["Sha256"]
m.write(o/"completion.json",{"Status":"ACTUAL_NATIVEPORT16_MANAGED_DIAGNOSTIC_ACQUISITION_NOT_ENVIRONMENT_CAUSE_PROOF","Targets":results,"Passed":sum(x["Passed"] for x in results),"Failed":sum(not x["Passed"] for x in results),"ProfileLabelOnly":v.profile,"HarnessDllSha256":binary,"OriginalOtpExecuted":False,"FullValidationExecuted":False,"MainChanged":False})
