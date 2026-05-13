-- Скрипт развёртывания БД «FinanceDubrovsky» (эталонная схема в 3НФ).
-- При работе приложения с EF6 первая инициализация создаёт базу автоматически (CreateDatabaseIfNotExists).
-- Этот файл можно выполнить вручную для справки или после удаления БД.

/*
Основные таблицы:
Roles, Users, UserRoles,
Accounts, Counterparties, DocumentTypes, DocumentStatuses, BudgetItems,
FinancialDocuments, AccountingEntries, DocumentBudgetAllocations,
AuditLogs, AppSettings
*/

IF DB_ID(N'FinanceDubrovsky') IS NULL
    CREATE DATABASE [FinanceDubrovsky];
GO
