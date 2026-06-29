# Altair BASIC (August 1975) - Feature Analysis Report

## Executive Summary

This document compares the Altair BASIC implementation against the EBNF specification and identifies gaps.

## Tokens Defined in Tokenizer.cs

### Statements (from Token enum):
- ✅ DATA, DEF, DIM
- ✅ FOR/NEXT, GOSUB/RETURN, GOTO
- ✅ IF...THEN, INPUT
- ✅ LET (optional in implementation)
- ✅ ON
- ✅ PRINT
- ✅ READ, REM, RESTORE
- ✅ END, STOP
- ❌ **CHANGE** - Token does NOT exist in enum (lines 43-131)
- ⚠️ **OPTION** - Token exists (line 66) but NOT in EBNF or statement switch

### String Functions (in Tokenizer, NOT in EBNF):
- CHR$() - Convert number to character
- LEFT$() - Left substring
- RIGHT$() - Right substring
- MID$() - Mid substring  
- STR$() - Convert number to string
- ASC() - Get ASCII value
- LEN() - String length
- VAL() - Convert string to number

### Logical Operators (in Tokenizer, NOT in EBNF):
- AND, OR, XOR, NOT

### Math Functions (in Tokenizer, some NOT in EBNF):
- SGN() - Sign function
- (Plus standard: SIN, COS, TAN, ATN, EXP, LOG, SQR, ABS, INT, RND)

### Relational Operators:
- <, >, =, <=, >=
- <> (not-equal) - Token exists (TOKENIZER_NOTEQ, line 97)

## EBNF vs Implementation Gaps

### Missing from EBNF (but in Tokenizer):
1. **String Functions**: CHR$, LEFT$, RIGHT$, MID$, STR$, ASC, LEN, VAL
2. **Logical Operators**: AND, OR, XOR, NOT
3. **Math Function**: SGN
4. **Relational Operator**: <>
5. **Statement**: OPTION (token exists but unclear what it does)

### Missing from Both EBNF and Implementation:
1. **CHANGE statement** - Defined in EBNF line 12, but:
   - NO token in Tokenizer
   - NO case in statement switch
   - NO implementation method

### Altair-Specific Enhancements Over Dartmouth:
1. ✅ **Optional LET** - Can omit LET keyword
2. ✅ **Multiple NEXT variables** - `NEXT I,J,K`
3. ✅ **ON...GOSUB** - In addition to ON...GOTO/THEN

## Statement Switch Analysis (Interpreter.cs lines 537-649)

### Implemented Statements:
- INPUT (539), DATA (544), RESTORE (549)
- PRINT (554), IF (559), GOTO (564)
- GOSUB (569), RETURN (574), FOR (579)
- NEXT (584), END (589), STOP (594)
- LET (599 - explicit, 636-642 - implicit via variable tokens)
- REM (605), DIM (610), READ (615)
- DEF (621), ON (626), RANDOMIZE (631)

### NOT in Statement Switch:
- ❌ CHANGE (in EBNF, not implemented)
- ⚠️ OPTION (token exists, not implemented)

## Functions Analysis

### EBNF Defined Functions (lines 47-62):
- SIN, COS, TAN, ATAN (note: Tokenizer uses ATN not ATAN)
- EXP, ABS, LOG, SQR
- INT, RND
- FN (user-defined)
- TAB (for PRINT formatting)

### Additional Functions in Tokenizer (NOT in EBNF):
- CHR, RIGHT, LEFT, MID
- VAL, ASC, LEN
- SGN, STR

## Recommendations

### Priority 1: Update EBNF to Match Implementation
The EBNF is **incomplete** - it's missing many features that are actually implemented:

1. Add string functions to EBNF
2. Add logical operators to EBNF
3. Add SGN function to EBNF
4. Add <> relational operator to EBNF
5. Change ATAN to ATN (to match tokenizer)

### Priority 2: Remove CHANGE from EBNF
Since CHANGE has no token and no implementation, either:
- Remove it from EBNF (if not in 1975 spec), OR
- Implement it (if it IS in 1975 spec)

### Priority 3: Clarify OPTION Statement
- Document what OPTION does
- Either implement it or remove the token

## PDF Verification Needed

To complete this analysis, we need to check the `MITS_AltairBasic_1975_Aug75.pdf` for:

1. **Does it include CHANGE?**
   - If YES: Implement it
   - If NO: Remove from EBNF

2. **Does it include string functions?**
   - If YES: Add to EBNF
   - If NO: Document why they're in the implementation

3. **Does it include logical operators (AND/OR/XOR/NOT)?**
   - If YES: Add to EBNF and verify implementation
   - If NO: Document why they're in the tokenizer

4. **What is OPTION statement?**
   - BASE 0/1 for array indexing?
   - Something else?

## Historical Note

Altair BASIC 1975 was written by Bill Gates and Paul Allen for the MITS Altair 8800. It came in several versions:
- 4K BASIC (minimal)
- 8K BASIC (Extended)

The extended version likely included the string functions and logical operators we see in the tokenizer.

---
*Generated: Automated analysis comparing EBNF, Tokenizer, and Interpreter implementations*
