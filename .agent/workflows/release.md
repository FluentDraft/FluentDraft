---
description: Create a new version release with changelog and git tag
---

# FluentDraft Release Skill

This skill guides the process of creating a new release for FluentDraft.

## Project Info
- **Repository**: https://github.com/FluentDraft/FluentDraft
- **Version file**: `src/FluentDraft/FluentDraft.csproj`
- **Release artifact**: `FluentDraft.exe`
- **Direct download URL**: `https://github.com/FluentDraft/FluentDraft/releases/latest/download/FluentDraft.exe`

## Prerequisites
Before running this skill, ensure:
- All changes are committed
- You are on the `main` branch
- Working directory is clean

## Release Process

### Step 1: Gather Release Information

1. **Get the last tag:**
   ```bash
   git describe --tags --abbrev=0
   ```

2. **Get commits since last tag:**
   ```bash
   git log $(git describe --tags --abbrev=0)..HEAD --oneline
   ```

3. **Get the current version from FluentDraft.csproj:**
   - Look for `<Version>X.Y.Z</Version>` in `src/FluentDraft/FluentDraft.csproj`

4. **Determine new version** based on changes:
   - **MAJOR** (x.0.0): Breaking changes
   - **MINOR** (0.x.0): New features (feat commits)
   - **PATCH** (0.0.x): Bug fixes and improvements (fix, refactor, perf commits)

### Step 2: Review Changes

1. **Generate a diff summary:**
   ```bash
   git diff $(git describe --tags --abbrev=0)..HEAD --stat
   ```

2. **List all commit authors:**
   ```bash
   git log $(git describe --tags --abbrev=0)..HEAD --format="%an" | sort -u
   ```

3. **Categorize commits by type:**
   - Group by: feat, fix, docs, refactor, perf, chore
   - Note any breaking changes

### Step 3: Generate Changelog

Create or update `CHANGELOG.md` with the following format:

```markdown
## [vX.Y.Z] - YYYY-MM-DD

### ✨ New Features
- Description of new feature (#commit-short-hash)

### 🐛 Bug Fixes  
- Description of fix (#commit-short-hash)

### 🔧 Improvements
- Description of improvement (#commit-short-hash)

### 📝 Documentation
- Description of docs change (#commit-short-hash)
```

### Step 4: Update Version

1. Update version in `src/FluentDraft/FluentDraft.csproj`:
   ```xml
   <Version>X.Y.Z</Version>
   ```

### Step 5: Commit Version Changes

```bash
git add CHANGELOG.md src/FluentDraft/FluentDraft.csproj
git commit -m "chore(release): bump version to vX.Y.Z"
```

### Step 6: Create Git Tag

```bash
git tag -a vX.Y.Z -m "Release vX.Y.Z

## Changelog
- Feature 1
- Fix 1
- etc."
```

### Step 7: Verify and Push

1. **Verify the tag:**
   ```bash
   git show vX.Y.Z
   ```

2. **Ask user for confirmation** before pushing

3. **Push with tags:**
   ```bash
   git push origin main --tags
   ```

## Output

After completion, provide:
- Summary of changes included in this release
- Link to GitHub releases page: https://github.com/FluentDraft/FluentDraft/releases
- Reminder that GitHub Actions will automatically build and publish the release

## Notes

- GitHub Actions workflow (`main.yml`) will automatically create a release when a tag is pushed
- The release artifact is named `FluentDraft.exe` (without version in filename)
- Users can always download the latest version from the direct download URL above
