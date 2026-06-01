---
title: Camada de Dados — PokeCollection
version: 1.0.0
domain: data
---

# Camada de Dados — EF Core, Modelos e Serviços

Leia este documento ao trabalhar em `Data/`, `AppDbContext.cs` ou ao adicionar/modificar lógica que acessa o banco de dados.

---

## 1. Modelos de domínio

Os modelos persistidos ficam em `Data/Models/`. São entidades do banco de dados gerenciadas pelo EF Core.

### PokemonSet

```csharp
public class PokemonSet
{
    public int Id { get; set; }
    public string ExternalId { get; set; } = "";   // ID da API (ex: "sv01")
    public string Name { get; set; } = "";
    public string Series { get; set; } = "";
    public DateTime? ReleaseDate { get; set; }
    public int TotalCards { get; set; }
    public string Symbol { get; set; } = "";
    public string Logo { get; set; } = "";
    public List<PokemonCard> Cards { get; set; } = new();
}
```

### PokemonCard

```csharp
public class PokemonCard
{
    public int Id { get; set; }
    public string ExternalId { get; set; } = "";   // ID da API (ex: "sv01-001")
    public int PokemonSetId { get; set; }
    public PokemonSet? Set { get; set; }
    public string Name { get; set; } = "";
    public string Number { get; set; } = "";        // número local no set (ex: "001")
    public string Rarity { get; set; } = "";
    public string ImageSmallUrl { get; set; } = ""; // URL da imagem pequena
    public bool Owned { get; set; }
}
```

**Regras dos modelos:**
- `ExternalId` é único por tabela (índice único no `AppDbContext`).
- `PokemonCard.Number` é o número local no set (não o ID global).
- `ImageSmallUrl` pode ser vazio — imagens são carregadas separadamente via API.
- `Owned` indica se o usuário possui fisicamente a carta.

---

## 2. AppDbContext

```csharp
public class AppDbContext : DbContext
{
    public DbSet<PokemonSet> Sets => Set<PokemonSet>();
    public DbSet<PokemonCard> Cards => Set<PokemonCard>();
}
```

**Configurações no `OnModelCreating`:**
- `PokemonSet.ExternalId` — índice único.
- `PokemonCard.ExternalId` — índice único.
- `PokemonCard → PokemonSet` — FK `PokemonSetId`, navegação bidirecional.

**Registro em `Program.cs`:**
```csharp
var dbPath = Path.Combine(AppContext.BaseDirectory, "poke_collection.db");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));
```

O banco fica em `poke_collection.db` na pasta do executável.

---

## 3. Padrões de query com EF Core

### Leitura simples (sem rastreamento)

Use `AsNoTracking()` para queries de leitura que não serão atualizadas no mesmo contexto:

```csharp
var cards = await Db.Cards
    .AsNoTracking()
    .Where(x => x.PokemonSetId == setId)
    .OrderBy(x => x.Number)
    .ToListAsync();
```

### Leitura com modificação

Omita `AsNoTracking()` quando for atualizar a entidade logo após:

```csharp
var card = await Db.Cards.FirstOrDefaultAsync(x => x.Id == cardId);
if (card is null) return;
card.Owned = true;
await Db.SaveChangesAsync();
```

### Projeções (evitar carregar entidades completas)

Para dados de exibição que não precisam da entidade completa:

```csharp
var progress = await Db.Sets
    .OrderBy(s => s.Name)
    .Select(s => new SetProgress(
        s.Name,
        s.TotalCards,
        s.Cards.Count(c => c.Owned)
    ))
    .ToListAsync();
```

### Include (carga ansiosa de navegações)

Somente quando necessário. Evitar include em loops:

```csharp
var setComCartas = await Db.Sets
    .Include(s => s.Cards)
    .FirstOrDefaultAsync(x => x.Id == setId);
```

---

## 4. Camada de serviços

Serviços ficam em `Data/Services/`. São classes injetadas via DI nos componentes Blazor.

**Regras:**
- Recebem `AppDbContext` via construtor (DI).
- Toda operação de banco é assíncrona (`async Task`).
- Retornam o padrão `(bool ok, string message, T result)` quando podem falhar.
- Não lançam exceções para a UI — capturam e retornam no `message`.

**Template de serviço:**

```csharp
namespace PokeCollection.Data.Services;

public class ExemploService
{
    private readonly AppDbContext _db;

    public ExemploService(AppDbContext db) => _db = db;

    public async Task<(bool ok, string message, List<PokemonCard> cards)> GetOwnedCardsAsync()
    {
        try
        {
            var cards = await _db.Cards
                .AsNoTracking()
                .Where(x => x.Owned)
                .OrderBy(x => x.Name)
                .ToListAsync();

            return (true, $"OK. Cartas encontradas: {cards.Count}", cards);
        }
        catch (Exception ex)
        {
            return (false, $"Erro ao buscar cartas: {ex.Message}", new());
        }
    }
}
```

**Registro em `Program.cs`:**
```csharp
builder.Services.AddScoped<ExemploService>();
```

---

## 5. Migrations e schema

O projeto não usa `dotnet ef migrations` de forma automatizada. O schema é criado via:

```csharp
app.Services.CreateScope().ServiceProvider
    .GetRequiredService<AppDbContext>()
    .Database.EnsureCreated();
```

em `Program.cs`. Isso cria o banco na primeira execução com base nos modelos.

**Para adicionar um campo ao modelo:**
1. Adicionar a propriedade no modelo.
2. Deletar `poke_collection.db` para recriar do zero (em desenvolvimento), **ou** adicionar uma migration manualmente.
3. Nunca alterar o schema diretamente no SQLite sem refletir no modelo C#.

---

## 6. Boas práticas EF Core

- **Não reutilizar DbContext entre requisições.** O `AppDbContext` é `Scoped` — um por requisição Blazor. Não armazenar em campos estáticos.
- **Evitar N+1 queries.** Se precisar de dados relacionados, usar `Include` ou projeção `Select` com subconsulta.
- **SaveChangesAsync no final.** Agrupar múltiplas mutações antes de chamar `SaveChangesAsync` uma vez.
- **Não misturar tracked e untracked.** Se leu com `AsNoTracking`, não tente salvar a entidade sem reanexar ao contexto.
