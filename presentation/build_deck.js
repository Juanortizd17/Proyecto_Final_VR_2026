// Genera la presentación de sustentación: node build_deck.js
const path = require("path");
const fs = require("fs");
const pptxgen = require("pptxgenjs");
const React = require("react");
const ReactDOMServer = require("react-dom/server");
const sharp = require("sharp");
const fa = require("react-icons/fa6");

const SKILL = process.env.PPTX_SKILL;
const OUT = path.join(__dirname, "Sustentacion_Control_de_Calidad_XR.pptx");
const SHOTS = path.join(__dirname, "..", "docs", "screenshots");
const shot = (n) => path.join(SHOTS, n + ".png");

// ---------- Tema: nave industrial (carbón + amarillo de seguridad) ----------
const THEME = {
  name: "Control de Calidad XR",
  headFontFace: "Arial",
  bodyFontFace: "Calibri",
  colors: {
    dk1: "1E2228", lt1: "FFFFFF", dk2: "3A4250", lt2: "F3F4F6",
    accent1: "FFC21A", accent2: "2BB673", accent3: "E5484D",
    accent4: "3D8BFD", accent5: "8A94A6", accent6: "FF8A3D",
    hlink: "3D8BFD", folHlink: "8A94A6",
  },
};
const HEX = THEME.colors;

const pres = new pptxgen();
pres.layout = "LAYOUT_WIDE"; // 13.33 x 7.5 in
pres.author = "Juanortizd17";
pres.title = "Control de Calidad XR – Sustentación";
pres.subject = "Proyecto Final Fundamentos de Realidad Virtual 2026";
pres.theme = { headFontFace: THEME.headFontFace, bodyFontFace: THEME.bodyFontFace };
const C = pres.SchemeColor;
const FOOT = "Control de Calidad XR  ·  Fundamentos de Realidad Virtual 2026";

// ---------- Layouts ----------
pres.defineSlideMaster({
  title: "DARK_TITLE",
  background: { color: HEX.dk1 },
  objects: [
    { placeholder: { options: { name: "title", type: "title", x: 0.8, y: 2.3, w: 11.7, h: 1.6, fontSize: 54, bold: true, color: C.background1, align: "left", valign: "bottom", fontFace: THEME.headFontFace }, text: "" } },
    { placeholder: { options: { name: "body", type: "body", x: 0.8, y: 4.0, w: 11.7, h: 1.2, fontSize: 22, color: C.accent1, align: "left", valign: "top" }, text: "" } },
  ],
});
pres.defineSlideMaster({
  title: "LIGHT_CONTENT",
  background: { color: HEX.lt1 },
  margin: [0.5, 0.6, 0.6, 0.6],
  objects: [
    { placeholder: { options: { name: "title", type: "title", x: 0.6, y: 0.35, w: 12.1, h: 0.9, fontSize: 36, bold: true, color: C.text1, align: "left", valign: "middle", fontFace: THEME.headFontFace }, text: "" } },
    { text: { text: FOOT, options: { x: 0.6, y: 7.0, w: 9, h: 0.3, fontSize: 10, color: C.accent5, fontFace: THEME.bodyFontFace } } },
  ],
  slideNumber: { x: 12.2, y: 7.0, w: 0.6, h: 0.3, fontSize: 10, color: HEX.accent5, align: "right" },
});

// ---------- Helpers ----------
async function icon(Comp, color, size = 256) {
  const svg = ReactDOMServer.renderToStaticMarkup(React.createElement(Comp, { color: "#" + color, size: String(size) }));
  const buf = await sharp(Buffer.from(svg)).resize(size, size).png().toBuffer();
  return "image/png;base64," + buf.toString("base64");
}

function iconTile(slide, data, x, y, size, bg) {
  slide.addShape(pres.ShapeType.roundRect, {
    x, y, w: size, h: size, rectRadius: 0.12, fill: { color: bg || C.accent1 }, line: { type: "none" }, objectName: "IconTile",
  });
  const pad = size * 0.22;
  slide.addImage({ data, x: x + pad, y: y + pad, w: size - 2 * pad, h: size - 2 * pad, objectName: "Icon" });
}

function screenshot(slide, name, x, y, w, alt) {
  const h = w * 9 / 16;
  slide.addShape(pres.ShapeType.roundRect, {
    x: x - 0.04, y: y - 0.04, w: w + 0.08, h: h + 0.08, rectRadius: 0.08,
    fill: { color: C.text1 }, line: { type: "none" },
    shadow: { type: "outer", color: "000000", opacity: 0.25, blur: 8, offset: 3, angle: 90 }, objectName: "ShotFrame",
  });
  if (fs.existsSync(shot(name))) {
    slide.addImage({ path: shot(name), x, y, w, h, altText: alt, objectName: "Screenshot" });
  } else {
    slide.addText("Captura: " + name, { x, y, w, h, align: "center", valign: "middle", color: C.background1, fontSize: 16, isTextBox: true });
  }
}

function caption(slide, text, x, y, w) {
  slide.addText(text, { x, y, w, h: 0.35, fontSize: 12, italic: true, color: C.accent5, margin: 0, isTextBox: true });
}

function content(title, section) {
  const s = pres.addSlide({ masterName: "LIGHT_CONTENT", sectionTitle: section });
  s.addText(title, { placeholder: "title" });
  return s;
}

(async () => {
  const I = {
    hand: await icon(fa.FaHandBackFist, HEX.dk1), plug: await icon(fa.FaPlug, HEX.dk1),
    bolt: await icon(fa.FaBolt, HEX.dk1), clock: await icon(fa.FaStopwatch, HEX.dk1),
    trophy: await icon(fa.FaTrophy, HEX.dk1), flag: await icon(fa.FaFlagCheckered, HEX.dk1),
    search: await icon(fa.FaMagnifyingGlass, HEX.dk1), screw: await icon(fa.FaScrewdriver, HEX.dk1),
    hammer: await icon(fa.FaHammer, HEX.dk1), vr: await icon(fa.FaVrCardboard, HEX.dk1),
    wave: await icon(fa.FaWaveSquare, HEX.dk1), volume: await icon(fa.FaVolumeHigh, HEX.dk1),
    pointer: await icon(fa.FaHandPointer, HEX.dk1), school: await icon(fa.FaGraduationCap, HEX.dk1),
    play: await icon(fa.FaPlay, HEX.dk1), box: await icon(fa.FaBoxOpen, HEX.dk1),
    warn: await icon(fa.FaTriangleExclamation, HEX.dk1), db: await icon(fa.FaDatabase, HEX.dk1),
    github: await icon(fa.FaGithub, HEX.lt1), check: await icon(fa.FaCircleCheck, HEX.accent2),
    x: await icon(fa.FaCircleXmark, HEX.accent3), gauge: await icon(fa.FaGaugeHigh, HEX.dk1),
  };

  // 1 · Portada -------------------------------------------------------------
  pres.addSection({ title: "Introducción" });
  let s = pres.addSlide({ masterName: "DARK_TITLE", sectionTitle: "Introducción" });
  if (fs.existsSync(shot("01_vista_general")))
    s.addImage({ path: shot("01_vista_general"), x: 0, y: 0, w: 13.333, h: 7.5, transparency: 72, altText: "Vista general de la nave industrial en VR", objectName: "CoverImage" });
  s.addText("Control de Calidad XR", { placeholder: "title" });
  s.addText("Proyecto Final · Fundamentos de Realidad Virtual 2026\nUAO Immersive Solutions · Unity 6 + XR Interaction Toolkit · Meta Quest 2/3", { placeholder: "body" });
  s.addNotes("Presentación del proyecto. Somos UAO Immersive Solutions y elegimos el reto Control de Calidad XR: una línea de inspección industrial en realidad virtual para Meta Quest.");

  // 2 · El reto --------------------------------------------------------------
  s = content("El reto: inspeccionar antes de aprobar", "Introducción");
  s.addText([
    { text: "Una banda transportadora entrega 5 tipos de producto terminado.", options: { bullet: true, breakLine: true } },
    { text: "El operario los examina con lupa, destornillador y martillo.", options: { bullet: true, breakLine: true } },
    { text: "Con defecto: se desecha y se genera el reporte del daño.", options: { bullet: true, breakLine: true } },
    { text: "Perfecto: se almacena en el contenedor de su tipo.", options: { bullet: true } },
  ], { x: 0.6, y: 1.5, w: 5.4, h: 3.2, fontSize: 18, color: C.text1, paraSpaceAfter: 10, valign: "top", isTextBox: true });
  s.addText("Productos: radio · lámpara · parlante · televisor · oso de peluche", { x: 0.6, y: 4.9, w: 5.4, h: 0.8, fontSize: 15, italic: true, color: C.text2, isTextBox: true });
  screenshot(s, "01_vista_general", 6.4, 1.5, 6.3, "Vista general de la línea de inspección");
  caption(s, "Vista general de la nave: banda, mesa de herramientas y contenedores", 6.4, 5.15, 6.3);
  s.addNotes("La problemática del enunciado: el usuario está en una banda que entrega 5 productos y debe decidir con herramientas si tienen defecto. Si lo tienen, se desecha y se reporta; si no, va a su sitio.");

  // 3 · Flujo ---------------------------------------------------------------
  pres.addSection({ title: "Experiencia" });
  s = content("Flujo de la experiencia", "Experiencia");
  const steps = [
    [I.school, "Onboarding", "Aprende a moverse, agarrar, inspeccionar y colocar"],
    [I.play, "Turno de 3:00", "La banda arranca y los productos llegan"],
    [I.search, "Inspección", "Cada herramienta revela un tipo de defecto"],
    [I.box, "Clasificación", "Desechar y reportar, o aprobar y almacenar"],
    [I.flag, "Cierre", "Puntaje, retroalimentación y récord guardado"],
  ];
  steps.forEach(([ic, t, d], i) => {
    const x = 0.6 + i * 2.5;
    iconTile(s, ic, x + 0.65, 1.9, 1.0);
    s.addText(String(i + 1), { x: x + 0.15, y: 1.9, w: 0.45, h: 0.45, fontSize: 18, bold: true, color: C.accent5, margin: 0, isTextBox: true });
    s.addText(t, { x, y: 3.1, w: 2.3, h: 0.5, fontSize: 20, bold: true, color: C.text1, align: "center", isTextBox: true });
    s.addText(d, { x, y: 3.6, w: 2.3, h: 1.1, fontSize: 14, color: C.text2, align: "center", valign: "top", isTextBox: true });
    if (i < steps.length - 1)
      s.addShape(pres.ShapeType.rightArrow, { x: x + 1.85, y: 2.25, w: 0.5, h: 0.3, fill: { color: C.accent5 }, line: { type: "none" }, objectName: "FlowArrow" });
  });
  s.addShape(pres.ShapeType.roundRect, { x: 0.6, y: 5.2, w: 12.1, h: 1.2, rectRadius: 0.1, fill: { color: C.background2 }, line: { type: "none" }, objectName: "Callout" });
  s.addText("Flujo estructurado con estados explícitos: Onboarding → Playing → Finished, gestionados por el GameManager.", { x: 0.9, y: 5.2, w: 11.5, h: 1.2, fontSize: 17, color: C.text1, valign: "middle", isTextBox: true });
  s.addNotes("El flujo completo: primero un entrenamiento guiado, luego un turno con tiempo, inspección, clasificación y un cierre con retroalimentación. Todo está modelado como una máquina de estados.");

  // 4 · Onboarding ----------------------------------------------------------
  s = content("Onboarding VR: aprender haciendo", "Experiencia");
  screenshot(s, "05_onboarding", 0.6, 1.5, 6.6, "Mesa y tablero de entrenamiento");
  caption(s, "Zona de entrenamiento con caja, lupa y soporte de práctica", 0.6, 5.3, 6.6);
  const ob = [
    ["Moverse", "Joystick izquierdo para caminar, derecho para girar"],
    ["Agarrar", "Botón lateral (grip) sobre la caja de práctica"],
    ["Inspeccionar", "Tocar la caja con una herramienta"],
    ["Colocar", "Soltar la caja en el soporte verde (socket)"],
  ];
  ob.forEach(([t, d], i) => {
    const y = 1.5 + i * 0.95;
    s.addShape(pres.ShapeType.ellipse, { x: 7.7, y, w: 0.6, h: 0.6, fill: { color: C.accent1 }, line: { type: "none" }, objectName: "StepDot" });
    s.addText(String(i + 1), { x: 7.7, y, w: 0.6, h: 0.6, fontSize: 18, bold: true, color: C.text1, align: "center", valign: "middle", margin: 0, isTextBox: true });
    s.addText([{ text: t, options: { bold: true, breakLine: true } }, { text: d, options: { color: HEX.dk2 } }],
      { x: 8.5, y: y - 0.1, w: 4.3, h: 0.85, fontSize: 15, color: C.text1, valign: "middle", isTextBox: true });
  });
  s.addText("Cada paso se valida con eventos XR reales; al terminar aparece INICIAR TURNO.", { x: 7.7, y: 5.4, w: 5.1, h: 0.7, fontSize: 14, italic: true, color: C.text2, isTextBox: true });
  s.addNotes("El onboarding enseña movimiento, agarre, uso de herramienta y colocación en un socket. Ningún paso avanza con un clic: se detecta el desplazamiento del rig, el evento selectEntered del objeto y el socket.");

  // 5 · Interacciones -------------------------------------------------------
  s = content("Interacciones XR implementadas", "Experiencia");
  const inter = [
    [I.hand, "Grab Interactable", "Productos y herramientas con agarre dinámico y lanzamiento físico"],
    [I.plug, "Socket", "Soporte de práctica que encaja la caja (XRSocketInteractor)"],
    [I.pointer, "UI XR", "Botones con rayo: iniciar, saltar, reiniciar y salir"],
    [I.wave, "Háptica", "Vibración fuerte si encuentras un defecto, suave si no"],
    [I.volume, "Audio espacial", "Sonido 3D por herramienta, contenedor y alarma"],
    [I.vr, "Locomoción", "Movimiento continuo, giro y teletransporte al puesto"],
  ];
  inter.forEach(([ic, t, d], i) => {
    const col = i % 2, row = Math.floor(i / 2);
    const x = 0.6 + col * 3.35, y = 1.5 + row * 1.7;
    iconTile(s, ic, x, y, 0.85);
    s.addText(t, { x: x + 1.0, y, w: 2.2, h: 0.4, fontSize: 16, bold: true, color: C.text1, margin: 0, isTextBox: true });
    s.addText(d, { x: x + 1.0, y: y + 0.42, w: 2.2, h: 1.1, fontSize: 12, color: C.text2, margin: 0, valign: "top", isTextBox: true });
  });
  screenshot(s, "03_herramientas", 7.5, 1.5, 5.2, "Mesa de herramientas");
  caption(s, "Lupa, destornillador y martillo listos para agarrar", 7.5, 4.55, 5.2);
  s.addNotes("Usamos los componentes del XR Interaction Toolkit 3: Grab Interactable, Socket Interactor, UI con TrackedDeviceGraphicRaycaster, háptica en el controlador y audio espacial.");

  // 6 · Mecánica de inspección ----------------------------------------------
  s = content("Mecánica de inspección", "Experiencia");
  const tools = [
    [I.search, "Lupa", "Grietas", HEX.accent4],
    [I.screw, "Destornillador", "Tornillos flojos", HEX.accent1],
    [I.hammer, "Martillo", "Piezas huecas", HEX.accent6],
  ];
  tools.forEach(([ic, t, d, c], i) => {
    const y = 1.55 + i * 1.25;
    iconTile(s, ic, 0.6, y, 0.95, c);
    s.addText(t, { x: 1.75, y, w: 2.6, h: 0.5, fontSize: 20, bold: true, color: C.text1, margin: 0, isTextBox: true });
    s.addText("revela: " + d, { x: 1.75, y: y + 0.5, w: 2.8, h: 0.4, fontSize: 15, color: C.text2, margin: 0, isTextBox: true });
  });
  s.addImage({ data: I.x, x: 0.65, y: 5.4, w: 0.4, h: 0.4, objectName: "IconBad" });
  s.addText("Indicador rojo: defecto encontrado", { x: 1.15, y: 5.38, w: 3.6, h: 0.45, fontSize: 14, color: C.text1, margin: 0, isTextBox: true });
  s.addImage({ data: I.check, x: 0.65, y: 5.9, w: 0.4, h: 0.4, objectName: "IconOk" });
  s.addText("Indicador verde: revisado con las 3 y en buen estado", { x: 1.15, y: 5.88, w: 3.8, h: 0.45, fontSize: 14, color: C.text1, margin: 0, isTextBox: true });
  screenshot(s, "06_banda_detalle", 5.3, 1.5, 7.4, "Productos sobre la banda con indicadores");
  caption(s, "Productos en la banda: uno con defecto revelado (rojo) y uno aprobado (verde)", 5.3, 5.75, 7.4);
  s.addNotes("Cada producto puede tener uno de tres defectos y solo la herramienta correcta lo revela. Esto obliga a usar las tres herramientas antes de decidir, que es justo lo que hace un inspector real.");

  // 7 · Eventos dinámicos ---------------------------------------------------
  pres.addSection({ title: "Sistemas" });
  s = content("Eventos dinámicos y sistema reactivo", "Sistemas");
  const stats = [
    ["3:00", "temporizador del turno", I.clock],
    ["5", "fallos máximos antes de perder", I.warn],
    ["×2.2", "aceleración inesperada de la banda", I.bolt],
    ["+8 s", "avería: se detiene y compensa tiempo", I.gauge],
  ];
  stats.forEach(([n, l, ic], i) => {
    const x = 0.6 + i * 3.05;
    s.addShape(pres.ShapeType.roundRect, { x, y: 1.6, w: 2.8, h: 2.7, rectRadius: 0.12, fill: { color: C.background2 }, line: { type: "none" }, objectName: "StatCard" });
    iconTile(s, ic, x + 0.25, 1.85, 0.6);
    s.addText(n, { x: x + 0.2, y: 2.55, w: 2.4, h: 1.0, fontSize: 54, bold: true, color: C.text1, margin: 0, isTextBox: true });
    s.addText(l, { x: x + 0.2, y: 3.55, w: 2.4, h: 0.7, fontSize: 14, color: C.text2, margin: 0, valign: "top", isTextBox: true });
  });
  s.addText([
    { text: "Producto que llega al final sin clasificar → cuenta como fallo.", options: { bullet: true, breakLine: true } },
    { text: "A 30 s del final: alerta visual y sonora; el reloj cambia a amarillo.", options: { bullet: true, breakLine: true } },
    { text: "Los eventos se publican por C# events: HUD, audio y banda reaccionan sin acoplarse.", options: { bullet: true } },
  ], { x: 0.6, y: 4.65, w: 12.1, h: 1.9, fontSize: 16, color: C.text1, paraSpaceAfter: 6, valign: "top", isTextBox: true });
  s.addNotes("El enunciado exige temporizador, fallo, evento inesperado y sistema reactivo. Cada 45 segundos ocurre aleatoriamente una aceleración o una avería. Todo se comunica con eventos.");

  // 8 · Puntuación y cierre -------------------------------------------------
  s = content("Puntuación, progreso y cierre", "Sistemas");
  screenshot(s, "02_puesto_de_trabajo", 0.6, 1.5, 7.0, "Vista del operario frente a la banda y el tablero");
  caption(s, "Vista del operario: tablero con tiempo, puntos, fallos, alertas y último reporte", 0.6, 5.55, 7.0);
  const sc = [
    ["+100", "por cada clasificación correcta", HEX.accent2],
    ["−50", "por error o producto perdido", HEX.accent3],
  ];
  sc.forEach(([n, l, c], i) => {
    const y = 1.5 + i * 1.35;
    s.addText(n, { x: 8.0, y, w: 1.9, h: 1.0, fontSize: 44, bold: true, color: c, margin: 0, isTextBox: true });
    s.addText(l, { x: 9.9, y: y + 0.2, w: 2.9, h: 0.7, fontSize: 15, color: C.text1, margin: 0, isTextBox: true });
  });
  s.addText([
    { text: "Panel final: turno completado o fallido", options: { bullet: true, breakLine: true } },
    { text: "Aciertos, fallos y últimos reportes de daño", options: { bullet: true, breakLine: true } },
    { text: "Retroalimentación según el desempeño", options: { bullet: true, breakLine: true } },
    { text: "Récord histórico y aviso de nuevo récord", options: { bullet: true } },
  ], { x: 8.0, y: 4.3, w: 4.8, h: 2.2, fontSize: 15, color: C.text1, paraSpaceAfter: 6, valign: "top", isTextBox: true });
  s.addNotes("La puntuación es verificable: cada acierto suma 100 y cada error resta 50. Al final se muestra un resumen con retroalimentación y el récord guardado.");

  // 9 · Arquitectura --------------------------------------------------------
  pres.addSection({ title: "Implementación" });
  s = content("Arquitectura de scripts", "Implementación");
  const box = (t, d, x, y, w, fill, txt) => {
    s.addShape(pres.ShapeType.roundRect, { x, y, w, h: 0.95, rectRadius: 0.1, fill: { color: fill }, line: { type: "none" }, objectName: "ArchBox" });
    s.addText([{ text: t, options: { bold: true, breakLine: true } }, { text: d, options: { fontSize: 11 } }],
      { x, y, w, h: 0.95, fontSize: 15, color: txt, align: "center", valign: "middle", isTextBox: true });
  };
  box("GameManager", "estado · tiempo · puntos · eventos", 4.9, 3.1, 3.5, C.text1, C.background1);
  const around = [
    ["ConveyorBelt", "banda física · spawn · eventos", 0.6, 1.5], ["InspectionTool", "lupa · destornillador · martillo", 4.9, 1.5],
    ["Product", "tipo · defecto · indicadores", 9.2, 1.5], ["ProductBin", "desechar / aprobar", 0.6, 4.7],
    ["HudController", "tablero y panel final", 4.9, 4.7], ["ScoreRepository", "historial JSON", 9.2, 4.7],
  ];
  around.forEach(([t, d, x, y]) => {
    box(t, d, x, y, 3.5, C.background2, C.text1);
    s.addShape(pres.ShapeType.line, {
      x: Math.min(x + 1.75, 6.65), y: y < 3 ? y + 0.95 : 4.05, w: Math.abs(x + 1.75 - 6.65), h: y < 3 ? 3.1 - (y + 0.95) : y - 4.05,
      flipH: x + 1.75 > 6.65, line: { color: HEX.accent5, width: 1.5 }, objectName: "ArchLink",
    });
  });
  s.addText("Namespace QualityControlXR · assembly definition propia · OnboardingTutorial, LostProductZone, ReturnHome, Spin", { x: 0.6, y: 6.0, w: 12.1, h: 0.5, fontSize: 13, italic: true, color: C.text2, isTextBox: true });
  s.addNotes("El GameManager es el centro: expone eventos de estado, puntaje, fallos y reportes. Los demás componentes publican resultados o escuchan cambios, sin referencias cruzadas entre ellos.");

  // 10 · Manejo de datos ----------------------------------------------------
  s = content("Manejo de datos: historial en JSON", "Implementación");
  iconTile(s, I.db, 0.6, 1.55, 0.9);
  s.addText([
    { text: "Cada turno se guarda al terminar en ", options: {} },
    { text: "persistentDataPath/qc_history.json", options: { bold: true } },
  ], { x: 1.7, y: 1.5, w: 4.6, h: 1.0, fontSize: 16, color: C.text1, valign: "middle", isTextBox: true });
  s.addText([
    { text: "SessionRecord: fecha, puntaje, aciertos, fallos, tiempo jugado y reportes de daño", options: { bullet: true, breakLine: true } },
    { text: "Se conservan las últimas 50 sesiones", options: { bullet: true, breakLine: true } },
    { text: "El récord histórico se calcula al cargar", options: { bullet: true, breakLine: true } },
    { text: "Lectura y escritura protegidas con try/catch", options: { bullet: true } },
  ], { x: 0.6, y: 2.8, w: 5.7, h: 3.0, fontSize: 15, color: C.text1, paraSpaceAfter: 8, valign: "top", isTextBox: true });
  s.addShape(pres.ShapeType.roundRect, { x: 6.7, y: 1.5, w: 6.0, h: 4.2, rectRadius: 0.1, fill: { color: C.text1 }, line: { type: "none" }, objectName: "CodeCard" });
  s.addText(
    '{\n  "sessions": [\n    {\n      "date": "2026-11-07 10:42",\n      "score": 650,\n      "correct": 8,\n      "mistakes": 3,\n      "secondsPlayed": 180.0,\n      "completed": true,\n      "reports": [\n        "[OK] Reporte: Radio desechado - grieta",\n        "[X] Error: Lámpara tenía tornillo flojo"\n      ]\n    }\n  ]\n}',
    { x: 6.95, y: 1.65, w: 5.6, h: 3.9, fontFace: "Courier New", fontSize: 12, color: C.accent1, valign: "top", isTextBox: true });
  s.addNotes("Para el criterio de manejo de datos: los resultados se serializan con JsonUtility y quedan en el almacenamiento persistente del visor. Así el récord se mantiene entre sesiones.");

  // 11 · Organización Unity -------------------------------------------------
  s = content("Organización del proyecto Unity", "Implementación");
  s.addShape(pres.ShapeType.roundRect, { x: 0.6, y: 1.5, w: 5.4, h: 4.9, rectRadius: 0.1, fill: { color: C.background2 }, line: { type: "none" }, objectName: "TreeCard" });
  s.addText(
    "Assets/\n├ Scripts/     lógica en 5 dominios\n├ Editor/      generador y setup\n├ Prefabs/     5 productos\n├ Materials/\n├ Scenes/      QualityControl\n├ Tests/       pruebas PlayMode\n├ ThirdParty/  Kenney (CC0)\n└ Samples/     XRI Starter Assets",
    { x: 0.85, y: 1.75, w: 5.0, h: 4.4, fontFace: "Courier New", fontSize: 16, color: C.text1, valign: "top", paraSpaceAfter: 4, isTextBox: true });
  const org = [
    ["Escena reproducible", "Tools › Quality Control XR › Build Scene regenera toda la escena desde código"],
    ["Assets libres (CC0)", "Kenney Factory Kit, Furniture Kit, Interface e Impact Sounds"],
    ["Control de versiones", "Repositorio público en GitHub con commits por funcionalidad"],
    ["Prueba sin visor", "XR Device Simulator activado solo en el Editor"],
  ];
  org.forEach(([t, d], i) => {
    const y = 1.5 + i * 1.25;
    s.addText([{ text: t, options: { bold: true, breakLine: true } }, { text: d, options: { color: HEX.dk2 } }],
      { x: 6.5, y, w: 6.2, h: 1.1, fontSize: 15, color: C.text1, valign: "top", isTextBox: true });
  });
  s.addNotes("La organización también se evalúa. Separamos scripts por dominio, tenemos herramientas de editor que generan la escena y configuran el proyecto, y todo está versionado en GitHub.");

  // 12 · Optimización -------------------------------------------------------
  s = content("Optimización para Meta Quest", "Implementación");
  const opt = [
    ["IL2CPP + ARM64", "Código nativo para el procesador del visor"],
    ["Vulkan + Single Pass Instanced", "Ambos ojos en una sola pasada de render"],
    ["ASTC + MSAA 4×", "Compresión de texturas móvil y bordes limpios"],
    ["Modelos low poly", "Assets Kenney optimizados, sin texturas pesadas"],
    ["1 luz con sombras", "Sombras de una cascada a 15 m; luces puntuales sin sombra"],
    ["Física simple", "Banda con un solo Rigidbody cinemático y colisionadores de caja"],
  ];
  opt.forEach(([t, d], i) => {
    const col = i % 3, row = Math.floor(i / 3);
    const x = 0.6 + col * 4.1, y = 1.6 + row * 2.4;
    s.addShape(pres.ShapeType.roundRect, { x, y, w: 3.8, h: 2.1, rectRadius: 0.1, fill: { color: C.background2 }, line: { type: "none" }, objectName: "OptCard" });
    iconTile(s, I.gauge, x + 0.25, y + 0.25, 0.55);
    s.addText(t, { x: x + 0.25, y: y + 0.9, w: 3.3, h: 0.5, fontSize: 17, bold: true, color: C.text1, margin: 0, isTextBox: true });
    s.addText(d, { x: x + 0.25, y: y + 1.4, w: 3.3, h: 0.6, fontSize: 13, color: C.text2, margin: 0, valign: "top", isTextBox: true });
  });
  s.addNotes("Configuración pensada para el hardware móvil del Quest: IL2CPP en ARM64, Vulkan con single pass instanced, texturas ASTC y una iluminación muy contenida.");

  // 13 · Rúbrica ------------------------------------------------------------
  pres.addSection({ title: "Cierre" });
  s = content("Cómo cubrimos la rúbrica", "Cierre");
  const hdr = (t) => ({ text: t, options: { bold: true, color: HEX.lt1, fill: { color: HEX.dk1 } } });
  const rows = [
    [hdr("Criterio"), hdr("Peso"), hdr("Evidencia en el proyecto")],
    ["Calidad de interfaz", "15%", "Tableros en World Space, alertas por color, panel final con reportes"],
    ["Implementación 3D", "15%", "Nave industrial, banda modular, 5 productos y 3 herramientas"],
    ["Interacciones", "25%", "Grab, socket, UI XR, háptica, locomoción"],
    ["Inmersión", "25%", "Audio espacial, onboarding diegético, eventos de la planta"],
    ["Montaje general", "20%", "Flujo completo de onboarding a cierre con un solo GameManager"],
    ["Organización Unity", "15%", "Carpetas por dominio, asmdef, escena reproducible"],
    ["Scripts", "15%", "Eventos C#, componentes desacoplados, 7 pruebas PlayMode automáticas"],
    ["Manejo de datos", "20%", "Historial JSON, récord y reportes persistentes"],
  ];
  s.addTable(rows, {
    x: 0.6, y: 1.45, w: 12.1, colW: [2.9, 1.0, 8.2], fontSize: 14, color: HEX.dk1, fontFace: THEME.bodyFontFace,
    border: { type: "solid", pt: 0.5, color: "D5D8DE" }, rowH: 0.52, valign: "middle", fill: { color: HEX.lt1 },
  });
  s.addNotes("Esta tabla resume cada criterio de la rúbrica con la evidencia concreta que mostraremos en la demo.");

  // 14 · Demo / Preguntas ---------------------------------------------------
  s = pres.addSlide({ masterName: "DARK_TITLE", sectionTitle: "Cierre" });
  if (fs.existsSync(shot("02_puesto_de_trabajo")))
    s.addImage({ path: shot("02_puesto_de_trabajo"), x: 0, y: 0, w: 13.333, h: 7.5, transparency: 75, altText: "Puesto de trabajo del operario", objectName: "ClosingImage" });
  s.addText("Demo en vivo y preguntas", { placeholder: "title" });
  s.addText("github.com/Juanortizd17/Proyecto_Final_VR_2026", { placeholder: "body" });
  s.addImage({ data: I.github, x: 0.8, y: 5.4, w: 0.55, h: 0.55, objectName: "GithubIcon" });
  s.addText("Repositorio público con el proyecto Unity, scripts y esta presentación", { x: 1.5, y: 5.4, w: 9, h: 0.55, fontSize: 16, color: C.background1, valign: "middle", isTextBox: true });
  s.addNotes("Pasamos a la demo en el visor: onboarding, un turno con un evento inesperado y el panel final. Luego, preguntas.");

  await pres.writeFile({ fileName: OUT });
  if (SKILL) {
    const { applyTheme } = require(path.join(SKILL, "scripts", "apply_theme.js"));
    await applyTheme(OUT, THEME);
  }
  console.log("OK " + OUT);
})().catch((e) => { console.error(e); process.exit(1); });
