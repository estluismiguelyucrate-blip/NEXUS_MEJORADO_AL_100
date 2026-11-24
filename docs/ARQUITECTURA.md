# Estructura de Nexus copia

- `src/`: pantallas y sistemas del juego.
- `assets/`: arte reemplazable (personajes, fondos, mapas, interfaz y jefes).
- `levels/`: catálogo y configuración declarativa de los mapas.
- `docs/`: decisiones de diseño y guías de contenido.

## Siguiente contenido

1. Separar movimiento, combate, cámara y colisiones en sistemas independientes.
2. Cargar mapas desde `levels/*.json` y enlazar fondos/tilesets de `assets/`.
3. Añadir escenas, animaciones y patrones de ataque configurables para jefes.
4. Añadir pausa, ajustes, selección de personaje, guardado y accesibilidad.

Los recursos indicados en JSON son puntos de extensión: si no existe una imagen, el juego dibuja fondos y personajes provisionales. Se pueden sustituir por arte propio sin cambiar el diseño de niveles.

