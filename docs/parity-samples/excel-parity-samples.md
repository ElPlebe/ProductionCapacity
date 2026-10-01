# Excel Parity Samples

Workbook: `C:\Users\carl0\OneDrive\Documents\1) Cap model BE CISCO V1 170726.xlsb`

These samples are used to validate that the .NET planning engine reproduces the Excel planning matrix behavior before expanding the model.

## Current Samples

| Source sheet | Part number | Demand | Operation | Excel load hours | Available hours | Remaining hours | Notes |
| --- | --- | ---: | --- | ---: | ---: | ---: | --- |
| `PLAN UABU` | `68-103450` | 1000 | `ASSY` | 20.519 | 254.700 | 234.181 | Load is demand divided by the effective backend rate. |
| `PLAN UABU` | `68-103450` | 1000 | `PACK` | 27.359 | 254.700 | 227.341 | Load is demand divided by the effective backend rate. |

## Formula Contract

```text
available_hours = (work_days - holiday_days) * hours_per_shift * shifts_per_day * equipment_quantity
effective_uph = (3600 / standard_time_seconds) * yield * utilization_factor * efficiency_factor
load_hours = demand_units / effective_uph
remaining_hours = available_hours - total_operation_load_hours
load_percent = total_operation_load_hours / available_hours * 100
```

## Next Samples To Add

- Chamber-constrained part from Longball.
- eORT chamber sample.
- Mobius/Quake PCB sample.
- NGFW sample.
- Rack/chasis constrained sample.
