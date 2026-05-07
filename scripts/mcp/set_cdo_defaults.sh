#!/bin/bash
BP="/Game/BluePrints/Exhibition/BP_ExhibitionStyleManager"

set_mat() {
  local prop=$1
  local val=$2
  curl -s -X POST http://localhost:3000/mcp/tool/blueprint_modify \
    -H "Content-Type: application/json" \
    -d "{\"operation\":\"set_cdo_default\",\"blueprint_path\":\"${BP}\",\"property\":\"${prop}\",\"value\":\"${val}\"}" \
    -o /tmp/r.json
  if grep -q '"success":true' /tmp/r.json; then
    echo "OK: ${prop}"
  else
    echo "FAIL: ${prop} -> $(grep -o '"message":"[^"]*"' /tmp/r.json)"
  fi
}

# Classic 4096
set_mat "Mat_Classic_Ceilings" "/Game/ExhibitionModular/Materials/Classic/4096/4096_Classic_Ceilings_Material.4096_Classic_Ceilings_Material"
set_mat "Mat_Classic_Display"  "/Game/ExhibitionModular/Materials/Classic/4096/4096_Classic_Display_Material.4096_Classic_Display_Material"
set_mat "Mat_Classic_Fittings" "/Game/ExhibitionModular/Materials/Classic/4096/4096_Classic_Fittings_Material.4096_Classic_Fittings_Material"
set_mat "Mat_Classic_Floors"   "/Game/ExhibitionModular/Materials/Classic/4096/4096_Classic_Floors_Material.4096_Classic_Floors_Material"
set_mat "Mat_Classic_Plaster"  "/Game/ExhibitionModular/Materials/Classic/4096/4096_Classic_Plaster_Material.4096_Classic_Plaster_Material"
set_mat "Mat_Classic_Walls"    "/Game/ExhibitionModular/Materials/Classic/4096/4096_Classic_Walls_Material.4096_Classic_Walls_Material"

# Aged 4096
set_mat "Mat_Aged_Ceilings" "/Game/ExhibitionModular/Materials/Aged/4096/4096_Aged_Ceilings_Material.4096_Aged_Ceilings_Material"
set_mat "Mat_Aged_Display"  "/Game/ExhibitionModular/Materials/Aged/4096/4096_Aged_Display_Material.4096_Aged_Display_Material"
set_mat "Mat_Aged_Fittings" "/Game/ExhibitionModular/Materials/Aged/4096/4096_Aged_Fittings_Material.4096_Aged_Fittings_Material"
set_mat "Mat_Aged_Floors"   "/Game/ExhibitionModular/Materials/Aged/4096/4096_Aged_Floors_Material.4096_Aged_Floors_Material"
set_mat "Mat_Aged_Plaster"  "/Game/ExhibitionModular/Materials/Aged/4096/4096_Aged_Plaster_Material.4096_Aged_Plaster_Material"
set_mat "Mat_Aged_Walls"    "/Game/ExhibitionModular/Materials/Aged/4096/4096_Aged_Walls_Material.4096_Aged_Walls_Material"

# Modern 4096
set_mat "Mat_Modern_Ceilings" "/Game/ExhibitionModular/Materials/Modern/4096/4096_Modern_Ceilings_Material.4096_Modern_Ceilings_Material"
set_mat "Mat_Modern_Display"  "/Game/ExhibitionModular/Materials/Modern/4096/4096_Modern_Display_Material.4096_Modern_Display_Material"
set_mat "Mat_Modern_Fittings" "/Game/ExhibitionModular/Materials/Modern/4096/4096_Modern_Fittings_Material.4096_Modern_Fittings_Material"
set_mat "Mat_Modern_Floors"   "/Game/ExhibitionModular/Materials/Modern/4096/4096_Modern_Floors_Material.4096_Modern_Floors_Material"
set_mat "Mat_Modern_Plaster"  "/Game/ExhibitionModular/Materials/Modern/4096/4096_Modern_Plaster_Material.4096_Modern_Plaster_Material"
set_mat "Mat_Modern_Walls"    "/Game/ExhibitionModular/Materials/Modern/4096/4096_Modern_Walls_Material.4096_Modern_Walls_Material"

echo "All done"
