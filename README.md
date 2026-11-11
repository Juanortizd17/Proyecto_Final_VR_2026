# Laboratorio de Mezclas

Proyecto Final de **Fundamentos de Realidad Virtual 2026** (opción 3 del enunciado: *Laboratorio Científico XR*),
hecho con **Unity 6 (6000.0.58f2)** para la empresa ficticia *UAO Immersive Solutions*.

Eres el encargado de un laboratorio-bar. Ramiro, el cliente de prueba, pide bebidas; tú combinas ingredientes
con propiedades químicas (ácido, gas, alcohol, picante, lácteo, cafeína…), ajustas la temperatura, agitas y sirves.
Si aciertas, suma puntos. Si te equivocas, **su cuerpo reacciona**: se pone rojo y suda con el picante, se marea,
se pone verde, le explota la mezcla en la cara o cae desmayado. Hay **12 reacciones por descubrir** en el libro.

![Vista del jugador](docs/screenshots/mix/01_jugador.png)

## Requerimientos del enunciado

- [x] **Onboarding**: entrenamiento guiado (moverse, verter, agitar, servir) validado con acciones reales
- [x] **Interacciones principales**: ingredientes, palanca de temperatura, palanca de agitar, vaso que se agarra,
      posavasos del cliente (socket), lavaplatos, botones en tableros
- [x] **Eventos dinámicos**: tiempo del turno, paciencia del cliente, *hora pico* (doble afán) y *apagón*
      (sin etiquetas: toca reconocer los ingredientes), 3 desmayos suspenden el turno
- [x] **Puntuación verificable**: +100 y bono por rapidez en pedido perfecto; cada reacción resta o suma
- [x] **Cierre**: panel final con puntaje, récord, pedidos perfectos, reacciones descubiertas y retroalimentación

## Cómo abrirlo

Por ahora es un **juego de PC en primera persona** (mouse y teclado). La conversión a Meta Quest es el siguiente paso.

1. Abrir la carpeta con **Unity 6000.0.58f2**.
2. Abrir `Assets/Scenes/MixLab.unity` y darle **Play**, o ejecutar `Builds/Windows/ControlCalidadXR.exe`.

| Control | Acción |
|---|---|
| WASD · Mouse | Caminar · mirar |
| Clic izquierdo sobre un ingrediente | Verter una dosis en el vaso (máximo 4) |
| Clic izquierdo sobre una palanca | Cambiar temperatura / agitar |
| Clic izquierdo sobre el vaso | Agarrarlo; luego apunta al cliente y haz clic para servir |
| Clic izquierdo sobre el lavaplatos | Vaciar el vaso |
| R | Dejar el vaso en la estación |
| Esc · H | Pausa (con sensibilidad del mouse) · ayuda |

**Modo demostración**: `ControlCalidadXR.exe -autodemo -roundSeconds 70` juega un turno solo y guarda capturas
(las de la última corrida están en [docs/pc_demo](docs/pc_demo)).

## Reacciones

| Mezcla | Reacción |
|---|---|
| Exactamente el pedido (y su temperatura) | Pedido perfecto |
| Ají | Picante: rojo y sudando |
| Salsa del Diablo | Picante infernal: humo por las orejas |
| Soda + efervescente | Explosión en la cara |
| Mucho alcohol | Mareo total |
| Alcohol fuerte + caliente | Llamarada |
| Leche + limón | Náusea verde |
| Menta + frío | Cerebro congelado |
| Doble café | Sobredosis de café |
| Salsa del Diablo + alcohol | Desmayo (cuenta para perder) |

## Manejo de datos

- **ScriptableObjects** en `Assets/Data/Mix`: 12 ingredientes (color y propiedades), 7 recetas y 12 reacciones
  (texto, puntaje, color). Se agregan o cambian sin tocar código.
- **JSON** en `Application.persistentDataPath/mix_save.json`: libro de reacciones descubiertas, récord e
  historial de turnos. El libro se conserva entre partidas.

## Estructura

```
Assets/
  Scripts/MixLab/   Data · Core · Interaction · Customer · UI
  Scripts/Desktop/  Jugador de PC, mira y HUD (compartidos)
  Editor/           MixSceneBuilder (genera la escena), MixSnapshot, BuildQuest
  Data/Mix/         Ingredients · Recipes · Reactions (ScriptableObjects)
  Scenes/           MixLab.unity  (y QualityControl.unity, el prototipo anterior)
  Tests/PlayMode/   19 pruebas automáticas
  ThirdParty/Kenney Modelos y sonidos CC0 (Food Kit, Furniture Kit, Factory Kit, sonidos)
docs/               Enunciado, capturas y guía de prueba con Quest
presentation/       Presentación de sustentación
```

Menú **Tools › Mix Lab**: *Build Scene* regenera la escena y los datos; *Capture Screenshots* saca vistas.
`Tools › Quality Control XR › Build Windows App` compila el `.exe`.

`Assets/Scenes/QualityControl.unity` conserva el primer prototipo (Control de Calidad XR, opción 1).

## Rúbrica

| Bloque | Peso | Criterios |
|---|---|---|
| Prueba ejecutable | 40% | UI 15% · Implementación 3D 15% · Interacciones 25% · Inmersión 25% · Montaje general 20% |
| Sustentación del proyecto | 60% | Organización Unity 15% · Scripts 15% · Manejo de datos 20% · Sustentación personal 40% · Presentación personal 10% |

## Créditos

Modelos y sonidos de [Kenney](https://kenney.nl) (CC0).
