# R002 — Runbook de Operação

> **Escopo:** Projetos, Ambientes, Marcas (BrandProfile), Lojas (Store), Áreas (Area), Georreferenciamento (GeoLocation), Métricas e Sumários

---

## Sumário

1. [Health Probes](#1-health-probes)
2. [Migrações](#2-migrações)
3. [Endpoints de Diagnóstico](#3-endpoints-de-diagnóstico)
4. [Smoke Test](#4-smoke-test)
5. [Rollback](#5-rollback)

---

## 1. Health Probes

### Endpoint básico de health check

```http
GET /api/health
```

**Resposta esperada (200):**
```json
{
  "status": "healthy",
  "timestamp": "2026-07-07T12:00:00Z"
}
```

### Liveness / Readiness (Kubernetes)

Se configurado em ambiente Kubernetes, usar os seguintes probes no deployment:

```yaml
livenessProbe:
  httpGet:
    path: /api/health
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 30

readinessProbe:
  httpGet:
    path: /api/health
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 15
```

### Probes dependentes de banco

Para verificar conectividade com PostgreSQL, pode-se estender o endpoint `/api/health` para incluir um check de banco:

```csharp
// Exemplo — adicionar ao Program.cs
app.MapGet("/api/health/ready", async (VoucherSystemDbContext db) =>
{
    try
    {
        await db.Database.CanConnectAsync();
        return Results.Ok(new { status = "ready", database = "connected" });
    }
    catch (Exception ex)
    {
        return Results.StatusCode(503);
    }
});
```

---

## 2. Migrações

### Aplicar migrações pendentes

```bash
cd /home/ubuntu/voucher-system/src/VoucherSystem.Infrastructure

# Usando dotnet ef (Entity Framework Core CLI)
dotnet ef database update \
  --startup-project ../VoucherSystem.Api/VoucherSystem.Api.csproj
```

Ou via runtime (já configurado em `Program.cs`):

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VoucherSystemDbContext>();
    await db.Database.MigrateAsync();
}
```

A aplicação executa `MigrateAsync()` automaticamente na inicialização.

### Criar nova migração

```bash
dotnet ef migrations add NomeDaMigracao \
  --startup-project ../VoucherSystem.Api/VoucherSystem.Api.csproj
```

### Verificar migrações pendentes

```bash
dotnet ef migrations list \
  --startup-project ../VoucherSystem.Api/VoucherSystem.Api.csproj
```

---

## 3. Endpoints de Diagnóstico

### Sumário do projeto

```http
GET /api/projects/{projectId}/summary
Authorization: Bearer <token>
X-Project-Id: <projectId>  # (opcional)
```

**Resposta:**
```json
{
  "activeCampaigns": 1,
  "totalVouchers": 0,
  "totalValidations": 0,
  "totalRedemptions": 0,
  "totalFailed": 0
}
```

> **Nota:** `activeCampaigns` conta o número de `BrandProfile` existentes no projeto. Os demais campos (vouchers, validações, resgates) são campos reservados para entidades futuras e retornam 0 no MVP.

**Permissão necessária:** `usage.read`

### Uso do projeto

```http
GET /api/projects/{projectId}/usage
Authorization: Bearer <token>
X-Project-Id: <projectId>  # (opcional)
```

**Resposta:**
```json
{
  "activeProjects": 1,
  "totalProjects": 1,
  "maxProjects": 1
}
```

**Interpretação dos campos:**
| Campo | Descrição |
|---|---|
| `activeProjects` | Projetos com status `"Active"` na mesma organização |
| `totalProjects` | Total de projetos (qualquer status) na organização |
| `maxProjects` | Limite máximo definido no plano da organização |

**Permissão necessária:** `usage.read`

### Listar projetos

```http
GET /api/projects/
Authorization: Bearer <token>
```

### Obter dados de ProjetoPromotionJob

> Útil para diagnosticar jobs de promoção travados ou com erro.

```http
GET /api/projects/{projectId}/promotions
Authorization: Bearer <token>
X-Project-Id: <projectId>
```

---

## 4. Smoke Test

Script de verificação rápida pós-deploy. Executar contra ambiente de staging ou sandbox.

```bash
#!/usr/bin/env bash
# smoke-test-r002.sh — Verificação R002
# Uso: ./smoke-test-r002.sh <base_url> <token>

BASE="${1:-http://localhost:5000}"
TOKEN="${2:-}"

if [ -z "$TOKEN" ]; then
  echo "Uso: $0 <base_url> <token>"
  exit 1
fi

AUTH="Authorization: Bearer $TOKEN"
PASS=0
FAIL=0

check() {
  local desc="$1"
  local expected="$2"
  local actual="$3"
  if [ "$expected" = "$actual" ]; then
    echo "  ✅ $desc"
    PASS=$((PASS + 1))
  else
    echo "  ❌ $desc (esperado: $expected, obtido: $actual)"
    FAIL=$((FAIL + 1))
  fi
}

echo "============================================"
echo " Smoke Test R002 — $(date -u +'%Y-%m-%dT%H:%M:%SZ')"
echo "============================================"

# 1. Health check
echo ""
echo "[1/9] Health check"
HEALTH=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/health")
check "GET /api/health" "200" "$HEALTH"

# 2. Listar projetos — espera 200
echo ""
echo "[2/9] Listar projetos"
PROJECTS=$(curl -s -H "$AUTH" "$BASE/api/projects/")
PROJECTS_STATUS=$(echo "$PROJECTS" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('totalCount',0))" 2>/dev/null || echo "0")
echo "  ℹ️  Total de projetos: $PROJECTS_STATUS"

# 3. Pegar ID do primeiro projeto ativo
echo ""
echo "[3/9] Obter primeiro projeto"
PROJECT_ID=$(echo "$PROJECTS" | python3 -c "
import sys,json
d=json.load(sys.stdin)
items=[i for i in d.get('items',[]) if i.get('status')=='Active']
if items: print(items[0]['id'])
" 2>/dev/null || echo "")

if [ -z "$PROJECT_ID" ]; then
  echo "  ⚠️  Nenhum projeto ativo encontrado, pulando testes específicos"
else
  echo "  ℹ️  Project ID: $PROJECT_ID"

  # 4. Summary
  echo ""
  echo "[4/9] GET /api/projects/\$PROJECT_ID/summary"
  SUMMARY=$(curl -s -H "$AUTH" "$BASE/api/projects/$PROJECT_ID/summary")
  SUMMARY_CODE=$(curl -s -o /dev/null -w "%{http_code}" -H "$AUTH" "$BASE/api/projects/$PROJECT_ID/summary")
  check "GET summary" "200" "$SUMMARY_CODE"

  # 5. Usage
  echo ""
  echo "[5/9] GET /api/projects/\$PROJECT_ID/usage"
  USAGE_CODE=$(curl -s -o /dev/null -w "%{http_code}" -H "$AUTH" "$BASE/api/projects/$PROJECT_ID/usage")
  check "GET usage" "200" "$USAGE_CODE"

  # 6. Buscar BrandProfile
  echo ""
  echo "[6/9] GET /api/projects/\$PROJECT_ID/brand"
  BRAND_CODE=$(curl -s -o /dev/null -w "%{http_code}" -H "$AUTH" "$BASE/api/projects/$PROJECT_ID/brand")
  # 200 se existe, 404 se não existe — ambos aceitáveis
  echo "  ℹ️  Brand status: $BRAND_CODE"

  # 7. Listar Stores
  echo ""
  echo "[7/9] GET /api/projects/\$PROJECT_ID/stores"
  STORES_CODE=$(curl -s -o /dev/null -w "%{http_code}" -H "$AUTH" "$BASE/api/projects/$PROJECT_ID/stores")
  check "GET stores" "200" "$STORES_CODE"

  # 8. Listar áreas
  echo ""
  echo "[8/9] GET /api/projects/\$PROJECT_ID/areas"
  AREAS_CODE=$(curl -s -o /dev/null -w "%{http_code}" -H "$AUTH" "$BASE/api/projects/$PROJECT_ID/areas")
  check "GET areas" "200" "$AREAS_CODE"

  # 9. GeoLocations
  echo ""
  echo "[9/9] GET /api/projects/\$PROJECT_ID/geo-locations"
  GEO_CODE=$(curl -s -o /dev/null -w "%{http_code}" -H "$AUTH" "$BASE/api/projects/$PROJECT_ID/geo-locations")
  # 200 ou 404 — ambos aceitáveis
  echo "  ℹ️  GeoLocations status: $GEO_CODE"
fi

echo ""
echo "============================================"
echo " Resultado: $PASS passed, $FAIL failed"
echo "============================================"
exit $FAIL
```

---

## 5. Rollback

### Reverter a última migração

```bash
cd /home/ubuntu/voucher-system/src/VoucherSystem.Infrastructure

# Reverter um passo
dotnet ef database update NomeDaMigracaoAnterior \
  --startup-project ../VoucherSystem.Api/VoucherSystem.Api.csproj
```

Exemplo — reverter a migração `AddR002BrandProfileStoresAreasPromotions`:

```bash
dotnet ef database update 20260707203242_AddR002BrandProfileStoresAreasPromotions \
  --startup-project ../VoucherSystem.Api/VoucherSystem.Api.csproj
```

### Remover a última migração (se ainda não aplicada)

```bash
dotnet ef migrations remove \
  --startup-project ../VoucherSystem.Api/VoucherSystem.Api.csproj
```

### Rollback completo de R002

Caso seja necessário reverter **todas** as migrações introduzidas em R002 (mantendo as de R001):

| Ordem | Nome da Migração | Comando |
|---|---|---|
| 5 | `20260707203245_AddR002BrandProfile` | `dotnet ef database update 20260707203242_AddR002BrandProfileStoresAreasPromotions --startup-project ../VoucherSystem.Api/VoucherSystem.Api.csproj` |
| 4 | `20260707203242_AddR002BrandProfileStoresAreasPromotions` | `dotnet ef database update 20260707203219_AddR002StoresAreas --startup-project ../VoucherSystem.Api/VoucherSystem.Api.csproj` |
| 3 | `20260707203219_AddR002StoresAreas` | `dotnet ef database update 20260707202849_AddR002GeoLocations --startup-project ../VoucherSystem.Api/VoucherSystem.Api.csproj` |
| 2 | `20260707202849_AddR002GeoLocations` | `dotnet ef database update 20260707190915_AddIt5It6It7 --startup-project ../VoucherSystem.Api/VoucherSystem.Api.csproj` |

### Pós-rollback

Após reverter as migrações:

1. Remover os arquivos de migração indesejados do diretório `Migrations/`
2. Remover os endpoints `MetricsEndpoints.cs` se necessário
3. Remover `app.MapMetricsEndpoints()` de `Program.cs`
4. Recompilar e reimplantar

---

## Tabela de endpoints R002

| Método | Rota | Permissão | Descrição |
|---|---|---|---|
| `GET` | `/api/projects` | — | Listar projetos da organização |
| `GET` | `/api/projects/{projectId}` | `projects.read` | Obter projeto por ID |
| `POST` | `/api/projects` | `projects.create` | Criar projeto |
| `PATCH` | `/api/projects/{projectId}` | `projects.update` | Atualizar projeto |
| `POST` | `/api/projects/{projectId}/disable` | `projects.update` | Desativar projeto |
| `POST` | `/api/projects/{projectId}/enable` | `projects.update` | Reativar projeto |
| `POST` | `/api/projects/{projectId}/archive` | `projects.update` | Arquivar projeto |
| `POST` | `/api/projects/{projectId}/restore` | `projects.update` | Restaurar projeto |
| `POST` | `/api/projects/{projectId}/make-primary` | `projects.update` | Definir como primário |
| `GET` | `/api/projects/{projectId}/promotions/plan` | `projects.promote` | Plano de promoção |
| `GET` | `/api/projects/{projectId}/promotions` | `projects.promote` | Listar jobs de promoção |
| `POST` | `/api/projects/{projectId}/promotions` | `projects.promote` | Criar job de promoção |
| `GET` | `/api/projects/{projectId}/promotions/{jobId}` | `projects.promote` | Status do job |
| `POST` | `/api/projects/{projectId}/promotions/{jobId}/cancel` | `projects.promote` | Cancelar job |
| `GET` | `/api/projects/{projectId}/brand` | `brands.read` | Obter brand profile |
| `POST` | `/api/projects/{projectId}/brand` | `brands.create` | Criar brand profile |
| `PUT` | `/api/projects/{projectId}/brand` | `brands.update` | Atualizar brand profile |
| `DELETE` | `/api/projects/{projectId}/brand` | `brands.delete` | Deletar brand profile |
| `GET` | `/api/projects/{projectId}/stores` | `stores.read` | Listar stores |
| `POST` | `/api/projects/{projectId}/stores` | `stores.create` | Criar store |
| `GET` | `/api/projects/{projectId}/stores/{storeId}` | `stores.read` | Obter store |
| `PUT` | `/api/projects/{projectId}/stores/{storeId}` | `stores.update` | Atualizar store |
| `DELETE` | `/api/projects/{projectId}/stores/{storeId}` | `stores.delete` | Deletar store (soft) |
| `GET` | `/api/projects/{projectId}/areas` | `areas.read` | Listar áreas (árvore) |
| `POST` | `/api/projects/{projectId}/areas` | `areas.create` | Criar área |
| `GET` | `/api/projects/{projectId}/areas/{areaId}` | `areas.read` | Obter área |
| `PUT` | `/api/projects/{projectId}/areas/{areaId}` | `areas.update` | Atualizar área |
| `DELETE` | `/api/projects/{projectId}/areas/{areaId}` | `areas.delete` | Deletar área (soft + cascata) |
| `POST` | `/api/projects/{projectId}/areas/{areaId}/stores` | `areas.update` | Associar stores |
| `DELETE` | `/api/projects/{projectId}/areas/{areaId}/stores/{storeId}` | `areas.update` | Desassociar store |
| `GET` | `/api/projects/{projectId}/geo-locations` | `geolocations.read` | Listar geo-locations |
| `POST` | `/api/projects/{projectId}/geo-locations` | `geolocations.create` | Criar geo-location |
| `PUT` | `/api/projects/{projectId}/geo-locations/{geoId}` | `geolocations.update` | Atualizar geo-location |
| `DELETE` | `/api/projects/{projectId}/geo-locations/{geoId}` | `geolocations.delete` | Deletar geo-location |
| **`GET`** | **`/api/projects/{projectId}/summary`** | **`usage.read`** | **Sumário do projeto** |
| **`GET`** | **`/api/projects/{projectId}/usage`** | **`usage.read`** | **Uso do plano** |

---

## Troubleshooting

### Problema: Endpoint retorna 403 ao chamar /summary ou /usage

**Causa:** O papel (role) do usuário não inclui a permissão `usage.read`.

**Solução:**
1. Verificar as permissões do papel: `GET /api/roles/{roleId}`
2. Adicionar `usage.read` ao papel via `PUT /api/roles/{roleId}/permissions`

### Problema: Dados de sumário inconsistentes

**Causa:** O `projectId` não pertence à organização do usuário autenticado.

**Solução:** Verificar o `OrganizationId` no token JWT e comparar com o `OrganizationId` do projeto.

### Problema: Migração falha ao aplicar

**Solução:**
1. Verificar conectividade com PostgreSQL
2. Verificar se a string de conexão está correta em `appsettings.json`
3. Executar `dotnet ef database update --verbose` para logs detalhados

---

## Referências

- [R002 - Requisitos](../../docs/requisitos/R002%20-%20PROJETOS-AMBIENTES-MARCAS-LOCALIZACOES.md)
- [Código dos Endpoints](../../src/VoucherSystem.Api/Endpoints/)
- [Modelos de Domínio](../../src/VoucherSystem.Domain/)
- [Contratos de API](../../src/VoucherSystem.Contracts/)
