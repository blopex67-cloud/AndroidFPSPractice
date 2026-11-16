# Android FPS Practice (Unity)

Prototipo FPS en primera persona inspirado en una arena de práctica: movimiento, giro táctil, arma de prueba, recarga y dummies con ragdoll físico. El mundo y sus materiales se generan por código; no requiere paquetes, modelos ni shaders descargados.

## Abrir
- Unity **6000.0.23f1** o una versión 6 compatible.
- Abre la carpeta `unity/` como proyecto.
- En Build Profiles elige Android y cambia la plataforma.
- Para un servicio de build conectado a GitHub, usa este repo y deja el **Project subdirectory** como `unity` (respeta minúsculas); la escena `Assets/Scenes/FPSArena.unity` ya está en Build Settings.
- Orientation: Landscape. Activa ARM64 y crea un APK para probar.
- No requiere escena preconfigurada: `Bootstrap` crea el prototipo al iniciar Play/build.

## Controles
- PC: WASD/flechas, mouse para mirar, clic para disparar, R para recargar.
- Móvil: lado izquierdo para desplazamiento, arrastra el lado derecho para mirar; botones FIRE, RELOAD y RAGDOLL.
- El botón RAGDOLL crea muñecos de práctica adicionales; dispararles activa articulaciones y física.

## Shaders/materiales
Se usan materiales `Standard` del shader Built-in de Unity y colores asignados por código; no hay referencias a shaders URP/HDRP ni texturas externas que puedan quedar magenta por incompatibilidad. El proyecto no instala URP/HDRP.

## Audio
Efectos de disparo/recarga/impacto generados proceduralmente en ejecución, sin archivos de audio ni licencias externas.

## Modelo FPS externo
El modelo/arma actual son placeholders 3D generados dentro del proyecto para que siempre funcionen y no se rompan las referencias. Recurso opcional para reemplazar el arma: [Low Poly Firearms Pack (CC0)](https://synofplay.itch.io/low-poly-firearms-pack-10-game-ready-weapons-cc0). Descarga e importa el pack en Unity, luego reemplaza el objeto `Training blaster`.

## Alcance
Es una base jugable/prototipo, no un juego terminado ni un APK probado en dispositivo. El ragdoll se construye con cuerpos y `CharacterJoint` al iniciar. Verifica controles, colisiones y escala en el teléfono antes de ampliar el proyecto.
