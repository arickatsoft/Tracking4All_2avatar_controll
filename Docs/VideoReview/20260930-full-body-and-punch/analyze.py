"""Analyze supplied schema-2 logs; cue times are not movement ground truth."""
import collections,csv,hashlib,json,math,statistics
from pathlib import Path
ROOT=Path(__file__).resolve().parent
FILES={'body':('full body.jsonl','full body.mp4',1.3),'punch':('punches.jsonl','punchs.mp4',-5.5)}
def pct(v,p):return sorted(v)[int((len(v)-1)*p)] if v else None
def stats(v):return {'min':min(v),'median':statistics.median(v),'p95':pct(v,.95),'max':max(v)} if v else {}
def angle(a,b):return math.degrees(2*math.acos(min(1,abs(sum(a[k]*b[k] for k in 'xyzw')))))
def bend(arm):
 e=complex(arm['elbow']['x'],arm['elbow']['y']); w=complex(arm['wrist']['x'],arm['wrist']['y']); a=-e;b=w-e
 return 180-math.degrees(math.acos(max(-1,min(1,(a.conjugate()*b).real/max(1e-10,abs(a)*abs(b))))))
def csvsave(name,rows):
 if not rows:return
 with (ROOT/name).open('w',newline='') as f:
  w=csv.DictWriter(f,fieldnames=list(rows[0]));w.writeheader();w.writerows(rows)
for tag,(log,video,offset) in FILES.items():
 source=Path('/Users/jotit/Desktop')/log;rs=[json.loads(x) for x in source.open()];m=rs[0];t0=m['recordingStartedAt'];ps=[x for x in rs if x['kind']=='pose'];obs=[x for x in rs if x['kind']=='observation'];cs=[x for x in rs if x['kind']=='guide_cue']
 summary={'metadata':m,'logSha256':hashlib.sha256(source.read_bytes()).hexdigest(),'videoSha256':hashlib.sha256((Path('/Users/jotit/Desktop')/video).read_bytes()).hexdigest(),'recordingRelativeEqualsVideoTimePlus':offset,'offsetPrecisionSeconds':.15,'duration':ps[-1]['time']-t0,'observations':len(obs),'poses':len(ps),'end':rs[-1],'newHits':ps[-1]['hits']-ps[0]['hits'],'initialHits':ps[0]['hits'],'finalHits':ps[-1]['hits'],'availabilityToReceivedMs':stats([(x['observation']['received']-x['observation']['time'])*1000 for x in obs]),'observationIntervalMs':stats([(b['observation']['time']-a['observation']['time'])*1000 for a,b in zip(obs,obs[1:])]),'finalPoseIntervalMs':stats([(b['time']-a['time'])*1000 for a,b in zip(ps,ps[1:])]),'poseOwners':dict(collections.Counter(x['context']['poseOwner'] for x in ps)),'videoExclusions':[{'start':142,'end':152,'reason':'User performed incorrect left knee movement; exclude from accuracy judgments.'}] if tag=='body' else []}
 acts={};base=ps[0]['action']
 for p in ps:
  if p['action']==base:continue
  a=acts.setdefault(p['action'],dict(action=p['action'],side=p['side'],start=p['time']-t0,startVideo=p['time']-t0-offset,commit=None,hit=None,hitsBefore=p['hits'],sequence=p['observationSequence'],maxPhase=0,contactReasons=[],minimumGap=None))
  if p['state']=='Striking' and a['commit'] is None:a['commit']=p['time']-t0
  if p['hits']>a['hitsBefore'] and a['hit'] is None:a['hit']=p['time']-t0
  a['maxPhase']=max(p['phase'],a['maxPhase']);ct=p['context']['leftContact' if p['side']==0 else 'rightContact']
  if p['state']=='Striking':
   if ct['reason'] not in a['contactReasons']:a['contactReasons'].append(ct['reason'])
   if ct['allowImpact'] and ct['tracked']:a['minimumGap']=ct['gap'] if a['minimumGap'] is None else min(ct['gap'],a['minimumGap'])
 summary['actions']=list(acts.values());summary['committedActions']=sum(a['commit'] is not None for a in acts.values());summary['cancelledActions']=sum(a['commit'] is None for a in acts.values())
 csvsave(tag+'-actions.csv',list(acts.values()))
 out=[]
 for r in obs:
  f=r['observation'];v={'sequence':f['sequence'],'relative':r['recordedAt']-t0,'video':r['recordedAt']-t0-offset,'captureRelative':f['time']-t0,'receivedRelative':f['received']-t0}
  for side in ['left','right']:
   a=f[side];v.update({side+'State':r[side+'State'],side+'Reason':r[side+'Reason'],side+'Confidence':a['confidence'],side+'Bend':bend(a),side+'Speed':r[side+'Speed'],side+'WristX':a['wrist']['x'],side+'WristY':a['wrist']['y']})
  v['minTorsoScore']=min(f['jointScores'][i] for i in [5,6,11,12]);v['leftLegScore']=min(f['jointScores'][i] for i in [11,13,15]);v['rightLegScore']=min(f['jointScores'][i] for i in [12,14,16]);out.append(v)
 csvsave(tag+'-observations.csv',out)
 stages=[]
 for idx,c in enumerate(cs):
  lo=c['scheduledAt']-t0;hi=cs[idx+1]['scheduledAt']-t0 if idx+1<len(cs) else summary['duration'];pp=[p for p in ps if lo<=p['time']-t0<hi];oo=[o for o in out if lo<=o['relative']<hi]
  if not pp:continue
  s=dict(label=c['label'],start=lo,end=hi,startVideo=lo-offset,endVideo=hi-offset,observations=len(oo),poses=len(pp),leftLegValid=sum(o['leftLegScore']>=.3 for o in oo),rightLegValid=sum(o['rightLegScore']>=.3 for o in oo),torsoBendMin=min(p['context']['bodyAngles']['x'] for p in pp),torsoBendMax=max(p['context']['bodyAngles']['x'] for p in pp),torsoRollMin=min(p['context']['bodyAngles']['z'] for p in pp),torsoRollMax=max(p['context']['bodyAngles']['z'] for p in pp),leftFootMinY=min(p['context']['leftFoot']['y'] for p in pp),leftFootMaxY=max(p['context']['leftFoot']['y'] for p in pp),rightFootMinY=min(p['context']['rightFoot']['y'] for p in pp),rightFootMaxY=max(p['context']['rightFoot']['y'] for p in pp),commits=sum(a['commit'] is not None and lo<=a['commit']<hi for a in acts.values()),excludedForAccuracy=tag=='body' and c['label']=='LEFT LEG · GENTLE FRONT EXTENSION',partiallyExcludedVideo=tag=='body' and c['label']!='LEFT LEG · GENTLE FRONT EXTENSION' and lo-offset<152 and hi-offset>142)
  stages.append(s)
 csvsave(tag+'-stages.csv',stages);summary['stages']=stages
 (ROOT/(tag+'-summary.json')).write_text(json.dumps(summary,indent=2)+'\n')
 print(tag,json.dumps({k:v for k,v in summary.items() if k not in ['metadata','stages','actions','logSha256','videoSha256']}))
 if tag=='punch':print('actions',json.dumps(list(acts.values()),indent=2))
