using System.CommandLine;

using OpenDsc.Resource.CommandLine;

using Temp;

var resource = new Resource(SourceGenerationContext.Default);
var command = CommandBuilder<Resource, Schema>.Build(resource, SourceGenerationContext.Default);
return command.Invoke(args);
