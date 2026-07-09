# Europe Data Protection Architecture

## Purpose

This note defines the technical direction for issue 573: introduce a defensible privacy boundary for Europe-regulated personal data.

The main architectural decision is that this work should be treated as a data-governance and access-boundary problem, not only as a role split.

## Why This Is Different From Normal Authorization Work

The existing platform mostly models access in terms of application roles plus NGB-level jurisdiction checks. That is useful for normal administration, but it is not sufficient if the requirement is that personal data connected to Europe must not be accessible to people outside that restricted privacy area.

From a legal and operational point of view, the platform must be able to answer all of these questions clearly:

- Which data is personal data?
- Which data is especially sensitive?
- Which region-specific rules apply to each record?
- Which users may access that data?
- Which operators may administer the systems storing that data?
- Which logs, backups, exports, and support tools can expose that data?

If the answer to those questions is only encoded in controller role attributes, the boundary is too weak.

## Legal Treatment Summary

This note is technical guidance, not legal advice, but the architecture should reflect the following GDPR and UK GDPR realities:

- Personal data is any information relating to an identified or identifiable natural person.
- Operational data such as account identity, role assignments, team affiliations, tournament participation, and user attributes can all be personal data.
- Some data may become special-category data depending on field meaning and use. Health-related data, sex-life or sexual-orientation data, and some sensitive identity data require stronger safeguards and a specific legal basis.
- Cross-border access can still be regulated processing even when the database stays on one server.
- International transfers or disclosures outside the protected area may require additional safeguards.
- The controller must be able to demonstrate lawful basis, data minimization, retention control, security, and transparency.
- A DPIA is likely warranted before broad rollout because the design introduces region-specific access boundaries over personal data.

## Recommended Architecture

### 1. First-Class Privacy Scope

Introduce a first-class privacy scope model that is separate from application role names.

At minimum, the platform should distinguish between:

- Global
- European Economic Area
- United Kingdom
- Switzerland

This allows the platform to express both unrestricted data and data that requires regional handling.

### 2. Separate Role and Scope Concepts

The system should treat these as different questions:

- What can this user do?
- Which privacy scopes may this user act within?

An admin role alone should not imply access to every privacy scope. Scope grants should be explicit, separately auditable, and separately revocable.

### 3. Scope Enforcement in Queries

Privacy scope must be enforced in the data-access path, not only in controllers.

Required pattern:

- resolve the caller's allowed scopes
- resolve the target resource scope
- constrain the query before projecting data
- deny access when scope is unknown or mismatched

Avoid fetch-then-filter patterns for protected data.

### 4. Operational Boundary

Application authorization is only one layer. The team also needs to define:

- who can deploy region-restricted systems
- who can access production databases
- who can read logs and telemetry
- who can restore backups
- who can use impersonation or support tooling
- where secrets are stored and who can rotate them

If non-region operators retain broad backend access, then the privacy boundary is not credible even if the API is correct.

### 5. Preferred Deployment Model

The cleanest design is separate deployment or separate hard tenant isolation for region-restricted data.

If the platform stays in a shared deployment, then the following become mandatory:

- first-class privacy scope on protected data
- consistent scope-aware authorization
- scope-aware data filtering in storage and service layers
- audit events for elevated access and denied cross-scope attempts
- operational controls for secrets, logs, backups, and support access

## Proposed Rollout Plan

### Phase 1: Foundation

- add a first-class privacy scope model
- add deterministic mapping from NGB country code to privacy scope
- document restricted jurisdictions and assumptions
- add unit tests for the classifier

### Phase 2: Identity and Authorization

- add scope grants to user/admin context
- introduce scope-aware authorization helpers and policies
- review impersonation and tech-admin access

### Phase 3: Data Classification

- identify which entities need explicit stored scope versus inherited scope
- define inheritance rules for users, teams, tournaments, exports, and attributes
- deny writes that would create ambiguous or cross-scope derived data

### Phase 4: API Enforcement

- enforce scope filters in read endpoints
- enforce scope checks in update and delete flows
- review export endpoints and support/admin utilities separately

### Phase 5: Operations

- separate secrets and operational access paths
- review backup placement and restore permissions
- verify logging and telemetry minimization
- define break-glass process and audit requirements

## Current Branch Foundation

This branch starts the architecture with a small backend foundation:

- `PrivacyScope` enum to represent protected handling classes
- `NgbPrivacyScopeClassifier` to map existing NGB country codes into those classes
- unit tests covering representative jurisdictions

This intentionally does not change authorization behavior yet. It creates a stable seam for future scope-aware enforcement.

## RFC Checklist

Use this checklist as the implementation tracker for the privacy-boundary rollout.

### Scope Model

- [x] Introduce first-class privacy scope enum.
- [x] Introduce NGB-to-privacy-scope classifier.
- [ ] Confirm jurisdiction list with legal and operations owners.
- [ ] Decide whether UK and Switzerland remain separate scopes or merge into a single restricted scope.

### Identity and Context

- [x] Add privacy scopes to user context model.
- [x] Derive context scopes from role/NGB constraints.
- [ ] Add persistence-level scope assignment for entities that require explicit storage.
- [ ] Define behavior for multi-scope users and cross-scope accounts.

### Authorization

- [x] Add reusable user-to-scope and user-to-user scope checks.
- [x] Enforce scope checks on exports.
- [x] Enforce scope checks on impersonation.
- [x] Enforce scope checks on high-risk NGB admin flows.
- [x] Enforce scope checks on sensitive user-attribute admin endpoints.
- [ ] Enforce scope checks across all other NGB/team/tournament mutation endpoints.
- [ ] Enforce scope checks in background-job entry points and worker-side execution.

### Data Access

- [ ] Ensure protected queries are scope-filtered before projection.
- [ ] Remove or refactor fetch-then-filter patterns for protected data.
- [ ] Add guardrails for writes that could create ambiguous scope inheritance.

### Operations

- [ ] Define scoped production access policy (DB, logs, backups, secrets, deploys).
- [ ] Implement scoped break-glass access workflow with audit trail.
- [ ] Confirm telemetry/log redaction for personal and special-category data.

### Testing and Verification

- [x] Add classifier unit tests for representative jurisdictions.
- [x] Add authorization tests for cross-scope denial scenarios.
- [ ] Add integration tests for export, impersonation, and user-admin boundaries.
- [ ] Add regression tests for existing authorized in-scope paths.

### Compliance Workstream

- [ ] Start and complete DPIA for privacy-scope rollout.
- [ ] Confirm lawful-basis mapping and data-category treatment with legal counsel/DPO.
- [ ] Update privacy notices and internal processing records.

## Open Questions For The Team

- Is the business boundary intended to be EU only, EEA, or broader Europe?
- Should UK and Switzerland be separate scopes or part of one restricted-Europe scope?
- Are IQA admins allowed emergency access across scopes, or is cross-scope support forbidden?
- Is separate deployment feasible, or must this remain a shared deployment?
- Which records should carry explicit stored scope rather than inherited scope?
- Which existing exports or admin endpoints are highest risk for cross-scope exposure?

## Recommendation

Move forward as if this will require hard scope enforcement, not just new role names. If legal guidance later relaxes that requirement, the system can still support it. The reverse is harder: a role-only model is not a reliable base for a stronger privacy boundary.