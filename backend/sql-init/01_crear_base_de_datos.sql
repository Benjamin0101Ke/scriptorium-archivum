-- Compose crea la base indicada en POSTGRES_DB con UTF8
-- Para crearla manualmente, ejecutar lo siguiente conectado a postgres
-- CREATE DATABASE scriptorium_archivum WITH ENCODING 'UTF8' TEMPLATE template0;

ALTER DATABASE scriptorium_archivum
    SET TimeZone TO 'America/El_Salvador';

SET TIME ZONE 'America/El_Salvador';
