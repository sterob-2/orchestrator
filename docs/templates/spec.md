# Spec: Issue {{ISSUE_NUMBER}} - {{ISSUE_TITLE}}

STATUS: DRAFT
UPDATED: {{UPDATED_AT_UTC}}

## Goal
Describe the goal in 2-3 sentences.

## Non-Goals
- ...

## Components
- ...

## Touch List
| Operation | Path | Notes |
| --- | --- | --- |
| Modify | src/Example.cs | ... |

## Interfaces

CRITICAL: Show concrete BEFORE/AFTER examples for each file in the Touch List.
- For MODIFY operations: Show the exact code being changed with clear before/after
- For ADD operations: Show the new code to be added
- For DELETE operations: Show what is being removed
- Use actual code from the repository, not simplified examples
- When removing code, show it in BEFORE but completely absent in AFTER

Example for MODIFY operation (removing methods):
```csharp
// BEFORE: src/Example.cs
public interface IExample
{
    Task DoSomethingAsync();
    Task ObsoleteMethodAsync();  // ← This method will be removed
    Task AnotherMethodAsync();
}

// AFTER: src/Example.cs
public interface IExample
{
    Task DoSomethingAsync();
    // ObsoleteMethodAsync removed completely
    Task AnotherMethodAsync();
}
```

Example for ADD operation:
```csharp
// NEW: src/NewFile.cs
public class NewClass
{
    public void NewMethod() { }
}
```

## Scenarios
Scenario: Example
Given ...
When ...
Then ...

Scenario: ...
Given ...
When ...
Then ...

Scenario: ...
Given ...
When ...
Then ...

## Sequence
1. ...
2. ...

## Test Matrix
| Test | Files | Notes |
| --- | --- | --- |
| Unit | tests/ExampleTests.cs | ... |
