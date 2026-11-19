---
name: unit-testing
description: Write focused unit tests for .NET domain logic and application handlers without HTTP, external services, or a real database.
---

# Unit testing

## Scope and discovery

Use this skill to add or update unit tests for domain types and application
handlers. Do not generate or modify production code, integration tests, endpoint
tests, or persistence implementations unless explicitly requested.

Inspect the current production APIs, observable behavior, handler and repository
interfaces, project/test conventions, target framework, test layout, and existing
tests. Follow the checked-out code rather than assumptions from an example or
planned refactor. Run tests only for behavior that exists in the requested scope.

## Framework and layout

Use the test framework, assertion library, and test SDK already used by the
repository. Add only dependencies needed for the requested tests; do not impose
unrelated package-version, licensing, or coverage-tool requirements.

Follow existing test namespaces and folder conventions. Domain tests usually
belong under a domain-aligned test folder; handler tests usually mirror their
feature/use-case organization. Name test classes after the subject and test
methods to clearly describe initial state, action, and expected behavior.

## Test design

- Use Arrange–Act–Assert and separate the sections clearly. Keep each test focused
  on one observable behavior.
- Assert returned values and error types/codes, resulting domain state, repository
  state, and relevant side effects. Avoid private implementation details and
  assertions that only prove a method was invoked.
- Use parameterized tests for input variants instead of conditional test logic.
  Keep helpers simple, deterministic, and shared only when they genuinely reduce
  duplication.
- Each test owns its data and state. Do not depend on execution order, shared
  mutable state, network access, external databases, sleeps, or wall-clock time.
- Use small hand-written fakes for interfaces when a handler needs a dependency,
  and assert through their resulting state. Use an existing mocking approach only
  when it is already standard and required by the task.
- For time-dependent rules, inject a controllable `TimeProvider` or repository
  fake. Avoid system clock access and sleeps; use explicit instants and time zones.
- Test expected business failures as returned results rather than exceptions.
  Assert that failed commands leave relevant state unchanged.
- For queries, assert returned DTO data and that source state remains unchanged.
- Cover success, validation, not-found, and conflict where the production
  contract supports them. Include boundary cases that protect invariants, without
  inventing undefined behavior.
- Do not unit-test routing, serialization, HTTP mapping, or DI composition; cover
  those through integration tests.

## Coverage

Use the repository's existing coverage tooling when coverage is in scope. Aim
coverage at meaningful domain and handler branches, including both success and
failure paths. Do not exclude code, change production behavior, or weaken
assertions merely to improve a metric. If a path cannot be tested due to a defect
or inaccessible design, report the specific limitation rather than concealing it.
State measured coverage only when the report was actually produced.

## Verification and handoff

Run the focused unit-test project or filter after adding tests, then any broader
suite appropriate to the change. Review failures and distinguish pre-existing
issues from regressions. If a test is wrong, correct the test; do not weaken a
valid assertion to make it pass.

Report the commands actually run and their outcomes. If the SDK, dependencies,
or test infrastructure are unavailable, state which checks could not run.
