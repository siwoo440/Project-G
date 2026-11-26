"""Export bounded Google Sheets CellData snapshots without rewriting the scenario.

Usage: python tools/story/export_story_catalog.py --source /path/to/snapshots
Inputs: dataset.json (step3), story_snapshot.json (tabs00–13),
step4_live_sheet.json (bounded, freshly read tabs16–21).
The output is a Unity JsonUtility-compatible array model. No network mutation.
"""
import argparse
import hashlib
import json
import re
from pathlib import Path

SCENE = re.compile(r"(?:C\d{2}|P|E|SC\d{2}|SR\d{2})-S\d{2}")
LINE = re.compile(r"(?:C\d{2}|P|E|SC\d{2}|SR\d{2})-S\d{2}-L\d+")
SETTLEMENT = {
    "ST-P-1": "P-S04", "ST-1-1": "C01-S03", "ST-1-2": "C01-S04",
    "ST-2-2": "C02-S03", "ST-3-1": "C03-S04", "ST-3-2": "C03-S06",
    "ST-4-2": "C04-S03", "ST-5-1": "C05-S02", "ST-5-3": "C05-S06",
    "ST-6-1": "C06-S02", "ST-7-1": "C07-S02", "ST-7-2": "C07-S06",
    "ST-8-OP": "C08-S07",
}


def rows(sheet):
    for grid in sheet.get("data", []):
        for row in grid.get("rowData", []):
            result = []
            for cell in row.get("values", []):
                value = cell.get("effectiveValue", cell.get("userEnteredValue", {}))
                result.append(cell.get("formattedValue", next(iter(value.values()), "")))
            yield result


def padded(row, n):
    return [str(x) for x in row] + [""] * max(0, n - len(row))


def clauses(text, objective_id):
    items = re.split(r"(?:^|\n)(?:AND\s+)?\d+\.\s*", text.strip())
    items = [s.strip() for s in items[1:] if s.strip()] or [text.strip()]
    result = []
    for n, item in enumerate(items, 1):
        # The write receipt is an effect of a successful transaction, never its prerequisite.
        system = "Persistence" if "정산 영수증" in item and "저장" in item else "Evidence"
        result.append({"id": f"{objective_id}.C{n:02d}", "text": item, "kind": system})
    return result


def build_catalog(combat_path, scene_path, live_path):
    combat = json.loads(Path(combat_path).read_text())
    source = json.loads(Path(scene_path).read_text())
    live = json.loads(Path(live_path).read_text())
    sheets = {s["properties"]["title"]: s for s in live["sheets"]}
    result = {
        "schemaVersion": 1,
        "sourceUrl": live["spreadsheetUrl"],
        "sourceCommit": combat["source_commit"],
        "sourceDate": "2026-11-26",
        "sourceHashes": [{"name": Path(p).name, "sha256": hashlib.sha256(Path(p).read_bytes()).hexdigest()}
                         for p in (combat_path, scene_path, live_path)],
        "missions": [], "stages": [], "scenes": [], "objectives": [], "lines": [],
        "numericGoals": [], "waves": [], "arenas": [], "rules": [],
    }
    for raw in rows(sheets["16. 임무 목록"]):
        r = padded(raw, 12)
        if not r[0].startswith("M-"):
            continue
        stage = re.search(r"ST-[A-Z0-9-]+", r[7])
        result["missions"].append({
            "id": r[0], "chapter": r[1], "title": r[2], "kind": r[3], "goal": r[4],
            "sceneIds": SCENE.findall(r[5]), "cast": r[6], "stageId": stage.group() if stage else "",
            "progressRules": r[8], "rewardProfile": r[9], "sourceStatus": r[10],
        })
    mission_for_scene = {s: m for m in result["missions"] for s in m["sceneIds"]}
    for raw in rows(sheets["17. 목표 판정"]):
        r = padded(raw, 13)
        if not r[0].startswith("O-"):
            continue
        result["objectives"].append({
            "id": r[0], "missionId": r[1], "sceneId": r[2], "kind": r[3], "goal": r[4],
            "startConditions": r[5], "successCriteria": r[6], "failureCriteria": r[7],
            "completionEvent": r[8], "record": r[9], "notes": r[10],
            "clauses": clauses(r[6], r[0]),
        })
    for raw in rows(source["01. 장면 목록"]):
        r = padded(raw, 13)
        if not SCENE.fullmatch(r[0]):
            continue
        result["scenes"].append({
            "id": r[0], "chapter": r[1], "title": r[2], "location": r[3], "cast": r[4],
            "startConditions": r[5], "purpose": r[6], "exitConditions": r[7],
            "missionId": mission_for_scene[r[0]]["id"], "objectiveId": "O-" + r[0],
        })
    for name, sheet in source.items():
        if not name[:2].isdigit() or not 2 <= int(name[:2]) <= 13:
            continue
        for raw in rows(sheet):
            r = padded(raw, 6)
            if not LINE.fullmatch(r[0]):
                continue
            result["lines"].append({"id": r[0], "sceneId": r[0].rsplit("-L", 1)[0],
                                    "kind": r[1], "speaker": r[2], "text": r[3], "action": r[4], "camera": r[5]})
    scene_order = {s["id"]: i for i, s in enumerate(result["scenes"])}
    for mission in result["missions"]:
        flow_id = mission["stageId"] or mission["id"]
        flow = next((x for x in result["stages"] if x["id"] == flow_id), None)
        if flow is None:
            flow = {"id": flow_id, "title": mission["title"], "chapter": mission["chapter"],
                    "missionIds": [], "sceneIds": [], "settlementScene": SETTLEMENT.get(flow_id, ""),
                    "hasCombat": flow_id in SETTLEMENT, "optional": mission["id"].startswith(("M-SC", "M-SR"))}
            result["stages"].append(flow)
        flow["missionIds"].append(mission["id"])
        flow["sceneIds"].extend(mission["sceneIds"])
        flow["sceneIds"].sort(key=scene_order.get)
    for o in combat["objectives"]:
        result["numericGoals"].append({"id": o["id"], "sceneId": o["scene"], "stageId": o["stage"],
            "kind": o["type"], "protectedTarget": o["protected_target"], "durability": o["durability"] or 0,
            "hasDurability": o["durability"] is not None, "hasProgress": o["progress_seconds"] is not None,
            "progressSeconds": o["progress_seconds"] or 0, "routeLength": o["route_length"] or 0, "speed": o["speed"] or 0,
            "success": o["success"], "wait": o["wait"], "recoverable": o["recoverable"],
            "terminal": o["terminal"], "notes": o["notes"]})
    for w in combat["waves"]:
        result["waves"].append({"id": w["id"], "sceneId": w["scene"], "stageId": w["stage"],
            "offset": w["offset"], "trigger": w["trigger"], "composition": w["composition"],
            "maxAlive": w["max_alive"], "spawn": w["spawn"], "exit": w["exit"], "goal": w["goal"], "notes": w["notes"]})
    arena_table = next(t for t in combat["tabs"] if t["name"].startswith("29."))
    for raw in arena_table["rows"]:
        r = padded(raw, 12)
        result["arenas"].append(dict(zip(("id", "stageId", "title", "scenes", "volume", "entryPoints",
            "protectedTargets", "placement", "transformRules", "safety", "completionNotes", "sourceStatus"), r)))
    for name, sheet in sheets.items():
        if name[:2] not in ("18", "19", "20", "21"):
            continue
        for row_number, row in enumerate(rows(sheet)):
            if row_number > 0 and row and str(row[0]).strip():
                result["rules"].append({"table": name, "id": str(row[0]), "values": [str(x) for x in row[1:]]})
    validate(result)
    return result


def validate(catalog):
    counts = {"missions": 32, "scenes": 100, "objectives": 100, "lines": 1034, "numericGoals": 22, "waves": 26, "arenas": 13}
    for name, size in counts.items():
        if len(catalog[name]) != size:
            raise ValueError(f"{name}: expected {size}, got {len(catalog[name])}")
    indexes = {}
    for name in (*counts, "stages"):
        items = catalog[name]
        indexes[name] = {item["id"]: item for item in items}
        if len(indexes[name]) != len(items):
            raise ValueError(f"duplicate {name} ID")
    for name in ("lines", "objectives", "numericGoals", "waves"):
        for item in catalog[name]:
            if item["sceneId"] not in indexes["scenes"]:
                raise ValueError(f"{item['id']}: missing scene {item['sceneId']}")
    scene_ids = [s for m in catalog["missions"] for s in m["sceneIds"]]
    if len(scene_ids) != 100 or set(scene_ids) != set(indexes["scenes"]):
        raise ValueError("mission scene ownership must be unique and exhaustive")
    c8 = indexes["stages"]["ST-8-OP"]
    if c8["sceneIds"] != [f"C08-S{i:02d}" for i in range(1, 8)] or c8["settlementScene"] != "C08-S07":
        raise ValueError("C8 must be a single ordered attempt with only S07 settlement")
    safe = {"ST-5-2", "ST-6-2", "ST-7-3"}
    for wave in catalog["waves"]:
        if wave["stageId"] in safe or wave["maxAlive"] < 1 or wave["offset"] < 0:
            raise ValueError(f"invalid finite wave {wave['id']}")
        if wave["sceneId"] not in indexes["stages"][wave["stageId"]]["sceneIds"]:
            raise ValueError(f"wave outside stage {wave['id']}")
    if {w["stageId"] for w in catalog["waves"]} != set(SETTLEMENT):
        raise ValueError("combat stage set differs from the approved settlement map")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--output", type=Path, default=Path(__file__).resolve().parents[2] / "Assets/ProjectG/Resources/Story/ProjectGStory.json")
    args = parser.parse_args()
    value = build_catalog(args.source / "dataset.json", args.source / "story_snapshot.json", args.source / "step4_live_sheet.json")
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n")
    print(f"Exported {len(value['missions'])} missions, {len(value['scenes'])} scenes, {len(value['lines'])} original script rows")
