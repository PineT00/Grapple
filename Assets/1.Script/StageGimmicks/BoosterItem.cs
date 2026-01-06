using UnityEngine;

public class BoosterItem : MonoBehaviour, IUsableItem
{
    [SerializeField] private float boostAmount = 10f;

    public void Use(RagdollCharacterController player)
    {
        player.AddTemporalAccelBonus(boostAmount);
        //Destroy(gameObject);

        Debug.Log("부스트 아이템!");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var controller = other.GetComponentInParent<RagdollCharacterController>();
            Use(controller);
        }
    }
}
