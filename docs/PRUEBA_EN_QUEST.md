# Prueba en el Meta Quest 3

La capa VR ya está hecha y probada con pruebas automáticas que simulan la mano, pero **nunca se ha probado con
el visor puesto**. Esta lista sirve para esa primera prueba: anota lo que falle o se sienta raro y con eso se ajusta.

## Preparar el visor (una sola vez)

1. En la app **Meta Horizon** del celular: *Dispositivos › Ajustes del visor › Modo desarrollador* → activar.
   (Pide crear o unirse a una organización de desarrollador en developers.meta.com; es gratis.)
2. Conectar el Quest al PC por USB-C y aceptar *Permitir depuración USB* dentro del visor.

## Opción A: APK directo en el visor (la de la entrega)

```bash
"C:/Program Files/Unity/Hub/Editor/6000.0.58f2/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe" install -r Builds/LaboratorioMezclas.apk
```

En el visor: *Biblioteca › Fuentes desconocidas › Laboratorio de Mezclas*.

## Opción B: desde el PC con Quest Link

1. Abrir **Meta Quest Link** en el PC y activar Link en el visor.
2. Ejecutar:

```bash
Builds/Windows/LaboratorioMezclas.exe -vr
```

Si no detecta el visor, el juego arranca en modo PC y lo avisa en el log.

## Qué revisar (unos 15 minutos)

Marca cada punto con ✔ (bien), ✘ (falla) o ~ (funciona pero se siente raro), y una nota corta.

**Arranque**
- [ ] Abre directo en VR, dentro del bar, detrás de la barra mirando al cliente
- [ ] La barra queda a la altura de la cintura (ni muy alta ni muy baja)
- [ ] Se ven las manos/controles

**Moverse**
- [ ] Joystick izquierdo camina, derecho gira
- [ ] Teletransporte detrás de la barra (joystick derecho hacia adelante, apuntar y soltar)
- [ ] No marea

**Tableros**
- [ ] Se leen bien el pedido, el libro de reacciones, el entrenamiento y el historial
- [ ] El rayo pulsa botones con el gatillo: *Saltar tutorial*, *INICIAR TURNO*, la rockola

**Ingredientes**
- [ ] Se agarra una botella con el botón lateral (de cerca y también de lejos con el rayo)
- [ ] Inclinarla sobre el vaso vierte una dosis (sale el chorro y vibra el control)
- [ ] Para la segunda dosis hay que enderezarla y volver a inclinar
- [ ] Hielo, limón, ají, azúcar, pastilla: inclinar el recipiente sobre el vaso
- [ ] Al soltar la botella vuelve sola al estante
- [ ] Las etiquetas de los ingredientes se leen

**Estación**
- [ ] La palanca de temperatura se arrastra a izquierda (frío) y derecha (caliente)
- [ ] La palanca AGITAR se tira y agita
- [ ] Sacudir el vaso con la mano también agita
- [ ] Voltear el vaso lo vacía
- [ ] El analizador cambia de color y se ve desde donde estás parado

**Servir**
- [ ] Al acercar el vaso al posavasos del cliente aparece la silueta y al soltarlo se sirve
- [ ] El vaso vuelve a la estación después de que el cliente bebe
- [ ] Si sueltas el vaso en otro lado, vuelve solo a la estación

**Turno completo**
- [ ] Un turno de principio a fin, incluida una mezcla inestable estabilizada con hielo
- [ ] Panel final y tablero de historial actualizados
- [ ] Fluidez: ¿se siente suave o a saltos? (si va a saltos, anotar en qué momento)

## Qué mandar después

- Esta lista marcada.
- Si algo se ve mal, una foto o un video corto (el Quest graba con *Botón Meta › Cámara › Grabar video*).
- El log del visor, si algo se cierra solo:

```bash
"C:/Program Files/Unity/Hub/Editor/6000.0.58f2/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe" logcat -d -s Unity > quest_log.txt
```
