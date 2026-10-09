using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang.Compiler;

public sealed record BitSegment(
    Expr Value,
    Expr? Size,
    string Type = BitSegmentTypes.Integer,
    int Unit = BitSyntaxDefaults.NumericUnit,
    string Endian = BitByteOrders.Big,
    bool Signed = false
);
