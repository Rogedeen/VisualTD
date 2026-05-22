using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "VisualTD/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyType = "Skeleton";
    public string poolTag = "Enemy"; // ObjectPooler'daki tag ile eşleşmeli
    
    [Header("Stats")]
    public float maxHealth = 50f;
    public float moveSpeed = 3.5f;
    public float attackDamage = 5f;
    public float attackRange = 2.5f;
    public float attackCooldown = 1.5f;
    
    [Header("Visuals")]
    public RuntimeAnimatorController animatorController;
    public Color damageFlashColor = Color.red;

    [Header("Economy")]
    public int goldReward = 10;

    [Header("Procedural Spawning")]
    [Tooltip("Düşmanın ne kadar güçlü olduğunu belirtir (Zorluk puanı)")]
    public float difficultyScore = 1f;
}