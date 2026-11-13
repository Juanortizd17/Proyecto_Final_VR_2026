// Genera la presentación de sustentación: node build_deck.js
const path = require("path");
const fs = require("fs");
const pptxgen = require("pptxgenjs");
const React = require("react");
const ReactDOMServer = require("react-dom/server");
const sharp = require("sharp");
const fa = require("react-icons/fa6");

const SKILL = process.env.PPTX_SKILL;
const OUT = path.join(__dirname, "Sustentacion_Laboratorio_de_Mezclas.pptx");
const DOCS = path.join(__dirname, "..", "docs");
const shot = (n) => path.join(DOCS, n + ".png");

// ---------- Tema: bar de noche (morado oscuro + cobre + neón turquesa) ----------
const THEME = {
  name: "Laboratorio de Mezclas",
  headFontFace: "Arial",
  bodyFontFace: "Calibri",
  colors: {
    dk1: "1B1530", lt1: "FFFFFF", dk2: "3B3358", lt2: "F4F2F9",
    accent1: "F29A3A", accent2: "2FBF71", accent3: "E5484D",
    accent4: "35C9B8", accent5: "8A84A3", accent6: "C26BFF",
    hlink: "35C9B8", folHlink: "8A84A3",
  },
};
const HEX = THEME.colors;

const pres = new pptxgen();
pres.layout = "LAYOUT_WIDE"; // 13.33 x 7.5 in
pres.title = "Laboratorio de Mezclas – Sustentación";
pres.subject = "Proyecto Final Fundamentos de Realidad Virtual 2026";
pres.theme = { headFontFace: THEME.headFontFace, bodyFontFace: THEME.bodyFontFace };
const C = pres.SchemeColor;
const FOOT = "Laboratorio de Mezclas  ·  Fundamentos de Realidad Virtual 2026";

pres.defineSlideMaster({
  title: "DARK_TITLE",
  background: { color: HEX.dk1 },
  objects: [
    { placeholder: { options: { name: "title", type: "title", x: 0.8, y: 2.3, w: 11.7, h: 1.6, fontSize: 54, bold: true, color: HEX.lt1, align: "left", valign: "bottom", fontFace: THEME.headFontFace }, text: "" } },
    { placeholder: { options: { name: "body", type: "body", x: 0.8, y: 4.0, w: 11.7, h: 1.2, fontSize: 22, color: HEX.accent1, align: "left", valign: "top" }, text: "" } },
  ],
});
pres.defineSlideMaster({
  title: "LIGHT_CONTENT",
  background: { color: HEX.lt1 },
  margin: [0.5, 0.6, 0.6, 0.6],
  objects: [
    { placeholder: { options: { name: "title", type: "title", x: 0.6, y: 0.35, w: 12.1, h: 0.9, fontSize: 34, bold: true, color: HEX.dk1, align: "left", valign: "middle", fontFace: THEME.headFontFace }, text: "" } },
    { text: { text: FOOT, options: { x: 0.6, y: 7.0, w: 9, h: 0.3, fontSize: 10, color: HEX.accent5, fontFace: THEME.bodyFontFace } } },
  ],
  slideNumber: { x: 12.2, y: 7.0, w: 0.6, h: 0.3, fontSize: 10, color: HEX.accent5, align: "right" },
});

async function icon(Comp, color, size = 256) {
  const svg = ReactDOMServer.renderToStaticMarkup(React.createElement(Comp, { color: "#" + color, size: String(size) }));
  const buf = await sharp(Buffer.from(svg)).resize(size, size).png().toBuffer();
  return "image/png;base64," + buf.toString("base64");
}

function iconTile(slide, data, x, y, size, bg) {
  slide.addShape(pres.ShapeType.roundRect, { x, y, w: size, h: size, rectRadius: 0.12, fill: { color: bg || HEX.accent1 }, line: { type: "none" }, objectName: "IconTile" });
  const pad = size * 0.22;
  slide.addImage({ data, x: x + pad, y: y + pad, w: size - 2 * pad, h: size - 2 * pad, objectName: "Icon" });
}

function screenshot(slide, name, x, y, w, alt) {
  const h = w * 9 / 16;
  slide.addShape(pres.ShapeType.roundRect, {
    x: x - 0.04, y: y - 0.04, w: w + 0.08, h: h + 0.08, rectRadius: 0.08, fill: { color: HEX.dk1 }, line: { type: "none" },
    shadow: { type: "outer", color: "000000", opacity: 0.25, blur: 8, offset: 3, angle: 90 }, objectName: "ShotFrame",
  });
  if (fs.existsSync(shot(name))) slide.addImage({ path: shot(name), x, y, w, h, altText: alt, objectName: "Screenshot" });
  else slide.addText("Captura: " + name, { x, y, w, h, align: "center", valign: "middle", color: HEX.lt1, fontSize: 16, isTextBox: true });
}

function caption(slide, text, x, y, w) {
  slide.addText(text, { x, y, w, h: 0.35, fontSize: 12, italic: true, color: HEX.accent5, margin: 0, isTextBox: true });
}

function content(title, section) {
  const s = pres.addSlide({ masterName: "LIGHT_CONTENT", sectionTitle: section });
  s.addText(title, { placeholder: "title" });
  return s;
}

function bullets(slide, items, x, y, w, h, fontSize = 16) {
  slide.addText(items.map((t, i) => ({ text: t, options: { bullet: true, breakLine: i < items.length - 1 } })),
    { x, y, w, h, fontSize, color: HEX.dk1, paraSpaceAfter: 8, valign: "top", isTextBox: true });
}

function table(slide, header, rows, opts) {
  const hdr = header.map((t) => ({ text: t, options: { bold: true, color: HEX.lt1, fill: { color: HEX.dk1 } } }));
  slide.addTable([hdr, ...rows], {
    fontSize: 14, color: HEX.dk1, fontFace: THEME.bodyFontFace, border: { type: "solid", pt: 0.5, color: "D8D4E4" },
    valign: "middle", fill: { color: HEX.lt1 }, ...opts,
  });
}

(async () => {
  const I = {
    flask: await icon(fa.FaFlask, HEX.dk1), fire: await icon(fa.FaFire, HEX.dk1), list: await icon(fa.FaListCheck, HEX.dk1),
    snow: await icon(fa.FaSnowflake, HEX.dk1), book: await icon(fa.FaBookOpen, HEX.dk1), hand: await icon(fa.FaHandBackFist, HEX.dk1),
    plug: await icon(fa.FaPlug, HEX.dk1), pointer: await icon(fa.FaHandPointer, HEX.dk1), sliders: await icon(fa.FaSliders, HEX.dk1),
    check: await icon(fa.FaCircleCheck, HEX.dk1), vr: await icon(fa.FaVrCardboard, HEX.dk1), wave: await icon(fa.FaWaveSquare, HEX.dk1),
    school: await icon(fa.FaGraduationCap, HEX.dk1), play: await icon(fa.FaPlay, HEX.dk1), martini: await icon(fa.FaMartiniGlass, HEX.dk1),
    flag: await icon(fa.FaFlagCheckered, HEX.dk1), clock: await icon(fa.FaStopwatch, HEX.dk1), bolt: await icon(fa.FaBolt, HEX.dk1),
    moon: await icon(fa.FaMoon, HEX.dk1), skull: await icon(fa.FaSkullCrossbones, HEX.dk1), db: await icon(fa.FaDatabase, HEX.dk1),
    gauge: await icon(fa.FaGaugeHigh, HEX.dk1), github: await icon(fa.FaGithub, HEX.lt1), music: await icon(fa.FaMusic, HEX.dk1),
  };

  // 1 · Portada
  pres.addSection({ title: "Introducción" });
  let s = pres.addSlide({ masterName: "DARK_TITLE", sectionTitle: "Introducción" });
  if (fs.existsSync(shot("screenshots/mix/02_general")))
    s.addImage({ path: shot("screenshots/mix/02_general"), x: 0, y: 0, w: 13.333, h: 7.5, transparency: 70, altText: "Vista general del bar", objectName: "CoverImage" });
  s.addText("Laboratorio de Mezclas", { placeholder: "title" });
  s.addText("Opción 3 · Laboratorio Científico XR\nUAO Immersive Solutions · Unity 6 + XR Interaction Toolkit · Meta Quest 3", { placeholder: "body" });
  s.addNotes("Somos UAO Immersive Solutions. Elegimos la opción 3, Laboratorio Científico XR, y la ambientamos en un bar: el Laboratorio de Mezclas.");

  // 2 · La idea
  s = content("La idea: un laboratorio de química detrás de una barra", "Introducción");
  bullets(s, [
    "El cliente pide una bebida: esa es la fórmula a lograr.",
    "Cada ingrediente tiene propiedades químicas: ácido, gas, alcohol, picante, lácteo, cafeína, frío.",
    "La mezcla y la temperatura deciden la reacción del cliente.",
    "Acertar suma puntos; equivocarse provoca reacciones grotescas.",
    "Antes de servir, el analizador clasifica la mezcla.",
  ], 0.6, 1.5, 5.6, 4.6, 17);
  screenshot(s, "pc_demo/03_pedido", 6.5, 1.5, 6.2, "Pedido del cliente y tableros");
  caption(s, "Pedido, tiempo, puntos, libro de reacciones y entrenamiento", 6.5, 5.1, 6.2);
  s.addNotes("La idea es que el jugador experimente con sustancias y vea el resultado. En vez de tubos de ensayo, son ingredientes con propiedades, y el resultado observable es la reacción del cliente.");

  // 3 · Opción 3
  s = content("Las cinco funciones de la opción 3", "Introducción");
  const f5 = [
    [I.flask, "Combinar", "19 ingredientes, hasta 4 por mezcla, temperatura y agitado"],
    [I.fire, "Activar reacciones", "17 reacciones según las propiedades sumadas"],
    [I.list, "Clasificar", "Analizador: estable, inestable o tóxica"],
    [I.snow, "Estabilizar", "El frío neutraliza lo inestable; si no, el vaso estalla"],
    [I.book, "Descubrir fórmulas", "38 recetas y un libro que se guarda"],
  ];
  f5.forEach(([ic, t, d], i) => {
    const x = 0.6 + i * 2.48;
    s.addShape(pres.ShapeType.roundRect, { x, y: 1.6, w: 2.3, h: 3.6, rectRadius: 0.12, fill: { color: HEX.lt2 }, line: { type: "none" }, objectName: "FnCard" });
    iconTile(s, ic, x + 0.65, 1.9, 1.0, i % 2 ? HEX.accent4 : HEX.accent1);
    s.addText(t, { x: x + 0.1, y: 3.05, w: 2.1, h: 0.6, fontSize: 18, bold: true, color: HEX.dk1, align: "center", isTextBox: true });
    s.addText(d, { x: x + 0.15, y: 3.65, w: 2.0, h: 1.4, fontSize: 13, color: HEX.dk2, align: "center", valign: "top", isTextBox: true });
  });
  s.addShape(pres.ShapeType.roundRect, { x: 0.6, y: 5.5, w: 12.1, h: 1.0, rectRadius: 0.1, fill: { color: HEX.dk1 }, line: { type: "none" }, objectName: "Callout" });
  s.addText("Validación química: ReactionRules evalúa la mezcla, el analizador avisa y el cliente reacciona.", { x: 0.9, y: 5.5, w: 11.5, h: 1.0, fontSize: 17, color: HEX.lt1, valign: "middle", isTextBox: true });
  s.addNotes("El enunciado de la opción 3 pide combinar, activar reacciones, clasificar, estabilizar y descubrir fórmulas. Cada una tiene una mecánica concreta en el juego.");

  // 4 · Flujo
  pres.addSection({ title: "Experiencia" });
  s = content("Flujo de la experiencia", "Experiencia");
  const steps = [
    [I.school, "Entrenamiento", "Moverse, agarrar y verter, agitar, servir"],
    [I.play, "Turno", "Pedidos con tiempo y paciencia del cliente"],
    [I.martini, "Servicio", "Mezclar, analizar, estabilizar y servir"],
    [I.fire, "Reacción", "Puntos, síntomas y nueva reacción en el libro"],
    [I.flag, "Cierre", "Resumen, récord e historial guardado"],
  ];
  steps.forEach(([ic, t, d], i) => {
    const x = 0.6 + i * 2.5;
    iconTile(s, ic, x + 0.65, 1.9, 1.0);
    s.addText(String(i + 1), { x: x + 0.15, y: 1.9, w: 0.45, h: 0.45, fontSize: 18, bold: true, color: HEX.accent5, margin: 0, isTextBox: true });
    s.addText(t, { x, y: 3.1, w: 2.3, h: 0.5, fontSize: 20, bold: true, color: HEX.dk1, align: "center", isTextBox: true });
    s.addText(d, { x, y: 3.6, w: 2.3, h: 1.1, fontSize: 14, color: HEX.dk2, align: "center", valign: "top", isTextBox: true });
    if (i < steps.length - 1)
      s.addShape(pres.ShapeType.rightArrow, { x: x + 1.85, y: 2.25, w: 0.5, h: 0.3, fill: { color: HEX.accent5 }, line: { type: "none" }, objectName: "FlowArrow" });
  });
  s.addShape(pres.ShapeType.roundRect, { x: 0.6, y: 5.2, w: 12.1, h: 1.2, rectRadius: 0.1, fill: { color: HEX.lt2 }, line: { type: "none" }, objectName: "Callout" });
  s.addText("Máquina de estados en MixGameManager: Onboarding → Playing → Finished. Los demás sistemas escuchan sus eventos.", { x: 0.9, y: 5.2, w: 11.5, h: 1.2, fontSize: 17, color: HEX.dk1, valign: "middle", isTextBox: true });
  s.addNotes("El flujo está estructurado con estados explícitos y eventos C#.");

  // 5 · Onboarding
  s = content("Onboarding: aprender haciendo", "Experiencia");
  screenshot(s, "screenshots/mix/02_general", 0.6, 1.5, 6.6, "Tablero de entrenamiento");
  caption(s, "Tablero de entrenamiento a la derecha; INICIAR TURNO aparece al completarlo", 0.6, 5.3, 6.6);
  const ob = [
    ["Moverse", "Joystick izquierdo camina, derecho gira o teletransporta"],
    ["Agarrar y verter", "Grip sobre una botella e inclinarla sobre el vaso"],
    ["Agitar", "Sacudir el vaso o tirar de la palanca"],
    ["Servir", "Soltar el vaso en el posavasos del cliente (socket)"],
  ];
  ob.forEach(([t, d], i) => {
    const y = 1.5 + i * 0.95;
    s.addShape(pres.ShapeType.ellipse, { x: 7.7, y, w: 0.6, h: 0.6, fill: { color: HEX.accent1 }, line: { type: "none" }, objectName: "StepDot" });
    s.addText(String(i + 1), { x: 7.7, y, w: 0.6, h: 0.6, fontSize: 18, bold: true, color: HEX.dk1, align: "center", valign: "middle", margin: 0, isTextBox: true });
    s.addText([{ text: t, options: { bold: true, breakLine: true } }, { text: d, options: { color: HEX.dk2 } }],
      { x: 8.5, y: y - 0.1, w: 4.3, h: 0.85, fontSize: 15, color: HEX.dk1, valign: "middle", isTextBox: true });
  });
  s.addText("Cada paso se valida con un evento real del juego; los textos cambian entre PC y VR.", { x: 7.7, y: 5.4, w: 5.1, h: 0.7, fontSize: 14, italic: true, color: HEX.dk2, isTextBox: true });
  s.addNotes("Ningún paso avanza con un botón de siguiente: se detecta el movimiento de la cabeza, el cambio del vaso, el agitado y la reacción del cliente.");

  // 6 · Interacciones XR
  s = content("Interacciones con XR Interaction Toolkit", "Experiencia");
  const inter = [
    [I.hand, "Grab Interactable", "19 botellas y recipientes, y el vaso"],
    [I.martini, "Verter inclinando", "Ángulo de la botella y boca sobre el vaso"],
    [I.plug, "Socket químico", "Posavasos que solo acepta el vaso con mezcla"],
    [I.sliders, "Palancas", "Temperatura de 3 posiciones y palanca de agitar"],
    [I.pointer, "UI XR", "Rayo y gatillo sobre tableros y rockola"],
    [I.wave, "Háptica y audio 3D", "Vibración al verter y en mezclas inestables"],
  ];
  inter.forEach(([ic, t, d], i) => {
    const col = i % 2, row = Math.floor(i / 2);
    const x = 0.6 + col * 3.35, y = 1.5 + row * 1.7;
    iconTile(s, ic, x, y, 0.85);
    s.addText(t, { x: x + 1.0, y, w: 2.2, h: 0.4, fontSize: 16, bold: true, color: HEX.dk1, margin: 0, isTextBox: true });
    s.addText(d, { x: x + 1.0, y: y + 0.42, w: 2.2, h: 1.1, fontSize: 12, color: HEX.dk2, margin: 0, valign: "top", isTextBox: true });
  });
  screenshot(s, "screenshots/mix/05_estacion", 7.5, 1.5, 5.2, "Estación de mezcla");
  caption(s, "Estación: palanca de temperatura, vaso, analizador y palanca de agitar", 7.5, 4.55, 5.2);
  s.addNotes("XR Origin de los Starter Assets, XRGrabInteractable, XRSimpleInteractable, un XRSocketInteractor propio, TeleportationArea y XRUIInputModule.");

  // 7 · Reacciones y analizador
  s = content("Reacciones, clasificación y estabilización", "Experiencia");
  screenshot(s, "screenshots/mix/reaccion_Llamarada", 0.6, 1.5, 4.0, "Llamarada");
  caption(s, "Llamarada: alcohol fuerte + caliente", 0.6, 3.85, 4.0);
  screenshot(s, "pc_demo/06_reaccion_Nausea", 4.75, 1.5, 4.0, "Náusea");
  caption(s, "Náusea verde: leche + limón", 4.75, 3.85, 4.0);
  screenshot(s, "pc_demo/14_reaccion_Explosion", 8.9, 1.5, 3.8, "Explosión");
  caption(s, "Explosión: gas + efervescente sin enfriar", 8.9, 3.75, 3.8);
  table(s, ["Analizador", "Qué significa", "Qué hacer"], [
    [{ text: "ESTABLE", options: { color: HEX.accent2, bold: true } }, "Se puede servir", "Servir"],
    [{ text: "INESTABLE", options: { color: HEX.accent1, bold: true } }, "Explosiva o inflamable; cuenta regresiva de 9 s", "Enfriar con hielo o palanca en FRÍO (+20)"],
    [{ text: "TÓXICA", options: { color: HEX.accent3, bold: true } }, "Provocará una reacción fuerte", "Vaciarla"],
  ], { x: 0.6, y: 4.45, w: 12.1, colW: [2.2, 5.0, 4.9], rowH: 0.48 });
  s.addNotes("Diecisiete reacciones con síntomas visibles. El analizador clasifica antes de servir y las mezclas inestables deben estabilizarse con frío o el vaso explota.");

  // 8 · Eventos dinámicos
  pres.addSection({ title: "Sistemas" });
  s = content("Eventos dinámicos y sistema reactivo", "Sistemas");
  const stats = [
    [I.clock, "Tiempo", "turno y paciencia del cliente"],
    [I.bolt, "Hora pico", "la paciencia baja el doble"],
    [I.moon, "Apagón", "luces y etiquetas apagadas"],
    [I.skull, "3 desmayos", "suspenden el turno"],
  ];
  stats.forEach(([ic, n, l], i) => {
    const x = 0.6 + i * 3.05;
    s.addShape(pres.ShapeType.roundRect, { x, y: 1.6, w: 2.8, h: 2.6, rectRadius: 0.12, fill: { color: HEX.lt2 }, line: { type: "none" }, objectName: "StatCard" });
    iconTile(s, ic, x + 0.25, 1.85, 0.6);
    s.addText(n, { x: x + 0.2, y: 2.6, w: 2.4, h: 0.8, fontSize: 30, bold: true, color: HEX.dk1, margin: 0, isTextBox: true });
    s.addText(l, { x: x + 0.2, y: 3.4, w: 2.4, h: 0.7, fontSize: 14, color: HEX.dk2, margin: 0, valign: "top", isTextBox: true });
  });
  bullets(s, [
    "La mezcla inestable estalla en la estación si nadie la estabiliza (−30).",
    "El público aplaude, se asusta o se ríe según la reacción; las luces de la pista cambian de color.",
    "Todo se comunica con eventos C#: OnStateChanged, OnReaction, OnScoreChanged, OnAlert.",
  ], 0.6, 4.55, 12.1, 2.0, 16);
  s.addNotes("Temporizador, fallo, evento inesperado y sistema reactivo, como pide el enunciado.");

  // 9 · Puntuación y cierre
  s = content("Puntuación, progreso y cierre", "Sistemas");
  screenshot(s, "pc_demo/16_panel_final", 0.6, 1.5, 7.0, "Panel final");
  caption(s, "Panel final con puntaje, récord, perfectos y reacciones descubiertas", 0.6, 5.55, 7.0);
  const sc = [["+100", "pedido perfecto + bono por rapidez", HEX.accent2], ["+20", "por estabilizar una mezcla", HEX.accent4], ["−30", "si el vaso estalla", HEX.accent3]];
  sc.forEach(([n, l, c], i) => {
    const y = 1.5 + i * 1.1;
    s.addText(n, { x: 8.0, y, w: 1.9, h: 0.9, fontSize: 40, bold: true, color: c, margin: 0, isTextBox: true });
    s.addText(l, { x: 9.9, y: y + 0.15, w: 2.9, h: 0.7, fontSize: 15, color: HEX.dk1, margin: 0, isTextBox: true });
  });
  bullets(s, ["Cada reacción suma o resta según su gravedad", "Retroalimentación según el desempeño", "Libro de reacciones y tablero de historial"], 8.0, 4.9, 4.8, 1.6, 15);
  s.addNotes("La puntuación es verificable y queda guardada en el JSON y en el CSV.");

  // 10 · Arquitectura
  pres.addSection({ title: "Implementación" });
  s = content("Arquitectura: una lógica, dos formas de jugar", "Implementación");
  const box = (t, d, x, y, w, fill, txt) => {
    s.addShape(pres.ShapeType.roundRect, { x, y, w, h: 0.95, rectRadius: 0.1, fill: { color: fill }, line: { type: "none" }, objectName: "ArchBox" });
    s.addText([{ text: t, options: { bold: true, breakLine: true } }, { text: d, options: { fontSize: 11 } }], { x, y, w, h: 0.95, fontSize: 15, color: txt, align: "center", valign: "middle", isTextBox: true });
  };
  box("MixGameManager", "estados · pedidos · puntaje · eventos", 4.9, 3.1, 3.5, HEX.dk1, HEX.lt1);
  const around = [
    ["ReactionRules", "reacción · clasificar · estabilizar", 0.6, 1.5], ["MixGlass + MixAnalyzer", "contenido · capas · cuenta regresiva", 4.9, 1.5],
    ["Customer", "síntomas de cada reacción", 9.2, 1.5], ["PC: MixInteractor", "mira y clic", 0.6, 4.7],
    ["VR: MixVrRig + Vr*", "XR Origin · grab · socket · palancas", 4.9, 4.7], ["MixSaveRepository", "JSON + CSV", 9.2, 4.7],
  ];
  around.forEach(([t, d, x, y]) => {
    box(t, d, x, y, 3.5, HEX.lt2, HEX.dk1);
    s.addShape(pres.ShapeType.line, {
      x: Math.min(x + 1.75, 6.65), y: y < 3 ? y + 0.95 : 4.05, w: Math.abs(x + 1.75 - 6.65), h: y < 3 ? 3.1 - (y + 0.95) : y - 4.05,
      flipH: x + 1.75 > 6.65, line: { color: HEX.accent5, width: 1.5 }, objectName: "ArchLink",
    });
  });
  s.addText("La entrada de PC y la de VR llaman a los mismos métodos (glass.Add, lever.Set, gm.Serve): todo se prueba sin visor.", { x: 0.6, y: 6.0, w: 12.1, h: 0.5, fontSize: 13, italic: true, color: HEX.dk2, isTextBox: true });
  s.addNotes("El MixGameManager no conoce a los tableros ni a las luces: ellos se suscriben a sus eventos. ReactionRules es lógica pura y está cubierta por pruebas.");

  // 11 · Manejo de datos
  s = content("Manejo de datos", "Implementación");
  iconTile(s, I.db, 0.6, 1.55, 0.9);
  s.addText("Configuración, progreso y exportación", { x: 1.7, y: 1.5, w: 4.6, h: 1.0, fontSize: 18, bold: true, color: HEX.dk1, valign: "middle", isTextBox: true });
  bullets(s, [
    "ScriptableObjects: 19 ingredientes, 38 recetas y 17 reacciones editables sin código",
    "mix_save.json: récord, reacciones descubiertas y últimos 50 turnos",
    "mix_save_historial.csv: el historial listo para Excel",
    "Tablero de historial en el bar: récord, promedio y últimos turnos",
    "PlayerPrefs: género y volumen de la música",
  ], 0.6, 2.75, 5.8, 3.8, 15);
  s.addShape(pres.ShapeType.roundRect, { x: 6.7, y: 1.5, w: 6.0, h: 4.6, rectRadius: 0.1, fill: { color: HEX.dk1 }, line: { type: "none" }, objectName: "CodeCard" });
  s.addText(
    '{\n  "bestScore": 755,\n  "discovered": ["Perfecto", "Nausea", "Explosion"],\n  "sessions": [\n    {\n      "date": "2026-11-13 18:45",\n      "score": 536,\n      "served": 10,\n      "perfects": 5,\n      "knockouts": 0,\n      "completed": true\n    }\n  ]\n}\n\nturno;fecha;puntaje;bebidas_servidas;...\n1;2026-11-13 18:45;536;10;5;0;si',
    { x: 6.95, y: 1.65, w: 5.6, h: 4.3, fontFace: "Courier New", fontSize: 12, color: HEX.accent1, valign: "top", isTextBox: true });
  s.addNotes("Los datos se serializan con JsonUtility y se exportan a CSV. Lectura y escritura protegidas: si el archivo falta, el juego empieza limpio.");

  // 12 · Organización y pruebas
  s = content("Organización del proyecto y pruebas", "Implementación");
  s.addShape(pres.ShapeType.roundRect, { x: 0.6, y: 1.5, w: 5.6, h: 4.9, rectRadius: 0.1, fill: { color: HEX.lt2 }, line: { type: "none" }, objectName: "TreeCard" });
  s.addText(
    "Assets/\n├ Scripts/MixLab/\n│   Core · Data · Interaction\n│   Customer · UI · Ambience\n│   Desktop · VR\n├ Editor/      generador de escena\n├ Data/Mix/    ScriptableObjects\n├ Scenes/      MixLab\n├ Tests/       17 pruebas PlayMode\n└ Samples/     XRI Starter Assets",
    { x: 0.85, y: 1.7, w: 5.2, h: 4.5, fontFace: "Courier New", fontSize: 15, color: HEX.dk1, valign: "top", isTextBox: true });
  const org = [
    ["Escena reproducible", "Tools › Mix Lab › Build Scene genera la escena, los datos y la capa VR desde código"],
    ["Assembly definitions", "MixLab y MixLab.Tests; namespace MixLab"],
    ["17 pruebas automáticas", "Reglas, flujo completo, datos y VR simulado (verter, agitar, palancas, socket)"],
    ["Control de versiones", "Repositorio público en GitHub con commits por funcionalidad"],
  ];
  org.forEach(([t, d], i) => {
    s.addText([{ text: t, options: { bold: true, breakLine: true } }, { text: d, options: { color: HEX.dk2 } }],
      { x: 6.6, y: 1.5 + i * 1.25, w: 6.1, h: 1.1, fontSize: 15, color: HEX.dk1, valign: "top", isTextBox: true });
  });
  s.addNotes("Separamos scripts por dominio, todo bajo un assembly definition, y la escena se genera desde código.");

  // 13 · Optimización
  s = content("Optimización para Meta Quest", "Implementación");
  const opt = [
    ["Regulador de calidad", "Tres niveles: limita público, luces extra y luces por píxel según los FPS"],
    ["Medición propia", "PerfProbe mide FPS y llamadas de dibujado en cada nivel"],
    ["Agrupado estático", "352 objetos de decoración marcados para unir llamadas"],
    ["Personajes livianos", "Sin sombras; animación solo cuando se ven"],
    ["Android", "IL2CPP ARM64, Vulkan, texturas ASTC, MSAA 4×"],
    ["Texto en VR", "Tamaños fijos y lienzos optimizados"],
  ];
  opt.forEach(([t, d], i) => {
    const col = i % 3, row = Math.floor(i / 3);
    const x = 0.6 + col * 4.1, y = 1.6 + row * 2.4;
    s.addShape(pres.ShapeType.roundRect, { x, y, w: 3.8, h: 2.1, rectRadius: 0.1, fill: { color: HEX.lt2 }, line: { type: "none" }, objectName: "OptCard" });
    iconTile(s, I.gauge, x + 0.25, y + 0.25, 0.55);
    s.addText(t, { x: x + 0.25, y: y + 0.9, w: 3.3, h: 0.5, fontSize: 17, bold: true, color: HEX.dk1, margin: 0, isTextBox: true });
    s.addText(d, { x: x + 0.25, y: y + 1.4, w: 3.3, h: 0.6, fontSize: 13, color: HEX.dk2, margin: 0, valign: "top", isTextBox: true });
  });
  s.addNotes("Las mediciones mostraron que el costo está en las luces en tiempo real, no en los personajes. El regulador baja de nivel solo si caen los FPS.");

  // 14 · Rúbrica
  pres.addSection({ title: "Cierre" });
  s = content("Cómo cubrimos la rúbrica", "Cierre");
  table(s, ["Criterio", "Peso", "Evidencia en el proyecto"], [
    ["Interfaz de usuario", "15%", "Tableros grandes en World Space, analizador por colores, panel final"],
    ["Implementación 3D", "15%", "Bar con barra en L, pista, DJ, público animado, reacciones con efectos"],
    ["Interacciones", "25%", "Grab, verter inclinando, socket, palancas, UI XR, háptica"],
    ["Inmersión", "25%", "Música y luces sincronizadas, público reactivo, audio 3D"],
    ["Montaje general", "20%", "Flujo completo de entrenamiento a cierre; APK para Quest 3"],
    ["Organización Unity", "15%", "Carpetas por dominio, asmdef, escena reproducible"],
    ["Scripts", "15%", "Eventos C#, lógica separada de la entrada, 17 pruebas"],
    ["Manejo de datos", "20%", "ScriptableObjects, JSON, CSV y tablero de historial"],
  ], { x: 0.6, y: 1.45, w: 12.1, colW: [2.9, 1.0, 8.2], rowH: 0.52 });
  s.addNotes("Esta tabla resume cada criterio con la evidencia que mostramos en la demo.");

  // 15 · Demo
  s = pres.addSlide({ masterName: "DARK_TITLE", sectionTitle: "Cierre" });
  if (fs.existsSync(shot("screenshots/mix/08_vista_bar")))
    s.addImage({ path: shot("screenshots/mix/08_vista_bar"), x: 0, y: 0, w: 13.333, h: 7.5, transparency: 72, altText: "Vista del bar", objectName: "ClosingImage" });
  s.addText("Demo en vivo y preguntas", { placeholder: "title" });
  s.addText("github.com/Juanortizd17/Proyecto_Final_VR_2026", { placeholder: "body" });
  s.addImage({ data: I.github, x: 0.8, y: 5.4, w: 0.55, h: 0.55, objectName: "GithubIcon" });
  s.addText("Repositorio público con el proyecto Unity, los scripts y esta presentación", { x: 1.5, y: 5.4, w: 9, h: 0.55, fontSize: 16, color: HEX.lt1, valign: "middle", isTextBox: true });
  s.addNotes("Demo: entrenamiento, un pedido perfecto, una mezcla inestable estabilizada con hielo, una reacción fuerte y el historial.");

  await pres.writeFile({ fileName: OUT });
  if (SKILL) {
    const { applyTheme } = require(path.join(SKILL, "scripts", "apply_theme.js"));
    await applyTheme(OUT, THEME);
  }
  console.log("OK " + OUT);
})().catch((e) => { console.error(e); process.exit(1); });
