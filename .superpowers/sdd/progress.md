Task 1: complete (commits 33b110b..ba7bdd3, review clean; minor notes: runtime behavior deferred to Task 2 VM validation, regasm version preference caveat)
Task 2: local validation complete; real VM validation still required

## Task 2 local verification (2026-07-07)

- `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tests\RegisterScriptVmCompat.Tests.ps1` → PASS
- PowerShell-scoped deterministic failure smoke:
  - `$env:QT_TABBAR_REGISTER_VALIDATE_ONLY='1'; $env:QT_TABBAR_REGISTER_FORCE_NO_ENV='1'; cmd /c '.\Register\Register.bat Release'`
  - Output: `Register environment bootstrap failed.` + Developer Command Prompt guidance
  - Exit code: 1
  - No `REG ADD` or `start taskmgr` side effects observed

## Real VM validation (manual, pending)

Run on a clean Windows VM with only repo + Build Tools (no pre-opened Dev Prompt):

```
Register\Register.bat Release
```

Verify:
- `REGISTER_ENV_SOURCE` is set (modern VS or VS2010 fallback)
- regasm discovery succeeds
- registration completes without manual env setup
