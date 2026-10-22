"""Passive source-function identity binding. No fixture execution or value rewriting."""
def bind_named(original,sdk,original_rows,sdk_bindings,original_md5,sdk_module_md5,sdk_pid):
    assert original[0]==sdk[0]=='new_fun' and len(original)==len(sdk)==11
    assert original[5]==sdk[5] and original[5][0]=='atom'
    name=original[5][1]
    original_entries=[r for r in original_rows if r['Index']==original[4]]
    assert len(original_entries)==1
    source=original_entries[0]
    assert original[2]==source['Arity'] and len(original[9])==source['FreeVariables']
    assert original[3]==original_md5
    assert original[7]==('integer',int.from_bytes(original_md5[:4],'big')>>5)
    assert original[7]==('integer',source['OldUnique'])
    assert original[6]==('integer',source['Index'])
    matches=[r for r in sdk_bindings if r['Module']==name and r['Index']==sdk[4] and r['Arity']==sdk[2] and r['UniqueHex']==sdk[3].hex().upper() and r['OldIndex']==sdk[6][1] and r['OldUnique']==sdk[7][1]]
    assert matches
    for r in matches:
        assert r['ResolverBound'] and r['NamedDefinitionExists']
        assert r['ResolvedNamedFunction']==source['Name'] and r['Name']==r['ResolvedName']==source['Name']
        assert r['EnvironmentCount']==len(sdk[9])==source['FreeVariables']
        assert r['ResolvedAnonymousBodySha256'] is None
    assert sdk[3]==sdk_module_md5, 'RAW_FUN_UNIQUE_DIFFERS_FROM_ACTUAL_SOURCE_MODULE_MD5'
    assert sdk[7]==('integer',int.from_bytes(sdk_module_md5[:4],'big')>>5)
    assert sdk[8]==sdk_pid and original[8]==sdk[8]
    assert original[1]==sdk[1] and original[2]==sdk[2] and original[9]==sdk[9]
    return dict(Module=name,SourceName=source['Name'],Arity=source['Arity'],OriginalIndex=original[4],SdkIndex=sdk[4],OriginalOldIndex=original[6][1],SdkOldIndex=sdk[6][1],EachCodeTableBound=True,OwnModuleMd5MatchesRawFun=True,OriginalPidExactSdkActualInitializationPid=True)
