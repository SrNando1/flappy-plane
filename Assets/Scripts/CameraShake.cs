using UnityEngine;

/// <summary>Tremor de câmera com decaimento linear.</summary>
public class CameraShake : MonoBehaviour
{
    Vector3 basePosition;
    float amount;
    float decay;

    void Awake()
    {
        basePosition = transform.localPosition;
    }

    public void Shake(float strength, float duration)
    {
        amount = Mathf.Max(amount, strength);
        decay = strength / Mathf.Max(0.01f, duration);
    }

    void LateUpdate()
    {
        if (amount <= 0f)
        {
            transform.localPosition = basePosition;
            return;
        }

        amount = Mathf.Max(0f, amount - decay * Time.deltaTime);
        transform.localPosition = basePosition + (Vector3)(Random.insideUnitCircle * amount);
    }
}
