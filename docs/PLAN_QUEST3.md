# Planteamiento para Meta Quest 3 — Laboratorio de Mezclas

Estado de partida: el juego está completo y probado en PC (mouse y teclado). El proyecto ya tiene
XR Interaction Toolkit 3.0.8, OpenXR y el pipeline de APK del primer prototipo (Control de Calidad),
pero la escena del bar todavía no tiene visor, manos ni objetos agarrables.

## 1. Qué significa "listo"

- Se instala como APK en el Quest 3 y corre sin PC, a 72 FPS estables.
- Todo el turno se juega con las manos: agarrar, verter, estabilizar, agitar, servir y vaciar.
- El entrenamiento enseña movimiento, agarre y controles dentro del visor (lo exige el enunciado).
- La versión de PC sigue funcionando igual: una sola base de código, dos formas de jugar.

## 2. Qué se conserva tal cual

Nada de la lógica cambia; solo se agrega una capa de entrada para VR.

| Pieza | Se reutiliza |
|---|---|
| Reglas de reacción, clasificar y estabilizar (`ReactionRules`) | Sí, sin tocar |
| Director del turno, puntaje, eventos, guardado (`MixGameManager`, JSON) | Sí |
| Cliente y sus reacciones, público, luces, rockola | Sí (con el nivel de calidad VR) |
| Tableros | Sí: ya tienen el componente para rayos de los controles |
| Pruebas automáticas (21) | Sí, y se agregan las de VR que no dependan del visor |

## 3. Cómo se juega con las manos

| Acción | En PC hoy | En Quest 3 |
|---|---|---|
| Moverse | WASD + mouse | Joystick izquierdo camina, derecho gira por pasos; viñeta al caminar para evitar mareo. El puesto está pensado para jugar de pie sin desplazarse |
| Verter líquidos | Clic en la botella | Agarrar la botella con el gatillo lateral e **inclinarla sobre el vaso**: cae una dosis, con vibración y sonido |
| Hielo, limón, ají, azúcar, pastilla | Clic | Agarrar la pieza y **soltarla dentro del vaso** |
| Devolver la botella | Automático | Al soltarla vuelve a su puesto del estante (**socket** por ingrediente) |
| Temperatura | Clic en la palanca | Agarrar la palanca y moverla entre tres posiciones con tope |
| Agitar | Clic en la palanca | **Sacudir el vaso con la mano**; la palanca queda como alternativa |
| Estabilizar | Hielo o palanca en frío | Igual, con las manos; el vaso tiembla y vibra en la mano mientras está inestable |
| Servir | Clic en el cliente | Dejar el vaso en el **posavasos** del cliente (socket) |
| Vaciar | Clic en el lavaplatos | Voltear el vaso sobre el lavaplatos |
| Botones (tutorial, rockola, reiniciar) | Mira en el centro | Rayo del control + gatillo, o tocarlos con el dedo |
| Avisos (vaso, temperatura, análisis) | Texto fijo en pantalla | En el analizador de la estación y en el tablero; nada pegado a la cara |

Esto cubre la lista de interacciones del enunciado: objetos agarrables, sockets, botones, palancas de
temperatura y sistema de validación.

## 4. Rendimiento en el visor

Las mediciones de `docs/RENDIMIENTO.md` muestran que el costo está en las luces en tiempo real.

| Medida | Efecto esperado |
|---|---|
| Una luz direccional + ambiente; el color de discoteca sale de materiales emisivos | De ~1.250 a ~700 llamadas de dibujado |
| Fundir en mallas únicas las botellas de la repisa, la barra y las piezas fijas del cliente | Unas 300 menos |
| Público en nivel VR (DJ, 2 en la barra, 6 bailarines, pareja del sofá) | Ya aplicado en el regulador |
| Sin sombras en tiempo real (sombra redonda bajo los personajes) | Menos pasadas |
| Agrupar los letreros en menos lienzos y apagar los que no se ven desde el puesto | Decenas menos |
| Renderizado de una pasada para ambos ojos, antialiasing 4×, resolución fija | Estándar en Quest |

Objetivo: menos de 250 llamadas de dibujado y 72 FPS, medido en el visor con el mismo `PerfProbe`.
Si sobra margen en Quest 3 se sube público o luces; si falta, se baja antes de tocar la jugabilidad.

## 5. Música en el visor

En Android las carpetas del juego van dentro del APK y no se pueden listar. Solución:

- Las cuatro pistas originales van incluidas, como hoy.
- Las canciones propias se copian por USB a la carpeta de datos del juego en el visor
  (`Android/data/<paquete>/files/Musica/<Género>`), que sí se puede leer.

## 6. Fases

Cada fase termina con algo que tú pruebas con las gafas. Yo no puedo ponerme el visor: verifico por
compilación, pruebas automáticas y el simulador de XR, y tú confirmas la sensación real.

| Fase | Qué se hace | Qué pruebas tú |
|---|---|---|
| 0. Preparación | Plataforma Android, OpenXR para Quest, escena con visor y manos, APK que abre el bar | Que instala, abre y se ve nítido; altura de la barra |
| 1. Moverse y botones | Locomoción, giro, rayos sobre tableros y rockola | Mareo, velocidad, si los botones se dejan pulsar |
| 2. Agarrar y verter | Botellas agarrables, inclinación, piezas sólidas, sockets del estante | Si verter se siente natural; ángulo y distancia |
| 3. Estación completa | Palanca de temperatura, sacudir, estabilizar, servir, vaciar | Un pedido completo de principio a fin |
| 4. Rendimiento | Luces, mallas fundidas, público VR, medición en el visor | Fluidez; que no se pierda el ambiente |
| 5. Entrenamiento y pulido | Tutorial VR paso a paso, vibración, sonido espacial, textos legibles | El tutorial sin ayuda mía; lectura de tableros |
| 6. Entrega | APK final, video de respaldo, guion de sustentación | Turno completo grabado |

Orden de riesgo: las fases 2 y 3 son las que más dependen de tu prueba, porque "se siente bien" no se
puede medir desde el PC. La fase 4 es la que más trabajo técnico lleva.

## 7. Riesgos

- **Verter por inclinación** puede resultar impreciso con botellas pequeñas. Alternativa lista: verter
  manteniendo el gatillo mientras la botella está sobre el vaso.
- **Texto pequeño** en etiquetas de ingredientes: en el visor se agrandan o se muestran al agarrar.
- **Rendimiento** con el público: el regulador ya permite bajar sin rehacer nada.
- **Tiempo**: si se aprieta, se entrega con palanca para agitar (ya existe) en vez de sacudir con la mano.

## 8. Lo que necesito de ti

1. Modo desarrollador activado en el Quest 3 y el cable USB (o Link funcionando).
2. Decir si juegas de pie o sentado, y con qué mano agarras el vaso.
3. Una prueba corta con las gafas al terminar cada fase (10 minutos cada una).
