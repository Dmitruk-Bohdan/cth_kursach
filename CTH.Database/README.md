## Database Setup

1. **Drop schema (optional)**  
   `UseCases/Schema/DropAllTables.sql`

2. **Create schema**  
   `UseCases/Schema/CreateAllTables.sql`

3. **Seed reference data** (в указанном порядке)  
   - `UseCases/Seeds/RolesSeed.sql`  
   - `UseCases/Seeds/SubjectsSeed.sql`

4. **Fix sequences (если данные вставлялись вручную с явным ID)**  
   `UseCases/Schema/FixSequences.sql`  
   Выполните этот скрипт, если возникают ошибки "duplicate key value violates unique constraint" при создании новых записей.

После этого можно запускать приложение и использовать SQL use-case'ы.
