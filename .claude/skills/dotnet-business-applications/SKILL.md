---
name: dotnet-business-applications
description: Build or evolve .NET business applications using object-oriented domain modeling, Minimal APIs, Result-based business errors, EF Core persistence, repository and unit-of-work abstractions, and CQRS command handlers.
---

# .NET business applications

Use this skill for end-to-end implementation or refactoring of business application
features in .NET. It synthesizes the course topics of object-oriented design,
Minimal APIs, rich domain modeling, EF Core with SQLite, repository and unit of
work, and CQRS commands. Adapt all examples and choices to the user's request and
the actual repository; do not assume a particular domain, framework version,
folder layout, database, or API contract.

If asked only to edit this skill, do not modify application code.

## Discover before changing code

Inspect repository instructions, solution and project files, target framework,
existing architecture, domain types, routes, request/response contracts, errors,
persistence setup, dependency injection, configuration, and relevant tests.
Identify which layers and use cases the request actually affects. Treat the
checked-out code and explicit user requirements as the source of truth.

Record each affected use case's inputs, success response, status and headers,
expected errors, and business invariants. Preserve existing behavior unless a
change is requested. Avoid broad rewrites, speculative abstractions, and new
features inferred from unused types. If requirements conflict, resolve using
explicit user direction or clarify a consequential ambiguity before proceeding.

## Object-oriented and domain modeling

Model concepts using names and types from the business domain. Use encapsulation
to protect invariants: validate construction or use a validating factory, keep
setters private or otherwise restricted, and expose state changes through
meaningful methods. Apply the same rules during later state changes as during
creation.

Use immutable records or value objects for concepts defined by their values, such
as ranges or identifiers. Give value objects responsibility for their own
invariants and operations. For time intervals, define endpoint semantics
explicitly; half-open intervals `[start, end)` allow adjacent periods without
overlap.

Use inheritance for genuine "is a" relationships and shared substitutable
behavior. Use composition and interfaces for replaceable policies or collaborators
whose behavior varies. Prefer polymorphic behavior to repeated type checks when
adding new variants should not require editing existing consumers. Avoid
hierarchies that multiply combinations of independent options.

Do not expose mutable internal collections. Offer read-only views and domain
operations that preserve invariants. Inject dependencies through constructors;
depend on abstractions where replaceability or testing warrants it. Keep
abstractions purposeful and proportionate to the application.

## Minimal APIs and feature organization

Use ASP.NET Core Minimal APIs according to the existing project style. Register
services before building the application, map routes through focused endpoint
groups or feature route modules, and use dependency injection for endpoint
dependencies. Keep `Program.cs` focused on composition and application startup.

Organize related use cases by feature or vertical slice when appropriate. Keep
each use case's transport request/response and execution flow close together,
while placing genuinely shared contracts in shared locations. Avoid coupling one
feature to another feature's transport DTOs or invoking another feature's
endpoint/handler as a shortcut.

Use `TypedResults` and explicit result unions where supported. The declared
success and failure types should match the outcomes the operation can actually
produce. Use meaningful status codes: commonly 201 Created with a resource
location, 200 OK, 204 No Content, 400 for invalid input, 404 for missing
resources, and 409 for business conflicts. Confirm exact response bodies,
headers, serialization, route constraints, and error formats from the
application contract rather than assuming these defaults.

An endpoint binds and translates HTTP data, invokes the application use case, and
returns the HTTP outcome. It should not own reusable business rules or directly
manipulate persistence. Use route names and metadata consistently with the
project's OpenAPI conventions.

## Rich domain and Result-based errors

Place business invariants and state transitions in domain entities, value objects,
or domain policies. Application handlers coordinate the use case and call those
operations; they should not duplicate the rules. Prefer validated factories or
domain methods that prevent invalid objects and transitions.

Represent expected business outcomes as values, using the repository's existing
`Result<T>` and error abstractions. Do not create a competing result family.
Errors should carry stable machine-readable codes and a category/type; messages
may be made clearer over time. Return expected validation, not-found, and
conflict outcomes rather than throwing exceptions for routine control flow.
Reserve exceptions for unexpected failures and programming defects. Do not catch
unexpected exceptions and disguise them as business errors.

Keep domain errors independent of ASP.NET. Translate them at the HTTP boundary,
ideally through one shared mapper, using the actual contract. Common mappings are
validation to 400, not-found to 404, and conflict to 409. Do not map all failures
to 400 or silently use a generic fallback. A branch representing an impossible
error type should remain visible as a defect rather than becoming a false success
or an inaccurate response.

Inject `TimeProvider` where a business rule depends on the current time. Obtain
the instant at an application boundary and pass the relevant value into domain
operations. Do not read `DateTime.Now` or `DateTime.UtcNow` inside domain logic.
This keeps time rules deterministic and testable.

## EF Core and SQLite

Keep EF Core and provider-specific details in infrastructure. Use a `DbContext`
with `DbSet`s and focused `IEntityTypeConfiguration<T>` mappings, discovered
consistently by the application. Configure keys, required fields, lengths,
conversions, and owned/value types to match real domain semantics. If identifiers
are generated by the domain, configure EF not to generate them again.

Preserve domain invariants when EF materializes objects. Prefer supported
constructor or backing-field mapping and the smallest necessary persistence
adjustment. Do not add unrestricted public setters or bypass invariant-preserving
operations merely to simplify mapping. Account for provider-specific behavior,
including SQLite's date/time representation and limitations, when choosing
conversions and queries.

Register `DbContext` as Scoped. Use asynchronous EF operations and pass
cancellation tokens. Call `SaveChangesAsync` for writes; tracking `Add` or
mutation alone does not persist data. Use no-tracking reads or projections when
the result is read-only. Avoid keeping tracked entities longer than the intended
unit of work.

Use migrations for schema evolution when required. For a genuinely fresh,
disposable database, schema creation may use `EnsureCreated` if that fits the
project. Do not mix `EnsureCreated` and migrations for the same database, or
silently delete/recreate a database containing user data. Keep development
seeding explicitly environment-gated and idempotent, and construct seed entities
through valid domain operations.

## Repository and Unit of Work

Decide whether repositories and a unit-of-work abstraction add value in the
application; do not add them mechanically. They are useful when application
logic should depend on domain-owned persistence contracts, when writes need a
clear commit boundary, or when tests need persistence fakes. A repository that
merely forwards every `DbSet` method can add ceremony without useful behavior;
direct `DbContext` use in an application boundary may be appropriate when the
project intentionally chooses that simpler design.

When using these abstractions, define interfaces outside infrastructure and keep
provider types out of their signatures. Implement them in infrastructure. Give a
repository only the operations a use case needs; avoid a generic repository
without meaningful shared behavior. Repositories may track or stage changes,
while a Unit of Work commits related changes together. Do not save inside each
repository method if a use case must atomically persist multiple operations.

Register the DbContext, repositories, unit of work, and handlers with compatible
Scoped lifetimes so all participating abstractions in a request share the same
context. Ensure the Unit of Work resolves to that same scoped context.
Cancellation tokens and asynchronous behavior should flow through the
abstractions.

## CQRS command handlers

A command represents one request to change application state. Use an immutable
command type containing only the data needed by its use case and a handler
dedicated to that command. The handler orchestrates loading aggregates,
delegating invariants and transitions to domain methods, staging persistence
changes, committing through the Unit of Work where used, and returning a result.

Keep handlers independent of HTTP and concrete infrastructure. Inject only the
dependencies needed for the use case, such as repository interfaces, a unit of
work, and `TimeProvider`. Return expected business failures as results and avoid
saving after a failed domain operation. Preserve cancellation and unexpected
infrastructure failures.

Keep command endpoints thin: bind the HTTP request, construct the command, call
the handler through its application contract, then map its result to the API
response. Do not mutate entity state directly in the handler when a domain
operation exists. Register handlers explicitly or use the repository's existing
startup registration approach; avoid runtime service location.

## Verification

Run the smallest relevant build and test commands, then broaden validation if
needed. Verify the exact requested behavior through the actual boundary:
domain invariants and transitions, result/error categories, persistence across
contexts or restart when relevant, and HTTP status/body/headers. Check that failed
commands do not partially change state, queries do not mutate state, and
adjacent time intervals behave as specified.

Use controllable time rather than sleeps or the system clock in time-dependent
tests. Use isolated persistence in integration tests and verify stored state
through a fresh context. Do not weaken assertions to hide defects. Report actual
commands and outcomes, distinguish pre-existing issues from regressions, and
state clearly which checks could not run.
