# Especificação Funcional e Técnica — Plataforma de APIs, Credenciais e OAuth

> **Produto:** Voucher System  
> **Macro-requisito:** R003  
> **Dependências:** R001 — Organizações, Logins, Papéis e Permissões; R002 — Projetos, Ambientes, Marcas e Localizações  
> **Stack alvo:** .NET 10 + PostgreSQL via EF Core 10 + Redis + React + TypeScript + Vite  
> **Status:** especificação para implementação incremental  
> **Última revisão:** 2026-07-03

---

# 1. Visão geral

Este documento especifica a plataforma pública de integração do Voucher System:
superfícies de API, credenciais, OAuth 2.0, contratos HTTP, idempotência, limites,
versionamento, documentação e experiência operacional.

O objetivo é permitir integrações server-to-server, aplicações web/mobile e
automação administrativa sem compartilhar credenciais humanas ou expor secrets.

```text
Integrador server-side ── Application Credential / OAuth ── Application API
Web ou mobile público ─── Client Credential ─────────────── Client API reduzida
Automação organizacional ─ Management Credential ───────── Management API
Usuário do portal ──────── JWT de sessão ───────────────── Administrative API
```

Toda identidade técnica pertence a uma organização e, salvo credenciais de
management, a um projeto imutável.

---

# 2. Objetivos de negócio

## 2.1 Objetivo principal

Oferecer uma plataforma de integração segura, previsível, observável e governável,
adequada a servidores, aplicações públicas e parceiros.

## 2.2 Objetivos secundários

- separar credenciais por finalidade e ambiente;
- aplicar privilégio mínimo por scopes;
- reduzir impacto de vazamento com rotação, expiração e tokens curtos;
- impedir abuso com allowlists, CORS e rate limiting distribuído;
- padronizar erros, paginação, filtros e idempotência;
- permitir evolução da API sem quebra não planejada;
- tornar consumo, falhas e limites visíveis no portal;
- disponibilizar OpenAPI e exemplos executáveis;
- suportar automação administrativa com superfície separada;
- manter rastreabilidade de toda chamada e alteração de credencial.

## 2.3 Resultados esperados

- integrações independentes de contas humanas;
- menor raio de impacto por credencial comprometida;
- onboarding técnico mais rápido;
- menos duplicidade em retries;
- diagnóstico por `request_id`;
- capacidade e custo controlados por projeto e credencial;
- mudanças incompatíveis com migração explícita.

---

# 3. Escopo

## 3.1 Incluído

- Application, Client e Management credentials;
- criação, consulta, edição, rotação, bloqueio, revogação e expiração;
- secrets apresentados uma única vez e armazenados apenas como hash;
- scopes tipados e autorização por endpoint;
- allowlist de IP/CIDR para credenciais privadas;
- allowlist de origins para Client API;
- OAuth 2.0 Client Credentials;
- emissão, introspecção e revogação de access tokens;
- superfícies Administrative, Application, Client e Management;
- contratos HTTP globais;
- paginação, filtros, ordenação, expansão e seleção de campos;
- idempotência transversal para mutações críticas;
- rate limits e quotas distribuídos;
- headers operacionais;
- versionamento e depreciação;
- OpenAPI, playground controlado e guia de integração;
- portal de credenciais, uso e logs;
- auditoria, métricas e alertas.

## 3.2 Fora do escopo

- login, MFA, SSO e sessão humana: R001;
- fronteira e ciclo de vida de projetos: R002;
- webhooks e assinatura de eventos enviados: R021;
- metadata schemas: R022;
- planos comerciais e entitlements: R031;
- arquitetura global de multi-tenancy: R027;
- compliance e gestão corporativa de secrets: R028;
- observabilidade global: R029;
- SDKs oficiais completos: evolução posterior, após estabilização da API.

R003 define contratos que os demais módulos devem obedecer.

---

# 4. Conceitos de domínio

## 4.1 Superfícies de API

```text
Administrative API  /api/...            JWT humano
Application API     /api/v1/...         API key privada ou Bearer OAuth
Client API          /client/v1/...      credencial publicável + Origin
Management API      /management/v1/...  credencial organizacional
OAuth               /oauth/...          emissão e gestão de tokens
```

As rotas atuais em `/api` serão classificadas gradualmente. Compatibilidade deve ser
preservada durante a migração.

## 4.2 Credencial de aplicação

Credencial privada para backend. Possui identificador público e secret, projeto
fixo, scopes, expiração e allowlist de rede.

## 4.3 Credencial client-side

Credencial publicável para browser ou mobile. Não é tratada como secret e concede
acesso somente à Client API. Exige origem autorizada quando aplicável.

## 4.4 Credencial de management

Credencial privada no escopo da organização para automação administrativa. Seu
acesso a projetos e ações é explícito e nunca implícito por header.

## 4.5 Scope

Permissão técnica atribuída a uma credencial ou token.

Padrão:

```text
recurso.ação
```

Exemplos:

```text
campaigns.read
vouchers.write
validations.execute
redemptions.execute
projects.read
projects.manage
```

## 4.6 OAuth client e access token

No MVP, uma Application Credential atua como OAuth client no fluxo
`client_credentials`. O access token é curto, limitado por scopes e herda as
restrições da credencial de origem.

## 4.7 Idempotency key

Chave opaca fornecida pelo consumidor para identificar uma tentativa lógica de
mutação. Seu escopo inclui organização, projeto, credencial, operação e chave.

## 4.8 Request ID e correlation ID

- `request_id`: identifica uma chamada HTTP;
- `correlation_id`: conecta várias chamadas, jobs e eventos do mesmo fluxo.

Ambos devem ser retornados ao consumidor e propagados internamente.

## 4.9 Quota e rate limit

- quota: volume permitido em período comercial;
- rate limit: velocidade operacional permitida em janela curta.

## 4.10 Versão de API

Contrato selecionado pelo projeto ou sobrescrito por request dentro das versões
suportadas. Versões incompatíveis são datadas.

---

# 5. Decisões de modelagem

## 5.1 Credencial unificada com tipo explícito

`ApiCredential` substitui progressivamente o conceito limitado de `ApiKey`.
Application, Client e Management compartilham ciclo de vida, mas possuem políticas
distintas.

## 5.2 Identificador e secret separados

O identificador/prefixo pode ser consultado e logado de forma sanitizada. O secret:

- é gerado com CSPRNG;
- é mostrado somente na criação/rotação;
- nunca é persistido ou logado em claro;
- é comparado por hash resistente.

## 5.3 Scopes normalizados

Scopes deixam de ser CSV livre e passam a relacionamentos validados com catálogo.

## 5.4 Access token opaco no MVP

Tokens OAuth serão opacos e armazenados somente por hash. Isso permite revogação
imediata e evita claims obsoletas. JWT client credentials exige ADR futuro.

## 5.5 Rate limit no Redis

O contador distribuído usa Redis e chave tenant-aware. PostgreSQL permanece fonte
de quotas, políticas e agregados de uso.

## 5.6 Idempotência persistente

Redis pode acelerar lookup e coordenação, mas o resultado final e a unicidade de
operações críticas ficam no PostgreSQL.

## 5.7 Versão datada

O header proposto é:

```text
X-Voucher-API-Version: 2026-07-03
```

Ausência usa a versão configurada no projeto. Uma versão não suportada retorna erro
explícito, sem fallback silencioso.

---

# 6. Regras de negócio

## RN-001 — Credencial possui proprietário

Toda credencial pertence a `AccountId`; Application e Client exigem `ProjectId`.
Management pode ter escopo organizacional e projetos permitidos.

## RN-002 — Projeto da credencial é imutável

Headers ou payloads nunca podem trocar o projeto resolvido pela credencial.

## RN-003 — Tipo define superfície

Uma Client Credential nunca autentica Application ou Management API.

## RN-004 — Secret é exibido uma vez

Respostas de listagem e detalhe nunca contêm secret. Após sair da tela de criação ou
rotação, não há recuperação; somente nova rotação.

## RN-005 — Somente hash é persistido

Banco, cache, logs, traces, auditoria e eventos não armazenam secret ou token
completo.

## RN-006 — Nome é obrigatório e identificável

Nome deve representar sistema e finalidade, ser único por tipo/projeto e ter limite
de tamanho.

## RN-007 — Expiração é respeitada imediatamente

Credencial ou token expirado retorna `401 CREDENTIAL_EXPIRED`.

## RN-008 — Revogação é irreversível

Credencial revogada não pode ser reativada. Uma nova credencial deve ser criada.

## RN-009 — Bloqueio é reversível e auditado

Bloqueio por incidente impede uso, preserva configuração e pode ser removido por
usuário autorizado.

## RN-010 — Rotação suporta janela segura

Rotação pode manter versão anterior válida por janela configurável, limitada a 24
horas. O usuário pode encerrar a janela imediatamente.

## RN-011 — Scopes aplicam privilégio mínimo

Credencial sem scope exigido recebe `403 INSUFFICIENT_SCOPE`. Scope desconhecido não
pode ser salvo.

## RN-012 — Token não amplia poder

Scopes solicitados no OAuth devem ser subconjunto dos scopes da credencial.

## RN-013 — Token herda restrições

Token herda projeto, allowlist, status e bloqueios da credencial. Revogar/bloquear a
origem invalida seus tokens em até 60 segundos, com meta de efeito imediato.

## RN-014 — Access token é curto

TTL padrão: 15 minutos. Limites configuráveis devem permanecer entre 5 e 60 minutos.

## RN-015 — Client API é reduzida

Ações sensíveis client-side ficam desabilitadas por padrão. Habilitação é individual
por capacidade e exige confirmação e auditoria.

## RN-016 — Origin é obrigatório no browser

Client API valida scheme, host e porta exatos. Wildcard é permitido somente em
Development/Sandbox e deve gerar alerta.

## RN-017 — IP allowlist usa CIDR

Quando configurada, a chamada privada deve vir de IP permitido. Proxy headers só são
confiáveis quando o proxy estiver explicitamente configurado.

## RN-018 — Production exige confirmação reforçada

Criação, rotação, revogação e ampliação de scopes em Production exigem permissão,
confirmação nominal e auditoria.

## RN-019 — Rate limit é distribuído

Contagem deve ser consistente entre instâncias. Limite pode combinar organização,
projeto, credencial, endpoint e IP.

## RN-020 — Headers informam limites

Respostas autenticadas devem informar política, limite, restante e reset. `429`
inclui `Retry-After`.

## RN-021 — Quota não é rate limit

Exceder quota comercial retorna `429 QUOTA_EXCEEDED`; indisponibilidade do contador
de quota não pode gerar cobrança duplicada.

## RN-022 — Idempotência é obrigatória em mutações críticas

Resgate, reversão, publicação, bulk, import, conversão, saldo, pontos e jobs exigem
`Idempotency-Key`.

## RN-023 — Mesmo payload retorna mesmo resultado

Repetição válida retorna status e body equivalentes ao primeiro resultado, com
header `Idempotency-Replayed: true`.

## RN-024 — Chave reutilizada com payload diferente falha

Retorna `409 IDEMPOTENCY_KEY_REUSED` após comparar hash canônico da operação.

## RN-025 — Execução concorrente é coordenada

Chamadas simultâneas com a mesma chave executam a mutação uma vez; as demais aguardam
resultado ou recebem `409 IDEMPOTENCY_IN_PROGRESS`.

## RN-026 — Chaves têm retenção

Retenção mínima deve cobrir retries esperados; padrão de 24 horas e política maior
para operações financeiras.

## RN-027 — Erro segue contrato único

Toda falha JSON contém `type`, `title`, `status`, `code`, `detail`, `request_id` e,
quando seguro, `errors`.

## RN-028 — Recursos inexistentes entre tenants são ocultados

Tentativa cross-tenant retorna `404`, evitando enumeração.

## RN-029 — Paginação é server-side

Listagens novas usam cursor opaco. `limit` possui padrão e máximo. Offset legado é
mantido apenas durante compatibilidade.

## RN-030 — Ordenação é determinística

Toda ordenação inclui desempate por ID e rejeita campos não permitidos.

## RN-031 — Expansões são allowlisted

`expand` e `fields` nunca viram expressões dinâmicas irrestritas e respeitam scopes.

## RN-032 — Mudança compatível pode entrar na versão atual

Adicionar campo opcional, endpoint ou enum documentado como extensível é compatível.
Consumidores devem ignorar campos desconhecidos.

## RN-033 — Breaking change exige nova versão

Remover/renomear campo, alterar semântica ou tornar opcional obrigatório exige versão
datada, changelog e janela de migração.

## RN-034 — Depreciação é comunicada

Resposta de versão em sunset usa headers `Deprecation`, `Sunset` e `Link`.

## RN-035 — Toda chamada é rastreável

Request ID, ator técnico, projeto, rota normalizada, status, duração e consumo são
registrados sem payload sensível.

## RN-036 — Alterações críticas geram auditoria

Criar, rotacionar, bloquear, desbloquear, revogar, alterar scopes/allowlists e
configurar Client API geram audit log.

---

# 7. Permissões

## 7.1 Credenciais

```text
credentials.read
credentials.create
credentials.update
credentials.rotate
credentials.block
credentials.revoke
credentials.reveal_once
credentials.usage.read
```

## 7.2 OAuth e políticas

```text
oauth.clients.read
oauth.clients.manage
api.settings.read
api.settings.update
api.versions.manage
api.usage.read
api.logs.read
```

## 7.3 Matriz inicial

| Ação | Owner | Org Admin | Project Admin | Developer | Analyst | ReadOnly |
|---|---:|---:|---:|---:|---:|---:|
| Listar credenciais | Sim | Sim | Sim* | Sim* | Não | Não |
| Criar/editar | Sim | Sim | Sim* | Sim* | Não | Não |
| Rotacionar/revogar | Sim | Sim | Sim* | Sim* | Não | Não |
| Management credential | Sim | Sim | Não | Não | Não | Não |
| Configurar Client API | Sim | Sim | Sim* | Sim* | Não | Não |
| Consultar uso | Sim | Sim | Sim* | Sim* | Sim* | Sim* |
| Consultar logs técnicos | Sim | Sim | Sim* | Sim* | Sim* | Não |

`*` Somente em projetos atribuídos e conforme permission set.

---

# 8. Modelo de dados

## 8.1 ApiCredential

Campos mínimos:

```text
Id, AccountId, ProjectId?, Type, Name, PublicId, SecretHash
Status, ScopesVersion, ExpiresAt, LastUsedAt, LastUsedIpMasked
CreatedByUserId, CreatedAt, UpdatedAt, RevokedAt, BlockedAt
```

Índices:

```text
UX_ApiCredentials_PublicId
UX_ApiCredentials_ProjectId_Type_Name WHERE RevokedAt IS NULL
IX_ApiCredentials_AccountId_ProjectId_Status
IX_ApiCredentials_ExpiresAt
```

## 8.2 ApiCredentialSecretVersion

Mantém hash atual e anterior durante rotação segura:

```text
Id, CredentialId, SecretHash, Version, ValidFrom, ValidUntil, RevokedAt
```

## 8.3 ApiScope e ApiCredentialScope

Catálogo versionado de scopes e associação N:N. O catálogo informa superfície,
descrição, risco e se é permitido para Client API.

## 8.4 ApiCredentialNetworkRule

```text
Id, CredentialId, Type, Value, CreatedAt
```

Tipos iniciais: `IpAddress`, `Cidr`, `Origin`.

## 8.5 OAuthAccessToken

```text
Id, CredentialId, AccountId, ProjectId, TokenHash, Scopes
IssuedAt, ExpiresAt, RevokedAt, LastUsedAt
```

## 8.6 ApiIdempotencyRecord

```text
Id, AccountId, ProjectId, CredentialId, Operation, KeyHash
RequestHash, Status, ResponseStatus, ResponseHeadersJson, ResponseBodyJson
ResourceType, ResourceId, LockedUntil, CreatedAt, CompletedAt, ExpiresAt
```

Restrição única:

```text
UX_Idempotency_Account_Project_Credential_Operation_KeyHash
```

## 8.7 ApiUsageBucket

Agregado por janela, sem payload:

```text
AccountId, ProjectId, CredentialId, Route, PeriodStart
RequestCount, ErrorCount, RateLimitedCount, DurationMsTotal
```

## 8.8 ProjectApiSettings

```text
ProjectId, DefaultApiVersion, ClientApiEnabled, AllowedOrigins
ClientCapabilities, DefaultRateLimitPolicyId, UpdatedAt
```

---

# 9. Estados

## 9.1 CredentialType

```text
Application
Client
Management
```

## 9.2 CredentialStatus

```text
Active
Blocked
Expired
Revoked
```

`Expired` pode ser derivado de `ExpiresAt`; não deve divergir da data.

## 9.3 IdempotencyStatus

```text
Processing
Completed
FailedRetryable
FailedFinal
Expired
```

---

# 10. APIs

## 10.1 Administração de credenciais

```text
GET    /api/credentials
POST   /api/credentials
GET    /api/credentials/{credentialId}
PATCH  /api/credentials/{credentialId}
POST   /api/credentials/{credentialId}/rotate
POST   /api/credentials/{credentialId}/end-rotation
POST   /api/credentials/{credentialId}/block
POST   /api/credentials/{credentialId}/unblock
POST   /api/credentials/{credentialId}/revoke
GET    /api/credentials/{credentialId}/usage
GET    /api/scopes
```

Criação e rotação retornam o secret em objeto separado com
`displayed_once: true`. Demais endpoints nunca possuem esse campo.

## 10.2 OAuth

```text
POST /oauth/token
POST /oauth/introspect
POST /oauth/revoke
```

`/oauth/token` aceita `application/x-www-form-urlencoded`:

```text
grant_type=client_credentials
scope=validations.execute redemptions.execute
```

Resposta:

```json
{
  "access_token": "<one-time-token>",
  "token_type": "Bearer",
  "expires_in": 900,
  "scope": "validations.execute redemptions.execute"
}
```

## 10.3 Configurações

```text
GET   /api/projects/{projectId}/api-settings
PATCH /api/projects/{projectId}/api-settings
GET   /api/projects/{projectId}/api-versions
POST  /api/projects/{projectId}/api-version
```

## 10.4 Uso e diagnóstico

```text
GET /api/api-usage/summary
GET /api/api-usage/timeseries
GET /api/api-requests
GET /api/api-requests/{requestId}
```

Payloads, secrets, tokens e PII não são disponibilizados.

---

# 11. Contratos HTTP globais

## 11.1 Autenticação

```text
Authorization: Bearer <oauth-token>
X-Api-Key: <public-id>.<secret>                 legado em transição
X-App-Id: <public-id>
X-App-Token: <secret>
X-Client-Application-Id: <public-id>
X-Client-Token: <publishable-token>
X-Management-Id: <public-id>
X-Management-Token: <secret>
```

Headers conflitantes retornam `400 AMBIGUOUS_AUTHENTICATION`.

## 11.2 Headers de request

```text
X-Request-Id
X-Correlation-Id
X-Voucher-API-Version
Idempotency-Key
```

## 11.3 Headers de response

```text
X-Request-Id
X-Correlation-Id
X-Voucher-API-Version
RateLimit-Policy
RateLimit-Limit
RateLimit-Remaining
RateLimit-Reset
Retry-After
Idempotency-Replayed
Deprecation
Sunset
```

## 11.4 Paginação

```text
GET /api/v1/vouchers?limit=50&after=<cursor>
```

Resposta:

```json
{
  "data": [],
  "pagination": {
    "next_cursor": null,
    "has_more": false,
    "limit": 50
  }
}
```

## 11.5 Filtros, ordenação e campos

```text
filter[status]=active
filter[created_at][gte]=2026-01-01T00:00:00Z
sort=-created_at,id
expand=campaign,customer
fields=id,code,status
```

## 11.6 Erro padrão

Baseado em Problem Details:

```json
{
  "type": "https://docs.local/errors/insufficient-scope",
  "title": "Insufficient scope",
  "status": 403,
  "code": "INSUFFICIENT_SCOPE",
  "detail": "The credential cannot execute redemptions.",
  "request_id": "req_...",
  "correlation_id": "corr_...",
  "errors": []
}
```

---

# 12. Fluxos principais

## 12.1 Criar credencial

Validar acesso e quota → selecionar tipo → selecionar scopes em lista → configurar
expiração e allowlist → gerar secret → persistir hash → auditar → exibir uma vez.

## 12.2 Rotacionar sem indisponibilidade

Confirmar ação → gerar nova versão → definir janela do secret anterior → invalidar
cache → exibir novo secret → acompanhar uso da versão anterior → encerrar janela.

## 12.3 Obter token OAuth

Autenticar client → validar status/rede/projeto → validar subset de scopes → gerar
token opaco → persistir hash/TTL → devolver token uma vez.

## 12.4 Executar operação idempotente

Validar credencial → aplicar limite → reservar chave → comparar request hash →
executar transação → persistir resposta → repetir resposta em retries.

## 12.5 Chamada client-side

Identificar credencial → validar projeto/origin/capacidade → aplicar limite por
credencial e IP → executar superfície reduzida → registrar uso sanitizado.

---

# 13. Frontend

## 13.1 Lista de credenciais

Tabela com nome, tipo, status, projeto, scopes resumidos, expiração, último uso e
ações. IDs técnicos são somente leitura e entidades relacionadas são listas.

## 13.2 Assistente de criação

Etapas:

1. tipo e finalidade;
2. projeto, quando aplicável;
3. scopes por grupos selecionáveis;
4. expiração;
5. IPs, CIDRs ou origins;
6. revisão e confirmação;
7. secret com copiar e confirmação de armazenamento.

Nenhum relacionamento é digitado como GUID ou código livre.

## 13.3 Detalhe e rotação

Exibe configuração, uso, versão em rotação, alertas e histórico auditável. Secret
antigo nunca é exibido.

## 13.4 Client API settings

Lista de origins e capacidades com descrições de risco. Ações perigosas ficam em
bloco destacado e desligadas por padrão.

## 13.5 API usage

Cards e séries por período, credencial, rota e status; apresenta quota, limite,
falhas e `429`.

## 13.6 API logs

Busca por request ID, rota, credencial, status e período. Não exibe body sensível.

## 13.7 Developer docs

OpenAPI por superfície/versão, exemplos, autenticação, erros, paginação, retries,
idempotência e changelog.

---

# 14. Auditoria

Eventos mínimos:

```text
credential.created
credential.updated
credential.rotated
credential.rotation_ended
credential.blocked
credential.unblocked
credential.revoked
credential.scope_changed
credential.network_rules_changed
client_api.settings_changed
api.version_changed
oauth.token.revoked
```

Auditoria registra IDs, ator, projeto, diferenças sanitizadas, request ID e data.
Nunca registra secrets, tokens ou hashes.

---

# 15. Eventos de domínio

Eventos R003 devem ser gravados via outbox:

```text
api_credential.created
api_credential.rotated
api_credential.blocked
api_credential.revoked
api_quota.threshold_reached
api_quota.exceeded
api_version.deprecation_started
```

Payload contém identificadores e metadados seguros.

---

# 16. Jobs

## 16.1 CredentialExpirationWorker

Detecta expirações, invalida caches, encerra versões antigas e gera alertas.

## 16.2 ApiUsageAggregator

Consolida contadores Redis em agregados operacionais no PostgreSQL sem dupla
contagem.

## 16.3 IdempotencyCleanupWorker

Remove registros após retenção, preservando políticas financeiras/auditoria.

## 16.4 ApiDeprecationNotifier

Notifica responsáveis por credenciais que ainda usam versão próxima do sunset.

---

# 17. Erros padronizados

```text
INVALID_CREDENTIAL_TYPE
CREDENTIAL_NOT_FOUND
CREDENTIAL_EXPIRED
CREDENTIAL_BLOCKED
CREDENTIAL_REVOKED
INVALID_CREDENTIAL
INSUFFICIENT_SCOPE
IP_NOT_ALLOWED
ORIGIN_NOT_ALLOWED
CLIENT_CAPABILITY_DISABLED
AMBIGUOUS_AUTHENTICATION
OAUTH_INVALID_CLIENT
OAUTH_INVALID_GRANT
OAUTH_INVALID_SCOPE
OAUTH_TOKEN_EXPIRED
OAUTH_TOKEN_REVOKED
RATE_LIMIT_EXCEEDED
QUOTA_EXCEEDED
IDEMPOTENCY_KEY_REQUIRED
IDEMPOTENCY_KEY_REUSED
IDEMPOTENCY_IN_PROGRESS
API_VERSION_UNSUPPORTED
API_VERSION_SUNSET
INVALID_CURSOR
INVALID_FILTER
INVALID_SORT
INVALID_EXPANSION
```

---

# 18. Segurança

## 18.1 Geração e armazenamento

- CSPRNG com entropia mínima de 256 bits para secrets privados;
- hash resistente e comparação em tempo constante;
- token opaco por hash;
- secrets fora de logs, traces, analytics, eventos e respostas posteriores;
- caches contendo material de autenticação com TTL curto e proteção adequada.

## 18.2 Transporte

- HTTPS obrigatório fora de desenvolvimento;
- HSTS no edge;
- limites de tamanho de headers/body;
- nenhum secret em query string;
- CORS específico por origin e superfície.

## 18.3 Proteção contra abuso

- rate limit Redis;
- limite especial para falhas de autenticação e OAuth;
- atraso/controle contra brute force;
- detecção de credencial anômala;
- revogação emergencial;
- não revelar se public ID existe em falhas públicas.

## 18.4 Dados pessoais

IP completo tem retenção mínima e acesso restrito. Portal usa versão mascarada
quando o valor integral não for necessário.

---

# 19. Observabilidade

Métricas mínimas:

```text
api.requests.total
api.requests.duration
api.requests.errors
api.auth.failures
api.rate_limit.rejections
api.quota.consumed
api.idempotency.hits
api.idempotency.conflicts
oauth.tokens.issued
oauth.tokens.revoked
credentials.active
credentials.expiring
```

Dimensões devem ter cardinalidade controlada: superfície, rota normalizada, status,
tipo de credencial e ambiente. Nunca usar secret, token ou ID livre como dimensão.

---

# 20. Requisitos não funcionais

## 20.1 Performance

- autenticação/cache p95 inferior a 30 ms sem contar rede externa;
- rate limit p95 inferior a 15 ms;
- paginação sem carregar coleção completa em memória;
- índices por tenant, status e tempo;
- logs de uso fora da transação de negócio quando seguro.

## 20.2 Disponibilidade

Falha de Redis deve seguir política por risco:

- autenticação pode consultar PostgreSQL;
- mutação financeira não pode perder garantia de idempotência;
- rate limit client-side falha fechado;
- leitura administrativa pode degradar com métricas explícitas.

## 20.3 Consistência

Revogação e rotação invalidam caches. Quotas e idempotência não dependem apenas de
cache.

## 20.4 Compatibilidade

Contratos públicos possuem testes de snapshot/OpenAPI e changelog. Campos novos são
aditivos e consumidores devem tolerar campos desconhecidos.

---

# 21. Configuração

Configurações esperadas, sem secrets no repositório:

```text
Api__DefaultVersion
Api__SupportedVersions__*
Api__CredentialCacheTtlSeconds
Api__OAuthTokenTtlSeconds
Api__RotationGraceHours
Api__IdempotencyRetentionHours
Api__RateLimits__*
Api__TrustedProxies__*
```

Secrets globais e chaves criptográficas remotas devem vir do Azure Key Vault.

---

# 22. Critérios de aceite

## CA-001 — Secret one-time

Dado que uma credencial foi criada, o secret aparece na resposta inicial e nunca em
listagem, detalhe, auditoria ou logs.

## CA-002 — Isolamento

Credencial de um projeto não acessa recurso de outro, mesmo enviando outro
`X-Project-Id`.

## CA-003 — Scope

Credencial sem scope recebe `403 INSUFFICIENT_SCOPE`; adicionar scope autorizado
permite a chamada após invalidação do cache.

## CA-004 — Rotação

Novo secret funciona, anterior funciona apenas na janela escolhida e deixa de
funcionar ao encerrá-la.

## CA-005 — Revogação

Credencial revogada e tokens derivados deixam de autenticar dentro do SLA.

## CA-006 — Client API

Origin não autorizado ou capacidade desligada não executa a operação.

## CA-007 — OAuth

`client_credentials` emite token curto somente com subset de scopes; introspecção e
revogação refletem estado.

## CA-008 — Rate limit

Chamadas concorrentes em instâncias distintas compartilham limite e recebem headers
coerentes.

## CA-009 — Idempotência

Duas chamadas simultâneas iguais geram uma mutação e resposta reproduzível.

## CA-010 — Conflito idempotente

Mesma chave com payload diferente retorna `409` sem nova mutação.

## CA-011 — Erro

Qualquer erro da API pública segue Problem Details e contém request ID.

## CA-012 — Paginação

Cursor percorre dados sem duplicidade ou omissão sob ordenação determinística.

## CA-013 — Versionamento

Versão suportada é retornada; versão desconhecida falha; versão depreciada envia
headers de depreciação.

## CA-014 — Portal

Usuário administra credenciais e client settings sem digitar IDs, scopes ou
referências livres.

---

# 23. Testes obrigatórios

## 23.1 Unitários

- geração, hash e mascaramento;
- validação/transição de status;
- catálogo e subset de scopes;
- CIDR/origin;
- request hash canônico;
- contrato de erros, cursor e versão.

## 23.2 Integração

- CRUD e isolamento de credenciais;
- autenticação por cada superfície;
- cache e revogação;
- OAuth token/introspection/revoke;
- rate limit com Redis real;
- idempotência com PostgreSQL real;
- paginação server-side;
- headers e Problem Details.

## 23.3 Concorrência

- mesma idempotency key;
- rotação durante autenticação;
- revogação durante emissão/uso de token;
- limites em múltiplas instâncias;
- consolidação de uso sem dupla contagem.

## 23.4 Segurança

- secret ausente em logs e respostas;
- brute force;
- spoof de forwarded headers;
- origin bypass;
- cross-project/cross-account;
- scope escalation;
- timing e enumeração de public IDs.

## 23.5 Frontend e E2E

- criação e cópia one-time;
- listas de scopes e projetos;
- confirmação Production;
- rotação/revogação;
- uso/logs;
- permissões e estados vazio/erro/loading.

---

# 24. Ordem de implementação

## Interação 1 — Contratos e hardening da API Key atual

Inventário de rotas, `ApiCredential`, tipos/status, scopes normalizados, secret
one-time separado, validações, migration, compatibilidade e testes.

## Interação 2 — Autorização por scope e superfícies

Catálogo de scopes, policies por endpoint, separação Administrative/Application,
contexto técnico e testes de isolamento.

## Interação 3 — Ciclo de vida e portal de credenciais

Edição, expiração, bloqueio, revogação, rotação com grace period, allowlist IP/CIDR,
telas e auditoria.

## Interação 4 — Client API

Client Credential, origins, capacidades reduzidas, CORS dinâmico, limites por IP e
primeiros endpoints client-side.

## Interação 5 — OAuth 2.0 Client Credentials

Token opaco, scopes, TTL, introspecção, revogação, cache e testes.

## Interação 6 — Contratos HTTP e idempotência

Problem Details, request ID, paginação cursor, filtros/sort/expand/fields e
middleware/armazenamento idempotente para operações críticas.

## Interação 7 — Rate limits, quotas e uso

Políticas Redis, headers, fallback, agregação, limites por plano/projeto/credencial e
portal de consumo.

## Interação 8 — Versionamento e Developer Experience

Versão datada, depreciação, OpenAPI por superfície, changelog, exemplos e playground.

## Interação 9 — Management API

Credencial organizacional, escopo explícito de projetos, rotas administrativas
priorizadas, limites e auditoria.

## Interação 10 — Hardening e operação

E2E, concorrência, segurança, alertas, performance, runbook e smoke tests DEV/HML.

Cada interação só avança após código, testes, build, documentação, evidência e
roadmap da interação atual.

---

# 25. Definition of Done

R003 será concluído quando:

- três tipos de credencial estiverem isolados;
- secrets forem one-time e somente hashes persistidos;
- ciclo de vida, rotação e allowlists funcionarem;
- scopes forem tipados e aplicados aos endpoints;
- Client API possuir superfície reduzida;
- OAuth Client Credentials estiver operacional;
- idempotência crítica for uniforme e concorrente;
- rate limit distribuído e quotas forem observáveis;
- contratos de erro e paginação forem uniformes;
- versão/depreciação estiverem documentadas;
- OpenAPI e portal do desenvolvedor estiverem disponíveis;
- portal administrativo cobrir credenciais, uso e logs;
- auditoria, outbox e métricas estiverem completas;
- testes e migrations passarem;
- DEV e HML forem validados;
- documentação e evidências estiverem atualizadas.

---

# 26. Riscos e decisões técnicas

## 26.1 Migração de API keys existentes

Alterar formato pode quebrar integrações. Decisão: adaptador de compatibilidade,
telemetria de uso legado e sunset documentado.

## 26.2 BCrypt em autenticação de alto volume

Hash resistente protege secrets, mas pode custar CPU. Decisão: public ID indexado,
cache curto seguro e benchmark antes de trocar algoritmo.

## 26.3 Redis indisponível

Falhar aberto permite abuso; falhar fechado causa indisponibilidade. A política deve
ser explícita por superfície e risco, com métricas de degradação.

## 26.4 Token opaco

Exige lookup, mas facilita revogação e evita claims antigas. É a escolha do MVP.

## 26.5 Cardinalidade de logs

Logar rota crua ou credential ID em métricas degrada observabilidade. Usar rota
normalizada e consultar detalhe no storage operacional.

## 26.6 Armazenar resposta idempotente

Pode capturar PII. Persistir somente o necessário, criptografar quando aplicável e
aplicar retenção.

---

# 27. Situação atual da implementação

## 27.1 Já existe

- entidade `ApiKey` com `AccountId` e `ProjectId`;
- prefixo público e secret gerado por CSPRNG;
- hash BCrypt;
- criação, listagem, rotação e revogação;
- expiração;
- scopes em CSV;
- autenticação `X-Api-Key`;
- contexto de projeto fixo para API key;
- cache Redis com invalidação;
- API Keys no portal;
- JWT/API Key por policy scheme;
- correlation ID;
- Swagger em Development;
- rate limit local apenas para autenticação humana;
- paginação offset em parte dos endpoints;
- idempotência específica em alguns domínios;
- health checks e Application Insights.

## 27.2 Gaps para R003

- tipos Application, Client e Management;
- scopes relacionais e enforcement uniforme;
- status bloqueado e rotação com grace period;
- allowlist IP/CIDR e origins;
- OAuth;
- Client e Management APIs;
- rate limiting distribuído por credencial;
- quotas e headers operacionais;
- idempotência transversal e concorrente;
- Problem Details único;
- request ID distinto de correlation ID;
- cursor/filtros/sort/expand/fields uniformes;
- versionamento e depreciação;
- histórico de uso e logs técnicos sanitizados;
- OpenAPI separado por superfície/versão;
- portal completo e testes de segurança/concorrência.

---

# 28. Referências públicas analisadas

- [Voucherify — API introduction](https://docs.voucherify.io/api-reference/introduction-api)
- [Voucherify — Authentication and authorization](https://docs.voucherify.io/guides/authentication)
- [Voucherify — OAuth 2.0 token](https://docs.voucherify.io/api-reference/oauth/generate-oauth-20-token)
- [Voucherify — API overview](https://docs.voucherify.io/guides/api-overview)
- [Voucherify — API versioning](https://docs.voucherify.io/api-reference/versioning)
- [Voucherify — Limits](https://docs.voucherify.io/guides/limits)
- [Voucherify — Management API](https://docs.voucherify.io/docs/management-api)
- [RFC 6749 — OAuth 2.0](https://www.rfc-editor.org/rfc/rfc6749)
- [RFC 9457 — Problem Details for HTTP APIs](https://www.rfc-editor.org/rfc/rfc9457)
- [RFC 9331 — RateLimit header fields](https://www.rfc-editor.org/rfc/rfc9331)

---

# 29. Checklist de entrega

```text
[x] ApiCredential e tipos implementados — base Application em R003.1
[x] Migração compatível das API keys atuais
[x] Secrets one-time e somente hash
[x] Scopes tipados, relacionais e catalogados
[x] Scopes aplicados uniformemente à superfície Application
[x] Ciclo de vida e rotação segura
[x] IP/CIDR allowlist
[x] Client Credential e allowed origins
[x] Client API reduzida
[x] OAuth Client Credentials
[x] Introspecção e revogação
[x] Problem Details uniforme para pipeline, autenticação e erros R003
[x] Request/correlation IDs
[x] Paginação cursor e query conventions
[x] Idempotência transversal para mutações críticas iniciais
[x] Rate limit Redis
[x] Quotas e headers
[x] Versionamento e depreciação
[x] Management API
[x] OpenAPI e documentação
[x] Portal de credenciais
[x] Uso e logs sanitizados
[x] Auditoria e outbox
[x] Testes unitários, integração, concorrência e segurança
[x] Migration validada
[x] Deploy DEV validado
[ ] Deploy HML validado
[x] Documento detalhado criado
```

---

# 30. Status de implementação

## Detalhamento — concluído em 2026-07-03

Entregue:

- escopo funcional e técnico completo;
- decisões de modelagem e 36 regras de negócio;
- permissões, dados, estados e contratos HTTP;
- fluxos, frontend, auditoria, eventos, jobs e observabilidade;
- critérios de aceite e testes obrigatórios;
- baseline real e gaps;
- plano em dez interações incrementais.

Próximo passo recomendado:

```text
R003.1 — Contratos e hardening da API Key atual
```

## Interação 1 — Contratos e hardening da API Key atual — concluída em 2026-07-03

Entregue:

- evolução compatível da entidade/tabela `ApiKey` para credencial tipada;
- tipos `Application`, `Client` e `Management`, com criação restrita a Application
  até as interações próprias;
- estados `Active`, `Blocked` e `Revoked`, com status efetivo `Expired`;
- catálogo inicial de scopes e seleção por lista no portal;
- normalização, ordenação, deduplicação e rejeição de scopes desconhecidos;
- envelope one-time separado para secret em criação e rotação;
- listagens sem propriedade de secret;
- registro de último uso limitado pelo cache de autenticação;
- autenticação legada por `X-Api-Key` preservada;
- erros de validação em Problem Details;
- migration com backfill seguro para credenciais existentes;
- testes unitários, build, lint e script idempotente de migration.

Validação:

```text
Build Release: aprovado, 0 warnings e 0 erros
Testes: 209 aprovados
Frontend build/lint: aprovados
Migration idempotente: script gerado
```

Pendente para a Interação 2:

- persistência relacional dos vínculos de scope;
- policies de autorização por scope;
- classificação das superfícies Administrative e Application;
- testes de autorização por endpoint.

## Interação 2 — Autorização por scope e superfícies — concluída em 2026-07-03

Entregue:

- catálogo persistente `api_scopes`;
- associação relacional N:N `api_key_scopes`;
- backfill de scopes canônicos e aliases legados `read`, `write` e `redeem`;
- carregamento dos scopes relacionais em autenticação, listagem e rotação;
- classificação explícita das superfícies Administrative e Application;
- bloqueio de Application Credential em organizações, projetos, roles, API Keys,
  promoções de projeto e auditoria;
- resolução obrigatória de scope por rota e método na superfície Application;
- tradução de permissões humanas para scopes técnicos canônicos;
- scopes `*.read`, `*.write` e `*.execute`, sem fallback genérico;
- erros `API_SURFACE_NOT_ALLOWED`, `API_SCOPE_NOT_CONFIGURED` e
  `INSUFFICIENT_SCOPE` em Problem Details;
- testes de mapeamento por rota, método e permissão.

Validação:

```text
Build Release: aprovado, 0 warnings e 0 erros
Testes: 223 aprovados
Migration idempotente: script gerado
```

Pendente para a Interação 3:

- edição de nome, expiração e allowlist;
- bloqueio e desbloqueio;
- rotação com grace period;
- portal de detalhe e histórico;
- confirmação reforçada em Production.

## Interação 3 — Ciclo de vida e portal de credenciais — concluída em 2026-07-03

Entregue:

- consulta detalhada e edição de nome, expiração, scopes e allowlist;
- regras de rede estruturadas para IP e CIDR IPv4/IPv6;
- autenticação bloqueada fora da allowlist;
- bloqueio e desbloqueio reversíveis com invalidação imediata de cache;
- revogação irreversível preservada;
- rotação imediata ou com grace period de 1 a 24 horas;
- versões anteriores armazenadas somente como hash;
- encerramento manual da janela de rotação;
- confirmação nominal e permissão `projects.manage_production` em Production;
- auditoria de edição, rotação, encerramento, bloqueio, desbloqueio e revogação;
- portal com detalhe, edição, listas de scopes, allowlist, histórico de versões e
  ações de ciclo de vida;
- migration para versões de secret e regras de rede;
- testes de grace period, allowlist e bloqueio.

Validação:

```text
Build Release: aprovado, 0 warnings e 0 erros
Testes: 226 aprovados
Frontend build/lint: aprovados
Migration idempotente: script gerado
```

Pendente para a Interação 4:

- Client Credential;
- origins permitidas;
- capacidades client-side reduzidas;
- CORS dinâmico e limites por IP;
- endpoints iniciais da Client API.

## Interação 4 — Client API — concluída em 2026-07-03

Entregue:

- credencial publicável do tipo `Client`;
- separação estrita entre credenciais Application e Client;
- headers `X-Client-Application-Id` e `X-Client-Token`;
- scopes exclusivos `client.validations` e `client.redemptions`;
- configuração única por projeto para ativação, origins e capacidades;
- origins HTTPS normalizadas, com HTTP restrito a localhost;
- preflight CORS dinâmico somente para origins cadastradas;
- superfície `/client/v1` separada;
- validação client-side;
- resgate client-side desabilitado por padrão e habilitável explicitamente;
- rate limit inicial de 5 chamadas por 5 segundos por IP;
- portal com seleção do tipo de credencial e configuração Client API;
- confirmação reforçada para mudanças em Production;
- auditoria das configurações client-side;
- migration e testes de origins/capacidades.

Validação:

```text
Build Release: aprovado, 0 warnings e 0 erros
Testes: 229 aprovados
Frontend build/lint: aprovados
Migration idempotente: script gerado
```

Pendente para a Interação 5:

- OAuth 2.0 Client Credentials;
- access tokens opacos;
- scopes e TTL;
- introspecção e revogação.

## Interação 5 — OAuth 2.0 Client Credentials — concluída em 2026-07-03

Entregue:

- fluxo OAuth 2.0 `client_credentials`;
- autenticação do client por `X-App-Id` e `X-App-Token`;
- endpoint `POST /oauth/token` com form URL encoded;
- tokens opacos CSPRNG com prefixo `vso_`;
- persistência exclusiva do hash SHA-256;
- TTL fixo inicial de 15 minutos;
- scopes obrigatórios como subconjunto da credencial Application;
- limite de 1.000 tokens ativos por credencial;
- autenticação `Authorization: Bearer vso_...` separada do JWT humano;
- herança de organização, projeto, scopes e allowlist;
- invalidação lógica quando a credencial de origem expira, bloqueia ou revoga;
- introspecção restrita ao client proprietário;
- revogação idempotente;
- registro sanitizado de último uso;
- rate limit nos endpoints OAuth;
- migration e testes de emissão, escalada de scope e bloqueio da origem.

Validação:

```text
Build Release: aprovado, 0 warnings e 0 erros
Testes: 232 aprovados
Migration idempotente: script gerado
```

Pendente para a Interação 6:

- Problem Details transversal;
- request ID separado de correlation ID;
- paginação por cursor;
- filtros, ordenação, expansão e seleção de campos;
- middleware de idempotência para mutações críticas.

## Interação 6 — Contratos HTTP e idempotência — concluída em 2026-07-03

Entregue:

- `X-Request-ID` próprio, validado e distinto de `X-Correlation-ID`;
- propagação dos dois identificadores em headers e Problem Details;
- Problem Details para exceptions, autenticação, autorização, 404, 405 e 429;
- códigos estáveis, status, detalhe e URI de documentação;
- contrato reutilizável de paginação por cursor opaco;
- cursor determinístico por `created_at + id`;
- `limit`, `after`, `sort`, `filter[]`, `fields` e `expand` com allowlists;
- cursor e filtros aplicados à listagem de credenciais, preservando paginação legada;
- entidade transacional `ApiIdempotencyRecord`;
- reserva atômica e unicidade por tenant, ator, operação e chave;
- hash da chave e do request, sem persistir o payload de entrada;
- replay de status/body com `Idempotency-Replayed`;
- conflito para chave reutilizada com payload diferente;
- bloqueio de execução simultânea;
- retenção inicial de 24 horas;
- middleware aplicado a resgates Application/Client e publicações;
- adaptação do header `Idempotency-Key` aos contratos existentes;
- migration e testes de IDs, cursor, filtros e reserva idempotente.

Validação:

```text
Build Release: aprovado, 0 warnings e 0 erros
Testes: 236 aprovados
Migration idempotente: script gerado
```

Pendente para a Interação 7:

- rate limiting distribuído com Redis;
- políticas por organização, projeto, credencial, IP e rota;
- quotas e headers operacionais;
- agregação e portal de uso.

## Interação 7 — Rate limits, quotas e uso — concluída em 2026-07-03

Entregue:

- fixed window distribuída e atômica via Lua/Redis;
- política Application/OAuth de 100 chamadas por minuto por identidade e rota;
- política Client API de 5 chamadas por 5 segundos por identidade, rota e IP;
- isolamento das chaves por organização, projeto e credencial;
- `RateLimit-Policy`, `RateLimit-Limit`, `RateLimit-Remaining` e
  `RateLimit-Reset`;
- `Retry-After` em bloqueios;
- quota mensal transacional usando `UsageQuota` no PostgreSQL;
- headers `X-Quota-Limit`, `X-Quota-Used`, `X-Quota-Remaining` e
  `X-Quota-Reset`;
- `RATE_LIMIT_EXCEEDED`, `QUOTA_EXCEEDED` e fallback explícito;
- Client API falha fechada quando Redis está indisponível;
- Application API sinaliza modo degradado;
- agregados diários por projeto, credencial e rota;
- contagem de requests, erros, bloqueios e duração;
- endpoint `/api/api-usage/summary`;
- painel de quota e uso por rota no portal;
- migration e testes das políticas e headers.

Validação:

```text
Build Release: aprovado, 0 warnings e 0 erros
Testes: 240 aprovados
Frontend build/lint: aprovados
Migration idempotente: script gerado
```

Pendente para a Interação 8:

- versionamento datado;
- política de depreciação e sunset;
- OpenAPI separado por superfície;
- changelog, exemplos e playground.

## Interação 8 — Versionamento e Developer Experience — concluída em 2026-07-03

Entregue:

- versão datada `2026-07-03` configurável por ambiente;
- header `X-Voucher-API-Version` de request e response;
- uso da versão padrão quando o header é omitido;
- rejeição explícita de versão desconhecida com `UNSUPPORTED_API_VERSION`;
- catálogo configurável de versões suportadas e depreciadas;
- validação das configurações no startup;
- headers `Deprecation`, `Sunset` e `Link` para versões depreciadas;
- OpenAPI separado nas superfícies Administrative, Application, Client e OAuth;
- versão e header documentados em cada contrato OpenAPI;
- índice de documentação do desenvolvedor em `/docs/developer`;
- Swagger UI como playground controlado somente em Development;
- guia de integração, changelog e exemplos HTTP executáveis;
- testes unitários do contrato de versão e smoke dos quatro documentos OpenAPI.

Validação:

```text
Build Release: aprovado, 0 warnings e 0 erros
Testes: 244 aprovados
OpenAPI: quatro documentos responderam 200
Playground e índice de Developer Docs: responderam 200
Versão desconhecida: 400
```

Pendente para a Interação 9:

- credencial organizacional Management;
- escopo explícito de projetos;
- rotas administrativas priorizadas;
- limites e auditoria da Management API.

## Interação 9 — Management API — concluída em 2026-07-03

Entregue:

- credencial Management pertencente à organização;
- criação restrita a Owner e Organization Admin;
- autenticação exclusiva por `X-Management-Id` e `X-Management-Token`;
- secret one-time e hash BCrypt, reutilizando o ciclo de vida seguro;
- associação relacional N:N entre credencial e projetos permitidos;
- criação e edição com seleção de projetos por listas no portal;
- validação de que todos os projetos pertencem à mesma organização;
- scopes exclusivos `management.projects.read` e `management.usage.read`;
- rejeição de scopes de outras superfícies na criação, edição e rotação;
- rota `/management/v1/projects` limitada aos projetos atribuídos;
- rota `/management/v1/projects/{projectId}/usage` com autorização explícita;
- rate limit Redis e quota organizacional aplicados à nova superfície;
- agregação de uso usando o projeto âncora da credencial;
- auditoria sanitizada de consulta de projetos e uso;
- documento OpenAPI Management separado;
- migration, testes de segurança e documentação para integração.

Validação:

```text
Build Release: aprovado, 0 warnings e 0 erros
Testes: 246 aprovados
Frontend build/lint: aprovados
Migration idempotente: script gerado
```

Pendente para a Interação 10:

- E2E e concorrência;
- testes de segurança ofensivos;
- alertas, performance e runbook;
- smoke tests finais em DEV/HML.

## Interação 10 — Hardening e operação — concluída em 2026-07-03

Entregue:

- rejeição transversal de autenticações conflitantes com
  `AMBIGUOUS_AUTHENTICATION`;
- bloqueio de secrets, tokens e API keys na query string;
- security headers `nosniff`, `DENY`, `no-referrer` e `no-store`;
- limites Kestrel para body, headers, keep-alive e timeout de headers;
- logs sanitizados com superfície, rota normalizada, status e duração;
- métricas de requests, duração, erros, autenticação, rate limit, quota e
  idempotência com cardinalidade controlada;
- alerta operacional no log para quota acima de 90% ou esgotada;
- eventos outbox sanitizados para criação, edição, rotação, bloqueio e revogação
  de credenciais;
- E2E Testcontainers para credenciais Application e Management, segredo one-time,
  isolamento de superfície e autenticação ambígua;
- testes unitários de transporte inseguro e security headers;
- scripts PowerShell e Bash de smoke sem impressão de credenciais;
- runbook de incidentes, degradação, rollback, métricas, alertas e gate HML;
- smoke local conectado aos serviços DEV.

Validação:

```text
Build Release: aprovado, 0 warnings e 0 erros
Testes: 252 aprovados
Smoke DEV: readiness/OpenAPI 200; versão e autenticação ambígua 400
```

Pendência externa:

- executar o gate autenticado em HML quando URL e secrets de HML forem
  disponibilizados. Essa pendência mantém apenas o aceite ambiental HML aberto,
  sem débito funcional da Interação 10.
