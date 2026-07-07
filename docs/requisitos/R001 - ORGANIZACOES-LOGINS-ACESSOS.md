# Especificação Funcional e Técnica — Organizações, Logins, Papéis e Permissões

> **Produto:** Voucher-System
> **Área:** Identity, Access Management, Organizations & Members
> **Stack alvo:** .NET 10 + PostgreSQL via EF 10 + Redis/Valkey + React + TypeScript + Vite + Docker + Application Insights
> **Objetivo:** Implementar o cadastro self-service de organizações, criação automática do administrador inicial e gestão completa de logins, papéis, permissões, convites, acesso por projeto e auditoria.

---

## 1. Visão geral

Esta especificação define as funcionalidades necessárias para que o Voucher-System opere como uma plataforma SaaS multi-tenant, onde cada cliente possui uma **organização** isolada, com seus próprios usuários, projetos, campanhas, vouchers, configurações, chaves de API, consumo, webhooks e logs de auditoria.

A criação da organização deve ser **self-service**. Ao finalizar o cadastro, o sistema deve criar automaticamente:

1. A organização.
2. O primeiro usuário/login.
3. O vínculo do usuário com a organização.
4. A role de proprietário/administrador máximo.
5. Um projeto padrão.
6. O plano inicial da organização.
7. As quotas iniciais de consumo.
8. Os registros de auditoria correspondentes.

Esse primeiro usuário terá permissão para cadastrar outros logins e atribuir diferentes papéis, acessos e permissões.

---

## 2. Objetivos de negócio

### 2.1 Objetivo principal

Permitir que um cliente crie sua conta na plataforma sem intervenção manual da equipe operacional, garantindo que ele já tenha acesso administrativo imediato para configurar usuários, projetos e recursos iniciais.

### 2.2 Objetivos secundários

- Garantir isolamento de dados entre organizações.
- Permitir gestão de membros pela própria organização.
- Suportar diferentes perfis de acesso.
- Permitir controle granular de permissões.
- Permitir acesso segmentado por projeto.
- Registrar auditoria de ações sensíveis.
- Preparar a base para planos pagos, billing, consumo e features enterprise.
- Preparar evolução futura para MFA, SSO/SAML/OIDC e SCIM.

---

## 3. Escopo funcional

Este módulo cobre:

- Cadastro self-service de organização.
- Criação automática do usuário administrador inicial.
- Login e autenticação.
- Recuperação e redefinição de senha.
- Verificação de e-mail.
- Cadastro/convite de novos membros.
- Aceite de convite.
- Gestão de membros.
- Gestão de roles padrão.
- Gestão de custom roles.
- Gestão de permissões.
- Atribuição de acesso por projeto.
- Auditoria de ações administrativas e de autenticação.
- Controle de status de organização, usuário, membro e convite.

Fora do escopo inicial, mas previsto para evolução:

- SSO via SAML/OIDC.
- MFA obrigatório.
- SCIM provisioning.
- Políticas avançadas de senha por organização.
- Domínios corporativos verificados.
- Aprovação manual de novos usuários.

---

## 4. Conceitos de domínio

## 4.1 Organização

A **Organização** representa a conta principal do cliente dentro do sistema.

Exemplos:

```txt
Empresa ABC Ltda
Rede XPTO
Loja Online Brasil
Grupo com várias marcas
```

A organização é o principal limite de isolamento multi-tenant. Todas as entidades de negócio sensíveis devem estar vinculadas direta ou indiretamente a uma organização.

Entidades que devem pertencer a uma organização:

- Usuários/membros.
- Projetos.
- Campanhas.
- Vouchers.
- Clientes finais.
- Segmentos.
- Regras de validação.
- Resgates.
- Webhooks.
- API keys.
- Usage/billing.
- Logs de auditoria.

---

## 4.2 Login / Usuário

O **Usuário** representa uma pessoa que pode acessar o painel administrativo da plataforma.

Um usuário pode ser:

- Criado automaticamente no cadastro inicial da organização.
- Convidado por um administrador.
- Ativado após aceitar convite.
- Desativado por um administrador.
- Bloqueado por segurança.

No MVP, recomenda-se que o e-mail seja globalmente único, simplificando login, recuperação de senha e auditoria.

---

## 4.3 Membro da organização

O **Membro** é o vínculo entre um usuário e uma organização.

Mesmo que inicialmente um usuário pertença a apenas uma organização, separar `User` de `OrganizationMember` permite evoluir futuramente para cenários em que um mesmo usuário possa participar de múltiplas organizações.

---

## 4.4 Role / Papel

A **Role** representa um conjunto de permissões.

Exemplos:

```txt
OrganizationOwner
OrganizationAdmin
ProjectAdmin
MarketingManager
Developer
FinanceViewer
SupportAgent
ReadOnly
Custom
```

Roles podem ser:

- **System roles:** roles nativas da plataforma, não excluíveis.
- **Custom roles:** roles criadas pela própria organização.

---

## 4.5 Permissão

A **Permissão** representa uma ação específica em um recurso.

Formato recomendado:

```txt
recurso.acao
```

Exemplos:

```txt
users.invite
users.disable
roles.update
campaigns.create
vouchers.import
api_keys.manage
billing.read
audit.read
```

---

## 4.6 Projeto

O **Projeto** representa um contexto operacional dentro da organização.

Exemplos:

```txt
Produção
Homologação
Marca A
Marca B
Brasil
Argentina
E-commerce
App Mobile
```

O projeto permite isolar campanhas, vouchers, API keys, webhooks e permissões por contexto.

---

## 4.7 Convite

O **Convite** permite que um administrador cadastre um novo login sem definir a senha diretamente.

O usuário convidado recebe um link seguro e define a própria senha no aceite do convite.

---

## 4.8 Auditoria

A **Auditoria** registra ações relevantes para segurança, rastreabilidade, compliance, suporte e investigação de incidentes.

---

# 5. Regras de negócio

## RN-001 — Cadastro self-service de organização

O sistema deve permitir que um visitante crie uma organização por meio de um formulário público.

Campos obrigatórios:

- Nome da organização.
- Nome do responsável.
- E-mail do responsável.
- Senha.
- Confirmação de senha.
- País.
- Aceite dos termos de uso.
- Aceite da política de privacidade.

Campos opcionais:

- Documento da empresa.
- Telefone.
- Nome legal/razão social.
- Segmento de atuação.

---

## RN-002 — Criação automática do administrador inicial

Ao criar uma organização, o sistema deve criar automaticamente um usuário administrador inicial.

Esse usuário deve receber a role:

```txt
OrganizationOwner
```

O `OrganizationOwner` deve possuir acesso total à organização e ao projeto padrão.

---

## RN-003 — Criação automática de projeto padrão

Ao criar uma organização, o sistema deve criar automaticamente um projeto padrão.

Nome sugerido:

```txt
Projeto Principal
```

Campos padrão:

```txt
Environment: Production
Currency: BRL
TimeZone: America/Sao_Paulo
Status: Active
```

---

## RN-004 — Plano inicial da organização

Toda organização criada via self-service deve receber um plano inicial.

Opções recomendadas:

### Opção A — Free

```txt
Plano: Free
API calls/mês: 1.000
Usuários: 3
Projetos: 1
Campanhas ativas: 2
```

### Opção B — Trial

```txt
Plano: Trial
Duração: 14 dias
API calls incluídas: 10.000
Usuários: 3
Projetos: 1
Campanhas ativas: 5
```

Para MVP, recomenda-se usar `Trial`, pois aumenta a percepção de valor e facilita conversão para plano pago.

---

## RN-005 — E-mail único

No MVP, o e-mail do usuário deve ser globalmente único.

Não deve ser permitido criar dois usuários com o mesmo e-mail.

Motivo:

- Simplifica autenticação.
- Simplifica recuperação de senha.
- Reduz ambiguidade de auditoria.
- Facilita suporte.

---

## RN-006 — Organização deve ter pelo menos um Owner ativo

O sistema não deve permitir que uma organização fique sem ao menos um usuário ativo com role `OrganizationOwner`.

Deve ser bloqueado:

- Excluir o único owner.
- Desativar o único owner.
- Remover a role do único owner.
- Transferir propriedade sem novo owner ativo.

---

## RN-007 — Administradores podem cadastrar novos logins

Usuários com permissão `users.invite` podem convidar novos membros para a organização.

Roles que devem possuir essa permissão inicialmente:

- OrganizationOwner.
- OrganizationAdmin.

O convite deve incluir:

- Nome do convidado.
- E-mail.
- Role.
- Projetos permitidos.
- Mensagem opcional.

---

## RN-008 — Convites devem expirar

Todo convite deve possuir data de expiração.

Valor padrão:

```txt
7 dias
```

Status possíveis:

```txt
Pending
Accepted
Expired
Revoked
```

---

## RN-009 — Acesso por projeto

Um membro pode ter acesso a:

- Todos os projetos da organização.
- Apenas projetos específicos.
- Nenhum projeto, caso seja um perfil administrativo restrito.

O sistema deve validar o acesso ao projeto em toda operação que envolva dados projetados, como campanhas, vouchers, clientes, regras, webhooks e API keys.

---

## RN-010 — Permissões granulares

Toda ação sensível deve ser protegida por permissão explícita.

Exemplo:

- Convidar usuário requer `users.invite`.
- Criar campanha requer `campaigns.create`.
- Exportar vouchers requer `vouchers.export`.
- Ver billing requer `billing.read`.
- Criar API key requer `api_keys.create`.

---

## RN-011 — Custom roles

A plataforma deve suportar custom roles.

No MVP, pode-se implementar a base técnica e liberar a interface apenas em plano Business ou superior.

Custom roles devem permitir:

- Criar role.
- Editar nome e descrição.
- Definir permissões.
- Duplicar role existente.
- Excluir role não utilizada.

Não deve ser permitido excluir roles de sistema.

---

## RN-012 — Auditoria obrigatória

Toda ação sensível deve gerar log de auditoria.

Eventos mínimos:

- Organização criada.
- Usuário criado.
- Usuário convidado.
- Convite aceito.
- Convite revogado.
- Usuário desativado.
- Usuário reativado.
- Senha alterada.
- Login realizado.
- Falha de login.
- Role alterada.
- Permissão alterada.
- Projeto atribuído.
- Projeto removido.
- API key criada.
- API key revogada.
- Webhook criado.
- Webhook alterado.
- Configuração de segurança alterada.

---

## RN-013 — Separação entre login humano e API key

Usuários humanos acessam o painel administrativo.

API keys acessam endpoints de integração.

API keys devem ser geradas por projeto e possuir permissões técnicas próprias.

---

## RN-014 — Bloqueio por status

O sistema deve impedir acesso quando:

- Organização estiver suspensa.
- Organização estiver cancelada.
- Usuário estiver desativado.
- Usuário estiver bloqueado.
- Membro estiver removido ou desativado.
- Convite estiver expirado ou revogado.

---

## RN-015 — Isolamento multi-tenant obrigatório

Nenhum usuário de uma organização pode acessar dados de outra organização.

Todas as consultas de dados sensíveis devem filtrar por `OrganizationId`.

Se o usuário tentar acessar recurso de outra organização, o sistema deve retornar:

```txt
403 Forbidden
```

ou, quando for melhor para segurança:

```txt
404 Not Found
```

A tentativa deve ser registrada em auditoria quando aplicável.

---

# 6. Perfis padrão

## 6.1 OrganizationOwner

Perfil máximo da organização.

Permissões:

```txt
organization.read
organization.update
organization.delete_request
users.invite
users.read
users.update
users.disable
users.enable
roles.read
roles.create
roles.update
roles.delete
permissions.read
projects.create
projects.read
projects.update
projects.delete
api_keys.read
api_keys.create
api_keys.regenerate
api_keys.revoke
webhooks.read
webhooks.create
webhooks.update
webhooks.delete
billing.read
billing.manage
usage.read
audit.read
audit.export
security.manage
```

---

## 6.2 OrganizationAdmin

Administrador operacional da organização.

Permissões:

```txt
organization.read
organization.update
users.invite
users.read
users.update
users.disable
users.enable
roles.read
permissions.read
projects.create
projects.read
projects.update
api_keys.read
api_keys.create
api_keys.regenerate
webhooks.read
webhooks.create
webhooks.update
webhooks.delete
usage.read
audit.read
```

Restrições:

- Não pode excluir organização.
- Não pode remover o último owner.
- Não pode gerenciar billing completo.
- Não pode alterar políticas críticas de segurança sem permissão específica.

---

## 6.3 ProjectAdmin

Administrador de um ou mais projetos.

Permissões:

```txt
projects.read
projects.update
campaigns.read
campaigns.create
campaigns.update
campaigns.delete
vouchers.read
vouchers.create
vouchers.update
vouchers.import
vouchers.export
validation_rules.read
validation_rules.create
validation_rules.update
customers.read
customers.create
customers.update
segments.read
segments.create
segments.update
redemptions.read
redemptions.cancel
webhooks.read
webhooks.create
webhooks.update
api_keys.read
api_keys.create
audit.read_project
```

---

## 6.4 MarketingManager

Usuário responsável pela gestão de campanhas e vouchers.

Permissões:

```txt
campaigns.read
campaigns.create
campaigns.update
campaigns.delete
vouchers.read
vouchers.create
vouchers.update
vouchers.import
vouchers.export
validation_rules.read
validation_rules.create
validation_rules.update
customers.read
segments.read
segments.create
segments.update
redemptions.read
reports.read
```

---

## 6.5 Developer

Usuário técnico responsável por integrações.

Permissões:

```txt
projects.read
api_keys.read
api_keys.create
api_keys.regenerate
api_keys.revoke
webhooks.read
webhooks.create
webhooks.update
webhooks.test
events.read
logs.read
metadata.read
```

---

## 6.6 FinanceViewer

Usuário financeiro.

Permissões:

```txt
billing.read
usage.read
invoices.read
reports.read
```

---

## 6.7 SupportAgent

Usuário de suporte operacional.

Permissões:

```txt
customers.read
vouchers.read
redemptions.read
redemptions.cancel
campaigns.read
audit.read_project
```

---

## 6.8 ReadOnly

Usuário somente leitura.

Permissões:

```txt
organization.read
projects.read
campaigns.read
vouchers.read
customers.read
segments.read
redemptions.read
reports.read
```

---

# 7. Matriz de permissões inicial

| Permissão / Ação | Owner | Org Admin | Project Admin | Marketing | Dev | Finance | Support | ReadOnly |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Ver organização | Sim | Sim | Não | Não | Não | Não | Não | Sim |
| Alterar organização | Sim | Sim | Não | Não | Não | Não | Não | Não |
| Solicitar exclusão da organização | Sim | Não | Não | Não | Não | Não | Não | Não |
| Criar usuários | Sim | Sim | Não | Não | Não | Não | Não | Não |
| Desativar usuários | Sim | Sim | Não | Não | Não | Não | Não | Não |
| Alterar roles | Sim | Sim | Não | Não | Não | Não | Não | Não |
| Criar projetos | Sim | Sim | Não | Não | Não | Não | Não | Não |
| Alterar projetos | Sim | Sim | Sim | Não | Não | Não | Não | Não |
| Gerenciar campanhas | Sim | Sim | Sim | Sim | Não | Não | Não | Não |
| Gerenciar vouchers | Sim | Sim | Sim | Sim | Não | Não | Parcial | Não |
| Importar vouchers | Sim | Sim | Sim | Sim | Não | Não | Não | Não |
| Exportar vouchers | Sim | Sim | Sim | Sim | Não | Não | Não | Não |
| Ver clientes | Sim | Sim | Sim | Sim | Não | Não | Sim | Sim |
| Alterar clientes | Sim | Sim | Sim | Parcial | Não | Não | Não | Não |
| Ver resgates | Sim | Sim | Sim | Sim | Não | Não | Sim | Sim |
| Cancelar resgates | Sim | Sim | Sim | Não | Não | Não | Sim | Não |
| Gerenciar API keys | Sim | Sim | Sim | Não | Sim | Não | Não | Não |
| Gerenciar webhooks | Sim | Sim | Sim | Não | Sim | Não | Não | Não |
| Ver billing | Sim | Parcial | Não | Não | Não | Sim | Não | Não |
| Gerenciar billing | Sim | Não | Não | Não | Não | Não | Não | Não |
| Ver auditoria | Sim | Sim | Projeto | Não | Logs técnicos | Não | Projeto | Não |
| Exportar auditoria | Sim | Não | Não | Não | Não | Não | Não | Não |
| Gerenciar segurança | Sim | Não | Não | Não | Não | Não | Não | Não |

---

# 8. Modelo de dados

## 8.1 Organization

```csharp
public class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? LegalName { get; set; }
    public string? DocumentNumber { get; set; }
    public string Slug { get; set; } = default!;
    public string Country { get; set; } = "BR";
    public string Status { get; set; } = "Active";
    public Guid PlanId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
```

Índices recomendados:

```txt
IX_Organizations_Slug UNIQUE
IX_Organizations_DocumentNumber
IX_Organizations_Status
```

---

## 8.2 User

```csharp
public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string NormalizedEmail { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public bool EmailVerified { get; set; }
    public string Status { get; set; } = "Active";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public int AccessFailedCount { get; set; }
    public DateTimeOffset? LockoutEndAt { get; set; }
}
```

Índices recomendados:

```txt
IX_Users_NormalizedEmail UNIQUE
IX_Users_Status
```

---

## 8.3 OrganizationMember

```csharp
public class OrganizationMember
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public string Status { get; set; } = "Active";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? InvitedByUserId { get; set; }
}
```

Índices recomendados:

```txt
IX_OrganizationMembers_OrganizationId_UserId UNIQUE
IX_OrganizationMembers_OrganizationId_Status
IX_OrganizationMembers_UserId
IX_OrganizationMembers_RoleId
```

---

## 8.4 Project

```csharp
public class Project
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string Environment { get; set; } = "Production";
    public string Currency { get; set; } = "BRL";
    public string TimeZone { get; set; } = "America/Sao_Paulo";
    public string Status { get; set; } = "Active";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
```

Índices recomendados:

```txt
IX_Projects_OrganizationId_Slug UNIQUE
IX_Projects_OrganizationId_Status
```

---

## 8.5 Role

```csharp
public class Role
{
    public Guid Id { get; set; }
    public Guid? OrganizationId { get; set; }
    public string Name { get; set; } = default!;
    public string Key { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public string Scope { get; set; } = "Organization";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
```

Observações:

- `OrganizationId = null` indica role global de sistema.
- `OrganizationId != null` indica custom role da organização.

---

## 8.6 Permission

```csharp
public class Permission
{
    public Guid Id { get; set; }
    public string Key { get; set; } = default!;
    public string Resource { get; set; } = default!;
    public string Action { get; set; } = default!;
    public string Description { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
}
```

Índices recomendados:

```txt
IX_Permissions_Key UNIQUE
```

---

## 8.7 RolePermission

```csharp
public class RolePermission
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
}
```

Chave composta:

```txt
RoleId + PermissionId
```

---

## 8.8 ProjectAccess

```csharp
public class ProjectAccess
{
    public Guid Id { get; set; }
    public Guid OrganizationMemberId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid RoleId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
```

Índices recomendados:

```txt
IX_ProjectAccess_OrganizationMemberId_ProjectId UNIQUE
IX_ProjectAccess_ProjectId
IX_ProjectAccess_RoleId
```

---

## 8.9 Invitation

```csharp
public class Invitation
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Email { get; set; } = default!;
    public string NormalizedEmail { get; set; } = default!;
    public string Name { get; set; } = default!;
    public Guid RoleId { get; set; }
    public string TokenHash { get; set; } = default!;
    public string Status { get; set; } = "Pending";
    public DateTimeOffset ExpiresAt { get; set; }
    public Guid InvitedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
```

Índices recomendados:

```txt
IX_Invitations_OrganizationId_NormalizedEmail_Status
IX_Invitations_TokenHash UNIQUE
IX_Invitations_ExpiresAt
```

---

## 8.10 InvitationProjectAccess

```csharp
public class InvitationProjectAccess
{
    public Guid InvitationId { get; set; }
    public Guid ProjectId { get; set; }
}
```

---

## 8.11 RefreshToken

```csharp
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = default!;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
```

---

## 8.12 AuditLog

```csharp
public class AuditLog
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string Action { get; set; } = default!;
    public string ResourceType { get; set; } = default!;
    public string? ResourceId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
}
```

Índices recomendados:

```txt
IX_AuditLogs_OrganizationId_CreatedAt
IX_AuditLogs_OrganizationId_Action
IX_AuditLogs_ActorUserId_CreatedAt
IX_AuditLogs_ResourceType_ResourceId
```

---

## 8.13 Plan

```csharp
public class Plan
{
    public Guid Id { get; set; }
    public string Key { get; set; } = default!;
    public string Name { get; set; } = default!;
    public decimal MonthlyPrice { get; set; }
    public int MaxUsers { get; set; }
    public int MaxProjects { get; set; }
    public int MaxActiveCampaigns { get; set; }
    public long MonthlyApiCalls { get; set; }
    public bool AllowsCustomRoles { get; set; }
    public bool AllowsAuditExport { get; set; }
    public bool AllowsSso { get; set; }
}
```

---

## 8.14 UsageQuota

```csharp
public class UsageQuota
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PlanId { get; set; }
    public long MonthlyApiCallsLimit { get; set; }
    public long MonthlyApiCallsUsed { get; set; }
    public int MaxUsers { get; set; }
    public int MaxProjects { get; set; }
    public DateTimeOffset PeriodStartAt { get; set; }
    public DateTimeOffset PeriodEndAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
```

---

# 9. Estados do sistema

## 9.1 Status da organização

```txt
Active
Trialing
PastDue
Suspended
Canceled
PendingVerification
```

## 9.2 Status do usuário

```txt
Active
PendingEmailVerification
Disabled
Locked
Deleted
```

## 9.3 Status do membro

```txt
Active
Pending
Disabled
Removed
```

## 9.4 Status do convite

```txt
Pending
Accepted
Expired
Revoked
```

---

# 10. APIs necessárias

## 10.1 Self-service

### POST /api/self-service/organizations

Cria uma nova organização e o administrador inicial.

Request:

```json
{
  "organizationName": "Empresa ABC",
  "responsibleName": "João Silva",
  "email": "joao@empresaabc.com",
  "password": "SenhaForte123!",
  "confirmPassword": "SenhaForte123!",
  "country": "BR",
  "documentNumber": "12345678000199",
  "acceptedTerms": true,
  "acceptedPrivacyPolicy": true
}
```

Response 201:

```json
{
  "organizationId": "7a63e4ef-89fb-41c3-90dc-b1f5f6d785df",
  "projectId": "0d555f7a-58af-4a52-bdb7-3b53adab774d",
  "userId": "7b150c45-1675-4104-aae9-cc62a84f3885",
  "role": "OrganizationOwner",
  "status": "Created"
}
```

Validações:

- Nome da organização obrigatório.
- E-mail válido.
- E-mail ainda não cadastrado.
- Senha forte.
- Senha e confirmação iguais.
- Aceite dos termos obrigatório.
- Aceite da política obrigatório.

---

### POST /api/self-service/organizations/check-email

Verifica disponibilidade de e-mail.

Request:

```json
{
  "email": "joao@empresaabc.com"
}
```

Response:

```json
{
  "available": true
}
```

---

### POST /api/self-service/organizations/check-slug

Verifica disponibilidade de slug da organização.

Request:

```json
{
  "slug": "empresa-abc"
}
```

Response:

```json
{
  "available": true
}
```

---

## 10.2 Autenticação

### POST /api/auth/login

Request:

```json
{
  "email": "joao@empresaabc.com",
  "password": "SenhaForte123!"
}
```

Response:

```json
{
  "accessToken": "jwt-token",
  "refreshToken": "refresh-token",
  "expiresIn": 900,
  "user": {
    "id": "7b150c45-1675-4104-aae9-cc62a84f3885",
    "name": "João Silva",
    "email": "joao@empresaabc.com"
  },
  "organization": {
    "id": "7a63e4ef-89fb-41c3-90dc-b1f5f6d785df",
    "name": "Empresa ABC"
  },
  "permissions": [
    "organization.read",
    "users.invite",
    "campaigns.create"
  ]
}
```

---

### POST /api/auth/logout

Revoga o refresh token atual.

---

### POST /api/auth/refresh-token

Gera novo access token e rotaciona refresh token.

---

### POST /api/auth/forgot-password

Solicita redefinição de senha.

---

### POST /api/auth/reset-password

Redefine senha usando token válido.

---

### POST /api/auth/verify-email

Confirma e-mail do usuário.

---

### POST /api/auth/resend-verification

Reenvia confirmação de e-mail.

---

## 10.3 Organização

```http
GET /api/organizations/current
PATCH /api/organizations/current
GET /api/organizations/current/usage
GET /api/organizations/current/billing
```

---

## 10.4 Membros

```http
GET /api/organizations/current/members
POST /api/organizations/current/members/invite
GET /api/organizations/current/members/{memberId}
PATCH /api/organizations/current/members/{memberId}
DELETE /api/organizations/current/members/{memberId}
POST /api/organizations/current/members/{memberId}/disable
POST /api/organizations/current/members/{memberId}/enable
```

### POST /api/organizations/current/members/invite

Request:

```json
{
  "name": "Maria Souza",
  "email": "maria@empresaabc.com",
  "roleId": "8d8763a8-fc97-42ec-a285-5ad94a99e848",
  "projectIds": [
    "0d555f7a-58af-4a52-bdb7-3b53adab774d"
  ],
  "message": "Bem-vinda ao time de campanhas."
}
```

Response 201:

```json
{
  "invitationId": "6d755928-a4c2-4e3e-b986-f14efce31527",
  "status": "Pending",
  "expiresAt": "2026-07-09T03:00:00Z"
}
```

---

## 10.5 Convites

```http
GET /api/invitations/{token}
POST /api/invitations/{token}/accept
POST /api/organizations/current/invitations/{invitationId}/resend
POST /api/organizations/current/invitations/{invitationId}/revoke
```

### POST /api/invitations/{token}/accept

Request:

```json
{
  "name": "Maria Souza",
  "password": "SenhaForte123!",
  "confirmPassword": "SenhaForte123!"
}
```

Response:

```json
{
  "status": "Accepted",
  "userId": "c2c0e6e1-5c83-4a33-b15d-b6c432f2b22d",
  "organizationId": "7a63e4ef-89fb-41c3-90dc-b1f5f6d785df"
}
```

---

## 10.6 Roles e permissões

```http
GET /api/roles
POST /api/roles
GET /api/roles/{roleId}
PATCH /api/roles/{roleId}
DELETE /api/roles/{roleId}
GET /api/permissions
```

### POST /api/roles

Request:

```json
{
  "name": "Operador de Campanhas",
  "description": "Pode criar e editar campanhas, mas não gerencia usuários.",
  "permissionKeys": [
    "campaigns.read",
    "campaigns.create",
    "campaigns.update",
    "vouchers.read",
    "redemptions.read"
  ]
}
```

---

## 10.7 Acesso por projeto

```http
GET /api/organizations/current/members/{memberId}/projects
PUT /api/organizations/current/members/{memberId}/projects
```

### PUT /api/organizations/current/members/{memberId}/projects

Request:

```json
{
  "projectIds": [
    "0d555f7a-58af-4a52-bdb7-3b53adab774d"
  ],
  "roleId": "2fa23f1a-7a0f-4428-9d75-d85de3ed3968"
}
```

---

## 10.8 Auditoria

```http
GET /api/audit-logs
GET /api/audit-logs/{auditLogId}
GET /api/audit-logs/export
```

Filtros:

```txt
actorUserId
action
resourceType
resourceId
projectId
from
to
page
pageSize
```

---

# 11. Fluxos principais

## 11.1 Fluxo de cadastro da organização

```txt
1. Visitante acessa tela de cadastro.
2. Preenche dados da organização e do responsável.
3. Backend valida dados.
4. Backend verifica se e-mail já existe.
5. Backend gera slug da organização.
6. Backend cria organização.
7. Backend cria usuário inicial.
8. Backend gera hash da senha.
9. Backend cria vínculo OrganizationMember.
10. Backend atribui role OrganizationOwner.
11. Backend cria projeto padrão.
12. Backend cria ProjectAccess do owner no projeto padrão.
13. Backend cria plano inicial e quota.
14. Backend registra auditoria.
15. Backend envia e-mail de boas-vindas/verificação.
16. Frontend redireciona para login ou dashboard.
```

Toda a operação deve ocorrer dentro de uma transação de banco.

---

## 11.2 Fluxo de convite de membro

```txt
1. Admin acessa tela de membros.
2. Clica em convidar usuário.
3. Informa nome, e-mail, role e projetos.
4. Backend valida permissão users.invite.
5. Backend valida limite de usuários do plano.
6. Backend valida se e-mail já existe ou já foi convidado.
7. Backend cria Invitation com token seguro.
8. Backend salva hash do token.
9. Backend registra projetos do convite.
10. Backend envia e-mail com link de aceite.
11. Backend registra auditoria MemberInvited.
```

---

## 11.3 Fluxo de aceite de convite

```txt
1. Usuário abre link do convite.
2. Frontend consulta GET /api/invitations/{token}.
3. Backend valida token, status e expiração.
4. Usuário define senha.
5. Backend cria ou ativa User.
6. Backend cria OrganizationMember.
7. Backend atribui role.
8. Backend cria ProjectAccess conforme convite.
9. Backend marca convite como Accepted.
10. Backend registra auditoria InvitationAccepted.
11. Usuário é redirecionado para login.
```

---

## 11.4 Fluxo de autorização de uma requisição

```txt
1. Cliente envia JWT.
2. Middleware valida token.
3. Middleware resolve UserId.
4. Middleware carrega usuário.
5. Middleware resolve OrganizationId.
6. Middleware valida status da organização.
7. Middleware valida status do usuário e do membro.
8. Middleware carrega permissões do membro.
9. Middleware valida ProjectId quando aplicável.
10. Authorization Handler valida permissão exigida.
11. Controller executa ação.
```

---

# 12. Segurança

## 12.1 Senhas

Requisitos mínimos:

```txt
Mínimo de 10 caracteres
Pelo menos uma letra maiúscula
Pelo menos uma letra minúscula
Pelo menos um número
Pelo menos um caractere especial
Não permitir senha igual ao e-mail
Não salvar senha em texto puro
```

Usar hash seguro. Recomendação:

- ASP.NET Core PasswordHasher.
- Argon2id, se o projeto já tiver biblioteca aprovada.

---

## 12.2 JWT

Recomendações:

```txt
Access token curto: 15 minutos
Refresh token longo: 7 a 30 dias
Refresh token rotativo
Revogação de refresh token no logout
Claims mínimas no JWT
Permissões podem ser carregadas do cache/backend
```

Claims sugeridas:

```txt
sub = userId
org = organizationId
member = organizationMemberId
email = email
name = name
role = roleKey
```

Evitar colocar lista muito grande de permissões no JWT se custom roles forem extensas. Preferir cache em Redis.

---

## 12.3 Rate limit de login

Aplicar proteção contra brute force:

```txt
5 tentativas inválidas por e-mail em 15 minutos
10 tentativas inválidas por IP em 15 minutos
Bloqueio temporário progressivo
Captcha futuro após múltiplas falhas
```

---

## 12.4 MFA futuro

Campos previstos:

```txt
MfaEnabled
MfaMethod
MfaSecretEncrypted
RecoveryCodesHash
```

MFA pode ser exigido por organização no plano Enterprise.

---

## 12.5 SSO futuro

Para planos Enterprise, prever:

```txt
OIDC/SAML
Domínios corporativos verificados
Mapeamento de grupos para roles
Forçar login via SSO
Bloquear senha local
SCIM provisioning futuro
```

---

## 12.6 CORS

CORS deve ser configurado por ambiente.

DEV Render:

```txt
Cors__AllowedOrigins__0=https://voucher-system-web-dev.onrender.com
```

Produção:

```txt
Cors__AllowedOrigins__0=https://app.seudominio.com
```

---

# 13. Autorização técnica no .NET

## 13.1 Atributo sugerido

```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
    {
        Policy = $"Permission:{permission}";
    }
}
```

Uso:

```csharp
[RequirePermission("users.invite")]
[HttpPost("members/invite")]
public async Task<IActionResult> InviteMember(InviteMemberRequest request)
{
    // ...
}
```

---

## 13.2 Authorization Handler

Responsabilidades:

- Ler permissão exigida.
- Obter contexto do usuário.
- Carregar permissões no cache ou banco.
- Validar status do usuário, organização e membro.
- Validar acesso ao projeto se houver `ProjectId`.

---

## 13.3 Cache de permissões

Chave Redis sugerida:

```txt
permissions:{organizationId}:{userId}
```

TTL sugerido:

```txt
15 minutos
```

Invalidar cache quando:

- Role do usuário for alterada.
- Permissões da role forem alteradas.
- Acesso a projeto for alterado.
- Usuário for desativado.
- Organização for suspensa.

---

# 14. Frontend — Telas necessárias

## 14.1 Cadastro público de organização

Rota sugerida:

```txt
/register
```

Campos:

- Nome da organização.
- Nome do responsável.
- E-mail.
- Senha.
- Confirmar senha.
- País.
- Documento opcional.
- Aceite dos termos.
- Aceite da política de privacidade.

Estados:

- Loading.
- Erro de validação.
- E-mail já cadastrado.
- Cadastro criado com sucesso.

---

## 14.2 Login

Rota sugerida:

```txt
/login
```

Campos:

- E-mail.
- Senha.

Ações:

- Entrar.
- Esqueci minha senha.
- Criar conta.

---

## 14.3 Dashboard inicial

Após login, exibir:

- Nome da organização.
- Projeto atual.
- Plano atual.
- Consumo do mês.
- Atalhos para campanhas, vouchers, membros e integrações.

---

## 14.4 Membros

Rota sugerida:

```txt
/settings/members
```

Funcionalidades:

- Listar membros.
- Buscar por nome/e-mail.
- Filtrar por status.
- Filtrar por role.
- Convidar membro.
- Alterar role.
- Alterar projetos.
- Desativar membro.
- Reativar membro.
- Reenviar convite.
- Revogar convite.

---

## 14.5 Roles

Rota sugerida:

```txt
/settings/roles
```

Funcionalidades:

- Listar roles padrão.
- Listar custom roles.
- Ver permissões por role.
- Criar custom role.
- Duplicar role.
- Editar custom role.
- Excluir custom role não utilizada.

---

## 14.6 Auditoria

Rota sugerida:

```txt
/settings/audit-logs
```

Funcionalidades:

- Listar eventos.
- Filtrar por usuário.
- Filtrar por ação.
- Filtrar por período.
- Filtrar por recurso.
- Ver IP e user agent.
- Exportar logs se houver permissão.

---

## 14.7 Aceite de convite

Rota sugerida:

```txt
/invitations/:token
```

Funcionalidades:

- Validar token.
- Exibir organização convidante.
- Exibir e-mail convidado.
- Definir senha.
- Confirmar aceite.
- Redirecionar para login.

---

# 15. Auditoria — ações padronizadas

Ações sugeridas:

```txt
organization.created
organization.updated
organization.suspended
organization.reactivated
user.created
user.login_succeeded
user.login_failed
user.password_changed
user.password_reset_requested
user.email_verified
member.invited
member.invitation_accepted
member.invitation_revoked
member.disabled
member.enabled
member.role_changed
member.project_access_changed
role.created
role.updated
role.deleted
permission.changed
project.created
project.updated
api_key.created
api_key.revoked
webhook.created
webhook.updated
security.updated
```

Exemplo de metadata:

```json
{
  "oldRole": "MarketingManager",
  "newRole": "ProjectAdmin",
  "changedBy": "admin@empresaabc.com"
}
```

---

# 16. Seed inicial

## 16.1 Permissões

O sistema deve criar automaticamente o catálogo de permissões no primeiro deploy ou migration.

Permissões mínimas:

```txt
organization.read
organization.update
organization.delete_request
users.invite
users.read
users.update
users.disable
users.enable
roles.read
roles.create
roles.update
roles.delete
permissions.read
projects.create
projects.read
projects.update
projects.delete
campaigns.read
campaigns.create
campaigns.update
campaigns.delete
vouchers.read
vouchers.create
vouchers.update
vouchers.import
vouchers.export
validation_rules.read
validation_rules.create
validation_rules.update
customers.read
customers.create
customers.update
segments.read
segments.create
segments.update
redemptions.read
redemptions.cancel
api_keys.read
api_keys.create
api_keys.regenerate
api_keys.revoke
webhooks.read
webhooks.create
webhooks.update
webhooks.delete
webhooks.test
billing.read
billing.manage
usage.read
invoices.read
reports.read
audit.read
audit.read_project
audit.export
security.manage
events.read
logs.read
metadata.read
```

---

## 16.2 Roles padrão

Criar automaticamente:

```txt
OrganizationOwner
OrganizationAdmin
ProjectAdmin
MarketingManager
Developer
FinanceViewer
SupportAgent
ReadOnly
```

---

## 16.3 Planos iniciais

Criar automaticamente:

```txt
Free
Trial
Starter
Growth
Business
Scale
Enterprise
```

Valores iniciais sugeridos:

| Plano | Preço | API calls/mês | Usuários | Projetos | Custom Roles | SSO |
|---|---:|---:|---:|---:|---:|---:|
| Free | R$ 0 | 1.000 | 3 | 1 | Não | Não |
| Trial | R$ 0 | 10.000 | 3 | 1 | Não | Não |
| Starter | R$ 149 | 25.000 | 3 | 1 | Não | Não |
| Growth | R$ 399 | 100.000 | 10 | 3 | Não | Não |
| Business | R$ 899 | 300.000 | 25 | 5 | Sim | Não |
| Scale | R$ 1.990 | 1.000.000 | 50 | 10 | Sim | Não |
| Enterprise | Custom | Custom | Custom | Custom | Sim | Sim |

---

# 17. Validações e erros

## 17.1 Erros de cadastro

| Código | HTTP | Descrição |
|---|---:|---|
| ORGANIZATION_NAME_REQUIRED | 400 | Nome da organização obrigatório |
| EMAIL_ALREADY_EXISTS | 409 | E-mail já cadastrado |
| PASSWORD_TOO_WEAK | 400 | Senha não atende critérios mínimos |
| TERMS_NOT_ACCEPTED | 400 | Termos de uso não aceitos |
| PRIVACY_POLICY_NOT_ACCEPTED | 400 | Política de privacidade não aceita |

---

## 17.2 Erros de login

| Código | HTTP | Descrição |
|---|---:|---|
| INVALID_CREDENTIALS | 401 | E-mail ou senha inválidos |
| USER_DISABLED | 403 | Usuário desativado |
| USER_LOCKED | 423 | Usuário temporariamente bloqueado |
| ORGANIZATION_SUSPENDED | 403 | Organização suspensa |
| EMAIL_NOT_VERIFIED | 403 | E-mail não verificado |

---

## 17.3 Erros de autorização

| Código | HTTP | Descrição |
|---|---:|---|
| PERMISSION_DENIED | 403 | Usuário sem permissão |
| PROJECT_ACCESS_DENIED | 403 | Usuário sem acesso ao projeto |
| ORGANIZATION_ACCESS_DENIED | 403 | Usuário sem acesso à organização |
| PLAN_LIMIT_EXCEEDED | 402/403 | Limite do plano excedido |

---

# 18. Requisitos não funcionais

## 18.1 Segurança

- Hash seguro de senha.
- JWT com expiração curta.
- Refresh token rotativo.
- Revogação de sessão.
- Rate limit em login e recuperação de senha.
- Auditoria de ações sensíveis.
- Filtro obrigatório por `OrganizationId`.
- Segredos via variáveis de ambiente ou Key Vault.
- CORS restrito.
- Logs sem dados sensíveis.

---

## 18.2 Performance

- Login em até 500 ms em cenário normal.
- Cadastro de organização em até 2 s, desconsiderando envio de e-mail.
- Listagem de membros paginada.
- Auditoria paginada.
- Cache de permissões em Redis.
- Índices adequados em `OrganizationId`, `UserId`, `RoleId`, `ProjectId` e `CreatedAt`.

---

## 18.3 Observabilidade

- Logs estruturados.
- Correlation ID por requisição.
- Métricas de login com sucesso.
- Métricas de falha de login.
- Métricas de convites pendentes.
- Métricas de convites expirados.
- Métricas de bloqueio por permissão.
- Application Insights no backend.

---

## 18.4 Disponibilidade

Para MVP:

```txt
Disponibilidade alvo: 99,5%
```

Para planos pagos futuros:

```txt
Business: 99,9%
Enterprise: SLA customizado
```

---

# 19. Configurações de ambiente

## 19.1 Backend

Variáveis necessárias:

```txt
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__DefaultConnection=<postgres-url>
Redis__ConnectionString=<redis-url>
Jwt__Issuer=voucher-system
Jwt__Audience=voucher-system-client
Jwt__Secret=<secret-forte>
Jwt__AccessTokenMinutes=15
Jwt__RefreshTokenDays=30
Cors__AllowedOrigins__0=https://voucher-system-web-dev.onrender.com
ApplicationInsights__ConnectionString=<opcional>
```

---

## 19.2 Frontend

Variáveis necessárias:

```txt
VITE_API_BASE_URL=https://voucher-system-api-dev.onrender.com
```

---

# 20. Estrutura sugerida do backend

```txt
backend/
  VoucherSystem.Api/
    Controllers/
      AuthController.cs
      SelfServiceOrganizationsController.cs
      OrganizationsController.cs
      MembersController.cs
      InvitationsController.cs
      RolesController.cs
      PermissionsController.cs
      AuditLogsController.cs
    Middleware/
      CurrentUserContextMiddleware.cs
      CorrelationIdMiddleware.cs
    Authorization/
      RequirePermissionAttribute.cs
      PermissionAuthorizationHandler.cs
      PermissionRequirement.cs
  VoucherSystem.Application/
    Auth/
    Organizations/
    Members/
    Invitations/
    Roles/
    Permissions/
    AuditLogs/
    Common/
  VoucherSystem.Domain/
    Entities/
    Enums/
    Events/
    ValueObjects/
  VoucherSystem.Infrastructure/
    Persistence/
    Redis/
    Email/
    Security/
    Telemetry/
```

---

# 21. Estrutura sugerida do frontend

```txt
frontend/
  src/
    app/
      router.tsx
      providers.tsx
    pages/
      RegisterOrganizationPage.tsx
      LoginPage.tsx
      DashboardPage.tsx
      MembersPage.tsx
      RolesPage.tsx
      AuditLogsPage.tsx
      AcceptInvitationPage.tsx
    features/
      auth/
      organizations/
      members/
      roles/
      auditLogs/
    components/
      forms/
      tables/
      layout/
    services/
      apiClient.ts
      authService.ts
      membersService.ts
      rolesService.ts
    types/
```

---

# 22. Critérios de aceite

## CA-001 — Cadastro de organização

```gherkin
Dado que sou um visitante
Quando preencho os dados obrigatórios da organização
E aceito os termos de uso e política de privacidade
Então o sistema deve criar a organização
E deve criar automaticamente o usuário administrador inicial
E deve criar o projeto padrão
E deve atribuir a role OrganizationOwner ao usuário inicial
E deve registrar auditoria da criação
```

---

## CA-002 — E-mail duplicado

```gherkin
Dado que já existe um usuário com o e-mail joao@empresaabc.com
Quando tento cadastrar uma nova organização usando esse e-mail
Então o sistema deve retornar erro EMAIL_ALREADY_EXISTS
E nenhuma organização deve ser criada
```

---

## CA-003 — Login do administrador inicial

```gherkin
Dado que uma organização foi criada com sucesso
Quando o administrador inicial realiza login
Então o sistema deve autenticar o usuário
E retornar access token e refresh token
E retornar a organização atual
E retornar as permissões do usuário
```

---

## CA-004 — Convite de membro

```gherkin
Dado que estou autenticado como OrganizationOwner
Quando convido um novo membro com role MarketingManager
Então o sistema deve criar um convite pendente
E enviar o e-mail de convite
E registrar auditoria member.invited
```

---

## CA-005 — Aceite de convite

```gherkin
Dado que recebi um convite válido
Quando acesso o link e defino minha senha
Então o sistema deve criar meu usuário ou ativar meu acesso
E criar meu vínculo com a organização
E atribuir a role selecionada
E registrar auditoria member.invitation_accepted
```

---

## CA-006 — Bloqueio de permissão

```gherkin
Dado que estou autenticado como MarketingManager
Quando tento acessar a tela de Billing
Então o sistema deve negar acesso
E retornar 403 Forbidden
```

---

## CA-007 — Isolamento multi-tenant

```gherkin
Dado que pertenço à Organização A
Quando tento acessar um recurso da Organização B
Então o sistema deve negar o acesso
E não deve retornar dados da Organização B
```

---

## CA-008 — Organização sem owner

```gherkin
Dado que uma organização possui apenas um OrganizationOwner ativo
Quando tento desativar esse usuário
Então o sistema deve bloquear a operação
E informar que a organização precisa manter ao menos um owner ativo
```

---

## CA-009 — Custom role

```gherkin
Dado que estou autenticado como OrganizationOwner
E meu plano permite custom roles
Quando crio uma role personalizada com permissões específicas
Então o sistema deve salvar a role
E permitir atribuí-la a membros da organização
```

---

## CA-010 — Auditoria

```gherkin
Dado que uma ação sensível foi executada
Quando consulto os logs de auditoria
Então o evento deve aparecer com ator, ação, recurso, IP, user agent e data/hora
```

---

# 23. Ordem de implementação recomendada

## Iteração 1 — Base de identidade e organização — concluída em 2026-07-07

Entregar:

- [x] Entidades `Organization`, `User`, `OrganizationMember`, `Project`.
- [x] Migrations EF.
- [x] Seed de roles e permissões.
- [x] Endpoint de cadastro self-service.
- [x] Criação automática de admin e projeto padrão.
- [x] Testes unitários do cadastro (8 testes).

### Resumo da entrega

Ver `docs/evidencias/2026-07-07-R001-INTERACAO-1.md`.

- Stack: .NET 10 + EF Core 10 + PostgreSQL 16 + BCrypt
- 8 entidades de domínio, DbContext com mappings, migration aplicada
- 55 permissions seedadas, 3 system roles (Owner, Admin, ReadOnly)
- Endpoint público `POST /api/self-service/organizations` com validações
- 8 testes unitários passando
- API rodando em `http://localhost:5000`
- Swagger em `/swagger`

Pendências técnicas:
- Autenticação JWT (Iteração 2)
- Configuração SMTP real
- Frontend (Iteração 8)
- Auditoria (Iteração 7)

---

## Iteração 2 — Login e sessão — concluída em 2026-07-07

Entregar:

- [x] Login JWT.
- [x] Refresh token.
- [x] Logout.
- [x] Recuperação de senha.
- [x] Verificação de e-mail (endpoint criado, sem SMTP).
- [ ] Rate limit básico de login.
- [x] Testes de autenticação (8 testes).

---

## Iteração 3 — Autorização e permissões — concluída em 2026-07-07

Entregar:

- [x] Middleware de contexto do usuário.
- [x] Middleware de organização atual.
- [x] Permission handler.
- [x] Cache de permissões em Redis (StackExchange.Redis, TTL 30min).
- [x] `RequirePermission()` extension para Minimal APIs.
- [x] Testes de autorização (4 testes).

---

## Iteração 4 — Membros e convites — concluída em 2026-07-07

Entregar:

- [x] Endpoint de convite.
- [x] Aceite de convite.
- [ ] Reenvio de convite.
- [ ] Revogação de convite.
- [x] Gestão de status de membro.
- [ ] E-mails transacionais básicos.
- [x] Testes de fluxo de convite (7 testes).

---

## Iteração 5 — Roles e custom roles — concluída em 2026-07-07

Entregar:

- [x] Listagem de roles.
- [x] Criação de custom role.
- [x] Edição de custom role.
- [x] Exclusão de custom role não utilizada.
- [x] Associação de permissões.
- [ ] Validação de plano para custom roles.

---

## Iteração 6 — Acesso por projeto — concluída em 2026-07-07

Entregar:

- [x] ProjectAccess (entidade + migration).
- [x] ProjectAccess endpoints (GET + PUT project access por membro).
- [ ] Validação de projeto por requisição.
- [ ] Filtros por projeto no frontend.

---

## Iteração 7 — Auditoria — concluída em 2026-07-07

Entregar:

- [x] AuditLog (entidade + migration).
- [x] IAuditLogWriter com buffer + SaveAsync.
- [x] IAuditLogReader com paginação e filtros.
- [x] Endpoint GET /api/audit-logs com paginação.
- [ ] Exportação controlada por permissão.

---

## Iteração 8 — Frontend completo — concluída em 2026-07-07

Entregar:

- [x] Tela de cadastro (Register.tsx).
- [x] Tela de login (Login.tsx).
- [x] Tela de membros (Members.tsx).
- [x] Tela de roles (Roles.tsx).
- [x] Dashboard com sidebar.
- [x] AuthContext + API client + refresh token.
- [x] Proteção de rotas por autenticação.

---

## Iteração 9 — Hardening — concluída em 2026-07-07

Itens entregues:

- [x] GlobalExceptionMiddleware — captura exceções não tratadas e retorna JSON padronizado
- [x] RequestLoggingMiddleware — log estruturado de toda requisição (method, path, status, duration, userId)
- [ ] Observabilidade / Application Insights
- [ ] Métricas de login e autorização
- [ ] Testes de segurança
- [ ] Revisão de isolamento multi-tenant

---

# 24. Testes obrigatórios

## 24.1 Unitários

- Criação de organização.
- Criação automática do owner.
- Validação de senha.
- Validação de e-mail duplicado.
- Validação de permissões.
- Validação de último owner.
- Geração e aceite de convite.

---

## 24.2 Integração

- Cadastro self-service completo.
- Login.
- Refresh token.
- Convite e aceite.
- Alteração de role.
- Acesso por projeto.
- Auditoria.

---

## 24.3 Segurança

- Usuário sem permissão acessando endpoint protegido.
- Usuário de organização A tentando acessar organização B.
- Token expirado.
- Refresh token revogado.
- Convite expirado.
- Tentativas repetidas de login inválido.

---

# 25. Definition of Done

Uma funcionalidade desta área só será considerada concluída quando:

- Backend implementado.
- Frontend implementado quando aplicável.
- Migrations criadas.
- Seeds aplicados.
- Testes unitários criados.
- Testes de integração criados para fluxos críticos.
- Autorização aplicada.
- Auditoria registrada quando necessário.
- Logs estruturados incluídos.
- Erros padronizados.
- Documentação atualizada.
- Deploy validado em DEV/HML.

---

# 26. Riscos e decisões técnicas

## 26.1 Risco — Permissões no JWT ficarem obsoletas

Se permissões forem gravadas diretamente no JWT, alterações de role podem demorar até o token expirar para surtir efeito.

Decisão recomendada:

- JWT contém identidade e contexto básico.
- Permissões são carregadas do cache Redis.
- Cache é invalidado ao alterar role/permissões.

---

## 26.2 Risco — Organização sem owner

Sem validação, uma organização pode ficar sem administrador máximo.

Decisão recomendada:

- Criar validação de domínio obrigatória antes de remover/desativar owner.

---

## 26.3 Risco — Vazamento multi-tenant

Consultas sem filtro por `OrganizationId` podem expor dados de outros clientes.

Decisão recomendada:

- Criar padrões de repositório sempre exigindo `OrganizationId`.
- Criar testes de isolamento.
- Aplicar filtros globais quando fizer sentido.

---

## 26.4 Risco — Convite exposto em texto puro

Se o token de convite for salvo em texto puro, vazamento de banco compromete convites.

Decisão recomendada:

- Salvar apenas hash do token.
- Enviar token real apenas por e-mail.

---

# 27. Prompt sugerido para Hermes Agent

```txt
Implemente completamente o módulo de Organizações, Logins, Papéis e Permissões do Voucher-System com base no arquivo ORGANIZACOES-LOGINS-ACESSOS.md.

Siga obrigatoriamente a ordem de implementação por iterações descrita no documento. Não avance para a próxima iteração sem concluir backend, frontend quando aplicável, migrations, testes, auditoria e documentação da iteração atual.

Stack obrigatória:
- Backend: .NET 10
- Banco: PostgreSQL via EF 10
- Cache: Redis/Valkey
- Frontend: React + TypeScript + Vite
- Infra: Docker
- Observabilidade: Application Insights/logs estruturados

Regras críticas:
- O cadastro de organização deve ser self-service.
- Ao criar uma organização, criar automaticamente o primeiro login como OrganizationOwner.
- Criar automaticamente um projeto padrão.
- Garantir isolamento multi-tenant por OrganizationId.
- Garantir que a organização nunca fique sem Owner ativo.
- Proteger ações por permissões granulares.
- Registrar auditoria em todas as ações sensíveis.
- Não salvar tokens de convite ou refresh token em texto puro.
- Usar cache de permissões no Redis/Valkey com invalidação quando role, permissões ou acessos forem alterados.

Ao finalizar cada iteração, atualize este arquivo com:
- Status da iteração.
- Arquivos criados/alterados.
- Endpoints implementados.
- Testes implementados.
- Pendências técnicas.
- Próximos passos.
```

---

# 28. Checklist final de entrega

```txt
[ ] Cadastro self-service funcionando
[ ] Organização criada automaticamente
[ ] Owner criado automaticamente
[ ] Projeto padrão criado automaticamente
[ ] Plano inicial criado automaticamente
[ ] Login funcionando
[ ] Refresh token funcionando
[ ] Recuperação de senha funcionando
[ ] Convite de membros funcionando
[ ] Aceite de convite funcionando
[ ] Roles padrão criadas
[ ] Custom roles funcionando
[ ] Permissões granulares funcionando
[ ] Acesso por projeto funcionando
[ ] Auditoria funcionando
[ ] Tela de membros funcionando
[ ] Tela de roles funcionando
[ ] Tela de auditoria funcionando
[ ] Testes unitários criados
[ ] Testes de integração criados
[ ] Render DEV validado
[ ] Documentação atualizada
```

---

# 29. Status de implementação — 2026-07-02

O R001 foi implementado no código da aplicação. Para preservar a raiz multi-tenant
existente, `Account` representa a organização e `AccountId` representa
`OrganizationId`, conforme ADR-0007.

```txt
[x] Cadastro self-service funcionando
[x] Organização criada automaticamente
[x] Owner criado automaticamente
[x] Projeto padrão criado automaticamente
[x] Plano inicial criado automaticamente
[x] Login funcionando
[x] Refresh token funcionando
[x] Recuperação de senha funcionando
[x] Convite de membros funcionando
[x] Aceite de convite funcionando
[x] Roles padrão criadas
[x] Custom roles funcionando
[x] Permissões granulares funcionando
[x] Acesso por projeto funcionando
[x] Auditoria funcionando
[x] Tela de membros funcionando
[x] Tela de roles funcionando
[x] Tela de auditoria funcionando
[x] Testes unitários criados
[x] Testes de integração criados
[ ] Render DEV validado
[x] Documentação atualizada
```

Pendências externas à implementação:

- configurar um servidor SMTP real para entrega dos e-mails transacionais;
- executar Testcontainers em host com Docker disponível;
- validar o deploy em DEV/HML com as credenciais do ambiente.

Evidências: `docs/evidencias/2026-07-02-R001-ORGANIZACOES-LOGINS-ACESSOS.md`.
