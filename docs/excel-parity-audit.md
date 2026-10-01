# Auditoria de paridad contra Excel

Fecha: 2026-09-13

Archivo revisado: `C:\Users\carl0\OneDrive\Documents\1) Cap model BE CISCO V1 170726.xlsb`

Objetivo: comparar el modelo Excel contra el sistema .NET/SQL Server actual para identificar que ya esta alineado, que falta para obtener las mismas cantidades y que se recomienda construir para que sea util como planeador de produccion manufacturera.

## Resumen ejecutivo

El sistema actual ya tiene una buena base: escenarios, semanas, maquinas, productos, rutas, demanda semanal, yield, factor de utilizacion, overrides por maquina/producto y restriccion fisica por racks/carros.

Sin embargo, todavia no esta en paridad 1:1 con el Excel. El Excel no es solo una calculadora de capacidad por ruta; funciona como una matriz semanal por operacion, con volumen por TAN PN, columnas de procesos, disponibilidad por equipo, tiempos de prueba derivados, capacidad de chambers/chasis/racks y varias formulas especificas por bloque. Para que ambos den las mismas cantidades hay que crear una fase de "paridad Excel" antes de confiar el sistema para planeacion completa.

## Lo que hace el Excel

### 1. Salida principal tipo matriz

La hoja `PLAN UABU` muestra:

- Filas por TAN PN y volumen.
- Columnas por operacion: ASSY, PACK, RFID, PCBST, PCB 2C, chambers, HIPOT, eORT, etc.
- Horas disponibles por operacion.
- Tiempo restante por operacion.
- Carga de cada producto por operacion calculada como volumen dividido entre el rate de backend.

Formula representativa:

```text
horas de carga = volumen / UPH+OEE de la operacion en Backend
```

Esto genera una vista muy natural para un planeador: que parte carga que proceso y donde se rompe la capacidad.

### 2. Disponibilidad por operacion/equipo

En `PLAN UABU`, la disponibilidad se calcula por columna de operacion:

```text
Available time = Days per week * Shift per day * Hrs per shift * Qty Equipment
```

Ejemplo observado:

```text
5.7 dias * 3 turnos * 7.5 horas * 2 equipos = 256.5 horas aprox.
```

El sistema actual calcula horas disponibles por semana, pero todavia no modela la cantidad de equipo por operacion/semana con el mismo peso que el Excel.

### 3. Backend con bloques repetidos de procesos

La hoja `Backend` contiene muchos bloques con columnas como:

- BU
- SKU NAME
- TAN PN
- Standard time
- UPH
- Yield
- Utilization
- OEE
- UPH+OEE
- MAX CAP
- Q HC FIXED
- UTILIZATION

Estos bloques son la fuente de los rates que usa `PLAN UABU`. Hay operaciones donde la logica no es solo sumar demanda; tambien se observan formulas con `MAXIFS`, lo que indica restricciones por grupo, chamber o configuracion.

### 4. Tiempos de prueba derivados

Las hojas `T.T. Long`, `T.T.Mb-Quake` y `T.T.NGFWBU` no solo guardan un tiempo estandar. Calculan segundos por unidad usando:

```text
TC x unidad = (test time min + handling min) / qty cell / qty equip * 60
```

El sistema actual guarda `StandardTimeSeconds` directamente. Eso sirve para MVP, pero para paridad falta capturar los componentes del tiempo de prueba y calcular el estandar.

### 5. Crossreference y 73-level PN

La hoja `Crossreference` mapea BU, SKU NAME, TAN PN y 73-level PN. El script SQL inicial ya creo una tabla `planning.PartCrossReferences`, pero el modelo EF y la pantalla de catalogos todavia no la exponen. Esto es importante para importar demanda o formulas que llegan con otro identificador distinto al TAN PN.

### 6. Chambers, chasis, racks, accesorios e inventarios

El Excel contiene secciones para:

- Qty Chambers.
- Tipos de chamber: Envirotronix, Vostron, Yinhe, Weiss dobles.
- Configuraciones por familia/producto.
- Chasis.
- WIP.
- Qty Chambers eORT.
- Chambers Available Time.
- Accesorios/chasis/golden segun notas de minuta.

El sistema actual ya tiene una restriccion generica de rack/carro:

```text
capacidad fisica = piezas por rack * racks disponibles
capacidad final = minimo(capacidad por tiempo, capacidad fisica)
racks requeridos = techo(demanda / piezas por rack)
```

Pero el Excel tiene mas detalle: chambers, chasis, configuraciones, WIP, accesorios especificos y algunos calculos por agrupacion.

## Cobertura actual del sistema

### Cubierto

- SQL Server como base.
- Escenarios.
- Semanas de produccion.
- Maquinas.
- Numeros de parte.
- Rutas/procesos por producto y maquina.
- Demanda semanal.
- Yield por producto.
- Factor de utilizacion por maquina.
- Overrides por escenario, maquina, producto y maquina-producto.
- Racks/carros, accesorios, configuracion producto-maquina-rack.
- Inventario semanal de racks por escenario.
- Calculo de capacidad por tiempo.
- Calculo de restriccion por rack.
- Catalogos base en la pagina.

### Parcial

- El Excel usa factor de utilizacion, yield y OEE. El sistema tiene yield y factor de utilizacion, pero no tiene un tercer factor explicito equivalente al OEE del Excel. Si se decide no llamarlo OEE, puede llamarse `Factor de eficiencia`, pero para paridad debe existir como dato separado o debe definirse que el factor de utilizacion absorbe ese valor.
- Existe tabla SQL para crossreferences, pero falta integrarla en EF, servicios y UI.
- Hay demanda semanal, pero no una pantalla tipo grid para capturar/importar muchas semanas y muchos productos.
- Hay rutas, pero no catalogo completo de operaciones por bloque de Excel ni matriz de resultado como `PLAN UABU`.

### No cubierto todavia

- Calculo multi-semana completo. El servicio actual toma la primera semana del escenario.
- Matriz de planeacion equivalente a `PLAN UABU`.
- Cantidad de equipo por operacion/semana como driver de disponibilidad.
- UPH, UPH+OEE, MAX CAP y Q HC FIXED como campos de calculo.
- Formulas con `MAXIFS` o reglas por familia/configuracion.
- Tiempos de prueba derivados desde test time, handling, qty cell y qty equip.
- Chambers/chasis como recursos fisicos de capacidad separados de racks.
- Inventario y necesidades de golden/accessories/chasis.
- Importacion directa del Excel.
- Pruebas automatizadas contra datos de ejemplo del Excel.

## Diferencias que pueden cambiar cantidades

1. El Excel calcula carga como `volumen / UPH+OEE`; el sistema calcula capacidad como `horas efectivas * 3600 / standard time`.
   - Pueden ser equivalentes solo si `UPH+OEE` se deriva exactamente de `standard time`, yield, utilizacion y factor de eficiencia.

2. El Excel multiplica horas disponibles por `Qty Equipment`.
   - El sistema no tiene una tabla de cantidad de equipos por operacion/semana.

3. El Excel trae datos de muchos bloques de Backend.
   - El sistema tiene rutas, pero no todas las columnas de esos bloques.

4. El Excel distingue test time, handling, qty cell y qty equip.
   - El sistema solo recibe tiempo estandar final.

5. El Excel tiene chambers/chasis/WIP.
   - El sistema solo tiene racks/carros genericos.

6. El Excel trabaja con muchas semanas.
   - El sistema calcula la primera semana cargada.

7. El Excel contiene algunas formulas con referencias `#REF!` en columnas posteriores de `PLAN UABU`.
   - Antes de copiar reglas al sistema hay que decidir si esas formulas se reparan, se ignoran o se reemplazan por reglas de negocio nuevas.

## Recomendacion de arquitectura

### Fase 1 - Paridad Excel controlada

Crear un modulo de comparacion con 10 a 20 casos representativos del Excel:

- 3 productos Longball.
- 3 productos Mobius/Quake.
- 2 productos NGFW.
- 2 casos con chambers/chasis.
- 2 casos con racks/accesorios.

Para cada caso se debe registrar:

- TAN PN.
- Semana.
- Volumen.
- Operacion.
- Rate Excel usado.
- Horas de carga esperadas.
- Horas disponibles esperadas.
- Tiempo restante esperado.

Luego crear pruebas automatizadas que comparen el motor .NET contra esos valores.

### Fase 2 - Modelo de datos faltante

Agregar tablas/entidades para:

- `Operations`: catalogo real de operaciones del Excel.
- `OperationWeeklyCapacity`: semana, operacion, horas/turno, turnos, dias, qty equipment, downtime, overtime.
- `OperationRates`: part number, operacion, standard time, UPH, yield, utilization, factor eficiencia, UPH efectivo, max cap, HC fixed.
- `TestTimeDefinitions`: test time, handling, qty cell, qty equipment, tiempo calculado por unidad.
- `PartCrossReferences`: integrarla en EF/UI.
- `ResourceTypes`: machine, rack, chamber, chasis, accessory/golden.
- `ResourceInventory`: inventario por escenario/semana/recurso.
- `ResourceConfigurations`: que recurso aplica a que producto/operacion/familia.

### Fase 3 - Motor de planeacion

Extender el motor para producir dos salidas:

- Vista operativa por producto/ruta, como hoy.
- Matriz de planeacion tipo Excel: producto x operacion x semana.

Reglas minimas:

```text
available_hours = days * shifts * hours_per_shift * qty_equipment + overtime - downtime
uph_base = 3600 / standard_time_seconds
uph_effective = uph_base * yield * utilization * factor_efficiency
load_hours = demand_units / uph_effective
time_capacity_units = available_hours * uph_effective
physical_capacity_units = units_per_resource * available_resource_count
capacity_units = min(time_capacity_units, physical_capacity_units cuando aplique)
remaining_hours = available_hours - load_hours
missing_resources = max(0, required_resources - available_resources)
```

### Fase 4 - UX para planeador

Agregar pantallas para:

- Grid de demanda por semanas.
- Matriz de capacidad por operacion.
- Semaforos por operacion: disponible, al limite, sobrecargado.
- Drill-down por producto: de donde sale la carga.
- Modulo de recursos: racks, chambers, chasis, accesorios, inventario y faltantes.
- Importador Excel para cargar catalogos/demanda/rates.

## Decision clave sobre OEE

El usuario pidio quitar el nombre OEE de la UI. Eso esta bien para negocio/UX, pero a nivel calculo debe decidirse una de estas opciones:

1. Guardar `FactorEficiencia` como equivalente al OEE del Excel.
2. Absorber OEE dentro de `FactorUtilizacion`.

Recomendacion: usar `FactorEficiencia` separado. Asi la UI evita el termino OEE, pero el motor mantiene trazabilidad y puede igualar el Excel.

## Conclusion

El MVP actual esta bien encaminado, pero no debe considerarse aun una replica exacta del Excel. Sirve como base tecnica para empezar: .NET, SQL Server, catalogos y motor aislado. Para que sea una herramienta confiable de planeacion manufacturera, la siguiente fase debe enfocarse en paridad numerica contra el Excel y en modelar operaciones, equipos por semana, rates efectivos, tiempos de prueba derivados y recursos fisicos especializados como chambers/chasis/racks.

La prioridad recomendada es construir primero la matriz `PLAN UABU` dentro del sistema y validarla contra una muestra real del Excel. Una vez que esa matriz cuadre, el resto del sistema puede crecer con mucha mas seguridad.
