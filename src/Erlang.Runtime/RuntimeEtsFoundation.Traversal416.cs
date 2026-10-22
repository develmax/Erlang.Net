namespace Erlang;
internal sealed partial class RuntimeEtsFoundation
{
 internal Term CoreTraverse416(ProcessContext context,Term identifier,Term key,bool step,bool backward,bool lookup)
 {
  lock(gate)
  {
   var table=CoreResolve407(context,identifier,false);var options=coreOptions407.GetValue(table,_=>throw new InvalidOperationException(EtsIndexNames420.InvalidIndex));
   var index=table.Primary420??throw new InvalidOperationException(EtsIndexNames420.InvalidIndex);EtsKeyGroup420? selected=null;
   if(options.Kind==EtsCoreTableKind407.OrderedSet)
   {
    if(step&&key is Atom{Name:EtsTraversalNames416.End})throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
    for(int scan=0;scan<index.Ordered.Length;scan++)
    {
     context.Cancellation.ThrowIfCancellationRequested();var group=index.Ordered[backward?index.Ordered.Length-1-scan:scan];int comparison=step?TermOrder.Compare(CoreKey407(CoreRead407(group.KeyPayload,context.Cancellation),options.KeyPosition),key):0;
     if(!step||(backward?comparison<0:comparison>0)){selected=group;break;}
    }
   }
   else
   {
    EtsKeyGroup420? current=step?CoreKeyGroup420(table,key,context.Cancellation):null;
    if(step&&current is null)throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
    bool after=!step;
    foreach(var group in index.Groups)
    {
     context.Cancellation.ThrowIfCancellationRequested();
     if(!after){if(ReferenceEquals(group,current))after=true;continue;}
     if(!group.Deleted){selected=group;break;}
    }
   }
   if(selected is null)return Term.A(EtsTraversalNames416.End);
   Term actualKey=CoreKey407(CoreRead407(selected.Rows[0].Payload,context.Cancellation),options.KeyPosition);
   if(!lookup)return actualKey;
   return Term.Tuple(actualKey,Cons.From(selected.Rows.Select(slot=>CoreRead407(slot.Payload,context.Cancellation))));
  }
 }
}
