# Perimeter Data Gateway v0.1 — Documentation

This directory contains the normative public documentation set for **Perimeter Data Gateway (PDG) v0.1**.

## Reading order

1. [`technical-requirements-v0.1.md`](technical-requirements-v0.1.md) — approved requirements for the v0.1 Preliminary Design / Prototype / Proof of Feasibility stage.
2. [`preliminary-design-explanatory-note-v0.1.docx`](preliminary-design-explanatory-note-v0.1.docx) — engineering rationale, trust-boundary analysis, design alternatives, residual risks, and preliminary-design groundwork.
3. [`technical-working-project-explanatory-note-v0.1.docx`](technical-working-project-explanatory-note-v0.1.docx) — implementation-level Technical Working Project (TWP) design.
4. [`appendix-a-implementation-file-manifest-v0.1.docx`](appendix-a-implementation-file-manifest-v0.1.docx) — complete planned implementation file manifest and acceptance-test file mapping.
5. [`appendix-b-ef-core-migrations-and-platform-store-bootstrap-v0.1.docx`](appendix-b-ef-core-migrations-and-platform-store-bootstrap-v0.1.docx) — normative clarification of the Platform Store schema lifecycle, EF Core Migrations, migration bundle, bootstrap identities, grants, seed, and security verification.

## Document precedence

- **Technical Requirements** define what PDG v0.1 must provide and are approved.
- The **Preliminary Design Explanatory Note** explains the architectural rationale and the engineering hypothesis.
- The **Technical Working Project Explanatory Note** fixes implementation-level design decisions.
- **Appendix A** is the implementation file manifest used to prevent omissions during implementation.
- **Appendix B** is mandatory for the TWP documentation set and supersedes any earlier wording that could be read as allowing SQL to create the Platform Store application schema/tables, or allowing the runtime API identity to apply EF Core Migrations.

Where Appendix B refines an earlier Platform Store bootstrap description, **Appendix B takes precedence for that subject**.

## Language and public-repository policy

The public documentation set is maintained in **English**. Russian originals and superseded working documents are retained outside the public source tree as project archive material.

The public source tree must not contain secrets, production credentials, local `.env` files, or other private runtime configuration.

## Current document status

- Technical Requirements v0.1 — **Approved**.
- Preliminary Design Explanatory Note v0.1 — **Issued for approval**.
- Technical Working Project Explanatory Note v0.1 — **Draft for approval**.
- Appendix A — **Draft for approval together with the TWP Explanatory Note**.
- Appendix B — **Draft for approval together with the TWP Explanatory Note**.
