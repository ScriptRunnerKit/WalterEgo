# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `WalterEgo.Core`: alter ego model (`HoneyToken`) with a mandatory placement, a decoy class (generic or unique)
  and severity defaults derived from the class.
- `WalterEgo.Core`: immutable token registry (`ITokenRegistry`) with allocation-free lookups by identifier and by
  raw value.
- `WalterEgo.Core`: keyed digests of decoy values (`ISecretHasher`, `SecretDigest`): HMAC-SHA256 with a mandatory
  key of at least 32 bytes and one derived subkey per trigger domain, so reading the configuration does not reveal
  which values are decoys.
