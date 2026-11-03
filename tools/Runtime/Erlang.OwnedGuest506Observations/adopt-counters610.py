"""Admit only an executed counters-family596 compiled-worker job with exact stock-owner evidence.

Reuses OwnedGuest506's existing completion/inventory verifier. No VM is started.
"""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import sys

sys.dont_write_bytecode = True
PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
TOOL = 'tools/Runtime/Erlang.OwnedGuest506Observations'
JOB = 'counters-family596'
SUBJECT = 'counters_family591'


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()


def require(value, why):
    if not value:
        raise ValueError(why)


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(value)
    return value


def admit_source(repo, tool, path, expected):
    require(path == tool/'source-plan.json' and sha(path) == expected.upper(), 'Exact reviewed source plan required')
    plan = json.loads(path.read_bytes())
    require(plan['PinnedCommit'] == PIN and plan['ToolPath'] == TOOL, 'Source profile differs')
    require(len({(r['Scope'],r['Path']) for r in plan['Inputs']}) == len(plan['Inputs']), 'Duplicate source rows')
    load('collect588_input_check',tool/'collect.py').check_inputs(repo,tool,plan)
    required = {'collect.py','stock-owners588.py','adopt-counters610.py','admitted_receipts.py','jobs.json',
                'fixtures/counters_family591.erl','fixtures/contract_worker513.erl'}
    bound = {r['Path'] for r in plan['Inputs'] if r['Scope']=='Tool' and r['HashMode']=='Raw'}
    require(required.issubset(bound), 'All executable observer/admission/stock bytes must be raw sealed')
    return plan


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--repository',type=Path,required=True)
    p.add_argument('--completion',type=Path,required=True)
    p.add_argument('--completion-sha256',required=True)
    p.add_argument('--source-plan',type=Path,required=True)
    p.add_argument('--source-plan-sha256',required=True)
    p.add_argument('--output',type=Path,required=True)
    args=p.parse_args()
    repo=args.repository.resolve();tool=Path(__file__).resolve().parent
    require(tool == repo/TOOL,'Integrated tool path required')
    source=args.source_plan.resolve();completion=args.completion.resolve();out=args.output.resolve()
    require(completion.is_relative_to(repo/'artifacts') and out.is_relative_to(repo/'artifacts') and not out.exists(),'Fresh confined evidence required')
    admit_source(repo,tool,source,args.source_plan_sha256)
    helper=load('owned506_receipts588',tool/'admitted_receipts.py')
    acquisition=helper.original_job(completion,args.completion_sha256.upper(),args.source_plan_sha256.upper(),PIN,JOB)
    c=json.loads(completion.read_bytes());require(not c['Errors'] and not c['Phases'][JOB].get('TimedOut',False),'Failed original job')
    files=acquisition['Artifacts']
    required={'counters_family591.erl','counters_family591.beam','contract_worker513.erl','contract_worker513.beam',
        'stock-owners-initial-preloaded588.etf','stock-load-receipts588.etf','stock-loaded-native-witness588.etf',
        'stock-images588.json','stock-atomics.beam','stock-counters.beam','stock-owners-before588.etf','stock-owners-after588.etf'}
    require(required.issubset(files),'Genuine stock/worker evidence absent')
    for name in ['counters_family591.erl','contract_worker513.erl']:
        require(files[name].read_bytes()==(tool/'fixtures'/name).read_bytes(),'Subject/worker bytes differ')
    require(files['stock-owners-before588.etf'].read_bytes()==files['stock-owners-after588.etf'].read_bytes(),'Running stock owner changed')
    before=helper.read_bound(completion.parent/'provider-before.json',c['ProviderBeforeSha256'])
    inventory=before['Inventory']
    if isinstance(inventory,list):
        inventory={row['Path']:row.get('Sha256',row.get('TargetSha256')) for row in inventory if row.get('Sha256',row.get('TargetSha256'))}
    stock_index=json.loads(files['stock-images588.json'].read_bytes())
    stock_policy=load('stock_policy598',tool/'stock-owners588.py')
    profile=stock_index['ExecutionProfile']
    require(stock_index['Schema']==2 and profile in (stock_policy.PROFILE,stock_policy.SOURCE_BUILT_PROFILE),'Explicit genuine loaded profile required')
    require((profile==stock_policy.SOURCE_BUILT_PROFILE)==(before['Profile']==stock_policy.SOURCE_BUILT_PROVIDER),'Initial stock identity policy does not match the admitted provider')
    rows=stock_index['Images']
    require([r['Module'] for r in rows]==['atomics','counters'],'Exact stock pair required')
    for row in rows:
        require(row['CopiedFile']=='stock-'+row['Module']+'.beam' and row['CopiedFile'] in files,'Unexpected copied owner')
        require(inventory.get(row['ProviderRelativePath'])==row['Sha256']==sha(files[row['CopiedFile']]),'Copied stock not in same admitted provider')
    def pathvalue(path):
        path=path.resolve();require(path.is_relative_to(repo),'Evidence escapes repository');return path.relative_to(repo).as_posix()
    evidence={}
    def bind(path):
        name=pathvalue(path);value=sha(path);require(name not in evidence or evidence[name]==value,'Evidence alias differs');evidence[name]=value;return name
    worker=dict(PinnedCommit=PIN,Job=JOB,OriginalAcquisitionPassed=True,SourcePlanSha256=args.source_plan_sha256.upper())
    for key,path in {
        'Source':files[SUBJECT+'.erl'],'Beam':files[SUBJECT+'.beam'],'WorkerSource':files['contract_worker513.erl'],
        'WorkerBeam':files['contract_worker513.beam'],'Raw':files['observed.etf'],'Deterministic':files['observed-deterministic.etf'],
        'Completion':completion,'Inventory':completion.parent/JOB/'artifacts.json',
        'ProviderBefore':completion.parent/'provider-before.json','ProviderAfter':completion.parent/'provider-after.json'}.items():
        worker[key+'Path']=bind(path);worker[key+'Sha256']=sha(path)
    for path in files.values():bind(path)
    bind(completion.parent/'launch-plan.json');bind(source)
    stocks=[dict(Module=r['Module'],BeamPath=bind(files[r['CopiedFile']]),BeamSha256=r['Sha256'],ProviderPath=r['ProviderPath']) for r in rows]
    result=dict(StockExecutionProfile=profile,
        InitialPreloadedOwnersPath=bind(files['stock-owners-initial-preloaded588.etf']),
        StockLoadReceiptsPath=bind(files['stock-load-receipts588.etf']),
        StockNativeWitnessPath=bind(files['stock-loaded-native-witness588.etf']),
        Worker=worker,Stock=stocks,WorkerOwnersPath=bind(files['executing-owners.etf']),StockOwnersBeforePath=bind(files['stock-owners-before588.etf']),
        StockOwnersAfterPath=bind(files['stock-owners-after588.etf']),Evidence=[dict(Path=k,Sha256=v) for k,v in sorted(evidence.items())])
    admit_source(repo,tool,source,args.source_plan_sha256)
    require(sha(completion)==args.completion_sha256.upper(),'Completion changed during adoption')
    for row in result['Evidence']:require(sha(repo/row['Path'])==row['Sha256'],'Evidence changed during adoption')
    out.parent.mkdir(parents=True,exist_ok=True)
    with out.open('xb') as stream:stream.write((json.dumps(result,indent=2)+'\n').encode())
    print(json.dumps(dict(AdoptionSha256=sha(out),Executed=False,RawEtfRewritten=False,WholeContractsClosed=0)))


if __name__=='__main__':main()
