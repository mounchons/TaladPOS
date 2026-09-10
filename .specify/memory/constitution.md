<!--
Sync Impact Report
==================
Version change: [TEMPLATE] → 1.0.0 (initial ratification)
Modified principles: N/A (first concrete fill of template placeholders)
Added sections:
  - I. Separation of API and Frontend
  - II. API Architecture & Technology Stack (DDD)
  - III. Test-First for Business Logic (NON-NEGOTIABLE)
  - IV. Frontend Technology Stack
  - Repository Structure (Section 2)
  - Development Workflow & Quality Gates (Section 3)
  - Governance
Removed sections: N/A (template placeholders only)
Templates requiring follow-up: none checked in this run (constitution-only scope;
  dependent templates/commands read this file at runtime and are not modified here)
Deferred/TODO items: none
-->

# TaladPOS Constitution

## Core Principles

### I. Separation of API and Frontend
The system MUST be built as two independently deployable applications: a backend API and a
frontend web application. The frontend MUST communicate with the backend exclusively through
the REST API contract; it MUST NOT connect to the database or any backend-internal component
directly, under any circumstance (including "temporary" scripts, admin tools, or reporting
features). Any data the frontend needs MUST be exposed through a versioned REST endpoint.

Rationale: enforcing a hard boundary at the network/API layer keeps the two applications
independently deployable and testable, prevents schema coupling between frontend and database,
and allows the API to evolve its persistence layer without breaking the frontend.

### II. API Architecture & Technology Stack (DDD)
The backend API MUST be implemented in .NET Core using ASP.NET Core Web API, with Entity
Framework Core (EF Core) as the data-access layer against PostgreSQL. The API MUST be organized
following Domain-Driven Design (DDD): business logic and invariants live in the domain layer,
independent of infrastructure and presentation concerns; application services orchestrate use
cases; EF Core entities/configuration and other infrastructure concerns MUST NOT leak domain
rules into controllers or persistence code.

Rationale: a DDD-organized API keeps business rules explicit, centralized, and testable in
isolation from the database and web framework, which is a prerequisite for Principle III.

### III. Test-First for Business Logic (NON-NEGOTIABLE)
All business logic (domain entities, domain services, application use-case logic) MUST have
unit test coverage. A change to business logic MUST NOT be merged without accompanying unit
tests that exercise the new or modified behavior. Tests MUST be runnable in isolation without a
live PostgreSQL database or network dependency (use in-memory/fakes/mocks at the infrastructure
boundary).

Rationale: business logic is the highest-value, highest-risk part of the API; requiring unit
tests keeps it correct and refactorable as the domain model evolves, and isolating tests from
the database keeps the suite fast and reliable.

### IV. Frontend Technology Stack
The frontend MUST be implemented using Next.js with Tailwind CSS for styling. All server data in
the frontend MUST be obtained by calling the backend REST API (Principle I); the frontend MUST
NOT embed direct database drivers, connection strings, or ORM clients.

Rationale: standardizing on Next.js + Tailwind CSS gives the team one frontend stack to
maintain, and strictly channeling data access through REST keeps Principle I enforceable in
practice, not just in intent.

## Repository Structure

The codebase MUST be split into clearly separated top-level locations for the API and the
frontend: `api/` for the ASP.NET Core Web API (DDD layers, EF Core, tests) and `web/` for the
Next.js + Tailwind CSS frontend. Whether `api/` and `web/` live in separate repositories or as
sibling folders in one repository, they MUST NOT share source code, build output, or a
dependency graph — the only permitted integration point between them is the REST API contract
defined in Principle I. Shared non-code artifacts (e.g., API contract/OpenAPI documentation)
MAY be referenced by both sides but MUST NOT introduce a compile-time or runtime code
dependency between `api/` and `web/`.

## Development Workflow & Quality Gates

Every pull request touching `api/` business logic MUST include or update unit tests per
Principle III before it can be merged; a reviewer MUST verify this explicitly. Every pull
request touching `web/` MUST be checked for direct database access, ORM usage, or database
connection configuration; any such code MUST be rejected and reworked to go through the REST
API instead. Reviewers MUST verify that changes respect the `api/` / `web/` boundary from the
Repository Structure section before approving.

## Governance

This constitution supersedes all other project practices and conventions for TaladPOS.
Amendments require: (1) a documented rationale for the change, (2) an explicit version bump
following the semantic versioning policy below, and (3) review/approval before merging the
amendment. All pull requests and code reviews MUST verify compliance with the principles above;
any deviation MUST be justified in the PR description or rejected. Complexity or architectural
exceptions (e.g., a proposed direct database access from the frontend) MUST be justified in
writing and approved before implementation, not discovered after the fact.

Versioning policy (semantic versioning for this document):
- MAJOR: backward-incompatible governance/principle removals or redefinitions.
- MINOR: a new principle or section added, or materially expanded guidance.
- PATCH: clarifications, wording fixes, or non-semantic refinements.

**Version**: 1.0.0 | **Ratified**: 2026-09-10 | **Last Amended**: 2026-09-10
