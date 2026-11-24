# Pruebas y depuración de Nexus Copy

Rama de trabajo: `testing-debugging`  
Método seleccionado: `ProgressStore.DeleteExpedition()`  

## Cambio funcional

`DeleteExpedition` elimina el guardado activo de `ExpeditionSave` y las filas asociadas de fragmentos, enemigos y trampas. Antes ejecutaba cuatro instrucciones `DELETE` en una sola llamada, pero sin una transacción explícita: si una instrucción fallaba después de que otra hubiera terminado, la base podía conservar un guardado incompleto.

Ahora inicia una transacción SQL, ejecuta los cuatro borrados en ella y confirma únicamente después de completarlos. Si ocurre una excepción, la transacción sin confirmar se revierte al cerrarse y el método conserva el tratamiento existente del error de conexión. El método no tiene parámetros. Si tiene éxito, no devuelve valor y `SqlAvailable` queda en `true`; si falla por un error SQL, establece `SqlAvailable` en `false` y comunica el error con las instrucciones de conexión existentes.

Antes:

```csharp
using var connection = OpenDatabase();
using var command = new SqlCommand(
    "DELETE dbo.ExpeditionSave WHERE ProfileId=1; " +
    "DELETE dbo.ExpeditionShardSave WHERE ProfileId=1; " +
    "DELETE dbo.ExpeditionEnemySave WHERE ProfileId=1; " +
    "DELETE dbo.ExpeditionTrapSave WHERE ProfileId=1;",
    connection);
command.ExecuteNonQuery();
SqlAvailable = true;
```

Después:

```csharp
using var connection = OpenDatabase();
using var transaction = connection.BeginTransaction();
using var command = new SqlCommand(
    "DELETE dbo.ExpeditionSave WHERE ProfileId=1; " +
    "DELETE dbo.ExpeditionShardSave WHERE ProfileId=1; " +
    "DELETE dbo.ExpeditionEnemySave WHERE ProfileId=1; " +
    "DELETE dbo.ExpeditionTrapSave WHERE ProfileId=1;",
    connection, transaction);
command.ExecuteNonQuery();
transaction.Commit();
SqlAvailable = true;
```

## Casos de prueba

| ID | Caso | Preparación y pasos | Resultado esperado |
|---|---|---|---|
| INPUT-01 | Selecciones fuera de rango | Navegar por cazadores, mazmorras y dificultades con teclado y botones | La selección permanece dentro de los elementos de los catálogos; la aplicación no lanza una excepción. |
| SAVE-01 | Guardar una partida | Iniciar una expedición, recoger un fragmento y guardar con F5 | `ExpeditionSave` y sus filas de fragmentos, enemigos y trampas reflejan el estado guardado. |
| LOAD-01 | Recuperar una partida | Cerrar la ventana de juego con Esc, volver al menú y seleccionar Continuar; para la demostración completa, cerrar y reabrir la app antes de Continuar | Se recuperan mapa, posición, checkpoint, puntaje, vidas, jefe, fragmentos, enemigos y trampas. |
| DELETE-01 | Borrar atómicamente | Crear una partida con datos relacionados y ejecutar `ProgressStore.DeleteExpedition()` | Al terminar, no quedan filas del perfil en ninguna de las cuatro tablas. La llamada usa una única transacción. |
| SQL-ERR-01 | Servidor detenido | Detener LocalDB o configurar una instancia inexistente; intentar cargar o guardar desde el menú | NEXUS muestra el error documentado, actualiza el estado de SQL y no confirma un guardado que no ocurrió. |
| SQL-ERR-02 | Fallo durante el borrado | En una base de prueba, provocar una restricción o error en una de las tablas durante el borrado | La transacción no confirma un borrado parcial; al consultar de nuevo, la operación queda revertida. |

## Estado de ejecución

La corrección está en el commit `99a46c34eb7b35bcf742ae920452c1d5b59f72d2` de la rama `testing-debugging`. El resultado de compilación de GitHub Actions y los casos que requieren una instancia SQL deben añadirse aquí después de ejecutarlos. No se marca como aprobada una prueba que no se haya ejecutado.

## Integración

Después de revisar la compilación y los casos de base de datos, integrar la rama `testing-debugging` en `main` mediante un pull request y repetir las comprobaciones en `main`.

