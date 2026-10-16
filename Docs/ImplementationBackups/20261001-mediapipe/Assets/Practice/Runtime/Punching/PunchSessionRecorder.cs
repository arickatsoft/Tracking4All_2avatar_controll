using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Threading;
using UnityEngine;

namespace FitFight.Practice
{
    /// <summary>Optional bounded writer. No synchronous file IO on the animation loop.</summary>
    public sealed class PunchSessionRecorder : IDisposable
    {
        private readonly BlockingCollection<string> queue = new BlockingCollection<string>(256);
        private readonly Thread writer;
        private volatile string error;
        public string Error => error;
        private volatile bool finished;
        public bool Finished => finished;
        public string Path { get; }
        public int Dropped { get; private set; }
        private bool closed;
        [Serializable] private struct ObservationRecord
        {
            public string kind;
            public double recordedAt;
            public PunchCameraInput.Frame observation;
            public string leftState, rightState, leftReason, rightReason;
            public float leftSpeed, rightSpeed;
        }
        [Serializable] private struct PoseRecord
        {
            public string kind, state;
            public double time;
            public long observationSequence, action;
            public int side, hits;
            public float phase;
            public Vector3 root, leftFist, rightFist;
            public Quaternion rootRotation;
            public Quaternion[] localRotations;
            public Vector3[] localPositions, localScales, worldPositions;
            public PoseContext context;
        }
        [Serializable] public struct PoseContext
        {
            public bool assisted, canHit;
            public string poseOwner;
            public Vector3 leftFoot, rightFoot, bodyAngles, headAngles, bagPosition, bagVelocity, strikeTarget;
            public Quaternion bagRotation;
            public PracticeHeavyBag.ContactSample leftContact, rightContact;
            public PracticeHeavyBag.ContactSample leftFootContact, rightFootContact;
            public int appliedSequence;
            public double guideElapsed, calibrationPause;
            public Quaternion[] appliedTargets;
            public bool[] targetApplied;
        }
        [Serializable] private struct CueRecord
        {
            public string kind, test, label, speech;
            public double scheduledAt, recordedAt;
        }
        public PunchSessionRecorder(string metadata)
        {
            string directory = System.IO.Path.Combine(Application.persistentDataPath, "PunchSessions");
            Directory.CreateDirectory(directory);
            Path = System.IO.Path.Combine(directory, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture) + ".jsonl");
            queue.Add(metadata);
            writer = new Thread(Write) { IsBackground = true, Name = "FitFight punch recorder" }; writer.Start();
        }
        public void Diagnostic(FitFight.BodyTracking.RTMPosePoseProvider.Diagnostics diagnostic) => Add(JsonUtility.ToJson(diagnostic));
        private void Write()
        {
            try
            {
                using var stream = new StreamWriter(Path);
                foreach (string line in queue.GetConsumingEnumerable()) stream.WriteLine(line);
                stream.WriteLine("{\"kind\":\"end\",\"droppedRecords\":" + Dropped + "}");
            }
            catch (Exception e) { error = e.Message; }
            finally { finished = true; }
        }
        private void Add(string value)
        {
            if (closed || error != null) return;
            if (!queue.TryAdd(value)) Dropped++;
        }
        public void Observation(PunchCameraInput.Frame frame, PunchIntent left, PunchIntent right)
            => Add(JsonUtility.ToJson(new ObservationRecord { kind="observation", recordedAt=Time.realtimeSinceStartupAsDouble,
                observation=frame, leftState=left != null ? left.Phase.ToString() : "Disabled", rightState=right != null ? right.Phase.ToString() : "Disabled",
                leftReason=left != null ? left.Reason : "Body test", rightReason=right != null ? right.Reason : "Body test",
                leftSpeed=left != null ? left.Speed : 0, rightSpeed=right != null ? right.Speed : 0 }));
        public void Cue(string test, PunchRecordingGuide.Cue cue, double startedAt, double now)
            => Add(JsonUtility.ToJson(new CueRecord { kind="guide_cue", test=test, label=cue.label, speech=cue.speech,
                scheduledAt=startedAt+cue.at, recordedAt=now }));
        public void Pose(Transform root, Transform[] bones, Transform left, Transform right, string state, float phase,
            long sequence, long action, int side, int hits, PoseContext context = default)
        {
            var rotations = new Quaternion[bones.Length];
            var positions = new Vector3[bones.Length]; var scales = new Vector3[bones.Length]; var world = new Vector3[bones.Length];
            for (int i=0; i<bones.Length; i++)
            { rotations[i] = bones[i].localRotation; positions[i] = bones[i].localPosition; scales[i] = bones[i].localScale; world[i] = bones[i].position; }
            Add(JsonUtility.ToJson(new PoseRecord { kind="pose", time=Time.realtimeSinceStartupAsDouble,
                observationSequence=sequence, action=action, side=side, state=state, phase=phase, hits=hits,
                root=root.position, rootRotation=root.rotation, leftFist=left.position, rightFist=right.position,
                localRotations=rotations, localPositions=positions, localScales=scales, worldPositions=world, context=context }));
        }
        public void Dispose()
        {
            if (closed) return;
            closed=true; queue.CompleteAdding();
            // Background thread drains on its own; never stall a rendered frame to flush.
        }
    }
}
