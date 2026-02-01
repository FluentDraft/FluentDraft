---
description: Create a new version release with changelog and git tag
---

# FluentDraft Release Skill

This skill guides the process of creating a new release for FluentDraft.

## Project Info
- **Repository**: https://github.com/FluentDraft/FluentDraft
- **Version file**: `src/FluentDraft/FluentDraft.csproj`
- **Direct download URL**: `https://github.com/FluentDraft/FluentDraft/releases/latest/download/FluentDraft-Setup.exe`

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
   - **MINOR** (0.x.0): New features
   - **PATCH** (0.0.x): Bug fixes and improvements

### Step 2: Write User-Friendly Release Notes

**IMPORTANT**: Release notes should be written for END USERS, not developers!

#### What to INCLUDE (user-facing):
- ✅ New features users can see or use
- ✅ Bug fixes that affected user experience
- ✅ Performance improvements users will notice
- ✅ UI/UX changes

#### What to EXCLUDE (internal/technical):
- ❌ Commit hashes
- ❌ Internal refactoring
- ❌ CI/CD changes
- ❌ Code cleanup
- ❌ Developer tooling changes
- ❌ Technical implementation details

### Step 3: Generate Changelog

Update `CHANGELOG.md` with the following USER-FRIENDLY format:

```markdown
## [X.Y.Z] - YYYY-MM-DD

### ✨ What's New
- Clear description of new feature from user perspective

### 🐛 Bug Fixes
- What was broken and now works correctly

### ⚡ Improvements
- What became better/faster
```

**Example - BAD (too technical):**
```markdown
- Add UpdateService and IUpdateService for managing updates (#442c9c8)
- Refactor App.xaml.cs to use custom Main() entry point
- Fix IsSetupCompleted flag not being set correctly
```

**Example - GOOD (user-friendly):**
```markdown
- The app now automatically checks for updates when you start it
- Fixed an issue where the setup wizard appeared every time you launched the app
```

### Step 4: Update Version

1. Update version in `src/FluentDraft/FluentDraft.csproj`:
   ```xml
   <Version>X.Y.Z</Version>
   <AssemblyVersion>X.Y.Z.0</AssemblyVersion>
   <FileVersion>X.Y.Z.0</FileVersion>
   ```

### Step 5: Commit Version Changes

```bash
git add CHANGELOG.md src/FluentDraft/FluentDraft.csproj
git commit -m "chore(release): bump version to vX.Y.Z"
```

### Step 6: Create Git Tag with User-Friendly Message

```bash
git tag -a vX.Y.Z -m "Version X.Y.Z

✨ What's New
- Feature description for users

🐛 Bug Fixes
- Fix description for users"
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
- Summary of changes in user-friendly language
- Link to GitHub releases page: https://github.com/FluentDraft/FluentDraft/releases

## Notes

- GitHub Actions will automatically build and publish the release
- The installer is named `FluentDraft-Setup.exe`
- App automatically updates from GitHub Releases via Velopack
