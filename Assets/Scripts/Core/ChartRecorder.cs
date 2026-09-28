using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RhythmGame
{
    [RequireComponent(typeof(AudioSource))]
    public class ChartRecorder : MonoBehaviour
    {
        [Header("Refs")]
        public ChartData chart;
        public InputHandler input;

        [Header("Recording")]
        [Tooltip("Làm tròn beat về lưới: 1 = nguyên phách, 2 = nửa phách, 4 = 1/4 phách. 0 = giữ nguyên, không làm tròn.")]
        public int snapDivision = 4;
        [Tooltip("Bù độ trễ phản xạ tay (giây). Người bấm thường trễ hơn nhịp thật, thử 0.05 - 0.1 nếu note bị lệch về sau.")]
        public float latencyCompensation = 0f;
        [Tooltip("Giữ phím tối thiểu bao nhiêu phách thì tính là hold note (ngắn hơn thì là tap).")]
        public float holdThresholdBeats = 0.5f;
        [Tooltip("Bật: xoá note cũ trong chart khi bắt đầu thu. Tắt: thêm vào note đang có.")]
        public bool clearExistingNotes = true;
        [Tooltip("Thời gian chờ trước khi nhạc bắt đầu phát (giây).")]
        public float leadInSeconds = 1f;

        AudioSource audioSource;
        readonly List<NoteData> recorded = new List<NoteData>();
        readonly NoteData[] pressedNote = new NoteData[16];
        readonly float[] pressedTime = new float[16];
        double dspStart;
        bool recording;
        string status = "Nhan Enter de bat dau thu";
        GUIStyle style;

        float SongTime => (float)(AudioSettings.dspTime - dspStart);

        void Start()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            if (chart != null) audioSource.clip = chart.audioClip;
            input.OnLanePressed += HandleLane;
            input.OnLaneReleased += HandleLaneReleased;
        }

        void OnDestroy()
        {
            if (input == null) return;
            input.OnLanePressed -= HandleLane;
            input.OnLaneReleased -= HandleLaneReleased;
        }

        void Update()
        {
            if (EnterPressed())
            {
                if (recording) StopRecording();
                else StartRecording();
            }

            if (recording && BackspacePressed() && recorded.Count > 0)
            {
                NoteData last = recorded[recorded.Count - 1];
                recorded.RemoveAt(recorded.Count - 1);
                for (int l = 0; l < pressedNote.Length; l++)
                    if (pressedNote[l] == last) pressedNote[l] = null;
            }

            if (recording && chart.audioClip != null && SongTime > chart.audioClip.length + 0.5f)
                StopRecording();
        }

        void StartRecording()
        {
            if (chart == null || chart.audioClip == null)
            {
                status = "Chart chua co audioClip!";
                Debug.LogError("ChartRecorder: ChartData chưa gán audioClip.");
                return;
            }

            recorded.Clear();
            System.Array.Clear(pressedNote, 0, pressedNote.Length);
            audioSource.clip = chart.audioClip;
            dspStart = AudioSettings.dspTime + leadInSeconds;
            audioSource.PlayScheduled(dspStart);
            recording = true;
        }

        void StopRecording()
        {
            for (int lane = 0; lane < pressedNote.Length; lane++)
                if (pressedNote[lane] != null) FinishHold(lane, SongTime - latencyCompensation);

            audioSource.Stop();
            recording = false;

            if (clearExistingNotes) chart.notes.Clear();
            chart.notes.AddRange(recorded);
            chart.notes.Sort((a, b) => a.beat.CompareTo(b.beat));

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(chart);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
            status = "Da luu " + recorded.Count + " note vao chart. Enter de thu lai.";
            Debug.Log("ChartRecorder: đã lưu " + recorded.Count + " note vào " + chart.name);
        }

        void HandleLane(int lane)
        {
            if (!recording || lane >= pressedNote.Length) return;

            float t = SongTime - latencyCompensation;
            float beat = (t - chart.firstNoteOffset) / chart.SecondsPerBeat;
            if (snapDivision > 0) beat = Mathf.Round(beat * snapDivision) / snapDivision;

            foreach (var n in recorded)
                if (n.lane == lane && Mathf.Approximately(n.beat, beat)) return;

            var note = new NoteData(beat, lane);
            recorded.Add(note);
            pressedNote[lane] = note;
            pressedTime[lane] = t;
        }

        void HandleLaneReleased(int lane)
        {
            if (!recording || lane >= pressedNote.Length) return;
            FinishHold(lane, SongTime - latencyCompensation);
        }

        void FinishHold(int lane, float releaseTime)
        {
            NoteData note = pressedNote[lane];
            pressedNote[lane] = null;
            if (note == null) return;

            float heldBeats = (releaseTime - pressedTime[lane]) / chart.SecondsPerBeat;
            if (heldBeats < holdThresholdBeats) return;

            if (snapDivision > 0) heldBeats = Mathf.Round(heldBeats * snapDivision) / snapDivision;
            if (heldBeats <= 0f) return;

            note.type = NoteType.Hold;
            note.holdBeats = heldBeats;
        }

        bool EnterPressed()
#if ENABLE_INPUT_SYSTEM
            => Keyboard.current != null &&
               (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame);
#else
            => Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif

        bool BackspacePressed()
#if ENABLE_INPUT_SYSTEM
            => Keyboard.current != null && Keyboard.current.backspaceKey.wasPressedThisFrame;
#else
            => Input.GetKeyDown(KeyCode.Backspace);
#endif

        void OnGUI()
        {
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label);
                style.normal.textColor = Color.white;
            }
            style.fontSize = Mathf.Max(14, Screen.height / 30);

            string line = status;
            if (recording)
            {
                float beat = chart != null ? (SongTime - chart.firstNoteOffset) / chart.SecondsPerBeat : 0f;
                line = "REC  " + SongTime.ToString("0.00") + "s  |  beat " + beat.ToString("0.0") +
                       "  |  note: " + recorded.Count;
            }

            GUI.Label(new Rect(20, 20, Screen.width - 40, Screen.height / 2f),
                      line + "\nEnter: bat dau / dung & luu    Backspace: xoa note cuoi    D F J K: dat note (giu de tao hold)", style);
        }
    }
}