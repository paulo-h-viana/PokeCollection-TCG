# ai-framework — Base de Conhecimento da IA

Este diretório contém os documentos RAG (Retrieval-Augmented Generation) que a IA deve consultar antes de implementar qualquer coisa neste projeto.

## Estrutura

```
ai-framework/
├── rag/
│   ├── conventions.md      # Convenções de código C# e regras gerais do projeto
│   ├── blazor.md           # Padrões de componentes Blazor (ciclo de vida, injeção, CSS)
│   ├── data.md             # Camada de dados: EF Core, AppDbContext, serviços
│   └── api.md              # Integração TCGdex: HttpClient, DTOs, tratamento de erros
└── prompts/
    └── prompt.md           # Template para descrever demandas à IA
```

## Como usar

Antes de implementar qualquer demanda, a IA deve:

1. Ler `rag/conventions.md` — sempre.
2. Ler os RAGs específicos do domínio da tarefa (ver mapa em [AI.md](../AI.md)).
3. Usar `prompts/prompt.md` como template para organizar o pedido.

## Quando atualizar

Atualize os RAGs sempre que:
- Uma convenção do projeto mudar (nova decisão arquitetural).
- Um padrão novo for estabelecido (novo jeito de usar EF Core, novo serviço, etc.).
- Um bug recorrente for identificado como causado por mal-entendido de um padrão.

Incremente o `version:` no frontmatter do RAG após cada atualização.
