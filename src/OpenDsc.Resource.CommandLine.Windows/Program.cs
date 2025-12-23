// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Resource.CommandLine;
using GroupNs = OpenDsc.Resource.Windows.Group;
using UserNs = OpenDsc.Resource.Windows.User;
using ServiceNs = OpenDsc.Resource.Windows.Service;
using EnvironmentNs = OpenDsc.Resource.Windows.Environment;
using ShortcutNs = OpenDsc.Resource.Windows.Shortcut;
using OptionalFeatureNs = OpenDsc.Resource.Windows.OptionalFeature;
using FileSystemAclNs = OpenDsc.Resource.Windows.FileSystem.Acl;
using FileNs = OpenDsc.Resource.FileSystem.File;
using DirectoryNs = OpenDsc.Resource.FileSystem.Directory;
using XmlElementNs = OpenDsc.Resource.Xml.Element;

var groupResource = new GroupNs.Resource(OpenDsc.Resource.Windows.SourceGenerationContext.Default);
var userResource = new UserNs.Resource(OpenDsc.Resource.Windows.SourceGenerationContext.Default);
var serviceResource = new ServiceNs.Resource(OpenDsc.Resource.Windows.SourceGenerationContext.Default);
var environmentResource = new EnvironmentNs.Resource(OpenDsc.Resource.Windows.SourceGenerationContext.Default);
var shortcutResource = new ShortcutNs.Resource(OpenDsc.Resource.Windows.SourceGenerationContext.Default);
var optionalFeatureResource = new OptionalFeatureNs.Resource(OpenDsc.Resource.Windows.SourceGenerationContext.Default);
var fileSystemAclResource = new FileSystemAclNs.Resource(OpenDsc.Resource.Windows.SourceGenerationContext.Default);
var fileResource = new FileNs.Resource(OpenDsc.Resource.FileSystem.SourceGenerationContext.Default);
var directoryResource = new DirectoryNs.Resource(OpenDsc.Resource.FileSystem.SourceGenerationContext.Default);
var xmlElementResource = new XmlElementNs.Resource(OpenDsc.Resource.Xml.SourceGenerationContext.Default);

var command = new CommandBuilder()
    .AddResource<GroupNs.Resource, GroupNs.Schema>(groupResource)
    .AddResource<UserNs.Resource, UserNs.Schema>(userResource)
    .AddResource<ServiceNs.Resource, ServiceNs.Schema>(serviceResource)
    .AddResource<EnvironmentNs.Resource, EnvironmentNs.Schema>(environmentResource)
    .AddResource<ShortcutNs.Resource, ShortcutNs.Schema>(shortcutResource)
    .AddResource<OptionalFeatureNs.Resource, OptionalFeatureNs.Schema>(optionalFeatureResource)
    .AddResource<FileSystemAclNs.Resource, FileSystemAclNs.Schema>(fileSystemAclResource)
    .AddResource<FileNs.Resource, FileNs.Schema>(fileResource)
    .AddResource<DirectoryNs.Resource, DirectoryNs.Schema>(directoryResource)
    .AddResource<XmlElementNs.Resource, XmlElementNs.Schema>(xmlElementResource)
    .Build();

return command.Parse(args).Invoke();
