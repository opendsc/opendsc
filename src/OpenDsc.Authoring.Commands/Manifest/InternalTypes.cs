// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation.Language;

namespace OpenDsc.Authoring.Commands;

internal sealed class DscResourceTypeInfo(TypeDefinitionAst typeDefinitionAst, TypeDefinitionAst[] allTypeDefinitions)
{
    public TypeDefinitionAst TypeDefinitionAst { get; } = typeDefinitionAst;

    public TypeDefinitionAst[] AllTypeDefinitions { get; } = allTypeDefinitions;
}

internal sealed class CommentBasedHelp
{
    public string Synopsis { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Dictionary<string, string> Parameters { get; } = [];
}

internal sealed class ModuleInfo
{
    public string ModuleName { get; set; } = string.Empty;

    public string Version { get; set; } = "0.0.1";

    public string Author { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ScriptPath { get; set; } = string.Empty;

    public string Psd1Path { get; set; } = string.Empty;

    public string Directory { get; set; } = string.Empty;
}

internal sealed class DscPropertyInfo
{
    public string Name { get; set; } = string.Empty;

    public string TypeName { get; set; } = "string";

    public bool IsKey { get; set; }

    public bool IsMandatory { get; set; }

    public string[]? EnumValues { get; set; }
}
