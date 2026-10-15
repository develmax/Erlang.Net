"""Offline terminal evidence review; reads downloaded GET snapshots/ZIP; no VM/SDK/network."""
import argparse
from datetime import datetime, timedelta
import hashlib
import importlib.util
import json
from pathlib import Path, PurePosixPath
import re
import subprocess
import sys
import zipfile

sys.dont_write_bytecode = True
p = argparse.ArgumentParser()
for name in ['repository', 'snapshot', 'artifact-zip', 'output']:
    p.add_argument('--' + name, type=Path, required=True)
p.add_argument('--expected-head', required=True)
p.add_argument('--expected-run', required=True)
args = p.parse_args()
here = Path(__file__).resolve().parent
SEAL = 'C73AFABAF43F98F81BD94C52788189429F85931C3D7228C8C8346C38FCEDD0F5'
PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
TOOL = 'tools/Erlang.AllocatorMemoryObservations'

def require(value, message):
    if not value:
        raise RuntimeError(message)

def load(path):
    return json.loads(path.read_bytes())

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

require(re.fullmatch('[0-9a-f]{40}', args.expected_head) and args.expected_run.isdigit(), 'Explicit immutable expected head and run required')
root = args.repository.resolve()
snapshot = args.snapshot.resolve()
run, jobs, artifacts = [load(snapshot / (n + '.json')) for n in ['run', 'jobs', 'artifacts']]
attempt = load(here / 'dispatch-attempt.json')
require(attempt['ExpectedHead'] == args.expected_head and attempt['ExecutionPlanSha256'] == SEAL, 'Dispatch attempt binding')
require(datetime.fromisoformat(run['created_at'].replace('Z', '+00:00')) >= datetime.fromisoformat(attempt['StartedAt'].replace('Z', '+00:00')) - timedelta(seconds=5), 'Run must follow recorded dispatch attempt')
require(str(run['id']) == args.expected_run and run['head_sha'] == args.expected_head and run['event'] == 'workflow_dispatch', 'Terminal run identity')
require(run['workflow_id'] == attempt['WorkflowId'] and run['path'] == attempt['WorkflowPath'] == '.github/workflows/allocator-memory-original-observations.yml', 'Terminal workflow identity')
require(run['status'] == 'completed' and run['conclusion'] == 'success' and run['run_attempt'] == 1, 'Successful original attempt1 terminal state required')
require(jobs['total_count'] == len(jobs['jobs']) == 1, 'One complete bounded original job required')
job = jobs['jobs'][0]
require(str(job['run_id']) == args.expected_run and job['head_sha'] == args.expected_head and job['run_attempt'] == 1, 'Terminal job identity')
require(job['name'] == 'original-allocator-memory' and job['status'] == 'completed' and job['conclusion'] == 'success', 'Original job success required')
required_steps = {
    'Verify reviewed complete execution closure', 'Verify passive synthetic decoder and report controls',
    'Build exact pinned original and bind tools and source headers',
    'Capture controls and base18 plus one full-info fresh original VM cases',
    'Verify original typed assertions and preserve physical observations',
}
require(required_steps.issubset({s['name'] for s in job['steps'] if s['status'] == 'completed' and s['conclusion'] == 'success'}), 'Every original gate must succeed')
require(artifacts['total_count'] == len(artifacts['artifacts']), 'Artifact metadata page closure')
matching = [a for a in artifacts['artifacts'] if a['name'] == 'allocator-memory-original-nineteen-observations']
require(len(matching) == 1 and not matching[0]['expired'], 'Exactly one nonexpired original19 artifact required')
artifact = matching[0]
if artifact.get('workflow_run'):
    require(str(artifact['workflow_run']['id']) == args.expected_run and artifact['workflow_run']['head_sha'] == args.expected_head, 'Artifact metadata run binding')
zip_path = args.artifact_zip.resolve()
require(zip_path.stat().st_size <= 256 * 1024 * 1024, 'Bounded complete ZIP required')
zip_sha = sha(zip_path)
download = load(snapshot / 'download-hashes.json')
require(download['ExpectedHead'] == args.expected_head and str(download['RunId']) == args.expected_run and download['RunAttempt'] == 1 and download['ArtifactId'] == artifact['id'] and download['ArtifactZipSha256'] == zip_sha, 'Captured download identity and archive hash')
require(sha(snapshot / 'terminal-logs.zip') == download['TerminalLogsZipSha256'], 'Preserved complete terminal log archive')
digest = artifact.get('digest')
if digest:
    require(digest.lower() == 'sha256:' + zip_sha.lower(), 'Downloaded ZIP must match GitHub artifact digest')
subprocess.run([sys.executable, '-B', str(here / 'validate-frozen-source.py'), '--repository', str(root)], check=True)
require(sha(root / TOOL / 'reviewed-execution-plan.json') == SEAL, 'Reviewed exact execution source seal')
spec = importlib.util.spec_from_file_location('reviewed_allocator_runner', root / TOOL / 'run-reviewed.py')
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)
out = args.output.resolve()
require(not out.exists() and not out.is_relative_to(root / 'tools') and not out.is_relative_to(snapshot), 'Fresh output outside source/snapshot required')
out.mkdir(parents=True)
raw = out / 'raw'
raw.mkdir()
members = []
with zipfile.ZipFile(zip_path) as archive:
    entries = archive.infolist()
    require(len(entries) <= 1000 and sum(i.file_size for i in entries) <= 512 * 1024 * 1024, 'Bounded full archive members')
    seen = set()
    for entry in entries:
        name = PurePosixPath(entry.filename)
        normalized = name.as_posix().casefold()
        require(not name.is_absolute() and '..' not in name.parts and '\\' not in entry.filename and ':' not in entry.filename and normalized not in seen, 'Safe unique archive path')
        require(all(part.rstrip(' .') == part and not re.fullmatch(r'(con|prn|aux|nul|com[1-9]|lpt[1-9])(?:\..*)?', part, re.I) for part in name.parts), 'Portable archive member names required')
        seen.add(normalized)
        require((entry.external_attr >> 16) & 0o170000 != 0o120000, 'Archive symlink refused')
        target = (raw / name).resolve()
        require(target.is_relative_to(raw), 'Archive destination scope')
        if entry.is_dir():
            target.mkdir(parents=True, exist_ok=True)
            continue
        target.parent.mkdir(parents=True, exist_ok=True)
        data = archive.read(entry)
        with target.open('xb') as stream:
            stream.write(data)
        members.append(dict(Path=entry.filename, Bytes=len(data), Sha256=hashlib.sha256(data).hexdigest().upper()))
candidates = list(raw.rglob('reviewed-inputs.json'))
require(len(candidates) == 1, 'One complete execution evidence root required')
evidence = candidates[0].parent
preflight = load(evidence / 'reviewed-inputs.json')
require(preflight['ExecutionPlanSha256'] == SEAL and preflight['HarnessCommit'] == args.expected_head and preflight['OriginalExecuted'] is False and preflight['SdkCompared'] is False, 'Execution preflight binding')
build = load(evidence / 'build-provenance.json')
require(build['ExecutionPlanSha256'] == SEAL and build['SourceHead'] == PIN, 'Source-built original execution provenance')
require(build['HarnessCommit'] == args.expected_head and str(build['RunId']) == args.expected_run and str(build['RunAttempt']) == '1' and build['Job'] == 'original-allocator-memory', 'Build CI identity binding')
require(build['InstalledPinnedPublicHeaders'] == [dict(Name=n, GitPath=g, Bytes=(next(r['Bytes'] for r in build['InstalledPinnedPublicHeaders'] if r['Name'] == n)), Sha256=h) for n, g, h in runner.HEADERS], 'Exact source-built pinned public header bindings')
require(all(row['Bytes'] > 0 and re.fullmatch('[0-9A-F]{64}', row['Sha256']) for row in build['Files']), 'Actual built binary file digests')
require(build['ConfiguredSizeHeader']['Path'].endswith('/erl_int_sizes_config.h') and build['ConfiguredSizeHeader']['Bytes'] > 0 and re.fullmatch('[0-9A-F]{64}', build['ConfiguredSizeHeader']['Sha256']), 'Actual configured size-header binding')
for name, count in [('synthetic-projections.json', 52), ('synthetic-decoder35.json', 35), ('synthetic-stack16.json', 16)]:
    control = load(evidence / name)
    require(control['Count'] == count, 'Complete synthetic control count: ' + name)
    require(control.get('Passed', True) is True and ('Status' not in control or control['Status'].endswith('_PASS')), 'Synthetic success: ' + name)
budget = load(evidence / 'synthetic-report-budget/report.json')
require(budget['Status'] == 'SYNTHETIC_REPORT_SERIALIZATION_BUDGET_PASS' and all(budget[k] is True for k in ['PositiveReportPassed', 'ByteBudgetRefusalPassed', 'PartialEvidenceRetained', 'PartialOverwriteRefused']), 'Complete report budget controls')
report = out / 'independent-original-nineteen-report.json'
command = [sys.executable, '-B', str(root / TOOL / 'guard/verify-original19.py')]
for key, value in {
    'observations': evidence / 'observations', 'proposal': root / TOOL / 'original',
    'build-provenance': evidence / 'build-provenance.json',
    'base-guard': root / 'tools/Erlang.NifPendingVarargsObservations/verify-original13.py',
    'stack-guard': root / 'tools/Erlang.RawOnLoadVisibilityObservations/guard/verify-original9.py',
    'decoder': root / 'tools/Erlang.RawOnLoadVisibilityObservations/guard/passive_etf.py',
    'controls': evidence / 'capture-controls', 'report': report, 'expected-head': args.expected_head,
    'expected-run': args.expected_run, 'expected-attempt': '1', 'expected-job': 'original-allocator-memory',
}.items():
    command.extend(['--' + key, str(value)])
subprocess.run(command, check=True, timeout=120)
typed = load(report)
require(typed == load(evidence / 'original-nineteen-report.json'), 'Independent report must match uploaded complete typed report')
require(typed['PinnedCommit'] == PIN and len(typed['Cases']) == 19 and typed['ExtraFullAllocatorCases'] == 1 and typed['BaseAllocatorCases'] == 9 and typed['MemoryCases'] == 6 and typed['AdjacentCases'] == 3, 'Independent nineteen-case typed closure')
require(all(typed[k] is False for k in ['SdkCompared', 'SdkPass', 'GenericRuntimeParity', 'OriginalExecutedByGuard', 'DisabledAllocatorProfileCovered']), 'Bounded original-only scope')
result = dict(Status='INDEPENDENT_TERMINAL_ORIGINAL19_TYPED_ARTIFACT_PASS', ExpectedHead=args.expected_head, RunId=args.expected_run, RunAttempt=1, JobId=job['id'], ArtifactId=artifact['id'], ZipSha256=zip_sha, GitHubArtifactDigestVerified=bool(digest), ExecutionPlanSha256=SEAL, PinnedCommit=PIN, Cases=19, IndependentReportSha256=sha(report), RunSnapshotSha256=sha(snapshot / 'run.json'), JobsSnapshotSha256=sha(snapshot / 'jobs.json'), ArtifactsSnapshotSha256=sha(snapshot / 'artifacts.json'), ArchiveMembers=members, SdkCompared=False, GenericRuntimeParity=False, OriginalExecutedByReview=False)
(out / 'terminal-review.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k != 'ArchiveMembers'}))
