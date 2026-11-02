"""Describe an already successful exact-head build; never builds or admits native code."""
import argparse,hashlib,json,subprocess
from pathlib import Path

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def need(v,why):
    if not v:raise RuntimeError(why)

def main():
    ap=argparse.ArgumentParser()
    for name in ['repository','runtime-directory','source-freeze','build-evidence','dotnet','output']:ap.add_argument('--'+name,type=Path,required=True)
    for name in ['expected-head','approved-source-freeze-sha256','approved-build-evidence-sha256','framework-version','configuration']:ap.add_argument('--'+name,required=True)
    ap.add_argument('--build-exit-code',type=int,required=True);a=ap.parse_args();repo=a.repository.resolve()
    need(a.build_exit_code==0 and a.configuration in ['Release','Debug'],'Only actual successful build receipt')
    need(subprocess.check_output(['git','--no-optional-locks','-C',str(repo),'rev-parse','HEAD'],text=True).strip()==a.expected_head,'Exact current checkout')
    source=a.source_freeze.resolve();evidence=a.build_evidence.resolve();runtime=a.runtime_directory.resolve();output=a.output.resolve()
    need(sha(source)==a.approved_source_freeze_sha256.upper() and sha(evidence)==a.approved_build_evidence_sha256.upper(),'Explicit source/build evidence binding')
    freeze=json.loads(source.read_bytes());need(freeze['SubjectHead']==a.expected_head and freeze['Rows'],'Actual current source freeze')
    for row in freeze['Rows']:
        f=(repo/row['Path']).resolve();need(f.is_relative_to(repo) and sha(f)==row['Sha256'],'Changed compiled source '+row['Path'])
    need(runtime.is_relative_to(repo) and (runtime/'Erlang.Runtime.dll').is_file(),'Actual built runtime closure')
    files=[dict(Path=str(f.resolve()),Sha256=sha(f)) for f in sorted(runtime.rglob('*')) if f.is_file()]
    need(files and output.is_relative_to(repo/'artifacts/nif-upgrade693') and not output.exists(),'Fresh bounded manifest output')
    value=dict(Status='ROOT_REVIEWED_CURRENT_RUNTIME_N2',Head=a.expected_head,WorkingTreeSnapshot=True,Configuration=a.configuration,BuildExitCode=0,
        BuildEvidence=dict(Path=str(evidence),Sha256=sha(evidence)),SourceFreeze=dict(Path=str(source),Sha256=sha(source)),RuntimeDirectory=str(runtime),Files=files,
        RuntimeHost=dict(Path=str(a.dotnet.resolve()),Sha256=sha(a.dotnet.resolve()),FrameworkVersion=a.framework_version),
        NativeExecutionPermission=False,BuildPerformedByThisTool=False,ReviewRequired=True)
    output.parent.mkdir(parents=True,exist_ok=True);output.write_bytes(json.dumps(value,indent=2).encode())
    print(sha(output))
if __name__=='__main__':main()
