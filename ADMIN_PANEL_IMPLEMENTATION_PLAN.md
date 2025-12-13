# План реализации админ-панели (модерация контента)

## Обзор
Админ-панель предназначена для модерации контента системы тестирования. Упрощенный функционал стандартной Django Admin панели - только CRUD операции для основных сущностей.

## Текущее состояние
- ✅ Роль Admin уже существует в системе (RoleTypeEnum.Admin = 3)
- ✅ Администраторы могут создавать/редактировать/удалять state archive тесты
- ✅ Администраторы могут редактировать любые задания (проверка isAdmin в UpdateTaskAsync)
- ✅ Есть таблица audit_log для логирования действий
- ✅ Проект `CTH.AdminPanelClient` уже создан
- ❌ Нет реализации клиента
- ❌ Нет API endpoints для административных функций
- ❌ Нет сервисов для административных операций

## Архитектура

### 1. Клиентское приложение
**Файл:** `CTH.AdminPanelClient/AdminPanelClientApp.cs`
- Консольное приложение аналогично TeacherWebClient и MobileClient
- Авторизация с проверкой роли Admin
- Главное меню с разделами для модерации контента

### 2. API Endpoints
**Файл:** `CTH.Api/Controllers/AdminController.cs`
- Все endpoints требуют роль Admin
- Использует `[Authorize]` с проверкой роли

### 3. Сервисы
**Файл:** `CTH.Services/Interfaces/IAdminService.cs`
**Файл:** `CTH.Services/Implementations/AdminService.cs`
- Бизнес-логика для административных операций модерации

### 4. Репозитории
- Использование существующих репозиториев
- Возможно, добавление методов для административных запросов

## Функциональность (только модерация контента)

### Раздел 1: Управление пользователями

#### 1.1 Просмотр пользователей
- **API:** `GET /admin/users`
- **Функции:**
  - Список всех пользователей с фильтрацией по роли
  - Поиск по имени, email
  - Пагинация
  - Отображение: ID, имя, email, роль, дата регистрации, последний вход

#### 1.2 Создание пользователя
- **API:** `POST /admin/users`
- **Функции:**
  - Создание пользователя с указанием роли
  - Установка пароля
  - Валидация email (уникальность)

#### 1.3 Редактирование пользователя
- **API:** `PUT /admin/users/{userId}`
- **Функции:**
  - Изменение имени, email
  - Изменение роли
  - Сброс пароля

#### 1.4 Блокировка/разблокировка пользователя
- **API:** `PUT /admin/users/{userId}/block` - блокировка
- **API:** `PUT /admin/users/{userId}/unblock` - разблокировка
- **Функции:**
  - Блокировка пользователя (через удаление всех сессий)
  - Разблокировка пользователя (разрешение входа)
  - Примечание: в user_account нет поля is_active, блокировка через удаление сессий из user_sessions

#### 1.5 Удаление пользователя
- **API:** `DELETE /admin/users/{userId}`
- **Функции:**
  - Полное удаление пользователя
  - Проверка зависимостей (тесты, попытки, статистика)
  - Предупреждение о последствиях

### Раздел 2: Управление предметами

#### 2.1 Просмотр предметов
- **API:** `GET /admin/subjects`
- **Функции:**
  - Список всех предметов
  - Поиск по названию, коду
  - Фильтрация по активности
  - Пагинация

#### 2.2 Создание предмета
- **API:** `POST /admin/subjects`
- **Функции:**
  - Создание предмета
  - Установка названия, кода
  - Установка статуса активности

#### 2.3 Редактирование предмета
- **API:** `PUT /admin/subjects/{subjectId}`
- **Функции:**
  - Изменение названия, кода
  - Изменение статуса активности

#### 2.4 Удаление предмета
- **API:** `DELETE /admin/subjects/{subjectId}`
- **Функции:**
  - Удаление предмета
  - Проверка зависимостей (темы, задания, тесты)

### Раздел 3: Управление темами

#### 3.1 Просмотр тем
- **API:** `GET /admin/subjects/{subjectId}/topics` - темы предмета
- **API:** `GET /admin/topics` - все темы
- **Функции:**
  - Список тем с фильтрацией по предмету
  - Поиск по названию, коду
  - Фильтрация по активности
  - Отображение иерархии (parent_id)
  - Пагинация

#### 3.2 Создание темы
- **API:** `POST /admin/subjects/{subjectId}/topics`
- **Функции:**
  - Создание темы
  - Привязка к предмету
  - Установка родительской темы (для иерархии)
  - Установка названия, кода
  - Установка статуса активности

#### 3.3 Редактирование темы
- **API:** `PUT /admin/topics/{topicId}`
- **Функции:**
  - Изменение названия, кода
  - Изменение предмета
  - Изменение родительской темы
  - Изменение статуса активности

#### 3.4 Удаление темы
- **API:** `DELETE /admin/topics/{topicId}`
- **Функции:**
  - Удаление темы
  - Проверка зависимостей (дочерние темы, задания)

### Раздел 4: Управление заданиями

#### 4.1 Просмотр заданий
- **API:** `GET /admin/tasks`
- **Функции:**
  - Список всех заданий с фильтрацией:
    - По предмету
    - По теме
    - По типу задания
    - По сложности
    - По активности
  - Поиск по условию задания
  - Пагинация
  - Сортировка

#### 4.2 Создание задания
- **API:** `POST /admin/tasks`
- **Функции:**
  - Создание задания
  - Установка всех параметров: предмет, тема, тип, сложность, условие, правильный ответ, объяснение
  - Привязка к exam_source (опционально)
  - Установка статуса активности

#### 4.3 Редактирование задания
- **API:** `PUT /admin/tasks/{taskId}`
- **Функции:**
  - Редактирование любого задания
  - Изменение всех параметров

#### 4.4 Активация/деактивация задания
- **API:** `PUT /admin/tasks/{taskId}/activate` - активация
- **API:** `PUT /admin/tasks/{taskId}/deactivate` - деактивация
- **Функции:**
  - Активация/деактивация задания (is_active)

#### 4.5 Удаление задания
- **API:** `DELETE /admin/tasks/{taskId}`
- **Функции:**
  - Удаление задания
  - Проверка использования в тестах
  - Предупреждение о последствиях

### Раздел 5: Управление тестами

#### 5.1 Просмотр тестов
- **API:** `GET /admin/tests`
- **Функции:**
  - Список всех тестов с фильтрацией:
    - По предмету
    - По типу теста (CUSTOM, PAST_EXAM, MIXED)
    - По автору
    - По статусу публикации
    - По state archive
  - Поиск по названию
  - Пагинация

#### 5.2 Создание теста
- **API:** `POST /admin/tests`
- **Функции:**
  - Создание любого типа теста
  - Создание state archive тестов
  - Установка всех параметров

#### 5.3 Редактирование теста
- **API:** `PUT /admin/tests/{testId}`
- **Функции:**
  - Редактирование любого теста
  - Изменение всех параметров включая state archive статус
  - Изменение состава заданий (через test_task)

#### 5.4 Управление заданиями в тесте
- **API:** `GET /admin/tests/{testId}/tasks` - список заданий теста
- **API:** `POST /admin/tests/{testId}/tasks` - добавление задания в тест
- **API:** `DELETE /admin/tests/{testId}/tasks/{taskId}` - удаление задания из теста
- **API:** `PUT /admin/tests/{testId}/tasks/reorder` - изменение порядка заданий
- **Функции:**
  - Просмотр заданий теста
  - Добавление/удаление заданий
  - Изменение порядка и веса заданий

#### 5.5 Удаление теста
- **API:** `DELETE /admin/tests/{testId}`
- **Функции:**
  - Удаление теста
  - Проверка зависимостей (попытки)

### Раздел 6: Управление справочниками

#### 6.1 Управление exam_source (источники экзаменов)
- **API:** `GET /admin/exam-sources` - список источников
- **API:** `POST /admin/exam-sources` - создание источника
- **API:** `PUT /admin/exam-sources/{sourceId}` - редактирование
- **API:** `DELETE /admin/exam-sources/{sourceId}` - удаление
- **Функции:**
  - Создание, редактирование, удаление источников экзаменов
  - Просмотр привязанных заданий

## Покрытие CRUD операций

### ✅ Полный CRUD (Create, Read, Update, Delete):
1. **user_account** - пользователи
2. **subject** - предметы
3. **topic** - темы
4. **task_item** - задания
5. **test** - тесты
6. **exam_source** - источники экзаменов

### ✅ Управление через связанные сущности:
- **test_task** - управление заданиями в тесте (через раздел "Управление тестами")

## Структура файлов

### Backend

```
CTH.Services/
├── Interfaces/
│   └── IAdminService.cs
├── Implementations/
│   └── AdminService.cs
└── Models/
    └── Dto/
        └── Admin/
            ├── UserListItemDto.cs
            ├── CreateUserRequestDto.cs
            ├── UpdateUserRequestDto.cs
            ├── SubjectListItemDto.cs
            ├── CreateSubjectRequestDto.cs
            ├── UpdateSubjectRequestDto.cs
            ├── TopicListItemDto.cs
            ├── CreateTopicRequestDto.cs
            ├── UpdateTopicRequestDto.cs
            ├── TaskListItemDto.cs (расширить существующий)
            ├── CreateTaskRequestDto.cs (использовать существующий)
            ├── UpdateTaskRequestDto.cs (использовать существующий)
            ├── TestListItemDto.cs (расширить существующий)
            ├── CreateTestRequestDto.cs (использовать существующий)
            ├── UpdateTestRequestDto.cs (использовать существующий)
            ├── ExamSourceDto.cs
            ├── CreateExamSourceRequestDto.cs
            └── UpdateExamSourceRequestDto.cs

CTH.Api/
└── Controllers/
    └── AdminController.cs

CTH.Database/
└── UseCases/
    └── AdminUseCases/
        ├── Queries/
        │   ├── GetAllUsers.sql
        │   ├── GetAllSubjects.sql
        │   ├── GetAllTopics.sql
        │   ├── GetAllTasks.sql
        │   ├── GetAllTests.sql
        │   └── GetAllExamSources.sql
        └── Commands/
            ├── CreateUser.sql
            ├── UpdateUser.sql
            ├── DeleteUser.sql
            ├── BlockUser.sql (удаление всех сессий)
            ├── CreateSubject.sql
            ├── UpdateSubject.sql
            ├── DeleteSubject.sql
            ├── CreateTopic.sql
            ├── UpdateTopic.sql
            ├── DeleteTopic.sql
            ├── CreateTask.sql (использовать существующий)
            ├── UpdateTask.sql (использовать существующий)
            ├── DeleteTask.sql
            ├── CreateTest.sql (использовать существующий)
            ├── UpdateTest.sql (использовать существующий)
            ├── DeleteTest.sql
            ├── CreateExamSource.sql
            ├── UpdateExamSource.sql
            └── DeleteExamSource.sql
```

### Frontend (Admin Panel Client)

```
CTH.AdminPanelClient/
├── AdminPanelClientApp.cs
├── ApiClient.cs
└── Program.cs
```

## Этапы реализации

### Этап 1: Базовая инфраструктура (Приоритет: Высокий)
1. Создать AdminPanelClientApp.cs с авторизацией
2. Создать ApiClient.cs для AdminPanelClient
3. Создать IAdminService и AdminService
4. Создать AdminController с базовыми endpoints
5. Реализовать главное меню

### Этап 2: Управление пользователями (Приоритет: Высокий)
1. Просмотр пользователей
2. Создание пользователя
3. Редактирование пользователя
4. Блокировка/разблокировка пользователя
5. Удаление пользователя

### Этап 3: Управление предметами (Приоритет: Высокий)
1. Просмотр предметов
2. Создание предмета
3. Редактирование предмета
4. Удаление предмета

### Этап 4: Управление темами (Приоритет: Высокий)
1. Просмотр тем
2. Создание темы
3. Редактирование темы
4. Удаление темы

### Этап 5: Управление заданиями (Приоритет: Высокий)
1. Просмотр заданий
2. Создание задания
3. Редактирование задания
4. Активация/деактивация задания
5. Удаление задания

### Этап 6: Управление тестами (Приоритет: Высокий)
1. Просмотр тестов
2. Создание теста
3. Редактирование теста
4. Управление заданиями в тесте
5. Удаление теста

### Этап 7: Управление справочниками (Приоритет: Средний)
1. CRUD для exam_source

## Дополнительные соображения

### Безопасность
- Все endpoints требуют роль Admin
- Логирование всех административных действий в audit_log
- Валидация всех входных данных
- Проверка зависимостей перед удалением

### Производительность
- Пагинация для больших списков
- Индексы в БД для частых запросов

### UX
- Понятные сообщения об ошибках
- Подтверждение для деструктивных операций
- Фильтры и поиск везде, где нужно
- Простой интерфейс как в Django Admin

## SQL запросы для реализации

### Получение всех пользователей
```sql
SELECT 
    ua.id,
    ua.user_name,
    ua.email,
    r.role_name,
    ua.last_login_at,
    ua.created_at
FROM user_account ua
JOIN role r ON r.id = ua.role_type_id
ORDER BY ua.created_at DESC;
```

### Получение всех предметов
```sql
SELECT 
    id,
    subject_code,
    subject_name,
    is_active,
    created_at,
    updated_at
FROM subject
ORDER BY subject_name;
```

### Получение всех тем
```sql
SELECT 
    t.id,
    t.subject_id,
    s.subject_name,
    t.topic_name,
    t.topic_code,
    t.topic_parent_id,
    t.is_active,
    t.created_at,
    t.updated_at
FROM topic t
JOIN subject s ON s.id = t.subject_id
ORDER BY s.subject_name, t.topic_name;
```

### Получение всех заданий
```sql
SELECT 
    ti.id,
    ti.subject_id,
    s.subject_name,
    ti.topic_id,
    t.topic_name,
    ti.task_type,
    ti.difficulty,
    ti.statement,
    ti.is_active,
    ti.created_at,
    ti.updated_at
FROM task_item ti
JOIN subject s ON s.id = ti.subject_id
LEFT JOIN topic t ON t.id = ti.topic_id
ORDER BY ti.created_at DESC;
```

### Получение всех тестов
```sql
SELECT 
    test.id,
    test.subject_id,
    s.subject_name,
    test.test_kind,
    test.title,
    test.author_id,
    ua.user_name AS author_name,
    test.is_published,
    test.is_public,
    test.is_state_archive,
    test.created_at,
    test.updated_at
FROM test
JOIN subject s ON s.id = test.subject_id
LEFT JOIN user_account ua ON ua.id = test.author_id
ORDER BY test.created_at DESC;
```

### Получение всех источников экзаменов
```sql
SELECT 
    id,
    year,
    variant_number,
    issuer,
    notes,
    created_at,
    updated_at
FROM exam_source
ORDER BY year DESC, variant_number;
```

## Примечания

1. **Только модерация контента:** Админка предназначена исключительно для управления основными сущностями системы
2. **Простой интерфейс:** Как в Django Admin - списки, формы редактирования, базовые фильтры
3. **Переиспользование кода:** Где возможно, использовать существующие сервисы и репозитории
4. **Консистентность:** Следовать существующим паттернам в коде
5. **Логирование:** Все действия администратора должны логироваться в audit_log
6. **Валидация:** Все формы должны иметь валидацию данных
7. **Зависимости:** При удалении всегда проверять зависимости и предупреждать пользователя
8. **Блокировка пользователей:** Через удаление всех сессий из user_sessions (в user_account нет поля is_active)
