"""Reproducible diagnostics for the supplied outward-intent phone recording.
No accuracy claims: action counts are app events, not manually labeled punches.
"""
import csv
import hashlib
import json
import math
import statistics
from collections import Counter
from pathlib import Path

root = Path(__file__).resolve().parent
records = [json.loads(line) for line in (root / 'tracking.jsonl').read_text().splitlines() if line.strip()]
observations = [r for r in records if r['kind'] == 'observation']
poses = [r for r in records if r['kind'] == 'pose']
t0 = poses[0]['time']
duration = poses[-1]['time'] - t0
by_sequence = {r['observation']['sequence']: r for r in observations}
def vector(arm, joint):
    p = arm[joint]
    return complex(p['x'], p['y'])
def bend(arm):
    u = -vector(arm, 'elbow')
    v = vector(arm, 'wrist') - vector(arm, 'elbow')
    cosine = (u.conjugate() * v).real / max(1e-10, abs(u) * abs(v))
    return 180 - math.degrees(math.acos(max(-1, min(1, cosine))))
def percentile(values, quantile):
    return sorted(values)[int((len(values) - 1) * quantile)]
def write_csv(name, rows):
    with (root / name).open('w', newline='') as output:
        writer = csv.DictWriter(output, fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows(rows)

initial_action = poses[0]['action']
actions = {}
for pose in poses:
    if pose['action'] == initial_action:
        continue  # Recording began after earlier actions and five existing hits.
    action = actions.setdefault(pose['action'], dict(action=pose['action'], side=pose['side'],
        start=pose['time']-t0, commit=None, hit=None, hitsBefore=pose['hits'], sequence=pose['observationSequence']))
    if pose['state'] == 'Striking' and action['commit'] is None:
        action['commit'] = pose['time']-t0
    if pose['hits'] > action['hitsBefore'] and action['hit'] is None:
        action['hit'] = pose['time']-t0
write_csv('actions.csv', list(actions.values()))

rows = []
for record in observations:
    frame = record['observation']
    row = dict(sequence=frame['sequence'], relative=record['recordedAt']-t0,
        captureRelative=frame['time']-t0, receivedRelative=frame['received']-t0)
    for side in ['left', 'right']:
        arm = frame[side]
        row.update({side+'State':record[side+'State'], side+'Reason':record[side+'Reason'],
            side+'Confidence':arm['confidence'], side+'Bend':bend(arm), side+'Speed':record[side+'Speed'],
            side+'WristX':arm['wrist']['x'], side+'WristY':arm['wrist']['y']})
    rows.append(row)
write_csv('observations.csv', rows)

latencies = [(r['observation']['received']-r['observation']['time'])*1000 for r in observations]
intervals = [(b['time']-a['time'])*1000 for a,b in zip(poses, poses[1:])]
observation_intervals = [(b['observation']['time']-a['observation']['time'])*1000 for a,b in zip(observations, observations[1:])]
committed = [a for a in actions.values() if a['commit'] is not None]
blocks=[]
start=last=None
for record in observations:
    time=record['recordedAt']-t0
    if record['leftReason']=='Move one arm at a time':
        if start is None: start=time
        last=time
    elif start is not None:
        blocks.append(dict(start=start,lastBlocked=last,nextUnblocked=time));start=None
if start is not None:blocks.append(dict(start=start,lastBlocked=last,nextUnblocked=None))

stages=[]
for lo,hi,label in [(0,10,'setup'),(10,14,'hold'),(14,18,'relax'),(18,26,'left slow'),(26,34,'left fast'),(34,42,'right slow'),(42,50,'right fast'),(50,58,'alternating'),(58,65,'withdrawals'),(65,70,'raise/lower'),(70,75,'step out')]:
    samples=[r for r in observations if lo<=r['recordedAt']-t0<hi]
    stages.append(dict(label=label,start=lo,end=hi,observations=len(samples),blocked=sum(r['leftReason']=='Move one arm at a time' for r in samples),
        commitments=sum(lo<=a['commit']<hi for a in committed)))
write_csv('stages.csv',stages)

metrics=dict(version=records[0]['version'],device=records[0]['device'],engine=records[0]['engine'],
    logSha256=hashlib.sha256((root/'tracking.jsonl').read_bytes()).hexdigest(),
    videoSha256=hashlib.sha256((root/'vid.mp4').read_bytes()).hexdigest(),
    t0=t0,duration=duration,observations=len(observations),poses=len(poses),
    initialHits=poses[0]['hits'],finalHits=poses[-1]['hits'],newHits=poses[-1]['hits']-poses[0]['hits'],
    newActions=len(actions),committedActions=len(committed),cancelledActions=len(actions)-len(committed),
    independentObservationsPerSecond=len(observations)/duration,renderPosesPerSecond=len(poses)/duration,
    medianObservationIntervalMs=statistics.median(observation_intervals),
    firstObservationRelative=observations[0]['recordedAt']-t0,
    medianCameraAvailabilityToResultMs=statistics.median(latencies),p95CameraAvailabilityToResultMs=percentile(latencies,.95),
    medianPreparationToCommitMs=statistics.median([(a['commit']-a['start'])*1000 for a in committed]),
    medianCommitToHitMs=statistics.median([(a['hit']-a['commit'])*1000 for a in committed if a['hit'] is not None]),
    medianCandidateCameraAvailabilityToHitMs=statistics.median([(a['hit']+t0-by_sequence[a['sequence']]['observation']['time'])*1000 for a in committed if a['hit'] is not None]),
    p95RenderedPoseIntervalMs=percentile(intervals,.95),renderIntervalsOver50ms=sum(t>50 for t in intervals),
    validLeftObservations=sum(r['observation']['left']['confidence']>=.35 for r in observations),
    validRightObservations=sum(r['observation']['right']['confidence']>=.35 for r in observations),
    blockedObservations=sum(r['leftReason']=='Move one arm at a time' for r in observations),
    commitmentsBySide=dict(Counter('left' if a['side']==0 else 'right' for a in committed)),
    endMarker=records[-1],blocks=blocks,
    caveat='Camera availability is not exposure time; no physical motion-to-photon latency or overall recognition accuracy is established.')
(root/'summary.json').write_text(json.dumps(metrics,indent=2)+'\n')
print(json.dumps({k:v for k,v in metrics.items() if k!='blocks'},indent=2))
