"""Acquire driver build evidence; admission is ONLY the caller-approved hash later."""
import argparse,json,sys
from pathlib import Path
from common import *

def main():
    p=argparse.ArgumentParser()
    for key in ['repository','output','runtime-manifest','dotnet']:p.add_argument('--'+key,type=Path,required=True)
    for key in ['expected-head','plan-sha256','approved-runtime-manifest-sha256']:p.add_argument('--'+key,required=True)
    a=p.parse_args();repo=a.repository.resolve();head=select_head(repo,a.expected_head);packet=Path(__file__).resolve().parent
    guard(repo,packet,a.plan_sha256,'managed-inputs.json')
    retained=checked(a.runtime_manifest,a.approved_runtime_manifest_sha256);runtime=json.loads(retained.read_bytes())
    need(runtime['Status']=='ROOT_REVIEWED_CURRENT_RUNTIME_N2' and runtime['Head']==head and runtime['WorkingTreeSnapshot'] and runtime['BuildExitCode']==0,'Explicit genuine current runtime')
    need(a.dotnet.resolve()==Path(runtime['RuntimeHost']['Path']).resolve() and sha(a.dotnet)==runtime['RuntimeHost']['Sha256'],'Authenticated host')
    for item in runtime['Files']:checked(item['Path'],item['Sha256'])
    sources=rows(p for p in (packet/'driver').glob('*') if p.is_file());need(len(sources)==3,'Three exact driver inputs')
    out=fresh(repo,a.output);env=environment();stage=stages(repo,sys.platform.startswith('linux'),env);errors=[];files=[];evidence=None;exit_code=None
    try:
        target=out/'bin';obj=out/'obj'
        stage([a.dotnet,'build',packet/'driver/Env709.csproj','-c',runtime['Configuration'],'-o',target,'-p:RuntimeDirectory='+runtime['RuntimeDirectory'],'-p:BaseIntermediateOutputPath='+str(obj)+'/', '--nologo'],out,'build-driver',180)
        exit_code=0
        files=rows(target/name for name in ['Env709.dll','Env709.deps.json','Env709.runtimeconfig.json'])
        log=out/('build-driver.stdout' if sys.platform.startswith('linux') else 'build-driver.stdout.log');evidence=row(log)
    except Exception as error:errors.append(repr(error))
    finally:
        try:
            verify(sources);checked(retained,a.approved_runtime_manifest_sha256)
            for item in runtime['Files']:checked(item['Path'],item['Sha256'])
            guard(repo,packet,a.plan_sha256,'managed-inputs.json')
        except Exception as error:errors.append('final-guard: '+repr(error))
    receipt=dict(Status='ENV709_DRIVER_BUILD',Head=head,Passed=exit_code==0 and not errors,BuildExitCode=exit_code,BuildEvidence=evidence,RuntimeManifestSha256=a.approved_runtime_manifest_sha256.upper(),SourceFiles=sources,Files=files,Errors=errors,ExecutionApproved=False)
    save(out/'build-receipt.json',receipt);return 0 if receipt['Passed'] else 1
if __name__=='__main__':raise SystemExit(main())
