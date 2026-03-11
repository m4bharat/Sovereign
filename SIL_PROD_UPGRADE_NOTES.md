# Sovereign production upgrade pass

This archive includes a substantial implementation pass toward the requested roadmap items:
1. JWT auth with registration, login, and `/api/auth/me`
2. PBKDF2 password hashing
3. Hardened AI decision engine with schema validation and safer prompt construction
4. Context-aware memory retrieval and semantic memory search
5. Relationship feedback loop via outcome recording
6. LinkedIn Chrome extension MVP with injectable comment suggestions
7. Dashboard API + Angular dashboard for momentum, decay alerts, and recent memories

Important: the .NET SDK was not available in this environment, so runtime compile and test verification could not be completed here.
Run `dotnet restore`, `dotnet build`, `dotnet test`, Angular build, and extension smoke tests before production deployment.
