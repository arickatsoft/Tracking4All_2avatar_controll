#!/usr/bin/env python3
"""Inspect/export exact FFR1 inference inputs. PNGs are lossless; screen video is separate."""
import argparse
import gzip
import json
import struct
from pathlib import Path

def frames(path):
 with gzip.open(path,'rb') as stream:
  if stream.read(4)!=b'FFR1':raise ValueError('Not an FFR1 input archive')
  header=struct.Struct('<iiiiqdddi?i')
  while data:=stream.read(header.size):
   if len(data)!=header.size:raise ValueError('Truncated input metadata')
   w,h,sequence,source,inference,available,start,completed,rotation,front,size=header.unpack(data)
   if not (1<=w<=512 and 1<=h<=512 and size==w*h*4):raise ValueError('Invalid dimensions/length')
   rgba=stream.read(size)
   if len(rgba)!=size:raise ValueError('Truncated RGBA input')
   yield dict(width=w,height=h,cameraSequence=sequence,sourceId=source,inferenceSequence=inference,
              availableAt=available,conversionStartedAt=start,conversionCompletedAt=completed,
              sensorRotation=rotation,front=front),rgba

def png(path,w,h,rgba):
 import binascii,zlib
 def chunk(kind,data):return struct.pack('>I',len(data))+kind+data+struct.pack('>I',binascii.crc32(kind+data)&0xffffffff)
 raw=b''.join(b'\0'+rgba[y*w*4:(y+1)*w*4] for y in range(h))
 path.write_bytes(b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',w,h,8,6,0,0,0))+chunk(b'IDAT',zlib.compress(raw))+chunk(b'IEND',b''))

if __name__=='__main__':
 parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('archive',type=Path);parser.add_argument('--export',type=Path)
 args=parser.parse_args();metadata=[]
 if args.export:args.export.mkdir(parents=True,exist_ok=True)
 for item,rgba in frames(args.archive):
  metadata.append(item)
  if args.export:png(args.export/f"{item['inferenceSequence']:06d}.png",item['width'],item['height'],rgba)
 if args.export:(args.export/'frames.json').write_text(json.dumps(metadata,indent=2))
 print(json.dumps(dict(frames=len(metadata),first=metadata[0] if metadata else None,last=metadata[-1] if metadata else None),indent=2))
