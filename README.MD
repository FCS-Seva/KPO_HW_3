
# Домашняя работа №3. Синхронное межсервисное взаимодействие.
# Система проверки студенческих работ

## 1. Краткое описание архитектуры

Система реализована как набор микросервисов на **C# / ASP.NET Core**, с использованием **PostgreSQL** и контейнеризацией через **Docker**.

### 1.1. Компоненты

- **Gateway.Api** (`gateway` в docker-compose)
    - Единственная точка входа для клиента.
    - Принимает работы студентов, вызывает внутренние сервисы, агрегирует результаты.
    - Публичное API:
        - `POST /api/submissions` (сохраняет файл работы и создаёт запись о сдаче)
        - `GET /api/submissions/{submissionId}` (возвращает сведения о сдаче по id: студент, задание, имя/путь файла, время)
        - `GET /api/assignments/{assignmentCode}/reports` (возвращает список всех сдач по заданию с результатами проверки и признаками плагиата)

- **FileService.Api** (`file-service`)
    - Хранит файлы и метаданные о сдачах.
    - Сохраняет файл в общий Docker volume `/files`.
    - Использует БД `appdb`, таблица `Submissions`.
    - Внутреннее API:
        - `POST /internal/files` (сохраняет файл работы и создаёт запись о сдаче)
        - `GET /internal/submissions/{submissionId}` (возвращает сведения о сдаче по id: студент, задание, имя/путь файла, время)

- **AnalysisService.Api** (`analysis-service`)
    - Анализирует текст работы, определяет плагиат, формирует отчёты и ссылку на облако слов.
    - Читает файлы из общего volume `/files`.
    - Использует БД `appdb`, таблица `Reports`.
    - Генерирует URL облака слов через внешний сервис QuickChart Word Cloud.
    - Внутреннее API:
        - `POST /internal/analyze` (запускает анализ работы по `submissionId` и `filePath`, сохраняет отчёт)
        - `GET /internal/reports/{submissionId}` (возвращает отчёт анализа по конкретной сдаче)
        - `GET /internal/reports/by-assignment/{assignmentCode}` (возвращает список отчётов по заданию/контрольной)
        - `GET /api/submissions/{submissionId}/wordcloud` (PNG-изображение облака слов)

### 1.2. Хранилище данных и запуск

- **PostgreSQL (`postgres`)**
    - Одна БД: `appdb`.
    - В ней таблицы:
        - `Submissions` — метаданные сдач (ID, студент, код задания, имя файла, путь, дата).
        - `Reports` — результаты анализа (статус, флаг плагиата, совпавшая сдача, ссылка на облако слов, нормализованный текст и т.п.).
- **Файлы**
    - Docker volume `filesdata`, смонтирован как `/files` в контейнерах:
        - `file-service`
        - `analysis-service`

- **Запуск**
    - Все сервисы и БД описаны в `docker-compose.yml`.
    - Команда запуска:
      ```bash
      docker compose up --build
      ```
    - Порты:
        - Gateway.Api: `http://localhost:8080`
        - FileService.Api: `http://localhost:8081`
        - AnalysisService.Api: `http://localhost:8082`
    - Во всех сервисах включён Swagger:
        - `http://localhost:8080/swagger`
        - `http://localhost:8081/swagger`
        - `http://localhost:8082/swagger`

---

## 2. Пользовательские сценарии и взаимодействие микросервисов

### 2.1. Загрузка работы (студент)

**Цель:** принять файл, сохранить сдачу, выполнить анализ и вернуть результат.

1. Клиент → **Gateway.Api**  
   `POST /api/submissions` (загрузка вместе с параметрами: `file`, `studentId`, `assignmentCode`).

2. **Gateway.Api → FileService.Api**  
   `POST /internal/files` (загрузка файла вместе с параметрами `studentId` и `assignmentCode`).  
   FileService:
    - сохраняет файл в `/files/{SubmissionId}.ext`;
    - создаёт запись в таблице `Submissions` (БД `appdb`);
    - возвращает `submissionId`, `createdAt`, `filePath`.

3. **Gateway.Api → AnalysisService.Api**  
   `POST /internal/analyze`:  
   `submissionId`, `studentId`, `assignmentCode`, `createdAt`, `filePath`.

4. **AnalysisService.Api**:
    - читает файл `/files/{filePath}`;
    - нормализует текст (нижний регистр, удаление пунктуации и т.п.);
    - ищет в таблице `Reports` (БД `appdb`) предыдущие сдачи по тому же `assignmentCode` с тем же нормализованным текстом и другим `studentId`;
    - при совпадении:
        - `PlagiarismFlag = true`,
        - `MatchedSubmissionId = id первой сдачи`;
    - формирует `WordCloudUrl` через QuickChart;
    - сохраняет отчёт в `Reports` со статусом `Completed` или `Error`;
    - отдаёт результат анализа Gateway.

5. **Gateway.Api → Клиент**  
   Возвращает `201 Created` с агрегированным результатом:
    - `submissionId`, `studentId`, `assignmentCode`, `createdAt`;
    - `analysisStatus`, `plagiarismFlag`, `matchedSubmissionId`, `wordCloudUrl`, `errorMessage`.

При недоступности AnalysisService:
- файл всё равно сохраняется FileService;
- Gateway возвращает `analysisStatus = Error`, но сдача зафиксирована.

---

### 2.2. Получение отчётов по заданию (преподаватель)

**Цель:** увидеть список всех сдач по контрольной работе и флаги плагиата.

1. Клиент → **Gateway.Api**  
   `GET /api/assignments/{assignmentCode}/reports`.

2. **Gateway.Api → AnalysisService.Api**  
   `GET /internal/reports/by-assignment/{assignmentCode}`.

3. **AnalysisService.Api**:
    - читает из `Reports` (БД `appdb`) все записи с заданным `assignmentCode`;
    - возвращает список:
        - `submissionId`, `studentId`, `createdAt`,
        - `plagiarismFlag`, `matchedSubmissionId`, `wordCloudUrl`.

4. **Gateway.Api → Клиент**  
   Проксирует список отчётов.

---

### 2.3. Детальная информация по конкретной сдаче

**Цель:** получить сведения о сдаче и подробный результат анализа.

1. Клиент → **Gateway.Api**  
   `GET /api/submissions/{submissionId}`.

2. **Gateway.Api → FileService.Api**  
   `GET /internal/submissions/{submissionId}` — сведения о сдаче работы (кто, что, когда сдал, имя и путь к файлу).

3. **Gateway.Api → AnalysisService.Api**  
   `GET /internal/reports/{submissionId}` — статус анализа, флаг плагиата, `WordCloudUrl`.

4. **Gateway.Api → Клиент**  
   Объединяет данные и возвращает единый объект `SubmissionDetailsResponse`.

---

### 2.4. Получение изображения облака слов

**Цель:** отдать готовую картинку облака слов по `submissionId`.

1. Клиент → **AnalysisService.Api**  
   `GET /api/submissions/{submissionId}/wordcloud`.

2. **AnalysisService.Api**:
    - находит запись в `Reports` по `submissionId`, берёт `WordCloudUrl`;
    - делает запрос к сервису QuickChart;
    - возвращает байты картинки с корректным `Content-Type`.

Если отчёт или `WordCloudUrl` отсутствуют, возвращается `404 Not Found`.
