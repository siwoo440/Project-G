"""The approved spreadsheet snapshots are the oracle, not generated C# constants."""
import importlib.util
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("export_story_catalog", ROOT / "tools/story/export_story_catalog.py")


class ExportContractTests(unittest.TestCase):
    def setUp(self):
        if not SPEC.origin or not Path(SPEC.origin).exists():
            self.fail("Production exporter has not been implemented")
        self.module = importlib.util.module_from_spec(SPEC)
        SPEC.loader.exec_module(self.module)

    def test_real_sheet_shapes(self):
        source = Path(__import__("os").environ["PROJECT_G_STORY_SOURCE"])
        result = self.module.build_catalog(source / "dataset.json", source / "story_snapshot.json", source / "step4_live_sheet.json")
        self.assertEqual((32, 100, 1034, 22, 26, 13), tuple(len(result[x]) for x in ("missions", "scenes", "lines", "numericGoals", "waves", "arenas")))
        self.module.validate(result)

    def test_c8_is_one_ordered_operation(self):
        source = Path(__import__("os").environ["PROJECT_G_STORY_SOURCE"])
        result = self.module.build_catalog(source / "dataset.json", source / "story_snapshot.json", source / "step4_live_sheet.json")
        operation = next(s for s in result["stages"] if s["id"] == "ST-8-OP")
        self.assertEqual([f"C08-S{i:02d}" for i in range(1, 8)], operation["sceneIds"])
        self.assertEqual("C08-S07", operation["settlementScene"])
        self.assertEqual(["M-8-1", "M-8-2", "M-8-3"], operation["missionIds"])

    def test_original_dialogue_is_not_rewritten(self):
        source = Path(__import__("os").environ["PROJECT_G_STORY_SOURCE"])
        raw = json.loads((source / "story_snapshot.json").read_text())
        expected = {}
        for name, sheet in raw.items():
            if name[:2].isdigit() and 2 <= int(name[:2]) <= 13:
                for row in self.module.rows(sheet):
                    if len(row) >= 4 and "-L" in str(row[0]):
                        expected[str(row[0])] = str(row[3])
        result = self.module.build_catalog(source / "dataset.json", source / "story_snapshot.json", source / "step4_live_sheet.json")
        self.assertEqual(expected, {line["id"]: line["text"] for line in result["lines"]})

    def test_duplicate_or_missing_reference_is_rejected(self):
        source = Path(__import__("os").environ["PROJECT_G_STORY_SOURCE"])
        result = self.module.build_catalog(source / "dataset.json", source / "story_snapshot.json", source / "step4_live_sheet.json")
        result["waves"][0]["sceneId"] = "C99-S99"
        with self.assertRaises(ValueError):
            self.module.validate(result)

    def test_no_battle_added_to_safe_segments(self):
        source = Path(__import__("os").environ["PROJECT_G_STORY_SOURCE"])
        result = self.module.build_catalog(source / "dataset.json", source / "story_snapshot.json", source / "step4_live_sheet.json")
        combat_stages = {w["stageId"] for w in result["waves"]}
        self.assertFalse(combat_stages & {"ST-5-2", "ST-6-2", "ST-7-3"})
        self.assertEqual(13, len(combat_stages))


if __name__ == "__main__":
    unittest.main()
