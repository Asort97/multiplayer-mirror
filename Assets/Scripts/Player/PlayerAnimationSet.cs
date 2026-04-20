using UnityEngine;

public enum PlayerAttackAnimationType
{
    Fist = 0,
    Knife = 1,
    Pistol = 2,
    Shotgun = 3
}

[CreateAssetMenu(fileName = "PlayerAnimationSet", menuName = "Last Standing/Player Animation Set")]
public class PlayerAnimationSet : ScriptableObject
{
    [SerializeField] private RuntimeAnimatorController controller;

    public RuntimeAnimatorController Controller => controller;

    public bool HasController => controller != null;
}