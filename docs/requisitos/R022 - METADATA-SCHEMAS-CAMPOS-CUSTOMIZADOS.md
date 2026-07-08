# R022 — Metadata, Schemas e Campos Customizados

## 1. Objetivo de negócio

Este requisito tem como objetivo adaptar o produto a diferentes verticais sem criar novas colunas ou código para cada necessidade. O valor para o produto é criar uma base operacional confiável para campanhas promocionais, validações, resgates, análises e integrações API-first. Para os usuários, reduz dependência de desenvolvimento sob demanda, aumenta rastreabilidade, melhora governança e permite operação segura em ambiente multi-tenant.

## 2. Escopo

### Incluído
- schemas por tipo de recurso.
- tipos string, number, boolean, date, datetime, image, geopoint e object.
- objetos aninhados.
- campos obrigatórios e opcionais.
- constraints e valores permitidos.
- modos strict/permissive/unknown.
- schemas de eventos customizados.
- cópia entre projetos.
- uso em filtros, regras, exports e templates.

### Fora de escopo
- editor de banco de dados livre.
- execução de código customizado.
- indexação automática irrestrita.
- schema registry público.

## 3. Personas impactadas

- Project Admin
- Developer / Integrator
- Campaign Manager
- Analyst

## 4. Fluxos funcionais

Fluxo feliz:
1. Ator autenticado seleciona organização e projeto válido.
2. Sistema carrega permissões efetivas do ator e valida escopo.
3. Ator cria ou consulta recurso de metadata, schemas e extensibilidade com payload válido.
4. Backend valida tenant, schema, estado, permissões e regras de negócio.
5. Operação é persistida no PostgreSQL com transação e, se crítica, idempotência.
6. Sistema grava audit log e outbox na mesma unidade de trabalho.
7. API retorna resposta padronizada com `requestId` e `correlationId`.
8. Portal atualiza listagem/detalhe e registra timeline do recurso.

Fluxos alternativos:
1. Ator executa criação de schema usando API key server-side; sistema aplica scopes e mesmo fluxo de auditoria.
2. Ator envia metadata extra; sistema valida conforme schema R022 e aceita/rejeita conforme modo do projeto.
3. Operação é longa; sistema cria async action e permite acompanhamento de progresso.
4. Recurso já existe por `source_id`; sistema executa upsert ou retorna conflito conforme endpoint.

Fluxos de erro:
1. Payload inválido retorna `400 validation_error` com erros por campo.
2. Falta de permissão retorna `403 insufficient_permission` e registra tentativa quando sensível.
3. Estado incompatível retorna `422 invalid_state_transition`.
4. Concorrência retorna `409 concurrency_conflict` ou `423 locked`.
5. Erro interno retorna `500 internal_error` com correlationId, sem expor stack trace ao cliente.

Permissões necessárias:
- Leitura: `r022:read`.
- Escrita: `r022:write`.
- Execução de ação crítica: `r022:execute`.
- Administração: `r022:admin`.
- Auditoria/exportação: `r022:audit` ou `r022:export`.

Impactos em auditoria:
- Registrar criação, alteração, mudança de status, ação crítica, erro de permissão e override.
- Preservar antes/depois em alterações administrativas.
- Vincular audit log a `accountId`, `projectId`, `actorId`, `entityId`, `correlationId` e IP/origem.

## 5. Regras de negócio

- **RN-001 — Isolamento tenant-aware**  
  Descrição: todo recurso de metadata, schemas e extensibilidade deve pertencer a uma organização e, quando aplicável, a um projeto.  
  Condição: qualquer criação, consulta, alteração ou exclusão.  
  Resultado esperado: somente dados do `account_id` e `project_id` do contexto autenticado são acessíveis.  
  Mensagem/erro: `403 forbidden` quando houver tentativa de acesso cruzado.

- **RN-002 — Estado permitido antes da operação crítica**  
  Descrição: operações como criação de schema, publicação de versão, validação antes da persistência só podem ocorrer em estados explicitamente permitidos.  
  Condição: recurso em estado incompatível, arquivado, removido, suspenso ou bloqueado.  
  Resultado esperado: a operação é recusada sem efeito colateral.  
  Mensagem/erro: `422 invalid_state_transition`.

- **RN-003 — Idempotência em escrita crítica**  
  Descrição: operações com efeito operacional ou financeiro devem aceitar e persistir `Idempotency-Key`.  
  Condição: mesma chave, mesmo tenant, mesmo endpoint e payload equivalente.  
  Resultado esperado: retornar o resultado original sem duplicar efeito, evento, ledger, contador ou auditoria crítica.  
  Mensagem/erro: `409 idempotency_payload_mismatch` quando a chave for reutilizada com payload diferente.

- **RN-004 — Auditoria obrigatória**  
  Descrição: alterações administrativas e ações críticas de metadata, schemas e extensibilidade devem gerar audit log append-only.  
  Condição: criação, alteração de status, publicação, exclusão lógica, override ou operação sensível.  
  Resultado esperado: registrar ator, entidade, antes/depois, correlationId, IP/origem e severidade.  
  Mensagem/erro: se auditoria falhar na mesma transação crítica, a operação deve falhar com `500 audit_write_failed`.

- **RN-005 — Metadata validada por schema**  
  Descrição: campos `metadata` devem respeitar schema ativo do projeto quando existir.  
  Condição: payload contém metadata fora do schema, tipo inválido ou campo obrigatório ausente.  
  Resultado esperado: recusar persistência ou classificar campo como unknown conforme modo do schema.  
  Mensagem/erro: `400 metadata_schema_validation_error`.

- **RN-006 — Segurança e menor privilégio**  
  Descrição: permissões devem ser avaliadas server-side por ação, recurso e projeto.  
  Condição: usuário/API key sem scope ou papel suficiente.  
  Resultado esperado: operação negada e tentativa auditada quando sensível.  
  Mensagem/erro: `403 insufficient_permission`.

## 6. Requisitos funcionais

- **RF-001 — CRUD/listagem do domínio**  
  Descrição: disponibilizar criação, edição permitida, consulta, listagem paginada e exclusão lógica dos recursos de metadata, schemas e extensibilidade.  
  Prioridade: Must.  
  Dependências: R002.  
  Critérios de aceite: endpoints retornam erros padronizados; filtros por status/data funcionam; acesso respeita tenant/projeto.

- **RF-002 — Execução das ações críticas**  
  Descrição: suportar ações de domínio como criação de schema, publicação de versão, validação antes da persistência, cópia entre projetos, criação controlada de índice.  
  Prioridade: Must.  
  Dependências: permissões, auditoria e idempotência.  
  Critérios de aceite: cada ação valida estado, gera evento, gera audit log e retorna correlationId.

- **RF-003 — Timeline e histórico operacional**  
  Descrição: exibir histórico das alterações, tentativas, eventos e decisões relevantes do recurso.  
  Prioridade: Should.  
  Dependências: R021 e R023.  
  Critérios de aceite: timeline apresenta data/hora no fuso do projeto, ator, ação, resultado e correlationId.

- **RF-004 — Busca, filtros e exportação operacional**  
  Descrição: permitir busca por identificadores, status, período, metadata e relacionamento principal.  
  Prioridade: Should.  
  Dependências: PostgreSQL, índices e R025 para exportações grandes.  
  Critérios de aceite: filtros combinados funcionam com paginação; exportações grandes geram async action.

- **RF-005 — Integração API-first**  
  Descrição: todos os fluxos essenciais de metadata, schemas e extensibilidade devem ser executáveis por API antes ou junto do portal.  
  Prioridade: Must.  
  Dependências: R003.  
  Critérios de aceite: OpenAPI documenta contratos, scopes, erros, payloads e exemplos.

- **RF-006 — Governança de status e publicação**  
  Descrição: quando aplicável, separar rascunho/configuração, simulação, publicação e operação.  
  Prioridade: Must.  
  Dependências: R008/R011 quando o recurso for usado em campanhas ou regras.  
  Critérios de aceite: versões publicadas ficam imutáveis para transações históricas.

## 7. Requisitos técnicos

- **RT-001 — Backend modular em .NET 10**: implementar casos de uso em `VoucherSystem.Application`, regras em `VoucherSystem.Domain`, persistência em `Infrastructure` e endpoints em `Api`, evitando regra de negócio em controller.
- **RT-002 — EF Core 10/PostgreSQL**: mapear entidades com `account_id`, `project_id`, `created_at`, `updated_at`, `created_by`, `updated_by`, `metadata jsonb`, `row_version` e índices tenant-aware.
- **RT-003 — Redis**: usar Redis apenas para cache, rate limit, locks temporários ou sessões operacionais; nunca como fonte única de saldo, pontos, resgate ou auditoria.
- **RT-004 — Application Insights/OpenTelemetry**: propagar `correlationId`, `requestId`, `accountId`, `projectId`, `operationName` e métricas customizadas sem PII/secrets.
- **RT-005 — Idempotência**: persistir `IdempotencyRecord` com hash do payload, endpoint, tenant, status, resposta resumida e expiração configurável.
- **RT-006 — Segurança**: validar JWT/API key/OAuth scopes; aplicar autorização por policy; mascarar secrets, códigos e PII; usar Azure Key Vault para segredos.
- **RT-007 — Outbox/Inbox**: toda ação que gera evento deve gravar outbox na mesma transação; consumers devem deduplicar por message id.
- **RT-008 — Performance**: listagens devem ser paginadas; queries devem usar índices compostos; endpoints críticos devem ter metas de p95 definidas; filtros JSONB devem ser seletivos.
- **RT-009 — Frontend React/TypeScript/Vite**: implementar feature isolada em `frontend/src/features/metadata-schemas-campos-customizados` com client tipado, hooks, rotas protegidas e tratamento de erro padronizado.
- **RT-010 — Docker/configuração**: variáveis por ambiente, health checks e conexão com PostgreSQL/Redis/Key Vault devem estar documentadas e testáveis via Docker Compose.

## 8. Modelo de dados sugerido

- `MetadataSchema`: schema. Campos sugeridos: `id uuid PK`, `account_id uuid`, `project_id uuid`, `status text`, `metadata jsonb`, `created_at timestamptz`, `updated_at timestamptz`, `row_version xmin/bytea`.
- `MetadataField`: campo. Campos sugeridos: `id uuid PK`, `account_id uuid`, `project_id uuid`, `status text`, `metadata jsonb`, `created_at timestamptz`, `updated_at timestamptz`, `row_version xmin/bytea`.
- `SchemaVersion`: versão. Campos sugeridos: `id uuid PK`, `account_id uuid`, `project_id uuid`, `status text`, `metadata jsonb`, `created_at timestamptz`, `updated_at timestamptz`, `row_version xmin/bytea`.
- `SchemaAssignment`: aplicação por recurso. Campos sugeridos: `id uuid PK`, `account_id uuid`, `project_id uuid`, `status text`, `metadata jsonb`, `created_at timestamptz`, `updated_at timestamptz`, `row_version xmin/bytea`.
- `SchemaValidationResult`: resultado. Campos sugeridos: `id uuid PK`, `account_id uuid`, `project_id uuid`, `status text`, `metadata jsonb`, `created_at timestamptz`, `updated_at timestamptz`, `row_version xmin/bytea`.
- `JsonbIndexDefinition`: índice seletivo. Campos sugeridos: `id uuid PK`, `account_id uuid`, `project_id uuid`, `status text`, `metadata jsonb`, `created_at timestamptz`, `updated_at timestamptz`, `row_version xmin/bytea`.

Relacionamentos sugeridos:
- Toda entidade operacional referencia `accounts(id)` por `account_id` e, quando aplicável, `projects(id)` por `project_id`.
- Entidades de histórico/timeline referenciam a entidade principal por `entity_id` e preservam snapshot mínimo.
- Entidades de evento/outbox referenciam `correlation_id` e `causation_id`.
- Entidades com operação crítica referenciam `idempotency_records(id)` ou armazenam `idempotency_key_hash`.

Índices e constraints:
- Índice composto obrigatório: `(account_id, project_id, status, created_at desc)`.
- Índice único para identificador externo quando existir: `(account_id, project_id, source_id) WHERE deleted_at IS NULL`.
- Índice GIN seletivo para `metadata jsonb` apenas nos campos aprovados pelo schema.
- Constraint de status com enum/check constraint ou tabela de domínio.
- Constraint de concorrência via `row_version`/`xmin` e validação `If-Match` em alterações críticas.

Campos de auditoria:
- `created_at`, `created_by`, `updated_at`, `updated_by`, `deleted_at`, `deleted_by`, `correlation_id`, `source`, `metadata`.
- Para recursos versionáveis: `version`, `published_at`, `published_by`, `archived_at`.
- Para transações: `occurred_at`, `idempotency_key_hash`, `external_reference`, `reason`.

## 9. APIs sugeridas

- `GET /api/v1/metadata-schemas`  
  Objetivo: listar recursos de metadata, schemas e extensibilidade com paginação server-side.  
  Autenticação exigida: Bearer JWT ou API key com scope `r022:read`.  
  Request: query `page`, `pageSize`, `sort`, filtros por `status`, `createdAt`, `updatedAt`, `metadata`.  
  Response: `items[]`, `page`, `pageSize`, `total`, `requestId`.  
  Erros: `401 unauthorized`, `403 forbidden`, `429 rate_limit_exceeded`.  
  Idempotência: não aplicável.

- `POST /api/v1/metadata-schemas`  
  Objetivo: criar recurso principal do requisito.  
  Autenticação exigida: Bearer JWT ou API key server-side com scope `r022:write`.  
  Request: payload com `accountId`, `projectId` quando aplicável, atributos de domínio, `metadata`, `idempotencyKey` para operação crítica.  
  Response: recurso criado, `id`, `status`, `createdAt`, `correlationId`.  
  Erros: `400 validation_error`, `409 duplicate_or_conflict`, `422 business_rule_violation`.  
  Idempotência: obrigatório em criação operacional ou importação; a mesma chave deve retornar o mesmo resultado.

- `GET /api/v1/metadata-schemas/{id}`  
  Objetivo: consultar detalhe, timeline resumida e estado atual.  
  Autenticação exigida: scope `r022:read` e acesso ao `account_id/project_id`.  
  Request: path `id`, query opcional `expand=audit,timeline,metadata`.  
  Response: recurso completo, relacionamentos permitidos e `requestId`.  
  Erros: `404 not_found`, `403 forbidden`.  
  Idempotência: não aplicável.

- `PATCH /api/v1/metadata-schemas/{id}`  
  Objetivo: alterar campos permitidos respeitando estado e concorrência.  
  Autenticação exigida: scope `r022:write`.  
  Request: JSON Merge Patch, `rowVersion`/`If-Match`, `metadata`, motivo quando ação crítica.  
  Response: recurso atualizado e nova versão.  
  Erros: `409 concurrency_conflict`, `422 invalid_state_transition`, `403 forbidden`.  
  Idempotência: recomendada para alterações operacionais repetíveis.

- `POST /api/v1/metadata-schemas/{id}/actions/{action}`  
  Objetivo: executar ação de domínio, como publicar, ativar, pausar, cancelar, expirar, reprocessar, aprovar, revogar ou simular.  
  Autenticação exigida: scope `r022:execute` e permissão funcional específica.  
  Request: `action`, `reason`, `effectiveAt`, `idempotencyKey`, `metadata`.  
  Response: `operationId`, estado final, eventos gerados e warnings.  
  Erros: `409 conflict`, `422 business_rule_violation`, `423 locked`, `429 rate_limit_exceeded`.  
  Idempotência: obrigatória para ações críticas com efeito de negócio.

## 10. Eventos e webhooks

- **`metadata_schema.created`**  
  Quando ocorre: após criação de schema ou mudança relevante no domínio de metadata, schemas e extensibilidade.  
  Payload mínimo: `eventId`, `eventType`, `occurredAt`, `accountId`, `projectId`, `entityId`, `entityType`, `actorId`, `correlationId`, `version`, `metadata`.  
  Deve gerar webhook: sim, quando o projeto tiver assinatura ativa para esse evento.  
  Deve entrar em outbox: sim, obrigatoriamente na mesma transação da mudança.

- **`metadata_schema.published`**  
  Quando ocorre: após criação de schema ou mudança relevante no domínio de metadata, schemas e extensibilidade.  
  Payload mínimo: `eventId`, `eventType`, `occurredAt`, `accountId`, `projectId`, `entityId`, `entityType`, `actorId`, `correlationId`, `version`, `metadata`.  
  Deve gerar webhook: sim, quando o projeto tiver assinatura ativa para esse evento.  
  Deve entrar em outbox: sim, obrigatoriamente na mesma transação da mudança.

- **`metadata_schema.deprecated`**  
  Quando ocorre: após criação de schema ou mudança relevante no domínio de metadata, schemas e extensibilidade.  
  Payload mínimo: `eventId`, `eventType`, `occurredAt`, `accountId`, `projectId`, `entityId`, `entityType`, `actorId`, `correlationId`, `version`, `metadata`.  
  Deve gerar webhook: sim, quando o projeto tiver assinatura ativa para esse evento.  
  Deve entrar em outbox: sim, obrigatoriamente na mesma transação da mudança.

- **`metadata_validation.failed`**  
  Quando ocorre: após criação de schema ou mudança relevante no domínio de metadata, schemas e extensibilidade.  
  Payload mínimo: `eventId`, `eventType`, `occurredAt`, `accountId`, `projectId`, `entityId`, `entityType`, `actorId`, `correlationId`, `version`, `metadata`.  
  Deve gerar webhook: sim, quando o projeto tiver assinatura ativa para esse evento.  
  Deve entrar em outbox: sim, obrigatoriamente na mesma transação da mudança.

## 11. Auditoria

- Ação: criação de schema.  
  Ator: usuário autenticado, API credential ou worker identificado.  
  Entidade afetada: recurso principal de metadata, schemas e extensibilidade.  
  Antes/depois: obrigatório para alteração; snapshot resumido para criação/execução.  
  correlationId: obrigatório.  
  Severidade: Medium.
- Ação: publicação de versão.  
  Ator: usuário autenticado, API credential ou worker identificado.  
  Entidade afetada: recurso principal de metadata, schemas e extensibilidade.  
  Antes/depois: obrigatório para alteração; snapshot resumido para criação/execução.  
  correlationId: obrigatório.  
  Severidade: High.
- Ação: validação antes da persistência.  
  Ator: usuário autenticado, API credential ou worker identificado.  
  Entidade afetada: recurso principal de metadata, schemas e extensibilidade.  
  Antes/depois: obrigatório para alteração; snapshot resumido para criação/execução.  
  correlationId: obrigatório.  
  Severidade: High.
- Ação: cópia entre projetos.  
  Ator: usuário autenticado, API credential ou worker identificado.  
  Entidade afetada: recurso principal de metadata, schemas e extensibilidade.  
  Antes/depois: obrigatório para alteração; snapshot resumido para criação/execução.  
  correlationId: obrigatório.  
  Severidade: Medium.
- Ação: criação controlada de índice.  
  Ator: usuário autenticado, API credential ou worker identificado.  
  Entidade afetada: recurso principal de metadata, schemas e extensibilidade.  
  Antes/depois: obrigatório para alteração; snapshot resumido para criação/execução.  
  correlationId: obrigatório.  
  Severidade: Critical.

## 12. Frontend / UX

Rotas:
- `/settings/metadata`: listagem principal.
- `/settings/metadata/new`: criação.
- `/settings/metadata/:id`: detalhe com abas de visão geral, configuração, timeline e auditoria.
- `/settings/metadata/:id/edit`: edição controlada por estado.
- `/settings/metadata/:id/actions`: ações críticas, quando fizer sentido.

Componentes:
- Tabela com paginação server-side, ordenação, seleção de colunas, filtros por status/período/metadata e ação de exportar.
- Formulários com validação client-side, preview de payload, confirmação para ações destrutivas e mensagens com correlationId.
- Timeline do recurso com eventos de domínio, alterações administrativas, webhooks e erros de processamento.
- Empty states para ausência de registros, ausência de permissão e feature não habilitada pelo plano.
- Loading states com skeleton e botões em estado pending.
- Error states com retry, detalhes técnicos expansíveis e link para logs quando o usuário tiver permissão.
- Permissões visuais: esconder ações sem permissão e exibir tooltip “sem permissão” quando a ação for visível mas bloqueada.
- UX de auditoria: exibir ator, data no fuso do projeto, origem, IP quando disponível e diff antes/depois.

## 13. Validações

Server-side:
- `accountId` e `projectId` obrigatórios e compatíveis com o contexto.
- Campos obrigatórios do domínio de metadata, schemas e extensibilidade não podem ser nulos ou vazios.
- `metadata` deve respeitar schema publicado do projeto.
- IDs externos devem respeitar unicidade por tenant/projeto.
- Estados e transições devem respeitar máquina de estado do requisito.
- Ações críticas exigem permissão funcional, scope e, quando aplicável, `Idempotency-Key`.
- Datas devem ser UTC e intervalos devem ter início menor que fim.
- Valores monetários/pontos/contadores, quando existirem, não podem violar precisão, escala, sinal e limites configurados.

Client-side:
- Validar obrigatoriedade, formato, tamanho máximo, enum e consistência básica antes do submit.
- Bloquear submit duplicado enquanto a requisição estiver pending.
- Exibir mensagens de erro por campo e erro geral com correlationId.
- Aplicar máscaras de data, moeda, código, email e URL quando aplicável.
- Não confiar em validação visual para autorização; API permanece fonte de verdade.

## 14. Cenários de teste

- **Unitário — regras de domínio**  
  Given recurso em estado permitido; When executar criação de schema; Then estado, eventos e validações devem ser consistentes.

- **Unitário — regra inválida**  
  Given payload sem campo obrigatório; When validar comando; Then retornar erro de validação sem acessar banco.

- **Integração — persistência tenant-aware**  
  Given dois projetos no mesmo banco; When consultar com contexto do Projeto A; Then nenhum dado do Projeto B deve aparecer.

- **API — criação idempotente**  
  Given `Idempotency-Key` nova; When enviar `POST` duas vezes com o mesmo payload; Then a segunda resposta deve reutilizar o resultado sem duplicar registros/eventos.

- **API — conflito de idempotência**  
  Given `Idempotency-Key` já usada; When reenviar com payload diferente; Then retornar `409 idempotency_payload_mismatch`.

- **Frontend — formulário e erro**  
  Given usuário com permissão; When preencher formulário inválido; Then mensagens por campo aparecem e submit não é enviado.

- **Frontend — permissão visual**  
  Given usuário sem permissão de execução; When abrir detalhe; Then ação crítica fica oculta ou desabilitada e API retornaria `403` se chamada.

- **Concorrência**  
  Given duas requisições simultâneas sobre o mesmo recurso; When ambas tentam alterar estado crítico; Then apenas uma confirma e a outra retorna conflito/lock.

- **Segurança**  
  Given API key sem scope; When chamar endpoint protegido; Then retornar `403` e registrar tentativa relevante.

- **Observabilidade**  
  Given operação concluída; When inspecionar telemetria; Then logs, traces, métricas e audit log compartilham o mesmo correlationId.

## 15. Observabilidade

Logs:
- Log estruturado em início/fim de operação, validações recusadas, conflitos de concorrência e chamadas externas.
- Campos mínimos: `timestamp`, `level`, `operation`, `accountId`, `projectId`, `actorId`, `entityId`, `correlationId`, `requestId`, `durationMs`, `outcome`.
- Não registrar PII completa, secrets, códigos sensíveis ou payloads de webhook sem sanitização.

Métricas:
- `voucher_system_r022_requests_total`
- `voucher_system_r022_failures_total`
- `voucher_system_r022_operation_duration_ms`
- `voucher_system_r022_business_events_total`
- Métricas específicas de domínio para contadores, saldos, volume, erros de validação ou backlog quando aplicável.

Traces:
- Span por endpoint, use case, query crítica, chamada Redis, gravação outbox e worker.
- Propagar `correlationId` para webhooks e async actions.

Application Insights:
- Custom event `R022.MetadataSchemasECamposCustomizados.OperationCompleted`.
- Custom event `R022.BusinessRuleViolation`.
- Custom metric de latência p50/p95/p99 por operação crítica.

Alertas sugeridos:
- taxa de erro 5xx acima de 2% por 5 minutos;
- p95 acima do SLO definido;
- falhas de outbox/webhook acima do limite;
- aumento anormal de `business_rule_violation`;
- dead letters ou jobs pendentes acima do threshold.

## 16. Riscos e decisões pendentes

- Risco: vazamento ou mistura de dados entre tenants/projetos.  
  Impacto: alto.  
  Probabilidade: média.  
  Mitigação: filtros globais tenant-aware, testes automatizados de isolamento e revisão de queries.

- Risco: duplicidade por retry ou concorrência em operação crítica.  
  Impacto: alto.  
  Probabilidade: média.  
  Mitigação: idempotency record transacional, constraints únicas, locks/row version e testes concorrentes.

- Risco: degradação de performance em listagens, filtros ou metadata JSONB.  
  Impacto: médio/alto.  
  Probabilidade: média.  
  Mitigação: paginação obrigatória, índices compostos, índices GIN seletivos, limites de page size e métricas p95.

- Risco: auditoria incompleta ou sem correlationId.  
  Impacto: alto.  
  Probabilidade: baixa/média.  
  Mitigação: middleware obrigatório de correlationId e audit interceptor na camada application/infrastructure.

- Risco: regras de estado mal definidas para metadata, schemas e extensibilidade.  
  Impacto: médio.  
  Probabilidade: média.  
  Mitigação: máquina de estados explícita, testes unitários por transição e mensagens de erro padronizadas.

Decisões pendentes:
- Definir limites de quota e page size específicos por plano.
- Definir SLO p95/p99 para endpoints críticos deste requisito.
- Definir quais campos de metadata terão índice GIN em produção.
- Confirmar nomenclatura final de scopes e roles no AGENTS/SOUL do agente de desenvolvimento.

## 17. Critérios de aceite finais

- Todos os endpoints definidos para metadata, schemas e extensibilidade estão implementados, documentados em OpenAPI e protegidos por autenticação/autorização.
- Todas as operações críticas têm idempotência, audit log, eventos de domínio e correlationId.
- Modelo de dados possui `account_id`, `project_id` quando aplicável, índices, constraints e campos de auditoria.
- Frontend possui listagem, detalhe, criação/edição, estados de loading/error/empty e controle visual por permissão.
- Testes unitários, integração, API, frontend, segurança e concorrência relevantes estão aprovados.
- Application Insights recebe logs, métricas e traces com cardinalidade controlada.
- Nenhuma PII, segredo, código sensível ou token é registrado em texto claro.
- Critérios de aceite funcionais foram validados em ambiente Docker Compose com PostgreSQL e Redis.

## 18. Arquivos que provavelmente serão alterados/criados

- `src/VoucherSystem.Domain/MetadataSchemasCamposCustomizados/...`
- `src/VoucherSystem.Application/MetadataSchemasCamposCustomizados/Commands/...`
- `src/VoucherSystem.Application/MetadataSchemasCamposCustomizados/Queries/...`
- `src/VoucherSystem.Api/Endpoints/MetadataSchemasCamposCustomizados/...`
- `src/VoucherSystem.Infrastructure/Persistence/Configurations/...`
- `src/VoucherSystem.Infrastructure/Migrations/...`
- `src/VoucherSystem.Infrastructure/Observability/...`
- `src/VoucherSystem.Workers/MetadataSchemasCamposCustomizados/...`
- `frontend/src/features/metadata-schemas-campos-customizados/...`
- `frontend/src/routes/...`
- `docs/requisitos/R022 - METADATA-SCHEMAS-CAMPOS-CUSTOMIZADOS.md`
- `tests/VoucherSystem.UnitTests/MetadataSchemasCamposCustomizados/...`
- `tests/VoucherSystem.IntegrationTests/MetadataSchemasCamposCustomizados/...`
- `tests/VoucherSystem.ApiTests/MetadataSchemasCamposCustomizados/...`
- `frontend/tests/metadata-schemas-campos-customizados/...`

## 19. Plano incremental de implementação

- **Iteração 1 — Modelo de domínio e persistência**  
  Objetivo: criar entidades, enums, constraints, migrations e repositórios/query services para metadata, schemas e extensibilidade.  
  Arquivos prováveis: Domain, Infrastructure/Persistence, migrations e testes de integração.  
  Testes esperados: criação de banco, constraints, isolamento tenant-aware e concorrência básica.  
  Critério de aceite: migration sobe e desce localmente; queries respeitam account/project; modelo compila sem warnings.  
  Dependências: R002.

- **Iteração 2 — Casos de uso e regras de negócio**  
  Objetivo: implementar comandos/queries, validações, máquina de estados, idempotência e audit log.  
  Arquivos prováveis: Application/Commands, Application/Queries, Domain services, Audit services.  
  Testes esperados: unitários de RN-001 a RN-006 e idempotência.  
  Critério de aceite: regras críticas cobertas e erros padronizados retornados.  
  Dependências: Iteração 1.

- **Iteração 3 — API, eventos e observabilidade**  
  Objetivo: expor endpoints REST, gerar OpenAPI, gravar outbox e instrumentar logs/métricas/traces.  
  Arquivos prováveis: Api/Endpoints, Contracts, Infrastructure/Outbox, Observability.  
  Testes esperados: API tests, autorização, idempotency replay, outbox e correlationId.  
  Critério de aceite: endpoints funcionam via Docker Compose com PostgreSQL/Redis e Application Insights configurável.  
  Dependências: Iteração 2 e R003/R021 quando aplicável.

- **Iteração 4 — Frontend operacional**  
  Objetivo: criar rotas, telas, formulários, listagens, detalhe, timeline e estados UX.  
  Arquivos prováveis: `frontend/src/features/...`, hooks, services, components, tests.  
  Testes esperados: componentes, fluxos de formulário, permissão visual e tratamento de erro.  
  Critério de aceite: usuário autorizado executa fluxo feliz; usuário sem permissão não vê/não executa ação.  
  Dependências: Iteração 3.

- **Iteração 5 — Hardening e validação funcional**  
  Objetivo: executar testes de concorrência, performance, segurança, rollback e documentação final.  
  Arquivos prováveis: tests, docs/requisitos, ROADMAP-DEVELOPMENT.md como referência externa, runbooks.  
  Testes esperados: carga mínima, conflito concorrente, retry, falhas simuladas e smoke test.  
  Critério de aceite: Definition of Done atendida e evidências anexadas ao requisito.  
  Dependências: Iterações 1–4.
