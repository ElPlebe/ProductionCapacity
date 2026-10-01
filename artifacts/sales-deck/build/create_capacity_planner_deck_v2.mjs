import fs from "node:fs/promises";
import path from "node:path";
import { pathToFileURL } from "node:url";
import { Presentation, PresentationFile } from "@oai/artifact-tool";

const root = path.resolve("C:/Users/carl0/source/repos/ProductionCapacity");
const skillDir = "C:/Users/carl0/.codex/plugins/cache/openai-primary-runtime/presentations/26.904.11930/skills/presentations";
const outDir = path.join(root, "artifacts", "sales-deck", "output");
const previewDir = path.join(root, "artifacts", "sales-deck", "preview-v2");
const shots = path.join(root, "artifacts", "sales-deck", "screenshots-desktop");
const outputPath = path.join(outDir, "CapacityPlanner_SMT_presentacion_usuarios_v2.pptx");

await fs.mkdir(outDir, { recursive: true });
await fs.mkdir(previewDir, { recursive: true });

const { resolvePresentationFont } = await import(
  pathToFileURL(path.join(skillDir, "container_tools/artifact_tool_utils.mjs")).href,
);
const fontFamily = resolvePresentationFont();

const deck = Presentation.create({ slideSize: { width: 1280, height: 720 } });
const slides = [];
const C = {
  ink: "#0D241F",
  green: "#143F36",
  green2: "#0B2D26",
  mint: "#EAF3EF",
  mist: "#F5F8F6",
  white: "#FFFFFF",
  slate: "#52615B",
  line: "#D9E3DE",
  yellow: "#FFF1B9",
  amber: "#E9A93B",
  red: "#D8615C",
  blue: "#2F70B7",
};

function slide() {
  const s = deck.slides.add();
  slides.push(s);
  return s;
}

function text(s, value, box, style = {}) {
  const shape = s.shapes.add({
    geometry: "textbox",
    position: box,
    fill: "none",
    line: { fill: "none", width: 0 },
    name: style.name,
  });
  shape.text = value;
  shape.text.style = {
    typeface: fontFamily,
    fontSize: style.size ?? 22,
    bold: style.bold ?? false,
    color: style.color ?? C.ink,
    alignment: style.align ?? "left",
    autoFit: "shrinkText",
  };
  return shape;
}

function rect(s, box, fill, line = "none", radius = "rounded-lg") {
  return s.shapes.add({
    geometry: "roundRect",
    position: box,
    fill,
    line: { style: "solid", fill: line, width: line === "none" ? 0 : 1 },
    borderRadius: radius,
  });
}

async function image(s, file, box, alt, fit = "cover", crop = undefined) {
  const bytes = await fs.readFile(path.join(shots, file));
  s.images.add({
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

function title(s, value, kicker = "Capacity Planner SMT") {
  text(s, kicker.toUpperCase(), { left: 72, top: 48, width: 520, height: 22 }, { size: 12, bold: true, color: C.green });
  text(s, value, { left: 72, top: 78, width: 820, height: 70 }, { size: 36, bold: true });
}

function foot(s, n) {
  text(s, `Capacity Planner SMT  ${String(n).padStart(2, "0")}`, { left: 72, top: 675, width: 260, height: 18 }, { size: 11, color: C.slate });
}

function sectionLabel(s, value, left, top, color = C.green) {
  rect(s, { left, top, width: 6, height: 42 }, color, "none", "rounded-sm");
  text(s, value, { left: left + 18, top: top - 1, width: 420, height: 48 }, { size: 25, bold: true });
}

function plainBullet(s, value, left, top, color = C.green) {
  rect(s, { left, top: top + 7, width: 8, height: 8 }, color, "none", "rounded-sm");
  text(s, value, { left: left + 22, top, width: 410, height: 36 }, { size: 18, color: C.ink });
}

function smallNote(s, value, box) {
  text(s, value, box, { size: 15, color: C.slate });
}

function metric(s, label, value, left, top, accent) {
  rect(s, { left, top, width: 190, height: 96 }, C.white, C.line, "rounded-xl");
  rect(s, { left, top, width: 7, height: 96 }, accent, "none", "rounded-sm");
  text(s, label, { left: left + 22, top: top + 18, width: 145, height: 20 }, { size: 13, color: C.slate });
  text(s, value, { left: left + 22, top: top + 44, width: 145, height: 34 }, { size: 27, bold: true });
}

{
  const s = slide();
  s.background.fill = C.green2;
  rect(s, { left: 760, top: 0, width: 520, height: 720 }, C.green, "none", "rounded-none");
  text(s, "Capacity Planner SMT", { left: 72, top: 94, width: 560, height: 126 }, { size: 58, bold: true, color: C.white });
  text(s, "Del Excel al plan de capacidad por linea", { left: 76, top: 238, width: 560, height: 42 }, { size: 26, color: "#DDECE7" });
  text(s, "Importa datos SMT, calcula capacidad, detecta sobrecarga y ayuda al planeador a decidir donde producir.", { left: 76, top: 328, width: 560, height: 96 }, { size: 25, color: C.white });
  rect(s, { left: 76, top: 505, width: 470, height: 72 }, "#215348", "none", "rounded-xl");
  text(s, "Audiencia: planeadores, lideres de produccion y equipos que revisan capacidad SMT.", { left: 102, top: 524, width: 420, height: 36 }, { size: 18, color: "#DDECE7" });
  text(s, "Presentacion funcional", { left: 880, top: 116, width: 240, height: 36 }, { size: 28, bold: true, color: C.white, align: "center" });
  text(s, "Que hace\nComo se usa\nDonde revisar\nCuando ajustar", { left: 858, top: 210, width: 300, height: 210 }, { size: 32, bold: true, color: "#DDECE7", align: "center" });
}

{
  const s = slide();
  s.background.fill = C.mist;
  title(s, "Que hace el sistema");
  text(s, "Carga el plan SMT y lo convierte en una vista de capacidad por linea, familia y mes.", { left: 78, top: 178, width: 720, height: 58 }, { size: 29, bold: true });
  metric(s, "Importa", "Excel", 86, 310, C.blue);
  metric(s, "Calcula", "Capacidad", 316, 310, C.green);
  metric(s, "Detecta", "Riesgo", 546, 310, C.amber);
  metric(s, "Permite", "Ajustar", 776, 310, C.red);
  text(s, "El usuario no necesita entender todas las formulas para empezar. Solo debe saber de donde vienen los datos y que significa cada resultado.", { left: 178, top: 500, width: 840, height: 52 }, { size: 24, color: C.slate, align: "center" });
  foot(s, 1);
}

{
  const s = slide();
  s.background.fill = C.mist;
  title(s, "Flujo de uso");
  const steps = [
    ["1", "Importar", "Carga FMD SMT o plantillas"],
    ["2", "Validar", "Revisa datos faltantes"],
    ["3", "Resumen", "Entiende el trimestre"],
    ["4", "Capacidad", "Ve lineas y familias"],
    ["5", "Ajustar", "Mueve familia o factor"],
  ];
  let left = 76;
  for (const [n, head, body] of steps) {
    rect(s, { left, top: 235, width: 185, height: 205 }, C.white, C.line, "rounded-xl");
    rect(s, { left: left + 22, top: 260, width: 45, height: 45 }, C.green, "none", "rounded-xl");
    text(s, n, { left: left + 22, top: 269, width: 45, height: 26 }, { size: 21, bold: true, color: C.white, align: "center" });
    text(s, head, { left: left + 22, top: 326, width: 140, height: 30 }, { size: 24, bold: true });
    text(s, body, { left: left + 22, top: 370, width: 135, height: 48 }, { size: 16, color: C.slate });
    left += 224;
  }
  text(s, "Lectura simple: primero se carga el plan, luego el sistema dice si los datos son confiables y donde se necesita actuar.", { left: 154, top: 520, width: 940, height: 52 }, { size: 24, color: C.slate, align: "center" });
  foot(s, 2);
}

{
  const s = slide();
  s.background.fill = C.mist;
  title(s, "Summary SMT");
  sectionLabel(s, "Para que sirve", 78, 190);
  smallNote(s, "Da una lectura ejecutiva del trimestre antes de entrar al detalle.", { left: 96, top: 250, width: 390, height: 54 });
  sectionLabel(s, "Que revisa el usuario", 78, 335, C.blue);
  plainBullet(s, "MPS total del periodo", 96, 392);
  plainBullet(s, "Lineas SMT cargadas", 96, 438, C.blue);
  plainBullet(s, "Cuello de botella", 96, 484, C.amber);
  plainBullet(s, "Distribucion mensual por BU", 96, 530, C.green);
  await image(s, "01-summary-smt.png", { left: 560, top: 94, width: 640, height: 505 }, "Pantalla Summary SMT", "contain");
  foot(s, 3);
}

{
  const s = slide();
  s.background.fill = C.mist;
  title(s, "Capacidad SMT");
  text(s, "La pantalla principal para decidir si la carga cabe en cada linea.", { left: 76, top: 152, width: 470, height: 54 }, { size: 24, bold: true });
  rect(s, { left: 78, top: 242, width: 410, height: 178 }, C.white, C.line, "rounded-xl");
  text(s, "Como leer la tabla", { left: 104, top: 264, width: 300, height: 28 }, { size: 23, bold: true });
  text(s, "MPS = volumen requerido\nCap = capacidad calculada\nDelta = capacidad menos volumen", { left: 104, top: 312, width: 330, height: 84 }, { size: 20, color: C.ink });
  rect(s, { left: 78, top: 452, width: 410, height: 122 }, C.white, C.line, "rounded-xl");
  text(s, "Semaforo de utilizacion", { left: 104, top: 472, width: 300, height: 24 }, { size: 21, bold: true });
  text(s, "Verde hasta 85.4%\nAmarillo de 85.5% a 99.4%\nRojo arriba de 99.4%", { left: 104, top: 508, width: 330, height: 56 }, { size: 17, color: C.slate });
  await image(s, "02-capacidad-smt-http.png", { left: 530, top: 88, width: 690, height: 510 }, "Pantalla Capacidad SMT", "cover", { left: 0.01, top: 0.02, right: 0.22, bottom: 0.02 });
  foot(s, 4);
}

{
  const s = slide();
  s.background.fill = C.mist;
  title(s, "Ajustes de planeacion");
  text(s, "Cuando cambia la demanda, el planeador puede probar alternativas sin rehacer el archivo completo.", { left: 78, top: 154, width: 760, height: 58 }, { size: 26, bold: true });
  sectionLabel(s, "Que puede ajustar", 94, 270);
  plainBullet(s, "Mover una familia a otra linea SMT", 112, 330);
  plainBullet(s, "Cambiar factor o distribucion de carga", 112, 382, C.blue);
  plainBullet(s, "Revisar el impacto en utilizacion y delta", 112, 434, C.amber);
  rect(s, { left: 710, top: 250, width: 380, height: 230 }, C.green, "none", "rounded-2xl");
  text(s, "Regla practica", { left: 750, top: 286, width: 280, height: 34 }, { size: 28, bold: true, color: C.white });
  text(s, "El objetivo es mantener las lineas en verde cuando sea posible. Si una linea queda amarilla, requiere revision. Si queda roja, hay sobrecarga.", { left: 750, top: 350, width: 290, height: 84 }, { size: 20, color: "#DDECE7" });
  foot(s, 5);
}

{
  const s = slide();
  s.background.fill = C.mist;
  title(s, "Administracion SMT");
  text(s, "Aqui viven los datos maestros que alimentan los calculos.", { left: 78, top: 154, width: 440, height: 54 }, { size: 25, bold: true });
  plainBullet(s, "Familias, subfamilias y numeros de parte", 92, 265);
  plainBullet(s, "UPH por familia y por linea", 92, 315, C.blue);
  plainBullet(s, "Distribucion por linea", 92, 365, C.green);
  plainBullet(s, "Calendario y targets SMT", 92, 415, C.amber);
  text(s, "Esta pantalla evita nombres duplicados y reduce errores de captura.", { left: 92, top: 510, width: 410, height: 50 }, { size: 20, color: C.slate });
  await image(s, "03-administracion-smt.png", { left: 560, top: 88, width: 640, height: 510 }, "Pantalla Administracion SMT", "contain");
  foot(s, 6);
}

{
  const s = slide();
  s.background.fill = C.mist;
  title(s, "Importacion de datos");
  text(s, "El sistema recibe el archivo FMD SMT o plantillas descargables.", { left: 78, top: 154, width: 480, height: 54 }, { size: 25, bold: true });
  sectionLabel(s, "Que debe hacer el usuario", 86, 260);
  plainBullet(s, "Descargar la plantilla correcta", 104, 320);
  plainBullet(s, "Llenar volumen, UPH y distribucion", 104, 370, C.blue);
  plainBullet(s, "Cargar el archivo y esperar confirmacion", 104, 420, C.green);
  plainBullet(s, "Revisar errores antes de seguir", 104, 470, C.red);
  await image(s, "04-importar-excel.png", { left: 565, top: 88, width: 635, height: 510 }, "Pantalla Importacion Excel", "contain");
  foot(s, 7);
}

{
  const s = slide();
  s.background.fill = C.mist;
  title(s, "Diagnostico SMT");
  text(s, "La pantalla de diagnostico responde una pregunta simple: los datos estan listos para planear?", { left: 78, top: 154, width: 470, height: 70 }, { size: 25, bold: true });
  plainBullet(s, "Critico: falta informacion para calcular bien", 92, 294, C.red);
  plainBullet(s, "Advertencia: conviene revisar antes de presentar", 92, 348, C.amber);
  plainBullet(s, "OK: el dato base esta cargado", 92, 402, C.green);
  text(s, "Cada hallazgo incluye una accion recomendada para que el usuario sepa que corregir.", { left: 92, top: 510, width: 430, height: 52 }, { size: 20, color: C.slate });
  await image(s, "05-diagnostico-smt.png", { left: 565, top: 88, width: 635, height: 510 }, "Pantalla Diagnostico SMT", "contain");
  foot(s, 8);
}

{
  const s = slide();
  s.background.fill = C.green2;
  text(s, "Como explicarlo en una demo", { left: 72, top: 70, width: 760, height: 70 }, { size: 44, bold: true, color: C.white });
  text(s, "Guion para que una persona nueva entienda el sistema en pocos minutos.", { left: 76, top: 146, width: 760, height: 36 }, { size: 23, color: "#DDECE7" });
  const rows = [
    ["1", "Summary SMT", "Empieza por la vista ejecutiva y el cuello de botella."],
    ["2", "Capacidad SMT", "Explica MPS, Cap, Delta y colores de utilizacion."],
    ["3", "Editar", "Muestra como mover familias o ajustar factor."],
    ["4", "Administracion", "Ensena donde viven familias, UPH y distribucion."],
    ["5", "Diagnostico", "Cierra mostrando si los datos estan listos."]
  ];
  let y = 232;
  for (const [n, head, body] of rows) {
    rect(s, { left: 96, top: y, width: 54, height: 54 }, "#23564B", "none", "rounded-xl");
    text(s, n, { left: 96, top: y + 13, width: 54, height: 24 }, { size: 21, bold: true, color: C.white, align: "center" });
    text(s, head, { left: 180, top: y + 2, width: 220, height: 28 }, { size: 24, bold: true, color: C.white });
    text(s, body, { left: 420, top: y + 5, width: 560, height: 32 }, { size: 19, color: "#DDECE7" });
    y += 76;
  }
  rect(s, { left: 930, top: 92, width: 230, height: 84 }, C.yellow, "none", "rounded-xl");
  text(s, "Mensaje clave", { left: 960, top: 116, width: 170, height: 28 }, { size: 22, bold: true, color: C.ink, align: "center" });
  text(s, "Importar, validar, revisar capacidad y ajustar.", { left: 920, top: 600, width: 260, height: 44 }, { size: 24, bold: true, color: C.white, align: "center" });
}

{
  const s = slide();
  s.background.fill = C.mist;
  title(s, "Que debe recordar el usuario");
  text(s, "Tres ideas son suficientes para empezar a usar el sistema.", { left: 78, top: 154, width: 760, height: 42 }, { size: 26, color: C.slate });
  rect(s, { left: 110, top: 260, width: 300, height: 210 }, C.white, C.line, "rounded-2xl");
  rect(s, { left: 490, top: 260, width: 300, height: 210 }, C.white, C.line, "rounded-2xl");
  rect(s, { left: 870, top: 260, width: 300, height: 210 }, C.white, C.line, "rounded-2xl");
  text(s, "1", { left: 135, top: 292, width: 42, height: 34 }, { size: 28, bold: true, color: C.green });
  text(s, "Importar bien", { left: 135, top: 344, width: 230, height: 34 }, { size: 26, bold: true });
  text(s, "El calculo depende de volumen, UPH, distribucion y calendario.", { left: 135, top: 400, width: 230, height: 46 }, { size: 17, color: C.slate });
  text(s, "2", { left: 515, top: 292, width: 42, height: 34 }, { size: 28, bold: true, color: C.green });
  text(s, "Leer el semaforo", { left: 515, top: 344, width: 230, height: 34 }, { size: 26, bold: true });
  text(s, "Verde cabe, amarillo se revisa, rojo necesita accion.", { left: 515, top: 400, width: 230, height: 46 }, { size: 17, color: C.slate });
  text(s, "3", { left: 895, top: 292, width: 42, height: 34 }, { size: 28, bold: true, color: C.green });
  text(s, "Ajustar con criterio", { left: 895, top: 344, width: 230, height: 34 }, { size: 26, bold: true });
  text(s, "Mover familias o cambiar factor sirve para probar escenarios.", { left: 895, top: 400, width: 230, height: 46 }, { size: 17, color: C.slate });
  text(s, "Con eso, una persona nueva entiende que hace la herramienta y donde debe entrar para cada actividad.", { left: 170, top: 545, width: 940, height: 44 }, { size: 24, bold: true, align: "center" });
  foot(s, 10);
}

await (await PresentationFile.exportPptx(deck)).save(outputPath);

const montage = await deck.export({ format: "webp", montage: true, scale: 0.8 });
await fs.writeFile(path.join(previewDir, "capacity-planner-smt-v2-montage.webp"), Buffer.from(await montage.arrayBuffer()));

for (let i = 0; i < slides.length; i += 1) {
  const png = await deck.export({ slide: slides[i], format: "png", scale: 1 });
  await fs.writeFile(path.join(previewDir, `slide-${String(i + 1).padStart(2, "0")}.png`), Buffer.from(await png.arrayBuffer()));
}

console.log(outputPath);
