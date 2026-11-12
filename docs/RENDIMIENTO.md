# Pruebas de rendimiento — Laboratorio de Mezclas

Medido con el `.exe` de Windows en modo demostración (un turno completo jugado por el propio juego),
a 1920×1080, en un portátil con NVIDIA GeForce RTX 4060 Laptop e Intel i7-13650HX.

```
Builds\Windows\ControlCalidadXR.exe -autodemo -perf -quality <0|1|2> [-pixellights N]
```

`PerfProbe` registra el tiempo de cada cuadro y los contadores de dibujado; `QualityGovernor` aplica el nivel.

## Resultados

| Nivel | Público de fondo | Luces por píxel | Llamadas de dibujado | Triángulos | FPS |
|---|---|---|---|---|---|
| Alto (PC) | 36 + 3 fijos | 12 | 2.956 | 539.000 | 60 o más |
| Medio | 16 + 3 fijos | 6 | 1.958 | 330.000 | 193 |
| VR | 8 + 3 fijos | 3 | 1.254 | 186.000 | 106 |
| Alto, 1 luz | 36 + 3 fijos | 1 | 1.071 | 188.000 | 60 o más |
| VR, 1 luz | 8 + 3 fijos | 1 | 831 | 121.000 | 60 o más |
| VR, 0 luces | 8 + 3 fijos | 0 | 704 | 96.000 | 60 o más |

Notas sobre los FPS: en varias corridas la ventana quedó limitada a 60 por el sistema (ventana sin foco),
así que "60 o más" es un piso, no el máximo. El 1 % de cuadros más lentos estuvo entre 40 y 54 FPS en todas
las corridas, incluida la más liviana: son tirones puntuales (carga de música, cambio de pedido), no carga de dibujado.

## Qué dicen los números

1. **En PC no hace falta recortar.** Con todo el público y las 12 luces el juego se mantiene en 60 FPS o más.
   El regulador queda activo como seguro: si un PC más modesto baja de 50 FPS, pasa solo a Medio y luego a VR.
2. **El costo principal son las luces en tiempo real, no la gente.** Pasar de 12 luces a 1 baja las llamadas
   de dibujado de 2.956 a 1.071 (−64 %) con el mismo público. Quitar 28 personajes con las mismas luces
   baja bastante menos.
3. **Para Meta Quest sí hay que recortar.** Un Quest 2/3 aguanta del orden de 150–250 llamadas de dibujado
   y unos 750.000 triángulos por cuadro. Los triángulos ya están dentro; las llamadas de dibujado no (704 en
   el mejor caso). Ese trabajo va en la conversión a VR.

## Plan para el visor (sin perder la esencia)

- Iluminación: una sola luz direccional y ambiente; los colores de discoteca salen de materiales emisivos
  (baldosas, neones, ecualizador), que no cuestan pasadas extra. Los focos y la bola se simulan con
  conos y destellos emisivos.
- Público: nivel VR (DJ, dos clientes en la barra, seis bailarines, la pareja del sofá).
- Decoración: fundir en una sola malla las botellas de la repisa y las piezas fijas del cliente y la barra.
- Objetivo: menos de 250 llamadas de dibujado a 72 FPS, medido en el visor con el mismo `PerfProbe`.

## Lo que ya quedó aplicado

- `QualityGovernor` con tres niveles y bajada automática por FPS (en Android arranca en VR).
- 352 objetos de decoración marcados como estáticos para agrupado.
- Los personajes de fondo no proyectan sombras y no se animan cuando están fuera de cámara.
