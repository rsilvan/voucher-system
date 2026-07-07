# Especificação Funcional e Técnica — Projetos, Ambientes, Marcas e Localizações

> **Produto:** Voucher System
> **Macro-requisito:** R002
> **Dependência:** R001 — Organizações, Logins, Papéis e Permissões
> **Stack alvo:** .NET 10 + PostgreSQL via EF Core 10 + Redis + React + TypeScript + Vite
> **Status:** especificação para implementação incremental
> **Última revisão:** 2026-07-02

---

# 1. Visão geral

Este documento especifica o domínio de projetos, ambientes, marcas, lojas, áreas de
acesso e localizações do Voucher System.

Uma organização pode operar:

- marcas diferentes;
- países e moedas diferentes;
- unidades de negócio independentes;
- ambientes de teste e produção;
- lojas físicas ou canais digitais;
- campanhas restritas por área ou localização.

O `Project` é a fronteira operacional principal abaixo da organização. Campanhas,
clientes, produtos, pedidos, vouchers, API keys, webhooks, schemas, eventos e
analytics pertencem a um projeto.

```text
Organization
  ├── Project: Marca A / Sandbox / BRL
  ├── Project: Marca A / Production / BRL
  ├── Project: Marca B / Production / USD
  └── Project: Europa / Production / EUR
```

---

# 2. Objetivos de negócio

## 2.1 Objetivo principal

Permitir que uma organização separe contextos operacionais sem risco de mistura de
dados, credenciais, campanhas ou transações.

## 2.2 Objetivos secundários

- suportar múltiplas marcas e regiões;
- separar testes de produção;
- centralizar moeda, fuso e localização padrão;
- definir identidade visual e informações legais da marca;
- controlar acesso de membros e integrações por projeto;
- restringir campanhas por lojas, áreas e geofences;
- promover configurações aprovadas entre ambientes;
- medir uso e saúde por projeto;
- permitir arquivamento sem apagar histórico;
- preparar gestão programática via API.

## 2.3 Resultados esperados

- redução de erro humano entre Sandbox e Production;
- relatórios coerentes por marca, país e moeda;
- credenciais e webhooks isolados;
- onboarding mais rápido de novas marcas;
- reutilização segura de configurações;
- rastreabilidade de toda mudança de contexto.

---

# 3. Escopo

## 3.1 Incluído

- CRUD e ciclo de vida de projetos;
- classificação de ambiente;
- configurações regionais;
- projeto principal da organização;
- acesso por projeto;
- troca de projeto no portal;
- identidade da marca por projeto;
- lojas e áreas organizacionais;
- localizações geográficas;
- regras de isolamento;
- templates e promoção de configuração;
- métricas resumidas por projeto;
- auditoria;
- quotas;
- APIs e telas administrativas.

## 3.2 Fora do escopo

Serão detalhados em outros requisitos:

- credenciais e OAuth: R003;
- metadata schemas: R022;
- campanhas e templates de campanha: R008;
- regras geográficas de elegibilidade: R011;
- webhooks: R021;
- billing e entitlements: R031;
- arquitetura global de multi-tenancy: R027.

R002 define os vínculos e contratos necessários para esses domínios.

---

# 4. Conceitos de domínio

## 4.1 Organização

Raiz comercial e administrativa do tenant. No modelo físico atual é representada
por `Account`.

Uma organização:

- possui um ou mais projetos;
- possui um projeto principal;
- possui quota máxima de projetos;
- controla membros e roles;
- pode ser suspensa sem apagar projetos.

## 4.2 Projeto

Fronteira operacional de dados e configurações.

Um projeto define:

- nome, slug e descrição;
- ambiente;
- moeda;
- fuso horário;
- locale e país;
- status;
- marca;
- lojas, áreas e localizações;
- membros autorizados;
- configurações de API e eventos.

Todos os recursos operacionais devem possuir `ProjectId`.

## 4.3 Ambiente

Classificação de finalidade do projeto.

Valores iniciais:

```text
Sandbox
Development
Staging
Production
```

Ambiente é atributo do projeto no MVP. Não é uma tabela independente.

## 4.4 Projeto principal

Projeto criado automaticamente no onboarding da organização.

Características:

- é o fallback inicial do portal;
- não pode ser removido enquanto for o único projeto;
- pode deixar de ser principal somente após outro projeto ser definido;
- a organização deve manter ao menos um projeto ativo.

## 4.5 Marca

Configuração de identidade e comunicação associada ao projeto.

No MVP:

- um projeto possui zero ou uma `BrandProfile`;
- múltiplas marcas são representadas por múltiplos projetos;
- a marca pode ser herdada pela comunicação e pelos portais públicos.

## 4.6 Loja

Unidade comercial física ou digital.

Exemplos:

- loja física;
- site;
- aplicativo;
- marketplace;
- franquia;
- terminal ou ponto de venda.

Loja não é sinônimo de localização geográfica. Uma loja pode possuir endereço e
coordenadas, mas também pode ser apenas um canal digital.

## 4.7 Área

Agrupamento hierárquico de lojas ou escopo organizacional.

Exemplos:

- Região Sul;
- Franquias;
- E-commerce;
- Lojas próprias;
- Unidade de negócio B2B.

Áreas podem ser usadas para restringir acesso de membros, API keys e campanhas.

## 4.8 Localização geográfica

Objeto espacial reutilizável por regras.

Tipos iniciais:

```text
Circle
Polygon
MultiPolygon
```

Uma localização pode representar:

- raio em torno de uma loja;
- cidade;
- estado;
- país;
- zona de entrega;
- área de exclusão.

## 4.9 Promoção de configuração

Processo controlado de copiar configurações entre projetos.

Não significa copiar transações, clientes ou saldos.

Exemplos de recursos promovíveis:

- campaign templates;
- metadata schemas;
- event schemas;
- configurações de stacking;
- modelos de distribuição;
- marca, opcionalmente.

## 4.10 Contexto de projeto

Contexto resolvido para cada requisição autenticada:

```text
OrganizationId
ProjectId
Environment
Currency
TimeZone
Locale
UserId ou ApiKeyId
Permissions
CorrelationId
```

---

# 5. Decisões de modelagem

## 5.1 `Account` representa `Organization`

Mantém-se a decisão do ADR-0007. `AccountId` é a chave física de organização.

## 5.2 Ambiente como atributo

`EnvironmentType` será enum persistido no projeto.

Justificativa:

- isolamento já ocorre no nível do projeto;
- evita árvore adicional de tenancy;
- cada ambiente precisa de credenciais e recursos próprios;
- simplifica autorização e roteamento.

## 5.3 Uma marca por projeto no MVP

Uma `BrandProfile` por projeto reduz ambiguidade em comunicações, moeda, locale e
relatórios.

Se uma organização precisar de marcas diferentes, deve criar projetos diferentes.

## 5.4 Loja, área e geofence são entidades distintas

- `Store`: unidade comercial;
- `Area`: agrupamento de acesso;
- `GeoLocation`: forma geográfica.

Uma loja pode referenciar uma localização, mas nenhuma delas substitui a outra.

## 5.5 Sem cópia direta de dados transacionais

Promoção entre ambientes copia definições. Nunca copia:

- resgates;
- saldos;
- pontos;
- pedidos;
- clientes, salvo importação explícita e autorizada;
- tokens;
- secrets;
- logs.

---

# 6. Regras de negócio

## RN-001 — Organização deve manter projeto ativo

Toda organização deve possuir ao menos um projeto não removido.

Não permitir:

- remover o único projeto;
- arquivar todos os projetos;
- desativar o último projeto ativo sem substituto.

Erro:

```text
LAST_ACTIVE_PROJECT_REQUIRED
```

## RN-002 — Projeto pertence a uma organização

O vínculo `OrganizationId` é obrigatório e imutável.

Um projeto nunca pode ser transferido para outra organização.

## RN-003 — Slug único por organização

O slug deve:

- ser normalizado;
- conter apenas caracteres seguros;
- ser único dentro da organização;
- permanecer estável após criação.

Alteração futura de slug exige alias e redirecionamento auditado.

## RN-004 — Nome não substitui identificador

Nome pode ser alterado. IDs e referências nunca devem depender do nome.

## RN-005 — Ambiente obrigatório

Todo projeto deve possuir um `EnvironmentType` válido.

## RN-006 — Ambiente é imutável após uso

O ambiente pode ser alterado somente quando o projeto:

- não possui recursos operacionais;
- não possui API keys;
- não possui eventos ou transações;
- não foi utilizado em integração externa.

Caso contrário:

```text
PROJECT_ENVIRONMENT_IMMUTABLE
```

## RN-007 — Production exige confirmação reforçada

Criação, arquivamento, exclusão e alterações críticas em Production exigem:

- permissão específica;
- confirmação visual com nome do projeto;
- auditoria;
- autenticação recente em evolução futura.

## RN-008 — Dados não atravessam projetos

Nenhuma consulta, comando ou evento pode retornar ou alterar recurso de outro
projeto, mesmo que pertença à mesma organização, salvo operação administrativa
explicitamente projetada para isso.

## RN-009 — Sandbox não impacta Production

Recursos Sandbox:

- usam API keys próprias;
- usam webhooks próprios;
- possuem contadores próprios;
- não consomem saldos ou quotas transacionais de Production;
- são identificados visualmente.

## RN-010 — Moeda padrão obrigatória

Moeda segue ISO 4217.

Exemplos:

```text
BRL
USD
EUR
GBP
```

Deve possuir três letras maiúsculas.

## RN-011 — Alteração de moeda é restrita

Após existir recurso monetário ou transação, a moeda do projeto não pode ser
alterada.

Erro:

```text
PROJECT_CURRENCY_IMMUTABLE
```

## RN-012 — Fuso horário obrigatório

O fuso deve usar identificador IANA.

Exemplos:

```text
America/Sao_Paulo
America/New_York
Europe/Lisbon
UTC
```

## RN-013 — Persistência temporal em UTC

Fuso do projeto serve para:

- entrada e exibição;
- calendário de campanhas;
- recorrências;
- relatórios.

Banco e eventos persistem instantes em UTC.

## RN-014 — Locale padrão

Locale deve seguir BCP 47.

Exemplos:

```text
pt-BR
en-US
es-AR
```

## RN-015 — País padrão

País usa ISO 3166-1 alpha-2.

## RN-016 — Quota de projetos

Criação deve validar `MaxProjects`.

Projetos arquivados continuam contando para quota por padrão. Planos podem definir
política diferente no R031.

## RN-017 — Acesso por projeto

Usuário visualiza apenas projetos:

- vinculados por `ProjectAccess`; ou
- todos, quando OrganizationOwner/OrganizationAdmin.

## RN-018 — API key permanece vinculada

API key possui projeto fixo. Header informado pelo cliente não pode trocar esse
projeto.

## RN-019 — Troca de projeto revalida contexto

Ao trocar projeto, o portal deve:

- validar acesso atual;
- atualizar `X-Project-Id`;
- recarregar permissões contextuais;
- invalidar dados da tela anterior;
- atualizar moeda, fuso e métricas;
- registrar contexto local sem tratar o storage como autorização.

## RN-020 — Projeto desativado bloqueia escrita

Projeto `Disabled`:

- permite leitura administrativa autorizada;
- bloqueia criação, alteração e transações;
- bloqueia APIs públicas e API keys;
- mantém histórico.

## RN-021 — Projeto arquivado é somente leitura

Projeto `Archived`:

- não aparece por padrão em seletores;
- permite consulta administrativa;
- não executa jobs operacionais;
- pode ser restaurado por permissão específica.

## RN-022 — Remoção é lógica

Projeto com histórico não deve ser removido fisicamente.

Exclusão física só pode existir para:

- projeto vazio;
- política administrativa explícita;
- job auditado;
- período de retenção cumprido.

## RN-023 — Marca é opcional

Projeto funciona sem marca. Ao configurar marca, nome é obrigatório.

## RN-024 — Uma marca ativa por projeto

No MVP existe no máximo uma `BrandProfile` não removida por projeto.

## RN-025 — URLs de marca devem ser seguras

URLs de website, termos, privacidade e logo devem:

- usar HTTPS em ambientes remotos;
- possuir tamanho máximo;
- rejeitar protocolos não permitidos;
- ser tratadas como conteúdo não confiável.

## RN-026 — Loja possui código único

`Store.Code` é único por projeto e estável.

## RN-027 — Loja pode ser digital

Endereço e geolocalização não são obrigatórios quando `StoreType` for digital.

## RN-028 — Áreas podem ser hierárquicas

Uma área pode possuir área pai do mesmo projeto.

Não permitir:

- ciclos;
- profundidade superior ao limite configurado;
- pai em outro projeto.

## RN-029 — Acesso por área é cumulativo

Acesso a uma área inclui suas lojas e subáreas, salvo exclusão explícita prevista em
evolução posterior.

## RN-030 — Localização deve ser válida

Circle exige:

- latitude entre -90 e 90;
- longitude entre -180 e 180;
- raio positivo;
- unidade normalizada.

Polygon/MultiPolygon devem usar GeoJSON válido, fechado e sem geometria vazia.

## RN-031 — Localização não rastreia usuário

O sistema apenas avalia coordenadas enviadas ou persistidas conforme consentimento.
Não realiza rastreamento contínuo.

## RN-032 — Promoção é explícita

Nenhuma configuração deve ser copiada automaticamente entre projetos.

## RN-033 — Promoção gera plano antes de executar

O usuário deve visualizar:

- recursos que serão criados;
- recursos reutilizados;
- conflitos;
- dependências ausentes;
- itens não suportados;
- impacto estimado.

## RN-034 — Secrets nunca são copiados

API keys, webhook secrets, tokens e credenciais de canal devem ser recriados no
destino.

## RN-035 — Promoção é idempotente

Repetir uma solicitação com a mesma chave não pode duplicar recursos.

## RN-036 — Cópia mantém rastreabilidade

Recursos criados devem registrar:

- projeto de origem;
- recurso de origem;
- job de promoção;
- ator;
- data;
- versão.

## RN-037 — Métricas respeitam projeto

Indicadores do seletor e dashboard são calculados exclusivamente no projeto ativo.

## RN-038 — Auditoria obrigatória

Criar, atualizar, desativar, arquivar, restaurar, remover, promover configurações,
alterar marca e administrar lojas/áreas/localizações deve gerar audit log.

---

# 7. Permissões

## 7.1 Projetos

```text
projects.read
projects.create
projects.update
projects.disable
projects.archive
projects.restore
projects.delete
projects.promote
projects.manage_production
projects.usage.read
```

## 7.2 Marca

```text
brands.read
brands.create
brands.update
brands.delete
```

## 7.3 Lojas e áreas

```text
stores.read
stores.create
stores.update
stores.archive
areas.read
areas.create
areas.update
areas.delete
```

## 7.4 Localizações

```text
locations.read
locations.create
locations.update
locations.delete
```

## 7.5 Matriz inicial

| Ação | Owner | Org Admin | Project Admin | Marketing | Developer | ReadOnly |
|---|---:|---:|---:|---:|---:|---:|
| Listar projetos acessíveis | Sim | Sim | Sim | Sim | Sim | Sim |
| Criar projeto | Sim | Sim | Não | Não | Não | Não |
| Alterar projeto | Sim | Sim | Sim* | Não | Não | Não |
| Administrar Production | Sim | Sim | Não | Não | Não | Não |
| Arquivar/restaurar | Sim | Sim | Não | Não | Não | Não |
| Promover configuração | Sim | Sim | Sim* | Não | Sim* | Não |
| Alterar marca | Sim | Sim | Sim | Não | Não | Não |
| Administrar lojas/áreas | Sim | Sim | Sim | Sim* | Não | Não |
| Administrar localizações | Sim | Sim | Sim | Sim* | Não | Não |

`*` Apenas nos projetos atribuídos e conforme permission set.

---

# 8. Modelo de dados

## 8.1 Project

```csharp
public class Project
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public EnvironmentType Environment { get; set; }
    public string Currency { get; set; } = "BRL";
    public string TimeZone { get; set; } = "America/Sao_Paulo";
    public string Locale { get; set; } = "pt-BR";
    public string Country { get; set; } = "BR";
    public ProjectStatus Status { get; set; }
    public bool IsPrimary { get; set; }
    public DateTimeOffset? DisabledAt { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
```

Índices:

```text
IX_Projects_AccountId_Slug UNIQUE
IX_Projects_AccountId_Status
IX_Projects_AccountId_Environment
UX_Projects_AccountId_IsPrimary WHERE IsPrimary = true AND IsDeleted = false
```

## 8.2 ProjectSettings

```csharp
public class ProjectSettings
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public bool CaseSensitiveCodes { get; set; }
    public string WebhookApiVersion { get; set; } = "v1";
    public string[] ClientTrustedDomains { get; set; } = [];
    public bool ClientRedeemEnabled { get; set; }
    public bool ClientPublishEnabled { get; set; }
    public bool ClientCustomerCreateEnabled { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
```

Índice:

```text
UX_ProjectSettings_ProjectId UNIQUE
```

Configurações específicas de API serão detalhadas no R003.

## 8.3 BrandProfile

```csharp
public class BrandProfile
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? WebsiteUrl { get; set; }
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? PrivacyPolicyUrl { get; set; }
    public string? TermsOfUseUrl { get; set; }
    public string? PermissionReminder { get; set; }
    public string? SupportEmail { get; set; }
    public string? SenderName { get; set; }
    public string? SenderEmail { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
```

Índice:

```text
UX_BrandProfiles_ProjectId UNIQUE
```

## 8.4 BrandAddress

```csharp
public class BrandAddress
{
    public Guid Id { get; set; }
    public Guid BrandProfileId { get; set; }
    public string? Line1 { get; set; }
    public string? Line2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
}
```

## 8.5 Store

```csharp
public class Store
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid ProjectId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public StoreType Type { get; set; }
    public StoreStatus Status { get; set; }
    public Guid? GeoLocationId { get; set; }
    public string? ExternalId { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
```

Índices:

```text
UX_Stores_ProjectId_Code UNIQUE
IX_Stores_ProjectId_Status
IX_Stores_ProjectId_ExternalId
```

## 8.6 Area

```csharp
public class Area
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? ParentAreaId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AreaStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
```

## 8.7 AreaStore

```csharp
public class AreaStore
{
    public Guid AreaId { get; set; }
    public Guid StoreId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
```

Índice:

```text
UX_AreaStores_AreaId_StoreId UNIQUE
```

## 8.8 GeoLocation

```csharp
public class GeoLocation
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public GeoLocationType Type { get; set; }
    public string ShapeJson { get; set; } = "{}";
    public GeoLocationStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
```

Implementação inicial pode usar GeoJSON validado em JSONB. PostGIS deve ser adotado
via ADR quando forem necessárias consultas espaciais complexas ou alto volume.

## 8.9 ProjectPromotionJob

```csharp
public class ProjectPromotionJob
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid SourceProjectId { get; set; }
    public Guid TargetProjectId { get; set; }
    public PromotionJobStatus Status { get; set; }
    public string ResourceSelectionJson { get; set; } = "{}";
    public string PlanJson { get; set; } = "{}";
    public string ResultJson { get; set; } = "{}";
    public string IdempotencyKey { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
```

## 8.10 ProjectResourceMapping

```csharp
public class ProjectResourceMapping
{
    public Guid Id { get; set; }
    public Guid PromotionJobId { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public string SourceResourceId { get; set; } = string.Empty;
    public string TargetResourceId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
}
```

---

# 9. Estados

## 9.1 ProjectStatus

```text
Active
Disabled
Archived
PendingDeletion
```

Transições:

```text
Active → Disabled → Active
Active → Archived → Active
Disabled → Archived
Archived → PendingDeletion
```

## 9.2 EnvironmentType

```text
Sandbox
Development
Staging
Production
```

## 9.3 StoreStatus

```text
Active
Disabled
Archived
```

## 9.4 PromotionJobStatus

```text
Draft
Planned
Queued
Running
Succeeded
PartiallySucceeded
Failed
Canceled
```

---

# 10. APIs

Todas as APIs autenticadas devem resolver organização pelo principal autenticado.
Não aceitar `accountId` arbitrário do cliente.

## 10.1 Projetos

```text
GET    /api/projects
POST   /api/projects
GET    /api/projects/{projectId}
PATCH  /api/projects/{projectId}
POST   /api/projects/{projectId}/disable
POST   /api/projects/{projectId}/enable
POST   /api/projects/{projectId}/archive
POST   /api/projects/{projectId}/restore
DELETE /api/projects/{projectId}
POST   /api/projects/{projectId}/make-primary
GET    /api/projects/{projectId}/summary
GET    /api/projects/{projectId}/usage
```

### POST /api/projects

Request:

```json
{
  "name": "Marca Brasil - Sandbox",
  "description": "Ambiente para integração e homologação",
  "environment": "Sandbox",
  "currency": "BRL",
  "timeZone": "America/Sao_Paulo",
  "locale": "pt-BR",
  "country": "BR"
}
```

Response `201`:

```json
{
  "id": "uuid",
  "name": "Marca Brasil - Sandbox",
  "slug": "marca-brasil-sandbox",
  "environment": "Sandbox",
  "currency": "BRL",
  "timeZone": "America/Sao_Paulo",
  "locale": "pt-BR",
  "country": "BR",
  "status": "Active",
  "isPrimary": false
}
```

## 10.2 Configurações

```text
GET   /api/projects/{projectId}/settings
PATCH /api/projects/{projectId}/settings
```

## 10.3 Marca

```text
GET    /api/projects/{projectId}/brand
POST   /api/projects/{projectId}/brand
PATCH  /api/projects/{projectId}/brand
DELETE /api/projects/{projectId}/brand
```

## 10.4 Lojas

```text
GET    /api/stores
POST   /api/stores
GET    /api/stores/{storeId}
PATCH  /api/stores/{storeId}
POST   /api/stores/{storeId}/disable
POST   /api/stores/{storeId}/enable
DELETE /api/stores/{storeId}
```

Listagem deve aceitar:

```text
search
status
type
areaId
externalId
cursor
limit
```

## 10.5 Áreas

```text
GET    /api/areas
POST   /api/areas
GET    /api/areas/{areaId}
PATCH  /api/areas/{areaId}
DELETE /api/areas/{areaId}
PUT    /api/areas/{areaId}/stores
GET    /api/areas/{areaId}/stores
```

## 10.6 Localizações

```text
GET    /api/locations
POST   /api/locations
GET    /api/locations/{locationId}
PATCH  /api/locations/{locationId}
DELETE /api/locations/{locationId}
POST   /api/locations/validate
```

### POST /api/locations

Circle:

```json
{
  "name": "Raio Loja Paulista",
  "type": "Circle",
  "shape": {
    "center": {
      "latitude": -23.5614,
      "longitude": -46.6559
    },
    "radius": 5,
    "unit": "km"
  }
}
```

Polygon:

```json
{
  "name": "Zona Sul",
  "type": "Polygon",
  "shape": {
    "type": "Polygon",
    "coordinates": [
      [
        [-46.70, -23.60],
        [-46.60, -23.60],
        [-46.60, -23.70],
        [-46.70, -23.60]
      ]
    ]
  }
}
```

## 10.7 Promoção entre projetos

```text
POST /api/project-promotions/plan
POST /api/project-promotions
GET  /api/project-promotions
GET  /api/project-promotions/{jobId}
POST /api/project-promotions/{jobId}/cancel
```

### POST /api/project-promotions/plan

Request:

```json
{
  "sourceProjectId": "uuid-sandbox",
  "targetProjectId": "uuid-production",
  "resources": [
    {
      "type": "CampaignTemplate",
      "id": "uuid"
    },
    {
      "type": "MetadataSchema",
      "id": "uuid"
    }
  ]
}
```

Response:

```json
{
  "compatible": true,
  "creates": 2,
  "reuses": 1,
  "conflicts": [],
  "missingDependencies": [],
  "unsupported": [],
  "secretWarnings": [
    "Webhook credentials are never copied"
  ]
}
```

---

# 11. Contratos de contexto

## 11.1 Usuário humano

O portal envia:

```http
Authorization: Bearer <jwt>
X-Project-Id: <project-id>
```

O backend:

1. obtém `OrganizationId` do principal;
2. valida `ProjectId` na mesma organização;
3. valida membership e acesso;
4. valida status da organização e projeto;
5. cria `CurrentProjectContext`.

## 11.2 API key

O projeto é obtido exclusivamente da API key persistida.

Ignorar ou rejeitar tentativa de sobrescrever por header.

## 11.3 Jobs

Todo job deve carregar:

```text
OrganizationId
ProjectId
CorrelationId
TriggeredBy
```

Jobs sem contexto de tenant devem falhar.

---

# 12. Fluxos principais

## 12.1 Criar projeto

1. Usuário acessa Organization > Projects.
2. Informa nome, ambiente, moeda, fuso, locale e país.
3. Backend valida permissão e quota.
4. Backend normaliza slug.
5. Backend cria Project e ProjectSettings na mesma transação.
6. Owner/Admin recebe acesso automático.
7. Backend registra `project.created`.
8. Portal oferece configurar marca e integrações.

## 12.2 Trocar projeto

1. Usuário abre project switcher.
2. Portal lista somente projetos acessíveis e ativos.
3. Usuário seleciona projeto.
4. Portal limpa queries/cache do projeto anterior.
5. Portal persiste seleção local.
6. Novas requests enviam `X-Project-Id`.
7. Header e telas exibem ambiente e moeda.

## 12.3 Arquivar projeto

1. Usuário solicita arquivamento.
2. Sistema mostra impacto: campanhas, keys, webhooks e jobs.
3. Production exige confirmação pelo nome.
4. Backend bloqueia novas operações.
5. Jobs operacionais são pausados.
6. API keys deixam de autenticar.
7. Histórico permanece disponível.
8. Auditoria registra before/after.

## 12.4 Promover Sandbox para Production

1. Usuário seleciona recursos.
2. Backend cria plano sem alterar destino.
3. Usuário resolve conflitos.
4. Usuário confirma projeto Production.
5. Job assíncrono copia definições em ordem de dependência.
6. Secrets são marcados para configuração manual.
7. Resultados e mappings são persistidos.
8. Usuário recebe notificação.

## 12.5 Criar marca

1. Project Admin abre Brand.
2. Informa dados visuais, legais e de contato.
3. URLs e cores são validadas.
4. Preview é exibido.
5. Backend salva e registra auditoria.

## 12.6 Criar localização

1. Usuário informa nome.
2. Desenha círculo ou polígono.
3. Frontend valida forma básica.
4. Backend valida novamente.
5. Localização é salva.
6. Pode ser usada em regras após publicação.

---

# 13. Frontend

## 13.1 Project switcher

Deve exibir:

- nome;
- badge de ambiente;
- moeda;
- status;
- pesquisa;
- opção de administrar projetos quando autorizado.

Production deve possuir destaque visual inequívoco.

## 13.2 Lista de projetos

Colunas:

```text
Name
Environment
Currency
Time zone
Status
Members
Campaigns
Last activity
```

Filtros:

```text
Environment
Status
Country
Search
```

## 13.3 Criação/edição

Wizard:

1. identificação;
2. ambiente;
3. regionalização;
4. membros;
5. marca opcional;
6. resumo.

## 13.4 Project overview

Cards:

- campanhas ativas;
- vouchers;
- validações;
- resgates;
- chamadas de API;
- falhas recentes;
- webhooks com erro;
- quota.

## 13.5 Brand settings

Formulário com preview:

- nome e logo;
- cores;
- website;
- termos e privacidade;
- remetente;
- endereço.

## 13.6 Stores and areas

- tabela de lojas;
- árvore de áreas;
- associação drag-and-drop opcional;
- importação CSV futura;
- estado ativo/inativo.

## 13.7 Locations

- lista e busca;
- editor em mapa;
- circle/polygon;
- preview;
- indicação de regras que usam a localização.

## 13.8 Promotion center

- origem e destino;
- seleção de recursos;
- diff;
- conflitos;
- confirmação;
- progresso;
- resultado por recurso.

---

# 14. Auditoria

Eventos mínimos:

```text
project.created
project.updated
project.primary_changed
project.disabled
project.enabled
project.archived
project.restored
project.deletion_requested
project.deleted
project.environment_changed
project.currency_changed
project.settings_updated
brand.created
brand.updated
brand.deleted
store.created
store.updated
store.disabled
store.enabled
store.deleted
area.created
area.updated
area.deleted
area.stores_changed
location.created
location.updated
location.deleted
project_promotion.planned
project_promotion.started
project_promotion.completed
project_promotion.failed
```

Metadata:

```text
organizationId
projectId
actorId
action
entityType
entityId
before
after
sourceProjectId
targetProjectId
ipAddress
userAgent
correlationId
createdAt
```

Secrets não entram em before/after.

---

# 15. Eventos de domínio

```text
ProjectCreated
ProjectUpdated
ProjectDisabled
ProjectEnabled
ProjectArchived
ProjectRestored
PrimaryProjectChanged
BrandUpdated
StoreCreated
StoreUpdated
AreaMembershipChanged
GeoLocationUpdated
ProjectPromotionRequested
ProjectPromotionCompleted
ProjectPromotionFailed
```

Eventos devem ser gravados via outbox na mesma transação da alteração.

---

# 16. Jobs

## 16.1 ProjectPromotionWorker

Responsável por:

- obter lease;
- validar origem e destino;
- executar grafo de dependências;
- persistir mappings;
- permitir retry;
- produzir relatório final.

## 16.2 ProjectDeletionWorker

Somente para exclusão física autorizada.

Deve:

- respeitar retenção;
- validar ausência de dependências;
- produzir evidência;
- ser irreversível apenas após confirmação.

## 16.3 ProjectUsageAggregator

Calcula resumo e quotas sem executar agregações pesadas a cada carregamento do portal.

---

# 17. Erros padronizados

| Código | HTTP | Descrição |
|---|---:|---|
| PROJECT_NOT_FOUND | 404 | Projeto inexistente ou fora do tenant |
| PROJECT_ACCESS_DENIED | 403 | Usuário sem acesso |
| PROJECT_DISABLED | 403 | Projeto desativado |
| PROJECT_ARCHIVED | 409 | Projeto arquivado |
| PROJECT_SLUG_EXISTS | 409 | Slug já utilizado |
| PROJECT_LIMIT_EXCEEDED | 403 | Quota de projetos atingida |
| LAST_ACTIVE_PROJECT_REQUIRED | 409 | Organização ficaria sem projeto |
| PROJECT_ENVIRONMENT_IMMUTABLE | 409 | Ambiente não pode ser alterado |
| PROJECT_CURRENCY_IMMUTABLE | 409 | Moeda não pode ser alterada |
| PRODUCTION_CONFIRMATION_REQUIRED | 400 | Confirmação reforçada ausente |
| INVALID_CURRENCY | 400 | Código de moeda inválido |
| INVALID_TIMEZONE | 400 | Fuso IANA inválido |
| INVALID_LOCALE | 400 | Locale inválido |
| BRAND_ALREADY_EXISTS | 409 | Projeto já possui marca |
| INVALID_BRAND_URL | 400 | URL de marca inválida |
| STORE_CODE_EXISTS | 409 | Código de loja duplicado |
| AREA_CYCLE_DETECTED | 409 | Hierarquia produziria ciclo |
| LOCATION_INVALID_SHAPE | 400 | Geometria inválida |
| RESOURCE_IN_USE | 409 | Recurso não pode ser removido |
| PROMOTION_CONFLICT | 409 | Conflito de promoção |
| PROMOTION_DEPENDENCY_MISSING | 409 | Dependência ausente |
| PROMOTION_ALREADY_RUNNING | 409 | Job equivalente em execução |

Erros multi-tenant devem preferir `404` quando revelar existência for risco.

---

# 18. Segurança

## 18.1 Isolamento

- todas as queries filtram `AccountId` e `ProjectId`;
- IDs nunca são autorizados apenas por existência;
- filtros e services recebem contexto;
- testes tentam acesso cruzado.

## 18.2 Produção

- ações críticas possuem permissão própria;
- confirmação exige nome exato;
- auditoria obrigatória;
- API keys de Sandbox não funcionam em Production.

## 18.3 URLs e conteúdo

- validar scheme e tamanho;
- não buscar URL arbitrária pelo backend;
- evitar SSRF;
- sanitizar conteúdo exibido.

## 18.4 Geolocalização

- coletar somente quando necessário;
- registrar finalidade;
- aplicar retenção;
- não armazenar localização corrente automaticamente.

---

# 19. Observabilidade

Toda telemetria deve incluir:

```text
accountId
projectId
environment
userId ou apiKeyId
correlationId
operation
duration
result
```

Métricas:

```text
projects_active_total
project_context_resolution_duration_ms
project_access_denied_total
project_promotion_jobs_total
project_promotion_duration_ms
project_promotion_failures_total
project_usage_aggregation_duration_ms
```

Alertas:

- falhas repetidas de promoção;
- job preso;
- projeto Production sem owner/admin;
- divergência de quota;
- API key ativa em projeto arquivado.

---

# 20. Requisitos não funcionais

## 20.1 Performance

- resolução de contexto p95 menor que 20 ms sem I/O remoto adicional;
- lista de projetos p95 menor que 300 ms;
- cache de membership permitido com invalidação;
- listagens paginadas;
- shapes possuem limite de tamanho e pontos.

## 20.2 Disponibilidade

- indisponibilidade de analytics não bloqueia troca de projeto;
- cache indisponível deve usar banco;
- promoção assíncrona pode ser retomada;
- alteração crítica não depende de email.

## 20.3 Consistência

- criação de projeto e settings é transacional;
- alteração de projeto principal é atômica;
- arquivamento bloqueia novas operações no mesmo commit lógico;
- mappings de promoção são persistidos.

## 20.4 Compatibilidade

- campos novos devem ser opcionais durante rollout;
- valores atuais `"Production"` devem migrar para enum;
- endpoints existentes de `/api/projects` devem permanecer compatíveis;
- mudanças de contrato exigem versionamento.

---

# 21. Configuração

```text
Projects__DefaultCurrency=BRL
Projects__DefaultTimeZone=America/Sao_Paulo
Projects__DefaultLocale=pt-BR
Projects__DefaultCountry=BR
Projects__MaxAreaDepth=5
Projects__MaxGeoJsonBytes=262144
Projects__ProductionConfirmation=true
ProjectPromotion__BatchSize=100
ProjectPromotion__LeaseSeconds=60
ProjectPromotion__MaxRetries=5
```

---

# 22. Critérios de aceite

## CA-001 — Criar projeto Sandbox

```gherkin
Dado que sou OrganizationOwner
E minha organização possui quota disponível
Quando crio um projeto Sandbox em BRL
Então o projeto deve ser criado ativo
E deve possuir settings padrão
E deve aparecer no project switcher
E deve registrar project.created
```

## CA-002 — Quota de projetos

```gherkin
Dado que a organização atingiu MaxProjects
Quando tento criar outro projeto
Então o sistema deve retornar PROJECT_LIMIT_EXCEEDED
E nenhum registro parcial deve ser criado
```

## CA-003 — Isolamento

```gherkin
Dado que tenho acesso ao projeto A
E não tenho acesso ao projeto B
Quando tento consultar B por ID ou header
Então o sistema deve negar sem expor seus dados
```

## CA-004 — API key vinculada

```gherkin
Dado que uma API key pertence ao projeto A
Quando envio X-Project-Id do projeto B
Então a requisição deve continuar vinculada a A ou ser rejeitada
E nunca deve operar em B
```

## CA-005 — Arquivar único projeto

```gherkin
Dado que a organização possui apenas um projeto ativo
Quando tento arquivá-lo
Então o sistema deve retornar LAST_ACTIVE_PROJECT_REQUIRED
```

## CA-006 — Projeto arquivado

```gherkin
Dado que um projeto foi arquivado
Quando uma API key desse projeto chama uma operação
Então o sistema deve retornar PROJECT_ARCHIVED ou falhar autenticação
E nenhuma alteração deve ocorrer
```

## CA-007 — Moeda imutável

```gherkin
Dado que um projeto possui transações monetárias
Quando tento trocar BRL por USD
Então o sistema deve retornar PROJECT_CURRENCY_IMMUTABLE
```

## CA-008 — Troca de projeto

```gherkin
Dado que tenho acesso a dois projetos
Quando seleciono o segundo projeto
Então o portal deve recarregar contexto e dados
E nenhuma informação do primeiro projeto deve permanecer na tela
```

## CA-009 — Marca

```gherkin
Dado que um projeto não possui marca
Quando salvo uma marca válida
Então ela deve ficar disponível para templates de comunicação
E brand.created deve ser auditado
```

## CA-010 — Área cíclica

```gherkin
Dado que a área Sul é ancestral da área SP
Quando tento definir SP como pai de Sul
Então o sistema deve retornar AREA_CYCLE_DETECTED
```

## CA-011 — Localização inválida

```gherkin
Dado que desenho um polígono não fechado
Quando tento salvar
Então o sistema deve retornar LOCATION_INVALID_SHAPE
```

## CA-012 — Planejar promoção

```gherkin
Dado um template em Sandbox
Quando planejo promoção para Production
Então o sistema deve listar criações, reusos, conflitos e dependências
E não deve modificar Production
```

## CA-013 — Executar promoção

```gherkin
Dado um plano compatível confirmado
Quando executo a promoção
Então as definições devem ser copiadas em ordem
E secrets não devem ser copiados
E mappings e auditoria devem ser registrados
```

## CA-014 — Retry idempotente

```gherkin
Dado um job de promoção concluído
Quando a mesma idempotency key é reenviada
Então nenhum recurso deve ser duplicado
E o resultado anterior deve ser retornado
```

---

# 23. Testes obrigatórios

## 23.1 Unitários

- normalização e colisão de slug;
- validação ISO 4217;
- validação IANA timezone;
- validação BCP 47;
- transições de status;
- último projeto ativo;
- mutabilidade de ambiente;
- mutabilidade de moeda;
- ciclo e profundidade de área;
- circle, polygon e multipolygon;
- plano de promoção;
- resolução de conflitos;
- sanitização de marca.

## 23.2 Integração

- criar projeto com settings;
- quota concorrente;
- listar somente projetos acessíveis;
- projeto de outra organização retorna 404;
- project header humano;
- API key não troca projeto;
- arquivar e bloquear operações;
- restaurar;
- trocar projeto principal;
- CRUD de marca;
- CRUD de loja e área;
- geolocation JSONB;
- promoção assíncrona idempotente;
- outbox e audit log.

## 23.3 Concorrência

- duas criações com mesmo slug;
- duas criações no último slot de quota;
- troca concorrente de projeto principal;
- arquivar enquanto ocorre operação;
- dois workers no mesmo promotion job.

## 23.4 Frontend

- project switcher;
- badge Production;
- limpeza de cache ao trocar;
- permissões;
- estados loading/empty/error;
- confirmação de arquivamento;
- preview de marca;
- mapa de localização;
- diff de promoção.

## 23.5 Segurança

- cross-tenant por path;
- cross-project por header;
- role sem permissão;
- projeto disabled/archived;
- SSRF em URLs;
- XSS em marca;
- GeoJSON excessivo;
- secret em plano ou log.

---

# 24. Ordem de implementação

## Iteração 1 — Hardening do Project atual

- enum de ambiente;
- locale, país, descrição e `IsPrimary`;
- validações de moeda/fuso/locale;
- GET por ID;
- status e transições;
- permissões granulares;
- testes.

## Iteração 2 — Contexto e isolamento

- `CurrentProjectContext`;
- validação uniforme;
- bloqueio por status;
- cache tenant-aware;
- testes cross-tenant e API key.

## Iteração 3 — Portal de projetos

- lista;
- criação e edição;
- project switcher;
- resumo;
- badges;
- confirmação Production.

## Iteração 4 — Marca

- BrandProfile e endereço;
- APIs;
- tela e preview;
- auditoria;
- testes de URL e sanitização.

## Iteração 5 — Lojas e áreas

- entidades e hierarquia;
- CRUD;
- associação;
- permissões;
- tela;
- testes de ciclo.

## Iteração 6 — Localizações

- GeoLocation;
- validação GeoJSON;
- APIs;
- mapa;
- vínculo com loja;
- testes espaciais básicos.

## Iteração 7 — Promoção entre ambientes

- plano;
- compatibilidade;
- job e mappings;
- worker;
- idempotência;
- diff e progresso.

## Iteração 8 — Hardening e operação

- métricas;
- quotas;
- performance;
- E2E;
- documentação;
- deploy DEV/HML.

Não avançar para a próxima interação sem código, testes, build, documentação,
evidência e roadmap da interação atual.

---

# 25. Definition of Done

R002 será concluído quando:

- projetos possuírem ciclo de vida completo;
- isolamento tiver testes automatizados;
- Sandbox e Production forem visualmente distintos;
- moeda, fuso, locale e país forem validados;
- acesso por projeto for aplicado uniformemente;
- projeto principal estiver protegido;
- marca estiver funcional;
- lojas e áreas estiverem funcionais;
- localizações estiverem funcionais;
- promoção de configurações for segura e idempotente;
- auditoria e outbox cobrirem ações críticas;
- portal possuir todas as telas;
- migrations estiverem validadas;
- build e testes passarem;
- deploy DEV/HML estiver validado;
- documentação e evidências estiverem atualizadas.

---

# 26. Riscos e decisões técnicas

## 26.1 Troca de moeda após transações

Risco de corromper interpretação de valores históricos.

Decisão: bloquear após primeiro recurso monetário/transação.

## 26.2 Confundir ambiente com deployment

`EnvironmentType` é contexto de dados do produto. Não é
`ASPNETCORE_ENVIRONMENT`.

Um deployment DEV pode acessar projetos Sandbox e Production conforme configuração
e autorização, embora isso não seja recomendado operacionalmente.

## 26.3 Clonar projeto inteiro

Cópia indiscriminada pode vazar clientes, secrets e transações.

Decisão: promover apenas definições selecionadas, com plano e mapping.

## 26.4 Geodados

JSONB é suficiente para armazenar e validar shapes inicialmente. Consultas espaciais
complexas podem exigir PostGIS.

Decisão: adoção de PostGIS exige ADR e migration própria.

## 26.5 Cache após troca de projeto

Dados do projeto anterior podem aparecer ou ser enviados incorretamente.

Decisão: project ID deve fazer parte de toda query key e cache key.

## 26.6 Arquivamento durante operação

Uma operação iniciada antes do arquivamento pode concluir depois.

Decisão: operações críticas revalidam status dentro da transação.

---

# 27. Situação atual da implementação

## 27.1 Já existe

- entidade `Project`;
- `AccountId`, nome e slug;
- ambiente como string;
- moeda e fuso;
- status Active/Disabled/Archived;
- quota básica;
- criação, listagem, atualização e soft delete;
- filtro por membership;
- `X-Project-Id` para usuário humano;
- API key vinculada ao projeto;
- project switcher carregando API;
- auditoria básica;
- índice único `AccountId + Slug`.

## 27.2 Gaps para R002

- ambiente tipado e política de imutabilidade;
- projeto principal explícito;
- descrição, locale e país;
- GET por ID, disable, enable, archive e restore explícitos;
- validações de moeda e fuso;
- bloqueio uniforme por status;
- permissões R002 completas;
- métricas por projeto;
- BrandProfile;
- Store e Area;
- GeoLocation;
- promoção de configurações;
- outbox de eventos;
- telas administrativas completas;
- testes de isolamento e concorrência específicos.

---

# 28. Referências públicas analisadas

- [Voucherify — Key concepts](https://docs.voucherify.io/get-started/key-concepts)
- [Voucherify — Integration blueprint](https://docs.voucherify.io/get-started/integration-overview)
- [Voucherify — Create project API](https://docs.voucherify.io/api-reference/management/create-project)
- [Voucherify — Multi-brand management and internationalization](https://docs.voucherify.io/docs/brand-management)
- [Voucherify — Create brand API](https://docs.voucherify.io/api-reference/management/create-brand)
- [Voucherify — Campaign templates](https://docs.voucherify.io/build/campaign-templates)
- [Voucherify — Copy campaign template](https://docs.voucherify.io/api-reference/management/copy-campaign-template-to-a-project)
- [Voucherify — Location object](https://docs.voucherify.io/api-reference/locations/location-object)
- [Voucherify — List locations](https://docs.voucherify.io/api-reference/locations/list-locations)
- [Voucherify — Geofencing](https://docs.voucherify.io/orchestrate/geofencing)

---

# 29. Checklist de entrega

```text
[x] Project model endurecido
[x] Environment enum implementado
[x] Projeto principal implementado
[x] Moeda validada
[x] Fuso IANA validado
[x] Locale e país validados
[x] Ciclo de vida implementado
[x] Isolamento cross-project testado
[x] Project switcher completo
[x] Tela de projetos completa
[x] BrandProfile implementada
[x] Tela de marca implementada
[x] Stores implementadas
[x] Areas implementadas
[x] Localizações implementadas
[x] Promoção entre ambientes implementada
[x] Idempotência de promoção testada
[x] Auditoria implementada
[x] Outbox implementado
[x] Métricas implementadas
[x] Testes unitários criados
[x] Testes de integração criados
[x] Testes E2E criados
[x] Migration validada
[x] Deploy DEV validado
[ ] Deploy HML validado — ambiente HML não disponibilizado
[x] Documentação atualizada
```

---

# 30. Status de implementação

## Interação 1 — Hardening do Project atual — concluída em 2026-07-02

Entregue:

- `ProjectEnvironment` tipado e persistido como string compatível;
- descrição, locale, país, projeto principal e timestamps de ciclo de vida;
- validação de ambiente, moeda ISO 4217, fuso IANA, locale e país;
- proteção de alteração de ambiente e moeda após entrada em uso;
- máquina de transições e proteção do último projeto ativo/principal;
- `GET /api/projects/{projectId}`;
- endpoints explícitos para disable, enable, archive, restore e make-primary;
- permissões granulares e seeds atualizados;
- migration `AddR002ProjectHardening` com backfill determinístico;
- testes unitários e integração ampliados;
- auditoria das novas operações.

Validação:

```text
Build Release: aprovado, 0 warnings e 0 erros
Testes: 164 aprovados
Migration idempotente: script gerado
NuGet: nenhuma vulnerabilidade conhecida
```

Pendente para a Interação 2:

- `CurrentProjectContext`;
- bloqueio uniforme de escrita por status;
- cache tenant-aware;
- testes dedicados de isolamento cross-project e API key.

## Interação 2 — Contexto e isolamento — concluída em 2026-07-02

Entregue:

- `CurrentProjectContext` com organização, projeto, ambiente, status e regionalização;
- resolução centralizada e validada para usuários humanos e API keys;
- `X-Project-Id` permitido somente para usuário humano autorizado;
- override de projeto ignorado para API key;
- projeto cross-tenant tratado como não encontrado;
- API keys bloqueadas em projetos Disabled, Archived ou PendingDeletion;
- projetos inativos em modo somente leitura para usuários humanos;
- exceção administrativa para reativação e gestão do projeto;
- contexto em cache Redis com chave `accountId + projectId`;
- fallback para PostgreSQL quando Redis estiver indisponível;
- invalidação do contexto após alterações do projeto;
- cache de permissões segregado por organização, projeto e usuário;
- cache de campanha segregado por organização e projeto;
- revogação/rotação de API key invalidando o cache público de autenticação;
- testes unitários e integração ampliados.

Validação:

```text
Build Release: aprovado, 0 warnings e 0 erros
Testes: 174 aprovados
NuGet: nenhuma vulnerabilidade conhecida
```

Pendente para a Interação 3:

- lista e formulário completos de projetos;
- project switcher enriquecido com ambiente e status;
- overview e badges;
- confirmação visual para Production.

## Interação 3 — Portal de projetos — concluída em 2026-07-02

Entregue:

- rota protegida `/settings/projects`;
- visão geral com total, ativos, Production e contexto atual;
- lista responsiva com ambiente, status, projeto principal e regionalização;
- criação e edição com validações nativas do formulário;
- confirmação explícita ao criar ou selecionar um projeto Production;
- permissão `projects.manage_production` reforçada no portal e no backend;
- ações de enable, disable, archive, restore e make-primary conforme permissão;
- project switcher enriquecido com pesquisa, ambiente, moeda, status e projeto principal;
- projetos inativos indisponíveis para seleção;
- faixa visual persistente enquanto o contexto ativo for Production;
- atualização do contexto após toda mutação;
- acesso pelo menu lateral, busca rápida e configurações da organização.
- moeda, fuso, locale, país e ambiente selecionados por catálogos legíveis,
  sem digitação de códigos técnicos.

Validação:

```text
Frontend build: aprovado
Frontend lint: aprovado
Build backend Release: aprovado, 0 warnings e 0 erros
Testes backend: 174 aprovados
```

Pendente para a Interação 4:

- BrandProfile e endereço;
- APIs de marca;
- tela, preview e sanitização;
- auditoria e testes de URL.

## Interação 4 — Marca — concluída em 2026-07-02

Entregue:

- `BrandProfile` único por projeto e `BrandAddress`;
- CRUD em `/api/projects/{projectId}/brand`;
- isolamento por organização/projeto e permissões `brands.*`;
- validação de HTTPS público, e-mail, cor hexadecimal e tamanhos;
- rejeição de hosts locais/IPs e conteúdo HTML;
- auditoria `brand.created`, `brand.updated` e `brand.deleted`;
- tela de identidade, comunicação, links legais e endereço;
- país selecionado por catálogo e preview visual em tempo real;
- migration `AddR002BrandProfile`;
- testes unitários de URL, sanitização e cor.

Validação: backend sem warnings, 184 testes aprovados, frontend build/lint aprovados.

Pendente para a Interação 5: lojas, áreas, hierarquia, CRUD, permissões e tela.

## Interação 5 — Lojas e áreas — concluída em 2026-07-02

Entregue:

- `Store`, `Area` e associação `AreaStore`;
- tipos físicos e digitais e ciclo ativo/inativo/arquivado;
- código único e estável por projeto;
- CRUD, filtros e paginação por cursor para lojas;
- árvore de áreas com pai no mesmo projeto, limite de profundidade e detecção de ciclo;
- associação validada de lojas por área;
- isolamento por organização/projeto e permissões `stores.*`/`areas.*`;
- auditoria de todas as mutações;
- portal com tabela, árvore, listas orientadas e códigos gerados automaticamente;
- migration `AddR002StoresAreas`;
- testes de código, ciclo direto/indireto e profundidade.

Validação: backend sem warnings, 192 testes aprovados, frontend build/lint aprovados.

Pendente para a Interação 6: GeoLocation, GeoJSON, APIs, mapa, vínculo e testes espaciais.

## Interação 6 — Localizações — concluída em 2026-07-02

Entregue:

- `GeoLocation` multi-tenant em JSONB, sem dependência de PostGIS;
- Circle, Polygon e MultiPolygon;
- validação de latitude, longitude, raio, unidade, anéis fechados e limites;
- CRUD, filtros, endpoint de validação e permissões `locations.*`;
- vínculo opcional e validado entre loja e localização do mesmo projeto;
- bloqueio de remoção enquanto houver loja vinculada;
- auditoria `location.created`, `location.updated` e `location.deleted`;
- editor estruturado sem digitação de JSON ou IDs;
- preview SVG local, fechamento automático de anéis e lista no formulário de loja;
- migration `AddR002GeoLocations`;
- testes espaciais básicos para formas válidas e inválidas.

Validação: backend sem warnings, 201 testes aprovados, frontend build/lint aprovados.

Pendente para a Interação 7: planejamento, compatibilidade, job, mappings, worker, idempotência, diff e progresso.

## Interação 7 — Promoção entre ambientes — concluída em 2026-07-02

Entregue:

- plano somente leitura com criações, reusos, conflitos e não suportados;
- promoção segura de BrandProfile e MetadataSchema;
- CampaignTemplate explicitamente não suportado enquanto não possuir modelo próprio;
- confirmação nominal para destino Production;
- job assíncrono com worker, status, cancelamento e resultado;
- idempotency key única por organização;
- mappings entre recursos de origem e destino;
- secrets e dados transacionais nunca copiados;
- isolamento de acesso aos dois projetos, auditoria e permissão `projects.promote`;
- portal com listas de projetos/recursos, diff e progresso;
- migration `AddR002ProjectPromotions`;
- testes de confirmação, idempotency key e origem/destino distintos.

Validação: backend sem warnings, 207 testes aprovados, frontend build/lint aprovados.

Pendente para a Interação 8: métricas, quotas, performance, E2E, operação e deploy DEV/HML.

## Interação 8 — Hardening e operação — concluída em código e DEV em 2026-07-03

Entregue:

- endpoints de summary e usage por projeto;
- cache tenant-aware de summary por 30 segundos e consultas `AsNoTracking`;
- apresentação de campanhas, vouchers, validações, resgates e falhas no portal;
- quotas de projetos e campanhas/API expostas para operação;
- métricas de contexto, acesso, promoção e agregação via `System.Diagnostics.Metrics`;
- probes separadas `/health/live` e `/health/ready`;
- eventos outbox sanitizados para mutações R002 na mesma persistência da auditoria;
- E2E de onboarding, isolamento, summary/usage, marca e promoção com mapping;
- runbook de probes, métricas, diagnóstico, smoke test e rollback;
- auditoria NuGet, build backend e frontend e validação do ambiente DEV.

Validação:

```text
Backend Release: 0 warnings e 0 erros
Testes: 207 aprovados
Frontend build/lint: aprovados
NuGet: nenhuma vulnerabilidade conhecida
DEV API/portal: saudáveis
Docker Compose local: não executado; Docker não está instalado neste host
HML: pendente; nenhum endpoint ou credencial HML está configurado no repositório
```

A implementação funcional da R002 está concluída. O encerramento operacional integral
permanece condicionado ao smoke test em HML.
