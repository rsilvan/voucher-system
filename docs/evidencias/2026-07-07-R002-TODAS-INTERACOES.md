# Evidência — R002: Projetos, Ambientes, Marcas e Localizações

> **Data:** 2026-07-07
> **Macro-requisito:** R002 — Projetos, Ambientes, Marcas, Lojas, Áreas e Localizações
> **Iterações:** 1 a 8 (completas)
> **Status:** Implementação funcional concluída

---

## Resumo

Implementação completa do R002 conforme especificação em `docs/requisitos/R002 - PROJETOS-AMBIENTES-MARCAS-LOCALIZACOES.md`.
Foram entregues 8 iterações cobrindo hardening de projetos, contexto/isolamento, portal frontend, marca, lojas, áreas, localizações geográficas, promoção entre ambientes e hardening operacional.

---

## Iteração 1 — Hardening do Project

- `ProjectEnvironment` enum (Sandbox, Development, Staging, Production)
- `ProjectStatus` enum (Active, Disabled, Archived, PendingDeletion)
- Project.cs atualizado com: Description, Locale, Country, IsPrimary, IsInUse, timestamps de ciclo de vida
- Validações de ambiente, moeda ISO 4217, fuso IANA, locale BCP 47, país ISO 3166-1 alpha-2
- Proteção de alteração de ambiente/moeda após entrada em uso (IsInUse)
- Máquina de transições: disable, enable, archive, restore, make-primary, delete
- Proteção do último projeto ativo/principal (LAST_ACTIVE_PROJECT_REQUIRED)
- Permissões: projects.create, projects.read, projects.update, projects.delete, projects.manage_production, projects.promote
- Seeds atualizados com novas permissões nos roles Owner, Admin e ReadOnly
- Endpoints: GET /api/projects, GET /api/projects/{id}, POST, PATCH, POST disable/enable/archive/restore/make-primary
- Auditoria de todas as operações
- Migration: AddR002ProjectHardening

## Iteração 2 — Contexto e isolamento

- `CurrentProjectContext` com organização, projeto, ambiente, status, moeda, fuso, locale, país
- `ProjectContextMiddleware` — resolve contexto do projeto por requisição
- Suporte a X-Project-Id header para usuários humanos
- Bloqueio de escrita em projetos Disabled (HTTP 423) e Archived (HTTP 423)
- Extração de claims de projeto do JWT no UserContextMiddleware

## Iteração 3 — Portal de projetos (Frontend)

- Rota protegida `/dashboard/projects`
- Sidebar atualizada com link "Projetos"
- Overview com cards de total, ativos, produção, arquivados
- Lista responsiva com badges de ambiente (Sandbox/Dev/Staging/Production) e status (Ativo/Desativado/Arquivado)
- Modal de criação com seletores de ambiente, moeda, fuso, locale, país
- Modal de edição com mesmas validações
- Ações: desativar, reativar, arquivar, restaurar, definir como principal
- Frontend build: ✓ (309KB JS + 23KB CSS)

## Iteração 4 — Marca

- `BrandProfile` entidade com Name, Description, WebsiteUrl, TermsUrl, PrivacyUrl, SupportEmail, LogoUrl, PrimaryColor, SecondaryColor
- `BrandAddress` value object (Street, City, State, ZipCode, Country)
- CRUD: GET/POST/PUT/DELETE `/api/projects/{projectId}/brand`
- Validação de URL HTTPS, rejeição de localhost/IPs, cores hex, tamanhos
- Auditoria: brand.created/brand.updated/brand.deleted
- Permissões: brands.*
- Migration: AddR002BrandProfileStoresAreasPromotions

## Iteração 5 — Lojas e áreas

- `Store` entidade com Code (único por projeto), Name, Description, StoreType (Physical/Digital), endereço completo, contato, status
- `Area` entidade com hierarquia (ParentAreaId), Depth, proteção de ciclos
- `AreaStore` associação N:N
- CRUD de lojas e áreas com paginação por cursor e árvore de áreas
- Auditoria de todas as mutações
- Permissões: stores.* e areas.*
- Testes de código único, ciclo direto/indireto e profundidade

## Iteração 6 — Localizações

- `GeoLocation` entidade multi-tenant em coluna text (JSON), sem dependência de PostGIS
- Suporte a Circle (lat/lng/radius/unit), Polygon e MultiPolygon (GeoJSON)
- Validação de latitude (-90/90), longitude (-180/180), raio positivo, anéis fechados, sem geometria vazia
- CRUD e endpoint de validação POST `/api/projects/{projectId}/locations/validate`
- Bloqueio de remoção enquanto houver loja vinculada
- Permissões: locations.*
- Migration: AddR002GeoLocations

## Iteração 7 — Promoção entre ambientes

- `ProjectPromotionJob` entidade com status (Pending/Planning/Running/Completed/Failed/Cancelled)
- Plano somente leitura (GET /api/.../promotions/plan?targetProjectId=)
- Criação de job com idempotency key
- Promoção segura de BrandProfile
- Confirmação implícita para destino Production
- Isolamento de acesso, auditoria e permissão `projects.promote`
- Endpoints: list, create, get, cancel

## Iteração 8 — Hardening e operação

- Pipeline de middlewares: GlobalException → RequestLogging → UserContext → ProjectContext
- CurrentProjectContext com IsReadOnly para projetos não-ativos
- Probes: GET /api/health
- Auditoria completa de todas as mutações R002

---

## Validação

```
Backend build:    0 warnings, 0 erros
Testes:           95 passed (35 R001 + 60 R002)
Frontend build:   ✓ (309KB JS + 23KB CSS)
Migration:        AddR002GeoLocations + AddR002BrandProfileStoresAreasPromotions
NuGet:            0 vulnerabilidades conhecidas
```

## Arquivos criados/modificados (R002)

### Domain (7 arquivos)
- `ProjectEnvironment.cs`, `ProjectStatus.cs`, `Project.cs` (alterado)
- `PromotionJobStatus.cs`, `ProjectPromotionJob.cs`
- `BrandProfile.cs`, `BrandAddress.cs`
- `Store.cs`, `Area.cs`, `AreaStore.cs`
- `GeoLocation.cs`

### Contracts (3 arquivos)
- `ProjectContracts.cs`, `BrandContracts.cs`, `PromotionContracts.cs`
- `StoreContracts.cs`, `AreaContracts.cs`, `GeoLocationContracts.cs`

### Application (12+ interfaces + services)
- `IProjectService.cs`, `ProjectService.cs`
- `IBrandService.cs`, `BrandService.cs`
- `IStoreService.cs`, `StoreService.cs`
- `IAreaService.cs`, `AreaService.cs`
- `IGeoLocationService.cs`, `GeoLocationService.cs`
- `IPromotionService.cs`, `PromotionService.cs`
- `IProjectRepository.cs` + 5 novos repositórios interface

### Infrastructure
- `ProjectRepository.cs`, `BrandRepository.cs`, `StoreRepository.cs`, `AreaRepository.cs`, `GeoLocationRepository.cs`, `PromotionRepository.cs`
- `VoucherSystemDbContext.cs` (alterado — 6 novas DbSets + mappings)
- `SeedData.cs` (alterado — 16 novas permissões)
- Migrations: `AddR002GeoLocations`, `AddR002BrandProfileStoresAreasPromotions`

### API (5 endpoint files)
- `ProjectEndpoints.cs`, `BrandEndpoints.cs`, `StoreEndpoints.cs`, `AreaEndpoints.cs`, `GeoLocationEndpoints.cs`, `PromotionEndpoints.cs`
- `Program.cs` (alterado)
- `Middleware/ProjectContextMiddleware.cs`, `Middleware/UserContextMiddleware.cs` (alterado)

### Frontend
- `pages/Projects.tsx`, `App.tsx` (alterado), `Dashboard.tsx` (alterado), `lib/types.ts` (alterado)

### Testes
- 60 novos testes unitários distribuídos entre as camadas R002

## Pendências

- [ ] HML: deploy smoke test (ambiente não configurado)
- [ ] Docker Compose local (Docker não instalado no host)
