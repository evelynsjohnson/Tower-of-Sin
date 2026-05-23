using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;

public class WrathAI : MonoBehaviour
{
    public enum BossPhase
    {
        Dormant,
        Intro,
        Phase1,
        Phase2Transition,
        Phase2,
        Dead
    }

    private BossArenaController arenaController;

    [Header("Core References")]
    [SerializeField] private Animator humanoidAnimator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform player;
    [SerializeField] private GameObject humanoidModelRoot;

    [Header("Phase 2 - Dragon References")]
    [SerializeField] private GameObject wrathDragonPrefab;
    [SerializeField] private Transform dragonHeadBone;
    private GameObject spawnedDragon;
    private Animator dragonAnimator;

    [Header("Detection / Movement")]
    [SerializeField] private float wakeRange = 15f;
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float faceSpeed = 15f;

    [Header("Health / Damage")]
    [SerializeField] private float baseMaxHP = 1000f;
    [SerializeField] private float baseAttackDamage = 25f;

    [Header("Attack Settings")]
    [SerializeField] private float attackCooldownPhase1 = 4.0f;
    [SerializeField] private float attackCooldownPhase2 = 5.0f;
    [SerializeField] private float telegraphDuration = 1.5f;

    [Header("UI / Arena")]
    [SerializeField] private Color bossLightColor = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private float lightIntensityMultiplier = 1.25f;
    [SerializeField] private Image bossHealthBarFill;
    [SerializeField] private TMP_Text bossHealthText;
    [SerializeField] private GameObject bossHealthUIRoot;

    [Header("Prefabs - Attacks")]
    [SerializeField] private GameObject conalCrystalAtkPrefab;
    [SerializeField] private GameObject larCirCrystalAtkPrefab;
    [SerializeField] private GameObject smaCirCrystalAtkPrefab;
    [SerializeField] private GameObject redBurningGroundPrefab;
    [SerializeField] private GameObject redBurningVertGroundPrefab;
    [SerializeField] private GameObject meteorPrefab;
    [SerializeField] private GameObject ballOrbPrefab;
    [SerializeField] private GameObject dragonFirePrefab;

    [Header("Telegraph Visuals")]
    [SerializeField] private float telegraphLineWidth = 0.15f;
    [SerializeField] private float telegraphYOffset = 0.05f;
    [SerializeField] private int coneArcSegments = 20;
    [SerializeField] private Color telegraphFillColor = new Color(1f, 0.2f, 0.05f, 0.22f);
    [SerializeField] private Color telegraphOutlineColor = new Color(0.45f, 0.05f, 0.02f, 0.95f);

    [Header("Audio")]
    [SerializeField] private AudioMixerGroup narrationMixer;
    [SerializeField] private AudioMixerGroup sfxMixer;
    [SerializeField] private AudioClip walkingClip;
    [SerializeField] private AudioClip phase1MusicClip;
    [SerializeField] private AudioClip phase2MusicClip;
    [SerializeField] private AudioClip introClip;
    [SerializeField] private AudioClip phase1To2Clip;
    [SerializeField] private AudioClip[] humanoidVoiceClips;
    [SerializeField] private AudioClip[] dragonVoiceClips;
    [SerializeField] private AudioClip dragonDeathClip;
    [SerializeField] private AudioClip melee1Audio;
    [SerializeField] private AudioClip melee2Audio;
    [SerializeField] private AudioClip crashDownAudio;
    [SerializeField] private AudioClip range1Audio;
    [SerializeField] private AudioClip leapBackAudio;
    [SerializeField] private AudioClip riseUpAudio;
    [SerializeField] private AudioClip dragonAtk01Audio;
    [SerializeField] private AudioClip dragonAtk02Audio;
    [SerializeField] private AudioClip dragonTailAudio;
    [SerializeField] private AudioClip dragonFireAudio;

    private AudioSource dialogueSource;
    private AudioSource sfxSource;
    private AudioSource walkSource;
    private AudioSource musicSourceP1;
    private AudioSource musicSourceP2;

    private BossPhase currentPhase = BossPhase.Dormant;
    private BossPhase requestedPhase = BossPhase.Dormant;

    private float maxHP;
    private float currentHP;
    private float scaledAttackDamage;
    private int currentFloor = 5;

    private bool hasSpawned = false;
    private bool isInvulnerable = false;
    private bool isDead = false;
    private bool isBusy = false;
    private bool isTransitioning = false;

    private int voiceIndex = 0;
    private int phase1AttackIndex = 0;
    private int phase2AttackIndex = 0;
    private readonly List<GameObject> spawnedTelegraphs = new List<GameObject>();

    // Animator Hashes
    private static readonly int AnimIsWalking = Animator.StringToHash("isWalking");
    private static readonly int AnimIntroFinished = Animator.StringToHash("introFinished");

    // Stella Triggers
    private static readonly int AnimStellaMelee1 = Animator.StringToHash("StellaMelee1");
    private static readonly int AnimStellaMelee2 = Animator.StringToHash("StellaMelee2");
    private static readonly int AnimStellaCrashDown = Animator.StringToHash("StellaCrashDown");
    private static readonly int AnimStellaRange1 = Animator.StringToHash("StellaRange1");
    private static readonly int AnimStellaLeapBack = Animator.StringToHash("StellaLeapBack");
    private static readonly int AnimStellaRiseUp = Animator.StringToHash("StellaRiseUp");
    private static readonly int AnimStellaRiseToFloat = Animator.StringToHash("StellaRiseToFloat");
    private static readonly int AnimStellaAirAtk = Animator.StringToHash("StellaAirAtk");
    private static readonly int AnimStellaRiseToBallFloat = Animator.StringToHash("StellaRiseToBallFloat");

    // Dragon Triggers
    private static readonly int AnimDragonEntry = Animator.StringToHash("Qishilong_down");
    private static readonly int AnimDragonAtk1 = Animator.StringToHash("Qishilong_attack01");
    private static readonly int AnimDragonAtk2 = Animator.StringToHash("Qishilong_attack02");
    private static readonly int AnimDragonTail = Animator.StringToHash("Qishilong_TailSwipe");
    private static readonly int AnimDragonDie = Animator.StringToHash("Qishilong_die");
    private static readonly int AnimDragonConeOfFire = Animator.StringToHash("Qishilong_ConeOfFire");

    #region Public API (Arena Integration)

    public void SetArenaController(BossArenaController controller)
    {
        arenaController = controller;
        if (arenaController != null)
        {
            bossHealthBarFill = arenaController.GetBossHealthBarFill();
            bossHealthText = arenaController.GetBossHealthText();
            bossHealthUIRoot = arenaController.GetBossHealthUIRoot();

            arenaController.OnBossSpawned(bossLightColor, lightIntensityMultiplier, phase1MusicClip, 1f);
        }
        UpdateBossUI();
    }

    public void SetupArenaReferences(Light[] arenaLights, Transform basementDoorLeft, Transform basementDoorRight, AudioSource gateAudioSource, AudioClip largeGateClip, AudioSource backgroundMusicSource, GameObject bossChestPrefab, Transform bossChestSpawnPoint, Image healthBarFill, TMP_Text healthText, GameObject healthUIRoot, float doorMoveDistanceZ, float doorMoveDuration)
    {
        bossHealthBarFill = healthBarFill;
        bossHealthText = healthText;
        bossHealthUIRoot = healthUIRoot;
        UpdateBossUI();
    }

    public void SetFloor(int floor)
    {
        currentFloor = Mathf.Max(5, floor);
        RecalculateScaledStats();
        currentHP = Mathf.Min(currentHP <= 0 ? maxHP : currentHP, maxHP);
        UpdateBossUI();
    }

    private void RecalculateScaledStats()
    {
        int steps = Mathf.Max(0, (currentFloor / 5) - 1);
        maxHP = baseMaxHP * (1f + 0.05f * steps);
        scaledAttackDamage = baseAttackDamage * (1f + 0.10f * steps);
    }

    #endregion

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        AutoFindReferences();
        SetupAudioSources();
    }

    private void Start()
    {
        RecalculateScaledStats();
        currentHP = maxHP;
        UpdateBossUI();

        if (bossHealthUIRoot != null) bossHealthUIRoot.SetActive(true);
        StartCoroutine(BossBrain());
    }

    private void Update()
    {
        if (isDead) return;

        HandleContinuousFacing();
        UpdateAnimationsAndAudio();
        HandlePhaseRequestsByHealth();
    }

    private void AutoFindReferences()
    {
        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }

        if (bossHealthUIRoot == null)
        {
            GameObject uiRoot = GameObject.Find("BossHealth");
            if (uiRoot != null)
            {
                bossHealthUIRoot = uiRoot;
                Transform barTransform = uiRoot.transform.Find("HealthBar");
                if (barTransform != null) bossHealthBarFill = barTransform.GetComponent<Image>();
                Transform textTransform = uiRoot.transform.Find("HealthText");
                if (textTransform != null) bossHealthText = textTransform.GetComponent<TMP_Text>();
            }
        }
    }

    #region AI Logic & Brain

    private IEnumerator BossBrain()
    {
        while (!isDead)
        {
            if (player == null) { yield return null; continue; }

            switch (currentPhase)
            {
                case BossPhase.Dormant:
                    yield return HandleDormant();
                    break;
                case BossPhase.Phase1:
                    yield return HandlePhase1();
                    break;
                case BossPhase.Phase2:
                    yield return HandlePhase2();
                    break;
            }
            yield return null;
        }
    }

    private IEnumerator HandleDormant()
    {
        while (!hasSpawned && !isDead)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= wakeRange)
            {
                currentPhase = BossPhase.Intro;
                isInvulnerable = true;
                isBusy = true;
                hasSpawned = true;

                FacePlayerImmediate();

                musicSourceP1.Play();
                yield return StartCoroutine(PlayDialogue(introClip));

                if (humanoidAnimator != null) humanoidAnimator.SetBool(AnimIntroFinished, true);

                isInvulnerable = false;
                isBusy = false;

                currentPhase = BossPhase.Phase1;
                requestedPhase = BossPhase.Phase1;
                yield break;
            }
            yield return null;
        }
    }

    private IEnumerator HandlePhase1()
    {
        while (currentPhase == BossPhase.Phase1 && !isDead)
        {
            if (TryProcessRequestedPhase()) yield break;

            if (!isBusy)
            {
                float dist = Vector3.Distance(transform.position, player.position);
                bool nextIsRanged = (phase1AttackIndex % 6) == 3 || (phase1AttackIndex % 6) == 4 || (phase1AttackIndex % 6) == 5;
                float effectiveAtkRange = nextIsRanged ? attackRange + 4f : attackRange;

                if (dist > effectiveAtkRange)
                {
                    MoveTowardsPlayer(effectiveAtkRange - 1f);
                }
                else
                {
                    StopMoving();

                    switch (phase1AttackIndex % 6)
                    {
                        case 0: yield return StartCoroutine(Stella_Melee1()); break;
                        case 1: yield return StartCoroutine(Stella_Melee2()); break;
                        case 2: yield return StartCoroutine(Stella_CrashDown()); break;
                        case 3: yield return StartCoroutine(Stella_Range1()); break;
                        case 4: yield return StartCoroutine(Stella_LeapBack()); break;
                        case 5: yield return StartCoroutine(Stella_RiseUp_Ultimate()); break;
                    }

                    phase1AttackIndex++;
                    yield return new WaitForSeconds(attackCooldownPhase1);
                }
            }
            yield return null;
        }
    }

    private IEnumerator HandlePhase2()
    {
        if (!isTransitioning && currentPhase != BossPhase.Phase2Transition)
            yield return StartCoroutine(Phase1ToPhase2Transition());

        while (currentPhase == BossPhase.Phase2 && !isDead)
        {
            if (!isBusy)
            {
                float dist = Vector3.Distance(transform.position, player.position);

                if (dist > attackRange + 2f)
                {
                    MoveTowardsPlayer(attackRange);
                }
                else
                {
                    StopMoving();

                    switch (phase2AttackIndex % 4)
                    {
                        case 0: yield return StartCoroutine(Dragon_Atk01()); break;
                        case 1: yield return StartCoroutine(Dragon_Atk02()); break;
                        case 2: yield return StartCoroutine(Dragon_TailSwipe()); break;
                        case 3: yield return StartCoroutine(Dragon_ConeOfFire()); break;
                    }

                    phase2AttackIndex++;
                    yield return new WaitForSeconds(attackCooldownPhase2);
                }
            }
            yield return null;
        }
    }

    private void HandlePhaseRequestsByHealth()
    {
        if (isDead) return;

        if (currentPhase == BossPhase.Phase1 && currentHP <= 0f)
        {
            currentHP = 1f;
            requestedPhase = BossPhase.Phase2Transition;
        }
    }

    private bool TryProcessRequestedPhase()
    {
        if (requestedPhase == currentPhase) return false;
        if (requestedPhase == BossPhase.Dormant || requestedPhase == BossPhase.Dead) return false;
        if (isBusy) return false;

        currentPhase = requestedPhase;
        return true;
    }

    #endregion

    #region Root Motion Synchronizer

    // Synchronizes the NavMeshAgent position with the Animation displacement
    private void OnAnimatorMove()
    {
        if (isDead) return;

        Animator currentAnim = (currentPhase == BossPhase.Phase1 || currentPhase == BossPhase.Phase2Transition) ? humanoidAnimator : dragonAnimator;

        // Only apply root motion when explicitly busy attacking, otherwise NavMesh handles running
        if (isBusy && currentAnim != null && currentAnim.applyRootMotion)
        {
            Vector3 newPos = transform.position + currentAnim.deltaPosition;

            // Sample nearest navmesh point to avoid her animating through a wall
            if (NavMesh.SamplePosition(newPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                agent.nextPosition = hit.position; // Keep agent snapped to her root body
            }
        }
    }

    #endregion

    #region Phase 1 Attacks (Stella)

    private IEnumerator Stella_Melee1()
    {
        isBusy = true;
        PlayRotatingVoice(humanoidVoiceClips);

        Vector3 atkDir = GetFlatDirectionToPlayer();
        GameObject indicator = SpawnConeTelegraph(transform.position, atkDir, 60f, 6f);

        yield return new WaitForSeconds(telegraphDuration);

        humanoidAnimator.ResetTrigger(AnimStellaMelee1);
        humanoidAnimator.SetTrigger(AnimStellaMelee1);
        PlayAttackAudio(melee1Audio);
        Destroy(indicator);

        //if (conalCrystalAtkPrefab) Instantiate(conalCrystalAtkPrefab, transform.position, transform.rotation);
        if (conalCrystalAtkPrefab) Instantiate(conalCrystalAtkPrefab, transform.position, GetAttackRotation());

        // --- DYNAMIC TIMING ---
        // Wait 2 frames so the Animator reliably switches into the target state
        yield return null; yield return null;

        float animLength = humanoidAnimator.GetCurrentAnimatorStateInfo(0).length;
        float preHitDelay = Mathf.Max(0f, animLength - 0.5f);

        yield return new WaitForSeconds(preHitDelay);

        DamageIfPlayerInsideCone(transform.position, atkDir, 60f, 6f);

        yield return new WaitForSeconds(0.5f); // Wait the remaining duration to finish fully
        isBusy = false;
    }

    private IEnumerator Stella_Melee2()
    {
        isBusy = true;
        Vector3 atkCenter = transform.position + transform.forward * 4f;
        GameObject indicator = SpawnRectangleTelegraph(atkCenter, transform.rotation, 2f, 8f);

        yield return new WaitForSeconds(telegraphDuration);

        humanoidAnimator.ResetTrigger(AnimStellaMelee2);
        humanoidAnimator.SetTrigger(AnimStellaMelee2);
        PlayAttackAudio(melee2Audio);
        Destroy(indicator);

        yield return null; yield return null;
        float animLength = humanoidAnimator.GetCurrentAnimatorStateInfo(0).length;
        float preHitDelay = Mathf.Max(0f, animLength - 0.5f);

        yield return new WaitForSeconds(preHitDelay);

        DamageIfPlayerInsideRectangle(atkCenter, transform.rotation, 2f, 8f);

        yield return new WaitForSeconds(0.5f);
        isBusy = false;
    }

    private IEnumerator Stella_CrashDown()
    {
        isBusy = true;
        PlayRotatingVoice(humanoidVoiceClips);

        GameObject indicator = SpawnCircleTelegraph(transform.position, 5f);

        yield return new WaitForSeconds(telegraphDuration);

        humanoidAnimator.ResetTrigger(AnimStellaCrashDown);
        humanoidAnimator.SetTrigger(AnimStellaCrashDown);
        PlayAttackAudio(crashDownAudio);
        Destroy(indicator);

        yield return null; yield return null;
        float animLength = humanoidAnimator.GetCurrentAnimatorStateInfo(0).length;
        float preHitDelay = Mathf.Max(0f, animLength - 0.5f);

        yield return new WaitForSeconds(preHitDelay);

        if (larCirCrystalAtkPrefab)
        {
            GameObject smash = Instantiate(larCirCrystalAtkPrefab, transform.position, Quaternion.identity);
            if (smash.TryGetComponent(out CrystalSmashArea area)) area.damageToDeal = scaledAttackDamage;
        }

        yield return new WaitForSeconds(0.5f);
        isBusy = false;
    }

    private IEnumerator Stella_Range1()
    {
        isBusy = true;
        humanoidAnimator.ResetTrigger(AnimStellaRange1);
        humanoidAnimator.SetTrigger(AnimStellaRange1);
        PlayAttackAudio(range1Audio);

        yield return null; yield return null;
        float animLength = humanoidAnimator.GetCurrentAnimatorStateInfo(0).length;

        for (int i = 0; i < 3; i++)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 8f + Random.insideUnitSphere * 2f;
            if (meteorPrefab)
            {
                GameObject meteor = Instantiate(meteorPrefab, spawnPos, Quaternion.identity);
                WrathMeteor logic = meteor.AddComponent<WrathMeteor>();
                logic.Setup(player, smaCirCrystalAtkPrefab, redBurningGroundPrefab, scaledAttackDamage);
            }
            yield return new WaitForSeconds(0.5f);
        }

        // Wait remaining time of animation
        float remaining = Mathf.Max(0f, animLength - 1.5f);
        yield return new WaitForSeconds(remaining);
        isBusy = false;
    }

    private IEnumerator Stella_LeapBack()
    {
        isBusy = true;
        Vector3 atkCenter = transform.position + transform.forward * 5f;
        GameObject indicator = SpawnRectangleTelegraph(atkCenter, transform.rotation, 3f, 10f);

        yield return new WaitForSeconds(telegraphDuration);

        humanoidAnimator.ResetTrigger(AnimStellaLeapBack);
        humanoidAnimator.SetTrigger(AnimStellaLeapBack);
        PlayAttackAudio(leapBackAudio);
        Destroy(indicator);

        yield return null; yield return null;
        float animLength = humanoidAnimator.GetCurrentAnimatorStateInfo(0).length;
        float preHitDelay = Mathf.Max(0f, animLength - 0.5f);

        yield return new WaitForSeconds(preHitDelay);

        if (redBurningVertGroundPrefab)
        {
            GameObject burn = Instantiate(redBurningVertGroundPrefab, atkCenter, transform.rotation);
            if (burn.TryGetComponent(out WrathBurnArea area)) area.baseDamageScale = scaledAttackDamage;
        }

        // Because we are using OnAnimatorMove now, we DO NOT warp the agent manually. 
        // The LeapBack animation will physically displace her, and the NavMeshAgent will follow seamlessly!

        yield return new WaitForSeconds(0.5f);
        isBusy = false;
    }

    private IEnumerator Stella_RiseUp_Ultimate()
    {
        isBusy = true;
        isInvulnerable = true;
        GetComponent<Collider>().enabled = false;

        // Animation 1: Rise Up
        PlayAttackAudio(riseUpAudio);
        humanoidAnimator.ResetTrigger(AnimStellaRiseUp);
        humanoidAnimator.SetTrigger(AnimStellaRiseUp);

        yield return null; yield return null;
        yield return new WaitForSeconds(humanoidAnimator.GetCurrentAnimatorStateInfo(0).length);

        // Animation 2: Float Loop
        humanoidAnimator.ResetTrigger(AnimStellaRiseToFloat);
        humanoidAnimator.SetTrigger(AnimStellaRiseToFloat);

        if (NavMesh.SamplePosition(Vector3.zero, out NavMeshHit centerHit, 15f, NavMesh.AllAreas))
        {
            agent.Warp(centerHit.position);
        }
        transform.position += Vector3.up * 5f;
        yield return new WaitForSeconds(1.5f);

        // Animation 3: Air Drop Attack
        humanoidAnimator.ResetTrigger(AnimStellaAirAtk);
        humanoidAnimator.SetTrigger(AnimStellaAirAtk);

        yield return null; yield return null;
        float airAtkLength = humanoidAnimator.GetCurrentAnimatorStateInfo(0).length;

        for (int i = 0; i < 7; i++)
        {
            Vector3 spawnPos = transform.position + Random.insideUnitSphere * 4f;
            if (meteorPrefab)
            {
                GameObject meteor = Instantiate(meteorPrefab, spawnPos, Quaternion.identity);
                WrathMeteor logic = meteor.AddComponent<WrathMeteor>();
                logic.Setup(player, smaCirCrystalAtkPrefab, redBurningGroundPrefab, scaledAttackDamage);
            }
            yield return new WaitForSeconds(airAtkLength / 7f);
        }

        isInvulnerable = false;
        GetComponent<Collider>().enabled = true;

        Vector3 targetPos = player.position + player.forward * 2f;
        if (NavMesh.SamplePosition(targetPos, out NavMeshHit teleportHit, 5f, NavMesh.AllAreas))
            agent.Warp(teleportHit.position);
        else
            agent.Warp(player.position);

        yield return StartCoroutine(Stella_Melee1());
    }

    #endregion

    #region Phase Transition

    public void TakeDamage(float amount)
    {
        TakeDamage(Mathf.RoundToInt(amount));
    }

    public void TakeDamage(int amount)
    {
        if (isDead || isInvulnerable) return;

        float dmg = (float)amount;
        if (currentPhase == BossPhase.Phase2) dmg *= 10f;

        currentHP -= dmg;
        currentHP = Mathf.Max(0f, currentHP);

        Debug.Log($"Wrath took {dmg} damage! Current HP: {currentHP} / {maxHP}");
        UpdateBossUI();

        if (currentPhase == BossPhase.Phase1 && currentHP <= 0f)
        {
            currentHP = 1f;
            requestedPhase = BossPhase.Phase2Transition;
        }
        else if (currentHP <= 0f)
        {
            Die();
        }
    }

    private IEnumerator Phase1ToPhase2Transition()
    {
        currentPhase = BossPhase.Phase2Transition;
        isTransitioning = true;
        isBusy = true;
        isInvulnerable = true;
        StopMoving();

        StartCoroutine(CrossfadeMusic(musicSourceP1, musicSourceP2, 3f));
        yield return StartCoroutine(PlayDialogue(phase1To2Clip));

        if (NavMesh.SamplePosition(Vector3.zero, out NavMeshHit centerHit, 20f, NavMesh.AllAreas))
        {
            agent.Warp(centerHit.position);
        }

        humanoidAnimator.ResetTrigger(AnimStellaRiseToBallFloat);
        humanoidAnimator.SetTrigger(AnimStellaRiseToBallFloat);

        GameObject orb = null;
        if (ballOrbPrefab)
        {
            orb = Instantiate(ballOrbPrefab, transform.position, Quaternion.identity);
            orb.transform.localScale = Vector3.zero;
        }

        float t = 0;
        while (t < 3f)
        {
            t += Time.deltaTime;
            if (orb) orb.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 5f, t / 3f);
            yield return null;
        }

        if (orb) Destroy(orb);
        humanoidModelRoot.SetActive(false);

        if (wrathDragonPrefab)
        {
            spawnedDragon = Instantiate(wrathDragonPrefab, transform.position, transform.rotation, transform);
            dragonAnimator = spawnedDragon.GetComponentInChildren<Animator>();

            dragonAnimator.ResetTrigger(AnimDragonEntry);
            dragonAnimator.SetTrigger(AnimDragonEntry);

            yield return null; yield return null;
            float dropAnimLength = dragonAnimator.GetCurrentAnimatorStateInfo(0).length;

            GameObject dropIndicator = SpawnCircleTelegraph(transform.position, 6f);

            // Wait until 0.5s before drop animation ends
            yield return new WaitForSeconds(Mathf.Max(0f, dropAnimLength - 0.5f));

            PlayAttackAudio(crashDownAudio);
            Destroy(dropIndicator);

            if (larCirCrystalAtkPrefab)
            {
                GameObject smash = Instantiate(larCirCrystalAtkPrefab, transform.position, Quaternion.identity);
                if (smash.TryGetComponent(out CrystalSmashArea area)) area.damageToDeal = scaledAttackDamage;
            }

            yield return new WaitForSeconds(0.5f);
        }

        currentHP = maxHP;
        UpdateBossUI();

        yield return new WaitForSeconds(1.5f);

        isInvulnerable = false;
        isBusy = false;
        isTransitioning = false;
        currentPhase = BossPhase.Phase2;
    }

    #endregion

    #region Phase 2 Attacks (Dragon)

    private IEnumerator Dragon_Atk01()
    {
        isBusy = true;
        PlayRotatingVoice(dragonVoiceClips);

        Vector3 atkCenter = transform.position + transform.forward * 3f;
        GameObject ind = SpawnRectangleTelegraph(atkCenter, transform.rotation, 3f, 4f);

        yield return new WaitForSeconds(telegraphDuration);

        dragonAnimator.ResetTrigger(AnimDragonAtk1);
        dragonAnimator.SetTrigger(AnimDragonAtk1);
        PlayAttackAudio(dragonAtk01Audio);
        Destroy(ind);

        yield return null; yield return null;
        float animLength = dragonAnimator.GetCurrentAnimatorStateInfo(0).length;
        float preHitDelay = Mathf.Max(0f, animLength - 0.5f);

        yield return new WaitForSeconds(preHitDelay);

        DamageIfPlayerInsideRectangle(atkCenter, transform.rotation, 3f, 4f);

        yield return new WaitForSeconds(0.5f);
        isBusy = false;
    }

    private IEnumerator Dragon_Atk02()
    {
        isBusy = true;
        dragonAnimator.ResetTrigger(AnimDragonAtk2);
        dragonAnimator.SetTrigger(AnimDragonAtk2);
        PlayAttackAudio(dragonAtk02Audio);

        yield return null; yield return null;
        float animLength = dragonAnimator.GetCurrentAnimatorStateInfo(0).length;

        float[] scales = { 1f, 1.5f, 2.2f };
        for (int i = 0; i < 3; i++)
        {
            if (larCirCrystalAtkPrefab)
            {
                GameObject atk = Instantiate(larCirCrystalAtkPrefab, transform.position, Quaternion.identity);
                atk.transform.localScale *= scales[i];
                if (atk.TryGetComponent(out CrystalSmashArea area)) area.damageToDeal = scaledAttackDamage;
            }
            yield return new WaitForSeconds(animLength / 3f);
        }

        isBusy = false;
    }

    private IEnumerator Dragon_TailSwipe()
    {
        isBusy = true;

        GameObject ind = SpawnCircleTelegraph(transform.position, 6f);
        yield return new WaitForSeconds(telegraphDuration);

        dragonAnimator.ResetTrigger(AnimDragonTail);
        dragonAnimator.SetTrigger(AnimDragonTail);
        PlayAttackAudio(dragonTailAudio);
        Destroy(ind);

        yield return null; yield return null;
        float animLength = dragonAnimator.GetCurrentAnimatorStateInfo(0).length;
        float preHitDelay = Mathf.Max(0f, animLength - 0.5f);

        yield return new WaitForSeconds(preHitDelay);

        DamageIfPlayerInsideCircle(transform.position, 6f);

        yield return new WaitForSeconds(0.5f);
        isBusy = false;
    }

    private IEnumerator Dragon_ConeOfFire()
    {
        isBusy = true;

        Vector3 atkDir = GetFlatDirectionToPlayer();
        GameObject ind = SpawnConeTelegraph(transform.position, atkDir, 60f, 8f);

        yield return new WaitForSeconds(telegraphDuration);

        dragonAnimator.ResetTrigger(AnimDragonConeOfFire);
        dragonAnimator.SetTrigger(AnimDragonConeOfFire);
        PlayAttackAudio(dragonFireAudio);
        Destroy(ind);

        GameObject fire = null;
        if (dragonFirePrefab && spawnedDragon != null)
        {
            Transform spawnPoint = dragonHeadBone != null ? dragonHeadBone : spawnedDragon.transform;
            fire = Instantiate(dragonFirePrefab, spawnPoint.position, transform.rotation, spawnPoint);
        }

        yield return null; yield return null;
        float animLength = dragonAnimator.GetCurrentAnimatorStateInfo(0).length;

        // Deal 3 instances of continuous fire damage spread across the animation duration
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(animLength / 3f);
            DamageIfPlayerInsideCone(transform.position, atkDir, 60f, 8f);
        }

        if (fire) Destroy(fire);
        isBusy = false;
    }

    #endregion

    #region Movement & Utility

    private bool HasValidNavMeshAgent()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

    private void MoveTowardsPlayer(float minDistance)
    {
        if (player == null || !HasValidNavMeshAgent()) return;

        Vector3 dir = (transform.position - player.position).normalized;
        agent.isStopped = false;
        agent.SetDestination(player.position + dir * minDistance);
    }

    private void StopMoving()
    {
        if (HasValidNavMeshAgent())
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private void HandleContinuousFacing()
    {
        if (player == null || isDead || isTransitioning) return;

        if (!HasValidNavMeshAgent() || agent.velocity.magnitude < 0.05f || isBusy)
        {
            Vector3 dir = GetFlatDirectionToPlayer();
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, faceSpeed * Time.deltaTime);
            }
        }
    }

    private void FacePlayerImmediate()
    {
        if (player == null) return;
        Vector3 dir = GetFlatDirectionToPlayer();
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    private Vector3 GetFlatDirectionToPlayer()
    {
        if (player == null) return transform.forward;
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        return dir.sqrMagnitude < 0.001f ? transform.forward : dir.normalized;
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        currentPhase = BossPhase.Dead;
        requestedPhase = BossPhase.Dead;
        scaledAttackDamage = 0f;

        StopAllCoroutines();
        StopMoving();

        if (currentPhase == BossPhase.Phase2 && dragonAnimator != null)
        {
            dragonAnimator.ResetTrigger(AnimDragonDie);
            dragonAnimator.SetTrigger(AnimDragonDie);
            PlayAttackAudio(dragonDeathClip);
        }

        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (Collider col in cols)
        {
            col.enabled = false;
        }

        if (arenaController != null) arenaController.OnBossDied();
    }

    private void UpdateAnimationsAndAudio()
    {
        bool isMoving = !isDead && !isBusy && HasValidNavMeshAgent() && agent.velocity.magnitude > 0.1f;

        if (currentPhase == BossPhase.Phase1 && humanoidAnimator != null && humanoidModelRoot.activeSelf)
        {
            humanoidAnimator.SetBool(AnimIsWalking, isMoving);
        }
        else if (currentPhase == BossPhase.Phase2 && dragonAnimator != null)
        {
            dragonAnimator.SetBool(AnimIsWalking, isMoving);
        }

        if (isMoving && !walkSource.isPlaying) walkSource.Play();
        else if (!isMoving && walkSource.isPlaying) walkSource.Stop();
    }

    #endregion

    #region Hitbox & Damage Calculation

    private void TryDamagePlayer(float damage)
    {
        if (player == null || damage <= 0f) return;

        // Sending floats and ints to be universally compatible with unknown PlayerHealth scripts
        player.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        player.SendMessage("TakeDamage", Mathf.RoundToInt(damage), SendMessageOptions.DontRequireReceiver);
    }

    private void DamageIfPlayerInsideRectangle(Vector3 center, Quaternion rotation, float width, float length)
    {
        if (player == null) return;

        // Transform player position into local space relative to the rectangle's center and rotation
        Vector3 localPos = Quaternion.Inverse(rotation) * (player.position - center);
        if (Mathf.Abs(localPos.x) <= width * 0.5f && Mathf.Abs(localPos.z) <= length * 0.5f)
        {
            TryDamagePlayer(scaledAttackDamage);
        }
    }

    private void DamageIfPlayerInsideCone(Vector3 origin, Vector3 forward, float angle, float radius)
    {
        if (player == null) return;

        Vector3 toPlayer = player.position - origin;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;

        if (dist > radius || dist < 0.01f) return;

        float playerAngle = Vector3.Angle(forward, toPlayer.normalized);
        if (playerAngle <= angle * 0.5f)
        {
            TryDamagePlayer(scaledAttackDamage);
        }
    }

    private void DamageIfPlayerInsideCircle(Vector3 center, float radius)
    {
        if (player == null) return;

        Vector3 toPlayer = player.position - center;
        toPlayer.y = 0f;
        if (toPlayer.magnitude <= radius)
        {
            TryDamagePlayer(scaledAttackDamage);
        }
    }

    #endregion

    #region Telegraph Generation

    private GameObject SpawnConeTelegraph(Vector3 origin, Vector3 forward, float angle, float length)
    {
        if (forward.sqrMagnitude < 0.001f) forward = transform.forward;
        float halfAngle = angle * 0.5f;

        Vector3[] points = new Vector3[coneArcSegments + 2];
        points[0] = origin + Vector3.up * telegraphYOffset;

        for (int i = 0; i <= coneArcSegments; i++)
        {
            float t = i / (float)coneArcSegments;
            float curAngle = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector3 dir = Quaternion.Euler(0f, curAngle, 0f) * forward;
            points[i + 1] = points[0] + dir.normalized * length;
        }

        return CreateFilledPolygonTelegraph("ConeTelegraph", points);
    }

    private GameObject SpawnRectangleTelegraph(Vector3 center, Quaternion rotation, float width, float length)
    {
        Vector3 baseCenter = center + Vector3.up * telegraphYOffset;
        float halfW = width * 0.5f;
        float halfL = length * 0.5f;

        Vector3 p0 = baseCenter + rotation * new Vector3(-halfW, 0f, -halfL);
        Vector3 p1 = baseCenter + rotation * new Vector3(-halfW, 0f, halfL);
        Vector3 p2 = baseCenter + rotation * new Vector3(halfW, 0f, halfL);
        Vector3 p3 = baseCenter + rotation * new Vector3(halfW, 0f, -halfL);

        Vector3[] rect = { p0, p1, p2, p3 };
        return CreateFilledPolygonTelegraph("RectTelegraph", rect);
    }

    private GameObject SpawnCircleTelegraph(Vector3 center, float radius)
    {
        int segments = 32;
        Vector3 baseCenter = center + Vector3.up * telegraphYOffset;
        Vector3[] points = new Vector3[segments];

        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * 360f;
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            points[i] = baseCenter + dir * radius;
        }

        return CreateFilledPolygonTelegraph("CircleTelegraph", points, true);
    }

    private GameObject CreateFilledPolygonTelegraph(string name, Vector3[] worldPoints, bool isCenterConvex = false)
    {
        GameObject root = new GameObject(name);
        root.transform.position = Vector3.zero;

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(root.transform, false);

        MeshFilter mf = fillObj.AddComponent<MeshFilter>();
        MeshRenderer mr = fillObj.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        Vector3[] verts = new Vector3[worldPoints.Length];
        for (int i = 0; i < worldPoints.Length; i++) verts[i] = worldPoints[i];

        List<int> tris = new List<int>();
        if (isCenterConvex)
        {
            for (int i = 1; i < worldPoints.Length - 1; i++)
            {
                tris.Add(0); tris.Add(i); tris.Add(i + 1);
            }
        }
        else
        {
            for (int i = 1; i < worldPoints.Length - 1; i++)
            {
                tris.Add(0); tris.Add(i); tris.Add(i + 1);
            }
        }

        mesh.vertices = verts;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();

        mf.mesh = mesh;

        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = telegraphFillColor;
        mr.material = mat;

        CreateLineRenderer(root.transform, "Outline", CloseLoop(worldPoints), telegraphOutlineColor);
        spawnedTelegraphs.Add(root);
        return root;
    }

    private LineRenderer CreateLineRenderer(Transform parent, string objName, Vector3[] points, Color color)
    {
        GameObject lineObj = new GameObject(objName);
        lineObj.transform.SetParent(parent);

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = false;
        lr.positionCount = points.Length;
        lr.SetPositions(points);
        lr.startWidth = telegraphLineWidth;
        lr.endWidth = telegraphLineWidth;

        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = color;
        lr.material = mat;

        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.textureMode = LineTextureMode.Stretch;

        return lr;
    }

    private Vector3[] CloseLoop(Vector3[] points)
    {
        Vector3[] closed = new Vector3[points.Length + 1];
        for (int i = 0; i < points.Length; i++) closed[i] = points[i];
        closed[points.Length] = points[0];
        return closed;
    }

    #endregion

    #region Audio & UI Setup
    private void SetupAudioSources()
    {
        dialogueSource = gameObject.AddComponent<AudioSource>();
        dialogueSource.outputAudioMixerGroup = narrationMixer;
        dialogueSource.spatialBlend = 0f;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.outputAudioMixerGroup = sfxMixer;
        sfxSource.spatialBlend = 1f;

        walkSource = gameObject.AddComponent<AudioSource>();
        walkSource.clip = walkingClip;
        walkSource.loop = true;
        walkSource.spatialBlend = 1f;

        musicSourceP1 = gameObject.AddComponent<AudioSource>();
        musicSourceP1.clip = phase1MusicClip;
        musicSourceP1.loop = true;

        musicSourceP2 = gameObject.AddComponent<AudioSource>();
        musicSourceP2.clip = phase2MusicClip;
        musicSourceP2.loop = true;
        musicSourceP2.volume = 0f;
    }

    private IEnumerator PlayDialogue(AudioClip clip)
    {
        if (clip == null) yield break;
        dialogueSource.clip = clip;
        dialogueSource.Play();
        yield return new WaitForSeconds(clip.length);
    }

    private void PlayRotatingVoice(AudioClip[] pool)
    {
        if (pool == null || pool.Length == 0) return;
        dialogueSource.PlayOneShot(pool[voiceIndex % pool.Length]);
        voiceIndex++;
    }

    private void PlayAttackAudio(AudioClip clip)
    {
        if (clip) sfxSource.PlayOneShot(clip);
    }

    private IEnumerator CrossfadeMusic(AudioSource fadeOut, AudioSource fadeIn, float duration)
    {
        fadeIn.Play();
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            fadeOut.volume = Mathf.Lerp(1f, 0f, t / duration);
            fadeIn.volume = Mathf.Lerp(0f, 1f, t / duration);
            yield return null;
        }
        fadeOut.Stop();
    }

    private void UpdateBossUI()
    {
        if (bossHealthBarFill) bossHealthBarFill.fillAmount = maxHP <= 0f ? 0f : currentHP / maxHP;
        if (bossHealthText) bossHealthText.text = $"{Mathf.CeilToInt(currentHP)} / {Mathf.CeilToInt(maxHP)}";
    }
    private Quaternion GetAttackRotation()
    {
        Vector3 dir = GetFlatDirectionToPlayer();
        if (dir == Vector3.zero) return transform.rotation;
        return Quaternion.LookRotation(dir, Vector3.up);
    }

    #endregion
}

// ====================================================================
// HELPER CLASSES
// ====================================================================

public class WrathMeteor : MonoBehaviour
{
    private Transform target;
    private GameObject smashPrefab;
    private GameObject burnPrefab;
    private bool locked = false;
    private Vector3 lockedPos;
    private float dmgToApply;

    public void Setup(Transform pTarget, GameObject pSmash, GameObject pBurn, float scaledDamage)
    {
        target = pTarget;
        smashPrefab = pSmash;
        burnPrefab = pBurn;
        dmgToApply = scaledDamage;
    }

    private void Update()
    {
        if (target == null) return;

        if (!locked)
        {
            Vector3 aimPos = target.position;
            if (Vector3.Distance(transform.position, aimPos) < 5f)
            {
                locked = true;
                lockedPos = aimPos;
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, aimPos, 15f * Time.deltaTime);
            }
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, lockedPos, 20f * Time.deltaTime);

            if (Vector3.Distance(transform.position, lockedPos) < 0.2f)
            {
                if (smashPrefab)
                {
                    GameObject smash = Instantiate(smashPrefab, lockedPos, Quaternion.identity);
                    if (smash.TryGetComponent(out CrystalSmashArea area)) area.damageToDeal = dmgToApply;
                }
                if (burnPrefab)
                {
                    GameObject burn = Instantiate(burnPrefab, lockedPos, Quaternion.identity);
                    if (burn.TryGetComponent(out WrathBurnArea area)) area.baseDamageScale = dmgToApply;
                }
                Destroy(gameObject);
            }
        }
    }
}

public class CrystalSmashArea : MonoBehaviour
{
    public float damageToDeal = 15f;

    private void Start()
    {
        Destroy(gameObject, 0.5f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Sending floats and ints to be universally compatible with unknown PlayerHealth scripts
            other.SendMessage("TakeDamage", damageToDeal, SendMessageOptions.DontRequireReceiver);
            other.SendMessage("TakeDamage", Mathf.RoundToInt(damageToDeal), SendMessageOptions.DontRequireReceiver);
            Debug.Log($"Player hit by Crystal Smash! Dealt {damageToDeal} Damage.");
        }
    }
}

public class WrathBurnArea : MonoBehaviour
{
    public float baseDamageScale = 25f;
    private float tickTimer = 0f;

    private void Start()
    {
        Destroy(gameObject, 10f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) ApplyBurnStacks(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            tickTimer += Time.deltaTime;
            if (tickTimer >= 1f)
            {
                tickTimer = 0f;
                ApplyBurnStacks(other);
                RollBurnDamage(other);
            }
        }
    }

    private void ApplyBurnStacks(Collider playerCollider)
    {
        // playerCollider.GetComponent<PlayerStatusManager>().SetBurnStacks(3);
    }

    private void RollBurnDamage(Collider playerCollider)
    {
        int dmgRoll = Random.Range(1, 7);
        float multiplier = baseDamageScale / 25f;
        int finalTickDamage = Mathf.Max(1, Mathf.RoundToInt(dmgRoll * multiplier));

        playerCollider.SendMessage("TakeDamage", (float)finalTickDamage, SendMessageOptions.DontRequireReceiver);
        playerCollider.SendMessage("TakeDamage", finalTickDamage, SendMessageOptions.DontRequireReceiver);
        Debug.Log($"Burn ticked! Player took {finalTickDamage} damage.");
    }
}