#!/bin/bash
BP="/Game/BluePrints/Exhibition/BP_ExhibitionStyleManager"

add_var() {
  local name=$1
  curl -s -X POST http://localhost:3000/mcp/tool/blueprint_modify \
    -H "Content-Type: application/json" \
    -d "{\"operation\":\"add_variable\",\"blueprint_path\":\"${BP}\",\"variable_name\":\"${name}\",\"variable_type\":\"Material*\"}" \
    -o /tmp/r.json
  if grep -q '"success":true' /tmp/r.json; then
    echo "OK: ${name}"
  else
    echo "FAIL: ${name} -> $(cat /tmp/r.json)"
  fi
}

add_var "Mat_Classic_Ceilings"
add_var "Mat_Classic_Display"
add_var "Mat_Classic_Fittings"
add_var "Mat_Classic_Floors"
add_var "Mat_Classic_Plaster"
add_var "Mat_Classic_Walls"
add_var "Mat_Aged_Ceilings"
add_var "Mat_Aged_Display"
add_var "Mat_Aged_Fittings"
add_var "Mat_Aged_Floors"
add_var "Mat_Aged_Plaster"
add_var "Mat_Aged_Walls"
add_var "Mat_Modern_Ceilings"
add_var "Mat_Modern_Display"
add_var "Mat_Modern_Fittings"
add_var "Mat_Modern_Floors"
add_var "Mat_Modern_Plaster"
add_var "Mat_Modern_Walls"
add_var "CurrentStyle"

echo "Done"
