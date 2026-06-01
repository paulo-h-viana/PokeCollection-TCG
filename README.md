# PokeCollection

App desktop para gerenciar sua coleção de cartas Pokémon TCG. Construído com .NET 8, Blazor Server e WebView2.

---

## Stack

| Camada | Tecnologia |
|---|---|
| Desktop | WinForms + WebView2 |
| UI | Blazor Server (.razor) |
| Banco de dados | SQLite via Entity Framework Core 8 |
| API externa | [TCGdex](https://api.tcgdex.net/v2/pt/) |
| Plataforma | .NET 8.0-windows |

---

## Estrutura de pastas

```
PokeCollection/
├── Pages/                      # Páginas Razor (UI)
│   ├── Dashboard.razor         # Stats gerais e progresso por coleção
│   ├── Cards.razor             # Grid de cartas de um set específico
│   └── Sets.razor              # Lista de coleções disponíveis
├── Shared/                     # Componentes compartilhados (layout, nav)
├── Data/
│   ├── Models/                 # Entidades do banco (PokemonSet, PokemonCard)
│   ├── Services/               # Lógica de negócio e acesso a dados
│   │   ├── PokemonApiService.cs    # Integração TCGdex
│   │   └── CardService.cs          # Operações de cartas no banco
│   └── AppDbContext.cs         # EF Core DbContext
├── wwwroot/
│   ├── css/                    # Estilos globais
│   ├── js/                     # darkmode.js e outros scripts
│   └── images/sets/            # Logos locais dos sets
├── MainForm.cs                 # Janela WinForms que hospeda o WebView2
├── Program.cs                  # DI, configuração, startup
├── AI.md                       # 🤖 Protocolo de operação da IA
└── ai-framework/               # 🤖 Base de conhecimento da IA
```

---

## Como rodar

**Pré-requisitos:** .NET 8 SDK, Windows

```powershell
# Restaurar dependências e rodar em modo debug
dotnet run

# Publicar executável standalone
dotnet publish -c Release -r win-x64 --self-contained false -o publish
```

O banco SQLite (`poke_collection.db`) é criado automaticamente na pasta do executável na primeira execução.

---

## Funcionalidades

- **Dashboard** — total de coleções, cartas e adquiridas; barra de progresso por set; modo escuro.
- **Sets** — lista todas as coleções; sincroniza sets novos da API TCGdex.
- **Cards** — grid de cartas de um set; marcar carta como adquirida/não adquirida; sincronizar cartas do TCGdex; carregar imagens faltantes.

---

## 🤖 Trabalhando com IA (Claude Code)

Este projeto tem um framework de governança de IA baseado no mesmo modelo usado no `cit-coding`. O objetivo é garantir que toda sessão de desenvolvimento com IA seja consistente, segura e bem documentada.

### Visão geral do fluxo

```
Você tem uma demanda
       │
       ▼
Preencher o template → ai-framework/prompts/prompt.md
       │
       ▼
Usar o slash command adequado
  /bugfix  →  para corrigir bugs
  /feature →  para features novas
  /review  →  para revisar o que foi feito
       │
       ▼
IA aplica o Gatekeeper (valida contexto)
       │
  ┌────┴────┐
  │         │
Falta    Contexto
contexto  completo
  │         │
  ▼         ▼
IA pede  IA investiga/planeja
mais       │
dados      ▼
        Você aprova o plano
           │
           ▼
        IA implementa
           │
           ▼
        /review → validar antes de commitar
```

---

### Passo 1 — Preencher o template de prompt

Antes de pedir qualquer coisa à IA, abra e preencha:

```
ai-framework/prompts/prompt.md
```

O template pede:
- **Tipo de demanda** — Bug, Feature ou Refactor
- **Arquivo afetado** — qual `.razor`, serviço ou modelo está envolvido
- **Comportamento atual vs. esperado**
- **Logs / erros** (se houver)
- **Critério de aceite** — como você vai validar que está pronto

> **Por que?** A IA tem um Gatekeeper que recusa prompts vagos. Preencher o template antecipadamente evita a rodada de perguntas e vai direto ao ponto.

---

### Passo 2 — Usar o slash command certo

Com o template preenchido, use o comando adequado na conversa com a IA:

#### `/bugfix` — Corrigir um bug

Inicia o **Fluxo A**:
1. IA lê os RAGs relevantes.
2. IA investiga a causa raiz e reporta o diagnóstico (`arquivo:linha`).
3. IA propõe a correção com justificativa.
4. **Você aprova** ("de acordo", "pode fazer", etc.).
5. IA aplica a correção.
6. IA executa `/poke-architect` + `/poke-security` e sugere `dotnet build`.

#### `/feature` — Implementar uma feature nova

Inicia o **Fluxo B**:
1. IA lê os RAGs relevantes.
2. IA reformula o pedido e explicita suposições.
3. IA apresenta o **plano de implementação** (arquivos, abordagem, decisões).
4. **Você aprova** o plano.
5. IA implementa.
6. IA executa `/poke-architect` + `/poke-security` e sugere `dotnet build`.

#### `/review` — Revisar o que foi implementado

Operação **read-only**. Roda os checklists de arquitetura e segurança no diff atual e reporta:
- Violações de convenção (`❌`)
- Vulnerabilidades (`🔴 🟡 🟢`)
- Sinal de ok para commitar (`✅`)

---

### Passo 3 — Subagentes disponíveis

Os subagentes podem ser invocados diretamente ou são chamados automaticamente pelos comandos acima:

| Agente | Quando usar |
|---|---|
| `/poke-gatekeeper` | Validar se seu prompt tem contexto suficiente antes de abrir uma demanda |
| `/poke-architect` | Checar se o código implementado segue as convenções do projeto |
| `/poke-security` | Checar vulnerabilidades antes de commitar |

---

### Passo 4 — Base de conhecimento RAG

A IA consulta os seguintes documentos automaticamente ao processar `/bugfix` e `/feature`. Você também pode lê-los para entender as convenções:

| Documento | Conteúdo |
|---|---|
| [ai-framework/rag/conventions.md](ai-framework/rag/conventions.md) | Naming, nullable, async/await, CSS, sem comentários |
| [ai-framework/rag/blazor.md](ai-framework/rag/blazor.md) | Ciclo de vida Razor, @inject, JS interop, binding |
| [ai-framework/rag/data.md](ai-framework/rag/data.md) | EF Core, AppDbContext, padrão de serviços, migrations |
| [ai-framework/rag/api.md](ai-framework/rag/api.md) | TCGdex, DTOs, HttpClient, normalização de URLs |

---

### Regra de ouro

> A IA nunca edita arquivos sem sua aprovação explícita.
> Ela investiga, diagnostica, propõe — e **espera seu "de acordo"** antes de qualquer mudança.

---

### Exemplo de sessão completa

```
1. Abre ai-framework/prompts/prompt.md e preenche:
   - Tipo: Bug
   - Arquivo: Pages/Cards.razor
   - Atual: ao clicar em "Sincronizar", a página trava sem mensagem de erro
   - Esperado: exibir mensagem de progresso e desabilitar o botão durante sincronização
   - Log: System.NullReferenceException em Cards.razor linha 174
   - Aceite: clicar em Sincronizar mostra "Sincronizando..." e não trava

2. Na conversa com a IA, digita: /bugfix

3. IA valida o contexto (Gatekeeper passa ✅)
4. IA lê blazor.md e data.md
5. IA investiga Cards.razor:174 e reporta a causa raiz
6. IA propõe a correção

7. Você digita: "de acordo, pode fazer"

8. IA aplica a correção
9. IA roda /poke-architect → sem violações ✅
10. IA roda /poke-security → sem vulnerabilidades ✅
11. IA sugere: dotnet build && git commit

12. Você testa no app e commita
```

---

## Contribuindo / Mantendo os RAGs

Se uma convenção mudar ou um padrão novo for estabelecido, atualize o RAG correspondente em `ai-framework/rag/` e incremente o campo `version:` no frontmatter do arquivo. Isso garante que sessões futuras com a IA usem o conhecimento atualizado.
