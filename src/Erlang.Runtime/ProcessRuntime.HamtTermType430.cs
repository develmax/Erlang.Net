using System.Numerics;

namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal void RequireHamtInput430(Term source)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed || IsHaltRequested, this);
            RequireExternalTermDomain(source);
        }
    }

    internal Term ReadHamtTermType430(Term source)
    {
        if (source is MapTerm { FlatKeys426: null } map)
        {
            using var owned = AcquireHamtIndex429(map);

            return Term.A(HamtTermTypeNames430.Head);
        }
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed || IsHaltRequested, this);
            var local = LocalIdentity.Snapshot;

            bool LocalPort(PortTerm value) => value.LocalIdentity is { } owner
                ? ReferenceEquals(owner, LocalIdentity) : value.WireIdentity == local;
            string type = source switch
            {
                Cons => HamtTermTypeNames430.List,
                TupleTerm => HamtTermTypeNames430.Tuple,
                HamtBitmapTerm429 => HamtTermTypeNames430.Node,
                MapTerm mapValue => mapValue.FlatKeys426 is null ? HamtTermTypeNames430.Head : HamtTermTypeNames430.Flat,
                FunctionTerm function => function.ExternalIdentity is null ? HamtTermTypeNames430.Fun : HamtTermTypeNames430.Export,
                BitString binary => binary.HasSubBitsStorage430 ? HamtTermTypeNames430.SubBits : HamtTermTypeNames430.HeapBits,
                Integer integer => integer.Value >= -(BigInteger.One << HamtTermTypeNames430.SmallBits)
                    && integer.Value < (BigInteger.One << HamtTermTypeNames430.SmallBits)
                    ? HamtTermTypeNames430.Small : HamtTermTypeNames430.Big,
                FloatTerm => HamtTermTypeNames430.Float,
                ReferenceTerm reference => IsCurrentLocal(reference) ? HamtTermTypeNames430.Reference : HamtTermTypeNames430.ExternalReference,
                Pid pid => IsCurrentLocal(pid) ? HamtTermTypeNames430.Pid : HamtTermTypeNames430.ExternalPid,
                PortTerm port => LocalPort(port) ? HamtTermTypeNames430.Port : HamtTermTypeNames430.ExternalPort,
                Atom => HamtTermTypeNames430.Atom,
                Nil => HamtTermTypeNames430.Nil,
                _ => throw new NotSupportedException(HamtNativeNames430.Unsupported)
            };

            return Term.A(type);
        }
    }
}
