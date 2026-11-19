---
name: cqrs
description: Design or refactor .NET application use cases around CQRS handlers, vertical slices, domain results, and thin HTTP endpoints while preserving the existing application contract.
---

# CQRS and vertical slices

## Scope and discovery

Use this skill when asked to introduce or improve commands, queries, handlers, or
feature-oriented organization in a .NET application. If asked only to edit this
skill, do not modify application code.

Before changing code, inspect repository instructions, solution and project files,
target framework, routes, request/response types, domain entities, result/error
types, persistence interfaces, dependency registration, and relevant tests. Treat
the current checkout and explicit user requirements as the source of truth. Do not
assume example paths, domain names, package versions, or planned refactors exist.

For each affected use case, record its route/input, success status and response,
error statuses and bodies, headers, and relevant domain invariants. Preserve these
contracts unless a behavior change is explicitly requested. Report conflicts
between requirements and existing behavior before making a consequential choice.
Keep changes limited to the requested use cases; do not infer new features from
unused domain types.

## Organization

Prefer one folder per use case under a feature, with shared application contracts
and domain types in existing shared locations:

```text
Common/
  Cqrs/
  Http/
Domain/
  Common/
Features/
  <Feature>/
    <UseCase>/
      <UseCase>Command.cs
      <UseCase>Handler.cs
      <UseCase>Request.cs
      <UseCase>Response.cs
```

Adapt names and layout to the repository's conventions. Do not duplicate existing
shared types. Use one file per type where that matches the codebase. Keep each
slice focused on one use case; slices should not invoke other slice handlers.
Place shared application contracts in a shared application area and shared business
policies in the domain.

Use commands for state-changing use cases and queries for reads. Name commands
imperatively and queries descriptively. Define request types only where the
transport binds a body; route-only operations should not gain unused request
records. Prefer immutable request, command, query, and response types. Do not pass
mutable domain entities as transport or command data, and project query results
into response DTOs.

## Handler boundaries

Define command/query handler interfaces once and follow existing async conventions.
A handler should implement one use case and depend on domain abstractions such as
repository interfaces—not ASP.NET, `HttpContext`, endpoint result types, concrete
infrastructure, or a service locator.

Pass cancellation tokens from the transport boundary through handlers to every
asynchronous dependency. Honor cancellation; do not turn it into a validation
failure. Use `TimeProvider` for time-dependent rules rather than static system
clock access. Avoid `Task.Run`, blocking waits, `.Result`, and `async void`.

Handlers coordinate domain operations and persistence. Keep invariants in rich
domain types or domain policies; do not move them to endpoints or duplicate them
only in handlers. Construct entities through their existing factories or
invariant-preserving methods. Validate before mutating state so an expected
failure cannot leave partial changes. Unexpected defects and infrastructure
failures must not be disguised as business validation failures.

## Results and expected failures

Reuse the application's result and error types. Do not introduce a parallel
`Result` abstraction. Propagate domain and repository errors without losing their
type, code, message, or field details. Access a result's value only on success and
its error only on failure. Expected business failures should be returned as
results, not thrown and caught as routine control flow.

Keep result types independent of HTTP. Map validation, missing-resource, and
conflict errors to the statuses required by the actual API contract (commonly
400, 404, and 409 respectively); inspect the existing contract rather than
assuming a universal mapping. Preserve response-body shapes and fixed messages
where required. Use explicit error-type branches. If a handler can return only a
subset of errors, its endpoint result type and OpenAPI metadata should declare
only those outcomes. Treat an impossible error branch as a programming defect,
not as a fallback business response.

When adapting request collections, account for JSON `null` values and null
elements where the transport can produce them despite non-nullable annotations.
Avoid dereferencing invalid data while mapping it. Preserve it until the
application layer can return the intended validation result; do not silently drop
invalid elements or replace them with defaults. Ensure validation happens before
references are checked or state is changed.

## Queries

Queries do not call write operations or mutate domain state. Return projected,
immutable snapshots rather than tracked entities. Define behavior for empty
collections according to the existing contract; a successful empty result
commonly maps to an empty JSON array. Preserve operation-specific behavior for
invalid identifiers and missing resources.

## HTTP endpoints and dependency injection

Keep endpoints thin: bind transport input, safely construct a command or query,
invoke the injected handler, and translate its result. Endpoints should not access
repositories, construct domain entities, or implement business rules.

Reuse a centralized, feature-independent HTTP result mapper when the application
has one or when several endpoints need consistent translation. Avoid importing
feature namespaces into shared HTTP code. Use explicit typed result unions and
small focused mapping methods instead of `IResult` everywhere or a configurable
mapping framework. Do not add impossible status/body combinations to the endpoint
contract.

Register handler interfaces explicitly using the application's established DI
lifetime and composition pattern. Avoid reflection scanning or service location
unless the repository already deliberately uses it and the task requires
preserving it. Register each route once and remove replaced implementations and
references when refactoring.

## Verification

Run the smallest relevant build and test commands available. Compare behavior
before and after for each changed use case, including success, validation,
missing-resource, conflict, headers, response shape, and unchanged state after a
rejected write. Check that endpoints do not depend on repositories, handlers do
not depend on HTTP or infrastructure, queries do not write, DI resolves handlers,
and shared HTTP code does not depend on features.

Do not weaken tests to hide regressions. Distinguish existing failures from
regressions, state which checks actually ran, and do not claim unmeasured coverage.
