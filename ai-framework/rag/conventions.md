---
title: Convenções de Código — PokeCollection
version: 1.0.0
domain: geral
---

# Convenções de Código

Leia este documento **sempre**, independente do domínio da tarefa.

---

## 1. Naming

| Elemento | Convenção | Exemplo |
|---|---|---|
| Classes, Records, Enums | PascalCase | `PokemonCard`, `SetProgress` |
| Interfaces | IPascalCase | `ICardService` |
| Métodos | PascalCase | `GetCardsBySetAsync` |
| Propriedades públicas | PascalCase | `ExternalId`, `TotalCards` |
| Campos privados | _camelCase | `_http`, `_db` |
| Variáveis locais | camelCase | `totalCards`, `existingIds` |
| Parâmetros | camelCase | `setId`, `cardId` |
| Namespaces | PascalCase, hierárquico | `PokeCollection.Data.Services` |

---

## 2. Sem comentários explicativos

O código deve ser auto-explicativo. **NUNCA** adicione comentários `//` ou `/* */` para explicar o que o código faz.

❌ Proibido:
```csharp
// Busca as cartas do banco de dados
var cards = await Db.Cards.ToListAsync();
```

✅ Correto:
```csharp
var cards = await Db.Cards.ToListAsync();
```

Comentários XML (`///`) são aceitos apenas em métodos públicos de serviços quando a assinatura não é autoexplicativa.

---

## 3. Nullable

O projeto usa `<Nullable>enable</Nullable>`. Regras:

- **Nunca** usar `!` (null-forgiving operator) sem garantia lógica de que o valor não é null.
- **Sempre** retornar `T?` quando um método pode não encontrar resultado.
- Usar `?.` (null-conditional) para acessar membros de objetos que podem ser null.
- Usar `??` (null-coalescing) para fornecer valores padrão.
- Não usar `#nullable disable`.

```csharp
// Correto
var set = await Db.Sets.FirstOrDefaultAsync(x => x.Id == id);
if (set is null) return;

// Errado
var set = await Db.Sets.FirstOrDefaultAsync(x => x.Id == id)!;
```

---

## 4. Async/Await

- **Toda** operação de I/O (banco de dados, HTTP, arquivo) deve ser assíncrona.
- Nomear métodos assíncronos com sufixo `Async`.
- **Nunca** usar `.Result`, `.Wait()` ou `GetAwaiter().GetResult()` — causa deadlock em Blazor.
- Usar `ConfigureAwait(false)` apenas em bibliotecas, não em código de aplicação Blazor.

```csharp
// Correto
var cards = await Db.Cards.ToListAsync();

// Errado — causa deadlock
var cards = Db.Cards.ToListAsync().Result;
```

---

## 5. Padrão de retorno de serviços

Serviços que podem falhar (chamadas HTTP, operações de banco com validação) usem o padrão de tupla:

```csharp
public async Task<(bool ok, string message, T result)> MetodoAsync(...)
```

Exemplo real:
```csharp
public async Task<(bool ok, string message, List<SetDto> sets)> GetSetsAsync()
```

Isso evita exceções não tratadas chegando na UI e permite mensagens de erro descritivas.

---

## 6. Records para projeções de dados

Use `record` para dados imutáveis usados como projeção de query ou DTO de tela:

```csharp
private record SetProgress(string Name, int Total, int Owned);
```

Não criar classes completas com getters/setters para objetos que são apenas dados de leitura.

---

## 7. CSS — apenas CSS puro

- **NUNCA** gerar `.scss`, `.sass`, `.less` ou qualquer preprocessador.
- **NUNCA** usar sintaxe de preprocessador dentro de `.css` (`$var`, `@mixin`, `@include`, `&` aninhado).
- Variáveis de tema usam CSS custom properties: `--cor-primaria: #...`.
- Estilos globais ficam em `wwwroot/css/site.css`.
- Estilos de componente ficam em `Pages/<Componente>.razor.css` ou `Shared/<Componente>.razor.css`.

---

## 8. Organização de arquivos

- Modelos de domínio: `Data/Models/`
- Serviços: `Data/Services/`
- Páginas Razor: `Pages/`
- Componentes compartilhados: `Shared/`
- Estilos globais: `wwwroot/css/`
- Scripts JS: `wwwroot/js/`
- Imagens de sets: `wwwroot/images/sets/`

Não criar pastas fora dessa estrutura sem justificativa e aprovação prévia.

---

## 9. Injeção de dependência

Serviços são registrados em `Program.cs`. Para adicionar um serviço novo:

```csharp
builder.Services.AddScoped<MeuServicoNovo>();
```

Usar `AddScoped` para serviços que acessam `AppDbContext` (que também é Scoped em Blazor Server). Usar `AddSingleton` apenas para serviços stateless sem dependência de DbContext.

---

## 10. Build e validação

Após qualquer mudança estrutural (novo arquivo, novo serviço, nova dependência), rodar:

```powershell
dotnet build
```

Para publicar:

```powershell
dotnet publish -c Release -r win-x64 --self-contained false -o publish
```
