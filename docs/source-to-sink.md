# Власна карта source-to-sink

Поле: Incident.Description. Стан/commit: заповніть. Вхід/Guid запису: заповніть.

|Крок|Файл та функція/DTO|Конкретне поле/значення|Доказ і довіра|
|-|-|-|-|
|Source|Client/browser-lab.html,форма створення інциденту|Поле опису Incident.Description, у яке вводиться тестовий HTML-маркер|Дані вводить користувач, тому вони є недовіреними|
|Request|Client/browser-lab.js, обробник створення інциденту. POST /api/incidents|JSON із полем description, що містить XSS-маркер|У DevTools Network отримано HTTP 201 Created. Передавання JSON не робить вміст безпечним HTML|
|Validation/mapping|Серверний endpoint створення інциденту та відповідний DTo|description із запиту перетворюється на Incident.Description |Сервер обробляє та зберігає опис. Перевірка даних не означає, що її можна вставляти в DOM як HTML|
|Storage|PostgreSQL, таблиця identity\_incidents, колонка description|Збережений текст із <img ... onerror=...>|Дані залишаються недовіреними після збереження в БД|
|Response|Application/Incidents/IncidentQueries.cs, GetDetailsAsync; IncidentDetailsResponse|Incident.Description -> JSON-поле description|GET запит повернув HTTP 200 ОК. JSON передав текст, але не визначає спосіб його відображення браузером|
|Client transform|Client/browser-ab.js, обробник отримання й відображення деталей інциденту|Значення description із JSON передається елементу сторінки|Якщо передати недовірений текст до HTML parser, можливе виконання XSS|
|Sink/context|Client/browser-lab.js, елемент опису інциденту|До виправлення - innerHTML; Після виправлення - textContent|innerHTML інтерпретує рядок як HTML. textContent відображає його як звичайний текст без виконання HTML обробників подій|

Інвентаризація: файл/функція → категорія даних → контекст → обґрунтоване рішення. Для «Добре / Відмінно» додайте повний другий потік IncidentComment.Text за тією самою таблицею. Для «Відмінно» додайте результат поглибленої перевірки comments/повторного render. Рівні накопичувальні; основний Description потрібний для всіх.