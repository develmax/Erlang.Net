"""Only predeclared saved-entry fields; complete outcomes are never rewritten."""
def frame(outcome, function):
 assert outcome[0]=='tuple' and len(outcome[1])==4
 tag,kind,reason,stack=outcome[1]
 assert (tag,kind,reason)==(('atom','error'),('atom','error'),('atom','badarg'))
 assert stack[0]=='list' and stack[1]
 first=stack[1][0];assert first[0]=='tuple' and len(first[1])==4
 assert first[1][:2]==(('atom','lists'),('atom',function))
 return first

def snapshot(args,count,function):
 assert args[0]=='list' and len(args[1])==2
 if function=='reverse':
  suffix,acc=args[1];assert acc[0]=='list';k=len(acc[1])
  assert all(x==('integer',k-i) for i,x in enumerate(acc[1]))
 else:
  item,suffix=args[1];assert item==('atom','missing')
  k=count-len(suffix[1]);assert k%16==0
 assert suffix[0]=='improper' and suffix[2]==('atom','tail') and 0<=k<count
 assert len(suffix[1])==count-k
 assert all(x==('integer',k+i+1) for i,x in enumerate(suffix[1]))
 return {'Consumed':k,'Remaining':count-k,'EveryIntegerVerified':True,'Terminal':'tail','OwnEntryState':True}

def compare(a,b,count,function,binding):
 assert binding is not None and binding.get('ActualOwnTrapEntrySnapshotsSourceReviewed') is True
 assert binding.get('EveryBudgetHeapStateDifferenceExplained') is True and binding.get('NativePartitionSeededIntoMain') is False
 af,bf=frame(a,function),frame(b,function)
 assert a[1][:3]==b[1][:3] and len(a[1][3][1])==len(b[1][3][1])
 assert a[1][3][1][1:]==b[1][3][1][1:]
 assert af[1][:2]==bf[1][:2] and af[1][3]==bf[1][3]
 x=snapshot(af[1][2],count,function);y=snapshot(bf[1][2],count,function)
 return {'Accepted':True,'OriginalSnapshot':x,'SdkSnapshot':y,'OtherWholeFieldsExact':True,'StrictArgumentsEqual':af[1][2]==bf[1][2]}
