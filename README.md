# WebServiceCosmetics

## Проект и учебные требования

Проект реализует CRUD для продуктов, сырья и ингредиентов на ASP.NET Core MVC и SQL Server.

- HTTP: http://localhost:5099
- HTTPS: https://localhost:7218
- Пояснение модульности, связности/сцепления, CRUD, структуры и метрик: [docs/ARCHITECTURE_AND_COURSEWORK.md](docs/ARCHITECTURE_AND_COURSEWORK.md)

Удаление продукта удаляет связанные строки рецепта, но блокируется при наличии истории производства или продаж.