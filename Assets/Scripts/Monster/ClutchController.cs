using UnityEngine;
using System.Collections;

public class ClutchController : MonoBehaviour
{
    [Header("Dependencies")]
    public MonsterDirector monsterDirector;
    public WinchController winchController;

    [Header("Clutch Tuning (Base Mechanics)")]
    public float clutchCutoffAngle = -133.3f;
    public float closedAngle = -90f;

    [Header("Clutch Tuning (RNG Modifiers)")]
    public float minAdrenalineFumble = -15f;
    public float maxAdrenalineFumble = 5f;
    public float minMonsterSurge = 0.8f;
    public float maxMonsterSurge = 1.3f;

    [Header("Physical Struggle Settings")]
    public float winCloseSpeed = 15f;
    public float minLoseResistTime = 2.0f;
    public float maxLoseResistTime = 4.0f;
    public float loseOpenSpeed = 35f;
    public float jitterAmount = 1.8f;
    public float jitterSpeed = 35f;

    [Header("The 3-Strike Penalty")]
    public int maxMistakes = 3;
    public float penaltyJerkAngle = 6f;
    public float inputForgivenessBuffer = 0.25f;
    public float recurringPenaltyInterval = 1.0f;

    [Header("Audio (Struggle Sounds)")]
    public AudioClip winchLoopStruggle;
    public AudioClip penaltyJerk;
    public AudioClip doorRipOpen;

    [Header("Live Debug")]
    public float lastReactionScore;
    public float lastAngleScore;
    public float lastPlayerTotal;
    public float lastMonsterTotal;
    public bool lastResultWon;

    public void EvaluateStruggle(float playerReactionTime, float maxDangerTime, float currentDoorAngle, DifficultyProfile difficulty)
    {
        Debug.Log("<color=cyan>--- CLUTCH STRUGGLE INITIATED ---</color>");

        float baseReactionScore = 0f;
        if (playerReactionTime >= 0f) baseReactionScore = Mathf.Lerp(100f, 0f, (playerReactionTime / maxDangerTime));

        float adrenalineFumble = Random.Range(minAdrenalineFumble, maxAdrenalineFumble);
        lastReactionScore = Mathf.Clamp(baseReactionScore + adrenalineFumble, 0f, 100f);

        lastAngleScore = Mathf.InverseLerp(clutchCutoffAngle, closedAngle, currentDoorAngle) * 100f;
        lastPlayerTotal = lastReactionScore + lastAngleScore;

        float baseRoll = difficulty.baseMonsterClutchStrength + Random.Range(-difficulty.clutchStrengthVariance, difficulty.clutchStrengthVariance);
        float surgeMultiplier = Random.Range(minMonsterSurge, maxMonsterSurge);
        lastMonsterTotal = baseRoll * surgeMultiplier;

        lastResultWon = lastPlayerTotal >= lastMonsterTotal;

        StartCoroutine(ActiveStruggleRoutine(lastResultWon, currentDoorAngle));
    }

    private IEnumerator ActiveStruggleRoutine(bool playerWinning, float startAngle)
    {
        if (winchController != null) winchController.isStruggling = true;

        // --- AUDIO HOOK: Hijack the normal ratchet with the struggling metal loop ---
        if (winchController != null && winchController.loopSource != null && winchLoopStruggle != null)
        {
            winchController.loopSource.clip = winchLoopStruggle;
            winchController.loopSource.pitch = 1f; // Lock pitch so the squeal sounds natural
            winchController.loopSource.Play();
        }

        float struggleBaseAngle = startAngle;
        int currentMistakes = 0;
        float timeSinceHeld = 0f;
        float nextStrikeThreshold = inputForgivenessBuffer;
        bool monsterBreached = false;
        float falseHopeDuration = Random.Range(minLoseResistTime, maxLoseResistTime);
        float falseHopeTimer = 0f;

        while (true)
        {
            if (winchController != null && winchController.IsBeingHeld)
            {
                timeSinceHeld = 0f;
                nextStrikeThreshold = inputForgivenessBuffer;
            }
            else
            {
                timeSinceHeld += Time.deltaTime;
            }

            bool isHolding = timeSinceHeld <= inputForgivenessBuffer;

            if (monsterBreached)
            {
                // --- AUDIO HOOK: Silence the struggle loop instantly, and trigger the explosive Rip Open! ---
                if (winchController != null && winchController.impactSource != null && doorRipOpen != null)
                {
                    winchController.loopSource.Stop();
                    winchController.impactSource.PlayOneShot(doorRipOpen);
                }

                float ripSpeed = (winchController != null) ? winchController.openSlamSpeed * 1.5f : 300f;
                while (Mathf.Abs(struggleBaseAngle - winchController.openAngle) > 0.5f)
                {
                    struggleBaseAngle = Mathf.MoveTowards(struggleBaseAngle, winchController.openAngle, ripSpeed * Time.deltaTime);
                    if (winchController != null) winchController.SetStruggleAngle(struggleBaseAngle);
                    yield return null;
                }

                if (winchController != null) winchController.isStruggling = false;
                if (monsterDirector != null && monsterDirector.jumpscareController != null)
                {
                    monsterDirector.jumpscareController.ExecuteJumpscare(MonsterDirector.StrikeType.PointBlank, monsterDirector.monsterTransform, monsterDirector.rampEntryTarget);
                }
                break;
            }
            else if (playerWinning)
            {
                if (isHolding)
                {
                    struggleBaseAngle = Mathf.MoveTowards(struggleBaseAngle, closedAngle, winCloseSpeed * Time.deltaTime);
                }
                else
                {
                    if (timeSinceHeld >= nextStrikeThreshold)
                    {
                        currentMistakes++;
                        nextStrikeThreshold += recurringPenaltyInterval;

                        struggleBaseAngle -= penaltyJerkAngle;
                        Debug.Log($"<color=orange>CLUTCH PENALTY! Player dropped the winch! Strike {currentMistakes}/{maxMistakes}</color>");

                        // --- AUDIO HOOK: Play the heavy chain-slip penalty jerk! ---
                        if (winchController != null && winchController.impactSource != null && penaltyJerk != null)
                        {
                            winchController.impactSource.PlayOneShot(penaltyJerk);
                        }

                        if (currentMistakes >= maxMistakes) monsterBreached = true;
                    }
                }

                if (struggleBaseAngle >= closedAngle - 2f)
                {
                    if (winchController != null) winchController.ForceSlamShut();
                    if (monsterDirector != null) monsterDirector.TransitionToState(MonsterDirector.EncounterState.Siege);
                    break;
                }
            }
            else
            {
                if (isHolding)
                {
                    falseHopeTimer += Time.deltaTime;
                    if (falseHopeTimer >= falseHopeDuration) monsterBreached = true;
                }
                else
                {
                    struggleBaseAngle = Mathf.MoveTowards(struggleBaseAngle, winchController.openAngle, loseOpenSpeed * Time.deltaTime);
                    if (struggleBaseAngle <= startAngle - 35f) monsterBreached = true;
                }
            }

            if (winchController != null)
            {
                float noise = Mathf.PerlinNoise(Time.time * jitterSpeed, 0f) - 0.5f;
                float jitter = noise * 2f * jitterAmount;
                winchController.SetStruggleAngle(struggleBaseAngle + jitter);
            }

            yield return null;
        }
    }
}