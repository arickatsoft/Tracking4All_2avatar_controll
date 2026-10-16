#!/usr/bin/env python3
"""Export editable movement labels from a MediaPipe JSONL session (standard library only).

Cue labels describe requested actions, not verified human poses. Annotate performed_correctly
and failures after watching the matching video. Times are seconds since session start.
"""
import argparse
import csv
import json
from pathlib import Path


def export(session: Path, output: Path) -> int:
    cues = []
    start = None
    end = None
    with session.open() as stream:
        for number, line in enumerate(stream, 1):
            try:
                record = json.loads(line)
            except json.JSONDecodeError as exc:
                raise ValueError(f"Malformed JSON at line {number}") from exc
            if start is None:
                start = record.get("recordingStartedAt")
            if record.get("kind") == "reconstruction":
                end = record.get("receivedAt", record.get("time", end))
                if start is None:
                    # New session metadata starts before first calibrated movement.
                    start = record.get("time", record.get("receivedAt"))
            if record.get("recordingEndedAt", 0) > 0:
                end = record["recordingEndedAt"]
            if record.get("kind") == "guide_cue":
                cues.append(record)
    if start is None:
        raise ValueError("Session has no monotonic start time or diagnostic samples")
    columns = ["session", "requested_movement", "start_s", "end_s", "performed_correctly",
               "body_region", "failure", "severity_0_to_3", "video_start_s", "notes"]
    with output.open("w", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=columns)
        writer.writeheader()
        for i, cue in enumerate(cues):
            began = cue["recordedAt"]
            finished = cues[i + 1]["recordedAt"] if i + 1 < len(cues) else end
            writer.writerow({"session": session.name, "requested_movement": cue["label"],
                             "start_s": round(began-start, 3),
                             "end_s": round(finished-start, 3) if finished is not None else "",
                             "performed_correctly": "unreviewed"})
    return len(cues)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("session", type=Path)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    output = args.output or args.session.with_suffix(".labels.csv")
    count = export(args.session, output)
    print(f"Wrote {count} movement intervals to {output}. Labels require video review; no model was trained.")


if __name__ == "__main__":
    main()
