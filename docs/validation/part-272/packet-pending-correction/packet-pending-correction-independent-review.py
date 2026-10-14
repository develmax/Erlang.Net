from pathlib import Path
import json, hashlib, subprocess, re
r = Path.cwd()
p = r / 'artifacts/packet-pending-correction-prototype'
f = r / 'artifacts/packet-pending-correction-source-freeze-initial.json'
def sha(raw): return hashlib.sha256(raw).hexdigest().upper()
d = json.loads(f.read_text())
assert len(d['Files']) == 5 and d['RequiredArtifacts'] == 18 and not d['NativeExecuted']
for row in d['Files']:
    assert sha((p / row['Path']).read_bytes()) == row['Sha256'], row['Path']
    assert sha(row['SourceContent'].encode()) == row['Sha256'], row['Path']
pin = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
source = subprocess.check_output(['git', '-C', '../otp', 'show', pin + ':erts/emulator/beam/dist.c'])
s = source.decode()
for exact in ['net_kernel->flags |= F_DISTRIBUTION;', 'ERTS_PROC_SET_DIST_ENTRY(net_kernel, erts_this_dist_entry);', 'true_nk = ERTS_PROC_GET_DIST_ENTRY(net_kernel) == erts_this_dist_entry;', 'if (dep->state != ERTS_DE_STATE_PENDING)', 'erts_set_dist_entry_pending(dep);', 'TUPLE4(hp, am_dist_ctrlr, ctrlr, dep->sysname, setup_pid)']:
    assert exact in s, exact
probe = (p / 'tools/Erlang.DistributionPacketContrasts/distribution_packet_probe.erl').read_text()
assert probe.index('register(net_kernel, self())') < probe.index('erlang:setnode(packet_local@localhost, 1)') < probe.index('erts_internal:new_connection(packet_remote@localhost)') < probe.index('spawn_monitor')
assert 'term_to_binary({packet_remote@localhost, flags(), 1, Pending})' in probe
assert 'put(packet_ready_down, {observed, Reason})' in probe and 'term_to_binary(Reason)' in probe and 'error({controller_died_before_ready, Reason})' in probe
assert 'case erase(packet_ready_down) of' in probe and '{observed, Reason} ->' in probe
plan = json.loads((p / 'tools/Erlang.DistributionPacketContrasts/source-plan.json').read_text())
assert len(plan['RequiredArtifacts']) == len(set(plan['RequiredArtifacts'])) == 18
assert plan['SourceSha256'] == sha((p / 'tools/Erlang.DistributionPacketContrasts/distribution_packet_probe.erl').read_bytes()) == d['SourceSha256']
names = (p / 'tools/Erlang.DistributionPacketContrasts/PacketNames.cs').read_text()
declared = re.findall(r'"([^"]+)"', names[names.index('RequiredArtifacts'):])
declared = ['stage' if value == 'Stage' else value for value in declared]
assert set(declared) | {'stage'} == set(plan['RequiredArtifacts'])
workflow = (p / '.github/workflows/distribution-packet-contrasts.yml').read_text()
assert 'type: boolean\n        default: true' in workflow
assert workflow.count('inputs.capture_mixed') == 3
assert 'continue-on-error' not in workflow and workflow.count('git clone --depth 1 --branch OTP-29.1.1') == 1
assert "!cancelled() && (steps.packet-tool.outcome == 'success' || steps.mixed-tool.outcome == 'success')" in workflow
receipt = {'Status': 'PASS_BOUNDED_SOURCE_REVIEW', 'ReviewedFreezeSha256': sha(f.read_bytes()), 'Files': [{'Path': row['Path'], 'Sha256': row['Sha256']} for row in d['Files']], 'PinnedCommit': pin, 'PinnedDistSourceSha256': sha(source), 'KernelActivationAndPendingBeforeAdoption': True, 'MonitoredReadyFailureFullReasonPreserved': True, 'ConsumedMonitorCleanupRetainsOriginalReason': True, 'RequiredArtifacts18AndProbeHashBound': True, 'OnePinnedBuildIndependentCaptureConditions': True, 'CaptureMixedDefaultTrueFalseSkipsMixedBuildCaptureUpload': True, 'Prior271RawFailureUnmodifiedByThisReview': True, 'ProductionCodecChanges': 0, 'NativeExecuted': False, 'BuildExecutedByReviewer': False, 'MainOrGitMutation': False, 'Limits': 'Source review only; pending adoption and packet behavior require new published native run. This does not change original271 outcomes or claim TCP/epmd/handshake compatibility.'}
out = r / 'artifacts/packet-pending-correction-independent-review.json'
out.write_text(json.dumps(receipt, indent=2))
print(sha(out.read_bytes()))
print(json.dumps({key: value for key, value in receipt.items() if key != 'Files'}, indent=2))
