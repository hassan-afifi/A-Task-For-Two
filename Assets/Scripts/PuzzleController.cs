using System;
using System.Collections;
using System.Globalization;
using TMPro;
using Unity.Netcode;
using UnityEngine;
[RequireComponent(typeof(NetworkObject))]

// Generates, syncs, and validates the two-player symbol puzzle.
public class PuzzleController : NetworkBehaviour
{
    [Serializable]
    private struct PuzzleDisplay
    {
        // Shows the target sum text for this puzzle side.
        public TMP_Text sumText;

        // Holds symbol objects for the first known value.
        public Transform firstKnownSymbols;

        // Holds symbol objects for the second known value.
        public Transform secondKnownSymbols;
    }

    private struct PuzzleEquation : IEquatable<PuzzleEquation>
    {
        // Stores the hidden symbol value for this equation.
        public int missing;

        // Stores the first visible symbol value.
        public int firstKnown;

        // Stores the second visible symbol value.
        public int secondKnown;

        // Stores the target sum of all three values.
        public int sum;

        // Serializes equation values for Netcode synchronization.
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref missing);
            serializer.SerializeValue(ref firstKnown);
            serializer.SerializeValue(ref secondKnown);
            serializer.SerializeValue(ref sum);
        }

        // Compares two equations by value.
        public bool Equals(PuzzleEquation other)
        {
            return missing == other.missing && firstKnown == other.firstKnown && secondKnown == other.secondKnown && sum == other.sum;
        }
    }

    private struct NetworkPuzzleState : INetworkSerializable, IEquatable<NetworkPuzzleState>
    {
        // Stores puzzle data for display 1.
        public PuzzleEquation equation1;

        // Stores puzzle data for display 2.
        public PuzzleEquation equation2;

        // Stores the required value for die 1.
        public int target1;

        // Stores the required value for die 2.
        public int target2;

        // Stores whether the puzzle is solved.
        public bool solved;

        // Stores the state version used to detect regenerations.
        public int version;

        // Serializes the full puzzle state for Netcode synchronization.
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            equation1.NetworkSerialize(serializer);
            equation2.NetworkSerialize(serializer);
            serializer.SerializeValue(ref target1);
            serializer.SerializeValue(ref target2);
            serializer.SerializeValue(ref solved);
            serializer.SerializeValue(ref version);
        }

        // Compares two network states by value.
        public bool Equals(NetworkPuzzleState other)
        {
            return equation1.Equals(other.equation1) && equation2.Equals(other.equation2) && target1 == other.target1 && target2 == other.target2 && solved == other.solved && version == other.version;
        }
    }

    private const int SymbolCount = 10;
    private const float BarsRaiseDistance = 4.82f;
    private const float BarsRaiseDuration = 1.28f;
    private const float SolveHoldDuration = 0.82f;
    [SerializeField] private PuzzleDisplay display1;
    [SerializeField] private PuzzleDisplay display2;
    [SerializeField] private DieClickAnimate die1;
    [SerializeField] private DieClickAnimate die2;
    [SerializeField] private Transform bars;
    [SerializeField] private AudioSource barsAudioSource;
    [SerializeField] private AudioClip barsRaiseClip;
    private readonly NetworkVariable<NetworkPuzzleState> netState = new NetworkVariable<NetworkPuzzleState>(new NetworkPuzzleState { solved = false, version = 0 }, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private int target1;
    private int target2;
    private bool solved;
    private float holdTime;
    private Vector3 barsClosedPos;
    private Coroutine barsRoutine;

    // Validates setup and caches closed bars position.
    void Awake()
    {
        EnsureSetup();
        holdTime = 0f;
        barsClosedPos = bars.localPosition;
    }

    // Generates an offline puzzle when not network-spawned.
    void Start()
    {
        if (IsSpawned)
        {
            return;
        }

        GeneratePuzzleLocal();
    }

    // Subscribes and applies puzzle state when the object spawns.
    public override void OnNetworkSpawn()
    {
        netState.OnValueChanged += OnNetworkStateChanged;

        if (IsServer && netState.Value.version == 0)
        {
            GeneratePuzzleServer();
        }
        else
        {
            ApplyPuzzleFromNetwork(netState.Value);
        }

        base.OnNetworkSpawn();
    }

    // Unsubscribes from puzzle state callbacks on despawn.
    public override void OnNetworkDespawn()
    {
        netState.OnValueChanged -= OnNetworkStateChanged;
        base.OnNetworkDespawn();
    }

    // Advances solve hold timing and commits solved state when ready.
    void Update()
    {
        if (!IsSpawned)
        {
            CheckSolvedOffline();
            return;
        }

        if (!IsServer || solved)
        {
            return;
        }

        if (!HoldOk(Time.deltaTime))
        {
            return;
        }

        // Commit solved only after both dice stayed correct for the hold duration.
        ResetHold();
        die1.SetLocked(true);
        die2.SetLocked(true);
        NetworkPuzzleState state = netState.Value;
        state.solved = true;
        netState.Value = state;
    }

    // Regenerates a new puzzle round.
    public void Regenerate()
    {
        if (!IsSpawned)
        {
            GeneratePuzzleLocal();
            return;
        }

        if (IsServer)
        {
            GeneratePuzzleServer();
            return;
        }

        RequestRegenerateServerRpc();
    }

    // Creates and applies a new offline puzzle state.
    void GeneratePuzzleLocal()
    {
        solved = false;
        ResetHold();
        die1.SetLocked(false);
        die2.SetLocked(false);
        ResetBars();
        PuzzleEquation equation1 = CreateEquation();
        PuzzleEquation equation2 = CreateEquation();
        ApplyDisplay(display1, equation1);
        ApplyDisplay(display2, equation2);
        target1 = equation2.missing;
        target2 = equation1.missing;
    }

    // Creates and publishes a new authoritative network puzzle state.
    void GeneratePuzzleServer()
    {
        // Each side receives its own equation and solves for the other side's missing value.
        PuzzleEquation equation1 = CreateEquation();
        PuzzleEquation equation2 = CreateEquation();
        NetworkPuzzleState state = netState.Value;
        state.equation1 = equation1;
        state.equation2 = equation2;
        state.target1 = equation2.missing;
        state.target2 = equation1.missing;
        state.solved = false;
        state.version = state.version + 1;
        netState.Value = state;
        ResetHold();
        die1.SetLocked(false);
        die2.SetLocked(false);
        ApplyPuzzleFromNetwork(state);
    }

    // Applies display/target/solve state from a network payload.
    void ApplyPuzzleFromNetwork(NetworkPuzzleState state)
    {
        ApplyDisplay(display1, state.equation1);
        ApplyDisplay(display2, state.equation2);
        target1 = state.target1;
        target2 = state.target2;
        ApplySolvedState(state.solved);
    }

    // Checks offline solve state using the hold timer.
    void CheckSolvedOffline()
    {
        if (solved)
        {
            return;
        }

        if (!HoldOk(Time.deltaTime))
        {
            return;
        }

        ResetHold();
        solved = true;
        die1.SetLocked(true);
        die2.SetLocked(true);
        StartRaiseBars();
    }

    // Applies incoming network state changes.
    void OnNetworkStateChanged(NetworkPuzzleState _, NetworkPuzzleState currentValue)
    {
        ApplyPuzzleFromNetwork(currentValue);
    }

    // Applies solved/unsolved behavior and bar movement state.
    void ApplySolvedState(bool isSolved)
    {
        solved = isSolved;
        ResetHold();

        if (solved)
        {
            StartRaiseBars();
            return;
        }

        ResetBars();
    }

    // Starts raising bars once if not already running.
    void StartRaiseBars()
    {
        if (barsRoutine != null)
        {
            return;
        }

        barsRoutine = StartCoroutine(RaiseBars());
    }

    // Stops active bar animation and snaps to closed position.
    void ResetBars()
    {
        if (barsRoutine != null)
        {
            StopCoroutine(barsRoutine);
            barsRoutine = null;
        }

        bars.localPosition = barsClosedPos;
    }

    // Handles regenerate requests from clients on the server.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void RequestRegenerateServerRpc()
    {
        GeneratePuzzleServer();
    }

    // Plays bars-raise sound from bars world position.
    void PlayBarsRaiseSfx()
    {
        barsAudioSource.transform.position = bars.position;
        barsAudioSource.PlayOneShot(barsRaiseClip, 1f);
    }

    // Generates one puzzle equation with random values.
    PuzzleEquation CreateEquation()
    {
        PuzzleEquation equation = new PuzzleEquation
        {
            missing = UnityEngine.Random.Range(0, SymbolCount),
            firstKnown = UnityEngine.Random.Range(0, SymbolCount),
            secondKnown = UnityEngine.Random.Range(0, SymbolCount)
        };

        equation.sum = equation.missing + equation.firstKnown + equation.secondKnown;
        return equation;
    }

    // Applies one equation to its visual display slots.
    void ApplyDisplay(PuzzleDisplay display, PuzzleEquation equation)
    {
        display.sumText.text = equation.sum.ToString(CultureInfo.InvariantCulture);
        SetSymbol(display.firstKnownSymbols, equation.firstKnown);
        SetSymbol(display.secondKnownSymbols, equation.secondKnown);
    }

    // Enables exactly one symbol child by index.
    void SetSymbol(Transform symbolsRoot, int symbolIndex)
    {
        for (int i = 0; i < symbolsRoot.childCount; i++)
        {
            symbolsRoot.GetChild(i).gameObject.SetActive(i == symbolIndex);
        }
    }

    // Animates bars from closed to raised position.
    IEnumerator RaiseBars()
    {
        PlayBarsRaiseSfx();
        Vector3 start = bars.localPosition;
        Vector3 target = barsClosedPos + Vector3.up * BarsRaiseDistance;
        float elapsed = 0f;

        while (elapsed < BarsRaiseDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / BarsRaiseDuration);
            // Smoothstep easing keeps bars start/end motion less abrupt.
            float eased = alpha * alpha * (3f - 2f * alpha);
            bars.localPosition = Vector3.LerpUnclamped(start, target, eased);
            yield return null;
        }

        bars.localPosition = target;
        barsRoutine = null;
    }

    // Returns whether both dice match targets and are not rolling.
    bool IsCorrect()
    {
        return !die1.IsRolling && !die2.IsRolling && die1.CurrentFace == target1 && die2.CurrentFace == target2;
    }

    // Advances hold timer only while current answer remains correct.
    bool HoldOk(float deltaTime)
    {
        if (!IsCorrect())
        {
            // Any wrong or still-rolling state resets the hold timer immediately.
            ResetHold();
            return false;
        }

        holdTime += Mathf.Max(0f, deltaTime);
        return holdTime >= SolveHoldDuration;
    }

    // Resets solve hold timer state.
    void ResetHold()
    {
        holdTime = 0f;
    }

    // Validates required puzzle references.
    void EnsureSetup()
    {
        EnsureDisplay(display1, nameof(display1));
        EnsureDisplay(display2, nameof(display2));

        if (die1 == null)
        {
            throw new InvalidOperationException("PuzzleController setup failed: die1 reference is missing.");
        }

        if (die2 == null)
        {
            throw new InvalidOperationException("PuzzleController setup failed: die2 reference is missing.");
        }

        if (bars == null)
        {
            throw new InvalidOperationException("PuzzleController setup failed: bars reference is missing.");
        }

        if (barsAudioSource == null)
        {
            throw new InvalidOperationException("PuzzleController setup failed: barsAudioSource reference is missing.");
        }

        if (barsRaiseClip == null)
        {
            throw new InvalidOperationException("PuzzleController setup failed: barsRaiseClip reference is missing.");
        }
    }

    // Validates one puzzle display reference block.
    void EnsureDisplay(PuzzleDisplay display, string displayName)
    {
        if (display.sumText == null)
        {
            throw new InvalidOperationException($"PuzzleController setup failed: {displayName}.sumText reference is missing.");
        }

        if (display.firstKnownSymbols == null)
        {
            throw new InvalidOperationException($"PuzzleController setup failed: {displayName}.firstKnownSymbols reference is missing.");
        }

        if (display.secondKnownSymbols == null)
        {
            throw new InvalidOperationException($"PuzzleController setup failed: {displayName}.secondKnownSymbols reference is missing.");
        }

        if (display.firstKnownSymbols.childCount < SymbolCount)
        {
            throw new InvalidOperationException($"PuzzleController setup failed: {displayName}.firstKnownSymbols must have at least 10 children.");
        }

        if (display.secondKnownSymbols.childCount < SymbolCount)
        {
            throw new InvalidOperationException($"PuzzleController setup failed: {displayName}.secondKnownSymbols must have at least 10 children.");
        }
    }
}
