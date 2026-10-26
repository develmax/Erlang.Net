"""Passive provider/call witnesses; does not infer whole-libc compatibility."""
from pathlib import Path
import json

R=Path(__file__).resolve().parents[3]
root=R/'artifacts/linux-callback-pthread-control';expected=['pthread_create','pthread_join','pthread_key_create','pthread_key_delete','pthread_getspecific','pthread_setspecific','__cxa_thread_atexit_impl','pthread_exit']
reports=[]
for route in ['reviewed','ordinary']:
    wrappers=[];base_providers=None
    for domain in ['first','second']:
        path=root/'sdk'/route/domain/'pthread-provider-witness.json';witness=json.loads(path.read_bytes())
        bindings=[json.loads(value) for value in witness['Bindings']];calls=json.loads(witness['Calls'])
        assert len(bindings)==2 and [row['Name'] for row in calls]==expected
        for binding in bindings:
            assert [row['Name'] for row in binding]==expected
            assert all(row['CapsuleLookupMatchesWrapper'] and row['BaseDiffersFromWrapper'] for row in binding)
            assert all(Path(row['BaseOwner']['Path']).name=='libc.so.6' for row in binding)
            providers=[row['BaseProvider'] for row in binding]
            if base_providers is None:base_providers=providers
            else:assert providers==base_providers
        assert bindings[0]==bindings[1]
        assert calls[0]['Calls']>0 and calls[0]['Calls']==calls[1]['Calls']
        wrappers.append(bindings[0][0]['WrapperOwner']['Base'])
        reports.append(dict(Route=route,Domain=domain,Calls=calls,Bindings=bindings))
    assert len(set(wrappers))==2
out=root/'provider-acceptance.json';assert not out.exists()
out.write_text(json.dumps(dict(Status='ACTUAL_FIXED_HOST_PROVIDER_CONTROL_ACCEPTED',LibraryPathsCopied=False,TargetNamespace='NEWLM',ProviderSurface=expected,CppTlsAndHiddenCallsVerified=False,WholeLibcCompatibilityClaimed=False,Rows=reports),indent=2)+'\n')
