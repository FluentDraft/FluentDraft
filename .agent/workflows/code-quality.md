---
description: Analyze and fix code quality, style, and security issues using .NET built-in tools
---

# Code Quality & Cleanup Skill

This skill guides the process of analyzing the codebase for strict code quality standards, enforcing style, and fixing issues automatically where possible.

## Prerequisites
- Working directory is clean (recommended to commit changes before running)

## Workflow

### Step 1: Format Code

Run the standard .NET formatter to fix whitespace and basic style issues automatically.

```powershell
dotnet format whitespace
dotnet format style
```

### Step 2: Enable Strict Analysis

To catch deeper issues (performance, reliability, security), we temporarily enable strict analysis mode in the project file.

1. **Modify `src/FluentDraft/FluentDraft.csproj`**:
   Add the following properties to the main `<PropertyGroup>`:
   ```xml
   <AnalysisLevel>latest-all</AnalysisLevel>
   <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
   <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
   ```

### Step 3: Run Analysis (Build)

Run the build to generate the report of issues.

```powershell
dotnet build
```

**Agent Action:**
- If the build fails with errors (which are actually warnings treated as errors), read the output.
- Identify the files and lines causing issues.
- **Iteratively fix** the issues in the code.
  - Focus on Security, Reliability, and Performance warnings first.
  - Naming/Style warnings can be fixed or suppressed if they conflict with project conventions.

### Step 4: Verification

Run the build again to ensure all critical issues are resolved.

```powershell
dotnet build
```

### Step 5: Cleanup & Revert Configuration

Once the code is clean, you can choose to keep the strict settings or revert them to avoid annoying the user during rapid prototyping.

1. **Revert changes to `src/FluentDraft/FluentDraft.csproj`** (Remove the lines added in Step 2), UNLESS the user explicitly asked to permanent enforce these rules.

2. **Commit the code fixes**:
   ```bash
   git add .
   git commit -m "refactor: code quality cleanup"
   ```
