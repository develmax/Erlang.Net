"""Full typed outcomes; one declared NEW_FUN creator relation, never a generic PID waiver."""
def info_fields(node):
    assert node[0]=='list'
    rows=[]
    for row in node[1]:
        assert row[0]=='tuple' and len(row[1])==2 and row[1][0][0]=='atom'
        rows.append((row[1][0][1],row[1][1]))
    assert len({k for k,v in rows})==len(rows)
    return rows

def declared_fun_result(left,right,sdk_init,expected_module):
    assert left[0]==right[0]=='tuple' and len(left[1])==len(right[1])==4
    nf,sf=left[1][0],right[1][0]
    assert nf[0]==sf[0]=='new_fun' and len(nf)==len(sf)==11
    # Raw span is decoder transport bookkeeping, not an Erlang field; the caller validates it against raw bytes.
    assert nf[2]==sf[2]==0 and nf[3]==sf[3] and nf[3] in [bytes(16),b'\xff'*16]
    assert nf[4]==sf[4]==0 and nf[5]==sf[5]==('atom',expected_module)
    assert nf[6]==sf[6]==('integer',0) and nf[7]==sf[7] and nf[7] in [('integer',1),('integer',2)] and nf[9]==sf[9]==()
    assert left[1][1:3]==right[1][1:3] and all(x in [('atom','true'),('atom','false')] for x in left[1][1:3])
    ni=info_fields(left[1][3]);si=info_fields(right[1][3]);assert [k for k,v in ni]==[k for k,v in si]
    nd=dict(ni);sd=dict(si)
    assert nd['pid']==nf[8] and sd['pid']==sf[8]==sdk_init
    for (nk,nv),(sk,sv) in zip(ni,si):
        assert nk==sk
        if nk!='pid':assert nv==sv,(nk,nv,sv)
    return dict(Kind='declared-NEW_FUN-own-init-pid',NativeSerializedCreatorMatchesActualFunInfo=True,SdkSerializedCreatorMatchesActualFunInfoAndIndependentRuntimeInit=True,OtherFieldsExact=True,NativeCreator=repr(nf[8]),SdkCreator=repr(sf[8]))

def compare_outcome(left,right,allow_relation,sdk_init,expected_module):
    if left==right:return 'strict',None
    assert left[0]==right[0]=='tuple' and len(left[1])==len(right[1])
    assert left[1][0]==right[1][0]==('atom','ok') and len(left[1])==2 and allow_relation
    return 'relation',declared_fun_result(left[1][1],right[1][1],sdk_init,expected_module)
