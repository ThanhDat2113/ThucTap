using UnityEngine;
using UnityEngine.Events;

namespace RhythmGame
{
    public class HealthSystem : MonoBehaviour
    {
        [Header("Health Settings")]
        public float maxHealth = 100f;
        public float currentHealth;

        [Header("Health Changes")]
        [Tooltip("Máu hồi khi bấm trúng note (Perfect).")]
        public float perfectHeal = 2f;
        [Tooltip("Máu hồi khi bấm trúng note (Good).")]
        public float goodHeal = 1.5f;
        [Tooltip("Máu hồi khi bấm trúng note (Bad).")]
        public float badHeal = 0.5f;
        [Tooltip("Máu hồi khi giữ xong hold note.")]
        public float holdCompleteHeal = 1f;
        [Tooltip("Máu mất khi miss note. Cần ~12 miss liên tiếp mới hết máu từ full.")]
        public float missDamage = 8f;

        [Header("Events")]
        public UnityEvent<float> OnHealthChanged;
        public UnityEvent OnHealthDepleted;
        
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float NormalizedHealth => maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
        
        public bool IsDead => currentHealth <= 0f;
        
        void Awake()
        {
            currentHealth = maxHealth;
        }
        
        public void ApplyJudgement(Judgement judgement)
        {
            float change = judgement switch
            {
                Judgement.Perfect => perfectHeal,
                Judgement.Good => goodHeal,
                Judgement.Bad => badHeal,
                Judgement.HoldComplete => holdCompleteHeal,
                Judgement.Miss => -missDamage,
                _ => 0f
            };
            
            ModifyHealth(change);
        }
        
        public void ModifyHealth(float amount)
        {
            if (IsDead) return;
            
            currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
            OnHealthChanged?.Invoke(NormalizedHealth);
            
            if (currentHealth <= 0f)
            {
                OnHealthDepleted?.Invoke();
            }
        }
        
        public void ResetHealth()
        {
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(NormalizedHealth);
        }
        
        public void SetHealth(float health)
        {
            currentHealth = Mathf.Clamp(health, 0f, maxHealth);
            OnHealthChanged?.Invoke(NormalizedHealth);
            if (currentHealth <= 0f) OnHealthDepleted?.Invoke();
        }
    }
}