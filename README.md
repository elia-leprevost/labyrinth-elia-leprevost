# Labyrinth – Projet C# (.NET)

Ce dépôt contient :

- **ApiTypes** : DTO (issus du swagger) partagés client/serveur.
- **Labyrinth.Core** : bibliothèque (représentation, crawlers, client API, exploration multi-agents).
- **Labyrinth.ConsoleClient** : client console (local ou distant).
- **Labyrinth.TrainingServer** : serveur d'entraînement (Minimal API + Swagger).
- **Labyrinth.Tests** : tests unitaires (xUnit).

## Démarrage rapide

### 1) Local (sans serveur)
```bash
dotnet run --project Labyrinth.ConsoleClient
```

### 2) Serveur d'entraînement (local)
```bash
dotnet run --project Labyrinth.TrainingServer
```
Swagger : http://localhost:5000/swagger

Puis (dans un autre terminal) :
```bash
dotnet run --project Labyrinth.ConsoleClient -- http://localhost:5000 <APPKEY_GUID>
```

### 3) Serveur de compétition
```bash
dotnet run --project Labyrinth.ConsoleClient -- https://labyrinth.syllab.com <APPKEY_GUID> [settings.json] [agents=3] [seconds=60]
```

## Arguments du client console

```
Labyrinth.ConsoleClient <serverUrl> <appKeyGuid> [settings.json] [agents=3] [seconds=60]
```

- `agents` est borné à 1..3 (limite serveur).
- Par défaut le client lance 3 agents asynchrones partageant une carte via `MapCoordinator`.

## CI / Release

Workflow GitHub Actions : `.github/workflows/dotnet.yml`

- build + tests + artifacts
- sur tag `v*` : création d'une release contenant un zip du binaire publié du client console.
