import fs from "node:fs/promises";
import path from "node:path";
import { Presentation, PresentationFile } from "@oai/artifact-tool";

const root = path.resolve("C:/Users/carl0/source/repos/ProductionCapacity");
const outDir = path.join(root, "artifacts", "sales-deck", "output");
const previewDir = path.join(root, "artifacts", "sales-deck", "preview");
const shots = path.join(root, "artifacts", "sales-deck", "screenshots-desktop");
const outputPath = path.join(outDir, "CapacityPlanner_SMT_presentacion_usuarios.pptx");

await fs.mkdir(outDir, { recursive: true });
await fs.mkdir(previewDir, { recursive: true });

const deck = Presentation.create({
  slideSize: { width: 1280, height: 720 },
});
const createdSlides = [];

function addSlide() {
  const slide = deck.slides.add();
  createdSlides.push(slide);
  return slide;
}

const C = {
  ink: "#102A25",
  green: "#173F36",
  green2: "#0F2E28",
  mint: "#EAF3EF",
  paper: "#F5F7F5",
  white: "#FFFFFF",
  slate: "#52615B",
  line: "#D8E1DC",
  yellow: "#FFF0B8",
  amber: "#F4B740",
  red: "#D95F59",
  blue: "#2772C2",
};

function addText(slide, text, box, style = {}) {
  const shape = slide.shapes.add({
    geometry: "textbox",
    position: box,
    fill: "none",
    line: { style: "solid", fill: "none", width: 0 },
    name: style.name ?? undefined,
  });
  shape.text = text;
  shape.text.style = {
    fontSize: style.fontSize ?? 24,
    bold: style.bold ?? false,
    color: style.color ?? C.ink,
    alignment: style.alignment ?? "left",
    ...style.textStyle,
  };
  return shape;
}

function addRect(slide, box, fill, line = "none", radius = "rounded-lg") {
  return slide.shapes.add({
    geometry: "roundRect",
    position: box,
    fill,
    line: { style: "solid", fill: line, width: line === "none" ? 0 : 1 },
    borderRadius: radius,
  });
}

async function addImage(slide, file, box, alt, fit = "cover", crop) {
  const bytes = await fs.readFile(path.join(shots, file));
  slide.images.add({
    blob: bytes,
    contentType: "image/png",
    alt,
    fit,
    position: box,
    geometry: "roundRect",
    borderRadius: "rounded-xl",
    ...(crop ? { crop } : {}),
  });
}

function addHeader(slide, title, kicker = "Capacity Planner SMT") {
  addText(slide, kicker.toUpperCase(), { left: 72, top: 48, width: 480, height: 28 }, {
    fontSize: 13,
    bold: true,
    color: C.green,
  });
  addText(slide, title, { left: 72, top: 78, width: 720, height: 68 }, {
    fontSize: 34,
    bold: true,
    color: C.ink,
  });
}

function addFooter(slide, page) {
  addText(slide, `Capacity Planner SMT · ${page}`, { left: 72, top: 674, width: 400, height: 22 }, {
    fontSize: 12,
    color: C.slate,
  });
}

function addBullet(slide, text, top, emphasis = "") {
  addRect(slide, { left: 86, top: top + 9, width: 8, height: 8 }, emphasis || C.green, "none", "rounded-sm");
  addText(slide, text, { left: 108, top, width: 440, height: 44 }, {
    fontSize: 20,
    color: C.ink,
  });
}

function addStat(slide, label, value, left, top, accent = C.green) {
  addRect(slide, { left, top, width: 178, height: 108 }, C.white, C.line, "rounded-xl");
  addRect(slide, { left, top, width: 7, height: 108 }, accent, "none", "rounded-sm");
  addText(slide, label, { left: left + 20, top: top + 18, width: 140, height: 24 }, {
    fontSize: 13,
    color: C.slate,
  });
  addText(slide, value, { left: left + 20, top: top + 48, width: 140, height: 38 }, {
    fontSize: 28,
    bold: true,
    color: C.ink,
  });
}

function addProcessStep(slide, n, title, body, left) {
  addRect(slide, { left, top: 250, width: 196, height: 210 }, C.white, C.line, "rounded-xl");
  addRect(slide, { left: left + 18, top: 272, width: 42, height: 42 }, C.green, "none", "rounded-xl");
  addText(slide, String(n), { left: left + 18, top: 280, width: 42, height: 24 }, {
    fontSize: 20,
    bold: true,
    color: C.white,
    alignment: "center",
  });
  addText(slide, title, { left: left + 18, top: 330, width: 160, height: 48 }, {
    fontSize: 20,
    bold: true,
    color: C.ink,
  });
  addText(slide, body, { left: left + 18, top: 392, width: 160, height: 52 }, {
    fontSize: 14,
    color: C.slate,
  });
}

{
  const slide = addSlide();
  slide.background.fill = C.green2;
  await addImage(slide, "02-capacidad-smt-http.png", { left: 600, top: 0, width: 680, height: 720 }, "Pantalla de Capacidad SMT", "cover", { left: 0.05, top: 0.02, right: 0.06, bottom: 0.02 });
  addRect(slide, { left: 0, top: 0, width: 690, height: 720 }, C.green2, "none", "rounded-none");
  addText(slide, "Capacity Planner SMT", { left: 76, top: 96, width: 500, height: 120 }, {
    fontSize: 58,
    bold: true,
    color: C.white,
  });
  addText(slide, "Planeacion de produccion con demanda, capacidad y alertas en una sola vista.", { left: 80, top: 246, width: 500, height: 90 }, {
    fontSize: 24,
    color: "#DCEBE6",
  });
  addRect(slide, { left: 80, top: 395, width: 420, height: 96 }, "#214D43", "none", "rounded-xl");
  addText(slide, "Para planeadores, lideres de produccion y equipos que necesitan decidir rapido donde producir y donde ajustar.", { left: 106, top: 416, width: 368, height: 56 }, {
    fontSize: 18,
    color: C.white,
  });
  addText(slide, "Presentacion funcional · SMT", { left: 80, top: 638, width: 420, height: 28 }, {
    fontSize: 14,
    color: "#B9D4CB",
  });
}

{
  const slide = addSlide();
  slide.background.fill = C.paper;
  addHeader(slide, "El reto que resuelve");
  addText(slide, "La planeacion SMT suele vivir entre archivos de Excel, formulas, cambios de demanda y decisiones urgentes sobre lineas.", { left: 72, top: 156, width: 580, height: 72 }, {
    fontSize: 22,
    color: C.slate,
  });
  addBullet(slide, "Saber si una linea puede cumplir antes de comprometer produccion.", 268);
  addBullet(slide, "Detectar sobrecarga por mes, familia y linea sin revisar celda por celda.", 330, C.amber);
  addBullet(slide, "Mover familias o ajustar factores cuando cambia la prioridad del cliente.", 392, C.blue);
  addBullet(slide, "Importar informacion sin perder trazabilidad ni depender de formulas ocultas.", 454, C.red);
  addRect(slide, { left: 710, top: 150, width: 430, height: 390 }, C.white, C.line, "rounded-2xl");
  addText(slide, "Antes", { left: 750, top: 194, width: 120, height: 34 }, { fontSize: 24, bold: true });
  addText(slide, "Archivos separados\nRevision manual\nRiesgo de error en formulas\nVisibilidad limitada", { left: 750, top: 246, width: 250, height: 154 }, { fontSize: 23, color: C.slate });
  addText(slide, "Ahora", { left: 750, top: 430, width: 120, height: 34 }, { fontSize: 24, bold: true, color: C.green });
  addText(slide, "Un flujo web, validado y accionable.", { left: 750, top: 482, width: 300, height: 34 }, { fontSize: 22, color: C.ink });
  addFooter(slide, "01");
}

{
  const slide = addSlide();
  slide.background.fill = C.paper;
  addHeader(slide, "Lo que hace el sistema");
  addText(slide, "Convierte volumen, UPH, distribucion y calendarios en vistas operativas para decidir donde producir.", { left: 72, top: 150, width: 680, height: 60 }, { fontSize: 22, color: C.slate });
  addStat(slide, "Demanda asignada", "99,272", 82, 260, C.green);
  addStat(slide, "Lineas SMT", "5", 292, 260, C.blue);
  addStat(slide, "Familias", "53", 502, 260, C.green);
  addStat(slide, "Revision", "2 lineas", 712, 260, C.amber);
  addStat(slide, "Sobrecarga", "2 lineas", 922, 260, C.red);
  addText(slide, "El sistema no reemplaza al planeador: le da una base consistente para revisar escenarios, validar datos y ajustar capacidad con criterio.", { left: 150, top: 450, width: 980, height: 72 }, { fontSize: 26, bold: true, color: C.ink, alignment: "center" });
  addFooter(slide, "02");
}

{
  const slide = addSlide();
  slide.background.fill = C.paper;
  addHeader(slide, "Summary SMT: vista ejecutiva");
  addText(slide, "La primera pantalla resume el trimestre: MPS total, lineas cargadas, cuello de botella y distribucion por BU.", { left: 72, top: 154, width: 420, height: 88 }, { fontSize: 21, color: C.slate });
  addBullet(slide, "Detecta rapidamente donde esta el cuello de botella.", 286);
  addBullet(slide, "Muestra linealidad y proyeccion por BU.", 348, C.blue);
  addBullet(slide, "Da contexto antes de entrar al detalle por linea.", 410, C.amber);
  await addImage(slide, "01-summary-smt.png", { left: 560, top: 80, width: 640, height: 520 }, "Pantalla Summary SMT", "contain");
  addFooter(slide, "03");
}

{
  const slide = addSlide();
  slide.background.fill = C.paper;
  addHeader(slide, "Capacidad SMT: decision por linea");
  addText(slide, "Aqui el planeador ve lineas, familias, distribucion, UPH, changeover, MPS, capacidad y delta por mes.", { left: 72, top: 154, width: 405, height: 95 }, { fontSize: 21, color: C.slate });
  addBullet(slide, "Verde: hasta 85.4% de utilizacion.", 298, C.green);
  addBullet(slide, "Amarillo: de 85.5% a 99.4%.", 356, C.amber);
  addBullet(slide, "Rojo: arriba de 99.4%, requiere accion.", 414, C.red);
  addBullet(slide, "Boton Editar para mover familia o ajustar factor.", 472, C.blue);
  await addImage(slide, "02-capacidad-smt-http.png", { left: 520, top: 80, width: 700, height: 520 }, "Pantalla Capacidad SMT", "cover", { left: 0.02, top: 0.02, right: 0.20, bottom: 0.02 });
  addFooter(slide, "04");
}

{
  const slide = addSlide();
  slide.background.fill = C.paper;
  addHeader(slide, "Administracion SMT: datos controlados");
  addText(slide, "Los catalogos ayudan a que el usuario no capture variantes de nombre ni valores inconsistentes.", { left: 72, top: 154, width: 420, height: 78 }, { fontSize: 21, color: C.slate });
  addBullet(slide, "Familias, subfamilias y numeros de parte.", 284);
  addBullet(slide, "UPH por familia y por linea.", 342, C.blue);
  addBullet(slide, "Distribucion por linea y calendario SMT.", 400, C.green);
  addBullet(slide, "Historial para revisar cambios importantes.", 458, C.amber);
  await addImage(slide, "03-administracion-smt.png", { left: 550, top: 86, width: 650, height: 510 }, "Pantalla Administracion SMT", "contain");
  addFooter(slide, "05");
}

{
  const slide = addSlide();
  slide.background.fill = C.paper;
  addHeader(slide, "Importacion desde Excel");
  addText(slide, "El sistema esta preparado para recibir plantillas sencillas y archivos FMD SMT. El usuario ve mensajes de estado, errores y validaciones antes de confiar en los datos.", { left: 72, top: 154, width: 440, height: 112 }, { fontSize: 20, color: C.slate });
  addBullet(slide, "Descarga plantillas oficiales.", 312);
  addBullet(slide, "Carga volumen, UPH y distribucion.", 370, C.blue);
  addBullet(slide, "Confirma cuando termina correctamente.", 428, C.green);
  addBullet(slide, "Marca datos faltantes o inconsistentes.", 486, C.red);
  await addImage(slide, "04-importar-excel.png", { left: 550, top: 82, width: 650, height: 520 }, "Pantalla Importar Excel", "contain");
  addFooter(slide, "06");
}

{
  const slide = addSlide();
  slide.background.fill = C.paper;
  addHeader(slide, "Diagnostico antes de planear");
  addText(slide, "Antes de tomar decisiones, el sistema muestra si hay volumen, UPH, distribucion, calendario o targets pendientes.", { left: 72, top: 154, width: 435, height: 84 }, { fontSize: 21, color: C.slate });
  addBullet(slide, "Prioriza hallazgos criticos.", 290, C.red);
  addBullet(slide, "Explica la accion recomendada.", 348, C.blue);
  addBullet(slide, "Reduce dudas al importar nuevos datos.", 406, C.green);
  addBullet(slide, "Hace visible lo que antes quedaba oculto en Excel.", 464, C.amber);
  await addImage(slide, "05-diagnostico-smt.png", { left: 550, top: 82, width: 650, height: 520 }, "Pantalla Diagnostico SMT", "contain");
  addFooter(slide, "07");
}

{
  const slide = addSlide();
  slide.background.fill = C.paper;
  addHeader(slide, "Flujo de trabajo del planeador");
  addText(slide, "Un proceso simple para operar el sistema semana a semana o cada vez que cambie el plan.", { left: 72, top: 152, width: 760, height: 46 }, { fontSize: 22, color: C.slate });
  addProcessStep(slide, 1, "Importar", "Carga FMD SMT o plantillas CSV oficiales.", 72);
  addProcessStep(slide, 2, "Validar", "Revisa diagnostico y corrige datos faltantes.", 300);
  addProcessStep(slide, 3, "Analizar", "Consulta Summary y Capacidad SMT.", 528);
  addProcessStep(slide, 4, "Ajustar", "Mueve familias o modifica factores.", 756);
  addProcessStep(slide, 5, "Decidir", "Identifica lineas sanas, en revision y sobrecargadas.", 984);
  addFooter(slide, "08");
}

{
  const slide = addSlide();
  slide.background.fill = C.green2;
  addText(slide, "Valor para la operacion", { left: 72, top: 70, width: 690, height: 68 }, { fontSize: 44, bold: true, color: C.white });
  addText(slide, "Menos tiempo buscando datos. Mas tiempo tomando decisiones.", { left: 76, top: 148, width: 650, height: 42 }, { fontSize: 23, color: "#DCEBE6" });
  const items = [
    ["Visibilidad", "Resumen ejecutivo y detalle por linea en la misma herramienta."],
    ["Control", "Catalogos y plantillas para evitar errores de captura."],
    ["Velocidad", "Importacion y diagnostico para responder a cambios de demanda."],
    ["Accion", "Semaforos, deltas y edicion para balancear produccion."]
  ];
  let top = 250;
  for (const [title, body] of items) {
    addRect(slide, { left: 88, top, width: 500, height: 72 }, "#214D43", "none", "rounded-xl");
    addText(slide, title, { left: 116, top: top + 14, width: 150, height: 28 }, { fontSize: 20, bold: true, color: C.white });
    addText(slide, body, { left: 260, top: top + 15, width: 300, height: 38 }, { fontSize: 16, color: "#DCEBE6" });
    top += 92;
  }
  await addImage(slide, "01-summary-smt.png", { left: 690, top: 110, width: 500, height: 440 }, "Summary SMT", "cover", { left: 0.03, top: 0.02, right: 0.03, bottom: 0.02 });
  addText(slide, "Un sistema listo para operar SMT hoy y crecer hacia mas procesos cuando el negocio lo pida.", { left: 690, top: 585, width: 500, height: 54 }, { fontSize: 20, bold: true, color: C.white, alignment: "center" });
}

{
  const slide = deck.slides.add();
  slide.background.fill = C.paper;
  addHeader(slide, "Que puede mostrar en una demo");
  addText(slide, "Guion sugerido para explicar el sistema en 7 minutos.", { left: 72, top: 152, width: 640, height: 42 }, { fontSize: 22, color: C.slate });
  const script = [
    ["1", "Empezar en Summary SMT", "Mostrar demanda total, cuello de botella y resumen por BU."],
    ["2", "Entrar a Capacidad SMT", "Explicar semaforos, MPS, capacidad, delta y boton Editar."],
    ["3", "Abrir Administracion SMT", "Mostrar catalogos, UPH, distribucion y calendarios."],
    ["4", "Cerrar con Importar y Diagnostico", "Probar que el flujo tiene validaciones y mensajes claros."]
  ];
  let y = 230;
  for (const [n, title, body] of script) {
    addRect(slide, { left: 90, top: y, width: 72, height: 72 }, C.green, "none", "rounded-xl");
    addText(slide, n, { left: 90, top: y + 18, width: 72, height: 32 }, { fontSize: 28, bold: true, color: C.white, alignment: "center" });
    addText(slide, title, { left: 190, top: y + 4, width: 360, height: 30 }, { fontSize: 22, bold: true, color: C.ink });
    addText(slide, body, { left: 190, top: y + 38, width: 730, height: 30 }, { fontSize: 17, color: C.slate });
    y += 96;
  }
  addRect(slide, { left: 935, top: 236, width: 230, height: 250 }, C.white, C.line, "rounded-2xl");
  addText(slide, "Mensaje clave", { left: 965, top: 270, width: 170, height: 30 }, { fontSize: 22, bold: true, color: C.green });
  addText(slide, "La herramienta traduce Excel en decisiones operativas claras: importar, validar, balancear y actuar.", { left: 965, top: 326, width: 170, height: 116 }, { fontSize: 18, color: C.ink });
  addFooter(slide, "09");
}

const pptx = await PresentationFile.exportPptx(deck);
await pptx.save(outputPath);

const montage = await deck.export({ format: "webp", montage: true, scale: 0.8 });
await fs.writeFile(path.join(previewDir, "capacity-planner-smt-montage.webp"), Buffer.from(await montage.arrayBuffer()));

for (let i = 0; i < createdSlides.length; i += 1) {
  const png = await deck.export({ slide: createdSlides[i], format: "png", scale: 1 });
  await fs.writeFile(path.join(previewDir, `slide-${String(i + 1).padStart(2, "0")}.png`), Buffer.from(await png.arrayBuffer()));
}

console.log(outputPath);
