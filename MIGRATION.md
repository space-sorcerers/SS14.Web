# Database Migration Instructions

## Apply Birthday Column Migration

Run this SQL on your PostgreSQL database:

```sql
ALTER TABLE "AspNetUsers" 
ADD COLUMN "Birthday" timestamp with time zone NOT NULL 
DEFAULT '1000-01-01 00:00:00+00';
```

This adds the Birthday field to all users. Default value `1000-01-01` indicates "no birthday set" for legacy users.

## Verify Migration

```sql
SELECT "Id", "UserName", "Birthday" 
FROM "AspNetUsers" 
LIMIT 5;
```

All existing users should have Birthday = '1000-01-01 00:00:00+00'.

## Rollback (if needed)

```sql
ALTER TABLE "AspNetUsers" DROP COLUMN "Birthday";
```
