using UnityEngine;

[DisallowMultipleComponent]
public sealed class DestructibleMaterialTag : MonoBehaviour
{
    [SerializeField] private DestructibleMaterialType materialType = DestructibleMaterialType.Wood;
    [SerializeField, Min(0f)] private float damageMultiplier = 1f;
    [SerializeField, Min(0f)] private float explosionForceMultiplier = 1f;

    [Header("Material Feedback")]
    [SerializeField] private GameObject hitDecalPrefab;
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private GameObject breakEffectPrefab;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip breakSound;

    public DestructibleMaterialType MaterialType => materialType;
    public float DamageMultiplier => damageMultiplier;
    public float ExplosionForceMultiplier => explosionForceMultiplier;
    public GameObject HitDecalPrefab => hitDecalPrefab;
    public GameObject HitEffectPrefab => hitEffectPrefab;
    public GameObject BreakEffectPrefab => breakEffectPrefab;
    public AudioClip HitSound => hitSound;
    public AudioClip BreakSound => breakSound;
}
