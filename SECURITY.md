# Security Policy

## Reporting a vulnerability

**Please do not report security vulnerabilities through public issues, discussions or pull requests.**

Report them privately with GitHub's private vulnerability reporting: open the **Security** tab of this repository and
choose **Report a vulnerability**. Only the maintainers can see the report.

Please include as much as you can of:

- the affected package and version (or commit);
- a description of the issue and its impact;
- steps to reproduce, a proof of concept or the affected code;
- any known workaround.

## Scope

WalterEgo is a defensive tool: it plants decoys (honeytokens) in your application and alerts when someone touches them.
Besides the usual classes of vulnerabilities, the following are security issues in WalterEgo:

- a decoy that grants real access: a honey user that authenticates, a honey API key that authorizes, a honey session
  token that validates;
- decoys, or the presence of WalterEgo itself, that an attacker can recognize (headers, response content, timing,
  recognizable generated values, a decoy registry that reveals which values are decoys);
- a way to use a decoy without the trip being detected or reported;
- a failure in WalterEgo (for example, an alert channel that is down or an exception in a sensor) that breaks or changes
  the behavior of the host application;
- secrets or decoy values leaking into logs, events or alert payloads.

A decoy that is fake and exposed on purpose is not a vulnerability: that is how honeytokens work.

## What to expect

- We will acknowledge the report as soon as possible, usually within a week.
- We will investigate, keep you informed of the progress and agree on a disclosure date with you.
- Fixes are released as a new version, and a GitHub Security Advisory credits the reporter (unless you prefer to remain
  anonymous).

## Supported versions

WalterEgo is in preview. Security fixes are made for the latest preview release.
