namespace Erlang;

public static class DistributionInputAdapter
{
    public static Term PutData(ProcessContext caller, Term handle, Term data)
    {
        if (data is Cons)
            data = ErlangIoData.Flatten(data, false, true);

        return caller.Runtime.DistributionPending.PutConnectedData(caller, handle, data);
    }
}
