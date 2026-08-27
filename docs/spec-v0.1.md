# Perimeter Data Gateway — Спецификация v0.1

## Назначение

Self-hosted шлюз, дающий локальным AI-агентам доступ к разрешённым
корпоративным данным в рамках полномочий человека, от имени которого они
действуют. Полностью on-premise развёртывание — корпоративные данные не
покидают периметр заказчика. Строится как практическая демонстрация
platform-архитектуры, governance, access control и auditability для
интеграции AI с данными.

## Действующие лица

- **Человек (subject)** — пользователь, чьи права доступа применяются. Роли
  для v0.1: `SalesManagerEurope`, `GlobalAnalyst`, `SupportAgent`.
- **AI-агент (actor)** — AI-клиент, действующий от имени человека (например,
  sales copilot). Всегда отдельная от subject идентичность в токене.

## Сценарий использования (v0.1)

AI sales copilot отвечает на вопросы вида «Какие у нас продажи по странам в
этом квартале?», запрашивая published resource `SalesSummary` через Gateway,
в рамках того, что действующему человеку разрешено видеть.

## Границы доверия

- Две независимые базы PostgreSQL:
  - **gateway-db** (Platform Store): policies, конфигурация published
    resources, audit log. Собственная роль Gateway имеет здесь
    чтение/запись.
  - **demo-corporate-db** (Corporate Data Source): демо-база Chinook,
    используется без изменений, имитирует реальные CRM/sales-данные
    клиента.
- Gateway подключается к demo-corporate-db под выделенной SELECT-only ролью
  (`pdg_reader`), с `SELECT` только на ограничивающую вьюху — без прав на
  базовые таблицы. Фильтрация по колонкам/строкам обеспечивается на уровне
  БД, а не только в коде приложения.
- AI-агент никогда не получает прямые credentials к БД и не выполняет raw
  SQL; весь доступ идёт через API Gateway.

## Что входит в v0.1

**Данные**
- Корпоративные сущности, без изменений из Chinook: `Customer`, `Invoice`,
  `InvoiceLine`.
- Published Resource: `SalesSummary`.
- Отдаваемые поля: `CustomerId`, `Country`, `InvoiceDate`, `Total`.
- Явно скрытые поля: `Address`, `PostalCode`, `Phone`, `Fax`, `Email`.

**AuthN / AuthZ**
- JWT bearer-аутентификация. Делегирование моделируется по RFC 8693: `sub` =
  человек, `act.sub` = AI-агент.

  ```json
  { "sub": "user_42", "act": { "sub": "sales_copilot_v1" }, "scope": "sales.read" }
  ```

- Токены выпускает in-process test issuer только для демонстрации (без
  полного token-exchange grant по RFC 8693, без внешнего IdP в v0.1).
- Policy-матрица:

  | Роль | Доступ к `SalesSummary` |
  |---|---|
  | `SalesManagerEurope` | Строки, где `Country` входит в статический allow-list Европы |
  | `GlobalAnalyst` | Все строки |
  | `SupportAgent` | Доступа нет — resource-level deny |

**Deny-сценарии (нужны оба)**
- Resource-level: `SupportAgent → SalesSummary` → `403` + запись в audit.
- Row-scope: `SalesManagerEurope → строка не-европейской страны` → `403` +
  запись в audit.

**Audit**
- Каждое решение `ALLOW` и `DENY` логируется: timestamp, subject, actor,
  capability, resource, scope, решение, причина, количество строк.
- Append-only на уровне приложения (нет endpoint'а на update/delete). Не
  криптографически защищённый от подделки лог — это будущая задача, здесь
  не заявляется.

**Инфраструктура**
- Docker Compose: сервис gateway + gateway-db + demo-corporate-db, запуск
  одной командой.
- Структура solution: `Api / Application / Domain / Infrastructure / Tests`.
  Application зависит только от портов (`IPolicyEngine`, порт чтения
  published resource, `IAuditWriter`); только Postgres-адаптер знает про
  ограничивающую вьюху.
- Unit-тесты: xUnit + Moq, policy/application-логика.
- Integration-тесты: оба deny-сценария выше.
- README: назначение, архитектура, границы доверия, инструкция запуска
  одной командой, атрибуция Chinook (лицензия MIT, сохранить `LICENSE.md`),
  явная заметка, что production требует настоящего Identity Provider — test
  issuer только для демо.

## Что не входит в v0.1

MediatR, Redis, RabbitMQ, GraphQL, BenchmarkDotNet, Keycloak, внешние
SaaS-коннекторы, адаптер под SQL Server, PostgreSQL Row-Level Security,
полный token-exchange grant по RFC 8693, tamper-proof/immutable audit log,
любой Published Resource кроме `SalesSummary`.

## Цепочка запроса

```
Человек (sub) → AI-агент (act.sub) → проверка JWT → resource policy → row scope
→ порт Published Resource → Postgres-адаптер → ограничивающая вьюха / SELECT-only роль
→ безопасная проекция → ответ → audit ALLOW/DENY
```
