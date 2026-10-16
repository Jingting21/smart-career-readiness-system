# SmartCareer administrator setup

This console project creates exactly one explicitly requested Administrator account through ASP.NET Core Identity. It is not referenced by web startup and exposes no HTTP route. It never applies migrations.

From the repository root in PowerShell, collect credentials locally and store them in the shared development User Secrets store:

```powershell
$adminEmail = Read-Host "Administrator email"
$secureAdminPassword = Read-Host "Administrator password" -AsSecureString
$adminCredential = [System.Management.Automation.PSCredential]::new("unused", $secureAdminPassword)
$adminPassword = $adminCredential.GetNetworkCredential().Password
dotnet user-secrets set "AdminSetup:Email" $adminEmail --project .\SmartCareer.AdminSetup\SmartCareer.AdminSetup.csproj
dotnet user-secrets set "AdminSetup:Password" $adminPassword --project .\SmartCareer.AdminSetup\SmartCareer.AdminSetup.csproj
Remove-Variable adminPassword, adminCredential, secureAdminPassword
```

Review the target connection string, then explicitly run setup once:

```powershell
dotnet run --project .\SmartCareer.AdminSetup\SmartCareer.AdminSetup.csproj --configuration Release
```

After a successful run, remove the temporary setup credentials:

```powershell
dotnet user-secrets remove "AdminSetup:Email" --project .\SmartCareer.AdminSetup\SmartCareer.AdminSetup.csproj
dotnet user-secrets remove "AdminSetup:Password" --project .\SmartCareer.AdminSetup\SmartCareer.AdminSetup.csproj
Remove-Variable adminEmail
```

Then open `/Admin/Login`. The trusted developer-operated setup marks the supplied email confirmed because no SMTP confirmation flow exists yet. Existing accounts are always rejected and are never promoted or modified.
