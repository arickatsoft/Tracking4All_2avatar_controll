import csv
import json
from pathlib import Path
import tempfile
import unittest
from prepare_labels import export


class LabelsTest(unittest.TestCase):
    def test_explicit_clock_and_unreviewed_labels(self):
        records = [
            {"kind": "mediapipe_summary", "recordingStartedAt": 100},
            {"kind": "reconstruction", "receivedAt": 105},
            {"kind": "guide_cue", "recordedAt": 108, "label": "still_start"},
            {"kind": "guide_cue", "recordedAt": 116, "label": "arms_side"},
            {"kind": "mediapipe_summary", "recordingEndedAt": 120},
            {"kind": "end", "droppedRecords": 0},
        ]
        with tempfile.TemporaryDirectory() as directory:
            session = Path(directory) / "test.jsonl"
            output = Path(directory) / "labels.csv"
            session.write_text("\n".join(map(json.dumps, records)) + "\n")
            self.assertEqual(export(session, output), 2)
            with output.open() as stream:
                rows = list(csv.DictReader(stream))
            self.assertEqual(float(rows[0]["start_s"]), 8)
            self.assertEqual(float(rows[0]["end_s"]), 16)
            self.assertEqual(float(rows[1]["end_s"]), 20)
            self.assertTrue(all(r["performed_correctly"] == "unreviewed" for r in rows))
            self.assertTrue(all(not r["failure"] for r in rows))

    def test_malformed_data_is_not_silently_labeled(self):
        with tempfile.TemporaryDirectory() as directory:
            session = Path(directory) / "bad.jsonl"
            session.write_text('{"kind": broken}\n')
            with self.assertRaisesRegex(ValueError, "line 1"):
                export(session, session.with_suffix(".csv"))


if __name__ == "__main__":
    unittest.main()
