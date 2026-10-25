namespace Erlang;

// The parent supplies its actual runtime-bound implementation. This path never
// reads the dedicated child's independent environment or starts another clock.
internal sealed class ErlDriverPrimitiveParent420
{
    private readonly IErlDriverPrimitiveHost422 host;

    internal ErlDriverPrimitiveParent420(IErlDriverPrimitiveHost422 host) => this.host = host;

    internal ValueTask<ErlDriverHostFrame420> Handle(ErlDriverHostFrame420 request, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        if (request.Operation != ErlDriverHostOperation420.PrimitiveHost || request.Driver != 0
            || request.Port != 0 || request.Actor != 0 || !Enum.IsDefined((ErlDriverPrimitiveCommand420)request.Command))
            throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        int status = 0;
        byte[]? data;
        switch ((ErlDriverPrimitiveCommand420)request.Command)
        {
            case ErlDriverPrimitiveCommand420.GetNow:
                if (request.Name is not null || request.Data is not null)
                    throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                data = ErlDriverPrimitiveWire420.Encode(host.GetNowMicroseconds());
                break;

            case ErlDriverPrimitiveCommand420.Clock:
                {
                    if (request.Name is not null) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                    var clock = ErlDriverPrimitiveWire420.Decode<ErlDriverClockRequest420>(request.Data);
                    data = ErlDriverPrimitiveWire420.Encode(host.Clock(clock.Offset, clock.Unit));
                    break;
                }

            case ErlDriverPrimitiveCommand420.ReadEnvironment:
                {
                    if (request.Name is null || request.Data is not null)
                        throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                    string? value = host.ReadEnvironment(request.Name);
                    status = value is null ? -1 : 0;
                    data = value is null ? null : ErlDriverPrimitiveWire420.Encode(value);
                    break;
                }

            case ErlDriverPrimitiveCommand420.WriteEnvironment:
                if (request.Name is null || request.Data is null)
                    throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
                status = host.WriteEnvironment(request.Name, request.Data);
                data = null;
                break;

            default:
                throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        }

        return ValueTask.FromResult(request with
        {
            Kind = ErlDriverHostKind420.Response,
            NativeResult = status,
            Name = null,
            Data = data,
            EventSequence = 0
        });
    }
}
