# Submission Package Checklist

Use this checklist before creating the final submission archive.

## 1. Source Scope

- [ ] Include `Assets/_Project/`
- [ ] Include required runtime assets under `Assets/`
- [ ] Include `Packages/manifest.json`
- [ ] Include `ProjectSettings/`
- [ ] Include `TEST/`
- [ ] Include all project documentation files

## 2. Exclusions

- [ ] Exclude `Library/`
- [ ] Exclude `Logs/`
- [ ] Exclude `obj/`
- [ ] Exclude `.vs/`
- [ ] Exclude `UserSettings/`
- [ ] Exclude `.git/`
- [ ] Exclude `*.csproj`
- [ ] Exclude `*.sln`
- [ ] Exclude `user.keystore`
- [ ] Exclude `Assets/OpenCVForUnity/` from the final archive

## 3. Attribution and Licensing

- [ ] `THIRD_PARTY_COMPONENTS.md` is included
- [ ] `SOURCE_ATTRIBUTION.md` is included
- [ ] Team-developed files in `Assets/_Project/` contain attribution headers
- [ ] Third-party packages and assets are clearly identified
- [ ] Redistribution constraints for third-party assets are documented
- [ ] Audio asset origin and usage conditions are documented
- [ ] The OpenCVForUnity exclusion and restore method are documented

## 4. Documentation

- [ ] `README.en.md` is included
- [ ] `README.zh-CN.md` is included
- [ ] `USER_MANUAL.en.md` is included
- [ ] `USER_MANUAL.zh-CN.md` is included
- [ ] `BUILD_GUIDE.zh-CN.md` is included
- [ ] `DEVELOPER_HANDOVER.en.md` is included
- [ ] `DEVELOPER_HANDOVER.zh-CN.md` is included
- [ ] Testing documents in `TEST/` are included

## 5. Final Verification

- [ ] Unity project opens without missing team-developed source files
- [ ] No vulgar or temporary internal project naming remains in submitted documentation
- [ ] The archive contents match the documented submission scope
- [ ] The final archive can be extracted successfully on another machine
- [ ] Third-party redistribution decisions have been finalised before packaging
