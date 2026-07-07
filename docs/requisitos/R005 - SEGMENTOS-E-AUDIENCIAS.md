# Especificação Funcional e Técnica — Segmentos e Audiências

> **Produto:** Voucher System
> **Macro-requisito:** R005
> **Dependências:** R004 — Clientes, Perfis, Consentimento e Privacidade; R011 — Motor de Regras e Elegibilidade
> **Stack alvo:** .NET 10 + PostgreSQL via EF Core 10 + Redis + React + TypeScript + Vite
> **Status:** especificação para implementação incremental
> **Última revisão:** 2026-07-07

---

# 1. Visão geral

R005 define o subsistema de segmentos e audiências que transforma dados de cliente
e comportamento em grupos reutilizáveis. Segmentos são a base para regras de
elegibilidade (R011), distribuições (R020), loyalty (R016), referrals (R018) e
analytics (R023).

O segmento pertence a um projeto. Nenhum dado de segmento ou associação pode
atravessar projetos implicitamente.

```text
Organization
  └── Project
      └── Segment
          ├── Static — lista explícita de Customer IDs
          ├── Dynamic (passive) — critérios via RuleSet, sem membership tracking
          └── Dynamic (active) — critérios via RuleSet, com eventos de entrada/saída
```

Segmentos estáticos mantêm a associação N:N em tabela própria. Segmentos dinâmicos
delegam a avaliação ao motor de regras (R011) e podem ou não armazenar membership
em cache materializado.

---

# 2. Objetivos de negócio

## 2.1 Objetivo principal

Permitir que equipes de marketing, CRM e produto criem audiências a partir de
atributos de cliente, comportamento, pedidos, eventos e metadata — e as reutilizem
em regras, campanhas, distribuições e análises sem depender de SQL ou engenharia.

## 2.2 Resultados esperados

- criação e manutenção self-service de segmentos no portal;
- avaliação de pertencimento determinística e explicável em < 100 ms;
- segmentos dinâmicos avaliados sem I/O durante a chamada transacional;
- reação a eventos de entrada e saída em segmentos ativos;
- exportação de membros para integração com sistemas externos;
- versionamento ou imutabilidade quando o segmento estiver em uso crítico;
- cache com invalidação controlada após mudanças relevantes;
- isolamento integral por `account_id` e `project_id`.

---

# 3. Escopo

## 3.1 Incluído

- CRUD de segmentos com nome, descrição, tipo e status;
- segmentos estáticos com adição/remoção manual de clientes;
- segmentos dinâmicos passivos (avaliação sob demanda via RuleSet);
- segmentos dinâmicos ativos (avaliação com membership materializada e eventos);
- suporte a RuleSet com operadores booleanos, numéricos, textuais, temporais e de
  conjunto, delegado a R011;
- composição de segmentos (AND, OR, NOT) para audiências compostas;
- preview de audiência com contagem e amostra;
- avaliação de pertencimento em tempo real (member check);
- exportação assíncrona de membros;
- importação de membros para segmentos estáticos;
- transições de estado (Draft → Active → Archived);
- versionamento do segmento no momento da ativação;
- auditoria e outbox para operações críticas;
- cache Redis de membership com invalidação;
- portal administrativo com lista, detalhe, preview e gerenciamento.

## 3.2 Fora do escopo

- motor de regras e elegibilidade em si: R011;
- clientes, perfis e consentimento: R004;
- distribuição de mensagens para segmentos: R020;
- analytics globais sobre segmentos: R023;
- metadata schemas avançados: R022;
- imports e bulk operations genéricos: R025.

R005 mantém referências e interfaces com esses domínios sem duplicá-los.

---

# 4. Conceitos de domínio

## 4.1 Segment

Audiência nomeada com tipo, critérios (RuleSet) e status. Pertencente a um projeto.

## 4.2 Segment type

| Tipo | Descrição | Membership |
|---|---|---|
| `Static` | Lista explícita de Customer IDs adicionados manualmente ou por import | Tabela `SegmentCustomer` |
| `DynamicPassive` | Critérios via RuleSet avaliados sob demanda. Sem membership armazenada. Nenhuma notificação de entrada/saída | Avaliação no momento da chamada |
| `DynamicActive` | Critérios via RuleSet com membership materializada em cache, atualizada por jobs e eventos. Gera eventos de entrada e saída | Cache + job de sincronização periódica |

## 4.3 RuleSet (R011)

Conjunto versionado de grupos e condições usado como critério de segmentação
dinâmica. A engine de avaliação é de responsabilidade de R011.

## 4.4 Segment membership

Associação entre um segmento e um cliente. Para segmentos estáticos, é a
entrada `SegmentCustomer`. Para dinâmicos ativos, é a projeção em cache.

## 4.5 Audience composite

Segmento lógico composto por operações AND, OR e NOT sobre outros segmentos,
avaliado sem materialização própria.

```text
Exemplo: (SegmentA AND SegmentB) NOT SegmentC
```

## 4.6 Segment status

| Estado | Descrição |
|---|---|
| `Draft` | Em edição, não utilizado por regras ou campanhas |
| `Active` | Publicado, utilizado por campanhas, regras e distribuições |
| `Archived` | Arquivado, não pode ser usado em novas regras. Preservado para referência |

## 4.7 Member check

Operação de tempo real que avalia se um Customer pertence a um segmento,
respeitando seu tipo (estático → lookup, dinâmico passivo → engine call,
dinâmico ativo → cache).

---

# 5. Decisões de modelagem

## 5.1 Segmento como entidade raiz

`Segment` é entidade agregada independente. Segmentos estáticos possuem uma
lista de `SegmentCustomer`. Segmentos dinâmicos referenciam `RuleSetId`.

## 5.2 PostgreSQL como fonte de verdade

Membership de segmento estático é transacional no PostgreSQL. Para segmentos
dinâmicos ativos, o cache Redis serve a avaliação em tempo real e o job de
sincronização reconcilia com o banco. O banco sempre pode reconstruir o cache.

## 5.3 Versionamento na ativação

Ao ativar um segmento dinâmico, uma cópia versionada do RuleSet referenciado
é associada (`SnapshotRuleSet` ou version lock). Isso garante que a avaliação
seja determinística mesmo se o RuleSet for alterado posteriormente.

R011 já versiona RuleSets; o segmento registra `RuleSetVersion` no momento da
ativação.

## 5.4 Membership materializada opcional

Segmentos dinâmicos ativos mantêm membership em cache Redis (Sorted Set com
Customer ID e timestamp de entrada). Um job periódico (cron) varre a base de
clientes e reconcilia o cache. Eventos de cliente (R007) disparam reavaliação
individual.

## 5.5 Segmentos compostos são virtuais

Audiências compostas não são materializadas. A avaliação resolve a árvore de
referências no momento da chamada, com cache curto para evitar degradação.

## 5.6 Deleção segura (soft delete)

Segmento utiliza `IsDeleted`. Quando `Archived`, não aceita novas associações
mas preserva o histórico. Remoção física só ocorre se não houver referências
de campanha, regra, distribuição ou auditoria.

---

# 6. Regras de negócio

## RN-001 — Tenant obrigatório

Toda leitura e escrita exige `account_id` e `project_id`. ID conhecido de outro
tenant retorna `404`.

## RN-002 — Nome único por projeto

`Name` normalizado (trim, lowercase) é único dentro do mesmo projeto para
segmentos não deletados.

## RN-003 — Segmento estático não aceita RuleSet

Criar um segmento `Static` com `RuleSetId` populado retorna erro de validação.

## RN-004 — Segmento dinâmico exige RuleSet

Criar um segmento `DynamicPassive` ou `DynamicActive` sem `RuleSetId` retorna
erro de validação.

## RN-005 — Apenas estático aceita adição manual de clientes

`POST /segments/{id}/customers` funciona somente para `Type = Static`. Demais
tipos retornam `400`.

## RN-006 — Cliente duplicado é idempotente

Adicionar o mesmo Customer ID duas vezes a um segmento estático não gera erro
ou duplicata (unique constraint + skip existing).

## RN-007 — Ativação congela critérios

Ativar um segmento dinâmico registra a versão atual do RuleSet. Alterações
posteriores no RuleSet não afetam o segmento ativo sem uma reativação explícita.

## RN-008 — Alteração de segmento ativo exige nova ativação

Modificar nome, descrição, tipo ou RuleSetId de um segmento `Active` transita
para `Draft`. A mudança só vale após nova ativação.

## RN-009 — Segmento em uso não pode ser excluído fisicamente

Campanhas, regras, distribuições ou exportações que referenciam o segmento
impedem deleção física. O segmento pode ser arquivado.

## RN-010 — Arquivo preserva associações

Archivar um segmento mantém `SegmentCustomer` e cache. Nenhuma nova adição é
permitida. Avaliação de membership retorna resultado até expiração do cache.

## RN-011 — Preview tem limite de amostra

Preview de audiência retorna contagem total + até 10 Customer IDs de amostra,
sem PII completa.

## RN-012 — Member check é tenant-aware e determinístico

Duas chamadas com o mesmo contexto de avaliação produzem o mesmo resultado
para o mesmo segmento.

## RN-013 — Segmento composto não pode criar ciclo

Avaliação de audiência composta detecta e rejeita referências circulares (A
contém B que contém A).

## RN-014 — Profundidade máxima de composição

Audiência composta permite no máximo 5 níveis de aninhamento para evitar
degradação.

## RN-015 — Exportação é assíncrona e segura

Export de membros gera arquivo CSV/JSON com URL temporária criptografada.
Inclui somente Customer IDs, sem PII completa, a menos que permissão explícita.

## RN-016 — Cache de membership tem TTL

Segmentos dinâmicos ativos possuem cache Redis com TTL configurável (default
5 minutos). O cache é invalidado por evento de cliente, alteração de RuleSet
ou job de reconciliação.

## RN-017 — Job de reconciliação é idempotente

O worker de sincronização de segmentos ativos pode ser executado múltiplas
vezes sem duplicar membership ou eventos.

## RN-018 — Eventos de entrada/saída são gerados apenas para ativos

Segmentos `DynamicActive` disparam eventos `segment.member.entered` e
`segment.member.left` via outbox. Segmentos estáticos e passivos não geram.

## RN-019 — Remoção de cliente do sistema reflete nos segmentos

Anonimizar ou excluir um Customer remove automaticamente suas associações de
segmento (estático e cache dinâmico).

## RN-020 — Segmento sem nome válido é rejeitado

Nome deve ter entre 3 e 200 caracteres, sem caracteres de controle ou leading/
trailing whitespace.

---

# 7. Permissões e scopes

Permissões humanas:

```text
segments.read
segments.create
segments.update
segments.activate
segments.archive
segments.delete
segments.members.manage
segments.export
```

Scopes técnicos (API key):

```text
segments.read
segments.write
segments.members.read
segments.members.write
```

Project Admin pode gerenciar segmentos no projeto. `segments.activate` e
`segments.archive` são operações administrativas que exigem permissão
explícita. `segments.export` é independente da permissão de leitura de PII.

---

# 8. Modelo de dados proposto

## 8.1 Segment

```text
Id, AccountId, ProjectId
Name, Description
Type (Static | DynamicPassive | DynamicActive)
Status (Draft | Active | Archived)
RuleSetId (nullable)
RuleSetVersion (nullable — versão congelada na ativação)
CompositionJson (nullable — árvore de referências para audiência composta)
CustomerCount (cache snapshot)
RowVersion
CreatedAt, UpdatedAt, ArchivedAt
```

## 8.2 SegmentCustomer

```text
Id, AccountId, ProjectId, SegmentId
CustomerId
CreatedAt
```

Unique `(SegmentId, CustomerId)`.

## 8.3 SegmentAudit (append-only)

```text
Id, AccountId, ProjectId, SegmentId
Action, ActorId, ActorType
OldValueJson, NewValueJson
RequestId, Timestamp
```

## 8.4 Índices e constraints

- unique parcial `(AccountId, ProjectId, lower(Name), IsDeleted = false)`;
- índice `(AccountId, ProjectId, Status, CreatedAt, Id)`;
- índice `(SegmentId, CustomerId)` unique para `SegmentCustomer`;
- índice `(CustomerId)` em `SegmentCustomer` para lookup reverso;
- FKs sempre compatíveis com tenant;
- concorrência otimista em `Segment` via `RowVersion`.

---

# 9. Estados

Segment:

```text
Draft ──→ Active ──→ Archived
  ↑          │
  └──────────┘ (deactivate → Draft)
```

- `Draft` → `Active`: ativa o segmento, congela versão do RuleSet,
  popula cache inicial (se ativo).
- `Active` → `Draft`: desativa, mantém membership materializada
  (se houver) mas não aceita avaliação de regras ativas.
- `Active` → `Archived`: arquivamento definitivo, bloqueia novas associações.
- `Draft` → `Archived`: arquivamento de rascunho abandonado.

---

# 10. APIs

## 10.1 Segment CRUD

```text
POST   /api/segments
GET    /api/segments
GET    /api/segments/{segmentId}
PATCH  /api/segments/{segmentId}
DELETE /api/segments/{segmentId}
```

## 10.2 Ciclo de vida

```text
POST   /api/segments/{segmentId}/activate
POST   /api/segments/{segmentId}/deactivate
POST   /api/segments/{segmentId}/archive
```

## 10.3 Membership — estático

```text
POST   /api/segments/{segmentId}/customers          (add lista de IDs)
DELETE /api/segments/{segmentId}/customers/{customerId}
GET    /api/segments/{segmentId}/customers           (lista paginada)
POST   /api/segments/{segmentId}/customers/import    (import assíncrono via R025)
GET    /api/segments/{segmentId}/customers/export    (export assíncrono)
```

## 10.4 Avaliação

```text
POST   /api/segments/{segmentId}/check               (member check para Customer)
POST   /api/segments/check-many                      (batch member check)
POST   /api/segments/preview                         (preview com contexto simulado)
```

## 10.5 Customer lookup reverso

```text
GET    /api/customers/{customerId}/segments          (segmentos do cliente)
```

Listas usam `limit`, `after`, filtros allowlisted e ordenação determinística.
Mutações críticas usam `Idempotency-Key`.

---

# 11. Contratos e validação

Create/update aceita nome, descrição, tipo, RuleSetId e composição.

Erros estáveis:

```text
SEGMENT_NOT_FOUND
SEGMENT_NAME_CONFLICT
SEGMENT_INVALID_TYPE
SEGMENT_RULESET_REQUIRED
SEGMENT_STATIC_NO_RULESET
SEGMENT_DYNAMIC_REQUIRES_RULESET
SEGMENT_ALREADY_ACTIVE
SEGMENT_ALREADY_ARCHIVED
SEGMENT_ACTIVE_CANNOT_EDIT
SEGMENT_IN_USE
SEGMENT_COMPOSITION_CYCLE
SEGMENT_COMPOSITION_DEPTH_EXCEEDED
SEGMENT_MEMBER_LIMIT_EXCEEDED
SEGMENT_EXPORT_IN_PROGRESS
```

Responses seguem Problem Details de R003.

---

# 12. Fluxos principais

## 12.1 Criar segmento estático

1. autenticar e resolver projeto;
2. validar nome, descrição, tipo;
3. verificar unicidade de nome no projeto;
4. criar `Segment` com `Type = Static`, `Status = Draft`;
5. registrar auditoria e outbox;
6. retornar `201` com segmento.

## 12.2 Ativar segmento dinâmico

1. validar que está em `Draft`;
2. carregar RuleSet referenciado e verificar se está `Active`;
3. registrar `RuleSetVersion` atual no segmento;
4. (se ativo) disparar job de população inicial do cache;
5. transicionar para `Active`;
6. registrar auditoria e outbox;
7. invalidar cache de consulta.

## 12.3 Member check em tempo real

1. resolver segmento por ID;
2. conforme tipo:
   - `Static`: consulta `SegmentCustomer` (Redis ou DB);
   - `DynamicPassive`: carregar RuleSet versionado + EvaluationContext
     do Customer, executar engine (R011), retornar resultado;
   - `DynamicActive`: consultar membership em cache Redis;
3. retornar `{ member: true/false, evaluated_at, source }`.

## 12.4 Adicionar clientes ao estático

1. validar que segmento é `Static` e `Active | Draft`;
2. deduplicar Customer IDs contra `SegmentCustomer` existente;
3. inserir em lote transacional;
4. atualizar `CustomerCount`;
5. invalidar cache;
6. registrar auditoria e outbox.

## 12.5 Exportar membros

1. validar permissão;
2. criar job de exportação assíncrona;
3. worker gera CSV/JSON com Customer IDs;
4. disponibilizar URL temporária criptografada;
5. notificar via outbox;
6. expirar arquivo após período configurável.

---

# 13. Portal

## 13.1 Lista

- busca por nome e tipo;
- filtros de status (Draft, Active, Archived);
- paginação server-side;
- contagem de membros visível;
- indicador de "em uso por" campanhas/regras;
- bulk actions (archive).

## 13.2 Detalhe

Abas:

```text
Overview | Members (static) | Criteria (dynamic) | Composition | Usage | Audit
```

## 13.3 Criação/Edição

Wizard com:

- nome, descrição, tipo;
- (estático) upload de CSV de Customer IDs ou busca/seleção;
- (dinâmico) selector de RuleSet existente ou criação inline;
- (composto) builder visual de referências AND/OR/NOT;
- preview de contagem antes de salvar.

## 13.4 Ativação

Confirmação com diff de alterações desde a última ativação, impacto estimado
(número de clientes afetados) e opção de ativação programada.

## 13.5 Member check

Campo de busca de Customer por source ID, e-mail ou ID, com resultado
sim/não e fonte da avaliação (DB/cache/engine).

---

# 14. Auditoria e eventos

Audit actions:

```text
segment.created
segment.updated
segment.activated
segment.deactivated
segment.archived
segment.members.added
segment.members.removed
segment.members.imported
segment.members.exported
```

Outbox events:

```text
segment.created
segment.activated
segment.archived
segment.member.entered       (apenas DynamicActive)
segment.member.left          (apenas DynamicActive)
segment.membership.changed   (reconciliação de lote)
```

Payload padrão: IDs, tipo de mudança, timestamp, versão e request ID.
Eventos de member não carregam PII.

---

# 15. Jobs

- `SegmentActivationWorker` — população inicial de cache e reconciliação ao
  ativar segmento dinâmico;
- `SegmentReconciliationWorker` — sincronização periódica de membership de
  segmentos ativos;
- `SegmentMemberCheckWorker` — reavaliação individual acionada por evento de
  cliente (R007);
- `SegmentExportWorker` — geração assíncrona de arquivo de membros;
- `SegmentCleanupWorker` — expurgo de exports expirados e soft deletes
  elegíveis.

Jobs possuem lease, retry, dead-letter, idempotência, progresso e métricas.

---

# 16. Segurança

- TLS obrigatório fora de Development;
- validação obrigatória de tenant em toda operação;
- nenhum Customer ID externo ou PII em logs, métricas, URLs ou eventos;
- exports criptografados e temporários;
- confirmação elevada para ativação e archive de segmentos com campanhas
  vinculadas;
- proteção contra enumeração de membership por Customer ID não autorizado;
- CORS, scopes, rate limit e quotas de R003;
- proteção contra timing attack em member check;
- composição avaliada com proteção contra ciclo infinito;
- threat model e testes cross-tenant.

---

# 17. Observabilidade e requisitos não funcionais

Métricas:

```text
segments.created
segments.activated
segments.archived
segments.member_check.latency
segments.member_check.hits      (cache hit/miss)
segments.reconciliation.runs
segments.reconciliation.members_changed
segments.export.completed
segments.export.failed
```

Metas iniciais:

- member check p95 < 50 ms (cache) / < 200 ms (engine);
- member check p95 < 50 ms para estático (lookup);
- preview de audiência p95 < 2 s para 10k clientes;
- ativação de segmento dinâmico < 30 s (inclui população de cache);
- exportação de 100k membros < 60 s;
- cache miss ratio < 10% para segmentos ativos;
- disponibilidade segue SLO da API;
- RPO/RTO seguem R029.

---

# 18. Critérios de aceite

## CA-001 — Isolamento de tenant

Nenhum segmento, membership ou auditoria atravessa projeto/organização.

## CA-002 — Estático

Criar segmento estático, adicionar clientes e verificar member check retorna
os membros adicionados.

## CA-003 — Dinâmico passivo

Criar segmento dinâmico com RuleSet de `order.total > 100`, avaliar member
check para Customer com pedido de $150 retorna `true`; com $50 retorna `false`.

## CA-004 — Ativação congela critérios

Ativar segmento dinâmico, alterar o RuleSet, verificar que avaliação ainda
usa a versão congelada.

## CA-005 — Composto sem ciclo

Avaliar audiência composta (A AND B) NOT C com referências válidas e verificar
resultado esperado. Referência circular retorna erro.

## CA-006 — Member check determinístico

Duas chamadas com mesmo Customer e segmento retornam mesmo resultado.

## CA-007 — Exportação

Export gera CSV com Customer IDs, arquivo expira, requer autorização.

## CA-008 — Portal

Usuário cria, ativa, arquiva e visualiza segmentos e suas associações.

## CA-009 — API

Erros, paginação, idempotência, scopes e versionamento seguem R003.

## CA-010 — Auditoria

Operações críticas (ativação, archive, alteração de critérios) são rastreáveis.

## CA-011 — Reversão de cache

Remover Customer de segmento estático reflete imediatamente no member check
após invalidação de cache.

## CA-012 — Evento de entrada/saída

Adicionar Customer a segmento `DynamicActive` gera evento `member.entered`.
Remover gera `member.left`.

---

# 19. Testes obrigatórios

## 19.1 Unitários

- normalização e unicidade de nome;
- validação de tipo vs. RuleSetId;
- transições de estado (Draft → Active → Archived);
- composição sem ciclo;
- deduplicação de Customer IDs;
- preview com limites de amostra;
- conversão de tipo entre contratos.

## 19.2 Integração

- CRUD completo com PostgreSQL;
- adição/remoção de membros em segmento estático;
- unique constraint de nome e membership;
- isolamento cross-tenant;
- ativação com versionamento de RuleSet;
- member check para cada tipo de segmento;
- outbox e auditoria;
- paginação de membros;
- export e expiração.

## 19.3 Segurança

- acesso cross-tenant a segmento;
- adição de membro sem permissão;
- member check com Customer de outro tenant;
- export sem permissão;
- payload de composição malicioso (ciclo, profundidade excessiva);
- timing e enumeração de membership.

## 19.4 Frontend/E2E

- create/edit/activate/archive;
- adicionar/remover membros em estático;
- member check;
- preview de audiência;
- loading, empty, error e permissão;
- composição com builder visual.

---

# 20. Ordem de implementação

## Interação 1 — Hardening do Segment atual

Migrar `SegmentType` para incluir `DynamicPassive` e `DynamicActive`; adicionar
`Status`, `RuleSetVersion`, `CompositionJson` e `RowVersion`; validar regras
RN-001 a RN-005; ajustar contratos para suportar novos tipos; corrigir
paginação e filtros; adicionar índices; testes e migration compatível.

## Interação 2 — Ciclo de vida e estado

Implementar ativação (congela RuleSet version), desativação, archive com
validação de dependências (RN-007 a RN-010); auditoria e outbox para
transições; testes.

## Interação 3 — Member check em tempo real

Implementar `POST /segments/{id}/check` e `POST /segments/check-many` para
os três tipos; integração com RuleEngine (R011) para dinâmico passivo;
cache Redis para dinâmico ativo com TTL; testes unitários e integração.

## Interação 4 — Preview e composição

Implementar `POST /segments/preview` com contagem e amostra; suporte a
audiência composta AND/OR/NOT com proteção de ciclo e profundidade; builder
visual no portal.

## Interação 5 — Eventos e segmentos ativos

Implementar eventos `segment.member.entered` e `segment.member.left` via
outbox; job de reconciliação periódica; reavaliação individual por evento de
cliente; invalidação de cache.

## Interação 6 — Exportação e import

Export assíncrono de membros (CSV/JSON); import para segmento estático via
R025; arquivo criptografado com URL temporária; job de cleanup.

## Interação 7 — Portal administrativo

Lista, detalhe, criação/edição wizard, ativação com preview, member check
UI, aba de auditoria.

## Interação 8 — Hardening e operação

E2E, concorrência, segurança, performance, observabilidade, runbook e smoke
DEV/HML.

Cada interação exige implementação, testes, build, documentação, evidência,
migration quando aplicável, commit e validação proporcional ao risco.

---

# 21. Baseline atual

## 21.1 Já existe

- entidade `Segment` com `AccountId`, `ProjectId`, `Name`, `Description`,
  `Type` (Static/Dynamic), `RuleSetId`, `CustomerCount`;
- entidade `SegmentCustomer` com `SegmentId` e `CustomerId` (N:N);
- enum `SegmentType` com `Static = 0` e `Dynamic = 1`;
- `ISegmentRepository` / `SegmentRepository` com CRUD completo, adição e
  remoção de clientes, contagem por segmento;
- `ISegmentService` / `SegmentService` com CRUD, adicionar/remover clientes;
- `SegmentEndpoints` REST: GET list, GET by id, POST create, PATCH update,
  DELETE, POST add customers, DELETE remove customer;
- contratos: `CreateSegmentRequest`, `UpdateSegmentRequest`,
  `SegmentResponse`, `AddCustomersToSegmentRequest`;
- configuração EF Core com `segments` e `segment_customers` tables;
- unique index `(SegmentId, CustomerId)` e tenant index `(AccountId, ProjectId)`;
- soft delete via `IsDeleted`;
- 7 testes unitários no `SegmentServiceTests`;
- RuleSet entity e RuleEngine já existentes em R011 (avaliação de condições).

## 21.2 Gaps

- `SegmentType` não inclui `DynamicPassive` nem `DynamicActive`;
- não existe `Status` — segmentos não passam por Draft/Active/Archived;
- não existe `RuleSetVersion` — ativação não congela critérios;
- não existe `CompositionJson` — sem suporte a audiência composta;
- não existe `RowVersion` — sem concorrência otimista;
- member check não implementado para nenhum tipo;
- sem suporte a avaliação dinâmica real (SegmentService não chama RuleEngine);
- sem cache Redis para membership;
- sem eventos de entrada/saída;
- sem jobs de reconciliação, export ou ativação;
- sem auditoria ou outbox em operações de segmento;
- sem permissões granulares para segmentos;
- sem portal administrativo dedicado;
- paginação atual é offset-based e carrega lista inteira em memória;
- sem export ou import de membros;
- sem validação de ciclo ou profundidade de composição;
- export list retorna lista toda sem paginação;
- sem testes de integração ou segurança.

---

# 22. Migração e compatibilidade

- adicionar coluna `Status` com default `Active` para segmentos existentes;
- adicionar coluna `RuleSetVersion` nullable e `CompositionJson` nullable;
- adicionar `RowVersion` para concorrência;
- backfill `Type` existente (`Dynamic` → `DynamicPassive`);
- manter contratos existentes durante janela de depreciação;
- endpoints atuais continuam funcionando com validação estendida;
- não tornar campos obrigatórios antes do backfill;
- script idempotente e validação de consistência antes do unique de nome.

---

# 23. Referências relacionadas

- mapa de macro-requisitos R005;
- R004 para Customer e identificação;
- R011 para RuleSet, RuleEngine e motor de elegibilidade;
- R003 para contratos de API e segurança;
- R007 para eventos de cliente e reavaliação;
- R020 para distribuições baseadas em segmento;
- R022 para metadata aplicada a filtros de segmento;
- R023 para analytics e exportações;
- R025 para bulk import;
- R028 para compliance.

Referências públicas de produto devem ser revalidadas no início da implementação,
pois contratos externos podem mudar.

---

# 24. Checklist de entrega

```text
[x] Segment hardening e migration compatível
[x] Status (Draft) no modelo e contratos
[ ] RuleSet versionamento na ativação
[ ] Composição booleana com proteção de ciclo
[ ] Member check em tempo real (3 tipos)
[ ] Cache Redis para membership ativa
[ ] Eventos de entrada/saída (DynamicActive)
[ ] Audit trail e outbox
[ ] Job de reconciliação periódica
[ ] Exportação de membros
[ ] Import para segmento estático
[ ] Portal administrativo
[ ] Permissões e scopes
[x] Testes unitários (285 aprovados, 0 warnings)
[x] Migrations validadas
[ ] Deploy DEV validado
[ ] Deploy HML validado
[x] Documento detalhado criado
```

---

# 25. Status do detalhamento

Detalhamento concluído em 2026-07-07.

## Interação 1 — Hardening do Segment atual — concluída em 2026-07-07

Entregue:

- `SegmentType` com `DynamicPassive = 2` e `DynamicActive = 3`;
- `SegmentStatus` enum (`Draft`, `Active`, `Archived`);
- `Segment` com `Status = Draft` como padrão, `RuleSetVersion` (nullable),
  `CompositionJson` (JSONB) e `Version` (concorrência, `long`, default 1);
- RN-002: nome normalizado único por projeto (unique index parcial com
  `IsDeleted = false`);
- RN-003: segmento estático com `RuleSetId` rejeita com `400`;
- RN-004: segmento dinâmico sem `RuleSetId` rejeita com `400`;
- RN-005: apenas segmentos estáticos aceitam adição manual de clientes
  (já existente, mantido);
- contratos com `Status`, `RuleSetVersion`, `CompositionJson`, `Version`;
- backward compat: string `"Dynamic"` mapeia para `DynamicPassive`;
- `ListAsync` com paginação no banco (skip/take), sem carga integral em memória;
- `PaginationHelpers.ToPagedResponse` com overload para `totalCount` explícito;
- frontend com `StatusBadge`, `TypeBadge` para `DynamicPassive`/`DynamicActive`;
- migration `AddR005SegmentHardening` com 4 colunas + unique index;
- 12 testes unitários do segment service (7 novos, 5 adaptados);
- 285 testes unitários aprovados (build + 0 warnings).

Próximo passo recomendado:

```text
R005.2 — Ciclo de vida e estado
```
