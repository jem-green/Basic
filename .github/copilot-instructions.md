# Copilot Instructions

## Project Guidelines
- C# Development Behavior Contract: (1) Never rewrite existing code unless explicitly requested, (2) Do not duplicate logic that already exists, (3) Modify only the specific functions/structs/files named by user, (4) Preserve all existing naming/patterns/macros/architecture, (5) Extend codebase using smallest possible change, (6) Reuse existing helpers and patterns - do not introduce new abstractions/helpers/types unless explicitly asked, (7) If something exists, reference it instead of recreating, (8) Output only changed sections or unified diff - not full files, (9) Before writing code, propose minimal change plan and wait for approval, (10) Maintain deterministic/minimal/localized modifications.

## EBNF File Updates
- When updating BASIC EBNF files, target the 8K version and keep changes minimal and aligned to the existing EBNF style.