# Guía de prueba con Meta Quest

Lista para la primera sesión con las gafas. Anota lo que observes en cada punto y pásalo tal cual:
con eso se ajustan los valores del proyecto.

## 1. Preparación (una sola vez)

1. En el celular, app **Meta Horizon** › Dispositivos › tu Quest › **Modo desarrollador: activado**.
2. En el PC instala la app **Meta Quest Link** (meta.com/quest/setup) e inicia sesión.
3. En la app de Link: Configuración › General › activar **OpenXR Runtime** (botón "Establecer Meta Quest Link como activo").
4. Conecta el Quest con cable USB-C (o usa **Air Link** con el PC y el Quest en la misma red 5 GHz).
5. En el visor acepta **Quest Link** y entra.

## 2. Formas de correrlo

| Forma | Cómo | Para qué |
|---|---|---|
| Desde Unity | Link activo › abrir `QualityControl.unity` › **Play** | Probar y ajustar rápido |
| App de Windows | Link activo › `Builds/Windows/ControlCalidadXR.exe` | Probar como app final con PC |
| Directo en el Quest | Tools › Quality Control XR › **Switch Platform to Meta Quest (Android)** › **Build Quest APK** › `adb install -r Builds/ControlCalidadXR.apk` | Entrega final sin PC |

Si al abrir aparece el simulador (manos que se mueven con el teclado), el visor no fue detectado:
revisa que Link esté activo **antes** de abrir la app.

## 3. Qué probar y qué anotar

Marca cada punto como ✅ bien, ⚠️ mejorable (con detalle) o ❌ falla.

### Comodidad
- [ ] Altura de la banda (0.9 m) y de la mesa (0.8 m): ¿cómoda de pie? ¿y sentado?
- [ ] Tamaño de los textos de los tableros: ¿se leen sin esfuerzo a la distancia normal?
- [ ] Movimiento con joystick: ¿causa mareo? ¿preferirías solo teletransporte?
- [ ] Giro: ¿suave o por saltos?

### Onboarding
- [ ] Paso 1: ¿se completa al acercarte a la mesa?
- [ ] Paso 2: ¿agarrar la caja con el grip se siente natural?
- [ ] Paso 3: ¿tocar la caja con la lupa avanza el paso?
- [ ] Paso 4: ¿la caja encaja en el soporte verde?
- [ ] Botón INICIAR TURNO: ¿se presiona con el rayo y el gatillo?
- [ ] Al iniciar, ¿quedas bien ubicado frente a la banda?

### Juego
- [ ] Velocidad de la banda (0.25 m/s): ¿da tiempo de inspeccionar? (anota: lenta / justa / rápida)
- [ ] Frecuencia de productos (cada 5 a 8 s): ¿muchos, pocos, bien?
- [ ] Agarrar productos de la banda: ¿se sienten estables en la mano?
- [ ] Herramientas: ¿la punta detecta al tocar el producto? ¿se siente la vibración?
- [ ] Indicador rojo / verde: ¿se entiende sin explicación?
- [ ] Soltar en contenedores: ¿cuenta bien? ¿se escucha el sonido?
- [ ] Evento inesperado (aceleración / avería, primer evento a los 40 s): ¿se nota la alerta?
- [ ] Herramientas que se caen al piso: ¿vuelven a la mesa?

### Cierre
- [ ] Panel final: ¿se lee bien el puntaje, el récord y los reportes?
- [ ] REINICIAR y SALIR: ¿funcionan?

### Rendimiento
- [ ] ¿Se ve fluido o con tirones? (con Link: Herramienta de depuración de Oculus › Performance HUD para ver FPS)
- [ ] ¿Algún objeto rosado, parpadeante o que atraviesa cosas?

## 4. Valores ajustables (para referencia)

| Qué | Dónde | Valor actual |
|---|---|---|
| Duración del turno | GameManager › Round Duration | 180 s |
| Fallos máximos | GameManager › Max Mistakes | 5 |
| Puntos por acierto / penalización | GameManager | 100 / 50 |
| Velocidad de la banda | BeltSurface › ConveyorBelt › Speed | 0.25 m/s |
| Intervalo entre productos | ConveyorBelt › Spawn Interval | 5 a 8 s |
| Probabilidad de defecto | ConveyorBelt › Defect Chance | 40 % |
| Primer evento / cada cuánto | ConveyorBelt › First Event At / Event Every Seconds | 40 s / 45 s |
| Fuerza de la vibración | Tool_* › InspectionTool › Haptic Strength | 0.7 |
