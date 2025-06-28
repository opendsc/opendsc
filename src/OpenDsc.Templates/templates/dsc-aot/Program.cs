using System.CommandLine;

using OpenDsc.Resource.CommandLine;

var resource = new TempResource(SourceGenerationContext.Default);
var command = CommandBuilder<TempResource, TempSchema>.Build(resource, SourceGenerationContext.Default);
return command.Invoke(args);
