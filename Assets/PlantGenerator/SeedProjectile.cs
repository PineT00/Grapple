using System.Collections;
using MoreMountains.Tools;
using UnityEngine;

public class SeedProjectile : MonoBehaviour
{
    public LayerMask landLayer;
    private PlantLauncher launcher;
    private Coroutine lifetimeCoroutine;
    private bool hasLanded = false;

    public void Initialize(PlantLauncher launcher)
    {
        this.launcher = launcher;
    }

    void OnEnable()
    {
        hasLanded = false;
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("트리거 감지");
        //if (hasLanded) return;

        // 지면이나 착탄 가능한 오브젝트와 충돌
        Debug.Log(other.gameObject.layer);
        if ((landLayer & (1 << other.gameObject.layer)) != 0)
        {
            Debug.Log("레이어 감지");
            hasLanded = true;

            if (lifetimeCoroutine != null)
            {
                StopCoroutine(lifetimeCoroutine);
            }

            Debug.Log("런처 실행");
            launcher.OnSeedLanded(this, transform.position);
        }
    }

    public void StartLifetimeTimer(float lifetime)
    {
        if (lifetimeCoroutine != null)
        {
            StopCoroutine(lifetimeCoroutine);
        }
        lifetimeCoroutine = StartCoroutine(LifetimeRoutine(lifetime));
    }

    IEnumerator LifetimeRoutine(float lifetime)
    {
        yield return new WaitForSeconds(lifetime);

        if (!hasLanded)
        {
            // 착탄하지 않고 시간 초과 시 자동 회수
            launcher.ReturnSeed(this);
        }
    }
}
