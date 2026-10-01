# Plan: Catalogo de numeros de parte con familia y subfamilia

## Objetivo

Agregar un catalogo claro para crear y administrar numeros de parte con estos datos:

- Num parte
- BU
- Familia
- SubFamilia

La familia debe existir antes de asignarla a un numero de parte. La subfamilia tambien debe existir antes de asignarla, y debe pertenecer a la familia seleccionada.

## Contexto actual

El sistema ya tiene un catalogo de numeros de parte en `planning.PartNumbers`, expuesto en `Catalogos` como "Productos y numeros de parte".

Hoy tiene estos campos principales:

- `PartNumber`
- `Description`
- `BusinessUnit`
- `ProductFamily`
- `DefaultYield`
- `IsActive`

Tambien ya existe un catalogo SMT de familias en `planning.SmtFamilies`, usado por la administracion de SMT. No existe todavia un catalogo formal de subfamilias.

## Supuestos

- Este cambio es para SMT, pero debe reutilizar el catalogo existente de numeros de parte para no duplicar informacion.
- `Familia` debe venir del catalogo SMT de familias activas.
- `SubFamilia` sera obligatoria para los numeros de parte que pertenezcan al flujo SMT.
- La subfamilia debe existir en catalogo antes de asignarla; no se debe capturar como texto libre.
- La subfamilia debe poder usarse para agrupar y filtrar informacion SMT.
- `Descripcion` y `DefaultYield` no se eliminan porque ya existen y pueden ser usados por otras partes del sistema; se pueden dejar como campos opcionales o avanzados.

## Cambios propuestos

### 1. Base de datos

Crear script nuevo `database/012_add_part_subfamilies.sql`.

Acciones:

- Crear tabla `planning.SubFamilies`.
- Agregar columna `SubFamily` a `planning.PartNumbers`.
- Crear indice para busquedas por `ProductFamily` y `SubFamily`.
- Normalizar los valores de familia/subfamilia al guardar para evitar diferencias por mayusculas, minusculas o espacios.
- Agregar restricciones de unicidad:
  - Una subfamilia no se repite dentro de la misma familia.
  - Un numero de parte sigue siendo unico.

Tabla propuesta:

```sql
planning.SubFamilies
(
    SubFamilyId int identity primary key,
    ProductFamily nvarchar(120) not null,
    SubFamilyName nvarchar(120) not null,
    Notes nvarchar(240) null,
    IsActive bit not null,
    CreatedAtUtc datetime2(0) not null,
    UpdatedAtUtc datetime2(0) not null
)
```

### 2. Modelo de datos .NET

Actualizar entidades y `DbContext`.

Archivos:

- `ProductionCapacity.Data/Entities/PlanningEntities.cs`
- `ProductionCapacity.Data/PlanningDbContext.cs`

Cambios:

- Agregar `SubFamily` a `PartNumberEntity`.
- Crear `SubFamilyEntity`.
- Agregar `DbSet<SubFamilyEntity>`.
- Mapear tabla, longitudes, indices y restricciones.

### 3. Servicio de catalogos

Actualizar `ProductionCapacity.Data/CatalogAdministrationService.cs`.

Cambios:

- Incluir `SubFamilies` en `CatalogSnapshot`.
- Crear `SubFamilyCatalogItem`.
- Crear metodo `SaveSubFamilyAsync`.
- Actualizar `PartNumberCatalogItem` con `SubFamily`.
- Validar al guardar numero de parte:
  - `PartNumber` requerido.
  - `BU` requerido.
  - `ProductFamily` requerida y existente en familias SMT activas.
  - `SubFamily` requerida y existente para esa familia.
  - La comparacion debe ser insensible a mayusculas/minusculas para evitar duplicados humanos.
  - Evitar duplicados.

### 4. Pantalla de catalogos

Actualizar `ProductionCapacity/Components/Pages/Catalogs.razor`.

Cambios:

- Renombrar tab actual de "Productos" a "Numeros de parte".
- Agregar una seccion/tab nueva "Subfamilias".
- En "Numeros de parte":
  - Campo `Num parte`.
  - Campo `BU`.
  - Selector/combobox de `Familia`.
  - Selector/combobox de `SubFamilia`, filtrado por familia.
  - Mensaje si no hay familias: "Primero agrega una familia SMT".
  - Mensaje si no hay subfamilias para esa familia: "Primero agrega una subfamilia".
- Mantener `Descripcion`, `Yield default` y `Activo` como campos secundarios si siguen siendo utiles.

### 5. Experiencia de usuario

Mejoras necesarias:

- Que el flujo sea: Familias SMT -> Subfamilias -> Numeros de parte.
- Evitar texto libre para familia y subfamilia mediante combobox/listas controladas.
- Mostrar mensajes de error entendibles:
  - "La familia seleccionada no existe o esta inactiva."
  - "La subfamilia seleccionada no pertenece a esa familia."
  - "El numero de parte ya existe."
- Agregar conteos por familia/subfamilia para que el usuario vea si ya capturo informacion.
- En pantallas/reportes SMT, permitir filtrar por subfamilia y agrupar resultados por subfamilia.

### 6. Importacion de datos

Actualizar plantilla:

- `import-templates/part_numbers.csv`

Nuevo encabezado sugerido:

```csv
PartNumber,Description,BusinessUnit,ProductFamily,SubFamily,DefaultYield
```

Actualizar parser/importador para aceptar `SubFamily`.

Regla de importacion:

- La importacion puede crear subfamilias automaticamente cuando vengan en el archivo Excel/CSV.
- La subfamilia importada debe normalizarse igual que el catalogo manual para evitar duplicados por mayusculas, minusculas o espacios.
- Si el archivo no trae subfamilia para numeros de parte SMT, debe dejar advertencia o bloqueo segun el flujo de importacion aplicable.

### 7. Pruebas

Actualizar/agregar pruebas en:

- `ProductionCapacity.Data.Tests/CatalogAdministrationServiceTests.cs`
- Pruebas del importador CSV/Excel si existen para `part_numbers.csv`.

Casos minimos:

- Puede crear subfamilia para una familia existente.
- No puede crear subfamilia sin familia.
- Puede crear numero de parte con BU, familia y subfamilia validas.
- No puede crear numero de parte con familia inexistente.
- No puede crear numero de parte con subfamilia que no pertenece a la familia.
- Crea subfamilias automaticamente desde importacion Excel/CSV.
- Puede filtrar y agrupar informacion SMT por subfamilia.
- La plantilla `part_numbers.csv` importa `SubFamily`.

### 8. Compatibilidad con datos actuales

Como ya hay numeros de parte sin `SubFamily`, el script debe permitir `NULL` inicialmente.

Luego hay dos caminos:

- Migracion suave: dejar `SubFamily` nullable para datos viejos y hacerla obligatoria solo en UI para nuevos registros.
- Migracion estricta: crear subfamilia temporal "SIN DEFINIR" por familia y rellenar registros actuales.

Recomendacion: migracion suave ahora, estricta despues si el negocio quiere limpieza total.

## Definiciones confirmadas

1. `SubFamilia` es obligatoria para numeros de parte SMT.
2. Las subfamilias si se pueden crear automaticamente desde importacion Excel/CSV.
3. Familia y subfamilia se eligen desde combobox/listas controladas.
4. SMT debe permitir agrupar y filtrar por subfamilia.

## Falta confirmar

1. Si el campo `Descripcion` debe seguir visible al usuario o quedar como opcional/avanzado.
2. Si `SubFamilia` debe ser obligatoria tambien para numeros de parte que no sean SMT.

## Orden de implementacion recomendado

1. Crear script SQL `012_add_part_subfamilies.sql`.
2. Actualizar entidades y `DbContext`.
3. Actualizar `CatalogAdministrationService`.
4. Actualizar pantalla de Catalogos con tab de Subfamilias y selectores.
5. Actualizar `part_numbers.csv` y parser de importacion.
6. Agregar pruebas.
7. Compilar con `dotnet build`.
8. Ejecutar pruebas con `dotnet test`.
