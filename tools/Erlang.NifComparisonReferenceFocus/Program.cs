using Erlang.NifNativeBackend;

try
{
    await NifNativeComparisonHostEntry.Ordering();
    Console.WriteLine("PASS ordering");
    await NifNativeComparisonHostEntry.ExactEquality();
    Console.WriteLine("PASS exact equality");
    await NifNativeComparisonHostEntry.PendingReads();
    Console.WriteLine("PASS pending reads remain live");
    await NifNativeComparisonHostEntry.ReferenceUniqueness();
    Console.WriteLine("PASS reference uniqueness");
    await NifNativeComparisonHostEntry.OwnershipBoundaries();
    Console.WriteLine("PASS ownership boundaries");
}
catch (Exception failure)
{
    Console.Error.WriteLine(failure);
    Environment.ExitCode = 1;
}
