import json,math,statistics,collections,hashlib,csv
from pathlib import Path
ROOT=Path(__file__).resolve().parent

def stats(v):
 if not v:return {}
 v=sorted(v);return dict(min=v[0],median=statistics.median(v),p95=v[int((len(v)-1)*.95)],max=v[-1])
def save(name,rows):
 if not rows:return
 with (ROOT/name).open('w') as f:
  w=csv.DictWriter(f,fieldnames=rows[0].keys());w.writeheader();w.writerows(rows)
for tag in ['body','punches']:
 path=Path('/Users/jotit/Desktop')/(tag+'.jsonl');obs=[];rec=[];cues=[];poses=[];counts=collections.Counter();h=hashlib.sha256();meta=None
 with path.open('rb') as src:
  for line in src:
   h.update(line);r=json.loads(line);kind=r['kind'];counts[kind]+=1
   if meta is None:meta=r;t0=meta['recordingStartedAt']
   if kind=='observation':r['relative']=r['recordedAt']-t0;obs.append(r)
   elif kind=='guide_cue':cues.append(r)
   elif kind=='reconstruction':r['relative']=r['time']-t0;rec.append(r)
   elif kind=='pose':
    c=r['context'];poses.append(dict(relative=r['time']-t0,seq=r['observationSequence'],applied=c['appliedSequence'],action=r['action'],side=r['side'],state=r['state'],phase=r['phase'],hits=r['hits'],owner=c['poseOwner'],lfy=c['leftFoot']['y'],rfy=c['rightFoot']['y'],bend=c['bodyAngles']['x'],yaw=c['bodyAngles']['y'],roll=c['bodyAngles']['z'],head=c['headAngles'],targets=sum(c['targetApplied']),lh=c['leftContact'],rh=c['rightContact']))
   elif kind=='end':end=r
 first={}
 for p in poses:
  if p['seq']==p['applied'] and p['targets']>0:first.setdefault(p['seq'],p['relative'])
 actions={}
 for p in poses:
  if p['action']==poses[0]['action']:continue
  a=actions.setdefault(p['action'],dict(id=p['action'],side=p['side'],start=p['relative'],commit=None,hit=None,sequence=p['seq'],hitsBefore=p['hits'],minGap=None,reasons=[]))
  if p['state']=='Striking' and a['commit'] is None:a['commit']=p['relative']
  if p['hits']>a['hitsBefore'] and a['hit'] is None:a['hit']=p['relative']
  ct=p['lh'] if p['side']==0 else p['rh']
  if p['state']=='Striking' and ct['allowImpact'] and ct['tracked']:
   a['minGap']=min(ct['gap'],a['minGap']) if a['minGap'] is not None else ct['gap']
   if ct['reason'] not in a['reasons']:a['reasons'].append(ct['reason'])
 summary=dict(metadata=meta,sha256=h.hexdigest(),counts=dict(counts),duration=poses[-1]['relative'],end=end,obsFPS=len(obs)/poses[-1]['relative'],newHits=poses[-1]['hits']-poses[0]['hits'],owners=dict(collections.Counter(p['owner'] for p in poses)),availabilityToReceiveMs=stats([(o['observation']['received']-o['observation']['time'])*1000 for o in obs]),observationIntervalMs=stats([(b['observation']['time']-a['observation']['time'])*1000 for a,b in zip(obs,obs[1:])]),poseIntervalMs=stats([(b['relative']-a['relative'])*1000 for a,b in zip(poses,poses[1:])]),cameraFPS=stats([r['cameraFPS'] for r in rec]),inferenceFPS=stats([r['inferenceFPS'] for r in rec]),availableToFirstDirectMs=stats([(first[o['observation']['sequence']]-(o['observation']['time']-t0))*1000 for o in obs if o['observation']['sequence'] in first]),receiveToFirstDirectMs=stats([(first[o['observation']['sequence']]-(o['observation']['received']-t0))*1000 for o in obs if o['observation']['sequence'] in first]),actions=list(actions.values()),cues=cues)
 stages=[]
 for i,c in enumerate(cues):
  lo=c['recordedAt']-t0;hi=cues[i+1]['recordedAt']-t0 if i+1<len(cues) else poses[-1]['relative'];oo=[o for o in obs if lo<=o['relative']<hi];rr=[r for r in rec if lo<=r['relative']<hi];pp=[p for p in poses if lo<=p['relative']<hi]
  if not pp:continue
  s=dict(label=c['label'],start=lo,end=hi,observations=len(oo),poses=len(pp),targets=stats([p['targets'] for p in pp]),lastPitch=rr[-1]['pitch'] if rr else None,lastGravity=rr[-1]['gravityPitch'] if rr else None,scale=rr[-1]['scale'] if rr else None,leftFootY=stats([p['lfy'] for p in pp]),rightFootY=stats([p['rfy'] for p in pp]),bend=stats([p['bend'] for p in pp]),yaw=stats([p['yaw'] for p in pp]),roll=stats([p['roll'] for p in pp]),headPitch=stats([p['head']['x'] for p in pp]),commits=sum(a['commit'] is not None and lo<=a['commit']<hi for a in actions.values()))
  for side in [0,1]:
   ids=[5+side,7+side,9+side];s['armRawValid'+str(side)]=sum(min(o['observation']['jointScores'][j] for j in ids)>=.38 for o in oo)
   s['armObserved'+str(side)]=sum(all(r['observed'][j] for j in ids) for r in rr)
  stages.append(s)
 summary['stages']=stages
 (ROOT/(tag+'-summary.json')).write_text(json.dumps(summary,indent=2));save(tag+'-stages.csv',stages);save(tag+'-actions.csv',list(actions.values()));save(tag+'-poses.csv',poses)
 (ROOT/(tag+'-reconstruction.jsonl')).write_text('\n'.join(json.dumps(r) for r in rec)+'\n')
 fixture=Path('Docs/PunchSessions')/('20260930-standing-fix-'+tag+'-regression.jsonl')
 with fixture.open('w') as out:
  out.write(json.dumps(dict(kind='fixture',metadata=meta,sourceSha256=h.hexdigest(),sourceName=path.name,caveat='New recording; prior video exclusion does not apply. Accepted observations only; calibration samples before acceptance absent.'))+'\n')
  for o in obs:out.write(json.dumps(dict(kind='observation',recordedRelative=o['relative'],observation=o['observation'],leftState=o['leftState'],rightState=o['rightState'],leftReason=o['leftReason'],rightReason=o['rightReason']))+'\n')
 print(tag,json.dumps({k:v for k,v in summary.items() if k not in ['metadata','cues','stages','actions']}))
 print('CUES',[(c['label'],round(c['recordedAt']-t0,2)) for c in cues]);print('ACTIONS',json.dumps(summary['actions']))
