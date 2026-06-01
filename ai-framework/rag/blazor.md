---
title: Padrões Blazor — PokeCollection
version: 1.0.0
domain: blazor
---

# Padrões de Componentes Blazor

Leia este documento ao trabalhar em qualquer arquivo `.razor` (páginas ou componentes).

---

## 1. Estrutura padrão de um componente Razor

```razor
@page "/rota"
@using Namespace.Necessario
@inject ServicoInjetado NomeLocal

<!-- markup HTML / Razor -->
<div>@variavel</div>

@code {
    // campos privados
    private Tipo campo;

    // parâmetros (se for componente, não página)
    [Parameter] public Tipo Parametro { get; set; }

    // ciclo de vida
    protected override async Task OnInitializedAsync()
    {
        // inicialização assíncrona
    }

    // handlers de eventos
    private async Task OnBotaoClicado()
    {
        // lógica
    }
}
```

Ordem dentro do bloco `@code`:
1. Campos privados de estado
2. Parâmetros `[Parameter]`
3. Métodos de ciclo de vida (`OnInitializedAsync`, `OnParametersSetAsync`, `OnAfterRenderAsync`)
4. Handlers de eventos e métodos privados

---

## 2. Ciclo de vida — quando usar cada método

| Método | Quando usar |
|---|---|
| `OnInitializedAsync` | Carregamento inicial de dados (queries no banco, chamadas HTTP) |
| `OnParametersSetAsync` | Quando um `[Parameter]` muda e precisa recarregar dados |
| `OnAfterRenderAsync(bool firstRender)` | JS Interop (precisa do DOM pronto), ler estado do localStorage |
| `StateHasChanged()` | Forçar re-render após mutação fora do ciclo normal de eventos |

Exemplo de `OnAfterRenderAsync` com `firstRender`:
```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        isDark = await JS.InvokeAsync<bool>("darkMode.get");
        StateHasChanged();
    }
}
```

---

## 3. Injeção de dependências

Use `@inject` para injetar serviços nas páginas:

```razor
@inject AppDbContext Db
@inject PokemonApiService Api
@inject IJSRuntime JS
@inject NavigationManager Nav
```

A injeção via `@inject` é equivalente a `[Inject]` em propriedades do `@code`. Preferir `@inject` no topo do arquivo para visibilidade.

---

## 4. Parâmetros de rota

Para páginas com parâmetro de rota:

```razor
@page "/cards/{SetId:int}"

@code {
    [Parameter] public int SetId { get; set; }

    protected override async Task OnInitializedAsync()
    {
        // SetId já está disponível aqui
    }
}
```

Tipos suportados em constraints de rota: `int`, `long`, `float`, `double`, `decimal`, `bool`, `datetime`, `guid`.

---

## 5. Eventos e binding

**Binding unidirecional (leitura):**
```razor
<span>@variavel</span>
```

**Binding bidirecional (inputs):**
```razor
<input @bind="campoBuscado" />
```

**Eventos:**
```razor
<button @onclick="MetodoHandler">Clique</button>
<button @onclick="() => MetodoComArg(id)">Com argumento</button>
```

Para eventos com `EventArgs`:
```razor
<input @oninput="OnInput" />

@code {
    private void OnInput(ChangeEventArgs e)
    {
        var valor = e.Value?.ToString();
    }
}
```

---

## 6. Renderização condicional

```razor
@if (set is null)
{
    <p>Carregando...</p>
}
else
{
    <div>@set.Name</div>
}
```

Para listas:
```razor
@foreach (var card in cards)
{
    <div class="card-item">@card.Name</div>
}
```

---

## 7. JavaScript Interop

Chamar JS a partir de C#:
```csharp
// Sem retorno
await JS.InvokeVoidAsync("nomeDaFuncao", arg1, arg2);

// Com retorno
var resultado = await JS.InvokeAsync<bool>("darkMode.toggle");
```

Funções JS ficam em `wwwroot/js/`. O padrão atual é o objeto `darkMode` em `wwwroot/js/darkmode.js`.

---

## 8. CSS de componente (isolamento)

Cada componente pode ter um arquivo `.razor.css` com o mesmo nome. O CSS nesse arquivo é scoped automaticamente para o componente:

- `Pages/Cards.razor` → `Pages/Cards.razor.css`
- `Shared/NavMenu.razor` → `Shared/NavMenu.razor.css`

Não usar `!important` para sobrescrever estilos de escopo — usar especificidade correta.

---

## 9. Estado de loading e feedback

Padrão estabelecido no projeto para operações assíncronas (sincronização, etc.):

```csharp
private bool isSyncing = false;
private string? syncMessage;

private async Task SyncCards()
{
    isSyncing = true;
    syncMessage = null;

    try
    {
        // operação
        syncMessage = "Concluído: X itens processados";
    }
    catch (Exception ex)
    {
        syncMessage = "Erro: " + ex.Message;
    }
    finally
    {
        isSyncing = false;
    }
}
```

Na UI:
```razor
<button @onclick="SyncCards" disabled="@isSyncing">
    @(isSyncing ? "Processando..." : "Sincronizar")
</button>

@if (!string.IsNullOrWhiteSpace(syncMessage))
{
    <pre>@syncMessage</pre>
}
```

---

## 10. Navegação

```csharp
@inject NavigationManager Nav

private void IrParaCards(int setId)
{
    Nav.NavigateTo($"/cards/{setId}");
}
```

Links no markup:
```razor
<a href="/cards/@set.Id">@set.Name</a>
```

Ou com `NavLink` (adiciona classe `active` automaticamente):
```razor
<NavLink href="/" Match="NavLinkMatch.All">Dashboard</NavLink>
```
