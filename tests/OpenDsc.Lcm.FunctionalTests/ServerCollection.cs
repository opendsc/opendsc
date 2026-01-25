// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Xunit;

namespace OpenDsc.Lcm.FunctionalTests;

[CollectionDefinition("Server")]
public sealed class ServerCollection : ICollectionFixture<ServerFixture>
{
}
