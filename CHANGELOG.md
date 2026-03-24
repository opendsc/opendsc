# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog][keep-a-changelog],
and this project adheres to [Semantic Versioning][semver].

[keep-a-changelog]: https://keepachangelog.com/en/1.0.0/
[semver]: https://semver.org/spec/v2.0.0.html

## [Unreleased]

## [0.5.0] - 2026-03-24

### Added

- Add ZIP resources (#43)
- Add Local Configuration Manager (LCM) (#44)
- Add SymbolicLink resource (#47)
- Add Scheduled Task resource (#53)
- Add JSON value resource (#58)
- Add SQL Server resources (#56, #61)
- Add Pull Server (#62)
- Add Export filter and set interface instances to nullable (#75)
- Add mTLS node auth (#72)
- Add OpenDsc.SqlServer/AgentJob resource (#65)
- Add OpenDsc.SqlServer/Configuration resource (#66)
- Add OpenDsc.SqlServer/LinkedServer resource (#67)
- Add OpenDsc.SqlServer/ObjectPermission resource (#68)
- Add OpenDsc.SqlServer/DatabaseUser resource (#69)
- Add composite configuration (#77)
- Add server UI (#86)
- Add OpenDsc.Windows/AccountLockoutPolicy resource (#79)
- Add OpenDsc.Windows/PasswordPolicy resource (#80)
- Add OpenDsc.Windows/AuditPolicy resource (#70)

### Fixed

- Fix directory resource when target directory exists (#92)

### Removed

- Remove _exist property for ACL resource (#52)

## [0.4.0] - 2025-12-23

### Added

- Add Windows, Linux and macOS resources (#32)

## [0.3.1] - 2025-12-17

### Fixed

- Fix exception exit codes not being returned (#29)

## [0.3.0] - 2025-12-09

### Added

- Multi-resource support - register multiple resources in a single
  executable (#25)
- Symbol packages (.snupkg) for improved debugging (#26)
- CI/CD workflows for automated testing and publishing
- Build script with DSC installation and cross-platform testing
- Comprehensive Pester test suites for all test resources

### Changed

- Command structure - commands now at root level (no `config` parent)
- Manifest generation with `--save` flag
- Refactored command builder to use registry pattern for multi-resource support
- Improved error handling with resource type validation
- Updated templates to support both single and multi-resource patterns

## [0.2.0] - 2025-07-16

### Added

- Add InvariantGlobalization to windows-service (#15)
- Add attributes (#14)

## [0.1.2] - 2025-06-30

### Fixed

- Fix manifest generation when using JsonSerializerOptions (#10)

## [0.1.1] - 2025-06-29

### Fixed

- Fix templates (#5)

## [0.1.0] - 2025-06-27

### Added

- Initial release

[Unreleased]: https://github.com/opendsc/opendsc/compare/v0.5.0...HEAD
[0.5.0]: https://github.com/opendsc/opendsc/releases/tag/v0.5.0
[0.4.0]: https://github.com/opendsc/opendsc/releases/tag/v0.4.0
[0.3.1]: https://github.com/opendsc/opendsc/releases/tag/v0.3.1
[0.3.0]: https://github.com/opendsc/opendsc/releases/tag/v0.3.0
[0.2.0]: https://github.com/opendsc/opendsc/releases/tag/v0.2.0
[0.1.2]: https://github.com/opendsc/opendsc/releases/tag/v0.1.2
[0.1.1]: https://github.com/opendsc/opendsc/releases/tag/v0.1.1
[0.1.0]: https://github.com/opendsc/opendsc/releases/tag/v0.1.0
