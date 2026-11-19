---
name: efcore-sqlite-repositories
description: Add or adapt EF Core SQLite persistence behind existing .NET repository abstractions without changing application contracts or domain behavior.
---

# EF Core SQLite persistence

## Purpose and inspection

Use this skill when asked to add EF Core SQLite persistence or implement existing
repository interfaces with SQLite. If asked only to edit this skill, do not
generate or modify application code.

Inspect repository instructions, solution and project files, target framework,
package management, domain entities, repository and unit-of-work contracts,
current implementations, handlers, dependency injection, configuration, and
tests. Confirm actual namespaces, signatures, null behavior, result semantics,
ordering, and write behavior. Do not assume example type names, paths, package
versions, or storage requirements.

Inventory each persisted aggregate's identity, constructors, private setters,
backing fields, collections, computed properties, and invariants. Choose and
explain a materialization strategy that restores existing identity and state
without replaying business operations or generating new identifiers.

Keep the change scoped to persistence. Preserve interfaces, feature/handler
contracts, HTTP behavior, and domain rules unless the user explicitly requests
otherwise. Keep EF Core dependencies in infrastructure, not in domain
abstractions or feature handlers.

## Dependencies and layout

Use `Microsoft.EntityFrameworkCore.Sqlite` at a stable version compatible with
the existing target framework and EF packages. Follow central package management
if present. Add design-time dependencies only when needed. Do not change the
target framework to accommodate a package without a requirement.

Follow the repository's established infrastructure layout. A typical organization
is:

```text
Infrastructure/
  Persistence/
    <Application>DbContext.cs
    Configurations/
    PersistenceServiceCollectionExtensions.cs
  Repositories/
    Ef<Aggregate>Repository.cs
```

Use explicit entity configurations where appropriate. Configure keys and value
generation to match the domain's identity rules. Keep in-memory implementations
if tests or other consumers use them; replace only the runtime registrations
required by the task.

## Mapping and domain integrity

- Map persisted fields explicitly. Ignore computed properties and avoid changing
  domain behavior merely to simplify EF mapping.
- Preserve collection ordering, duplicates, empty collections, decimal precision,
  and value semantics where the application relies on them. If serializing
  collections, use a reversible representation and a structural value comparer
  with an independent snapshot when change tracking requires it.
- Configure private/backing-field access only where needed. Prefer supported
  constructor binding and the smallest invariant-preserving domain adjustment.
  Do not add public setters, reflection-based mutation, EF-specific public
  construction paths that bypass invariants, or replay domain operations during
  materialization.
- Map aggregate-owned children and relationships according to existing
  ownership/deletion semantics. Do not invent uniqueness constraints or
  cross-aggregate foreign keys.
- If optimistic concurrency is required, use a token the provider can actually
  support and preserve its original value. Do not claim aggregate-wide protection
  from a child token alone.
- Add only constraints supported by defined application semantics. Do not
  silently change rounding, deletion, or identity behavior to suit SQLite.

If existing domain behavior cannot be materialized safely, report the specific
blocker rather than weakening the domain or changing unrelated handlers.

## Repository behavior

Inject a scoped DbContext and use real asynchronous EF operations with
cancellation tokens. Reads should not save changes. Return fully loaded
aggregates where callers require their owned data, and preserve established
tracking and ordering expectations.

Successful writes save before returning success. Preserve existing result types,
error codes/messages, and semantics for duplicate inserts, null inputs, and
missing updates/deletes. An update is not an upsert unless the existing contract
explicitly says so. Handle tracked and detached aggregates without attaching two
instances of the same key. Synchronize child collections without deleting and
recreating retained children when that would lose identity or state.

Treat an aggregate write atomically. After an expected failed save, ensure failed
pending changes cannot be accidentally committed later in the same scope. Do not
return tracked unsaved values as persisted data.

Map only known, expected provider failures (such as a specific uniqueness or
concurrency constraint) into business results. A pre-check alone does not prevent
a duplicate-insert race. Unexpected connection, schema, disk, or programming
failures should propagate through the application's normal error handling.
Never catch every exception and convert it to false, validation, or not-found.
Do not retry writes blindly.

Avoid unsupported SQLite expressions, especially around decimals. Inspect actual
query requirements before choosing server-side versus client-side evaluation.

## Composition and database lifecycle

Register DbContext and repositories with scoped lifetimes. Provide a composition
point that allows the host and tests to configure SQLite without competing
configuration delegates. Keep existing application service registrations intact.
Use the application's normal configuration mechanism for the file database.

Document how a new database is initialized. `EnsureCreated` can suit a fresh,
disposable database; use migrations when schema evolution or existing data
requires them. Never mix `EnsureCreated` and migrations for the same database.
Do not silently delete or recreate a user's database, seed business data, or
ignore a test-provided connection.

## Test handoff and verification

Coordinate SQLite in-memory setup with the existing test architecture. For tests
using `Data Source=:memory:`, keep the connection open through schema creation
and all requests, and dispose the host/scopes before the connection. Each test
should have an isolated database. If concurrent contexts share one in-memory
database, use a unique named shared-memory database with a keep-alive connection
only if that matches the provider/test constraints. Do not add test-only branches
to production code.

Verify round trips in a fresh context for identifiers, scalar values, collections,
aggregate children, and concurrency state. Exercise successful create/read/update/
delete, duplicate and missing-resource behavior, child synchronization, isolated
databases, failed-write cleanup, and read-only requests where applicable. Verify
that interfaces and feature handlers remain unchanged unless an explicit
requirement necessitates a narrow change.

Run the relevant build and tests and report actual commands and outcomes. Explain
mapping decisions, database initialization, and test connection setup. State
clearly which checks could not run; do not claim unverified persistence behavior.
