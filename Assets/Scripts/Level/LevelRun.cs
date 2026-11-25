using System;
using System.Collections;
using System.Collections.Generic;
using Flicker.Generation;
using TMPro;
using UnityEngine;

// Owns the run: the level number, the fuel count and the trip to the next block.
// Collect every canister, walk back to the car and press Interact: the player gets in, the car drives off,
// the screen fades to a "LEVEL N" card, the next block is built behind it and the car pulls up to the new
// curb. Health and ammo carry over; only a restart (N, a typed seed, Retry) resets them.
// Level seeds come from the run seed, so a typed seed replays the whole run.
public class LevelRun : MonoBehaviour
{
    [SerializeField] private BlockBuilder builder;
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private ScreenFader fader;

    [Header("HUD")]
    [SerializeField] private TMP_Text fuelLabel;
    [SerializeField] private TMP_Text levelLabel;
    [SerializeField] private TMP_Text promptLabel;

    [Header("Cutscene")]
    [SerializeField] private float driveOffDuration = 2f;
    [SerializeField] private float fadeDuration = 0.6f;
    [SerializeField] private float cardHold = 0.9f;
    [SerializeField] private float arriveDuration = 2f;

    [Header("Backup")]
    [Tooltip("Seconds of play on a level before unfound canisters glow through the dark and get edge arrows.")]
    [SerializeField] private float backupDelay = 90f;

    public int Level { get; private set; } = 1;
    public int Fuel { get; private set; }
    public int FuelNeeded { get; private set; }
    public int RunSeed { get; private set; }
    // True while the cutscene runs; restarts and interaction are ignored.
    public bool Busy { get; private set; }
    // Seconds the player has had control on this level (paused during the cutscene and after death).
    public float LevelTime { get; private set; }
    public float BackupDelay => backupDelay;
    // The backup has fired on this level: unfound canisters are beacons and the compass shows them.
    public bool BackupActive { get; private set; }
    public event Action BackupStarted;

    private ExitCar car;
    private Rigidbody2D playerBody;
    private readonly List<SpriteRenderer> hiddenSprites = new List<SpriteRenderer>();
    private bool inCar;
    private bool hasRunSeed;
    private bool arrivalPending = true; // the first block of the scene opens with the car pulling up

    void Awake()
    {
        playerBody = playerHealth.GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        builder.Generated += HandleGenerated;
        FuelCanister.PickedUp += HandlePickedUp;
        input.InteractPressed += HandleInteract;
        playerHealth.OnDied += RefreshPrompt;
        playerHealth.OnRevived += RefreshPrompt;
    }

    void OnDisable()
    {
        builder.Generated -= HandleGenerated;
        FuelCanister.PickedUp -= HandlePickedUp;
        input.InteractPressed -= HandleInteract;
        playerHealth.OnDied -= RefreshPrompt;
        playerHealth.OnRevived -= RefreshPrompt;
        BindCar(null);
        FuelCanister.RevealPaused = false; // static: don't carry a paused reveal into the next scene
    }

    void Start()
    {
        // The first block is still being built: start behind the black card, player in the car.
        if (builder.Current == null && arrivalPending)
        {
            Busy = true;
            EnterCar();
            fader.SetAlpha(1f);
            fader.ShowCard(CardText(Level));
        }
        RefreshHud();
    }

    // Backup timer: counts only while the player is in control; fires once per level.
    void Update()
    {
        if (Busy || BackupActive || playerHealth.IsDead) return;
        LevelTime += Time.deltaTime;
        if (LevelTime >= backupDelay) StartBackup();
    }

    private void StartBackup()
    {
        BackupActive = true;
        foreach (var canister in FuelCanister.All)
            if (canister != null && !canister.Found) canister.MakeBeacon();
        BackupStarted?.Invoke();
    }

    // Riding: the hidden player sits in the car, facing along it, so its flashlight works as headlights
    // and the camera, zombies and lights all follow the car.
    void LateUpdate()
    {
        if (!inCar || car == null) return;
        playerHealth.transform.SetPositionAndRotation(car.transform.position, Quaternion.identity);
    }

    // A fresh run from level 1 with full health, built instantly (debug restart: N or a typed seed).
    public void StartRun(int seed)
    {
        StopAllCoroutines();
        if (inCar) LeaveCar();
        fader.SetAlpha(0f);
        fader.ShowCard(null);
        Busy = false;
        arrivalPending = false;
        Level = 1;
        RunSeed = seed;
        hasRunSeed = true;
        builder.Generate(seed);
    }

    public void StartRandomRun() => StartRun(Rng.Derive(Environment.TickCount, 1));

    private void HandleGenerated(BlockLayout layout)
    {
        if (!hasRunSeed)
        {
            RunSeed = layout.Seed;
            hasRunSeed = true;
        }
        Fuel = 0;
        FuelNeeded = layout.Canisters.Count;
        LevelTime = 0f;
        BackupActive = false;
        BindCar(builder.ExitCar);
        RefreshHud();

        if (arrivalPending)
        {
            arrivalPending = false;
            StartCoroutine(Arrival());
        }
    }

    private void HandlePickedUp()
    {
        Fuel = Mathf.Min(Fuel + 1, FuelNeeded);
        RefreshHud();
    }

    private void HandleInteract()
    {
        if (Busy || car == null || !car.PlayerInRange || playerHealth.IsDead || Fuel < FuelNeeded) return;
        StartCoroutine(DriveToNextLevel());
    }

    private IEnumerator DriveToNextLevel()
    {
        Busy = true;
        RefreshPrompt();
        EnterCar();

        // Fade out during the last part of the drive.
        var drive = StartCoroutine(car.DriveOff(driveOffDuration));
        yield return new WaitForSeconds(Mathf.Max(0f, driveOffDuration - fadeDuration));
        yield return fader.FadeTo(1f, fadeDuration);
        yield return drive;

        Level++;
        fader.ShowCard(CardText(Level));
        arrivalPending = true;
        builder.Generate(Rng.Derive(RunSeed, Level - 1), revivePlayer: false);
        // HandleGenerated starts the arrival once the block is built.
    }

    private IEnumerator Arrival()
    {
        Busy = true;
        RefreshPrompt();
        EnterCar();
        car.PrepareArrival();
        cameraController.Follow(car.transform);

        yield return new WaitForSeconds(cardHold);
        StartCoroutine(fader.FadeTo(0f, fadeDuration));
        yield return car.Arrive(arriveDuration);
        fader.ShowCard(null);

        LeaveCar();
        Busy = false;
        RefreshPrompt();
    }

    // Player gets in: hidden, out of physics, no input, no damage; the camera follows the car.
    private void EnterCar()
    {
        if (inCar) return;
        inCar = true;
        FuelCanister.RevealPaused = true; // headlights shouldn't find fuel during the cutscene
        input.enabled = false;
        playerHealth.Invulnerable = true;
        playerBody.linearVelocity = Vector2.zero;
        playerBody.simulated = false;

        hiddenSprites.Clear();
        foreach (var sprite in playerHealth.GetComponentsInChildren<SpriteRenderer>())
            if (sprite.enabled)
            {
                sprite.enabled = false;
                hiddenSprites.Add(sprite);
            }

        if (car != null) cameraController.Follow(car.transform);
    }

    // Player steps out onto the car spot.
    private void LeaveCar()
    {
        if (!inCar) return;
        inCar = false;
        FuelCanister.RevealPaused = false;

        Vector2 spawn = builder.PlayerSpawn;
        playerHealth.transform.SetPositionAndRotation(spawn, Quaternion.Euler(0f, 0f, 90f));
        playerBody.position = spawn;
        playerBody.rotation = 90f;
        playerBody.simulated = true;

        foreach (var sprite in hiddenSprites)
            if (sprite != null) sprite.enabled = true;
        hiddenSprites.Clear();

        playerHealth.Invulnerable = false;
        input.enabled = true;
        cameraController.Follow(null);
    }

    private void BindCar(ExitCar next)
    {
        if (car != null) car.RangeChanged -= RefreshPrompt;
        car = next;
        if (car != null) car.RangeChanged += RefreshPrompt;
        RefreshPrompt();
    }

    private void RefreshHud()
    {
        if (fuelLabel != null) fuelLabel.text = $"Fuel {Fuel}/{FuelNeeded}";
        if (levelLabel != null) levelLabel.text = $"Level {Level}";
        RefreshPrompt();
    }

    private void RefreshPrompt()
    {
        if (promptLabel == null) return;
        bool show = car != null && car.PlayerInRange && !Busy && !playerHealth.IsDead;
        promptLabel.gameObject.SetActive(show);
        if (show)
            promptLabel.text = Fuel >= FuelNeeded ? "Press E to drive" : $"Need fuel ({Fuel}/{FuelNeeded})";
    }

    private static string CardText(int level) => $"LEVEL {level}";
}
