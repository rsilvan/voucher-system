# Especificação Funcional e Técnica — Clientes, Perfis, Consentimento e Privacidade

> **Produto:** Voucher System
> **Macro-requisito:** R004
> **Dependências:** R002 — Projetos e ambientes; R003 — APIs e credenciais; R022 — Metadata e schemas
> **Stack alvo:** .NET 10 + PostgreSQL via EF Core 10 + Redis + React + TypeScript + Vite
> **Status:** especificação para implementação incremental
> **Última revisão:** 2026-07-03

---

# 1. Visão geral

R004 define o perfil operacional do consumidor usado por campanhas, regras,
segmentos, vouchers, gift cards, loyalty, referrals, distribuição e atendimento.

O cliente pertence a um projeto. A organização pode reconhecer a mesma pessoa em
projetos diferentes, mas nenhum vínculo ou dado pode atravessar projetos
implicitamente.

```text
Organization
  └── Project
      └── Customer
          ├── Identities
          ├── Contact points
          ├── Addresses
          ├── Preferences
          ├── Consents
          ├── Metadata
          ├── Activity summary
          └── Incentive relationships
```

O perfil é uma fonte operacional de personalização, não um CRM completo. Dados
devem ser coletados somente quando necessários para uma finalidade legítima.

---

# 2. Objetivos de negócio

## 2.1 Objetivo principal

Manter uma visão confiável, atualizável e auditável do consumidor para decidir
elegibilidade, atribuir incentivos e apoiar atendimento sem comprometer privacidade.

## 2.2 Resultados esperados

- integração idempotente por `source_id`;
- menor duplicidade de perfis;
- personalização por dados atuais e consentidos;
- customer 360 explicável;
- tratamento consistente de consentimento por canal e finalidade;
- atendimento de exportação, anonimização e esquecimento;
- histórico transacional preservado sem manter PII desnecessária;
- isolamento integral por `account_id` e `project_id`.

---

# 3. Escopo

## 3.1 Incluído

- CRUD, busca e upsert de clientes;
- identificadores externos e aliases;
- nome, contato, data de nascimento, endereço e preferências;
- metadata validada;
- status e ciclo de vida do perfil;
- consentimentos e evidências;
- merge, deduplicação assistida e aliases de perfis;
- importação, exportação e alterações em massa;
- customer 360;
- anonimização, retenção e direito ao esquecimento;
- auditoria, eventos, métricas e portal administrativo.

## 3.2 Fora do escopo

- segmentos e audiências: R005;
- pedidos e catálogo: R006;
- eventos detalhados de atividade: R007;
- metadata schemas: R022;
- distribuição de mensagens: R020;
- analytics globais: R023;
- políticas corporativas de compliance: R028.

R004 mantém referências e resumos necessários, sem duplicar esses domínios.

---

# 4. Conceitos de domínio

## 4.1 Customer

Perfil operacional canônico dentro de um projeto.

## 4.2 Source ID

Identificador imutável fornecido pelo sistema de origem. Substitui o termo legado
`external_id` no contrato novo, mantendo compatibilidade durante a migração.

Unicidade:

```text
project_id + source + source_id
```

Quando `source` não for enviado, utiliza-se `default`.

## 4.3 Customer identity

Identificador alternativo associado ao perfil:

- ID de CRM;
- ID de e-commerce;
- ID de aplicativo;
- ID legado;
- hash de identidade importada.

Identidade possui tipo, origem, valor normalizado, estado e data de verificação.

## 4.4 Contact point

E-mail ou telefone com valor original protegido, valor normalizado para busca,
estado de verificação, prioridade e capacidade de contato.

## 4.5 Address

Endereço estruturado de cobrança, entrega, residência ou outro tipo permitido.
País e região seguem referências controladas; coordenadas são opcionais.

## 4.6 Preference

Preferência funcional não jurídica, por exemplo idioma, moeda, loja favorita e
categorias de interesse.

## 4.7 Consent

Decisão explícita por finalidade e canal, com evidência:

```text
Granted | Denied | Withdrawn | Expired
```

Consentimento não é inferido de uma preferência.

## 4.8 Customer alias

Redirecionamento permanente de um perfil mesclado para o perfil vencedor.

## 4.9 Customer 360

Leitura agregada e paginada de perfil, consentimentos, vouchers, publicações,
validações, resgates, gift cards, loyalty e referrals. Falhas parciais são
identificadas; não se monta uma transação distribuída de leitura.

## 4.10 Anonimização

Remoção irreversível de PII mantendo identificadores técnicos e fatos
transacionais necessários para integridade, antifraude, auditoria e obrigações
legais.

---

# 5. Decisões de modelagem

## 5.1 Perfil canônico e tabelas especializadas

`Customer` mantém dados de leitura frequente. Contatos, endereços, identidades,
preferências e consentimentos ficam em tabelas próprias para unicidade, histórico
e minimização.

## 5.2 PostgreSQL como fonte de verdade

Perfis, merges e consentimentos são transacionais. Redis pode armazenar lookup
curto e projeções, nunca a evidência única.

## 5.3 PII normalizada e protegida

Busca exata usa valores normalizados ou hashes determinísticos protegidos. Logs,
métricas, eventos e audit metadata usam IDs e valores mascarados.

Criptografia de campos sensíveis deve usar envelope encryption com chave externa
em ambiente remoto. Nenhuma chave fica no repositório.

## 5.4 Exclusão em dois níveis

- arquivar: reversível, retira o perfil da operação normal;
- anonimizar/esquecer: irreversível para PII, preserva fatos obrigatórios.

DELETE físico só ocorre por job controlado quando não houver retenção ou vínculo.

## 5.5 Merge transacional

Merge escolhe um vencedor, move vínculos permitidos, resolve conflitos por política,
cria alias e anonimiza o perdedor na mesma transação.

## 5.6 Metadata condicionada ao R022

Até R022, metadata permanece JSONB com limites e tipos simples. Após R022, schema,
campos secretos, índices e validação são delegados ao catálogo de metadata.

---

# 6. Regras de negócio

## RN-001 — Tenant obrigatório

Toda leitura e escrita exige `account_id` e `project_id`. ID conhecido de outro
tenant retorna `404`.

## RN-002 — Identificação mínima

Um perfil ativo exige ao menos `source_id`, identidade, e-mail ou telefone.

## RN-003 — Source ID único

`source + source_id` é único por projeto, comparado de forma normalizada.

## RN-004 — Upsert é determinístico

Upsert por source ID cria ou atualiza um único perfil e aceita
`Idempotency-Key`. Identificadores conflitantes retornam `409`.

## RN-005 — E-mail e telefone não são identidade global

Contato igual pode sugerir duplicidade, mas não causa merge automático sem
política explícita.

## RN-006 — Normalização preserva original

E-mail usa trim e lowercase para busca. Telefone usa E.164 quando país/contexto
permite. Valor de apresentação não é usado como chave.

## RN-007 — PII não entra em URL

E-mail, telefone, nome, documento ou endereço não são aceitos em path. Busca usa
query somente quando mascarada/protegida e não deve ser logada integralmente.

## RN-008 — Data de nascimento é validada

Não aceita data futura ou idade impossível configurável. Somente data é persistida.

## RN-009 — Consentimento é finalidade + canal

Exemplo: `marketing/email`, `marketing/sms`, `profiling/none`.

## RN-010 — Evidência de consentimento

Mudança registra estado, timestamp, origem, texto/versão legal, actor técnico,
request ID e base legal quando aplicável.

## RN-011 — Withdrawal prevalece

Retirada passa a valer imediatamente. Jobs e distribuições consultam o estado
atual antes do envio.

## RN-012 — Preferência não concede consentimento

Ativar preferência de newsletter não cria `Granted`.

## RN-013 — Contato verificado é explícito

Importação ou integração não marca contato como verificado sem evidência.

## RN-014 — Endereço usa referência controlada

País, tipo e status vêm de listas; usuário não digita códigos internos.

## RN-015 — Metadata possui limites

Limites iniciais: 100 chaves, profundidade 5 e payload total 32 KB. Chaves
reservadas e conteúdo secreto são rejeitados.

## RN-016 — Merge não cruza projeto

Origem e destino precisam pertencer ao mesmo projeto.

## RN-017 — Merge é idempotente

Repetir o mesmo merge retorna o vencedor e não duplica vínculos.

## RN-018 — Perfil mesclado resolve para vencedor

Lookup por ID ou identidade antiga pode informar redirect canônico sem revelar
dados de outro tenant.

## RN-019 — Conflitos de merge são explícitos

Consentimento mais restritivo prevalece. Saldos e ledgers não são somados por
R004; cada domínio define sua política.

## RN-020 — Arquivamento é reversível

Arquivar bloqueia novas atribuições e mutações promocionais, preservando leitura
autorizada e restauração.

## RN-021 — Anonimização é irreversível

Operação exige permissão elevada, motivo e confirmação. PII recebe remoção ou
tokenização irreversível.

## RN-022 — Direito ao esquecimento respeita retenção

Solicitação pode ficar `PendingRetention` até expirar obrigação legal. O sistema
explica categorias retidas, sem expor dados desnecessários.

## RN-023 — Exportação é assíncrona

Export inclui somente dados do titular no projeto e usa arquivo criptografado,
URL temporária, expiração e auditoria.

## RN-024 — Customer 360 é paginado

Cada coleção possui cursor e limite; nenhuma tela carrega históricos ilimitados.

## RN-025 — Atualização concorrente é protegida

Perfil possui versão de concorrência/ETag. Atualização obsoleta retorna `412`.

## RN-026 — Operações em massa são assíncronas

Imports e bulk updates usam R025, com validação por linha, idempotência, progresso
e arquivo de erros sanitizado.

## RN-027 — Busca respeita privilégio mínimo

Resultado de lista mascara contatos para quem não possui permissão de PII.

## RN-028 — Dados derivados têm validade

Resumo de atividade e contagens informam `calculated_at`; não substituem ledgers.

## RN-029 — Mudança crítica é auditada

Consentimento, merge, anonimização, exportação e alteração de identidade geram
audit log e outbox.

## RN-030 — Eventos não carregam PII por padrão

Eventos contêm customer ID, projeto, tipo de alteração e request ID. Consumidores
buscam detalhes com autorização.

---

# 7. Permissões e scopes

Permissões humanas:

```text
customers.read
customers.read_pii
customers.create
customers.update
customers.archive
customers.restore
customers.merge
customers.consent.manage
customers.export
customers.anonymize
customers.retention.manage
```

Scopes técnicos:

```text
customers.read
customers.write
customers.consent.read
customers.consent.write
customers.privacy.execute
```

Owner e Organization Admin podem executar privacidade. Project Admin pode
administrar perfis no projeto, mas anonimização exige permissão explícita.

---

# 8. Modelo de dados proposto

## 8.1 Customer

```text
Id, AccountId, ProjectId, Source, SourceId
DisplayName, GivenName, FamilyName, Birthdate
Status, Locale, TimeZone, PreferredCurrency
MetadataJson, RowVersion
MergedIntoCustomerId, ArchivedAt, AnonymizedAt
LastActivityAt, CreatedAt, UpdatedAt
```

## 8.2 CustomerIdentity

```text
Id, AccountId, ProjectId, CustomerId
Type, Source, NormalizedValueHash, DisplaySuffix
IsVerified, VerifiedAt, CreatedAt, RevokedAt
```

## 8.3 CustomerContact

```text
Id, AccountId, ProjectId, CustomerId
Type, ProtectedValue, NormalizedLookupHash, MaskedValue
IsPrimary, IsVerified, VerifiedAt, Status, CreatedAt, UpdatedAt
```

## 8.4 CustomerAddress

```text
Id, AccountId, ProjectId, CustomerId, Type
Name, Line1, Line2, City, Region, PostalCode, Country
Latitude, Longitude, IsPrimary, CreatedAt, UpdatedAt
```

## 8.5 CustomerPreference

```text
Id, AccountId, ProjectId, CustomerId
Key, ValueJson, Source, CreatedAt, UpdatedAt
```

## 8.6 CustomerConsent

```text
Id, AccountId, ProjectId, CustomerId
Purpose, Channel, Status, LegalBasis
PolicyVersion, EvidenceReference, Source
EffectiveAt, ExpiresAt, CreatedAt
```

Histórico é append-only; estado atual é a entrada efetiva mais recente.

## 8.7 CustomerAlias

```text
Id, AccountId, ProjectId
AliasCustomerId, CanonicalCustomerId, MergeOperationId, CreatedAt
```

## 8.8 CustomerPrivacyRequest

```text
Id, AccountId, ProjectId, CustomerId
Type, Status, RequestedBy, Reason
RequestedAt, RetentionUntil, CompletedAt, FailureCode
```

## 8.9 Índices e constraints

- unique parcial `(ProjectId, Source, SourceId)`;
- índice `(AccountId, ProjectId, Status, CreatedAt, Id)`;
- unique de identity normalizada ativa por projeto/tipo/origem;
- índice de lookup hash de contato;
- unique `(CustomerId, Key)` para preferência atual;
- índice `(CustomerId, Purpose, Channel, EffectiveAt desc)`;
- FKs sempre compatíveis com tenant, validadas pela aplicação e testes;
- concorrência otimista em `Customer`.

---

# 9. Estados

Customer:

```text
Active → Archived → Active
Active|Archived → Merged
Active|Archived → Anonymized
```

Privacy request:

```text
Requested → InReview → PendingRetention → Processing → Completed
                                      └──→ Rejected | Failed
```

Consent:

```text
Granted | Denied | Withdrawn | Expired
```

---

# 10. APIs

## 10.1 Perfil

```text
POST   /api/customers
PUT    /api/customers/upsert
GET    /api/customers
GET    /api/customers/{customerId}
PATCH  /api/customers/{customerId}
POST   /api/customers/{customerId}/archive
POST   /api/customers/{customerId}/restore
POST   /api/customers/resolve
```

## 10.2 Sub-recursos

```text
GET|POST|PATCH|DELETE /api/customers/{id}/identities
GET|POST|PATCH|DELETE /api/customers/{id}/contacts
GET|POST|PATCH|DELETE /api/customers/{id}/addresses
GET|PUT               /api/customers/{id}/preferences
GET|POST              /api/customers/{id}/consents
```

## 10.3 Merge e privacidade

```text
POST /api/customers/merge
POST /api/customers/{id}/privacy-requests
GET  /api/customers/{id}/privacy-requests
POST /api/customer-privacy-requests/{id}/approve
POST /api/customer-privacy-requests/{id}/reject
GET  /api/customer-privacy-requests/{id}/download
```

## 10.4 Customer 360

```text
GET /api/customers/{id}/overview
GET /api/customers/{id}/activities
GET /api/customers/{id}/vouchers
GET /api/customers/{id}/redemptions
GET /api/customers/{id}/loyalty
GET /api/customers/{id}/referrals
```

## 10.5 Bulk

```text
POST /api/customers/imports
GET  /api/customers/imports/{jobId}
POST /api/customers/bulk
```

Listas usam `limit`, `after`, filtros allowlisted e ordenação determinística.
Mutações críticas usam `Idempotency-Key`.

---

# 11. Contratos e validação

Create/upsert aceita objetos estruturados e referências por lista. Códigos de país,
locale, timezone, finalidade, canal, tipo de contato e endereço são enumerados ou
obtidos por endpoints de referência.

Erros estáveis:

```text
CUSTOMER_NOT_FOUND
CUSTOMER_IDENTITY_REQUIRED
CUSTOMER_SOURCE_ID_CONFLICT
CUSTOMER_IDENTITY_CONFLICT
CUSTOMER_VERSION_CONFLICT
CUSTOMER_ARCHIVED
CUSTOMER_ALREADY_MERGED
CUSTOMER_MERGE_CONFLICT
CUSTOMER_PII_ACCESS_DENIED
CONSENT_EVIDENCE_REQUIRED
PRIVACY_REQUEST_CONFLICT
PRIVACY_RETENTION_ACTIVE
METADATA_LIMIT_EXCEEDED
```

Responses seguem Problem Details de R003 e nunca retornam lookup hashes, valores
criptográficos ou evidências internas.

---

# 12. Fluxos principais

## 12.1 Upsert

1. autenticar e resolver projeto;
2. validar source/source ID e idempotência;
3. localizar perfil canônico ou alias;
4. validar ETag, contatos, metadata e referências;
5. criar/atualizar em transação;
6. invalidar cache;
7. gravar auditoria e outbox;
8. retornar perfil com PII conforme permissão.

## 12.2 Merge

1. selecionar vencedor e duplicados por lista;
2. simular conflitos;
3. confirmar operação;
4. bloquear registros em ordem determinística;
5. aplicar política por sub-recurso;
6. mover vínculos permitidos;
7. criar aliases e anonimizar perdedores;
8. auditar e publicar evento.

## 12.3 Consentimento

1. validar finalidade/canal e policy version;
2. registrar nova decisão append-only;
3. atualizar projeção de estado;
4. invalidar caches de distribuição;
5. emitir evento sem PII.

## 12.4 Esquecimento

1. abrir solicitação;
2. avaliar retenção e vínculos;
3. aprovar ou justificar rejeição;
4. processar em job idempotente;
5. anonimizar PII e arquivos derivados;
6. preservar fatos obrigatórios;
7. registrar relatório de conclusão.

---

# 13. Portal

## 13.1 Lista

- busca por source ID, nome e contatos com permissão;
- filtros de status, source, consentimento e atividade;
- paginação server-side;
- contatos mascarados;
- bulk actions controladas.

## 13.2 Perfil

Abas:

```text
Overview | Contacts | Addresses | Preferences | Consents
Incentives | Activity | Privacy | Audit
```

## 13.3 Merge

Wizard com seleção por lista, comparação lado a lado, conflitos, preview e
confirmação. IDs não são digitados livremente.

## 13.4 Privacidade

Solicitações, retenção, status, justificativas e download temporário. Ações
irreversíveis exigem confirmação reforçada.

---

# 14. Auditoria e eventos

Audit actions:

```text
customer.created
customer.updated
customer.archived
customer.restored
customer.identity_changed
customer.merged
customer.consent_changed
customer.privacy_requested
customer.anonymized
customer.exported
```

Outbox events:

```text
customer.created
customer.updated
customer.merged
customer.archived
customer.consent.granted
customer.consent.withdrawn
customer.anonymized
```

Payload padrão: IDs, tipo de mudança, timestamp, versão e request ID.

---

# 15. Jobs

- `CustomerImportWorker`;
- `CustomerPrivacyWorker`;
- `CustomerRetentionWorker`;
- `CustomerDeduplicationScanner`;
- `CustomerProjectionRebuilder`;
- cleanup de exports expirados.

Jobs possuem lease, retry, dead-letter, idempotência, progresso e métricas.

---

# 16. Segurança e privacidade

- TLS obrigatório fora de Development;
- criptografia em trânsito e para PII protegida;
- mascaramento por permissão;
- nenhuma PII em logs, métricas, URLs ou eventos;
- exports criptografados e temporários;
- confirmação elevada para merge e anonimização;
- proteção contra enumeração e timing;
- CORS, scopes, rate limit e quotas de R003;
- retenção configurável por projeto e categoria;
- trilha de acesso a PII;
- threat model e testes cross-tenant.

---

# 17. Observabilidade e requisitos não funcionais

Métricas:

```text
customers.created
customers.updated
customers.upsert_conflicts
customers.merge.completed
customers.merge.failed
customers.consent.changed
customers.privacy.pending
customers.privacy.completed
customers.import.rows
customers.import.errors
```

Metas iniciais:

- lookup por source ID p95 < 50 ms sem rede externa;
- listagem p95 < 300 ms para página de 50;
- upsert p95 < 500 ms;
- nenhuma consulta sem filtro tenant-aware;
- import suporta retomada e não carrega arquivo inteiro em memória;
- disponibilidade segue SLO da API;
- RPO/RTO seguem R029.

---

# 18. Critérios de aceite

## CA-001 — Isolamento

Nenhuma identidade, contato, alias ou customer ID atravessa projeto/organização.

## CA-002 — Upsert

Duas chamadas concorrentes para o mesmo source ID produzem um perfil.

## CA-003 — Concorrência

ETag obsoleto não sobrescreve atualização recente.

## CA-004 — Consentimento

Withdrawal passa a ser observado antes de nova distribuição.

## CA-005 — Merge

Merge concorrente não duplica vínculos e aliases resolvem para o vencedor.

## CA-006 — Privacidade

Anonimização remove PII sem quebrar fatos financeiros ou promocionais retidos.

## CA-007 — Exportação

Arquivo expira, requer autorização e não contém dados de outro titular.

## CA-008 — Portal

Usuário escolhe referências em listas e vê PII conforme permissão.

## CA-009 — API

Erros, paginação, idempotência, scopes e versionamento seguem R003.

## CA-010 — Auditoria

Operações críticas são rastreáveis sem registrar PII completa.

---

# 19. Testes obrigatórios

## 19.1 Unitários

- normalização de source ID, e-mail e telefone;
- metadata e data de nascimento;
- transições de status;
- precedência de consentimento;
- política de conflitos de merge;
- mascaramento e anonimização.

## 19.2 Integração

- unicidade e upsert concorrente com PostgreSQL;
- isolamento cross-tenant;
- ETag;
- aliases e merge transacional;
- consentimento append-only;
- outbox e auditoria;
- paginação e índices;
- export e retenção.

## 19.3 Segurança

- enumeração por source ID/contato;
- PII em logs, traces, eventos e errors;
- scope escalation;
- acesso a export expirado;
- spoof de tenant;
- payload e metadata excessivos.

## 19.4 Frontend/E2E

- create/upsert/edit/archive/restore;
- listas de referências;
- abas do customer 360;
- consentimento;
- merge com preview;
- privacidade e confirmação;
- loading, empty, error e permissão.

---

# 20. Ordem de implementação

## Interação 1 — Hardening do Customer atual

Contratos, source/source ID, normalização, validações, status, unicidade por tenant,
erros, migration compatível e testes.

## Interação 2 — Busca, upsert e concorrência

Upsert idempotente, lookup, paginação cursor, filtros, ETag e índices.

## Interação 3 — Contatos, identidades e endereços

Entidades relacionais, normalização, verificação, listas de referência e portal.

## Interação 4 — Preferências e metadata

Preferências tipadas, limites JSONB, integração progressiva com R022 e cache.

## Interação 5 — Consentimentos

Modelo append-only, finalidades/canais, evidências, APIs, portal, eventos e testes.

## Interação 6 — Customer 360

Overview e históricos paginados de vouchers, resgates, loyalty e referrals.

## Interação 7 — Merge e deduplicação

Preview, conflitos, operação transacional, aliases, scanner e wizard.

## Interação 8 — Privacidade e retenção

Solicitações, export, anonimização, retenção, jobs e confirmação elevada.

## Interação 9 — Bulk e import

Import/bulk assíncrono via R025, arquivos de erro, progresso e retomada.

## Interação 10 — Hardening e operação

E2E, concorrência, segurança, performance, observabilidade, runbook e smoke
DEV/HML.

Cada interação exige implementação, testes, build, documentação, evidência,
migration quando aplicável, commit e validação proporcional ao risco.

---

# 21. Baseline atual

## 21.1 Já existe

- entidade `Customer` com `AccountId` e `ProjectId`;
- `ExternalId`, nome, e-mail, telefone, nascimento, source e metadata JSONB;
- índice unique parcial por projeto/external ID;
- CRUD e lookup de repositório por external ID;
- soft delete;
- escopo Application e permissões básicas;
- endpoint de vouchers atribuídos;
- lista e criação no portal;
- testes unitários básicos;
- vínculos existentes em vouchers, publicações, loyalty e referrals.

## 21.2 Gaps

- `ExternalId` não considera source na unicidade;
- criação não é upsert nem idempotente;
- normalização e validação são insuficientes;
- listagem carrega `int.MaxValue` antes de paginar;
- update não possui ETag;
- delete não diferencia arquivo e anonimização;
- sem contatos/identidades/endereços relacionais;
- sem consentimentos ou preferências;
- metadata sem limites/schema;
- sem merge, aliases ou deduplicação;
- customer 360 incompleto;
- sem import/bulk funcional;
- sem privacy request, retenção, export ou anonimização;
- respostas e portal exibem PII sem permissão granular;
- sem auditoria/outbox de Customer;
- sem métricas, jobs e testes de concorrência/privacidade.

---

# 22. Migração e compatibilidade

- manter `ExternalId` como alias de contrato durante janela de depreciação;
- backfill `Source = default` e `SourceId = ExternalId`;
- criar tabelas novas sem reescrever histórico;
- manter endpoints atuais até clientes migrarem;
- não tornar campo obrigatório antes do backfill;
- script idempotente e validação de duplicidades antes do unique;
- conflitos encontrados no backfill viram relatório, nunca merge automático.

---

# 23. Referências relacionadas

- mapa de macro-requisitos R004;
- requisito legado `old/07-CLIENTES-SEGMENTOS-E-AUDIENCIA.md`;
- R002 para projeto, locale, país e timezone;
- R003 para contratos de API e segurança;
- R005 para segmentos;
- R007 para eventos;
- R022 para metadata;
- R023 para analytics e exportações;
- R025 para bulk;
- R028 para compliance.

Referências públicas de produto devem ser revalidadas no início da implementação,
pois contratos externos podem mudar.

---

# 24. Checklist de entrega

```text
[x] Customer hardening e migration compatível
[x] Source/source ID e upsert idempotente
[x] Paginação, filtros, busca e ETag
[x] Identidades e contatos
[x] Endereços e referências
[x] Preferências e metadata
[ ] Consentimentos e evidências
[ ] Customer 360
[ ] Merge, aliases e deduplicação
[ ] Privacidade, export, anonimização e retenção
[ ] Bulk/import
[ ] Portal completo
[ ] Permissões, scopes e PII masking
[ ] Auditoria e outbox
[ ] Jobs e observabilidade
[ ] Testes unitários, integração, concorrência e segurança
[ ] Migrations validadas
[ ] Deploy DEV validado
[ ] Deploy HML validado
[x] Documento detalhado criado
```

---

# 25. Status do detalhamento

Detalhamento concluído em 2026-07-03.

Próximo passo recomendado:

```text
R004.1 — Hardening do Customer atual
```

## Interação 1 — Hardening do Customer atual — concluída em 2026-07-05

Entregue:

- `CustomerStatus` com estados Active, Archived, Merged e Anonymized;
- `source` obrigatório e normalizado, com fallback `default`;
- `source_id` e chave normalizada para unicidade tenant-aware;
- compatibilidade bidirecional de contrato com `external_id`;
- normalização Unicode de source ID e nome;
- normalização e validação de e-mail;
- normalização E.164 de telefone;
- data de nascimento estrita em `yyyy-MM-dd`, sem datas futuras ou anteriores a
  1900;
- identificação mínima por source ID, e-mail ou telefone;
- source imutável após criação;
- erros estáveis em Problem Details;
- tratamento de concorrência da unique constraint como `409`;
- arquivamento legado via DELETE refletido no status;
- índices por tenant para source ID, e-mail, telefone, status e criação;
- backfill seguro de dados legados;
- auditoria e outbox sanitizados para criação, atualização e arquivamento;
- portal usando Source ID e lista controlada de origens;
- migration, testes e validação DEV.

Validação:

```text
Build Release: aprovado, 0 warnings e 0 erros
Testes: 260 aprovados
Frontend build/lint: aprovados
Migration idempotente: script gerado
PostgreSQL/Redis DEV: readiness 200
```

Pendente para a Interação 2:

- upsert idempotente;
- busca segura;
- paginação por cursor;
- filtros e ordenação;
- concorrência otimista por ETag.

## Interação 2 — Busca, upsert e concorrência — concluída em 2026-07-05

Entregue:

- upsert por `source` e `source_id`, protegido por `Idempotency-Key`;
- resolução determinística de Customer pela identidade externa;
- listagem tenant-aware executada no PostgreSQL, sem carga integral em memória;
- paginação estável por cursor composto de `created_at` e `id`;
- filtros allowlisted por origem, status e busca;
- ordenação allowlisted por data de criação;
- busca normalizada por nome, e-mail, telefone e source ID;
- versão persistida e ETag no contrato HTTP;
- `If-Match` obrigatório em atualizações e em upsert de registros existentes;
- respostas `428` para precondição ausente e `412` para versão obsoleta;
- concorrência otimista configurada no EF Core;
- auditoria e outbox preservadas para criação e atualização;
- portal com busca, filtros em listas e navegação por cursor;
- migration e testes proporcionais ao risco.

Próximo passo recomendado:

```text
R004.3 — Contatos, identidades e endereços
```

## Interação 3 — Contatos, identidades e endereços — concluída em 2026-07-05

Entregue:

- entidades relacionais tenant-aware `CustomerIdentity`, `CustomerContact` e
  `CustomerAddress`;
- APIs autenticadas de listagem, criação, atualização e remoção lógica;
- identidade com tipo/origem controlados, hash HMAC de lookup, sufixo de exibição,
  verificação e revogação;
- contatos de e-mail e telefone normalizados, protegidos pelo Data Protection,
  pesquisáveis por hash interno e sempre mascarados nas respostas;
- verificação explícita de contato, status e contato principal por tipo;
- endereços estruturados com tipo, país, região e coordenadas validados;
- catálogo de referências para tipos, status, países e regiões;
- troca de contato/endereço principal protegida por transação;
- constraints e índices tenant-aware no PostgreSQL;
- idempotência obrigatória na criação de sub-recursos;
- auditoria e outbox sem PII;
- portal de perfil com abas Contacts, Identities e Addresses, usando listas para
  referências;
- testes unitários, integração, migration e validação DEV.

Próximo passo recomendado:

```text
R004.4 — Preferências e metadata
```

## Interação 4 — Preferências e metadata — concluída em 2026-07-06

Entregue:

- preferências tipadas em tabela relacional, únicas por Customer e chave;
- API para listar, criar/atualizar e remover preferências;
- chaves normalizadas, allowlist de formato e bloqueio de nomes secretos;
- valores JSON tipados com limites próprios;
- `Customer.Metadata` migrado no código de `Dictionary<string,string>` para
  documento JSON tipado, sem camada legada;
- limite de 100 chaves, profundidade 5 e payload de 32 KB;
- rejeição de chaves reservadas, secretas e valores JSON inválidos;
- validação progressiva pelo schema R022 de object type `customer`;
- validação aplicada em create, update, upsert e endpoint dedicado;
- PostgreSQL como fonte de verdade e cache Redis de curta duração com invalidação;
- auditoria e outbox sem conteúdo de preferências ou metadata;
- portal com referências controladas para preferências e editor de metadata;
- migration, testes e validação DEV.

Próximo passo recomendado:

```text
R004.5 — Consentimentos
```
