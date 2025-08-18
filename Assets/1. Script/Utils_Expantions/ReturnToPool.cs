using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ReturnToPool : MonoBehaviour
{
    public string poolName;

    private ParticleSystem ps;

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    private void OnEnable()
    {
        StartCoroutine(CheckIfAlive());
    }

    private IEnumerator CheckIfAlive() //파티클뿐 아니라 범용적으로 오브젝트 비활성화
    {
        yield return null;

        while (ps.IsAlive(true))
        {
            yield return new WaitForSeconds(0.5f);
        }

        if (!string.IsNullOrEmpty(poolName))
        {
            ParticleManager.Instance.ReturnToPool(poolName, ps);
        }
    }
}