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

## Menú de inicio

`src/UI/MainMenuForm.Designer.cs` contiene los botones nativos de Windows Forms y sus etiquetas de acción. `src/UI/MainMenuForm.cs` conecta esas acciones, cambia cazador/mazmorra/dificultad y dibuja los paneles informativos sobre el fondo.

- **Entrar** inicia la mazmorra seleccionada; **Continuar** carga la expedición guardada en SQL Server.
- **Cambiar cazador** y **Cambiar mazmorra** son acciones. Los nombres y detalles seleccionados se muestran en las tarjetas del menú, no dentro del texto de los botones.
- **Dificultad** recorre Recluta, Cazador y Monarca. La tarjeta de mazmorra indica el modo seleccionado.
- **Progreso** consulta rango, experiencia, créditos, victorias, récord y estado del guardado.
- **Configuración** y **Controles y objetivos** abren sus respectivos paneles informativos.
- El puntero alimenta una transición interpolada de luz ambiental. El arte y las tarjetas estáticas se componen una vez en una imagen en memoria; cada fotograma solo dibuja esa composición y los efectos móviles.
- Los textos de las tarjetas se ajustan a su ancho disponible para evitar recortes. En pantallas grandes, el menú y el juego usan escalado bilineal y menos partículas para limitar el coste de renderizado.

Los nombres del cazador, clase, título y habilidad vienen de `HunterCatalog`. El nombre, región, jefe y ataques de cada nivel vienen de `DungeonCatalog`; así los paneles se mantienen sincronizados con la selección que recibe `PlatformerForm`.

## Simulación y suavidad de mapas

`PlatformerForm` separa el paso de simulación del dibujo. La lógica usa pasos fijos de 1/60 s; entre pasos, el render interpola la posición del cazador, el seguimiento de cámara, los centinelas y los efectos ambientales. Las colisiones y el guardado usan las coordenadas de simulación, no las posiciones interpoladas que solo sirven para mostrar el movimiento.

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

## Guía de cambios

1. Ajusta botones y orden visual en `src/UI/MainMenuForm.Designer.cs` y `MainMenuForm.cs`.
2. Mantén los datos de cazadores, habilidades, mazmorras, jefes y ataques en `src/Game/GameCatalog.cs`.
3. Mantén reglas de movimiento, cámara, colisiones, enemigos y trampas en `src/Game/PlatformerForm.cs`.
4. Coloca imágenes en `assets/characters/`, `assets/backgrounds/` o `assets/bosses/` y registra la ruta relativa al ejecutable en el catálogo.
5. Compila con `dotnet build .\NexusCopy.sln --configuration Release` o deja que **Nexus Copy CI** compile el push a `main`.
