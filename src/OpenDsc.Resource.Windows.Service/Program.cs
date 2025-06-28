// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.CommandLine;

using OpenDsc.Resource.CommandLine;
using OpenDsc.Resource.Windows.Service;

var resource = new Resource(SourceGenerationContext.Default);
var command = CommandBuilder<Resource, Schema>.Build(resource, SourceGenerationContext.Default);
return command.Invoke(args);
