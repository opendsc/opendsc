using System.CommandLine;

using OpenDsc.Resource.CommandLine;

using Temp;

var resource = new Resource();
var command = CommandBuilder<Resource, Schema>.Build(resource, resource.SerializerOptions);
return command.Invoke(args);
