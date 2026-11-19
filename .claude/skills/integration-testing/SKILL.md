---
name: integration-testing
description: Create or update .NET API integration tests that exercise real HTTP routes, dependency injection, and isolated persistence.
---

# API integration testing

## Before writing tests

Inspect the API project, routes, request/response types, endpoint result types,
centralized error mapping, database context, persistence registrations, test
project conventions, and existing tests. Use the actual application contract and
test infrastructure.

Do not change production code, handlers, or feature slices merely to make tests
pass. If the requested storage provider or test host is missing, report the
blocker rather than substituting an in-memory repository or mock for the requested
integration. Use the test framework and assertion library already adopted by the
repository; do not silently introduce a conflicting major version.

## Layout and names

Follow the test project's layout and namespaces. Organize integration tests by
feature or use case when that matches existing conventions, and keep helpers
small and free of assertions/business logic. Name tests so their initial state,
action, and expected outcome are clear.

Keep unit tests and endpoint/HTTP tests in their appropriate layers. Do not move
or rewrite unrelated legacy tests. Update a legacy assertion only when the task
explicitly changes the public contract and the assertion is directly obsolete;
preserve the rest of the test.

## Host setup and isolation

Use the repository's real test host, such as `WebApplicationFactory<Program>`,
and exercise HTTP requests instead of calling endpoints or handlers directly.
Each test should own an isolated database and host unless the existing test
architecture provides an equivalent, safe isolation strategy.

For SQLite in-memory databases, keep the connection alive from schema
initialization through requests and assertions. Avoid static/shared databases,
test-order dependencies, competing DbContext registrations, and concurrent use
of one SQLite connection where it is unsupported. Dispose scopes and the host
before disposing the connection. Do not create the schema twice if host startup
already owns initialization.

Use the host's dependency-injection scope for seeding and state verification.
Seed only the data the test needs, using domain factories and valid invariants.
Verify persisted state through a fresh scope/context and avoid relying on tracked
objects from Arrange.

## Test cases and assertions

Cover each requested use case's success and distinct expected HTTP failures:
validation, not-found, conflict, or other outcomes actually supported by the
contract. Do not invent failures for operations that cannot return them. Include
empty and populated collection behavior where relevant.

Use Arrange–Act–Assert with clear separation and focused tests. Avoid loops,
conditionals, test-order assumptions, network access, wall-clock dependence, and
external databases unless they are explicitly part of the task.

Assert the complete observable contract:

- HTTP status and relevant headers, including `Location` for created resources.
- JSON response shape and important values, using the actual DTOs and serializer
  behavior.
- Exact error body shape and message where defined; account for ProblemDetails or
  validation-field details when applicable.
- Database state after both successful writes and rejected writes. Queries should
  leave state unchanged.

Do not infer a relative `Location` URI's path using absolute-URI properties.
Avoid brittle assertions on irrelevant serialization details.

## Verification and handoff

Run the focused integration tests first, then any broader suite appropriate to
the change. Confirm test isolation by running the focused suite repeatedly when
that is practical. Report the actual commands and results; if the SDK or
dependencies are unavailable, say which checks were not run.

Do not weaken assertions to hide production defects. Distinguish test mistakes,
contract disagreements, and production failures in the handoff. Do not claim
coverage or verification that was not measured.
