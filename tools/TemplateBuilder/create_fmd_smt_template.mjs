import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "..", "..");
const outputPath = path.join(repoRoot, "import-templates", "fmd_smt_template.xlsx");
const fontName = "Aptos";

function styleTitle(sheet, rangeAddress) {
  const range = sheet.getRange(rangeAddress);
  range.format.font = { name: fontName, bold: true, size: 14, color: "#183F36" };
}

function styleHeaders(sheet, rangeAddress) {
  const range = sheet.getRange(rangeAddress);
  range.format = {
    fill: "#213B35",
    font: { name: fontName, bold: true, color: "#FFFFFF" },
    horizontalAlignment: "center",
    verticalAlignment: "center",
    wrapText: true,
  };
}

function styleInputs(sheet, rangeAddress) {
  const range = sheet.getRange(rangeAddress);
  range.format = {
    fill: "#FFF2CC",
    font: { name: fontName, color: "#111827" },
    borders: { preset: "all", style: "thin", color: "#D9E2DF" },
  };
}

function applyBaseStyle(sheet) {
  sheet.showGridLines = false;
  sheet.getRange("A1:Z120").format.font = { name: fontName, size: 10, color: "#111827" };
}

function addInstructions(workbook) {
  const sheet = workbook.worksheets.add("INSTRUCCIONES");
  applyBaseStyle(sheet);
  sheet.getRange("A1").values = [["Plantilla SMT"]];
  styleTitle(sheet, "A1");
  sheet.getRange("A3:D3").values = [["Hoja", "Que captura", "Campos clave", "Notas"]];
  sheet.getRange("A4:D9").values = [
    ["CONFIG", "Cuarto y anio fiscal", "FiscalYear, FiscalQuarter", "Q1 Ago/Sep/Oct, Q2 Nov/Dic/Ene, Q3 Feb/Mar/Abr, Q4 May/Jun/Jul."],
    ["VOL", "Volumen mensual por familia", "BU, SUB FAMILY, TAN PN, M1, M2, M3", "Usa una fila por numero de parte; el sistema resume por familia."],
    ["UPH", "UPH y changeover por familia", "SUB FAMILY, Change over, BOTTLENECK", "Si no tienes BOTTLENECK, llena StandardTimeSeconds, YieldFactor y EfficiencyFactor."],
    ["% Distribution", "Asignacion por linea SMT", "Line 1 a Line 6", "Captura porcentajes como 100%, 50% o 0%; no captures 100 como numero."],
    ["Capacity SMT", "Targets opcionales de comparacion", "MAX CAP, SMT Utilization, Delta Cap", "Solo se usa para comparar contra Excel; puedes dejarlo vacio."],
    ["Importacion", "Carga del sistema", "Archivo completo .xlsx", "Importa este archivo desde Cargar FMD SMT."],
  ];
  styleHeaders(sheet, "A3:D3");
  sheet.getRange("A4:D9").format = {
    fill: "#FFFFFF",
    font: { name: fontName, color: "#111827" },
    borders: { preset: "all", style: "thin", color: "#D9E2DF" },
    wrapText: true,
    verticalAlignment: "top",
  };
  sheet.getRange("A:D").format.autofitColumns();
  sheet.getRange("D:D").format.columnWidth = 54;
  sheet.freezePanes.freezeRows(3);
}

function addConfig(workbook) {
  const sheet = workbook.worksheets.add("CONFIG");
  applyBaseStyle(sheet);
  sheet.getRange("A1").values = [["Configuracion fiscal"]];
  styleTitle(sheet, "A1");
  sheet.getRange("A3:B3").values = [["FiscalYear", "FiscalQuarter"]];
  sheet.getRange("A4:B4").values = [[2026, "Q1"]];
  sheet.getRange("B4:B20").dataValidation = { rule: { type: "list", values: ["Q1", "Q2", "Q3", "Q4"] } };
  sheet.getRange("A6:B9").values = [
    ["Q1", "Agosto / Septiembre / Octubre"],
    ["Q2", "Noviembre / Diciembre / Enero"],
    ["Q3", "Febrero / Marzo / Abril"],
    ["Q4", "Mayo / Junio / Julio"],
  ];
  sheet.getRange("D3:E7").values = [
    ["Campo", "Uso"],
    ["FiscalYear", "Anio del primer mes del cuarto."],
    ["FiscalQuarter", "Usa Q1, Q2, Q3 o Q4."],
    ["M1 y M2", "4 semanas"],
    ["M3", "5 semanas"],
  ];
  styleHeaders(sheet, "A3:B3");
  styleInputs(sheet, "A4:B4");
  styleHeaders(sheet, "A6:B6");
  styleHeaders(sheet, "D3:E3");
  sheet.getRange("A:E").format.autofitColumns();
}

function addVol(workbook) {
  const sheet = workbook.worksheets.add("VOL");
  applyBaseStyle(sheet);
  sheet.getRange("A1").values = [["Volumen por familia SMT"]];
  styleTitle(sheet, "A1");
  sheet.getRange("A3:H3").values = [["BU", "FAMILY", "SUB FAMILY", "TAN PN", "MPS", "M1", "M2", "M3"]];
  styleHeaders(sheet, "A3:H3");
  styleInputs(sheet, "A4:H103");
  sheet.getRange("E4:H103").format.numberFormat = "#,##0.000";
  sheet.freezePanes.freezeRows(3);
  sheet.getRange("A:H").format.autofitColumns();
}

function addUph(workbook) {
  const sheet = workbook.worksheets.add("UPH");
  applyBaseStyle(sheet);
  sheet.getRange("A1").values = [["UPH y changeover por familia SMT"]];
  styleTitle(sheet, "A1");
  sheet.getRange("A2").values = [["Llena BOTTLENECK como UPH directo, o deja BOTTLENECK vacio y captura StandardTimeSeconds, YieldFactor y EfficiencyFactor para que el sistema calcule UPH efectivo."]];
  sheet.getRange("A2:L2").format = { font: { name: fontName, italic: true, color: "#4B5563" }, wrapText: true };
  sheet.getRange("A3:L3").values = [[
    "ACT", "FAMILY", "SUB FAMILY", "TAN PN", "Change over", "BOTTLENECK",
    "StandardTimeSeconds", "YieldFactor", "EfficiencyFactor", "ROUTER", "WAVE", "PRESSFIT",
  ]];
  styleHeaders(sheet, "A3:L3");
  styleInputs(sheet, "A4:L103");
  sheet.getRange("E4:G103").format.numberFormat = "#,##0.00";
  sheet.getRange("H4:I103").format.numberFormat = "0.00%";
  sheet.getRange("J4:L103").format.numberFormat = "#,##0.00";
  sheet.freezePanes.freezeRows(3);
  sheet.getRange("A:L").format.autofitColumns();
  sheet.getRange("G:I").format.columnWidth = 18;
}

function addDistribution(workbook) {
  const sheet = workbook.worksheets.add("% Distribution");
  applyBaseStyle(sheet);
  sheet.getRange("A1").values = [["Distribucion por linea SMT"]];
  sheet.getRange("X1").values = [["Captura porcentajes como 100%, 50% o 0%; no captures 100 como numero."]];
  sheet.getRange("X1").format = { font: { name: fontName, italic: true, color: "#4B5563" }, wrapText: true };
  styleTitle(sheet, "A1");
  sheet.getRange("C2:I2").values = [["SMT M1", "", "", "", "", "", ""]];
  sheet.getRange("J2:P2").values = [["SMT M2", "", "", "", "", "", ""]];
  sheet.getRange("Q2:W2").values = [["SMT M3", "", "", "", "", "", ""]];
  sheet.getRange("A3:W3").values = [[
    "BU", "FAMILY",
    "Line 1", "Line 2", "Line 3", "Line 4", "Line 5", "Line 6", "Total",
    "Line 1", "Line 2", "Line 3", "Line 4", "Line 5", "Line 6", "Total",
    "Line 1", "Line 2", "Line 3", "Line 4", "Line 5", "Line 6", "Total",
  ]];
  styleHeaders(sheet, "A3:W3");
  styleInputs(sheet, "A4:W103");
  sheet.getRange("C4:W103").format.numberFormat = "0%";
  sheet.freezePanes.freezeRows(3);
  sheet.freezePanes.freezeColumns(2);
  sheet.getRange("A:X").format.autofitColumns();
}

function addCapacitySmt(workbook) {
  const sheet = workbook.worksheets.add("Capacity SMT");
  applyBaseStyle(sheet);
  sheet.getRange("A1").values = [["Targets opcionales para comparar Capacity SMT"]];
  styleTitle(sheet, "A1");
  sheet.getRange("B2:R2").values = [[
    "SMT 1", "", "", "", "M1", "", "", "", "", "", "", "", "", "", "", "", "",
  ]];
  sheet.getRange("B3:R3").values = [[
    "LINE", "FAMILY", "Line distribution", "CISCO MPS (W)", "CISCO MPS (M)", "CISCO MPS (A M)",
    "SMT UPH", "CO Month", "CO Hrs Month", "MAX CAP (D) 100%", "MAX CAP (W) 100%",
    "MAX CAP (M) 100%", "MAX CAP (A M)", "SMT Utilization", "SMT UTILIZATION MAX", "Delta Cap", "Notas",
  ]];
  styleHeaders(sheet, "B3:R3");
  styleInputs(sheet, "B4:R103");
  sheet.getRange("B4:B103").format.numberFormat = "0";
  sheet.getRange("D4:Q103").format.numberFormat = "#,##0.000";
  sheet.freezePanes.freezeRows(3);
  sheet.getRange("A:R").format.autofitColumns();
}

const workbook = Workbook.create();
addInstructions(workbook);
addConfig(workbook);
addVol(workbook);
addUph(workbook);
addDistribution(workbook);
addCapacitySmt(workbook);

await fs.mkdir(path.dirname(outputPath), { recursive: true });
const xlsx = await SpreadsheetFile.exportXlsx(workbook);
await xlsx.save(outputPath);
console.log(outputPath);
