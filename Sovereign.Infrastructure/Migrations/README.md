Create EF migrations from the Infrastructure project using the API startup project.

Example:
dotnet ef migrations add InitialHardening --project src/Sovereign.Infrastructure --startup-project src/Sovereign.API
dotnet ef database update --project src/Sovereign.Infrastructure --startup-project src/Sovereign.API
