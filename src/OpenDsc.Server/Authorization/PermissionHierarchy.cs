// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using MudBlazor;

namespace OpenDsc.Server.Authorization;

/// <summary>
/// Represents a hierarchical organization of permissions.
/// </summary>
public class PermissionGroup
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<PermissionItem> Items { get; set; } = [];
    public List<PermissionGroup> SubGroups { get; set; } = [];
}

/// <summary>
/// Represents a single permission item.
/// </summary>
public class PermissionItem
{
    public string Value { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}

/// <summary>
/// Helper class to organize and format permissions hierarchically.
/// </summary>
public static class PermissionHierarchyHelper
{
    /// <summary>
    /// Organizes a flat list of permissions into a hierarchical tree structure for MudBlazor's MudTreeView.
    /// </summary>
    public static List<TreeItemData<string>> CreateTreeData(IEnumerable<string> allPermissions, IEnumerable<string> selectedPermissions)
    {
        var selectedSet = new HashSet<string>(selectedPermissions, StringComparer.Ordinal);
        var groups = OrganizePermissions(allPermissions, selectedPermissions);
        return ConvertToTreeItemData(groups);
    }

    /// <summary>
    /// Converts PermissionGroup hierarchy to MudBlazor TreeItemData for MudTreeView rendering.
    /// </summary>
    private static List<TreeItemData<string>> ConvertToTreeItemData(List<PermissionGroup> groups)
    {
        return ConvertToTreeItemDataRecursive(groups, isRoot: true);
    }

    private static List<TreeItemData<string>> ConvertToTreeItemDataRecursive(List<PermissionGroup> groups, bool isRoot)
    {
        var result = new List<TreeItemData<string>>();

        foreach (var group in groups)
        {
            // For subgroups, show only the last component of the path, not the full formatted path
            var displayName = isRoot ? group.DisplayName : GetLastPathComponent(group.Name);

            var groupItem = new TreeItemData<string>
            {
                Value = group.Name,
                Text = displayName,
                Children = new()
            };

            // Add permission items as leaf nodes (selectable)
            foreach (var item in group.Items)
            {
                groupItem.Children.Add(new TreeItemData<string>
                {
                    Value = item.Value,
                    Text = item.DisplayName,
                    Children = new()
                });
            }

            // Recursively add subgroups
            if (group.SubGroups.Count > 0)
            {
                groupItem.Children.AddRange(ConvertToTreeItemDataRecursive(group.SubGroups, isRoot: false));
            }

            result.Add(groupItem);
        }

        return result;
    }

    /// <summary>
    /// Organizes a flat list of permissions into a hierarchical tree structure.
    /// </summary>
    public static List<PermissionGroup> OrganizePermissions(IEnumerable<string> allPermissions, IEnumerable<string> selectedPermissions)
    {
        var selectedSet = new HashSet<string>(selectedPermissions, StringComparer.Ordinal);
        var groups = new Dictionary<string, PermissionGroup>();

        foreach (var permission in allPermissions.OrderBy(p => p))
        {
            var parts = permission.Split('.');
            var groupPath = string.Join(".", parts.Take(parts.Length - 1));
            var action = parts.Last();

            if (!groups.ContainsKey(groupPath))
            {
                groups[groupPath] = new PermissionGroup
                {
                    Name = groupPath,
                    DisplayName = FormatGroupName(groupPath)
                };
            }

            groups[groupPath].Items.Add(new PermissionItem
            {
                Value = permission,
                DisplayName = FormatActionName(action),
                IsSelected = selectedSet.Contains(permission)
            });
        }

        // Build hierarchy - create intermediate groups for each path level
        // First pass: collect all intermediate paths that need to be created
        var intermediatePaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var kvp in groups.Keys)
        {
            var parts = kvp.Split('.');
            for (int i = 1; i < parts.Length; i++)
            {
                var currentPath = string.Join(".", parts.Take(i));
                if (!groups.ContainsKey(currentPath))
                {
                    intermediatePaths.Add(currentPath);
                }
            }
        }

        // Second pass: create intermediate groups
        foreach (var path in intermediatePaths)
        {
            groups[path] = new PermissionGroup
            {
                Name = path,
                DisplayName = FormatGroupName(path)
            };
        }

        // Third pass: link groups to their parents
        var rootGroups = new Dictionary<string, PermissionGroup>();
        foreach (var kvp in groups)
        {
            var parts = kvp.Key.Split('.');
            if (parts.Length == 1)
            {
                // Root level
                rootGroups[kvp.Key] = kvp.Value;
            }
            else
            {
                // Link this group to its parent
                var parentPath = string.Join(".", parts.Take(parts.Length - 1));
                if (groups.ContainsKey(parentPath))
                {
                    groups[parentPath].SubGroups.Add(kvp.Value);
                }
            }
        }

        // Filter out empty groups (groups with no items and no non-empty subgroups)
        var result = FilterEmptyGroups(rootGroups.Values.OrderBy(g => g.DisplayName).ToList());
        return result;
    }

    private static List<PermissionGroup> FilterEmptyGroups(List<PermissionGroup> groups)
    {
        var filtered = new List<PermissionGroup>();
        foreach (var group in groups)
        {
            // Recursively filter subgroups
            group.SubGroups = FilterEmptyGroups(group.SubGroups);

            // Keep group if it has items or non-empty subgroups
            if (group.Items.Count > 0 || group.SubGroups.Count > 0)
            {
                filtered.Add(group);
            }
        }
        return filtered;
    }

    private static string FormatGroupName(string groupPath)
    {
        var parts = groupPath.Split('.');
        return string.Join(" > ", parts.Select(p => CapitalizeWords(p.Replace("-", " ").Replace("_", " "))));
    }

    private static string GetLastPathComponent(string path)
    {
        var parts = path.Split('.');
        var lastPart = parts[^1];
        return CapitalizeWords(lastPart.Replace("-", " ").Replace("_", " "));
    }

    private static string FormatActionName(string action)
    {
        return CapitalizeWords(action.Replace("-", " ").Replace("_", " "));
    }

    private static string CapitalizeWords(string text)
    {
        return System.Globalization.CultureInfo.CurrentCulture
            .TextInfo
            .ToTitleCase(text.ToLower());
    }
}
