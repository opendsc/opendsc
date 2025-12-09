# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog][keep-a-changelog],
and this project adheres to [Semantic Versioning][semver].

[keep-a-changelog]: https://keepachangelog.com/en/1.0.0/
[semver]: https://semver.org/spec/v2.0.0.html

## [Unreleased]

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

[Unreleased]: https://github.com/opendsc/opendsc/compare/v0.3.0...HEAD
[0.3.0]: https://github.com/opendsc/opendsc/releases/tag/v0.3.0
[0.2.0]: https://github.com/opendsc/opendsc/releases/tag/v0.2.0
[0.1.2]: https://github.com/opendsc/opendsc/releases/tag/v0.1.2
[0.1.1]: https://github.com/opendsc/opendsc/releases/tag/v0.1.1
[0.1.0]: https://github.com/opendsc/opendsc/releases/tag/v0.1.0
