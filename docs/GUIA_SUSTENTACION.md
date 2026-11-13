# Guía de sustentación — Laboratorio de Mezclas

Para preparar la parte oral (Sustentación personal 40% y Presentación 10% del bloque de sustentación).
Cada integrante debería poder explicar cualquier sección, no solo la suya.

## 1. El juego en 30 segundos

> Es un laboratorio de química ambientado en un bar. El jugador combina ingredientes que tienen propiedades
> químicas (ácido, gas, alcohol, picante, lácteo, cafeína, frío). Según lo que mezcle y la temperatura, se activa
> una reacción en el cliente: si acierta la fórmula del pedido gana puntos; si no, el cliente vomita, explota,
> se prende o se desmaya. Antes de servir, un analizador clasifica la mezcla como estable, inestable o tóxica, y
> las inestables hay que estabilizarlas con frío o el vaso estalla. Se juega en Meta Quest 3 con las manos.

## 2. Cómo cumplimos el enunciado

### Opción 3: Laboratorio Científico XR

| Función pedida | Dónde está | Script principal |
|---|---|---|
| Combinar elementos | Botellas y recipientes del estante, hasta 4 por vaso | `Bottle`, `VrPourable`, `MixGlass` |
| Activar reacciones | 17 reacciones según las propiedades sumadas | `ReactionRules.Evaluate`, `Customer` |
| Clasificar compuestos | Letrero analizador frente al vaso | `MixAnalyzer`, `ReactionRules.Classify` |
| Estabilizar mezclas | Hielo o palanca en FRÍO; si no, cuenta regresiva y el vaso estalla | `ReactionRules.WillExplode`, `MixGlass.ExplodeAtStation` |
| Descubrir fórmulas | 38 recetas, libro de reacciones guardado | `RecipeData`, `ReactionBookBoard`, `MixSaveRepository` |

| Interacción pedida | Componente de XR Interaction Toolkit |
|---|---|
| Grab interactivos | `XRGrabInteractable` en las 19 botellas/recipientes y el vaso |
| Sockets químicos | `CoasterSocket` (hereda de `XRSocketInteractor`): solo acepta el vaso con mezcla y sirve al encajarlo |
| Botones de laboratorio | Canvas en World Space con `TrackedDeviceGraphicRaycaster`; `XRUIInputModule` en VR |
| Palancas de temperatura | `XRSimpleInteractable` + `VrLever`: se arrastra a FRÍO / AMBIENTE / CALIENTE |
| Validación química | `ReactionRules` (lógica pura, probada) + analizador + reacción del cliente |

### Requerimientos obligatorios

| Requisito | Cómo se cumple |
|---|---|
| Onboarding VR | `MixTutorial`: moverse, agarrar y verter, agitar, servir. Cada paso se valida con un evento real (el vaso cambia, se agita, el cliente reacciona). En VR enseña joystick, grip, inclinar y soltar en el posavasos |
| Manipulación física, objetivos, progresión | Botellas y vaso con la mano; el pedido es el objetivo; libro y récord dan progresión |
| Temporizador | Turno con tiempo y paciencia del cliente (`MixGameManager`) |
| Fallo | 3 desmayos suspenden el turno; el cliente se cansa si tardas |
| Evento inesperado | *Hora pico* (la paciencia baja el doble) y *apagón* (se apagan luces y etiquetas) cada cierto tiempo |
| Sistema reactivo | Eventos C# (`OnStateChanged`, `OnReaction`, `OnScoreChanged`…): tableros, luces, público y tutorial reaccionan sin conocerse entre sí |
| Puntuación verificable | Puntos por perfecto, bono de rapidez, bono por estabilizar, penalizaciones; todo queda en el JSON y el CSV |
| Cierre | Panel final con resumen y retroalimentación; tablero de historial |

## 3. Arquitectura

```
                 ┌────────────────────────────┐
  Datos          │  ScriptableObjects          │  IngredientData · RecipeData · ReactionData
                 └─────────────┬──────────────┘
                               ▼
  Reglas         ReactionRules (estático, sin MonoBehaviour: fácil de probar)
                               ▼
  Director       MixGameManager ── eventos ──► MixBoard · ReactionBookBoard · HistoryBoard
                   │    ▲                       MixTutorial · DiscoLights · Patron · Dancer
                   │    │ Serve()
  Interacción    MixGlass ◄─ Bottle / VrPourable     MixAnalyzer (clasifica cada frame)
                   ▲          TemperatureLever / ShakeLever ◄─ VrLever
                   │          ServeSpot (PC) / CoasterSocket (VR)
  Entrada        PC: DesktopPlayer + MixInteractor (mira)   VR: MixVrRig + XR Origin (XRI)
                               ▼
  Persistencia   MixSaveRepository → mix_save.json + mix_save_historial.csv
```

Idea clave: **una sola lógica, dos formas de jugar**. La entrada de PC y la de VR terminan llamando a los mismos
métodos (`glass.Add`, `lever.Set`, `glass.Shake`, `gm.Serve`). Por eso la versión de PC sirve para probar todo sin visor.

### Scripts por carpeta (`Assets/Scripts/MixLab`)

| Carpeta | Script | Qué hace |
|---|---|---|
| Data | `IngredientData`, `RecipeData`, `ReactionData`, `MixEnums` | Datos editables sin código: propiedades, recetas, reacciones |
| Core | `ReactionRules` | Suma propiedades y decide reacción, clasificación y si explota |
| Core | `MixGameManager` | Máquina de estados (Onboarding → Playing → Finished), pedidos, puntaje, eventos inesperados, guardado |
| Core | `MixSaveRepository` | Lee y escribe el JSON; exporta el CSV |
| Core | `MixAutoDemo`, `PerfProbe` | Demo automática con capturas y medición de rendimiento |
| Interaction | `MixGlass` | Contenido del vaso, capas de color, agitar, estallar |
| Interaction | `Bottle` | Ingrediente; en PC hace la animación de verter |
| Interaction | `MixAnalyzer` | Clasifica la mezcla y lleva la cuenta regresiva |
| Interaction | `TemperatureLever`, `ShakeLever`, `ServeSpot`, `SinkSpot`, `StationPad` | Estación de trabajo |
| Interaction | `MixInteractor`, `MixInteractable` | Mano de PC: apuntar y hacer clic |
| Customer | `Customer` | Cara, cuerpo y síntomas de cada reacción |
| Customer | `Patron` | Público que reacciona a lo que pasa en la barra |
| UI | `MixBoard`, `ReactionBookBoard`, `MixTutorial`, `HistoryBoard`, `JukeboxBoard` | Tableros del bar |
| Ambience | `MusicPlayer`, `DiscoLights`, `Dancer`, `QualityGovernor` | Música, luces al ritmo, baile, nivel de calidad |
| Desktop | `DesktopPlayer`, `DesktopHud` | Jugador de PC |
| VR | `MixVrRig` | Decide si hay visor y activa el XR Origin y los componentes XRI |
| VR | `VrPourable` | Verter inclinando la botella sobre el vaso |
| VR | `VrGlass` | Agitar sacudiendo, vaciar volteando, volver a la estación |
| VR | `VrLever` | Arrastrar las palancas con la mano |
| VR | `CoasterSocket` | Socket del posavasos que sirve la bebida |

`Assets/Editor/MixSceneBuilder*.cs` genera toda la escena desde código (sala, barra, cliente, público, tableros,
capa VR). Ventaja: la escena es reproducible y cualquier cambio de diseño se aplica regenerándola.

## 4. Recorrido de un servicio (para explicar en vivo)

1. El jugador inclina una botella sobre el vaso → `VrPourable` detecta el ángulo y la posición → `MixGlass.Add`.
2. `MixGlass` dispara `OnChanged`: el vaso muestra la capa de color; el tutorial avanza si corresponde.
3. Cada frame, `MixAnalyzer` llama a `ReactionRules.Classify`: estable (verde), inestable (ámbar, cuenta regresiva) o tóxica (rojo).
4. El jugador suelta el vaso sobre el posavasos → `CoasterSocket` lo acepta → `MixGameManager.Serve`.
5. `ServeRoutine` evalúa con `ReactionRules.Evaluate`, el cliente bebe y `Customer.ShowReaction` muestra los síntomas.
6. Se suman puntos, se guarda la reacción descubierta en el JSON y se emite `OnReaction`: tableros, público y luces reaccionan.

## 5. Manejo de datos (20%)

- **Configuración**: ScriptableObjects (`Assets/Data/Mix`). Agregar una bebida = crear un `RecipeData`, sin tocar código.
- **Progreso**: `mix_save.json` con `JsonUtility` (clases `MixSave` y `MixSession`): récord, reacciones descubiertas y últimos 50 turnos. Lectura y escritura con `try/catch`; si el archivo falta o está dañado, se empieza limpio.
- **Exportación**: `mix_save_historial.csv` (separado por `;` para Excel en español), se reescribe al guardar.
- **En el juego**: tablero de historial (récord, promedio, completados, últimos 6 turnos) y libro de reacciones.
- **Preferencias**: género y volumen de música en `PlayerPrefs`.
- **Dónde quedan**: PC `%USERPROFILE%\AppData\LocalLow\UAO Immersive Solutions\Laboratorio de Mezclas\`; Quest `Android/data/com.uao.laboratoriomezclas/files/`.

## 6. Optimización para VR

- Medido con `PerfProbe` (ver `docs/RENDIMIENTO.md`): el costo está en las luces en tiempo real, no en el público.
- `QualityGovernor` con tres niveles (Alto, Medio, VR): limita personajes, luces extra y luces por píxel; baja de nivel solo si caen los FPS. En el Quest arranca en VR.
- Decoración marcada como estática para agrupado (352 objetos); personajes sin sombras; animación con *culling* por visibilidad.
- Android: IL2CPP ARM64, Vulkan, texturas ASTC, MSAA 4×.
- Texto: tamaños fijos y menos resolución por letra para no desbordar la textura de la fuente.

## 7. Pruebas automáticas

17 pruebas PlayMode sobre la escena real (`Assets/Tests/PlayMode`):
- Reglas: cada combinación da su reacción; clasificar y estabilizar; recetas exactas con temperatura.
- Flujo: verter-agitar-servir da pedido perfecto; 3 desmayos suspenden; el vaso inestable estalla si no se enfría.
- Datos: el libro persiste; el historial y el CSV se guardan.
- Tutorial con acciones reales; el jugador no atraviesa la barra.
- VR simulado: activar modo VR, verter inclinando, recipiente de piezas, agitar sacudiendo, vaciar volteando, palancas, socket del posavasos.

## 8. Preguntas probables

**¿Por qué un bar y no un laboratorio con tubos de ensayo?**
Porque mantiene las cinco funciones de la opción 3 y la hace más divertida y memorable. Las propiedades de los ingredientes son las "sustancias", la reacción del cliente es el resultado observable, y el analizador es la validación química.

**¿Qué componentes de XR Interaction Toolkit usaron?**
XR Origin (Starter Assets) con Near-Far Interactor, locomoción continua, giro y teletransporte; `XRGrabInteractable`, `XRSimpleInteractable`, `XRSocketInteractor` (heredado en `CoasterSocket`), `TeleportationArea`, `XRUIInputModule`, `TrackedDeviceGraphicRaycaster`, `XRInteractionManager` y háptica con `SendHapticImpulse`.

**¿Cómo detectan que el jugador está vertiendo?**
Con el ángulo entre el eje de la botella y la vertical (más de 100°) y la posición de la boca encima del borde del vaso, durante 0,35 s. Para otra dosis hay que enderezar la botella.

**¿Cómo detectan que agitó el vaso?**
Con la velocidad de la mano: se cuentan los cambios de dirección rápidos (más de 0,8 m/s); con 5 vaivenes seguidos la mezcla queda agitada.

**¿Por qué el socket del posavasos es una clase propia?**
Para que solo acepte el vaso, solo si tiene algo y solo si el cliente está libre. Se sobrescriben `CanHover` y `CanSelect`.

**¿Cómo se comunican los sistemas?**
Con eventos C#. El `MixGameManager` no sabe que existen los tableros ni las luces: ellos se suscriben.

**¿Cómo se puede probar sin visor?**
La misma escena tiene un modo PC. Las pruebas automáticas activan el modo VR y simulan los movimientos de la mano.

**¿Qué pasa si se borra el archivo de guardado?**
`MixSaveRepository.Load` devuelve un guardado nuevo; nada se rompe.

**¿Qué fue lo más difícil?**
Respuesta sugerida: que la misma lógica sirviera para PC y VR, y el rendimiento con tantas luces y personajes.

## 9. Demo sugerida (5 minutos)

1. Entrenamiento: moverse, verter dos ingredientes, agitar, servir.
2. Iniciar turno y hacer un pedido perfecto.
3. Provocar una mezcla inestable (soda + efervescente): mostrar el analizador y estabilizar con hielo.
4. Provocar una reacción fuerte (leche + limón o alcohol caliente).
5. Mostrar el libro de reacciones y el tablero de historial al terminar.
