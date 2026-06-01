---
title: Integração API TCGdex — PokeCollection
version: 1.0.0
domain: api
---

# Integração com a API TCGdex

Leia este documento ao trabalhar em `Data/Services/PokemonApiService.cs` ou ao adicionar novas chamadas à API externa.

---

## 1. Serviço responsável

`PokemonApiService` em `Data/Services/PokemonApiService.cs` é a **única** classe que deve fazer chamadas HTTP ao TCGdex. Nenhuma página Razor ou outro serviço deve instanciar `HttpClient` diretamente.

Registro em `Program.cs`:
```csharp
builder.Services.AddHttpClient<PokemonApiService>();
```

O `HttpClient` é injetado via construtor pelo `AddHttpClient`.

---

## 2. Base URL da API

```
https://api.tcgdex.net/v2/pt/
```

Idioma `pt` (português) está fixo na URL. Todos os endpoints usam essa base.

### Endpoints conhecidos

| Endpoint | Descrição |
|---|---|
| `GET /v2/pt/sets` | Lista todos os sets |
| `GET /v2/pt/sets/{setId}` | Detalhes do set + lista de cartas |
| `GET /v2/pt/cards/{cardId}` | Detalhes de uma carta (inclui imagem) |

---

## 3. Padrão de método da API

Todos os métodos públicos do `PokemonApiService` seguem o padrão de retorno:

```csharp
public async Task<(bool ok, string message, T result)> MetodoAsync(...)
```

- `ok = true` → chamada bem-sucedida, `result` contém os dados.
- `ok = false` → falha (HTTP error ou exception), `message` contém o detalhe do erro, `result` é valor padrão.

Nunca lançar exceções para fora do serviço — capturar no `catch` e retornar `(false, mensagem, default)`.

---

## 4. DTOs da API

Os DTOs ficam como classes **internas** ao `PokemonApiService`. Não expor DTOs de API fora da classe.

### SetDto (resposta de `/sets`)

```csharp
public class SetDto
{
    public string id { get; set; } = "";
    public string name { get; set; } = "";
    public string serie { get; set; } = "";
    public string symbol { get; set; } = "";
    public string logo { get; set; } = "";
    public CardCountDto cardCount { get; set; } = new();

    public class CardCountDto
    {
        public int total { get; set; }
    }
}
```

### SetDetailsDto (resposta de `/sets/{id}`)

```csharp
public class SetDetailsDto
{
    public string id { get; set; } = "";
    public string name { get; set; } = "";
    public JsonElement serie { get; set; }
    public List<CardListItemDto> cards { get; set; } = new();
}
```

### CardListItemDto (item dentro de `SetDetailsDto.cards`)

```csharp
public class CardListItemDto
{
    public string id { get; set; } = "";
    public string name { get; set; } = "";
    public string localId { get; set; } = "";
    public string? rarity { get; set; }
    public JsonElement image { get; set; }
    public JsonElement images { get; set; }
}
```

### CardDetailsDto (resposta de `/cards/{id}`)

```csharp
public class CardDetailsDto
{
    public string id { get; set; } = "";
    public string name { get; set; } = "";
    public JsonElement image { get; set; }
    public JsonElement images { get; set; }
}
```

**Por que `JsonElement` para `image`/`images`?**
A API TCGdex é inconsistente — o campo `image` pode ser uma `string` (URL direta), um objeto `{ "url": "..." }` ou um objeto `{ "path": "..." }`. O campo `images` pode ser um objeto `{ "small": "...", "large": "..." }`. Usar `JsonElement` permite lidar com todos os formatos.

---

## 5. Extração de URL de imagem

O método estático `ExtractFinalImageUrlFromDetails` centraliza a lógica de extrair a URL de imagem de um `CardDetailsDto`:

```csharp
public static string ExtractFinalImageUrlFromDetails(CardDetailsDto? d)
```

**Prioridade de extração:**
1. `images.small` (string)
2. `images.large` (string)
3. `image` (string direta)
4. `image.url` (objeto com campo `url`)
5. `image.path` (objeto com campo `path`, adiciona `.png` se sem extensão)

Sempre normaliza a URL resultante com `NormalizeUrl`.

---

## 6. Normalização de URLs

O método privado `NormalizeUrl` garante que todas as URLs sejam HTTPS:

```csharp
private static string NormalizeUrl(string? url)
{
    if (string.IsNullOrWhiteSpace(url)) return "";
    url = url.Trim();
    if (url.StartsWith("//")) return "https:" + url;
    if (url.StartsWith("http://")) return "https://" + url.Substring("http://".Length);
    return url;
}
```

Usar **sempre** ao retornar URLs para o banco de dados.

---

## 7. Construção de URL de imagem de carta (UI)

Em `Cards.razor`, as URLs das imagens podem ser parciais (sem extensão). O método estático `BuildCardImageUrl` resolve isso:

```csharp
private static string BuildCardImageUrl(string? baseUrl, string quality = "low", string ext = "webp")
```

- Se a URL já termina com uma extensão de imagem (`.png`, `.jpg`, `.webp`), retorna como está.
- Caso contrário, appenda `/{quality}.{ext}` (ex: `/low.webp`).

Usar sempre ao exibir imagens de cartas na UI.

---

## 8. Desserialização JSON

Usar `JsonSerializerOptions` com `PropertyNameCaseInsensitive = true` para tolerar variações de casing da API:

```csharp
var resultado = JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
});
```

---

## 9. Imagens locais dos sets

Os logos dos sets podem ser servidos localmente em `wwwroot/images/sets/`. O padrão de nome é `{externalId}.png` (ou outra extensão). A UI tenta carregar localmente antes de usar a URL da API como fallback.

---

## 10. Adicionando um novo endpoint

Para adicionar um endpoint novo ao `PokemonApiService`:

1. Criar o(s) DTO(s) correspondente(s) como classe interna.
2. Implementar o método seguindo o padrão `(bool ok, string message, T result)`.
3. Usar `JsonSerializerOptions { PropertyNameCaseInsensitive = true }`.
4. Usar `NormalizeUrl` em qualquer URL retornada.
5. Capturar exceções com `catch (Exception ex)` e retornar `(false, msg, default)`.
6. Nunca lançar exceções para fora do serviço.
