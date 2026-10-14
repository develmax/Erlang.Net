import hashlib,json,subprocess,sys
head=sys.argv[1]
def blob(path):return subprocess.run(['git','show',head+':'+path],capture_output=True,check=True).stdout
plan=json.loads(blob('tools/Erlang.NodeIdentityContrasts/source-plan.json'))
assert plan['Pin']=='ad05823719d77c8faee87348ea39513d4e2f99c5'
assert plan['Scenarios']==['start','start-stop','start-stop-restart'] and plan['PendingScenario']=='pending-handle'
for path,field in [('node_identity_probe.erl','SourceSha256'),('distribution_pending_probe.erl','PendingSourceSha256')]:
 data=blob('tools/Erlang.NodeIdentityContrasts/'+path)
 assert not data.startswith(bytes([239,187,191])) and bytes([13]) not in data
 assert hashlib.sha256(data).hexdigest().upper()==plan[field]
parameters=blob('tools/Erlang.AllocationDiagnostic/AllocationDiagnosticParameters.cs').decode()
assert 'Samples = 16;' in parameters and 'BudgetBytes = 4096;' in parameters
print('Committed269 node source hashes/pin/four declarations and diagnostic immutable budget verified')
