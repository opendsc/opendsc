// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Resource.CommandLine;

using TestResource.NonAot;

var resource = new Resource(SourceGenerationContext.Default);
var command = CommandBuilder<Resource, Schema>.Build(resource, SourceGenerationContext.Default);
return command.Parse(args).Invoke();
