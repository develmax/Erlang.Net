namespace Erlang;

internal sealed record EtfDecodeAdmission(EtfDecodeOptions Options, IDisposable Reservation, Action<Term> Observe) : IDisposable
{
    public void Dispose()
    {
        Reservation.Dispose();
    }
}
