// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server;

/// <summary>
/// Configuration for the OpenDSC Server.
/// </summary>
public sealed class ServerConfig
{
    private string? _dataDirectory;
    private string? _configurationsDirectory;
    private string? _parametersDirectory;
    private string? _databaseDirectory;

    /// <summary>
    /// Root directory for all server data.
    /// Defaults to a platform-specific path under the server config directory.
    /// </summary>
    public string DataDirectory
    {
        get
        {
            if (string.IsNullOrEmpty(_dataDirectory))
            {
                _dataDirectory = Path.Combine(ServerPaths.GetServerConfigDirectory(), "data");
            }

            return _dataDirectory;
        }

        set
        {
            _dataDirectory = value;
        }
    }

    /// <summary>
    /// Directory where configuration files are stored.
    /// Defaults to <see cref="DataDirectory"/>/configurations.
    /// </summary>
    public string ConfigurationsDirectory
    {
        get
        {
            if (string.IsNullOrEmpty(_configurationsDirectory))
            {
                _configurationsDirectory = Path.Combine(DataDirectory, "configurations");
            }

            return _configurationsDirectory;
        }

        set
        {
            _configurationsDirectory = value;
        }
    }

    /// <summary>
    /// Directory where parameter files are stored.
    /// Defaults to <see cref="DataDirectory"/>/parameters.
    /// </summary>
    public string ParametersDirectory
    {
        get
        {
            if (string.IsNullOrEmpty(_parametersDirectory))
            {
                _parametersDirectory = Path.Combine(DataDirectory, "parameters");
            }

            return _parametersDirectory;
        }

        set
        {
            _parametersDirectory = value;
        }
    }

    /// <summary>
    /// Directory where the database file is stored (SQLite).
    /// Defaults to <see cref="DataDirectory"/>/database.
    /// </summary>
    public string DatabaseDirectory
    {
        get
        {
            if (string.IsNullOrEmpty(_databaseDirectory))
            {
                _databaseDirectory = Path.Combine(DataDirectory, "database");
            }

            return _databaseDirectory;
        }

        set
        {
            _databaseDirectory = value;
        }
    }
}
