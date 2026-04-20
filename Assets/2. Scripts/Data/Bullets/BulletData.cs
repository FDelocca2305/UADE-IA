using UnityEngine;

namespace ScriptableObjects.Bullets
{
    [CreateAssetMenu(menuName = "Main/Bullets/Bullet Data")]
    public class BulletData : ScriptableObject
    {
        [SerializeField] private BulletObject prefab;
        [SerializeField] private float speed = 10f;
        [SerializeField] private float damage = 25f;
        [SerializeField] private float lifetime = 5f;

        [Header("Visual")]
        [SerializeField] private Color color = Color.yellow;

        [Header("Physics")]
        [SerializeField] private bool useGravity = false;

        public BulletObject Prefab => prefab;
        public float Speed => speed;
        public float Damage => damage;
        public float Lifetime => lifetime;
        public Color Color => color;
        public bool UseGravity => useGravity;

        public void AddSpeed(float delta, float minValue, float maxValue)
        {
            speed = Mathf.Clamp(speed + delta, minValue, maxValue);
        }

        public void AddDamage(float delta, float minValue, float maxValue)
        {
            damage = Mathf.Clamp(damage + delta, minValue, maxValue);
        }
    }
}
