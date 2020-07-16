﻿using Xunit.Abstractions;
using static ExRam.Gremlinq.Core.GremlinQuerySource;

namespace ExRam.Gremlinq.Core.Tests
{
    public sealed class GroovyGremlinQuerySerializationTest : QuerySerializationTest
    {
        public GroovyGremlinQuerySerializationTest(ITestOutputHelper testOutputHelper) : base(
            g.ConfigureEnvironment(_ => _
                .UseSerializer(GremlinQuerySerializer.Default.ToGroovy())),
            testOutputHelper)
        {
        }
    }

    public sealed class InlinedGroovyGremlinQuerySerializationTest : QuerySerializationTest
    {
        public InlinedGroovyGremlinQuerySerializationTest(ITestOutputHelper testOutputHelper) : base(
            g.ConfigureEnvironment(_ => _
                .UseSerializer(GremlinQuerySerializer.Default.ToGroovy(GroovyFormatting.AllowInlining))),
            testOutputHelper)
        {
        }
    }

    public sealed class InlinedLaterGroovyGremlinQuerySerializationTest : QuerySerializationTest
    {
        public InlinedLaterGroovyGremlinQuerySerializationTest(ITestOutputHelper testOutputHelper) : base(
            g.ConfigureEnvironment(_ => _
                .UseSerializer(GremlinQuerySerializer.Default.ToGroovy())
                .ConfigureSerializer(serializer => serializer.Select(query => ((GroovyGremlinQuery)query).Inline()))),
            testOutputHelper)
        {
        }
    }
}
