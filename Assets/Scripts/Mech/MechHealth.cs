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
        // Only the owner actually modifies health to avoid desyncs
        if (photonView.IsMine)
        {
            currentHealth -= amount;
            
            // Sync the new health value to all clients
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
        // Only the owner destroys the mech
        if (photonView.IsMine)
        {
            Debug.Log("Mech destroyed!");
            PhotonNetwork.Destroy(transform.root.gameObject);
        }
    }
}