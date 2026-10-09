

namespace HelloHybrid;

public sealed record Box<T>(T Value) : IBox<T>;
