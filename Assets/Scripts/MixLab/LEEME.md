# Scripts del Laboratorio de Mezclas

El código está dividido en tres carpetas según **quién lo usa**:

| Carpeta | Scripts | Qué contiene |
|---|---|---|
| `Comun/` | 24 | Todo lo que comparten las dos versiones: la química, el turno, los datos, el cliente, los tableros, el ambiente y la estación (vaso, botellas, palancas, analizador). |
| `VR/` | 5 | Lo que se agrega para jugar en Meta Quest 3 con las manos (XR Interaction Toolkit). |
| `PC/` | 9 | Lo que se agrega para jugar en PC con mouse y teclado, más la demo automática. |

**Versión VR = `Comun` + `VR`** · **Versión PC = `Comun` + `PC`**

Las dos versiones terminan llamando a los mismos métodos de `Comun` (`MixGlass.Add`, `TemperatureLever.Set`,
`MixGlass.Shake`, `MixGameManager.Serve`), así que la lógica del juego existe una sola vez.

## VR (5 scripts)

| Script | Qué hace |
|---|---|
| `MixVrRig` | Detecta el visor y activa el XR Origin y los componentes de XR Interaction Toolkit |
| `VrPourable` | Verter inclinando la botella sobre el vaso |
| `VrGlass` | Agitar sacudiendo el vaso, vaciarlo volteándolo, volver a la estación |
| `VrLever` | Arrastrar con la mano la palanca de temperatura y la de agitar |
| `CoasterSocket` | Posavasos del cliente (socket): al encajar el vaso se sirve |

## PC (9 scripts)

| Script | Qué hace |
|---|---|
| `DesktopPlayer` | Primera persona: caminar, mirar, chocar con la barra |
| `DesktopHud`, `IAimHud` | Mira, textos de acción, ayuda de controles y pausa |
| `MixInteractor`, `MixInteractable` | Apuntar con la mira y hacer clic sobre las cosas |
| `ServeSpot`, `SinkSpot`, `StationPad` | Servir al cliente, vaciar en el lavaplatos, dejar el vaso (con clic) |
| `MixAutoDemo` | Demo automática del `.exe` (`-autodemo`) que juega un turno y saca capturas |

## Comun (24 scripts)

- `Core/` — `ReactionRules` (la química), `MixGameManager` (el turno), `MixSaveRepository` (JSON y CSV), `PerfProbe` (rendimiento)
- `Data/` — `IngredientData`, `RecipeData`, `ReactionData`, `MixEnums`
- `Interaction/` — `MixGlass`, `Bottle`, `MixAnalyzer`, `TemperatureLever`, `ShakeLever`
- `Customer/` — `Customer`, `Patron`
- `UI/` — `MixBoard`, `ReactionBookBoard`, `MixTutorial`, `HistoryBoard`, `JukeboxBoard`
- `Ambience/` — `MusicPlayer`, `DiscoLights`, `Dancer`, `QualityGovernor`
