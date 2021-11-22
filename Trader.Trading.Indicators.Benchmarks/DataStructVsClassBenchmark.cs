using BenchmarkDotNet.Attributes;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Trader.Trading.Indicators.Benchmarks
{
    [MemoryDiagnoser]
    public class DataStructVsClassBenchmark
    {
        [Params(10, 100, 1000)]
        public int N { get; set; }

        [Benchmark(Baseline = true)]
        public MutableStruct WithMutableStruct()
        {
            var value = new MutableStruct();

            for (var i = 0; i < N; i++)
            {
                value.Value0 += i;
                value.Value1 += i;
                value.Value2 += i;
                value.Value3 += i;
                value.Value4 += i;
                value.Value5 += i;
                value.Value6 += i;
                value.Value7 += i;
                value.Value8 += i;
                value.Value9 += i;
            }

            return value;
        }

        [Benchmark]
        public ImmutableStruct WithImmutableStruct()
        {
            var value0 = 0M;
            var value1 = 0M;
            var value2 = 0M;
            var value3 = 0M;
            var value4 = 0M;
            var value5 = 0M;
            var value6 = 0M;
            var value7 = 0M;
            var value8 = 0M;
            var value9 = 0M;

            for (var i = 0; i < N; i++)
            {
                value0 += i;
                value1 += i;
                value2 += i;
                value3 += i;
                value4 += i;
                value5 += i;
                value6 += i;
                value7 += i;
                value8 += i;
                value9 += i;
            }

            return new ImmutableStruct
            {
                Value0 = value0,
                Value1 = value1,
                Value2 = value2,
                Value3 = value3,
                Value4 = value4,
                Value5 = value5,
                Value6 = value6,
                Value7 = value7,
                Value8 = value8,
                Value9 = value9,
            };
        }

        [Benchmark]
        public MutableClass WithMutableClass()
        {
            var value = new MutableClass();

            for (var i = 0; i < N; i++)
            {
                value.Value0 += i;
                value.Value1 += i;
                value.Value2 += i;
                value.Value3 += i;
                value.Value4 += i;
                value.Value5 += i;
                value.Value6 += i;
                value.Value7 += i;
                value.Value8 += i;
                value.Value9 += i;
            }

            return value;
        }

        [Benchmark]
        public ImmutableClass WithImmutableClass()
        {
            var builder = ImmutableClass.CreateBuilder();

            for (var i = 0; i < N; i++)
            {
                builder.Value0 += i;
                builder.Value1 += i;
                builder.Value2 += i;
                builder.Value3 += i;
                builder.Value4 += i;
                builder.Value5 += i;
                builder.Value6 += i;
                builder.Value7 += i;
                builder.Value8 += i;
                builder.Value9 += i;
            }

            return builder.ToImmutable();
        }
    }

    [StructLayout(LayoutKind.Auto)]
    public record struct MutableStruct
    {
        public decimal Value0 { get; set; }
        public decimal Value1 { get; set; }
        public decimal Value2 { get; set; }
        public decimal Value3 { get; set; }
        public decimal Value4 { get; set; }
        public decimal Value5 { get; set; }
        public decimal Value6 { get; set; }
        public decimal Value7 { get; set; }
        public decimal Value8 { get; set; }
        public decimal Value9 { get; set; }
    }

    [StructLayout(LayoutKind.Auto)]
    public record struct ImmutableStruct
    {
        public decimal Value0 { get; init; }
        public decimal Value1 { get; init; }
        public decimal Value2 { get; init; }
        public decimal Value3 { get; init; }
        public decimal Value4 { get; init; }
        public decimal Value5 { get; init; }
        public decimal Value6 { get; init; }
        public decimal Value7 { get; init; }
        public decimal Value8 { get; init; }
        public decimal Value9 { get; init; }
    }

    public record class MutableClass
    {
        public decimal Value0 { get; set; }
        public decimal Value1 { get; set; }
        public decimal Value2 { get; set; }
        public decimal Value3 { get; set; }
        public decimal Value4 { get; set; }
        public decimal Value5 { get; set; }
        public decimal Value6 { get; set; }
        public decimal Value7 { get; set; }
        public decimal Value8 { get; set; }
        public decimal Value9 { get; set; }
    }

    public record class ImmutableClass
    {
        public decimal Value0 { get; init; }
        public decimal Value1 { get; init; }
        public decimal Value2 { get; init; }
        public decimal Value3 { get; init; }
        public decimal Value4 { get; init; }
        public decimal Value5 { get; init; }
        public decimal Value6 { get; init; }
        public decimal Value7 { get; init; }
        public decimal Value8 { get; init; }
        public decimal Value9 { get; init; }

        public static Builder CreateBuilder() => new();

        [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Immutable Builder Pattern")]
        public class Builder
        {
            public decimal Value0 { get; set; }
            public decimal Value1 { get; set; }
            public decimal Value2 { get; set; }
            public decimal Value3 { get; set; }
            public decimal Value4 { get; set; }
            public decimal Value5 { get; set; }
            public decimal Value6 { get; set; }
            public decimal Value7 { get; set; }
            public decimal Value8 { get; set; }
            public decimal Value9 { get; set; }

            public ImmutableClass ToImmutable() => new()
            {
                Value0 = Value0,
                Value1 = Value1,
                Value2 = Value2,
                Value3 = Value3,
                Value4 = Value4,
                Value5 = Value5,
                Value6 = Value6,
                Value7 = Value7,
                Value8 = Value8,
                Value9 = Value9,
            };
        }
    }
}