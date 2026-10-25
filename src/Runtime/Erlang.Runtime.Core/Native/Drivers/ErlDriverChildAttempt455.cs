namespace Erlang;

// Session-local tombstones make reserve/commit/abort idempotent. Data pointers
// never cross this parent-side transaction or the wire.
internal sealed class ErlDriverChildAttempt455(ErlDriverChildReserve455 request, ErlDriverChildReservation455 reservation, NativeDriverPortRow422 row)
{
    internal ErlDriverChildReserve455 Request { get; } = request;
    internal ErlDriverChildReservation455 Reservation { get; } = reservation;
    internal NativeDriverPortRow422? Row { get; set; } = row;
    internal bool Committed;
    internal bool Aborted;
}
