# Documentação de Requisitos

Esta pasta organiza a evolução funcional e técnica do Voucher System.

O objetivo é transformar capacidades amplas do produto em entregas pequenas,
rastreáveis, testáveis e documentadas.

## Organização

### Mapa de macro-requisitos

O arquivo [`00-MAPA-MACRO-REQUISITOS.md`](00-MAPA-MACRO-REQUISITOS.md) é o
documento diretor do produto.

Ele apresenta:

- os domínios identificados como `R001`, `R002`, `R003` etc.;
- objetivos e capacidades principais;
- prioridades e dependências;
- ordem recomendada de refinamento e implementação.

O mapa orienta o trabalho, mas não substitui a especificação detalhada.

### Requisitos refinados

Cada macro deve possuir um documento próprio:

```text
R001 - ORGANIZACOES-LOGINS-ACESSOS.md
R002 - PROJETOS-AMBIENTES-MARCAS-LOCALIZACOES.md
R003 - PLATAFORMA-APIS-CREDENCIAIS-OAUTH.md
R004 - CLIENTES-PERFIS-CONSENTIMENTO-PRIVACIDADE.md
```

O documento refinado deve conter, quando aplicável:

- visão e objetivos de negócio;
- escopo e dependências;
- conceitos e decisões de modelagem;
- regras de negócio;
- permissões e scopes;
- entidades, estados e constraints;
- APIs e contratos;
- fluxos e telas;
- auditoria, eventos e jobs;
- segurança e observabilidade;
- critérios de aceite e testes;
- baseline real do código e gaps;
- ordem incremental de implementação.

### Interações

Um requisito refinado é implementado em interações numeradas:

```text
R004.1
R004.2
R004.3
```

Cada interação deve entregar uma parte completa: implementação, testes, build,
documentação, evidência, commit e validação proporcional ao risco.

Não se deve avançar para a próxima interação enquanto a atual estiver incompleta.

### Documentos antigos

Arquivos em [`old/`](old/) são referências históricas. Seu conteúdo deve ser
consolidado nos documentos `RNNN`, sem copiar requisitos desatualizados
automaticamente.

## Fluxo recomendado

```text
Mapa macro
  → análise do código e documentos existentes
  → requisito RNNN refinado
  → implementação RNNN.1, RNNN.2...
  → testes, evidências e roadmap
  → validação DEV/HML
```

## Prompts prontos para copiar e colar

Substitua os valores entre colchetes antes de enviar o prompt. Os blocos abaixo
foram escritos para funcionar de forma independente e podem ser copiados
integralmente.

### 1. Refinar e detalhar um macro-requisito

```text
Analise o macro-requisito [RNNN — NOME] presente em
docs/requisitos/00-MAPA-MACRO-REQUISITOS.md.

Crie o documento detalhado seguindo o padrão completo dos requisitos R002, R003
e R004. Antes de escrever, audite o código atual, o roadmap, os documentos
relacionados e docs/requisitos/old.

O documento deve incluir visão de negócio, escopo, conceitos, decisões técnicas,
regras de negócio numeradas, permissões, modelo de dados, estados, APIs,
contratos, fluxos, portal, auditoria, eventos, jobs, segurança, observabilidade,
critérios de aceite, testes, baseline, gaps, migração, compatibilidade e uma ordem
de implementação incremental RNNN.1, RNNN.2 etc.

Atualize também o mapa e gere uma evidência em
docs/evidencias. Não implemente funcionalidades nesta etapa.
```

### 2. Iniciar a implementação de um requisito refinado

```text
Implemente a próxima interação pendente do requisito
[RNNN — NOME], começando por [RNNN.X — INTERAÇÃO].

Antes de alterar código, leia o requisito refinado e
audite o baseline atual. Trabalhe somente no escopo dessa interação.

Conclua backend, frontend, migration e infraestrutura apenas quando forem
aplicáveis. Preserve account_id e project_id, autenticação, autorização,
idempotência, transações, auditoria, segurança e compatibilidade.

Adicione testes proporcionais ao risco, execute build/test/lint, valide em DEV
quando possível, atualize o requisito refinado (Checklist de entrega) e o roadmap,
crie uma evidência ([DATA-INTERACAO-X-N]) e registre pendências reais. Ao final,
gere um commit específico da interação e envie ao repositório. Não avance para a
interação seguinte.
```

## Regra prática

O mapa informa **o que o produto precisa**. O documento `RNNN` explica **como o
domínio deve funcionar**. A interação `RNNN.X` define **o que será entregue
agora**.
