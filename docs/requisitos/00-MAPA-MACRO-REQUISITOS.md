# Mapa de Macro-Requisitos de Negócio e Técnicos

> **Produto:** Voucher System
> **Referência funcional:** Voucherify e plataformas API-first de incentivos
> **Objetivo:** organizar o desenvolvimento em macro-requisitos numerados, refináveis e rastreáveis
> **Status:** documento diretor inicial
> **Última revisão:** 2026-07-03

---

# 1. Propósito

Este documento é o mapa diretor dos requisitos do Voucher System. Ele divide o
produto em macroáreas identificadas por `R001`, `R002`, `R003` e assim por diante.

Cada macro-requisito deve originar, no momento apropriado, um documento próprio e
detalhado seguindo o padrão adotado em:

```text
docs/requisitos/R001 - ORGANIZACOES-LOGINS-ACESSOS.md
```

Este mapa não substitui os documentos detalhados. Ele define:

- a visão de negócio de cada domínio;
- as capacidades técnicas esperadas;
- as entidades e regras centrais;
- os requisitos não funcionais críticos;
- as dependências entre domínios;
- a ordem recomendada de refinamento e implementação;
- a relação com documentos e código que já existem.

---

# 2. Método e limites da análise

O catálogo foi construído a partir de:

1. documentação pública e API pública da Voucherify;
2. funcionalidades observáveis no produto e em seus guias públicos;
3. requisitos existentes em `docs/requisitos`;
4. arquitetura e funcionalidades já presentes no Voucher System;
5. necessidades típicas de uma plataforma SaaS multi-tenant de incentivos.

Não é objetivo reproduzir código, propriedade intelectual interna, interface ou
marca da Voucherify. A referência é usada para identificar capacidades de produto,
fluxos e problemas de negócio. Decisões técnicas permanecem próprias do Voucher
System.

---

# 3. Visão de produto

O Voucher System deve ser uma plataforma API-first para criação, execução,
distribuição e análise de incentivos digitais.

Equipes de marketing, produto, growth, atendimento e engenharia devem conseguir:

- criar campanhas sem alterar código;
- personalizar incentivos por cliente, pedido, produto, evento e contexto;
- validar elegibilidade em tempo real;
- executar resgates de forma transacional e idempotente;
- combinar cupons, promoções, gift cards, loyalty e referrals;
- distribuir incentivos em múltiplos canais;
- integrar o motor a checkout, CRM, e-commerce, aplicativos e data platforms;
- auditar decisões e movimentações financeiras ou de pontos;
- medir custo, conversão, receita incremental e risco de fraude;
- operar múltiplas organizações, projetos, marcas, regiões e ambientes.

---

# 4. Princípios obrigatórios

## 4.1 Negócio

1. Incentivos devem ser configuráveis por usuários autorizados.
2. Toda decisão de elegibilidade deve ser explicável.
3. Campanhas devem possuir ciclo de vida, orçamento, limites e responsáveis.
4. Saldos, pontos, resgates e reversões devem formar um histórico imutável.
5. O produto deve separar configuração, simulação, publicação e operação.
6. Funcionalidades premium devem ser controladas por plano e quota.
7. Dados de clientes devem respeitar privacidade, retenção e direito ao apagamento.

## 4.2 Técnicos

1. PostgreSQL é a fonte transacional.
2. Redis é apoio operacional e nunca a única fonte de saldo ou pontos.
3. Toda entidade operacional deve preservar `account_id` e `project_id`.
4. Operações críticas devem ser transacionais e idempotentes.
5. APIs devem possuir contratos versionados, documentados e erros padronizados.
6. Eventos devem usar outbox e entrega tolerante a falhas.
7. Secrets nunca devem ser armazenados ou registrados em texto puro.
8. Auditoria deve ser append-only para ações críticas.
9. Horários devem ser persistidos em UTC e exibidos no fuso do projeto.
10. Valores monetários devem usar tipo decimal ou unidade monetária inteira.

---

# 5. Personas principais

| Persona | Responsabilidade |
|---|---|
| Platform Admin | Opera tenants, planos, quotas, segurança e saúde global |
| Organization Owner | Administra organização, projetos, usuários e faturamento |
| Project Admin | Administra configurações e acessos de um projeto |
| Campaign Manager | Cria campanhas, regras, códigos e distribuições |
| CRM/Growth Manager | Segmenta audiências e automatiza incentivos |
| Loyalty Manager | Opera programas, tiers, pontos e recompensas |
| Analyst | Consulta desempenho, custos, conversões e exportações |
| Customer Service | Consulta clientes e executa ajustes autorizados |
| Developer/Integrator | Usa APIs, API keys, OAuth, webhooks e logs técnicos |
| Auditor/Security | Revisa acessos, alterações, riscos e evidências |
| End Customer | Recebe, consulta e utiliza incentivos |

---

# 6. Fluxos de valor

```text
Configurar tenant
  → preparar projeto e integrações
  → sincronizar clientes, catálogo e eventos
  → criar campanha e incentivo
  → definir regras, orçamento e stacking
  → simular e publicar
  → distribuir ou qualificar
  → validar e reservar
  → resgatar ou reverter
  → entregar recompensa
  → analisar, auditar e otimizar
```

---

# 7. Catálogo executivo

| ID | Macro-requisito | Tipo predominante | Prioridade | Dependências principais |
|---|---|---|---:|---|
| R001 | Organizações, Logins, Papéis e Permissões | Fundação SaaS | P0 | — |
| R002 | Projetos, Ambientes, Marcas e Localizações | Fundação SaaS | P0 | R001 |
| R003 | Plataforma de APIs, Credenciais e OAuth | Plataforma | P0 | R001, R002 |
| R004 | Clientes, Perfis, Consentimento e Privacidade | Negócio | P0 | R002, R022 |
| R005 | Segmentos e Audiências | Negócio | P0 | R004, R011 |
| R006 | Catálogo, Produtos, SKUs, Coleções e Pedidos | Negócio | P0 | R002, R022 |
| R007 | Eventos de Cliente e Atividades | Plataforma/Negócio | P1 | R004, R021, R022 |
| R008 | Campanhas, Templates, Categorias e Calendário | Negócio | P0 | R001, R002 |
| R009 | Vouchers, Códigos e Ciclo de Vida | Negócio | P0 | R008 |
| R010 | Promoções e Tipos de Desconto | Negócio | P0 | R006, R008 |
| R011 | Motor de Regras e Elegibilidade | Motor | P0 | R004, R005, R006, R022 |
| R012 | Qualificação, Validação e Sessões | Motor | P0 | R009, R010, R011 |
| R013 | Resgates, Reversões e Idempotência | Motor | P0 | R012 |
| R014 | Stacking e Orquestração de Incentivos | Motor | P1 | R010, R012, R013 |
| R015 | Gift Cards, Créditos e Ledger Financeiro | Negócio/Motor | P1 | R009, R013 |
| R016 | Loyalty, Pontos, Tiers e Expiração | Negócio/Motor | P1 | R004, R007, R013 |
| R017 | Catálogo e Entrega de Recompensas | Negócio | P1 | R006, R015, R016 |
| R018 | Referral e Conversões de Indicação | Negócio | P1 | R004, R007, R017 |
| R019 | Publicações, Holders e Atribuição de Códigos | Negócio | P1 | R004, R009 |
| R020 | Distribuições, Mensagens e Canais | Negócio/Integração | P1 | R005, R019, R021 |
| R021 | Webhooks, Eventos e Processamento Assíncrono | Plataforma | P0 | R002, R003 |
| R022 | Metadata, Schemas e Campos Customizados | Plataforma | P0 | R002 |
| R023 | Analytics, Auditoria, Exportações e Notificações | Operação | P1 | Todos os domínios operacionais |
| R024 | Orçamento, Limites, Fraude e Gestão de Risco | Motor/Operação | P1 | R011, R013, R023 |
| R025 | Imports, Bulk Operations e Async Actions | Plataforma | P1 | R003, R021 |
| R026 | Portal Administrativo e Experiência Operacional | Frontend | P0 | Todos os módulos expostos |
| R027 | Arquitetura, Dados e Multi-Tenancy | Plataforma | P0 | R001, R002 |
| R028 | Segurança, Privacidade e Compliance | Plataforma | P0 | R001, R003, R027 |
| R029 | Observabilidade, Confiabilidade e Operação | Plataforma | P0 | R021, R027 |
| R030 | Qualidade, Testes, CI/CD e Deploy | Engenharia | P0 | Todos |
| R031 | Planos, Quotas, Billing e Entitlements | SaaS/Comercial | P1 | R001, R023 |

---

# 8. Macro-requisitos

## R001 — Organizações, Logins, Papéis e Permissões

### Objetivo de negócio

Permitir onboarding self-service e administração segura de pessoas, responsabilidades
e acessos dentro de uma organização.

### Capacidades

- cadastro e manutenção da organização;
- primeiro usuário como `OrganizationOwner`;
- autenticação, recuperação de senha, verificação e gestão de sessões;
- membros, convites, ativação, bloqueio e remoção;
- roles nativas e customizadas;
- permissões granulares e acesso por projeto;
- proteção do último owner;
- auditoria de autenticação e administração.

### Fundação técnica

- separação entre `User` e `OrganizationMember`;
- hashes para senhas, refresh tokens, convites e API secrets;
- cache Redis de permissões com invalidação;
- JWT curto e refresh token rotativo;
- isolamento obrigatório por organização.

### Documento detalhado

`R001 - ORGANIZACOES-LOGINS-ACESSOS.md` — existente.

---

## R002 — Projetos, Ambientes, Marcas e Localizações

### Objetivo de negócio

Permitir que uma organização separe marcas, países, moedas, equipes e ambientes de
desenvolvimento ou produção sem misturar dados.

### Capacidades

- criar, editar, arquivar e excluir projetos;
- ambientes Sandbox, Development, Staging e Production;
- moeda, idioma, fuso horário e região;
- marcas e identidade visual;
- localizações, lojas, áreas ou unidades de negócio;
- cópia controlada de schemas, templates e configurações;
- troca de projeto no portal;
- quotas e acesso de usuários por projeto.

### Regras técnicas críticas

- recursos nunca atravessam projetos implicitamente;
- chaves e webhooks pertencem ao projeto;
- referências cruzadas exigem operação explícita de cópia;
- exclusão deve validar recursos dependentes e retenção.

### Documento detalhado

`R002 - PROJETOS-AMBIENTES-MARCAS-LOCALIZACOES.md` — especificado, aguardando implementação incremental.

---

## R003 — Plataforma de APIs, Credenciais e OAuth

Documento detalhado:
`R003 - PLATAFORMA-APIS-CREDENCIAIS-OAUTH.md` — especificado, aguardando implementação incremental.

### Objetivo de negócio

Oferecer uma plataforma de integração segura, previsível e adequada para servidores,
aplicações web, mobile e parceiros.

### Capacidades

- API keys privadas, públicas/client-side e de integração;
- scopes, roles, expiração, rotação, revogação e IP allowlist;
- OAuth 2.0 para clientes e parceiros;
- paginação, filtros, ordenação, expansão e seleção de campos;
- versionamento de API e política de depreciação;
- idempotency keys;
- rate limits, quotas e headers operacionais;
- OpenAPI, exemplos e portal do desenvolvedor;
- erros padronizados com `request_id`, código e detalhes.

### Regras técnicas críticas

- armazenar somente hash dos secrets;
- segredo completo exibido apenas na criação/rotação;
- rate limiting distribuído;
- APIs client-side possuem superfície reduzida;
- toda chamada deve gerar correlation/request ID.

---

## R004 — Clientes, Perfis, Consentimento e Privacidade

> **Detalhamento:** `R004 - CLIENTES-PERFIS-CONSENTIMENTO-PRIVACIDADE.md`
> **Status:** detalhado; implementação incremental pendente

### Objetivo de negócio

Manter a visão operacional do consumidor usada para personalização, elegibilidade,
atribuição de incentivos e atendimento.

### Capacidades

- CRUD e upsert por `source_id`;
- dados de contato, endereço, preferências e metadata;
- histórico de atividades e incentivos atribuídos;
- merge e deduplicação de perfis;
- importação e atualização em massa;
- consentimentos por finalidade e canal;
- anonimização, retenção, exportação e direito ao esquecimento;
- customer 360 para atendimento.

### Regras técnicas críticas

- PII deve ser minimizada e protegida;
- `source_id` deve ser único por projeto;
- exclusão administrativa e apagamento permanente são operações distintas;
- logs não podem expor dados sensíveis completos.

---

## R005 — Segmentos e Audiências

> **Detalhamento:** `R005 - SEGMENTOS-E-AUDIENCIAS.md`
> **Status:** detalhado; implementação incremental pendente

### Objetivo de negócio

Transformar dados de cliente e comportamento em audiências reutilizáveis para regras,
loyalty, referrals e distribuições.

### Capacidades

- segmentos estáticos;
- segmentos dinâmicos passivos;
- segmentos dinâmicos ativos com eventos de entrada e saída;
- filtros por perfil, metadata, pedidos e eventos;
- composição booleana e preview de audiência;
- avaliação de pertencimento em tempo real;
- exportação de membros;
- versionamento ou imutabilidade quando o segmento estiver em uso crítico.

### Regras técnicas críticas

- avaliação deve ser determinística e explicável;
- segmentos ativos devem emitir eventos via outbox;
- atualização em massa não pode bloquear APIs transacionais;
- cache deve ser invalidado após mudanças relevantes.

---

## R006 — Catálogo, Produtos, SKUs, Coleções e Pedidos

### Objetivo de negócio

Representar o contexto comercial necessário para descontos, regras, loyalty e
analytics sem substituir obrigatoriamente o ERP ou e-commerce.

### Capacidades

- produtos, SKUs, preço, atributos e metadata;
- coleções estáticas e dinâmicas;
- importação e sincronização em massa;
- pedidos e itens com estados `CREATED`, `PAID`, `CANCELED` e `FULFILLED`;
- impostos, frete, moeda, descontos e totais;
- vínculo com cliente, canal, loja e fonte externa;
- cálculo de valores antes e depois de descontos;
- eventos de mudança de estado do pedido.

### Regras técnicas críticas

- dinheiro deve usar precisão explícita e moeda;
- `source_id` deve suportar idempotência de sincronização;
- snapshots de itens preservam o contexto usado no resgate;
- coleção dinâmica não pode produzir dupla contagem.

---

## R007 — Eventos de Cliente e Atividades

### Objetivo de negócio

Permitir que comportamentos externos acionem segmentos, pontos, referrals,
distribuições e análises.

### Capacidades

- schemas de eventos customizados;
- ingestão server-side e client-side controlada;
- eventos de pedido, cliente, segmento e campanha;
- deduplicação por ID externo;
- validação de payload e metadata;
- timeline de atividades;
- replay administrativo seguro;
- retenção e exportação.

### Regras técnicas críticas

- eventos recebidos devem ser idempotentes;
- evento aceito não pode ser perdido;
- processamento assíncrono deve suportar retry e dead-letter;
- ordem deve ser preservada quando necessária por agregado.

---

## R008 — Campanhas, Templates, Categorias e Calendário

### Objetivo de negócio

Fornecer a raiz de configuração, governança e ciclo de vida dos programas de
incentivo.

### Capacidades

- campanhas em draft, scheduled, active, paused, ended e archived;
- tipos coupon, promotion, gift card, loyalty e referral;
- período, recorrência, dias e horários válidos;
- categorias e tags;
- owner, descrição, objetivo e centro de custo;
- templates reutilizáveis e cópia entre projetos;
- calendário de campanhas;
- aprovação e publicação;
- resumo de desempenho.

### Regras técnicas críticas

- configurações publicadas devem ser versionadas;
- alteração não pode modificar retroativamente transações concluídas;
- campanhas referenciadas não devem ser removidas fisicamente;
- ativação deve validar consistência integral.

---

## R009 — Vouchers, Códigos e Ciclo de Vida

### Objetivo de negócio

Criar e administrar códigos únicos ou genéricos usados como instrumentos de
incentivo.

### Capacidades

- geração com prefixo, sufixo, charset, comprimento e padrão;
- códigos únicos, stand-alone e genéricos;
- lotes síncronos e assíncronos;
- importação de códigos externos;
- ativação, desativação, expiração e exclusão lógica;
- limites globais e por cliente;
- holder, publicação e histórico;
- busca segura e mascaramento;
- transações e atividades por voucher.

### Regras técnicas críticas

- unicidade por projeto com comparação definida;
- geração deve resistir a colisões;
- códigos sensíveis não devem aparecer integralmente em logs;
- operações em lote devem ser retomáveis e idempotentes.

---

## R010 — Promoções e Tipos de Desconto

### Objetivo de negócio

Configurar descontos automáticos ou acionados por código para diferentes estratégias
comerciais.

### Capacidades

- percentual, valor fixo, frete grátis e unidade grátis;
- desconto por pedido, item, SKU, produto ou coleção;
- limites mínimo e máximo do desconto;
- tiers promocionais;
- promoções automáticas;
- `buy X get Y`;
- descontos proporcionais e fórmulas;
- preview do efeito no pedido;
- arredondamento e distribuição do desconto entre itens.

### Regras técnicas críticas

- cálculo deve ser determinístico;
- nenhuma aplicação pode gerar total inválido;
- regra monetária deve declarar moeda e arredondamento;
- resposta deve explicar cada efeito aplicado.

---

## R011 — Motor de Regras e Elegibilidade

### Objetivo de negócio

Permitir que equipes configurem quem, quando, onde e em qual contexto pode receber
ou utilizar um incentivo.

### Capacidades

- condições por cliente, segmento, pedido, produto, evento e metadata;
- operadores booleanos, numéricos, textuais, temporais e de conjunto;
- grupos `AND`, `OR` e negação;
- limites por campanha, voucher, cliente e período;
- regras reutilizáveis e assignments;
- builder visual;
- teste com contexto simulado;
- versionamento, publicação e rollback;
- explicação de falhas por condição.

### Regras técnicas críticas

- engine sem efeitos colaterais durante avaliação;
- AST ou representação versionada;
- proteção contra complexidade e profundidade excessivas;
- cache por versão publicada;
- testes extensivos de combinação e fronteira.

---

## R012 — Qualificação, Validação e Sessões

### Objetivo de negócio

Descobrir incentivos elegíveis, validar os selecionados e reservar temporariamente
capacidade durante o checkout.

### Capacidades

- qualification de incentivos aplicáveis;
- validação de um ou vários redeemables;
- respostas applicable, inapplicable e skipped;
- cálculo de pedido resultante;
- motivos e detalhes de falha;
- validation sessions com lock, TTL, overwrite e release;
- validação server-side e superfície client-side restrita;
- tracking ID e auditoria de tentativas.

### Regras técnicas críticas

- validação não altera saldo permanente;
- sessão deve impedir consumo concorrente;
- expiração libera capacidade automaticamente;
- resultado deve referenciar versões de regras e campanhas.

---

## R013 — Resgates, Reversões e Idempotência

### Objetivo de negócio

Aplicar incentivos de forma consistente, evitar uso duplicado e permitir reversões
seguras em cancelamentos ou devoluções.

### Capacidades

- resgate simples e múltiplo;
- vínculo com pedido, cliente e sessão;
- estados pending, succeeded, failed e rolled_back;
- idempotency key;
- rollback integral ou compatível com o tipo de incentivo;
- histórico de tentativas;
- ajuste atômico de limites, saldo e pontos;
- reconciliação e atendimento operacional.

### Regras técnicas críticas

- transação PostgreSQL única para efeitos críticos;
- concorrência controlada por lock ou versão;
- retry não duplica efeito;
- rollback referencia a transação original;
- ledger e auditoria são imutáveis.

---

## R014 — Stacking e Orquestração de Incentivos

### Objetivo de negócio

Controlar como diferentes incentivos podem ser combinados para maximizar conversão
sem perder margem ou previsibilidade.

### Capacidades

- aplicação `ALL` ou `PARTIAL`;
- categorias joint e exclusive;
- limites totais e por categoria;
- ordem solicitada, melhor desconto e prioridades;
- combinação de coupon, promotion, gift card, loyalty e referral;
- política para incentivos sem efeito;
- aplicação por produto ou sobre pedido já descontado;
- rollback do conjunto e dos filhos;
- explicação dos itens ignorados.

### Regras técnicas críticas

- resultado estável para a mesma entrada;
- limite de complexidade e quantidade;
- efeitos calculados antes do commit;
- parent redemption representa o conjunto.

---

## R015 — Gift Cards, Créditos e Ledger Financeiro

### Objetivo de negócio

Administrar valor armazenado que pode ser emitido, creditado, gasto, estornado e
expirado com rastreabilidade financeira.

### Capacidades

- campanhas e cartões;
- saldo inicial e recargas;
- débito, crédito, ajuste e reversão;
- limites por operação;
- expiração de créditos;
- moedas e política de arredondamento;
- associação a cliente;
- extrato e exportação;
- bloqueio, encerramento e fraude.

### Regras técnicas críticas

- ledger de dupla referência ou movimentação imutável;
- saldo é derivado/reconciliável;
- nenhuma alteração direta sem lançamento;
- débito concorrente não pode tornar saldo negativo;
- idempotência por transação externa.

---

## R016 — Loyalty, Pontos, Tiers e Expiração

### Objetivo de negócio

Criar programas de relacionamento que recompensem comportamento e aumentem retenção
e valor de vida do cliente.

### Capacidades

- programas, cartões e membros;
- auto-join e join-once;
- earning rules fixas, proporcionais e por evento;
- pontos ativos, pendentes, cancelados e expirados;
- regras de expiração por campanha ou earning rule;
- tiers por saldo ou pontos acumulados no período;
- upgrade, downgrade, prolongamento e eventos de tier;
- transferência e ajustes autorizados;
- estimativa de pontos;
- extrato e expiring buckets.

### Regras técnicas críticas

- ledger imutável de pontos;
- consumo prioriza buckets com expiração mais próxima;
- jobs de ativação e expiração são idempotentes;
- alterações de regra não reescrevem transações históricas.

---

## R017 — Catálogo e Entrega de Recompensas

### Objetivo de negócio

Reutilizar recompensas em loyalty e referral, controlando custo, estoque e entrega.

### Capacidades

- recompensas digitais e materiais;
- coupon, gift credit, loyalty points e pay-with-points;
- produto físico e fulfillment manual;
- catálogo reutilizável;
- assignment para programa e tier;
- custo em pontos;
- estoque e disponibilidade;
- compra/resgate e estados de entrega;
- auto-redeem com proteção contra loops.

### Regras técnicas críticas

- compra e débito de pontos são atômicos;
- emissão digital deve ser idempotente;
- recompensa material exige estado de fulfillment;
- limites antiabuso devem ser configuráveis.

---

## R018 — Referral e Conversões de Indicação

### Objetivo de negócio

Transformar clientes em promotores, medir conversões e recompensar referrer e
referee.

### Capacidades

- programas single-sided e double-sided;
- códigos únicos por referrer;
- join-once;
- conversão por resgate ou evento customizado;
- vínculo referrer/referee;
- bloqueio de autorreferência;
- tiers por cada conversão ou marcos acumulados;
- rewards digitais e materiais;
- regras de elegibilidade e antifraude;
- publicação, distribuição e analytics.

### Regras técnicas críticas

- conversão é idempotente;
- um evento não pode premiar duas vezes;
- vínculo histórico deve ser preservado;
- regras devem impedir ciclos e autoindicação.

---

## R019 — Publicações, Holders e Atribuição de Códigos

### Objetivo de negócio

Controlar a atribuição de vouchers, loyalty cards e referral codes a clientes.

### Capacidades

- selecionar código disponível;
- publicar para um holder;
- publicação por `source_id`;
- uma ou múltiplas atribuições conforme campanha;
- expiração relativa após publicação;
- listagem de redeemables do cliente;
- unpublish quando permitido;
- histórico e eventos de publicação;
- publicação client-side com chave restrita.

### Regras técnicas críticas

- seleção e atribuição devem ser atômicas;
- retry deve retornar a mesma publicação;
- códigos reservados não podem ser atribuídos em paralelo;
- políticas join-once devem possuir constraint.

---

## R020 — Distribuições, Mensagens e Canais

### Objetivo de negócio

Entregar incentivos e mensagens ao público correto no momento correto, usando canais
próprios ou integrações.

### Capacidades

- distribuições manuais, agendadas e automáticas;
- triggers por publicação, evento, segmento, pontos, reward e referral;
- email, SMS, push, webhook e conectores;
- templates, variáveis e preview;
- mapeamento de payload;
- múltiplos canais por distribuição;
- consentimento, preferência e opt-out;
- pause, resume e cancelamento;
- logs de entrega e métricas.

### Regras técnicas críticas

- publicação e envio são etapas separadas;
- falha de canal não pode duplicar atribuição;
- retries com backoff e dead-letter;
- PII em payload exige política explícita.

---

## R021 — Webhooks, Eventos e Processamento Assíncrono

### Objetivo de negócio

Permitir integração reativa e confiável com sistemas externos.

### Capacidades

- webhooks de projeto e de distribuição;
- catálogo versionado de eventos;
- assinatura HMAC, timestamp e proteção contra replay;
- filtros por evento;
- headers e payload customizável;
- tentativas, backoff, timeout e dead-letter;
- replay manual;
- logs de request/response sanitizados;
- outbox para eventos internos;
- consumers idempotentes.

### Regras técnicas críticas

- commit de negócio e outbox na mesma transação;
- entrega ao menos uma vez;
- ordenação quando exigida;
- secrets criptografados;
- circuit breaker e limites por destino.

---

## R022 — Metadata, Schemas e Campos Customizados

### Objetivo de negócio

Adaptar o produto a diferentes verticais sem criar novas colunas ou código para cada
necessidade.

### Capacidades

- schemas por tipo de recurso;
- string, number, boolean, date, datetime, image, geopoint e object;
- objetos aninhados;
- campos obrigatórios e opcionais;
- constraints e valores permitidos;
- modos strict, permissive e unknown;
- schemas de eventos customizados;
- cópia entre projetos;
- uso em filtros, regras, exports e templates.

### Regras técnicas críticas

- JSONB com índices seletivos;
- validação antes da persistência;
- evolução de schema deve tratar dados existentes;
- campos desconhecidos não podem contornar segurança.

---

## R023 — Analytics, Auditoria, Exportações e Notificações

### Objetivo de negócio

Dar visibilidade sobre desempenho, custo, operação, alterações e resultados
assíncronos.

### Capacidades

- dashboards por campanha e incentivo;
- métricas de validação, resgate, publicação e conversão;
- receita, desconto, breakage e custo de rewards;
- funis de loyalty e referral;
- audit log pesquisável;
- timeline por recurso;
- exports CSV/JSON assíncronos;
- central de notificações e arquivos;
- retenção e acesso por permissão.

### Regras técnicas críticas

- analytics não bloqueia fluxo transacional;
- auditoria crítica é imutável;
- exportações grandes usam storage temporário seguro;
- links de download expiram;
- timestamps e moeda são consistentes.

---

## R024 — Orçamento, Limites, Fraude e Gestão de Risco

### Objetivo de negócio

Proteger margem, orçamento e integridade dos programas contra erro de configuração e
abuso.

### Capacidades

- budget monetário e por quantidade;
- limites por campanha, voucher, cliente, dia, canal e loja;
- velocity checks;
- bloqueio de autorreferência e múltiplas identidades;
- allowlist e denylist;
- alertas de consumo e anomalias;
- kill switch;
- aprovação para campanhas de alto risco;
- painel de tentativas suspeitas.

### Regras técnicas críticas

- contadores críticos devem ser transacionais;
- limites aproximados podem usar Redis apenas como primeira barreira;
- decisão antifraude deve ser auditável;
- bloqueios automáticos precisam de override autorizado.

---

## R025 — Imports, Bulk Operations e Async Actions

### Objetivo de negócio

Operar grandes volumes sem bloquear o portal ou exigir integrações registro a
registro.

### Capacidades

- importação de clientes, produtos, vouchers e pedidos;
- validação prévia e dry-run;
- processamento em chunks;
- progresso, sucesso parcial e relatório de erros;
- geração e atualização em massa;
- async actions consultáveis;
- cancelamento quando seguro;
- retomada e deduplicação;
- downloads de resultado.

### Regras técnicas críticas

- jobs são idempotentes e possuem lease;
- payloads grandes usam storage apropriado;
- falha de uma linha não perde o lote inteiro;
- limites protegem banco e memória.

---

## R026 — Portal Administrativo e Experiência Operacional

### Objetivo de negócio

Permitir que usuários de negócio configurem e operem o produto com segurança, sem
depender da API para atividades rotineiras.

### Capacidades

- navegação orientada por domínio e permissão;
- project switcher;
- builders de campanha, regra, distribuição e schema;
- drafts, previews, confirmação e diff;
- tabelas com paginação server-side;
- filtros persistentes e exports;
- loading, empty, error e retry states;
- acessibilidade e responsividade;
- localização e formatação por projeto;
- feature flags e entitlements visíveis.

### Regras técnicas críticas

- frontend não substitui autorização da API;
- nenhum secret permanece no bundle;
- rotas são protegidas por permissão;
- erros exibem correlation ID;
- operações destrutivas exigem confirmação contextual.

---

## R027 — Arquitetura, Dados e Multi-Tenancy

### Objetivo de negócio

Sustentar crescimento de tenants, volume e funcionalidades sem comprometer
isolamento ou velocidade de entrega.

### Capacidades técnicas

- arquitetura modular Domain/Application/Infrastructure/API/Workers;
- limites de módulos e contratos internos;
- multi-tenancy por `account_id` e `project_id`;
- migrations backward-compatible;
- PostgreSQL transacional e Redis operacional;
- outbox/inbox;
- soft delete e retenção;
- clock abstrato e IDs globais;
- estratégias de particionamento e arquivamento;
- feature flags e evolução incremental.

### Regras técnicas críticas

- toda consulta tenant-aware recebe contexto obrigatório;
- testes automatizados de isolamento;
- migrations expand-and-contract em produção;
- cache keys sempre incluem tenant e versão.

---

## R028 — Segurança, Privacidade e Compliance

### Objetivo de negócio

Proteger clientes, dados e operações e fornecer evidências para auditoria e contratos
enterprise.

### Capacidades

- autenticação forte e MFA evolutivo;
- RBAC e menor privilégio;
- secrets em Key Vault;
- criptografia em trânsito e repouso;
- CORS, CSP e headers seguros;
- rate limiting e proteção contra abuso;
- logs sanitizados;
- consentimento, retenção e apagamento;
- trilhas de auditoria;
- SAST, dependency scan e secret scan;
- resposta a incidentes e rotação de credenciais.

### Regras técnicas críticas

- nenhuma PII ou secret completo em logs;
- ações críticas exigem autenticação recente quando aplicável;
- permissões são verificadas server-side;
- ameaças devem ser modeladas por fluxo.

---

## R029 — Observabilidade, Confiabilidade e Operação

### Objetivo de negócio

Detectar problemas antes que afetem campanhas e reduzir tempo de diagnóstico e
recuperação.

### Capacidades

- logs estruturados e correlation ID;
- traces distribuídos;
- métricas técnicas e de negócio;
- health, readiness e liveness;
- dashboards e alertas;
- SLOs por API crítica;
- retry, timeout, circuit breaker e bulkhead;
- backup, restore e disaster recovery;
- runbooks;
- status de workers, filas e dead letters.

### Regras técnicas críticas

- telemetria não pode registrar secrets;
- resgate e validação possuem métricas próprias;
- restore deve ser testado;
- alertas precisam indicar ação e responsável.

---

## R030 — Qualidade, Testes, CI/CD e Deploy

### Objetivo de negócio

Entregar mudanças frequentes sem comprometer dinheiro, pontos, acesso ou campanhas
ativas.

### Capacidades

- testes unitários de engines e regras;
- integração real com PostgreSQL e Redis;
- contratos de API;
- E2E dos fluxos críticos;
- concorrência, idempotência e property-based tests;
- performance e carga;
- segurança automatizada;
- build reproduzível;
- migrations e smoke tests no pipeline;
- deploy progressivo e rollback;
- evidências por interação.

### Gates mínimos

- build sem warnings;
- testes e lint aprovados;
- dependências sem vulnerabilidade crítica;
- migration validada;
- nenhum secret detectado;
- smoke test após deploy.

---

## R031 — Planos, Quotas, Billing e Entitlements

### Objetivo de negócio

Transformar capacidades da plataforma em ofertas comerciais controláveis e
mensuráveis.

### Capacidades

- catálogo de planos;
- trial e conversão;
- quotas de usuários, projetos, chamadas, vouchers e eventos;
- entitlements por funcionalidade;
- medição de uso;
- alertas de aproximação do limite;
- upgrade, downgrade, suspensão e grace period;
- histórico de assinatura;
- integração futura com billing;
- visão de uso e faturamento.

### Regras técnicas críticas

- entitlement é validado na API;
- medição não pode duplicar em retries;
- suspensão preserva dados;
- downgrade deve tratar recursos acima da nova quota.

---

# 9. Ordem recomendada de detalhamento

## Onda 1 — Fundação e transação principal

```text
R001 → R002 → R003 → R027 → R028
                  ↘ R021 → R029 → R030
R004 → R006 → R008 → R009 → R011 → R012 → R013
```

Objetivo: tenant seguro, API integrável e fluxo completo de voucher até resgate.

## Onda 2 — Personalização e promoções

```text
R005 → R007 → R010 → R014 → R019 → R022 → R024
```

Objetivo: campanhas personalizadas, stackable e protegidas por orçamento.

## Onda 3 — Stored value e engagement

```text
R015 → R016 → R017 → R018 → R020
```

Objetivo: gift cards, loyalty, rewards, referrals e comunicação.

## Onda 4 — Escala operacional e monetização

```text
R023 → R025 → R026 → R031
```

Objetivo: operação em volume, portal completo, insights e planos comerciais.

---

# 10. Política para criação dos documentos detalhados

Cada novo macro-requisito deve ser refinado em arquivo próprio:

```text
RNNN - NOME-DO-DOMINIO.md
```

O documento detalhado deve conter, no mínimo:

1. visão e objetivo de negócio;
2. escopo e fora de escopo;
3. personas e permissões;
4. glossário;
5. jornadas e fluxos;
6. regras de negócio numeradas;
7. estados e máquinas de estado;
8. modelo de domínio;
9. modelo de dados e índices;
10. APIs e contratos;
11. eventos e webhooks;
12. jobs assíncronos;
13. telas e experiência;
14. segurança e auditoria;
15. observabilidade;
16. erros padronizados;
17. casos de aceitação;
18. testes;
19. plano incremental de implementação;
20. Definition of Done;
21. riscos, decisões e pendências.

### Regra de rastreabilidade

Código, migration, endpoint, tela e teste devem referenciar o requisito detalhado ou
sua interação no roadmap.

---

# 11. Relação com a documentação existente

Os documentos antigos continuam válidos como material de domínio. Ao refinar os
novos `RNNN`, o conteúdo deve ser consolidado, não simplesmente duplicado.

| Documento existente | Macro-requisitos relacionados |
|---|---|
| `01-CONTAS-PROJETOS-E-AMBIENTES.md` | R001, R002, R031 |
| `02-AUTENTICACAO-AUTORIZACAO-E-API-KEYS.md` | R001, R003, R028 |
| `03-CAMPANHAS.md` | R008 |
| `04-VOUCHERS-CODIGOS-E-GERACAO.md` | R009, R025 |
| `05-VALIDACAO-QUALIFICACAO-E-RESGATE.md` | R012, R013 |
| `06-REGRAS-DE-NEGOCIO-E-SEGMENTACAO.md` | R005, R011 |
| `07-CLIENTES-SEGMENTOS-E-AUDIENCIA.md` | R004, R005 |
| `08-CATALOGO-PRODUTOS-PEDIDOS.md` | R006 |
| `09-PROMOCOES-DESCONTOS-E-STACKING.md` | R010, R014 |
| `10-GIFT-CARDS-E-SALDOS.md` | R015 |
| `11-LOYALTY-E-RECOMPENSAS.md` | R016, R017 |
| `12-REFERRAL.md` | R018 |
| `13-DISTRIBUICAO-PUBLICACAO-E-CANAIS.md` | R019, R020 |
| `14-WEBHOOKS-E-EVENTOS.md` | R007, R021 |
| `15-METADATA-SCHEMAS-E-CAMPOS-CUSTOMIZADOS.md` | R022 |
| `16-ANALYTICS-AUDITORIA-E-EXPORTACOES.md` | R023 |
| `17-BACKEND-ARQUITETURA-DOTNET.md` | R027 |
| `18-FRONTEND-REACT-TYPESCRIPT-VITE.md` | R026 |
| `19-INFRA-DOCKER-AZURE-KEYVAULT-OBSERVABILIDADE.md` | R028, R029 |
| `20-TESTES-QUALIDADE-E-SEGURANCA.md` | R028, R030 |
| `21-DEPLOY-OPERACAO-E-RUNBOOK.md` | R029, R030 |

---

# 12. Critérios para priorizar um macro-requisito

Antes de iniciar um novo `RNNN`, avaliar:

- valor direto para aquisição, conversão, retenção ou eficiência;
- bloqueio que ele remove de outros domínios;
- risco financeiro ou de segurança;
- cobertura atual no código;
- complexidade de migration;
- necessidade de dados ou integração externa;
- capacidade de testar em DEV;
- impacto em compatibilidade.

O roadmap deve apontar qual macro-requisito está ativo e qual documento detalhado é
a fonte de verdade da interação.

---

# 13. Definition of Done deste mapa

Este mapa será considerado maduro quando:

- todos os macros tiverem owner de produto e técnico;
- dependências forem validadas;
- cada macro possuir status no roadmap;
- documentos detalhados forem criados conforme a ordem aprovada;
- requisitos legados forem consolidados e marcados como substituídos quando couber;
- houver rastreabilidade entre requisito, código, testes e evidências.

---

# 14. Referências públicas analisadas

- [Voucherify — Key concepts](https://docs.voucherify.io/get-started/key-concepts)
- [Voucherify — Documentation index](https://docs.voucherify.io/llms.txt)
- [Voucherify — API quickstart](https://docs.voucherify.io/guides/api-quickstart)
- [Voucherify — Authentication and authorization](https://docs.voucherify.io/guides/authentication)
- [Voucherify — Customer overview](https://docs.voucherify.io/prepare/customer-overview)
- [Voucherify — Customer segments](https://docs.voucherify.io/prepare/customer-segments)
- [Voucherify — Metadata](https://docs.voucherify.io/prepare/metadata)
- [Voucherify — Campaign limits](https://docs.voucherify.io/guides/campaign-limits)
- [Voucherify — Validations and redemptions](https://docs.voucherify.io/optimize/validations-and-redemptions)
- [Voucherify — Locking validation session](https://docs.voucherify.io/guides/locking-validation-session)
- [Voucherify — Stackable discounts API](https://docs.voucherify.io/guides/manage-stackable-discounts)
- [Voucherify — Loyalty campaign overview](https://docs.voucherify.io/build/loyalty-campaign-overview)
- [Voucherify — Earning rules](https://docs.voucherify.io/build/earning-rules)
- [Voucherify — Referral program overview](https://docs.voucherify.io/build/referral-campaign-overview)
- [Voucherify — Create referral campaign](https://docs.voucherify.io/build/create-referral-campaign)
- [Voucherify — Create rewards](https://docs.voucherify.io/optimize/create-rewards)
- [Voucherify — Introduction to webhooks](https://docs.voucherify.io/api-reference/introduction-to-webhooks)
- [Voucherify — Distribution webhooks](https://docs.voucherify.io/api-reference/distribution-webhooks)
