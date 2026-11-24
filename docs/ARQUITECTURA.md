# Arquitectura de Nexus Copy

Nexus Copy conserva la identidad de la aventura de cazadores y mazmorras, y adopta una estructura por responsabilidades similar a la del repositorio principal. El ejecutable, la interfaz, las reglas y el cliente de base de datos están implementados en C# y Windows Forms.

## Carpetas

```text
Program.cs                    Punto de entrada Windows Forms
src/
  UI/                         Menú, controles y formularios de interfaz
  Game/                       Simulación, combate, mapas y catálogos
  Audio/                      Música generada por el juego
  Models/                     Perfil y estado de expedición
  Data/                       Lectura y escritura del progreso SQL Server
  Database/                   Creación de base y tablas desde C#
assets/
  characters/                 Cazadores
  bosses/                     Jefes
  backgrounds/                Mundos y escenarios
  audio/                      Documentación de música y sonido
levels/                       Borradores de configuración de niveles
docs/                         Arquitectura y guías de desarrollo
.github/workflows/            Compilación automática para Windows
```

## Flujo del juego

`Program` inicia `MainMenuForm`. El menú carga el perfil y el guardado mediante `ProgressStore`; `SqlServerSchema` crea `NexusCopyDb` y las tablas si faltan. Al iniciar o continuar, `PlatformerForm` ejecuta el mapa seleccionado, el movimiento, las trampas, los proyectiles, los enemigos y el jefe. El perfil y las partidas se guardan en SQL Server.

## Datos SQL

- `dbo.HunterProgress`: cazador, experiencia, créditos, rango, mazmorras completadas y récord.
- `dbo.ExpeditionSave`: mapa, dificultad, posición, checkpoint, vidas, puntaje y estado del jefe.
- `dbo.ExpeditionShardSave`: fragmentos recogidos.
- `dbo.ExpeditionEnemySave`: estado y posición de los centinelas.
- `dbo.ExpeditionTrapSave`: ciclo de las trampas.

El esquema se mantiene en `src/Database/SqlServerSchema.cs`; el acceso a los datos está en `src/Data/ProgressStore.cs`. No hace falta un script externo para crear la base.

## Recursos y cambios

El logo y el icono de Windows siguen la identidad visual de NEXUS. Los mapas, personajes y jefes propios de Nexus Copy permanecen en sus subcarpetas de `assets/`.

Para añadir un recurso, colócalo en la carpeta correspondiente y actualiza el catálogo o formulario C# que lo usa. Para modificar las tablas, cambia `SqlServerSchema` y mantén las operaciones de lectura y escritura en `ProgressStore`.
