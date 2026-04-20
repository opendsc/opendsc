// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

namespace OpenDsc.Resource.Tests;

/// <summary>
/// Test resource implementation for testing DscResource base class.
/// </summary>
[DscResource("OpenDsc.Test/TestResource", Description = "A test resource")]
[ExitCode(1)]
[ExitCode(2, Exception = typeof(ArgumentException))]
[ExitCode(3, Exception = typeof(InvalidOperationException))]
public class TestResource : DscResource<TestSchema>, IGettable<TestSchema>, ISettable<TestSchema>, ITestable<TestSchema>, IDeletable<TestSchema>
{
    public TestResource(JsonSerializerContext context) : base(context)
    {
    }

    public TestSchema Get(TestSchema? instance)
    {
        return instance ?? new TestSchema();
    }

    public SetResult<TestSchema>? Set(TestSchema? instance)
    {
        if (instance == null)
            return null;

        return new SetResult<TestSchema>(instance)
        {
            ChangedProperties = new HashSet<string> { "Value", "Enabled" }
        };
    }

    public TestResult<TestSchema> Test(TestSchema? instance)
    {
        return new TestResult<TestSchema>(instance ?? new TestSchema())
        {
            DifferingProperties = new HashSet<string> { "Value" }
        };
    }

    public void Delete(TestSchema? instance)
    {
        // No-op for testing
    }
}
