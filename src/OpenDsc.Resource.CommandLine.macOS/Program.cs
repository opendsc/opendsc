// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Resource.CommandLine;
using FileNs = OpenDsc.Resource.FileSystem.File;
using DirectoryNs = OpenDsc.Resource.FileSystem.Directory;
using XmlElementNs = OpenDsc.Resource.Xml.Element;

var fileResource = new FileNs.Resource(OpenDsc.Resource.FileSystem.SourceGenerationContext.Default);
var directoryResource = new DirectoryNs.Resource(OpenDsc.Resource.FileSystem.SourceGenerationContext.Default);
var xmlElementResource = new XmlElementNs.Resource(OpenDsc.Resource.Xml.SourceGenerationContext.Default);

var command = new CommandBuilder()
    .AddResource<FileNs.Resource, FileNs.Schema>(fileResource)
    .AddResource<DirectoryNs.Resource, DirectoryNs.Schema>(directoryResource)
    .AddResource<XmlElementNs.Resource, XmlElementNs.Schema>(xmlElementResource)
    .Build();

return command.Parse(args).Invoke();
