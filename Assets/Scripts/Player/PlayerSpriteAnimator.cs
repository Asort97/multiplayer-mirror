using Mirror;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(NetworkIdentity), typeof(Animator))]
public class PlayerSpriteAnimator : NetworkBehaviour
{
    private const int BaseLayerIndex = 0;
    private const float LocomotionBlendDuration = 0.05f;

    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
    private static readonly int AttackTypeHash = Animator.StringToHash("AttackType");
    private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
    private static readonly int IdleStateHash = Animator.StringToHash("Idle");
    private static readonly int WalkStateHash = Animator.StringToHash("Walk");
    private static readonly int DeathStateHash = Animator.StringToHash("Death");
    private static readonly int AttackFistStateHash = Animator.StringToHash("AttackFist");
    private static readonly int AttackKnifeStateHash = Animator.StringToHash("AttackKnife");
    private static readonly int AttackPistolStateHash = Animator.StringToHash("AttackPistol");
    private static readonly int AttackShotgunStateHash = Animator.StringToHash("AttackShotgun");

    private static readonly int[] PistolIdleStateHashes =
    {
        Animator.StringToHash("Main_Idle_Pistol"),
        Animator.StringToHash("Support_Idle_Pistol"),
        Animator.StringToHash("Idle_Pistol")
    };

    private static readonly int[] ShotgunIdleStateHashes =
    {
        Animator.StringToHash("Main_Idle_Shotgun"),
        Animator.StringToHash("Support_Idle_Shotgun"),
        Animator.StringToHash("Idle_Shotgun")
    };

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rigidbody2d;
    [SerializeField] private PlayerAnimationSet mainCharacterSet;
    [SerializeField] private PlayerAnimationSet supportingCharacterSet;
    [SerializeField] private float movementThreshold = 0.0001f;

    private PlayerAnimationSet activeSet;
    private PlayerInventory inventory;
    private Vector3 lastPosition;
    private bool controllerApplied;
    private bool isDead;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null)
            animator = GetComponent<Animator>();
        if (rigidbody2d == null)
            rigidbody2d = GetComponent<Rigidbody2D>();
        if (inventory == null)
            inventory = GetComponent<PlayerInventory>();

        lastPosition = transform.position;
    }

    private void OnEnable()
    {
        lastPosition = transform.position;

        if (Application.isPlaying)
            ResetAnimatorState();
    }

    public override void OnStartClient()
    {
        ApplyAnimationSet();
        ResetAnimatorState();
    }

    public override void OnStartLocalPlayer()
    {
        ApplyAnimationSet();
        ResetAnimatorState();
    }

    public void Configure(Animator targetAnimator, SpriteRenderer targetRenderer, PlayerAnimationSet mainSet, PlayerAnimationSet supportSet)
    {
        animator = targetAnimator;
        spriteRenderer = targetRenderer;
        mainCharacterSet = mainSet;
        supportingCharacterSet = supportSet;
        ApplyAnimationSet();
        ResetAnimatorState();
    }

    public void PlayAttack(PlayerAttackAnimationType attackType)
    {
        if (isDead)
            return;

        if (!EnsureAnimator())
            return;

        animator.ResetTrigger(AttackTriggerHash);
        animator.SetInteger(AttackTypeHash, (int)attackType);
        animator.SetTrigger(AttackTriggerHash);
    }

    public void PlayDeath()
    {
        if (isDead)
            return;

        isDead = true;

        if (!EnsureAnimator())
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = false;
            return;
        }

        animator.ResetTrigger(AttackTriggerHash);
        animator.SetBool(IsDeadHash, true);
    }

    private void Update()
    {
        if (!Application.isPlaying)
            return;

        if (!EnsureAnimator())
            return;

        if (isDead)
            return;

        bool isMoving = IsCurrentlyMoving();
        bool shouldPlayWalk = isMoving && !ShouldUseWeaponHoldIdle();
        lastPosition = transform.position;

        animator.SetBool(IsMovingHash, shouldPlayWalk);
        UpdateLocomotionState(shouldPlayWalk);
    }

    private bool EnsureAnimator()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (rigidbody2d == null)
            rigidbody2d = GetComponent<Rigidbody2D>();

        if (animator == null)
            return false;

        if (!controllerApplied || activeSet == null || animator.runtimeAnimatorController == null)
            ApplyAnimationSet();

        return activeSet != null && activeSet.HasController;
    }

    private void ApplyAnimationSet()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (inventory == null)
            inventory = GetComponent<PlayerInventory>();

        bool useMainCharacterSet = !Application.isPlaying || isLocalPlayer;
        activeSet = useMainCharacterSet ? mainCharacterSet : supportingCharacterSet;

        if (activeSet == null)
            activeSet = mainCharacterSet != null ? mainCharacterSet : supportingCharacterSet;

        controllerApplied = activeSet != null && activeSet.HasController;

        if (controllerApplied && animator.runtimeAnimatorController != activeSet.Controller)
            animator.runtimeAnimatorController = activeSet.Controller;

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;
    }

    private void ResetAnimatorState()
    {
        if (animator == null)
            return;

        animator.Rebind();
        animator.Update(0f);
        animator.SetBool(IsMovingHash, false);
        animator.SetBool(IsDeadHash, isDead);
        animator.SetInteger(AttackTypeHash, 0);
        lastPosition = transform.position;

        if (Application.isPlaying && !isDead)
            UpdateLocomotionState(false);
    }

    private bool IsCurrentlyMoving()
    {
        if (isLocalPlayer && rigidbody2d != null)
            return rigidbody2d.linearVelocity.sqrMagnitude >= movementThreshold;

        return (transform.position - lastPosition).sqrMagnitude >= movementThreshold;
    }

    private void UpdateLocomotionState(bool isMoving)
    {
        if (animator == null)
            return;

        if (animator.IsInTransition(BaseLayerIndex))
            return;

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(BaseLayerIndex);
        if (IsLockedState(currentState))
            return;

        int desiredStateHash = isMoving ? WalkStateHash : GetDesiredIdleStateHash();
        if (!animator.HasState(BaseLayerIndex, desiredStateHash))
            return;

        if (currentState.shortNameHash == desiredStateHash)
            return;

        animator.CrossFadeInFixedTime(desiredStateHash, LocomotionBlendDuration, BaseLayerIndex);
    }

    private int GetDesiredIdleStateHash()
    {
        if (!TryGetHoldIdleStateHash(out int holdIdleStateHash))
            return IdleStateHash;

        return holdIdleStateHash;
    }

    private bool ShouldUseWeaponHoldIdle()
    {
        return TryGetHoldIdleStateHash(out _);
    }

    private bool TryGetHoldIdleStateHash(out int stateHash)
    {
        if (inventory == null)
            inventory = GetComponent<PlayerInventory>();

        ItemData activeItem = inventory != null ? inventory.GetActiveItemData() : null;
        if (activeItem == null || activeItem.itemType != ItemType.Ranged)
        {
            stateHash = IdleStateHash;
            return false;
        }

        if (activeItem.itemName == "Pistol")
        {
            int pistolStateHash = GetFirstAvailableStateHash(PistolIdleStateHashes, 0);
            if (pistolStateHash != 0)
            {
                stateHash = pistolStateHash;
                return true;
            }
        }

        if (activeItem.itemName == "Shotgun")
        {
            int shotgunStateHash = GetFirstAvailableStateHash(ShotgunIdleStateHashes, 0);
            if (shotgunStateHash != 0)
            {
                stateHash = shotgunStateHash;
                return true;
            }
        }

        stateHash = IdleStateHash;
        return false;
    }

    private int GetFirstAvailableStateHash(int[] candidateStateHashes, int fallbackStateHash)
    {
        for (int index = 0; index < candidateStateHashes.Length; index++)
        {
            int candidateStateHash = candidateStateHashes[index];
            if (animator.HasState(BaseLayerIndex, candidateStateHash))
                return candidateStateHash;
        }

        return fallbackStateHash;
    }

    private static bool IsLockedState(AnimatorStateInfo stateInfo)
    {
        int shortNameHash = stateInfo.shortNameHash;
        return shortNameHash == DeathStateHash
            || shortNameHash == AttackFistStateHash
            || shortNameHash == AttackKnifeStateHash
            || shortNameHash == AttackPistolStateHash
            || shortNameHash == AttackShotgunStateHash;
    }
}