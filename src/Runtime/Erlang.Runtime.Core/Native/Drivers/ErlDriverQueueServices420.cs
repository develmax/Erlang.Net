namespace Erlang;

internal sealed class ErlDriverQueueServices420
{
    private readonly Func<nint, ErlDriverPortReadLease420?> acquire;
    private readonly ErlDriverServiceFault420 fault;

    internal ErlDriverQueueServices420(Func<nint, ErlDriverPortReadLease420?> acquire, ErlDriverServiceFault420 fault)
    {
        this.acquire = acquire;
        this.fault = fault;
        Callbacks = new Dictionary<string, Delegate>(StringComparer.Ordinal)
        {
            [nameof(ErlDriverCallbackTypes420.driver_enq)] = new ErlDriverCallbackTypes420.driver_enq(Enqueue),
            [nameof(ErlDriverCallbackTypes420.driver_pushq)] = new ErlDriverCallbackTypes420.driver_pushq(Push),
            [nameof(ErlDriverCallbackTypes420.driver_deq)] = new ErlDriverCallbackTypes420.driver_deq(Dequeue),
            [nameof(ErlDriverCallbackTypes420.driver_sizeq)] = new ErlDriverCallbackTypes420.driver_sizeq(Size),
            [nameof(ErlDriverCallbackTypes420.driver_enq_bin)] = new ErlDriverCallbackTypes420.driver_enq_bin(EnqueueBinary),
            [nameof(ErlDriverCallbackTypes420.driver_pushq_bin)] = new ErlDriverCallbackTypes420.driver_pushq_bin(PushBinary),
            [nameof(ErlDriverCallbackTypes420.driver_peekqv)] = new ErlDriverCallbackTypes420.driver_peekqv(PeekVector),
            [nameof(ErlDriverCallbackTypes420.driver_peekq)] = new ErlDriverCallbackTypes420.driver_peekq(Peek),
            [nameof(ErlDriverCallbackTypes420.driver_enqv)] = new ErlDriverCallbackTypes420.driver_enqv(EnqueueVector),
            [nameof(ErlDriverCallbackTypes420.driver_pushqv)] = new ErlDriverCallbackTypes420.driver_pushqv(PushVector)
        };
    }

    internal IReadOnlyDictionary<string, Delegate> Callbacks { get; }

    private T Use<T>(nint port, Func<ErlDriverQueue420, T> body, T invalid)
    {
        try
        {
            using var lease = acquire(port);
            if (lease is null) return invalid;

            return body(lease.Port.Queue);
        }
        catch (OutOfMemoryException)
        {
            return invalid;
        }
        catch (InvalidDataException)
        {
            return invalid;
        }
        catch (ObjectDisposedException)
        {
            return invalid;
        }
        catch (Exception error)
        {
            fault.Record(error);

            return invalid;
        }
    }

    private int Enqueue(nint port, nint bytes, nuint size)
        => Use(port, queue =>
 {
     queue.EnqueueBytes(bytes, size, false);

     return 0;
 }, -1);

    private int Push(nint port, nint bytes, nuint size)
        => Use(port, queue =>
 {
     queue.EnqueueBytes(bytes, size, true);

     return 0;
 }, -1);

    private int EnqueueBinary(
        nint port,
        nint binary,
        nuint offset,
        nuint size
    )
        => Use(port, queue =>
 {
     queue.EnqueueBinary(
         binary,
         offset,
         size,
         false
     );

     return 0;
 }, -1);

    private int PushBinary(
        nint port,
        nint binary,
        nuint offset,
        nuint size
    )
        => Use(port, queue =>
 {
     queue.EnqueueBinary(
         binary,
         offset,
         size,
         true
     );

     return 0;
 }, -1);

    private int EnqueueVector(nint port, nint vector, nuint skip)
        => Use(port, queue =>
 {
     queue.EnqueueVector(vector, skip, false);

     return 0;
 }, -1);

    private int PushVector(nint port, nint vector, nuint skip)
        => Use(port, queue =>
 {
     queue.EnqueueVector(vector, skip, true);

     return 0;
 }, -1);

    private nuint Dequeue(nint port, nuint size) => Use(port, queue => queue.Dequeue(size), nuint.MaxValue);

    private nuint Size(nint port) => Use(port, queue => queue.Size, nuint.MaxValue);

    private nuint PeekVector(nint port, nint vector) => Use(port, queue => queue.PeekVector(vector), nuint.MaxValue);

    private nint Peek(nint port, nint count) => Use(port, queue => queue.Peek(count), nint.Zero);
}
