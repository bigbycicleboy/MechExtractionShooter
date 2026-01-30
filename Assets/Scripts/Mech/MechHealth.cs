using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;

public class MechHealth : MonoBehaviourPun
{
    public float maxHealth = 100f;
    public float currentHealth;
    public TextMeshProUGUI healthText;
    public Slider healthSlider;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    [PunRPC]
    public void TakeDamage(float amount)
    {
        if (photonView.IsMine)
        {
            currentHealth -= amount;
            
            photonView.RPC("SyncHealth", RpcTarget.AllBuffered, currentHealth);
            
            if (currentHealth <= 0f)
            {
                Die();
            }
        }
    }

    [PunRPC]
    void SyncHealth(float newHealth)
    {
        currentHealth = newHealth;
        UpdateHealthUI();
    }

    void UpdateHealthUI()
    {
        if (healthText != null)
        {
            healthText.text = "Mech Health: " + currentHealth.ToString("F0");
        }
        
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth / maxHealth;
        }
    }

    private void Die()
    {
        if (photonView.IsMine)
        {
            Debug.Log("Mech destroyed!");
            PhotonNetwork.Destroy(transform.root.gameObject);
        }
    }
}