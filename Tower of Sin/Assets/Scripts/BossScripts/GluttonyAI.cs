using UnityEngine;

public class GluttonyAI : MonoBehaviour
{
    public float maxHP = 800f;
    private float currentHP;
    private bool isDead = false;

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHP -= amount;
        currentHP = Mathf.Max(0f, currentHP);
        //UpdateBossUI();

        //if (currentHP <= 0f)
            //Die();
    }

    public void TakeDamage(int amount)
    {
        TakeDamage((float)amount);
    }
}
