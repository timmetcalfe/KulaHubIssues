sqlpackage /Action:Script `
  /SourceFile:"Database/bin/Debug/Database.dacpac" `
  /TargetConnectionString:"Server=tcp:sql-metsoft-test-uksouth.database.windows.net,1433;Initial Catalog=KulaHubDemo;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" `
  /OutputPath:"Database/bin/Debug/deploy.sql" `
  /Diagnostics:True