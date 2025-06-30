using System.CommandLine;

using OpenDsc.Resource.CommandLine;

using Temp;

var resource = new TempResource();
var command = CommandBuilder<TempResource, TempSchema>.Build(resource, resource.SerializerOptions);
return command.Invoke(args);
